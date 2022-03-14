using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Grpc.Core;

namespace Emissions.Controllers {
    [Authorize]
    public class CarbonEntriesGrpcService : Proto.Services.CarbonEntries.CarbonEntriesBase {
        public CarbonEntriesGrpcService(Data.ApplicationDbContext context, NotificationQueue notifier, ILogger<CarbonEntriesGrpcService> logger) {
            db_ = context;
            notifier_ = notifier;
            log_ = logger;
        }

        public override async Task<Proto.GetCarbonEntriesResponse> GetEntries(Proto.GetCarbonEntriesRequest request, ServerCallContext context) {
            var offset = Math.Max(0, request.Offset);
            var limit = Math.Min(request.HasLimit ? request.Limit : int.MaxValue, Data.Parameters.MAX_NUM_FETCHED_CARBON_ENTRIES);
            IQueryable<Data.CarbonEntry> query = db_.CarbonEntries;

            if (is_regular_user(context))
                query = query.Where(e => e.UserId == currentUserId(context));

            if (request.EmittedSince != null)
                query = query.Where(e => e.EmittedTimestamp >= request.EmittedSince.ToDateTime());
            if (request.EmittedUntil != null)
                query = query.Where(e => e.EmittedTimestamp < request.EmittedUntil.ToDateTime());
            return new Proto.GetCarbonEntriesResponse() {
                Entries = { 
                    await query.OrderByDescending(e => e.EmittedTimestamp).Skip(offset).Take(limit)
                       .Select(e => e.Raw).ToListAsync() 
                }
            };
        }

        public override async Task<Proto.CarbonEntry> GetEntry(Proto.CarbonEntry entry, ServerCallContext context) {
            var carbon_entry = await db_.CarbonEntries.FindAsync(entry.Id);

            if (carbon_entry == null)
                throw new RpcException(new Status(StatusCode.NotFound, $"not found id {entry.Id}"));

            if (!can_access_entry(context, carbon_entry.Raw, false))
                throw new RpcException(new Status(StatusCode.PermissionDenied, $"can't access id {entry.Id}"));

            return carbon_entry.Raw;
        }

        public async override Task<Proto.CarbonEntry> AddEntry(Proto.CarbonEntry entry, ServerCallContext context) {
            if (is_regular_user(context))
                entry.UserId = currentUserId(context);

            var db_entry = new Data.CarbonEntry(entry);
            if (!await assign_specified_user(db_entry))
                throw new RpcException(new Status(StatusCode.NotFound, $"user {db_entry.UserId} not found"));

            db_entry.CreationTimestamp = DateTime.UtcNow;

            db_entry = db_.CarbonEntries.Add(db_entry).Entity;
            await db_.SaveChangesAsync();
            notify(context, entry);

            return db_entry.Raw;
        }

        public async override Task<Proto.CarbonEntry> UpdateEntry(Proto.CarbonEntry entry, ServerCallContext context) {
            if (!can_access_entry(context, entry, /* check db for existence of Id+UserId pair */ true))
                throw new RpcException(new Status(StatusCode.PermissionDenied, $"can't access id {entry.Id}"));

            log_.LogDebug("Saving entry with id {0} with emitted time {1} for user {2}", entry.Id, entry.EmittedTimestamp, currentUserId(context));

            // Use provided entity to save new values in storage, but restrict some fields from being modified,
            // also adjusted based on user's role
            var db_entry = new Data.CarbonEntry(entry);
            db_.Entry(db_entry).State = EntityState.Modified;
            db_.Entry(db_entry).Property(e => e.CreationTimestamp).IsModified = false;

            // For regular user we do not update user field, otherwise verify it exists if specified 
            if (is_regular_user(context) || string.IsNullOrEmpty(entry.UserId)) {
                db_.Entry(db_entry).Property(e => e.UserId).IsModified = false;
            } else {
                if (!await assign_specified_user(db_entry))
                    throw new RpcException(new Status(StatusCode.NotFound, $"user {db_entry.UserId} not found"));
            }

            try {
                await db_.SaveChangesAsync();
                notify(context, entry);
            } catch (DbUpdateConcurrencyException) {
                if (!carbon_entry_exists(db_entry.Id))
                    throw new RpcException(new Status(StatusCode.NotFound, $"not found id {db_entry.Id}"));
                else
                    throw;
            }

            return entry;
        }

        public override async Task<Proto.CarbonEntry> DeleteEntry(Proto.CarbonEntry entry, ServerCallContext context) {
            var db_entry = await db_.CarbonEntries.FindAsync(entry.Id);
            if (db_entry == null)
                throw new RpcException(new Status(StatusCode.NotFound, $"not found id {entry.Id}"));
            if (!can_access_entry(context, entry, false))
                throw new RpcException(new Status(StatusCode.PermissionDenied, $"can't access id {entry.Id}"));

            db_.CarbonEntries.Remove(db_entry);
            await db_.SaveChangesAsync();
            notify(context, entry);

            return entry;
        }

        void notify(ServerCallContext ctx, Proto.CarbonEntry carbon_entry) {
            notifier_.AddNotification(carbon_entry.UserId, new Proto.Notifications.ListenResponse() { EntriesChanged = true, ReportsChanged = true });
            var current_user_id = currentUserId(ctx);
            if (current_user_id != carbon_entry.UserId)
                notifier_.AddNotification(current_user_id, new Proto.Notifications.ListenResponse() { EntriesChanged = true, ReportsChanged = true });
        }

        async Task<bool> assign_specified_user(Data.CarbonEntry carbon_entry) {
            var user_id = carbon_entry.UserId;
            if (string.IsNullOrEmpty(user_id))
                return false;
            var application_user = await db_.Users.FindAsync(user_id);
            if (application_user == null)
                return false;
            carbon_entry.User = application_user;
            return true;
        }

        bool carbon_entry_exists(long id) => db_.CarbonEntries.Any(e => e.Id == id);
        bool can_access_entry(ServerCallContext ctx, Proto.CarbonEntry entry, bool check_db) {
            if (is_admin(ctx))
                return true;
            if (!string.IsNullOrEmpty(entry.UserId) && entry.UserId != currentUserId(ctx))
                return false;
            if (check_db)
                return db_.CarbonEntries.Any(e => e.Id == entry.Id && e.UserId == currentUserId(ctx));
            return entry.UserId != null;
        }

        static string currentUserId(ServerCallContext ctx) => ctx.GetHttpContext().User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        static bool is_admin(ServerCallContext ctx) => ctx.GetHttpContext().User.IsInRole(Data.Parameters.ADMIN_ROLE);
        static bool is_regular_user(ServerCallContext ctx) => !is_admin(ctx);

        readonly Data.ApplicationDbContext db_;
        readonly NotificationQueue notifier_;
        readonly ILogger log_;
    }
}
