<template>
    <div class="app-header-inner">
        <div class="container-fluid py-2">
            <div class="app-header-content">
                <div class="row justify-content-between align-items-center">
                    <div class="col-auto"></div>
                    <div class="col-auto notification-bar" v-if="showEmissionsWarning">
                        <span class="text-warning">Daily emissions limit exceeded</span>
                        <router-link :to="{ path: '/user_summary' }" class="btn-sm app-btn-secondary">
                            View
                        </router-link>
                    </div>
                    <div class="col-auto notification-bar" v-if="showExpensesWarning">
                        <span class="text-warning">Monthly expenses limit exceeded</span>
                        <router-link :to="{ path: '/user_summary' }" class="btn-sm app-btn-secondary">
                            View
                        </router-link>
                    </div>
                    <div class="app-utilities col-auto">
                        <div class="app-utility-item">
                            {{userName}}
                            <b-icon-person class="icon" @click="updateSelectedUserToken" />
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import { Vue, Options } from 'vue-decorator';
    import * as bicon from 'bootstrap-icons-vue';
    import { Getter, Mutation } from 's-vuex-class';
    import { AccountsStore } from '../store/modules/Accounts';
    import { UserSummaryStore } from '../store/modules/UserSummary';

    @Options({
        components: {
            BIconPerson: bicon.BIconPerson,
        }
    })
    export default class HeaderBar extends Vue {
        @Getter(AccountsStore.MODULE + AccountsStore.GET_USER_NAME)
        userName: string;

        @Getter(UserSummaryStore.MODULE + UserSummaryStore.GET_SHOW_CALORIES_WARNING)
        showEmissionsWarning: boolean;
        @Getter(UserSummaryStore.MODULE + UserSummaryStore.GET_SHOW_EXPENSES_WARNING)
        showExpensesWarning: boolean;

        @Mutation(AccountsStore.MODULE + AccountsStore.UPDATE_SELECTED_USER_TOKEN)
        updateSelectedUserToken: (o: null) => void;
    }
</script>

<style scoped lang="scss">
    .notification-bar {
        span {
            background-color: black;
            padding: 3px 5px;
        }
        a {
            margin-left: 5px;
        }
    }
</style>