import * as vuex from 'vuex';
import { AccountsState, RootState } from '@/store/store-types';

const HARDCODED_TOKENS = [
    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiJmZDk2NGFjMC01Njc2LTRlNGQtYmIyOS0yZjFhZWY0NjFkN2YiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjEyMy11c2VyLTEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoidXNlcjEiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJVc2VyIiwiZXhwIjoxNjc4NjM4OTI5LCJpc3MiOiJodHRwOi8vbG9jYWxob3N0IiwiYXVkIjoiaHR0cDovL2xvY2FsaG9zdCJ9.FGR_dwmS0pwZJXWo9d2n6f3AO8M-S08MXsHcB7xyJUo",
    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiI3MmU1MDM4Mi1kZDkzLTRjMzYtYmJhYi05OWRlNGZjNjE5OWQiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjEyMy1hZG1pbi0xIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSI6ImFkbWluIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiQWRtaW4iLCJleHAiOjE2Nzg2Mzg0MjksImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3QiLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0In0.Li767jtRh9688Y9E0OLLSGsRShOiuoMdXM4U9y4Odzk"
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