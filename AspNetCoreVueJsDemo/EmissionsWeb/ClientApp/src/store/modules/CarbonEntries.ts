import axios from 'axios';
import * as vuex from 'vuex';
import { CarbonEntriesState, CarbonEntry, RootState } from '@/store/store-types';
import { AccountsStore } from './Accounts';
import { UserSummaryStore } from './UserSummary';
import { DatesUtil } from '../DatesUtil';

const CARBON_ENTRIES_API_ENDPOINT = '/api/CarbonEntries';

const SKIP_CACHE_HEADERS = { 'Cache-Control': 'no-cache', 'Pragma': 'no-cache' };
function client(rootGetters: any, skip_cache = false) {
    const config = rootGetters[AccountsStore.MODULE + AccountsStore.GET_REQUEST_HEADERS];
    if (skip_cache)
        Object.assign(config.headers, SKIP_CACHE_HEADERS);
    return axios.create(config);
}

export enum CarbonEntriesStore {
    MODULE = "CarbonEntries/",

    // mutations
    UPDATE_ERROR = "UPDATE_ERROR",
    UPDATE_ENTRIES = "UPDATE_ENTRIES",
    UPDATE_TAKEN_SINCE = "UPDATE_TAKEN_SINCE",
    UPDATE_TAKEN_UNTIL = "UPDATE_TAKEN_UNTIL",

    // actions
    DO_FETCH_ENTRIES = "DO_FETCH_ENTRIES",
    DO_GET_ENTRY = "DO_GET_ENTRY",
    DO_SAVE_ENTRY = "DO_SAVE_ENTRY",
    DO_DELETE_ENTRY = "DO_DELETE_ENTRY"
}


const state: CarbonEntriesState = {
    CurrentEntries: [],
    EmittedSince: null,
    EmittedUntil: null,
    Errors: {}
}

const mutations: vuex.MutationTree<CarbonEntriesState> = {
    [CarbonEntriesStore.UPDATE_ENTRIES](state: CarbonEntriesState, payload: CarbonEntry[]) {
        DatesUtil.TransformStringsAsDates(payload);
        state.CurrentEntries = payload;
    },
    [CarbonEntriesStore.UPDATE_TAKEN_SINCE](state: CarbonEntriesState, payload: string) {
        state.EmittedSince = payload;
    },
    [CarbonEntriesStore.UPDATE_TAKEN_UNTIL](state: CarbonEntriesState, payload: string) {
        state.EmittedUntil = payload;
    },
    [CarbonEntriesStore.UPDATE_ERROR](state, payload) {
        if (payload === null)
            state.Errors = {};
        else if (payload.status == 400)
            state.Errors = payload.errors;
        else
            state.Errors = { "Error": ["Unknown error: " + payload] };
    }
}

const getters: vuex.GetterTree<CarbonEntriesState, RootState> = {
}

const actions: vuex.ActionTree<CarbonEntriesState, RootState> = {
    async [CarbonEntriesStore.DO_FETCH_ENTRIES]({ commit, rootGetters, state }, skip_browser_cache = false) {
        const filter_params = {} as any;
        if (state.EmittedSince)
            filter_params.emitted_since = DatesUtil.ParseDayStringAsFullDate(state.EmittedSince);

        if (state.EmittedUntil) {
            // API upper time limit is exclusive, so extend the value by one day
            filter_params.emitted_until = DatesUtil.AddDays(DatesUtil.ParseDayStringAsFullDate(state.EmittedUntil), 1);
        }

        const response = await client(rootGetters, skip_browser_cache).get(CARBON_ENTRIES_API_ENDPOINT, { params: filter_params });
        if (response.status == 200)
            commit(CarbonEntriesStore.UPDATE_ENTRIES, response.data);
    },
    async [CarbonEntriesStore.DO_GET_ENTRY]({ rootGetters }, id: number) {
        const response = await client(rootGetters).get<CarbonEntry>(`${CARBON_ENTRIES_API_ENDPOINT}/${id}`);
        if (response.status == 200) {
            DatesUtil.TransformStringsAsDates(response.data);
            return response.data;
        }
        return null;
    },
    async [CarbonEntriesStore.DO_DELETE_ENTRY]({ dispatch, rootGetters }, id: number) {
        const response = await client(rootGetters).delete(`${CARBON_ENTRIES_API_ENDPOINT}/${id}`);
        if (response.status == 204)
            await Promise.all([
                dispatch(CarbonEntriesStore.DO_FETCH_ENTRIES, true),
                dispatch(UserSummaryStore.MODULE + UserSummaryStore.DO_FETCH_SUMMARY, null, { root: true })
            ]);
    },
    async [CarbonEntriesStore.DO_SAVE_ENTRY]({ commit, dispatch, rootGetters }, entry: CarbonEntry) {
        try {
            if (entry.Id > 0)
                await client(rootGetters).put(`${CARBON_ENTRIES_API_ENDPOINT}/${entry.Id}`, entry);
            else
                await client(rootGetters).post(CARBON_ENTRIES_API_ENDPOINT, entry);
        } catch (e) {
            commit(CarbonEntriesStore.UPDATE_ERROR, e.response?.data ?? { error: e });
            return false;
        }
        await Promise.all([
            dispatch(CarbonEntriesStore.DO_FETCH_ENTRIES, true),
            dispatch(UserSummaryStore.MODULE + UserSummaryStore.DO_FETCH_SUMMARY, null, { root: true })
        ]);
        return true;
    },
}

export const CarbonEntries: vuex.Module<CarbonEntriesState, RootState> = {
    namespaced: true,
    state,
    getters,
    mutations,
    actions
}