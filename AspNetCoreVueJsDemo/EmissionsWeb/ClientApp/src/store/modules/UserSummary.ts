import * as vuex from 'vuex';
import { UserSummaryState, RootState } from '@/store/store-types';
import { AccountsStore } from './Accounts';
import { DatesUtil } from '../DatesUtil';
import * as reports_pb from '../../protos/reports';
import { GrpcWebFetchTransport } from '@protobuf-ts/grpcweb-transport';
import { UserSummariesClient } from '../../protos/services.client';
import { Timestamp } from '../../protos/google/protobuf/timestamp';

function client(rootGetters: any) {
    const meta = rootGetters[AccountsStore.MODULE + AccountsStore.GET_REQUEST_HEADERS].headers;
    const transport = new GrpcWebFetchTransport({
        baseUrl: window.location.origin, meta
    });
    return new UserSummariesClient(transport);
}

export enum UserSummaryStore {
    MODULE = "UserSummary/",

    // mutations
    UPDATE_SUMMARY = "UPDATE_SUMMARY",
    UPDATE_DISMISS_POINTS = "UPDATE_DISMISS_POINTS",

    GET_SHOW_CALORIES_WARNING = "GET_SHOW_CALORIES_WARNING",
    GET_SHOW_EXPENSES_WARNING = "GET_SHOW_EXPENSES_WARNING",

    // actions
    DO_FETCH_SUMMARY = "DO_FETCH_SUMMARY",
}

const state: UserSummaryState = {
    emissions: [],
    expenses: [],
    userDailyEmissionsLimit: 0,
    userMonthlyExpensesLimit: 0,
    DismissEmissionsWarningUpTo: { day: { seconds: 0, nanos: 0 }, emissions: 0 },
    DismissExpensesWarningUpTo: { year: 0, month: 0, expenses: 0 }
}

const mutations: vuex.MutationTree<UserSummaryState> = {
    [UserSummaryStore.UPDATE_SUMMARY](state, payload: reports_pb.UserSummary) {
        Object.assign(state, payload);
    },
    [UserSummaryStore.UPDATE_DISMISS_POINTS](state) {
        if (state.emissions.length)
            state.DismissEmissionsWarningUpTo = { ...state.emissions[0] };
        if (state.expenses.length)
            state.DismissExpensesWarningUpTo = { ...state.expenses[0] };
    }
}

const getters: vuex.GetterTree<UserSummaryState, RootState> = {
    [UserSummaryStore.GET_SHOW_CALORIES_WARNING](state) {
        const dismiss_time = state.DismissEmissionsWarningUpTo.day!.seconds!;
        return state.emissions.some(e => e.day!.seconds > dismiss_time ||
            e.day!.seconds == dismiss_time && e.emissions > state.DismissEmissionsWarningUpTo.emissions);
    },
    [UserSummaryStore.GET_SHOW_EXPENSES_WARNING](state) {
        const dismiss_time = state.DismissExpensesWarningUpTo.year * 100 + state.DismissExpensesWarningUpTo.month;
        return state.expenses.some(e => {
            const time = e.year * 100 + e.month;
            return time > dismiss_time || time == dismiss_time && e.expenses > state.DismissExpensesWarningUpTo.expenses
        });
    },
}

const actions: vuex.ActionTree<UserSummaryState, RootState> = {
    async [UserSummaryStore.DO_FETCH_SUMMARY]({ commit, rootGetters }) {
        const now = new Date();
        const call = await client(rootGetters).getUserSummaryOfExceededThresholds({
            until: Timestamp.fromDate(now),
            untilTzOffsetMinutes: now.getTimezoneOffset()
        });
        commit(UserSummaryStore.UPDATE_SUMMARY, call.response);
    },
}

export const UserSummary: vuex.Module<UserSummaryState, RootState> = {
    namespaced: true,
    state,
    getters,
    mutations,
    actions
}