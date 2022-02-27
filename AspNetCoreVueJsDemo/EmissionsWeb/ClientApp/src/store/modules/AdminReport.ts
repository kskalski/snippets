import axios from 'axios';
import * as vuex from 'vuex';
import { AdminReportState, RootState } from '@/store/store-types';
import { AccountsStore } from './Accounts';
import { DatesUtil } from '../DatesUtil';

const ADMIN_REPORT_API_ENDPOINT = '/api/AdminReport';
function client(rootGetters: any) {
    return axios.create(rootGetters[AccountsStore.MODULE + AccountsStore.GET_REQUEST_HEADERS]);
}

export enum AdminReportStore {
    MODULE = "AdminReport/",

    // mutations
    UPDATE_REPORT = "UPDATE_REPORT",

    GET_AVERAGE_CALORIES_PER_USER = "GET_AVERAGE_CALORIES_PER_USER",

    // actions
    DO_FETCH_REPORT = "DO_FETCH_REPORT",
}

const state: AdminReportState = {
    AddedEntries: { NumLastWeek: 0, NumPrecedingWeek: 0, PerDayCounts: [] },
    UsersEmissions: { NumActiveUsers: 0, SumAddedEmissions: 0, AverageEmissionsPerUser: 0 }
}

const mutations: vuex.MutationTree<AdminReportState> = {
    [AdminReportStore.UPDATE_REPORT](state, payload: AdminReportState) {
        state.AddedEntries = payload.AddedEntries;
        state.UsersEmissions = payload.UsersEmissions
    }
}

const getters: vuex.GetterTree<AdminReportState, RootState> = {
    [AdminReportStore.GET_AVERAGE_CALORIES_PER_USER](state) {
        if (state.UsersEmissions.NumActiveUsers)
            return state.UsersEmissions.AverageEmissionsPerUser;
        return 0;
    }
}

const actions: vuex.ActionTree<AdminReportState, RootState> = {
    async [AdminReportStore.DO_FETCH_REPORT]({ commit, rootGetters }) {
        const response = await client(rootGetters).get(ADMIN_REPORT_API_ENDPOINT, {
            // Generate report using moving time window ending *now*, so that "last X days" is always
            // comparable with "preceding X days". Technically this still includes "current day".
            params: { until: new Date() }
        });
        if (response.status == 200) {
            commit(AdminReportStore.UPDATE_REPORT, response.data);
        }
    },
}

export const AdminReport: vuex.Module<AdminReportState, RootState> = {
    namespaced: true,
    state,
    getters,
    mutations,
    actions
}