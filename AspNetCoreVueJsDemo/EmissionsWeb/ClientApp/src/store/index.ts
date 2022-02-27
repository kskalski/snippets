﻿import { createStore } from 'vuex';

import { RootState } from './store-types';
import { CarbonEntries } from './modules/CarbonEntries';
import { AdminReport } from './modules/AdminReport';
import { UserSummary } from './modules/UserSummary';
import { Accounts } from './modules/Accounts';

export const store = createStore<RootState>({
    strict: false,
    modules: {
        Accounts,
        AdminReport,
        CarbonEntries,
        UserSummary
    }
})
