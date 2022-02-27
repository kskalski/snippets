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

@Options({
    components: {
        HeaderBar,
        NavMenu
  }
})
export default class App extends Vue {
    @Getter(AccountsStore.MODULE + AccountsStore.GET_IS_ADMIN)
    isAdmin: boolean;

    @Action(UserSummaryStore.MODULE + UserSummaryStore.DO_FETCH_SUMMARY)
    doRefreshUserSummary: () => Promise<void>;

    created() {
        !this.isAdmin && this.doRefreshUserSummary();
    }
}

</script>

<style>

</style>
