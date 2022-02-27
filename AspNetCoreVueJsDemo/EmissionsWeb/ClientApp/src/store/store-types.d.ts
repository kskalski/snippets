export interface CarbonEntriesState {
    CurrentEntries: CarbonEntry[];
    EmittedSince: string | null;
    EmittedUntil: string | null;
    Errors: Record<string, string[]>;
}

export interface AdminReportState {
    AddedEntries: AdminReport_AddedEntriesCounts;
    UsersEmissions: AdminReport_EmissionsByUsers;
}

export interface UserSummaryState {
    Emissions: UserSummary_EmissionsExceededItem[];
    Expenses: UserSummary_ExpensesExceededItem[];
    UserDailyEmissionsLimit: number;
    UserMonthlyExpensesLimit: number;
    DismissEmissionsWarningUpTo: UserSummary_EmissionsExceededItem;
    DismissExpensesWarningUpTo: UserSummary_ExpensesExceededItem;
}

export interface AccountsState {
    Token: string;
    UserIdToName: Record<string, string>;
}

export interface RootState {
    Accounts: AccountsState;
    CarbonEntries: CarbonEntriesState;
    AdminReport: AdminReportState;
    UserSummary: UserSummaryState;
}

export interface CarbonEntry {
    Id: number;
    UserId: string;
    Name: string;
    EmittedTimestamp: Date;
    Emissions: number;
    Price?: number | null;
}

export interface AdminReport_AddedEntriesCounts {
    PerDayCounts: number[];
    NumLastWeek: number;
    NumPrecedingWeek: number;
}

export interface AdminReport_EmissionsByUsers {
    NumActiveUsers: number;
    SumAddedEmissions: number;
    AverageEmissionsPerUser: number;
}

export interface UserSummary_EmissionsExceededItem {
    Day: Date;
    Emissions: number;
}
export interface UserSummary_ExpensesExceededItem {
    Year: number;
    Month: number;
    Expenses: number;
}
