<template>
    <div class="app-content pt-3 p-md-3 p-lg-4">
        <div class="container-xl" v-if="carbonEntry">
            <h1 class="app-page-title" v-if="entryId <= 0">Adding new carbon emission</h1>
            <h1 class="app-page-title" v-else>Editing carbon emission</h1>
            <hr class="mb-4">
            <div class="row g-4 settings-section">
                <div class="col-12 col-md-4">
                    <h3 class="section-title">Carbon emission data</h3>
                    <div class="section-intro">Specify the type of carbon emitting activity, when did you emit it and the estimated amount of CO2 equivalent</div>
                    <div class="app-error" v-if="errors">
                        <div v-for="(field_errors, name) in errors" class="row" v-bind:key="name">
                            {{name}}: <span v-for="(msg, idx) in field_errors">{{msg}}</span>
                        </div>
                    </div>
                </div>
                <div class="col-12 col-md-8">
                    <div class="app-card app-card-settings shadow-sm p-4">

                        <div class="app-card-body">
                            <form class="settings-form">
                                <div class="mb-3" v-if="isAdmin">
                                    <label for="carbon-entry-input-1" class="form-label">User</label>
                                    <select class="form-control" id="carbon-entry-user" v-model="carbonEntry.userId">
                                         <option>- select -</option>
                                         <option v-for="(user_name, user_id) in userNameById" :value="user_id">
                                            {{user_name}}
                                         </option>
                                    </select>
                                </div>
                                <div class="mb-3">
                                    <label for="carbon-entry-input-1" class="form-label">Activity/product name</label>
                                    <input type="text" class="form-control" id="carbon-entry-input-1" v-model.trim='carbonEntry.name' required>
                                </div>
                                <div class="mb-3">
                                    <label for="carbon-entry-input-2" class="form-label">Emission time</label>
                                    <input type="datetime-local" class="form-control" id="carbon-entry-input-2" v-model='emittedTimestamp' required>
                                </div>
                                <div class="mb-3">
                                    <label for="carbon-entry-input-3" class="form-label">Emissions</label>
                                    <input type="number" class="form-control" id="carbon-entry-input-3" v-model='carbonEntry.emissions' required>
                                </div>
                                <div class="mb-3">
                                    <label for="carbon-entry-input-4" class="form-label">Price</label>
                                    <input type="number" class="form-control" id="carbon-entry-input-4" v-model='carbonEntry.price'>
                                </div>
                            </form>
                        </div>

                    </div>
                </div>
            </div>
            <hr class="my-4">
            <div class="row justify-content-left">
                <div class="col-1"><button class="btn app-btn-primary" @click="handleSave">Save</button></div>
                <div class="col-1"><button class="btn app-btn-danger" @click="handleDelete">Delete</button></div>
                <div class="col-1"><button class="btn app-btn-secondary" @click="handleBack">Back</button></div>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import { Vue, Options, Prop } from 'vue-decorator';
    import * as bicon from 'bootstrap-icons-vue';
    import { RootState } from '../store/store-types';
    import { Action, Getter, Mutation, State } from 's-vuex-class';
    import { CarbonEntriesStore } from '../store/modules/CarbonEntries';
    import { DatesUtil } from '../store/DatesUtil';
    import { AccountsStore } from '../store/modules/Accounts';
    import { CarbonEntry } from '../protos/carbon';
    import { Timestamp } from '../protos/google/protobuf/timestamp';

    @Options({
        components: {
            BIconInfoCircle: bicon.BIconInfoCircle
        }
    })
    export default class EditCarbonEntry extends Vue {
        @Prop(Number)
        entryId: number = 0;

        @State((state: RootState) => state.Accounts.UserIdToName)
        userNameById: Record<string, string>;

        @State((state: RootState) => state.CarbonEntries.Errors)
        errors: Record<string, string[]>;

        @Getter(AccountsStore.MODULE + AccountsStore.GET_IS_ADMIN)
        isAdmin: boolean;

        @Mutation(CarbonEntriesStore.MODULE + CarbonEntriesStore.UPDATE_ERROR)
        updateError: (e: any) => void;

        @Action(CarbonEntriesStore.MODULE + CarbonEntriesStore.DO_GET_ENTRY)
        doGetEntry: (id: number) => Promise<CarbonEntry>;

        @Action(CarbonEntriesStore.MODULE + CarbonEntriesStore.DO_SAVE_ENTRY)
        doSaveEntry: (e: CarbonEntry) => Promise<boolean>;

        @Action(CarbonEntriesStore.MODULE + CarbonEntriesStore.DO_DELETE_ENTRY)
        doDeleteEntry: (e: number) => Promise<boolean>;

        carbonEntry: CarbonEntry | null = null;

        get emittedTimestamp() {
            return DatesUtil.DateToDatetimeInputString(Timestamp.toDate(this.carbonEntry!.emittedTimestamp!));
        }
        set emittedTimestamp(val: string) {
            this.carbonEntry!.emittedTimestamp = Timestamp.fromDate(new Date(val));
        }

        async handleSave() {
            if (!this.carbonEntry!.price)
                this.carbonEntry!.price = undefined;
            if (await this.doSaveEntry(this.carbonEntry!))
                this.handleBack();
        }
        async handleDelete() {
            if (await this.doDeleteEntry(this.entryId))
                this.handleBack();
        }
        handleBack() {
            this.$router.push('/carbon_entries');
        }

        async created() {
            if (this.entryId > 0)
                this.carbonEntry = await this.doGetEntry(this.entryId);
            else
                this.carbonEntry = { id: 0, name: '', emittedTimestamp: Timestamp.now(), emissions: 1, userId: '' };
            if (this.carbonEntry)
                this.updateError(null);
            else
                this.$router.push('/');
        }
    }
</script>

<style scoped>
    .app-error {
        margin-top: 20px;
        background-color: red;
        color: white;
        padding-left: 20px;
    }

</style>