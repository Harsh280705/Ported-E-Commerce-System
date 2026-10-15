<script setup lang="ts">
import { onMounted, onUnmounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useCatalogStore } from '@/stores/catalog'
import { useProductBrowse, type BrowseMode } from '@/composables/useProductBrowse'
import type { ProductSort } from '@/types'
import ProductList from '@/components/ProductList.vue'
import Pagination from '@/components/Pagination.vue'
import LoadingSpinner from '@/components/LoadingSpinner.vue'

const route = useRoute()
const router = useRouter()
const catalog = useCatalogStore()
const browse = useProductBrowse(24)
const sentinel = ref<HTMLElement | null>(null)
let observer: IntersectionObserver | null = null
let debounce: ReturnType<typeof setTimeout> | null = null
let initialized = false

const controls = reactive({
  search: '',
  categoryId: '',
  sort: 'recommended' as ProductSort
})

function queryState() {
  return {
    search: typeof route.query.search === 'string' ? route.query.search : '',
    categoryId: typeof route.query.categoryId === 'string' ? route.query.categoryId : '',
    sort: (typeof route.query.sort === 'string' ? route.query.sort : 'recommended') as ProductSort,
    page: Number(route.query.page ?? 1) || 1
  }
}

function pushQuery(page?: number) {
  router.push({
    path: '/products',
    query: {
      search: controls.search || undefined,
      categoryId: controls.categoryId || undefined,
      sort: controls.sort,
      page: browse.mode.value === 'page' ? String(page ?? browse.page.value) : undefined
    }
  })
}

async function syncFromRoute() {
  const q = queryState()
  controls.search = q.search
  controls.categoryId = q.categoryId
  controls.sort = q.sort
  const f = browse.filters.value
  if (!initialized || f.search !== q.search || f.categoryId !== q.categoryId || f.sort !== q.sort) {
    initialized = true
    await browse.applyFilters({ search: q.search, categoryId: q.categoryId, sort: q.sort })
    if (browse.mode.value === 'page' && q.page > 1) await browse.gotoPage(q.page)
  } else if (browse.mode.value === 'page' && browse.page.value !== q.page) {
    await browse.gotoPage(q.page)
  }
}

function onSearchInput() {
  if (debounce) clearTimeout(debounce)
  debounce = setTimeout(() => pushQuery(1), 350)
}

function clearFilters() {
  controls.search = ''
  controls.categoryId = ''
  controls.sort = 'recommended'
  pushQuery(1)
}

async function setMode(m: BrowseMode) {
  await browse.setMode(m)
  if (browse.mode.value === 'page') pushQuery(1)
}

function setupObserver() {
  teardownObserver()
  if (browse.mode.value !== 'infinite' || !('IntersectionObserver' in window)) return
  observer = new IntersectionObserver(
    (entries) => {
      if (entries.some((e) => e.isIntersecting)) void browse.loadMore()
    },
    { rootMargin: '600px 0px' }
  )
  if (sentinel.value) observer.observe(sentinel.value)
}

function teardownObserver() {
  observer?.disconnect()
  observer = null
}

watch(
  () => route.query,
  () => {
    void syncFromRoute()
  }
)
watch(
  () => [browse.mode.value, browse.endReached.value, browse.error.value, browse.items.value.length] as const,
  () => {
    setupObserver()
    if (browse.mode.value === 'infinite' && sentinel.value) {
      teardownObserver()
      setupObserver()
    }
  }
)

onMounted(async () => {
  try {
    await catalog.fetchCategories()
  } catch {
    // ignore
  }
  await syncFromRoute()
  setupObserver()
})

onUnmounted(() => {
  teardownObserver()
  if (debounce) clearTimeout(debounce)
})
</script>

<template>
  <div>
    <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-3">
      <h1 class="h3 mb-0">Products</h1>
      <div class="btn-group stx-modeswitch" role="group" aria-label="Browsing mode">
        <button
          type="button"
          class="btn btn-outline-secondary"
          :class="{ active: browse.mode.value === 'page' }"
          @click="setMode('page')"
        >
          Pagination
        </button>
        <button
          type="button"
          class="btn btn-outline-secondary"
          :class="{ active: browse.mode.value === 'infinite' }"
          @click="setMode('infinite')"
        >
          Infinite scroll
        </button>
      </div>
    </div>

    <div class="stx-toolbar p-3 mb-3">
      <div class="row g-3">
        <div class="col-md-4">
          <label for="pl-search" class="form-label">Search</label>
          <input
            id="pl-search"
            v-model="controls.search"
            class="form-control"
            placeholder="Name, description or SKU…"
            @input="onSearchInput"
            @keyup.enter="pushQuery(1)"
          />
        </div>
        <div class="col-md-4">
          <label for="pl-category" class="form-label">Category</label>
          <select id="pl-category" v-model="controls.categoryId" class="form-select" @change="pushQuery(1)">
            <option value="">All categories</option>
            <option v-for="c in catalog.categories" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>
        <div class="col-md-4">
          <label for="pl-sort" class="form-label">Sort</label>
          <select id="pl-sort" v-model="controls.sort" class="form-select" @change="pushQuery(1)">
            <option value="recommended">Recommended</option>
            <option value="price_asc">Price: low to high</option>
            <option value="price_desc">Price: high to low</option>
            <option value="name_asc">Name A–Z</option>
            <option value="name_desc">Name Z–A</option>
            <option value="newest">Newest</option>
          </select>
        </div>
      </div>
      <div class="d-flex flex-wrap align-items-center gap-2 mt-3">
        <span class="text-muted small" data-testid="result-count">
          {{ browse.totalCount.value }} result{{ browse.totalCount.value === 1 ? '' : 's' }}
          <template v-if="browse.mode.value === 'page' && browse.totalPages.value > 0">
            · page {{ browse.page.value }} of {{ browse.totalPages.value }}
          </template>
          <template v-if="browse.mode.value === 'infinite' && browse.items.value.length > 0">
            · showing {{ browse.items.value.length }}
          </template>
        </span>
        <span v-if="controls.search" class="stx-chip">“{{ controls.search }}” <button type="button" aria-label="Clear search" @click="controls.search = ''; pushQuery(1)">×</button></span>
        <span v-if="controls.categoryId" class="stx-chip">Category <button type="button" aria-label="Clear category" @click="controls.categoryId = ''; pushQuery(1)">×</button></span>
        <span v-if="controls.sort !== 'recommended'" class="stx-chip">Sorted <button type="button" aria-label="Clear sort" @click="controls.sort = 'recommended'; pushQuery(1)">×</button></span>
        <button
          v-if="controls.search || controls.categoryId || controls.sort !== 'recommended'"
          type="button"
          class="btn btn-sm btn-link"
          @click="clearFilters"
        >
          Clear all
        </button>
      </div>
    </div>

    <div v-if="browse.error.value && browse.items.value.length === 0" class="alert alert-danger d-flex justify-content-between align-items-center">
      <span>{{ browse.error.value }}</span>
      <button class="btn btn-sm btn-outline-danger" type="button" @click="browse.retry()">Retry</button>
    </div>
    <template v-else>
      <ProductList :products="browse.items.value" :loading="browse.loading.value || !browse.hasLoaded.value" />

      <template v-if="browse.mode.value === 'page'">
        <Pagination
          v-if="!browse.loading.value && browse.totalPages.value > 1"
          :page="browse.page.value"
          :total-pages="browse.totalPages.value"
          class="mt-3"
          @change="pushQuery($event)"
        />
      </template>
      <template v-else>
        <div ref="sentinel" aria-hidden="true" style="height: 2px"></div>
        <div class="text-center my-3">
          <LoadingSpinner v-if="browse.loadingMore.value" label="Loading more products…" />
          <p v-else-if="browse.endReached.value && browse.items.value.length > 0" class="text-muted" data-testid="end-of-results">
            You have seen all {{ browse.totalCount.value }} products.
          </p>
          <div v-else-if="browse.error.value" class="alert alert-warning d-inline-flex gap-2 align-items-center">
            <span>{{ browse.error.value }}</span>
            <button class="btn btn-sm btn-outline-secondary" type="button" @click="browse.retry()">Retry</button>
          </div>
        </div>
      </template>
    </template>
  </div>
</template>
