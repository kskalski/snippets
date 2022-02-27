using Emissions.Data;
using Emissions.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emissions.Test {
    public class AdminReportQueryTest {
        [SetUp]
        public void Setup() {
            entries_.Clear();
        }

        AdminReport.AddedEntriesCounts run_added_entries_query(DateTime until_timestamp) {
            var day_counts = Controllers.AdminReportController.AddedEntriesDayCountsQuery(entries_.AsQueryable(), until_timestamp).ToArray();
            return Controllers.AdminReportController.CalculateAddedEntriesStats(day_counts);
        }
        AdminReport.EmissionsByUsers? run_avg_emissions_query(DateTime until_timestamp) {
            return Controllers.AdminReportController.EmissionsPerUserStatsQuery(entries_.AsQueryable(), until_timestamp).SingleOrDefault();
        }

        [Test]
        public void EmissionsEmpty() {
            Assert.IsNull(run_avg_emissions_query(new DateTime(2022, 2, 2, 2, 2, 2)));
        }
        
        [Test]
        public void EmissionsSingleUser() {
            var time = new DateTime(2022, 2, 21, 10, 0, 0, DateTimeKind.Utc);
            entries_.Add(new CarbonEntry() { UserId = "1", EmittedTimestamp = time, Emissions = 100 });
            var stats = run_avg_emissions_query(time.AddDays(2));
            Assert.IsNotNull(stats);
            Assert.AreEqual(100, stats!.AverageEmissionsPerUser);
            Assert.AreEqual(100, stats.SumAddedEmissions);
            Assert.AreEqual(1, stats.NumActiveUsers);

            entries_.Add(new CarbonEntry() { UserId = "1", EmittedTimestamp = time.AddHours(1), Emissions = 200 });
            stats = run_avg_emissions_query(time.AddDays(2));
            Assert.IsNotNull(stats);
            Assert.AreEqual(150, stats!.AverageEmissionsPerUser);
            Assert.AreEqual(300, stats.SumAddedEmissions);
            Assert.AreEqual(1, stats.NumActiveUsers);

            entries_.Add(new CarbonEntry() { UserId = "1", EmittedTimestamp = time.AddHours(2), Emissions = 300 });
            stats = run_avg_emissions_query(time.AddDays(2));
            Assert.IsNotNull(stats);
            Assert.AreEqual(200, stats!.AverageEmissionsPerUser);
            Assert.AreEqual(600, stats.SumAddedEmissions);
            Assert.AreEqual(1, stats.NumActiveUsers);

            entries_.Add(new CarbonEntry() { UserId = "1", EmittedTimestamp = time.AddHours(3), Emissions = 400 });
            stats = run_avg_emissions_query(time.AddDays(2));
            Assert.IsNotNull(stats);
            Assert.AreEqual(250, stats!.AverageEmissionsPerUser);
            Assert.AreEqual(1000, stats.SumAddedEmissions);
            Assert.AreEqual(1, stats.NumActiveUsers);

            Assert.IsNull(run_avg_emissions_query(time.Date), "entries should be out of datetime limit");
        }

        [Test]
        public void EmissionsTwoUsers() {
            var time = new DateTime(2022, 2, 21, 10, 0, 0, DateTimeKind.Utc);
            entries_.Add(new CarbonEntry() { UserId = "1", EmittedTimestamp = time, Emissions = 100 });
            var stats = run_avg_emissions_query(time.AddDays(2));
            Assert.IsNotNull(stats);
            Assert.AreEqual(100, stats!.AverageEmissionsPerUser);
            Assert.AreEqual(100, stats.SumAddedEmissions);
            Assert.AreEqual(1, stats.NumActiveUsers);

            entries_.Add(new CarbonEntry() { UserId = "2", EmittedTimestamp = time.AddHours(1), Emissions = 200 });
            stats = run_avg_emissions_query(time.AddDays(2));
            Assert.IsNotNull(stats);
            Assert.AreEqual(150, stats!.AverageEmissionsPerUser);
            Assert.AreEqual(300, stats.SumAddedEmissions);
            Assert.AreEqual(2, stats.NumActiveUsers);

            entries_.Add(new CarbonEntry() { UserId = "1", EmittedTimestamp = time.AddHours(2), Emissions = 300 });
            stats = run_avg_emissions_query(time.AddDays(2));
            Assert.IsNotNull(stats);
            Assert.AreEqual(200, stats!.AverageEmissionsPerUser);
            Assert.AreEqual(600, stats.SumAddedEmissions);
            Assert.AreEqual(2, stats.NumActiveUsers);

            entries_.Add(new CarbonEntry() { UserId = "2", EmittedTimestamp = time.AddHours(3), Emissions = 400 });
            stats = run_avg_emissions_query(time.AddDays(2));
            Assert.IsNotNull(stats);
            Assert.AreEqual(250, stats!.AverageEmissionsPerUser);
            Assert.AreEqual(1000, stats.SumAddedEmissions);
            Assert.AreEqual(2, stats.NumActiveUsers);

            Assert.IsNull(run_avg_emissions_query(time.Date), "entries should be out of datetime limit");
        }

        [Test]
        public void AddedEntriesEmpty() {
            var stats = run_added_entries_query(new DateTime(2022, 2, 2, 2, 2, 2));
            Assert.AreEqual(0, stats.NumLastWeek);
            Assert.AreEqual(0, stats.NumPrecedingWeek);
        }

        [Test]
        public void AddedEntriesSingleWeek() {
            var time = new DateTime(2022, 2, 21, 10, 0, 0, DateTimeKind.Utc);
            for (int i = 0; i < 7; ++i) {
                entries_.Add(new CarbonEntry() { CreationTimestamp = time.AddDays(i) });
                var stats = run_added_entries_query(time.AddDays(7));
                Assert.AreEqual(i + 1, stats.NumLastWeek);
                Assert.AreEqual(0, stats.NumPrecedingWeek, "for i = " + i);
            }
        }

        [Test]
        public void AddedEntriesTwoWeekSpead() {
            var time = new DateTime(2022, 2, 21, 10, 0, 0, DateTimeKind.Utc);
            for (int i = 0; i < 14; ++i) {
                entries_.Add(new CarbonEntry() { CreationTimestamp = time.AddDays(i) });
            }
            var stats = run_added_entries_query(time.AddDays(14));
            Assert.AreEqual(7, stats.NumLastWeek);
            Assert.AreEqual(7, stats.NumPrecedingWeek);
        }

        List<CarbonEntry> entries_ = new();
    }
}