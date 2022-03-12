<template>
        <div id="app-sidepanel" class="app-sidepanel">
            <div id="sidepanel-drop" class="sidepanel-drop"></div>
            <div class="sidepanel-inner d-flex flex-column">
                <a href="#" id="sidepanel-close" class="sidepanel-close d-xl-none">&times;</a>
                <div class="app-branding">
                    <router-link class="app-logo" :to="{ path: '/' }">
                        <img class="logo-icon me-2" src="/favicon.png" alt="logo">
                        <span class="logo-text">EMISSIONS</span>
                    </router-link>
                </div>

                <nav id="app-nav-main" class="app-nav app-nav-main flex-grow-1">
                    <ul class="app-menu list-unstyled accordion" id="menu-accordion">
                        <li class="nav-item">
                            <router-link class="nav-link" :to="{ path: '/carbon_entries' }">
                                <span class="nav-icon">
                                    <b-icon-cup-straw class="bi" />
                                </span>
                                <span class="nav-link-text">Carbon entries</span>
                            </router-link>
                        </li>
                        <li class="nav-item" v-if="!isAdmin">
                            <router-link :to="{ path: '/user_summary' }" class="nav-link text-dark">
                                <span class="nav-icon">
                                    <b-icon-bar-chart-line class="bi" />
                                </span>
                                <span class="nav-link-text">Summary of warnings</span>
                            </router-link>
                        </li>
                        <li class="nav-item" v-if="isAdmin">
                            <router-link :to="{ path: '/admin_report' }" class="nav-link text-dark">
                                <span class="nav-icon">
                                    <b-icon-graph-up class="bi" />
                                </span>
                                <span class="nav-link-text">Overview</span>
                            </router-link>
                        </li>
                    </ul>
                </nav>
            </div>
        </div>
</template>

<script lang="ts">
import { Vue, Options } from 'vue-decorator';
import * as bicon from 'bootstrap-icons-vue';
import { AccountsStore } from '../store/modules/Accounts';
import { Getter } from 's-vuex-class';

@Options({
    components: {
        BIconCaretRightSquare: bicon.BIconCaretRightSquare,
        BIconCupStraw: bicon.BIconCupStraw,
        BIconBarChartLine: bicon.BIconBarChartLine,
        BIconGraphUp: bicon.BIconGraphUp,
    }
})
export default class NavMenu extends Vue {
    @Getter(AccountsStore.MODULE + AccountsStore.GET_IS_ADMIN)
    isAdmin: boolean;
}
</script>