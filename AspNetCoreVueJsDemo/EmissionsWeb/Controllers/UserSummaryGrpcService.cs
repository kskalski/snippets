using Emissions.Data;
using Emissions.Proto.Reports;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Emissions.Controllers {
    [Authorize]
    public class UserSummaryGrpcService : Proto.Services.UserSummaries.UserSummariesBase {
        public UserSummaryGrpcService(ApplicationDbContext context, ILogger<UserSummaryGrpcService> logger) {
            context_ = context;
            log_ = logger;
        }

        public override async Task<UserSummary> GetUserSummaryOfExceededThresholds(
            UserSummaryRequest request, ServerCallContext context) {
            var user_id = currentUserId(context.GetHttpContext().User);
            var until = request.Until?.ToDateTimeOffset() ?? DateTimeOffset.UtcNow;
            until = until.ToOffset(TimeSpan.FromMinutes(request.UntilTzOffsetMinutes));
            int max_num_emissions_items = request.HasMaxNumEmissionsItems ? 
                request.MaxNumEmissionsItems : Parameters.USER_SUMMARY_DEFAULT_EMISSIONS_ITEMS;
            int max_num_expenses_items = request.HasMaxNumExpensesItems ? 
                request.MaxNumExpensesItems : Parameters.USER_SUMMARY_DEFAULT_EXPENSE_ITEMS;

            log_.LogInformation("Calculating user summary for {0} until timestamp {1}", user_id, until);

            var user = await context_.Users.FindAsync(user_id);
            if (user == null)
                throw new RpcException(new Status(StatusCode.NotFound, "User not found: " + user_id));

            var user_entries = context_.CarbonEntries.Where(e => e.UserId == user.Id);

            var exceeded_emissions = await Core.Queries.GroupEmissionsPerDayAndFindExceeding(
                    user_entries, until, max_num_emissions_items, user.DailyEmissionsWarningThreshold).ToListAsync();
            var exceeded_expenses = await Core.Queries.GroupMonthlyExpensesAndFindExceeding(
                    user_entries, until, max_num_expenses_items, (double)user.MontlyExpensesWarningThreshold).ToListAsync();

            return new UserSummary() {
                UserDailyEmissionsLimit = user.DailyEmissionsWarningThreshold,
                Emissions = { exceeded_emissions },
                UserMonthlyExpensesLimit = (double) user.MontlyExpensesWarningThreshold,
                Expenses = { exceeded_expenses }
            };
        }

        string currentUserId(ClaimsPrincipal user) => user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        readonly ApplicationDbContext context_;
        readonly ILogger log_;
    }
}
