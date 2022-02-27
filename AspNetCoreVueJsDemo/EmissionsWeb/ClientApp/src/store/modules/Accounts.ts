import * as vuex from 'vuex';
import { AccountsState, RootState } from '@/store/store-types';

const HARDCODED_TOKENS = [
    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiIzMzg3YmE2YS05YzdiLTQxOWUtOGQxNS1jMGNhMmJhYjRmNWEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjYxMDUzNjhhLWY1NmQtNDQ5OC1hNTdlLTU0NTExYzcwYWM4NiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJ1c2VyMSIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IlVzZXIiLCJleHAiOjE2Nzc0NTA5NTcsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3QiLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0In0.iJeIUmzzsCDbCg2iVxXMWE1xU0JyMzJhZ8k4pK_0m3c",
    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiJhYjM4NWZhOC1jMDhhLTQ3OWYtOWIxYS1hYjVjMTU0MzIzM2MiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjJjM2YwMTQzLWI4ZDQtNGYyYi05MDBkLTRlNGI3ZWIwNzA1NyIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJhZG1pbiIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IkFkbWluIiwiZXhwIjoxNjc3NDUwOTU3LCJpc3MiOiJodHRwOi8vbG9jYWxob3N0IiwiYXVkIjoiaHR0cDovL2xvY2FsaG9zdCJ9.lF297UjcQ5488QLYUA1IabinHJha7Qj15WX3FahQuS8",
];

function parse_token(token: string): Record<string, string> {
    return JSON.parse(atob(token.split('.')[1]));
}
function find_claim(token_json: Record<string, string>, key_suffix: string) {
    return Object.entries(token_json).find(([k, v]) => k.endsWith(key_suffix))![1];
}

export enum AccountsStore {
    MODULE = "Accounts/",

    // mutations
    UPDATE_SELECTED_USER_TOKEN = "UPDATE_SELECTED_USER_TOKEN",

    GET_REQUEST_HEADERS = "GET_REQUEST_HEADERS",
    GET_USER_TOKEN_JSON = "GET_USER_TOKEN_JSON",
    GET_USER_NAME = "GET_USER_NAME",
    GET_IS_ADMIN = "GET_IS_ADMIN",

    // actions
}

const state: AccountsState = {
    Token: HARDCODED_TOKENS[0],
    UserIdToName: Object.fromEntries(HARDCODED_TOKENS.map(token => {
        const json = parse_token(token);
        return [find_claim(json, '/nameidentifier'), find_claim(json, '/name')];
    }))
}

const mutations: vuex.MutationTree<AccountsState> = {
    [AccountsStore.UPDATE_SELECTED_USER_TOKEN](state, o: null) {
        const current_user_idx = HARDCODED_TOKENS.findIndex(u => u === state.Token);
        state.Token = HARDCODED_TOKENS[(current_user_idx + 1) % HARDCODED_TOKENS.length];
    }
}

const getters: vuex.GetterTree<AccountsState, RootState> = {
    [AccountsStore.GET_REQUEST_HEADERS](state) {
        return { headers: { 'Authorization': `Bearer ${state.Token}` } };
    },
    [AccountsStore.GET_USER_TOKEN_JSON](state) {
        return parse_token(state.Token);
    },
    [AccountsStore.GET_USER_NAME](state, getters) {
        return find_claim(getters[AccountsStore.GET_USER_TOKEN_JSON], '/name');
    },
    [AccountsStore.GET_IS_ADMIN](state, getters) {
        return find_claim(getters[AccountsStore.GET_USER_TOKEN_JSON], '/role') === 'Admin';
    },
}

const actions: vuex.ActionTree<AccountsState, RootState> = {
}

export const Accounts: vuex.Module<AccountsState, RootState> = {
    namespaced: true,
    state,
    getters,
    mutations,
    actions
}