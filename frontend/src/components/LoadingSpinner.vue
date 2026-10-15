<script setup lang="ts">
withDefaults(defineProps<{ label?: string }>(), { label: 'Loading…' })
</script>

<template>
  <div class="d-flex align-items-center gap-2" role="status" aria-live="polite" data-testid="loading-spinner">
    <div class="spinner-border spinner-border-sm" aria-hidden="true"></div>
    <span>{{ label }}</span>
  </div>
</template>
