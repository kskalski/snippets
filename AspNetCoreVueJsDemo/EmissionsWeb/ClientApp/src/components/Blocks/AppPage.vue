<template>
    <div class="app-content pt-3 p-md-3 p-lg-4">
        <div class="container-xl">
            <slot name="header">
                <h1 class="app-page-title">{{title}}</h1>
            </slot>
            <slot>
            </slot>
        </div>
    </div>
</template>

<script lang="ts">
import { Vue, Options, Prop } from 'vue-decorator';

@Options({})
export default class AppPage extends Vue {
   @Prop({ default: '' }) title: string;
}
</script>