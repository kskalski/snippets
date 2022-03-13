using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Emissions.Data;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Emissions.Controllers {
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class CarbonEntriesController : ControllerAppCommon {
        public CarbonEntriesController(ApplicationDbContext context, NotificationQueue notifier, ILogger<CarbonEntriesController> logger): 
            base(context, logger) {
            notifier_ = notifier;
        }

        // GET: api/CarbonEntries
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CarbonEntry>>> GetCarbonEntries(DateTime? emitted_since, DateTime? emitted_until, int offset, int limit = int.MaxValue) {
            offset = Math.Max(0, offset);
            limit = Math.Min(limit, Parameters.MAX_NUM_FETCHED_CARBON_ENTRIES);
            IQueryable<CarbonEntry> query = context_.CarbonEntries;

            if (is_regular_user())
                query = query.Where(e => e.UserId == currentUserId());

            if (emitted_since != null)
                query = query.Where(e => e.EmittedTimestamp >= emitted_since);
            if (emitted_until != null)
                query = query.Where(e => e.EmittedTimestamp < emitted_until);
            return await query.OrderByDescending(e => e.EmittedTimestamp).Skip(offset).Take(limit).ToListAsync();
        }

        // GET: api/CarbonEntries/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CarbonEntry>> GetCarbonEntry(long id) {
            var carbon_entry = await context_.CarbonEntries.FindAsync(id);

            if (carbon_entry == null)
                return NotFound();

            if (!can_access_entry(carbon_entry, false))
                return Forbid();

            return carbon_entry;
        }

        // PUT: api/CarbonEntries/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCarbonEntry(long id, CarbonEntry carbon_entry) {
            if (id != carbon_entry.Id)
                return BadRequest();
            if (carbon_entry.EmittedTimestamp.Kind != DateTimeKind.Utc)
                return BadRequest("timestamps should be specified in UTC");
            if (!can_access_entry(carbon_entry, /* check db for existence of Id+UserId pair */ true))
                return Forbid();

            log_.LogDebug("Saving entry with id {0} with emitted time {1} for user {2}", id, carbon_entry.EmittedTimestamp, currentUserId());

            // Use provided entity to save new values in storage, but restrict some fields from being modified,
            // also adjusted based on user's role
            context_.Entry(carbon_entry).State = EntityState.Modified;
            context_.Entry(carbon_entry).Property(e => e.CreationTimestamp).IsModified = false;

            // For regular user we do not update user field, otherwise verify it exists if specified 
            if (is_regular_user() || carbon_entry.UserId == null) {
                context_.Entry(carbon_entry).Property(e => e.UserId).IsModified = false;
            } else {
                if (!await assign_specified_user(carbon_entry))
                    return NotFound($"user {carbon_entry.UserId} not found");
            }

            try {
                await context_.SaveChangesAsync();
                notify(carbon_entry);
            } catch (DbUpdateConcurrencyException) {
                if (!carbon_entry_exists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        void notify(CarbonEntry carbon_entry) {
            notifier_.AddNotification(carbon_entry.UserId, new Proto.Notifications.ListenResponse() { EntriesChanged = true, ReportsChanged = true });
            var current_user_id = currentUserId();
            if (current_user_id != carbon_entry.UserId)
                notifier_.AddNotification(current_user_id, new Proto.Notifications.ListenResponse() { EntriesChanged = true, ReportsChanged = true });
        }

        // POST: api/CarbonEntries
        [HttpPost]
        public async Task<ActionResult<CarbonEntry>> PostCarbonEntry(CarbonEntry carbon_entry) {
            if (carbon_entry.EmittedTimestamp.Kind != DateTimeKind.Utc)
                return BadRequest("timestamps should be specified in UTC");

            if (is_regular_user())
                carbon_entry.UserId = currentUserId();
            if (!await assign_specified_user(carbon_entry))
                return NotFound($"user {carbon_entry.UserId} doesn't exist");

            carbon_entry.CreationTimestamp = DateTime.UtcNow;

            context_.CarbonEntries.Add(carbon_entry);
            await context_.SaveChangesAsync();
            notify(carbon_entry);

            return CreatedAtAction("GetCarbonEntry", new { id = carbon_entry.Id }, carbon_entry);
        }

        async Task<bool> assign_specified_user(CarbonEntry carbon_entry) {
            var user_id = carbon_entry.UserId;
            if (user_id == null)
                return false;
            var application_user = await context_.Users.FindAsync(user_id);
            if (application_user == null)
                return false;
            carbon_entry.User = application_user;
            return true;
        }

        // DELETE: api/CarbonEntries/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCarbonEntry(long id) {
            var carbon_entry = await context_.CarbonEntries.FindAsync(id);
            if (carbon_entry == null)
                return NotFound();
            if (!can_access_entry(carbon_entry, false))
                return Forbid();

            context_.CarbonEntries.Remove(carbon_entry);
            await context_.SaveChangesAsync();
            notify(carbon_entry);

            return NoContent();
        }

        bool carbon_entry_exists(long id) => context_.CarbonEntries.Any(e => e.Id == id);
        bool can_access_entry(CarbonEntry entry, bool check_db) {
            if (is_admin())
                return true;
            if (entry.UserId != null && entry.UserId != currentUserId())
                return false;
            if (check_db)
                return context_.CarbonEntries.Any(e => e.Id == entry.Id && e.UserId == currentUserId());
            return entry.UserId != null;
        }

        bool is_admin() => User.IsInRole(Parameters.ADMIN_ROLE);
        bool is_regular_user() => !is_admin();

        NotificationQueue notifier_;
    }
}
