namespace Emissions.Data {
    public class Parameters {
        public const double DEFAULT_DAILY_EMISSIONS_WARNING_THRESHOLD = 2100.0;
        public const decimal DEFAULT_MONTHLY_EXPENSES_WARNING_THRESHOLD = 1000.0m;
        public const int MAX_NUM_FETCHED_CARBON_ENTRIES = 50;
        public const int ADMIN_REPORT_ADDED_ENTRIES_WINDOW_DAYS = 7;
        public const int ADMIN_REPORT_AVG_EMISSIONS_WINDOW_DAYS = 7;
        public const int USER_SUMMARY_DEFAULT_EMISSIONS_ITEMS = 30;
        public const int USER_SUMMARY_DEFAULT_EXPENSE_ITEMS = 12;
        public const string ADMIN_ROLE = "Admin";
        public const string USER_ROLE = "User";
        public static readonly string[] ROLE_NAMES = { ADMIN_ROLE, USER_ROLE };
    }
}
