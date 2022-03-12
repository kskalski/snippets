<template>
    <header class="app-header fixed-top">
        <HeaderBar></HeaderBar>
        <NavMenu></NavMenu>
     </header>
     <div class="app-wrapper">
          <router-view />
     </div>
</template>

<script lang="ts">
    import { Vue, Options } from 'vue-decorator';
    import NavMenu from './components/NavMenu.vue';
    import HeaderBar from './components/HeaderBar.vue';
    import { Action, Getter } from 's-vuex-class';
    import { UserSummaryStore } from './store/modules/UserSummary';
    import { AccountsStore } from './store/modules/Accounts';

    import { GrpcWebFetchTransport } from "@protobuf-ts/grpcweb-transport";
    import { WebNotifierClient } from "./protos/services.client";
    import { AdminReportStore } from './store/modules/AdminReport';
    import { CarbonEntriesStore } from './store/modules/CarbonEntries';

    @Options({
        components: {
            HeaderBar,
            NavMenu
        }
    })
    export default class App extends Vue {
        @Getter(AccountsStore.MODULE + AccountsStore.GET_IS_ADMIN)
        isAdmin: boolean;
        @Getter(AccountsStore.MODULE + AccountsStore.GET_REQUEST_HEADERS)
        authHeaders: Record<string, Record<string, string>>;

        @Action(UserSummaryStore.MODULE + UserSummaryStore.DO_FETCH_SUMMARY)
        doRefreshUserSummary: () => Promise<void>;
        @Action(AdminReportStore.MODULE + AdminReportStore.DO_FETCH_REPORT)
        doRefreshAdminReport: () => Promise<void>;
        @Action(CarbonEntriesStore.MODULE + CarbonEntriesStore.DO_FETCH_ENTRIES)
        doRefreshEntries: () => Promise<void>;

        refreshReport() {
            if (this.isAdmin)
                return this.doRefreshAdminReport();
            else
                return this.doRefreshUserSummary();
        }

        async runNotificationStream() {
            const transport = new GrpcWebFetchTransport({
                baseUrl: window.location.origin, meta: this.authHeaders.headers
            });
            const client = new WebNotifierClient(transport);
            for await (const r of client.listen({}).responses) {
                if (r.reportsChanged)
                    await this.refreshReport();
                if (r.entriesChanged)
                    await this.doRefreshEntries();
            }
        }

        created() {
            this.runNotificationStream();
        }
    }
</script>

<style>

</style>
