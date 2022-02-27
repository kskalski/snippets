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
    [Authorize(Roles = Parameters.ADMIN_ROLE)]
    [ApiController]
    public class AdminReportController : ControllerAppCommon {
        public AdminReportController(ApplicationDbContext context, ILogger<AdminReportController> logger): base(context, logger) {
        }

        /**
         *  GET: api/AdminReport
         *
         * <summary>
         * Calculates added entries stats for the time window spanning configured number of days ending at 'until'
         * datetime (exclusive range) or current time (when not specified).
         * </summary>
         */
        [HttpGet]
        public async Task<ActionResult<AdminReport>> GetAdminReport(DateTime? until) {
            until ??= DateTime.UtcNow;
            log_.LogInformation("Calculating admin start for time interval until {0}", until);
            var emissions_stats = await EmissionsPerUserStatsQuery(context_.CarbonEntries, until.Value).SingleOrDefaultAsync();
            var added_counts = await AddedEntriesDayCountsQuery(context_.CarbonEntries, until.Value).ToArrayAsync();
            return new AdminReport() {
                AddedEntries = CalculateAddedEntriesStats(added_counts),
                UsersEmissions = emissions_stats ?? new AdminReport.EmissionsByUsers()
            };
        }

        public static IQueryable<DayCount> AddedEntriesDayCountsQuery(IQueryable<CarbonEntry> source, DateTime until_timestamp) {
            var since_timestamp = until_timestamp.AddDays(-Parameters.ADMIN_REPORT_ADDED_ENTRIES_WINDOW_DAYS * 2);
            // We need a query grouping data spanning time intervals of day(s) into separate buckets. Each bucket
            // ends at the time of day specified by 'until_timestamp', this way we can easily support alternative
            // requirements on what constitutes a "day".
            // For this purpose we calculate the offset that needs to be applied to UTC timestamps in DB such that
            // their date string would allow us to group data-points into appropriate buckets (thus query can be
            // executed in DB).
            var day_start_h_offset = (until_timestamp.Date - until_timestamp).TotalHours;
            return source
                .Where(e => e.CreationTimestamp >= since_timestamp && e.CreationTimestamp < until_timestamp)
                .GroupBy(e => e.CreationTimestamp.AddHours(day_start_h_offset).Date)
                .Select(g => new DayCount { Day = g.Key.AddHours(-day_start_h_offset) - since_timestamp, NumEntries = g.Count() });
        }

        public static AdminReport.AddedEntriesCounts CalculateAddedEntriesStats(DayCount[] day_counts) {
            var day_buckets = new int[Parameters.ADMIN_REPORT_ADDED_ENTRIES_WINDOW_DAYS * 2];
            foreach (var item in day_counts) {
                int day_index = (int) item.Day.TotalDays;
                day_buckets[day_index] = item.NumEntries;
            }
            return new AdminReport.AddedEntriesCounts() {
                PerDayCounts = day_buckets,
                NumLastWeek = day_buckets.Skip(Parameters.ADMIN_REPORT_ADDED_ENTRIES_WINDOW_DAYS).Sum(),
                NumPrecedingWeek = day_buckets.Take(Parameters.ADMIN_REPORT_ADDED_ENTRIES_WINDOW_DAYS).Sum()
            };
        }

        public static IQueryable<AdminReport.EmissionsByUsers> EmissionsPerUserStatsQuery(IQueryable<CarbonEntry> source, DateTime until_timestamp) {
            var since_timestamp = until_timestamp.AddDays(-Parameters.ADMIN_REPORT_AVG_EMISSIONS_WINDOW_DAYS);
            var filter_query = source.Where(e => e.EmittedTimestamp >= since_timestamp && e.EmittedTimestamp < until_timestamp);
            return filter_query
                .GroupBy(e => e.UserId)
                .Select(g => (new { AverageEmissions = g.Average(e => e.Emissions), SumEmissions = g.Sum(e => e.Emissions) }))
                .GroupBy(e => 1)
                .Select(e => new AdminReport.EmissionsByUsers() {
                    AverageEmissionsPerUser = e.Average(x => x.AverageEmissions),
                    NumActiveUsers = e.Count(),
                    SumAddedEmissions = e.Sum(e => e.SumEmissions)
                });
        }
    }
}
