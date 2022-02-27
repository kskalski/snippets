<template>
    <AppPage title="Summary">
        <div class="row g-4 mb-4">
            <div class="col-6 col-lg-3">
                <div class="app-card app-card-stat shadow-sm h-100">
                    <div class="app-card-body p-3 p-lg-4">
                        <h4 class="stats-type mb-1">Daily emissions limit</h4>
                        <div class="stats-figure">{{ emissionsLimit }}</div>
                        <div class="stats-meta">
                        </div>
                    </div>
                </div>
            </div>

            <div class="col-6 col-lg-3">
                <div class="app-card app-card-stat shadow-sm h-100">
                    <div class="app-card-body p-3 p-lg-4">
                        <h4 class="stats-type mb-1">Monthly expenses limit</h4>
                        <div class="stats-figure">{{ expensesLimit }}</div>
                    </div>
                </div>
            </div>
        </div>
        <div class="row">
            <div class="app-card shadow-sm mb-5">
                <div class="app-card-title" v-if="emissions.length">
                    Days with exceeded emissions limit
                </div>
                <div class="app-card-body">
                    <div class="table-responsive" v-if="emissions.length">
                        <table class="table app-table-hover mb-0 text-left">
                            <thead>
                                <tr>
                                    <th class="cell">Date</th>
                                    <th class="cell">Emissions</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr v-for="entry of emissions" v-bind:key="entry.Day">
                                    <td class="cell">{{ entry.Day.toLocaleDateString() }}</td>
                                    <td class="cell">{{ entry.Emissions }}</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                    <div v-else>
                        Bravo! You are keeping an eco footprint.
                    </div>
                </div>
            </div>
        </div>
        <div class="row">
            <div class="app-card shadow-sm mb-5">
                <div class="app-card-title" v-if="expenses.length">
                    Months with expenses above limit
                </div>
                <div class="app-card-body">
                    <div class="table-responsive" v-if="expenses.length">
                        <table class="table app-table-hover mb-0 text-left">
                            <thead>
                                <tr>
                                    <th class="cell">Month</th>
                                    <th class="cell">Expenses</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr v-for="(entry, idx) of expenses" v-bind:key="idx">
                                    <td class="cell">{{ expensePeriodName(entry) }}</td>
                                    <td class="cell">{{ entry.Expenses }}</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                    <div v-else>
                        You control your budget well.
                    </div>
                </div>
            </div>
        </div>

    </AppPage>
</template>

<script lang="ts">
    import { Action, Mutation, State } from 's-vuex-class';
    import { Vue, Options } from 'vue-decorator';
import { DatesUtil } from '../store/DatesUtil';
    import { UserSummaryStore } from '../store/modules/UserSummary';
    import { RootState, UserSummary_EmissionsExceededItem, UserSummary_ExpensesExceededItem } from '../store/store-types';
    import AppCard from './Blocks/AppCard.vue';
    import AppPage from './Blocks/AppPage.vue';

    @Options({
        components: {
            AppPage,
            AppCard
        }
    })
    export default class UserSummary extends Vue {
        @State((state: RootState) => state.UserSummary.Expenses)
        expenses: UserSummary_ExpensesExceededItem[];

        @State((state: RootState) => state.UserSummary.Emissions)
        emissions: UserSummary_EmissionsExceededItem[];

        @State((state: RootState) => state.UserSummary.UserDailyEmissionsLimit)
        emissionsLimit: number;
        @State((state: RootState) => state.UserSummary.UserMonthlyExpensesLimit)
        expensesLimit: number;

        @Mutation(UserSummaryStore.MODULE + UserSummaryStore.UPDATE_DISMISS_POINTS)
        updateDismissPoints: () => void;

        @Action(UserSummaryStore.MODULE + UserSummaryStore.DO_FETCH_SUMMARY)
        doRefreshSummary: () => Promise<void>;

        emissionsPeriodName(emissions_item: UserSummary_EmissionsExceededItem) {
            return emissions_item.Day.toLocaleDateString();
        }

        expensePeriodName(expense: UserSummary_ExpensesExceededItem) {
            return DatesUtil.formatMonth(new Date(expense.Year, expense.Month - 1));
        }

        async created() {
            await this.doRefreshSummary();
            this.updateDismissPoints();
        }
    }
</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->
<style scoped>

</style>
