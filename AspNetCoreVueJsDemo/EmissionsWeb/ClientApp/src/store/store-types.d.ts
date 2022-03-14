import { AdminReport } from "../protos/reports";

export interface CarbonEntriesState {
    CurrentEntries: CarbonEntry[];
    EmittedSince: string | null;
    EmittedUntil: string | null;
    Errors: Record<string, string[]>;
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
    AdminReportModule: AdminReport;
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

export interface UserSummary_EmissionsExceededItem {
    Day: Date;
    Emissions: number;
}
export interface UserSummary_ExpensesExceededItem {
    Year: number;
    Month: number;
    Expenses: number;
}
