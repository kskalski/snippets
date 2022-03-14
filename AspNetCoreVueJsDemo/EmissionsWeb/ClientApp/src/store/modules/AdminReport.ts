import * as vuex from 'vuex';
import { RootState } from '@/store/store-types';
import { AccountsStore } from './Accounts';
import { AdminReport } from '../../protos/reports';
import { AdminReportsClient } from '../../protos/services.client';
import { GrpcWebFetchTransport } from '@protobuf-ts/grpcweb-transport';
import { Timestamp } from '../../protos/google/protobuf/timestamp';

function client(rootGetters: any) {
    const meta = rootGetters[AccountsStore.MODULE + AccountsStore.GET_REQUEST_HEADERS].headers;
    const transport = new GrpcWebFetchTransport({
        baseUrl: window.location.origin, meta
    });
    return new AdminReportsClient(transport);
}

export enum AdminReportStore {
    MODULE = "AdminReportModule/",

    // mutations
    UPDATE_REPORT = "UPDATE_REPORT",

    GET_AVERAGE_CALORIES_PER_USER = "GET_AVERAGE_CALORIES_PER_USER",

    // actions
    DO_FETCH_REPORT = "DO_FETCH_REPORT",
}

const state: AdminReport = {
    addedEntries: { numLastWeek: 0, numPrecedingWeek: 0, perDayCounts: [] },
    usersEmissions: { numActiveUsers: 0, sumAddedEmissions: 0, averageEmissionsPerUser: 0 }
}

const mutations: vuex.MutationTree<AdminReport> = {
    [AdminReportStore.UPDATE_REPORT](state, payload: AdminReport) {
        state.addedEntries = payload.addedEntries;
        state.usersEmissions = payload.usersEmissions
    }
}

const getters: vuex.GetterTree<AdminReport, RootState> = {
    [AdminReportStore.GET_AVERAGE_CALORIES_PER_USER](state) {
        if (state.usersEmissions?.numActiveUsers)
            return state.usersEmissions.averageEmissionsPerUser;
        return 0;
    }
}

const actions: vuex.ActionTree<AdminReport, RootState> = {
    async [AdminReportStore.DO_FETCH_REPORT]({ commit, rootGetters }) {
        const call = await client(rootGetters).report({
            // Generate report using moving time window ending *now*, so that "last X days" is always
            // comparable with "preceding X days". Technically this still includes "current day".
            until: Timestamp.now()
        });
        commit(AdminReportStore.UPDATE_REPORT, call.response);
    },
}

export const AdminReportModule: vuex.Module<AdminReport, RootState> = {
    namespaced: true,
    state,
    getters,
    mutations,
    actions
}