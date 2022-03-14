<template>
    <AppPage>
        <template #header>
            <div class="row g-3 mb-4 align-items-center justify-content-between">
                <div class="col-auto">
                    <h1 class="app-page-title mb-0">Carbon entries</h1>
                </div>
                <div class="col-auto">
                    <div class="page-utilities">
                        <div class="row g-2 justify-content-start justify-content-md-end align-items-center">
                            <div class="col-auto">
                                <form class="table-search-form row gx-1 align-items-center">
                                    <div class="col-auto">
                                        <label for="emitted-since">Date from</label>
                                    </div>
                                    <div class="col-auto">
                                        <input type="date" id="emitted-since" name="emitted-since" class="form-control search-orders"
                                               :value="emittedSince" @change="datesChanged($event.target, 'since')" >
                                    </div>
                                    <div class="col"></div>
                                    <div class="col-auto">
                                        <label for="emitted-until">Date to</label>
                                    </div>
                                    <div class="col-auto">
                                        <input type="date" id="emitted-until" name="emitted-until" class="form-control search-orders"
                                               :value="emittedUntil" @change="datesChanged($event.target, 'until')" >
                                    </div>
                                </form>

                            </div><!--//col-->
                            <div class="col text-end">
                                <router-link :to="{ path: '/new_carbon_entry' }" class="btn-sm app-btn-primary">
                                    Add new
                                </router-link>
                            </div>
                        </div><!--//row-->
                    </div><!--//table-utilities-->
                </div><!--//col-auto-->
            </div>
        </template>

        <div class="tab-content" id="orders-table-tab-content">
            <div class="tab-pane fade show active" id="orders-all" role="tabpanel" aria-labelledby="orders-all-tab">
                <div class="app-card shadow-sm mb-5">
                    <div class="app-card-body">
                        <div class="table-responsive">
                            <table class="table app-table-hover mb-0 text-left">
                                <thead>
                                    <tr>
                                        <th v-if="isAdmin" class="cell">User</th>
                                        <th class="cell">Name</th>
                                        <th class="cell">Date</th>
                                        <th class="cell">Emissions</th>
                                        <th class="cell">Price</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr v-for="entry of entries" v-bind:key="entry.Id">
                                        <td v-if="isAdmin" class="cell">{{userNameById[entry.UserId]}}</td>
                                        <td class="cell"><router-link :to="{ path: '/carbon_entry/' + entry.Id }">{{ entry.Name }}</router-link></td>
                                        <td class="cell">{{ entry.EmittedTimestamp.toLocaleString() }}</td>
                                        <td class="cell">{{ entry.Emissions }}</td>
                                        <td class="cell">{{ entry.Price }}</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </AppPage>
</template>


<script lang="ts">
import { Vue, Options } from 'vue-decorator';
import { Action, Getter, Mutation, State } from 's-vuex-class';
import { CarbonEntry, RootState } from '../store/store-types';
import AppPage from './Blocks/AppPage.vue';
import { CarbonEntriesStore } from '../store/modules/CarbonEntries';
import { AccountsStore } from '../store/modules/Accounts';

@Options({
    components: {
        AppPage
    }
})
export default class CarbonEntries extends Vue {
    @State((state: RootState) => state.CarbonEntries.CurrentEntries)
    entries: CarbonEntry[];

    @State((state: RootState) => state.CarbonEntries.EmittedSince)
    emittedSince: string;
    @State((state: RootState) => state.CarbonEntries.EmittedUntil)
    emittedUntil: string;

    @State((state: RootState) => state.Accounts.UserIdToName)
    userNameById: Record<string, string>;

    @Getter(AccountsStore.MODULE + AccountsStore.GET_IS_ADMIN)
    isAdmin: boolean;

    @Mutation(CarbonEntriesStore.MODULE + CarbonEntriesStore.UPDATE_TAKEN_SINCE)
    updateEmittedSince: (val: string) => void
    @Mutation(CarbonEntriesStore.MODULE + CarbonEntriesStore.UPDATE_TAKEN_UNTIL)
    updateEmittedUntil: (val: string) => void

    @Action(CarbonEntriesStore.MODULE + CarbonEntriesStore.DO_FETCH_ENTRIES)
    doRefreshEntries: () => Promise<void>;

    datesChanged(target: EventTarget | null, type: "since" | "until") {
        const val = (target as HTMLInputElement).value ?? "";
        if (type == "since")
            this.updateEmittedSince(val);
        else
            this.updateEmittedUntil(val);
        this.doRefreshEntries();
    }

    created() {
        this.doRefreshEntries();
    }
}
</script>