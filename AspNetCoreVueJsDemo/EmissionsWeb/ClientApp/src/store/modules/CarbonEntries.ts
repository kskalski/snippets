import * as vuex from 'vuex';
import { CarbonEntriesState, RootState } from '@/store/store-types';
import { AccountsStore } from './Accounts';
import { DatesUtil } from '../DatesUtil';
import { GrpcWebFetchTransport } from '@protobuf-ts/grpcweb-transport';
import * as carbon_pb from '../../protos/carbon';
import * as services_pb from '../../protos/services.client';
import { Timestamp } from '../../protos/google/protobuf/timestamp';

function client(rootGetters: any) {
    const meta = rootGetters[AccountsStore.MODULE + AccountsStore.GET_REQUEST_HEADERS].headers;
    const transport = new GrpcWebFetchTransport({
        baseUrl: window.location.origin, meta
    });
    return new services_pb.CarbonEntriesClient(transport);
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
    [CarbonEntriesStore.UPDATE_ENTRIES](state: CarbonEntriesState, payload: carbon_pb.GetCarbonEntriesResponse) {
        state.CurrentEntries = payload.entries;
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
    async [CarbonEntriesStore.DO_FETCH_ENTRIES]({ commit, rootGetters, state }) {
        const filter_params = { offset: 0 } as carbon_pb.GetCarbonEntriesRequest;
        if (state.EmittedSince)
            filter_params.emittedSince = Timestamp.fromDate(DatesUtil.ParseDayStringAsFullDate(state.EmittedSince));

        if (state.EmittedUntil) {
            // API upper time limit is exclusive, so extend the value by one day
            filter_params.emittedUntil = Timestamp.fromDate(DatesUtil.AddDays(DatesUtil.ParseDayStringAsFullDate(state.EmittedUntil), 1));
        }

        const call = await client(rootGetters).getEntries(filter_params);
        commit(CarbonEntriesStore.UPDATE_ENTRIES, call.response);
    },
    async [CarbonEntriesStore.DO_GET_ENTRY]({ rootGetters }, id: number) {
        const call = await client(rootGetters).getEntry(carbon_pb.CarbonEntry.create({ id: id }));
        return call.response;
    },
    async [CarbonEntriesStore.DO_DELETE_ENTRY]({ commit, rootGetters }, id: number) {
        try {
            const call = await client(rootGetters).deleteEntry(carbon_pb.CarbonEntry.create({ id: id }));
        } catch (e: any) {
            commit(CarbonEntriesStore.UPDATE_ERROR, e.response.data ?? { error: e.response.statusText });
            return false;
        }
        return true;
    },
    async [CarbonEntriesStore.DO_SAVE_ENTRY]({ commit, rootGetters }, entry: carbon_pb.CarbonEntry) {
        try {
            if (entry.id > 0)
                await client(rootGetters).updateEntry(entry);
            else
                await client(rootGetters).addEntry(entry);
        } catch (e: any) {
            commit(CarbonEntriesStore.UPDATE_ERROR, e.response?.data ?? { error: e });
            return false;
        }
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