import { CarbonEntry } from "../protos/carbon";
import { AdminReport, UserSummary, UserSummary_EmissionsExceededItem, UserSummary_ExpensesExceededItem } from "../protos/reports";

export interface CarbonEntriesState {
    CurrentEntries: CarbonEntry[];
    EmittedSince: string | null;
    EmittedUntil: string | null;
    Errors: Record<string, string[]>;
}

export interface UserSummaryState extends UserSummary {
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
