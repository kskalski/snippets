using System;
using System.Collections.Generic;

namespace Emissions.Data {
    // Intermediary data for calculating weekly stats
    public struct DayCount {
        public TimeSpan Day;
        public int NumEntries;
    }


    public class AdminReport {
        public class AddedEntriesCounts {
            public IList<int> PerDayCounts { get; set; }
            public int NumLastWeek { get; set; }
            public int NumPrecedingWeek { get; set; }
        }

        public class EmissionsByUsers {
            public int NumActiveUsers { get; set; }
            public double SumAddedEmissions { get; set; }
            public double AverageEmissionsPerUser { get; set; }
        }

        public AddedEntriesCounts AddedEntries { get; set; }
        public EmissionsByUsers UsersEmissions { get; set; }
    }
}
