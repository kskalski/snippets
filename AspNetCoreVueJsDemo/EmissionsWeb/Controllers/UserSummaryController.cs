using Emissions.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Emissions.Controllers {
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class UserSummaryController : ControllerAppCommon {
        public UserSummaryController(ApplicationDbContext context, ILogger<UserSummaryController> logger): base(context, logger) {
        }

        // GET: api/UserSummary
        [HttpGet]
        public async Task<ActionResult<UserSummary>> GetUserSummaryOfExceededThresholds(
            DateTimeOffset? until, 
            int max_num_emissions_items = Parameters.USER_SUMMARY_DEFAULT_EMISSIONS_ITEMS, 
            int max_num_expenses_items = Parameters.USER_SUMMARY_DEFAULT_EXPENSE_ITEMS) {

            until ??= DateTimeOffset.UtcNow;

            log_.LogInformation("Calculating user summary for {0} until timestamp {1}", currentUserId(), until);

            var user = await context_.Users.FindAsync(currentUserId());
            if (user == null)
                return NotFound("User not found: " + currentUserId());

            var user_entries = context_.CarbonEntries.Where(e => e.UserId == user.Id);

            return new UserSummary() {
                UserDailyEmissionsLimit = user.DailyEmissionsWarningThreshold,
                Emissions = await Core.Queries.GroupEmissionsPerDayAndFindExceeding(
                    user_entries, until.Value, max_num_emissions_items, user.DailyEmissionsWarningThreshold).ToListAsync(),
                UserMonthlyExpensesLimit = user.MontlyExpensesWarningThreshold,
                Expenses = await Core.Queries.GroupMonthlyExpensesAndFindExceeding(
                    user_entries, until.Value, max_num_expenses_items, user.MontlyExpensesWarningThreshold).ToListAsync()
            };
        }
    }
}
