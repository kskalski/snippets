<template>
    <div class="item border-bottom py-3">
        <div class="row justify-content-between align-items-center">
            <slot></slot>
        </div>
    </div>
</template>

<script lang="ts">
import { Vue, Options } from 'vue-decorator';

@Options({})
export default class AppCardItem extends Vue {
}
</script>