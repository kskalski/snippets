<template>
    <AppPage title="Admin report">
        <div class="row g-4 mb-4">
            <div class="col-6 col-lg-3">
                <div class="app-card app-card-stat shadow-sm h-100">
                    <div class="app-card-body p-3 p-lg-4">
                        <h4 class="stats-type mb-1">Added last week</h4>
                        <div class="stats-figure">{{addedLastWeek}}</div>
                        <div class="stats-meta" :class="addedEntriesClass">
                            <b-icon-arrow-up v-if="addedEntriesClass == 'text-success'"></b-icon-arrow-up>
                            <b-icon-arrow-down v-if="addedEntriesClass == 'text-warning'"></b-icon-arrow-down>
                            {{addedEntriesDiffPercent}}%
                        </div>
                    </div>
                </div>
            </div>

            <div class="col-6 col-lg-3">
                <div class="app-card app-card-stat shadow-sm h-100">
                    <div class="app-card-body p-3 p-lg-4">
                        <h4 class="stats-type mb-1">Added preceding week</h4>
                        <div class="stats-figure">{{addedPrevWeek}}</div>
                    </div>
                </div>
            </div>
            <div class="col-6 col-lg-3">
                <div class="app-card app-card-stat shadow-sm h-100">
                    <div class="app-card-body p-3 p-lg-4">
                        <h4 class="stats-type mb-1">Average emissions per user</h4>
                        <div class="stats-figure">{{averageEmissionsPerUser}}</div>
                    </div>
                </div>
            </div>
        </div>
    </AppPage>
</template>

<script lang="ts">
    import { Action, Getter, State } from 's-vuex-class';
    import { Vue, Options } from 'vue-decorator';
    import * as bicon from 'bootstrap-icons-vue';
    import { AdminReportStore } from '../store/modules/AdminReport';
    import { AdminReportState, RootState } from '../store/store-types';
    import AppCard from './Blocks/AppCard.vue';
    import AppPage from './Blocks/AppPage.vue';

    @Options({
        components: {
            AppCard,
            AppPage,
            BIconArrowDown: bicon.BIconArrowDown,
            BIconArrowUp: bicon.BIconArrowUp,
        }
    })
    export default class AdminReport extends Vue {
        @State((state: RootState) => state.AdminReport)
        report: AdminReportState;

        @Getter(AdminReportStore.MODULE + AdminReportStore.GET_AVERAGE_CALORIES_PER_USER)
        avgEmissions: number;

        @Action(AdminReportStore.MODULE + AdminReportStore.DO_FETCH_REPORT)
        doRefreshReport: () => Promise<void>;

        get addedEntriesClass() {
            if (this.report.AddedEntries.NumLastWeek > this.report.AddedEntries.NumPrecedingWeek)
                return 'text-success';
            else if (this.report.AddedEntries.NumLastWeek < this.report.AddedEntries.NumPrecedingWeek)
                return 'text-warning';
            return 'text-info';
        }

        get addedEntriesDiffPercent() {
            if (this.report.AddedEntries.NumPrecedingWeek)
                return (100 * this.report.AddedEntries.NumLastWeek / this.report.AddedEntries.NumPrecedingWeek).toPrecision(4);
            return 100;
        }

        get addedLastWeek() {
            return this.report.AddedEntries.NumLastWeek.toLocaleString();
        }
        get addedPrevWeek() {
            return this.report.AddedEntries.NumPrecedingWeek.toLocaleString();
        }
        get averageEmissionsPerUser() {
            return this.avgEmissions.toFixed(2).toLocaleString();
        }

        created() {
            this.doRefreshReport();
        }
    }
</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->
<style scoped>

</style>
