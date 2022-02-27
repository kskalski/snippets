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
                Emissions = await GroupEmissionsPerDayAndFindExceeding(
                    user_entries, until.Value, max_num_emissions_items, user.DailyEmissionsWarningThreshold).ToListAsync(),
                UserMonthlyExpensesLimit = user.MontlyExpensesWarningThreshold,
                Expenses = await GroupMonthlyExpensesAndFindExceeding(
                    user_entries, until.Value, max_num_expenses_items, user.MontlyExpensesWarningThreshold).ToListAsync()
            };
        }

        public static IQueryable<UserSummary.EmissionsExceededItem> GroupEmissionsPerDayAndFindExceeding(
            IQueryable<CarbonEntry> source, 
            DateTimeOffset until_timestamp, 
            int max_num_items, 
            double emissions_threshold) {

            until_timestamp = Utils.Dates.MoveUpToNearestMidnight(until_timestamp);
            var utc_to_user_offset_h = until_timestamp.Offset.TotalHours;
            return source
                .Where(e => e.EmittedTimestamp < until_timestamp.UtcDateTime)
                .GroupBy(e => e.EmittedTimestamp.AddHours(utc_to_user_offset_h).Date)
                .Select(g => new UserSummary.EmissionsExceededItem() {
                    Day = g.Key.AddHours(-utc_to_user_offset_h),
                    Emissions = g.Sum(e => e.Emissions)
                })
                .Where(e => e.Emissions > emissions_threshold)
                .OrderByDescending(e => e.Day)
                .Take(max_num_items);
        }

        public static IQueryable<UserSummary.ExpensesExceededItem> GroupMonthlyExpensesAndFindExceeding(
            IQueryable<CarbonEntry> source, 
            DateTimeOffset until_timestamp, 
            int max_num_items, 
            decimal expenses_threshold) {

            until_timestamp = Utils.Dates.MoveUpToNearestMonthStart(until_timestamp);
            var utc_to_user_offset_h = until_timestamp.Offset.TotalHours;
            return source
                .Where(e => e.EmittedTimestamp < until_timestamp.UtcDateTime && e.Price != null)
                .GroupBy(e => new {
                    Year = e.EmittedTimestamp.AddHours(utc_to_user_offset_h).Year,
                    Month = e.EmittedTimestamp.AddHours(utc_to_user_offset_h).Month
                })
                .Select(g => new UserSummary.ExpensesExceededItem() {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Expenses = g.Sum(e => e.Price.Value)
                })
                .Where(e => e.Expenses > expenses_threshold)
                .OrderByDescending(e => e.Year).ThenByDescending(e => e.Month)
                .Take(max_num_items);
        }
    }
}
