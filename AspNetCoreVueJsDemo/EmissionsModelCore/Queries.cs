using Emissions.Proto.Reports;
using Emissions.Data;
using GPW = Google.Protobuf.WellKnownTypes;

namespace Emissions.Core {
    public class Queries {
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
                .Select(g => new Proto.Reports.DayCount { Day = GPW.Duration.FromTimeSpan(g.Key.AddHours(-day_start_h_offset) - since_timestamp), NumEntries = g.Count() });
        }
        public static AdminReport.Types.AddedEntriesCounts CalculateAddedEntriesStats(Proto.Reports.DayCount[] day_counts) {
            var day_buckets = new int[Parameters.ADMIN_REPORT_ADDED_ENTRIES_WINDOW_DAYS * 2];
            foreach (var item in day_counts) {
                int day_index = (int)item.Day.ToTimeSpan().TotalDays;
                day_buckets[day_index] = item.NumEntries;
            }
            return new AdminReport.Types.AddedEntriesCounts() {
                PerDayCounts = { day_buckets },
                NumLastWeek = day_buckets.Skip(Parameters.ADMIN_REPORT_ADDED_ENTRIES_WINDOW_DAYS).Sum(),
                NumPrecedingWeek = day_buckets.Take(Parameters.ADMIN_REPORT_ADDED_ENTRIES_WINDOW_DAYS).Sum()
            };
        }

        public static IQueryable<AdminReport.Types.EmissionsByUsers> EmissionsPerUserStatsQuery(IQueryable<CarbonEntry> source, DateTime until_timestamp) {
            var since_timestamp = until_timestamp.AddDays(-Parameters.ADMIN_REPORT_AVG_EMISSIONS_WINDOW_DAYS);
            var filter_query = source.Where(e => e.EmittedTimestamp >= since_timestamp && e.EmittedTimestamp < until_timestamp);
            return filter_query
                .GroupBy(e => e.UserId)
                .Select(g => (new { AverageEmissions = g.Average(e => e.Emissions), SumEmissions = g.Sum(e => e.Emissions) }))
                .GroupBy(e => 1)
                .Select(e => new AdminReport.Types.EmissionsByUsers() {
                    AverageEmissionsPerUser = e.Average(x => x.AverageEmissions),
                    NumActiveUsers = e.Count(),
                    SumAddedEmissions = e.Sum(e => e.SumEmissions)
                });
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
                    e.EmittedTimestamp.AddHours(utc_to_user_offset_h).Year,
                    e.EmittedTimestamp.AddHours(utc_to_user_offset_h).Month
                })
                .Select(g => new UserSummary.ExpensesExceededItem() {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Expenses = g.Sum(e => e.Price!.Value)
                })
                .Where(e => e.Expenses > expenses_threshold)
                .OrderByDescending(e => e.Year).ThenByDescending(e => e.Month)
                .Take(max_num_items);
        }
    }
}
