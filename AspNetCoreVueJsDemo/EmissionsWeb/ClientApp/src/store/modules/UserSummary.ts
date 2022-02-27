import axios from 'axios';
import * as vuex from 'vuex';
import { UserSummaryState, RootState } from '@/store/store-types';
import { AccountsStore } from './Accounts';
import { DatesUtil } from '../DatesUtil';

const USER_SUMMARY_API_ENDPOINT = '/api/UserSummary';

function client(rootGetters: any) {
    return axios.create(rootGetters[AccountsStore.MODULE + AccountsStore.GET_REQUEST_HEADERS]);
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
    Emissions: [],
    Expenses: [],
    UserDailyEmissionsLimit: 0,
    UserMonthlyExpensesLimit: 0,
    DismissEmissionsWarningUpTo: { Day: new Date(0, 0, 0), Emissions: 0 },
    DismissExpensesWarningUpTo: { Year: 0, Month: 0, Expenses: 0 }
}

const mutations: vuex.MutationTree<UserSummaryState> = {
    [UserSummaryStore.UPDATE_SUMMARY](state, payload) {
        DatesUtil.TransformStringsAsDates(payload);
        Object.assign(state, payload);
    },
    [UserSummaryStore.UPDATE_DISMISS_POINTS](state) {
        if (state.Emissions.length)
            state.DismissEmissionsWarningUpTo = { ...state.Emissions[0] };
        if (state.Expenses.length)
            state.DismissExpensesWarningUpTo = { ...state.Expenses[0] };
    }
}

const getters: vuex.GetterTree<UserSummaryState, RootState> = {
    [UserSummaryStore.GET_SHOW_CALORIES_WARNING](state) {
        const dismiss_time = state.DismissEmissionsWarningUpTo.Day.getTime();
        return state.Emissions.some(e => e.Day.getTime() > dismiss_time ||
            e.Day.getTime() == dismiss_time && e.Emissions > state.DismissEmissionsWarningUpTo.Emissions);
    },
    [UserSummaryStore.GET_SHOW_EXPENSES_WARNING](state) {
        const dismiss_time = state.DismissExpensesWarningUpTo.Year * 100 + state.DismissExpensesWarningUpTo.Month;
        return state.Expenses.some(e => {
            const time = e.Year * 100 + e.Month;
            return time > dismiss_time || time == dismiss_time && e.Expenses > state.DismissExpensesWarningUpTo.Expenses
        });
    },
}

const actions: vuex.ActionTree<UserSummaryState, RootState> = {
    async [UserSummaryStore.DO_FETCH_SUMMARY]({ commit, rootGetters }) {
        const response = await client(rootGetters).get(USER_SUMMARY_API_ENDPOINT, {
            params: { until: DatesUtil.DateStringWithOffset(new Date()) }
        });
        if (response.status == 200)
            commit(UserSummaryStore.UPDATE_SUMMARY, response.data);
    },
}

export const UserSummary: vuex.Module<UserSummaryState, RootState> = {
    namespaced: true,
    state,
    getters,
    mutations,
    actions
}