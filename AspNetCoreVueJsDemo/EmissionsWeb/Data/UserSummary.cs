using System;
using System.Collections.Generic;

namespace Emissions.Data {
    public class UserSummary {

        public class EmissionsExceededItem {
            public DateTime Day { get; set; }
            public double Emissions { get; set; }
        }
        public class ExpensesExceededItem {
            public int Year { get; set; }
            public int Month { get; set; }
            public decimal Expenses { get; set; }
        }

        public double UserDailyEmissionsLimit { get; set; }
        public decimal UserMonthlyExpensesLimit { get; set; }

        public IEnumerable<EmissionsExceededItem> Emissions { get; set; }
        public IEnumerable<ExpensesExceededItem> Expenses { get; set; }
    }
}
