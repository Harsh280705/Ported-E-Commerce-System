<script setup lang="ts">
import { useUiStore } from '@/stores/ui'

const ui = useUiStore()
</script>

<template>
  <div class="toast-container position-fixed bottom-0 end-0 p-3" aria-live="polite" data-testid="toasts">
    <div
      v-for="t in ui.toasts"
      :key="t.id"
      class="toast show"
      role="alert"
      :class="{
        'text-bg-success': t.type === 'success',
        'text-bg-danger': t.type === 'danger',
        'text-bg-info': t.type === 'info',
        'text-bg-warning': t.type === 'warning'
      }"
    >
      <div class="d-flex">
        <div class="toast-body">{{ t.message }}</div>
        <button type="button" class="btn-close btn-close-white me-2 m-auto" :aria-label="`Dismiss ${t.message}`" @click="ui.dismiss(t.id)"></button>
      </div>
    </div>
  </div>
</template>
