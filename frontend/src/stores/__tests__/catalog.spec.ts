import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'

vi.mock('@/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn()
  },
  setAccessToken: vi.fn(),
  getAccessToken: vi.fn(() => ''),
  getCartId: vi.fn(() => 'guest-123'),
  ensureCartId: vi.fn(() => 'guest-123')
}))

import { apiClient } from '@/api/client'
import { useCatalogStore } from '@/stores/catalog'

const get = apiClient.get as unknown as ReturnType<typeof vi.fn>

describe('catalog store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('fetchCategories loads categories', async () => {
    get.mockResolvedValueOnce({
      data: [{ id: 'c1', name: 'Phones', slug: 'phones', productCount: 4 }]
    })
    const store = useCatalogStore()
    await store.fetchCategories()
    expect(store.categories).toHaveLength(1)
    expect(store.categories[0].name).toBe('Phones')
  })

  it('fetchProducts sends search/pagination/sort params', async () => {
    get.mockResolvedValueOnce({
      data: { items: [], page: 2, pageSize: 12, totalCount: 30, totalPages: 3 }
    })
    const store = useCatalogStore()
    await store.fetchProducts({ page: 2, search: 'phone', sort: 'price_asc', categoryId: 'c1' })
    expect(get).toHaveBeenCalledWith(
      '/products',
      expect.objectContaining({
        params: expect.objectContaining({ page: 2, search: 'phone', sort: 'price_asc', categoryId: 'c1' })
      })
    )
    expect(store.page).toBe(2)
    expect(store.totalPages).toBe(3)
    expect(store.search).toBe('phone')
    expect(store.sort).toBe('price_asc')
    expect(store.selectedCategory).toBe('c1')
  })

  it('fetchProducts omits empty search and category', async () => {
    get.mockResolvedValueOnce({
      data: { items: [], page: 1, pageSize: 12, totalCount: 0, totalPages: 0 }
    })
    const store = useCatalogStore()
    await store.fetchProducts({ page: 1, search: '', categoryId: '' })
    const params = (get.mock.calls[0] as unknown as Array<{ params: Record<string, unknown> }>)[1].params
    expect(params).not.toHaveProperty('search')
    expect(params).not.toHaveProperty('categoryId')
  })

  it('setters update filter state', () => {
    const store = useCatalogStore()
    store.setSearch('laptop')
    store.setSort('name_desc')
    store.setCategory('c9')
    store.setPage(4)
    expect(store.search).toBe('laptop')
    expect(store.sort).toBe('name_desc')
    expect(store.selectedCategory).toBe('c9')
    expect(store.page).toBe(4)
  })
})
