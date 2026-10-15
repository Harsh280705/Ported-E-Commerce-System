import { ref, computed } from 'vue'
import { apiClient } from '@/api/client'
import type { ProductCardDto, ProductSort } from '@/types'

export type BrowseMode = 'page' | 'infinite'

export interface BrowseFilters {
  search: string
  categoryId: string
  sort: ProductSort
}

interface PageEnvelope {
  items: ProductCardDto[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

const MODE_KEY = 'browse-mode'

function loadMode(): BrowseMode {
  try {
    return localStorage.getItem(MODE_KEY) === 'infinite' ? 'infinite' : 'page'
  } catch {
    return 'page'
  }
}

/**
 * Server-driven product browsing with two modes. Pagination replaces the list
 * per page; infinite appends pages with id-dedupe. A monotonically increasing
 * request id makes stale responses harmless (last query wins).
 */
export function useProductBrowse(pageSize = 24) {
  const mode = ref<BrowseMode>(loadMode())
  const items = ref<ProductCardDto[]>([])
  const page = ref(1)
  const totalPages = ref(0)
  const totalCount = ref(0)
  const loading = ref(false)
  const loadingMore = ref(false)
  const error = ref('')
  const hasLoaded = ref(false)
  const filters = ref<BrowseFilters>({ search: '', categoryId: '', sort: 'recommended' })

  let reqId = 0
  const seen = new Set<string | number>()

  const endReached = computed(
    () => items.value.length > 0 && page.value >= totalPages.value
  )
  const canLoadMore = computed(
    () => mode.value === 'infinite' && !loading.value && !loadingMore.value && !endReached.value && !error.value
  )

  function paramsFor(p: number): Record<string, string | number> {
    const f = filters.value
    const params: Record<string, string | number> = { page: p, pageSize }
    if (f.categoryId) params.categoryId = f.categoryId
    if (f.search) params.search = f.search
    if (f.sort) params.sort = f.sort
    return params
  }

  async function fetchPage(p: number, append: boolean): Promise<void> {
    const id = ++reqId
    if (append) loadingMore.value = true
    else loading.value = true
    error.value = ''
    try {
      const res = await apiClient.get<PageEnvelope>('/products', { params: paramsFor(p) })
      if (id !== reqId) return // stale response — ignore
      const env = res.data
      page.value = env.page ?? p
      totalPages.value = env.totalPages ?? 0
      totalCount.value = env.totalCount ?? 0
      const fresh = (env.items ?? []).filter((it) => {
        if (seen.has(it.id)) return false
        seen.add(it.id)
        return true
      })
      items.value = append ? [...items.value, ...fresh] : fresh
    } catch {
      if (id !== reqId) return
      error.value = 'Could not load products. Check your connection and retry.'
    } finally {
      if (id === reqId) {
        loading.value = false
        loadingMore.value = false
        if (!append) hasLoaded.value = true
      }
    }
  }

  function resetList() {
    reqId++ // invalidate in-flight responses
    items.value = []
    seen.clear()
    page.value = 1
    totalPages.value = 0
    totalCount.value = 0
    error.value = ''
    loading.value = false
    loadingMore.value = false
    hasLoaded.value = false
  }

  async function initialLoad(): Promise<void> {
    resetList()
    await fetchPage(1, false)
  }

  async function applyFilters(f: BrowseFilters): Promise<void> {
    filters.value = { ...f }
    await initialLoad()
  }

  async function gotoPage(p: number): Promise<void> {
    const target = Math.min(Math.max(1, p), Math.max(1, totalPages.value))
    resetList()
    await fetchPage(target, false)
  }

  async function loadMore(): Promise<void> {
    if (!canLoadMore.value) return
    if (loadingMore.value) return // prevent duplicate concurrent requests
    await fetchPage(page.value + 1, true)
  }

  async function retry(): Promise<void> {
    if (mode.value === 'infinite' && items.value.length > 0) {
      await fetchPage(page.value + 1, true)
    } else {
      await initialLoad()
    }
  }

  async function setMode(m: BrowseMode): Promise<void> {
    if (mode.value === m) return
    mode.value = m
    try {
      localStorage.setItem(MODE_KEY, m)
    } catch {
      // ignore storage errors
    }
    await initialLoad()
  }

  return {
    mode,
    items,
    page,
    totalPages,
    totalCount,
    loading,
    loadingMore,
    hasLoaded,
    error,
    filters,
    endReached,
    canLoadMore,
    applyFilters,
    gotoPage,
    loadMore,
    retry,
    setMode,
    initialLoad
  }
}
