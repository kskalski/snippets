using Emissions.Proto.Reports;
using Emissions.Data;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Emissions.Test {
    public class UserSummaryQueryTest {
        [SetUp]
        public void Setup() {
            entries_.Clear();
        }

        List<UserSummary.Types.EmissionsExceededItem> run_emissions_query(DateTimeOffset until_timestamp, int max_items, double threshold) {
            return Core.Queries.GroupEmissionsPerDayAndFindExceeding(
                entries_.AsQueryable(), until_timestamp, max_items, threshold).ToList();
        }
        List<UserSummary.Types.ExpensesExceededItem> run_expenses_query(DateTimeOffset until_timestamp, int max_items, double threshold) {
            return Core.Queries.GroupMonthlyExpensesAndFindExceeding(
                entries_.AsQueryable(), until_timestamp, max_items, threshold).ToList();
        }

        [Test]
        public void EmissionsEmpty() {
            Assert.AreEqual(0, run_emissions_query(MAX_UTC_DATE, int.MaxValue, 0).Count);
        }

        [Test]
        public void EmissionsSingleDayLimitedToEmpty() {
            var time = new DateTime(2022, 2, 21, 10, 0, 0, DateTimeKind.Utc);
            entries_.Add(new CarbonEntry() { EmittedTimestamp = time, Emissions = 100 });
            Assert.AreEqual(0, run_emissions_query(MAX_UTC_DATE, int.MaxValue, 1000).Count);

            entries_.Add(new CarbonEntry() { EmittedTimestamp = time.AddHours(1), Emissions = 200 });
            Assert.AreEqual(0, run_emissions_query(MAX_UTC_DATE, int.MaxValue, 1000).Count);

            entries_.Add(new CarbonEntry() { EmittedTimestamp = time.AddHours(2), Emissions = 300 });
            Assert.AreEqual(0, run_emissions_query(MAX_UTC_DATE, int.MaxValue, 1000).Count);

            entries_.Add(new CarbonEntry() { EmittedTimestamp = time.AddHours(3), Emissions = 400 });
            Assert.AreEqual(0, run_emissions_query(MAX_UTC_DATE, int.MaxValue, 1000).Count);
            Assert.AreEqual(1000, entries_.Sum(e => e.Emissions));

            Assert.AreEqual(0, run_emissions_query(time.Date, int.MaxValue, 0).Count, "entries should be out of datetime limit");
            Assert.AreEqual(0, run_emissions_query(MAX_UTC_DATE, 0, 0).Count, "entries should be out of max_items limit");
        }

        [Test]
        public void EmissionsSingleDayExceedingLimit() {
            var day_start_time = new DateTime(2022, 2, 21, 0, 0, 0, DateTimeKind.Utc);
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time, Emissions = 200 });
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time.AddHours(10), Emissions = 300 });
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time.AddHours(23).AddMinutes(59), Emissions = 501 });
            
            var items = run_emissions_query(MAX_UTC_DATE, int.MaxValue, 1000);
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(1001, items[0].Emissions);
            Assert.AreEqual(new DateTime(2022, 2, 21, 0, 0, 0, DateTimeKind.Utc), items[0].Day);

            items = run_emissions_query(day_start_time.AddDays(1), int.MaxValue, 1000);
            Assert.AreEqual(1, items.Count, "time limit should contain all above items (single UTC day)");
            Assert.AreEqual(1001, items[0].Emissions);
            Assert.AreEqual(new DateTime(2022, 2, 21, 0, 0, 0, DateTimeKind.Utc), items[0].Day);
        }

        [Test]
        public void EmissionsMultipleDays() {
            var day_start_time = new DateTime(2022, 2, 21, 0, 0, 0, DateTimeKind.Utc);
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time, Emissions = 2000 });
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time.AddDays(1), Emissions = 300 });
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time.AddDays(2), Emissions = 500 });
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time.AddDays(2).AddHours(5), Emissions = 501 });

            var items = run_emissions_query(MAX_UTC_DATE, int.MaxValue, 1000);
            Assert.AreEqual(2, items.Count);
            Assert.AreEqual(1001, items[0].Emissions);
            Assert.AreEqual(day_start_time.AddDays(2), items[0].Day);
            Assert.AreEqual(2000, items[1].Emissions);
            Assert.AreEqual(day_start_time, items[1].Day);

            items = run_emissions_query(day_start_time.AddDays(3), int.MaxValue, 1000);
            Assert.AreEqual(2, items.Count, "time limit should contain all above items (three UTC days)");

            items = run_emissions_query(day_start_time.AddDays(1), int.MaxValue, 1000);
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(2000, items[0].Emissions);
            Assert.AreEqual(day_start_time, items[0].Day);

            items = run_emissions_query(day_start_time.AddDays(3), 1, 1000);
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(1001, items[0].Emissions);
            Assert.AreEqual(day_start_time.AddDays(2), items[0].Day);

            items = run_emissions_query(day_start_time.AddDays(3), 2, 200);
            Assert.AreEqual(2, items.Count);
            Assert.AreEqual(1001, items[0].Emissions);
            Assert.AreEqual(day_start_time.AddDays(2), items[0].Day);
            Assert.AreEqual(300, items[1].Emissions);
            Assert.AreEqual(day_start_time.AddDays(1), items[1].Day);
        }

        [Test]
        public void EmissionsNotAlignedToUTC() {
            var day_start_time = new DateTime(2022, 2, 21, 10, 45, 0, DateTimeKind.Utc);
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time.AddMinutes(-1), Emissions = 1 });
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time, Emissions = 200 });
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time.AddHours(10), Emissions = 300 });
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time.AddHours(23).AddMinutes(59), Emissions = 501 });
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time.AddDays(1), Emissions = 210 });
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time.AddDays(1).AddHours(10), Emissions = 310 });
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time.AddDays(1).AddHours(23).AddMinutes(59), Emissions = 511 });
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time.AddDays(2), Emissions = 2 });

            var items = run_emissions_query(new DateTimeOffset(2022, 2, 22, 0, 0, 0, new TimeSpan(-10, -45, 0)), int.MaxValue, 1000);
            Assert.AreEqual(1, items.Count, "time limit should contain items from first 24h (single day in requested timezone)");
            Assert.AreEqual(1001, items[0].Emissions);
            Assert.AreEqual(new DateTime(2022, 2, 21, 10, 45, 0, DateTimeKind.Utc), items[0].Day);

            items = run_emissions_query(new DateTimeOffset(2022, 2, 23, 0, 0, 0, new TimeSpan(-10, -45, 0)), int.MaxValue, 1000);
            Assert.AreEqual(2, items.Count, "time limit should contain all above items (two days in requested timezone)");
            Assert.AreEqual(1031, items[0].Emissions);
            Assert.AreEqual(new DateTime(2022, 2, 22, 10, 45, 0, DateTimeKind.Utc), items[0].Day);
            Assert.AreEqual(1001, items[1].Emissions);
            Assert.AreEqual(new DateTime(2022, 2, 21, 10, 45, 0, DateTimeKind.Utc), items[1].Day);
        }

        [Test]
        public void ExpensesEmpty() {
            Assert.AreEqual(0, run_expenses_query(MAX_UTC_DATE, int.MaxValue, 0).Count);
        }

        [Test]
        public void ExpensesSingleMonthLimitedToEmpty() {
            var time = new DateTime(2022, 2, 21, 10, 0, 0, DateTimeKind.Utc);
            entries_.Add(new CarbonEntry() { EmittedTimestamp = time, Price = 100 });
            Assert.AreEqual(0, run_expenses_query(MAX_UTC_DATE, int.MaxValue, 1000).Count);

            entries_.Add(new CarbonEntry() { EmittedTimestamp = time.AddHours(1), Price = 200 });
            Assert.AreEqual(0, run_expenses_query(MAX_UTC_DATE, int.MaxValue, 1000).Count);

            entries_.Add(new CarbonEntry() { EmittedTimestamp = time.AddHours(2), Price = 300 });
            Assert.AreEqual(0, run_expenses_query(MAX_UTC_DATE, int.MaxValue, 1000).Count);

            entries_.Add(new CarbonEntry() { EmittedTimestamp = time.AddHours(3), Price = 400 });
            Assert.AreEqual(0, run_expenses_query(MAX_UTC_DATE, int.MaxValue, 1000).Count);
            Assert.AreEqual(1000, entries_.Sum(e => e.Price));

            Assert.AreEqual(0, run_expenses_query(new DateTime(2022, 1, 30), int.MaxValue, 0).Count, "entries should be out of datetime limit");
            Assert.AreEqual(0, run_expenses_query(MAX_UTC_DATE, 0, 0).Count, "entries should be out of max_items limit");
        }

        [Test]
        public void ExpensesSingleMonthExceedingLimit() {
            var day_start_time = new DateTime(2022, 2, 21, 0, 0, 0, DateTimeKind.Utc);
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time, Price = 200 });
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time.AddHours(10), Price = 300 });
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time.AddHours(23).AddMinutes(59), Price = 501 });

            var items = run_expenses_query(MAX_UTC_DATE, int.MaxValue, 1000);
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(1001, items[0].Expenses);
            Assert.AreEqual(2022, items[0].Year);
            Assert.AreEqual(2, items[0].Month);

            items = run_expenses_query(day_start_time.AddDays(1), int.MaxValue, 1000);
            Assert.AreEqual(1, items.Count, "time limit should contain all above items (single UTC day)");
            Assert.AreEqual(1001, items[0].Expenses);
            Assert.AreEqual(2022, items[0].Year);
            Assert.AreEqual(2, items[0].Month);
        }

        [Test]
        public void ExpensesMultipleMonths() {
            var day_start_time = new DateTime(2022, 2, 21, 0, 0, 0, DateTimeKind.Utc);
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time, Price = 2000 });
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time.AddMonths(1), Price = 300 });
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time.AddMonths(2), Price = 500 });
            entries_.Add(new CarbonEntry() { EmittedTimestamp = day_start_time.AddMonths(2).AddHours(5), Price = 501 });

            var items = run_expenses_query(MAX_UTC_DATE, int.MaxValue, 1000);
            Assert.AreEqual(2, items.Count);
            Assert.AreEqual(1001, items[0].Expenses);
            Assert.AreEqual(2022, items[0].Year);
            Assert.AreEqual(4, items[0].Month);
            Assert.AreEqual(2000, items[1].Expenses);
            Assert.AreEqual(2022, items[1].Year);
            Assert.AreEqual(2, items[1].Month);

            items = run_expenses_query(day_start_time.AddMonths(3), int.MaxValue, 1000);
            Assert.AreEqual(2, items.Count, "time limit should contain all above items (three UTC days)");

            items = run_expenses_query(day_start_time.AddMonths(1), int.MaxValue, 1000);
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(2000, items[0].Expenses);
            Assert.AreEqual(2022, items[0].Year);
            Assert.AreEqual(2, items[0].Month);

            items = run_expenses_query(day_start_time.AddMonths(3), 1, 1000);
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(1001, items[0].Expenses);
            Assert.AreEqual(2022, items[0].Year);
            Assert.AreEqual(4, items[0].Month);

            items = run_expenses_query(day_start_time.AddMonths(3), 2, 200);
            Assert.AreEqual(2, items.Count);
            Assert.AreEqual(1001, items[0].Expenses);
            Assert.AreEqual(2022, items[0].Year);
            Assert.AreEqual(4, items[0].Month);
            Assert.AreEqual(300, items[1].Expenses);
            Assert.AreEqual(2022, items[1].Year);
            Assert.AreEqual(3, items[1].Month);
        }

        [Test]
        public void ExpensesNotAlignedToUTC() {
            var month_starts = new [] {
                Tuple.Create(new DateTime(2022, 2, 1, 10, 45, 0, DateTimeKind.Utc), new TimeSpan(-10, -45, 0)),
                Tuple.Create(new DateTime(2022, 1, 31, 20, 45, 0, DateTimeKind.Utc), new TimeSpan(3, 15, 0))
            };

            foreach (var (month_start_time, offset) in month_starts) {
                entries_.Clear();
                entries_.Add(new CarbonEntry() { EmittedTimestamp = month_start_time.AddMinutes(-1), Price = 1 });
                entries_.Add(new CarbonEntry() { EmittedTimestamp = month_start_time, Price = 200 });
                entries_.Add(new CarbonEntry() { EmittedTimestamp = month_start_time.AddDays(10), Price = 300 });
                entries_.Add(new CarbonEntry() { EmittedTimestamp = month_start_time.AddDays(27).AddHours(23), Price = 501 });
                entries_.Add(new CarbonEntry() { EmittedTimestamp = month_start_time.AddDays(28), Price = 210 });
                entries_.Add(new CarbonEntry() { EmittedTimestamp = month_start_time.AddDays(28 + 10), Price = 310 });
                entries_.Add(new CarbonEntry() { EmittedTimestamp = month_start_time.AddDays(28 + 30).AddHours(23), Price = 511 });
                entries_.Add(new CarbonEntry() { EmittedTimestamp = month_start_time.AddDays(28 + 31), Price = 2 });

                var items = run_expenses_query(new DateTimeOffset(2022, 3, 1, 0, 0, 0, offset), int.MaxValue, 1000);
                Assert.AreEqual(1, items.Count, "time limit should contain items from first 24h (single day in requested timezone)");
                Assert.AreEqual(1001, items[0].Expenses, $"for {month_start_time}");
                Assert.AreEqual(2022, items[0].Year);
                Assert.AreEqual(2, items[0].Month);

                items = run_expenses_query(new DateTimeOffset(2022, 4, 1, 0, 0, 0, offset), int.MaxValue, 1000);
                Assert.AreEqual(2, items.Count, "time limit should contain all above items (two days in requested timezone)");
                Assert.AreEqual(1031, items[0].Expenses);
                Assert.AreEqual(2022, items[0].Year);
                Assert.AreEqual(3, items[0].Month);
                Assert.AreEqual(1001, items[1].Expenses);
                Assert.AreEqual(2022, items[1].Year);
                Assert.AreEqual(2, items[1].Month);
            }
        }

        static readonly DateTime MAX_UTC_DATE = DateTime.MaxValue.AddMonths(-1).ToUniversalTime().Date;

        readonly List<CarbonEntry> entries_ = new();
    }
}