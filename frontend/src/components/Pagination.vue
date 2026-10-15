<script setup lang="ts">
const props = withDefaults(
  defineProps<{ page: number; totalPages: number }>(),
  { page: 1, totalPages: 1 }
)
const emit = defineEmits<{ (e: 'change', page: number): void }>()

function pages(): number[] {
  const total = Math.max(1, props.totalPages)
  const current = Math.min(Math.max(1, props.page), total)
  const start = Math.max(1, current - 2)
  const end = Math.min(total, start + 4)
  const out: number[] = []
  for (let i = start; i <= end; i++) out.push(i)
  return out
}
</script>

<template>
  <nav aria-label="Pagination" data-testid="pagination">
    <ul class="pagination">
      <li class="page-item" :class="{ disabled: page <= 1 }">
        <button class="page-link" type="button" aria-label="Previous page" :disabled="page <= 1" @click="emit('change', page - 1)">
          Previous
        </button>
      </li>
      <li v-for="p in pages()" :key="p" class="page-item" :class="{ active: p === page }">
        <button class="page-link" type="button" :aria-label="`Go to page ${p}`" @click="emit('change', p)">
          {{ p }}
        </button>
      </li>
      <li class="page-item" :class="{ disabled: page >= totalPages }">
        <button class="page-link" type="button" aria-label="Next page" :disabled="page >= totalPages" @click="emit('change', page + 1)">
          Next
        </button>
      </li>
    </ul>
  </nav>
</template>
