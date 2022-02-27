<template>
    <div class="app-card shadow-sm d-flex flex-column align-items-start">
        <div class="app-card-header p-3 border-bottom-0">
            <slot name="header">
                <div class="row align-items-center gx-3">
                    <div class="col-auto">
                        <div class="app-icon-holder">
                            <slot name="header-icon">
                            </slot>
                        </div>
                    </div>
                    <div class="col-auto">
                        <h4 class="app-card-title">{{ title }}</h4>
                    </div>
                </div>
            </slot>
        </div>
        <div class="app-card-body px-4 w-100">
            <slot>
            </slot>
        </div>
        <div class="app-card-footer p-4 mt-auto">
            <slot name="footer">
            </slot>
        </div>
    </div>
</template>

<script lang="ts">
import { Vue, Options, Prop } from 'vue-decorator';

@Options({})
export default class AppCard extends Vue {
    @Prop({ default: '' }) title: string;
}
</script>