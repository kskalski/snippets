import { createWebHistory, createRouter, RouteRecordRaw } from "vue-router";
import CarbonEntries from "../components/CarbonEntries.vue";
import EditCarbonEntry from "../components/EditCarbonEntry.vue";
import AdminReport from "../components/AdminReport.vue";
import UserSummary from "../components/UserSummary.vue";

const routes: RouteRecordRaw[] = [
    {
        path: "/",
        name: "Home",
        component: CarbonEntries,
    },
    {
        path: "/carbon_entries",
        name: "CarbonEntries",
        component: CarbonEntries,
    },
    {
        path: "/carbon_entry/:entryId",
        name: "EditCarbonEntry",
        component: EditCarbonEntry,
        props: route => ({ entryId: parseInt(route.params.entryId as string) })
    },
    {
        path: "/new_carbon_entry",
        name: "NewCarbonEntry",
        component: EditCarbonEntry
    },
    {
        path: "/admin_report",
        name: "AdminReport",
        component: AdminReport,
    },
    {
        path: "/user_summary",
        name: "UserSummary",
        component: UserSummary,
    },
];

const router = createRouter({
    history: createWebHistory(),
    routes,
});

export default router;