import { describe, it, expect, vi, beforeEach } from 'vitest'

vi.mock('@/api/client', () => ({
  apiClient: { get: vi.fn() }
}))

import { apiClient } from '@/api/client'
import { useProductBrowse } from '@/composables/useProductBrowse'

const get = apiClient.get as unknown as ReturnType<typeof vi.fn>

function page(items: Array<{ id: string }>, page = 1, totalPages = 3, totalCount = 60) {
  return { data: { items, page, pageSize: 2, totalCount, totalPages } }
}

const item = (id: string) => ({
  id,
  name: `Product ${id}`,
  slug: id,
  price: 100,
  stock: 5,
  inStock: true,
  categoryId: 'c1'
})

describe('useProductBrowse', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    try {
      localStorage.removeItem('browse-mode')
    } catch {
      // ignore
    }
  })

  it('loads the first page in pagination mode', async () => {
    get.mockResolvedValueOnce(page([item('a'), item('b')]))
    const b = useProductBrowse(2)
    await b.initialLoad()
    expect(b.items.value).toHaveLength(2)
    expect(b.page.value).toBe(1)
    expect(b.totalPages.value).toBe(3)
    expect(get).toHaveBeenCalledWith('/products', expect.objectContaining({ params: expect.objectContaining({ page: 1 }) }))
  })

  it('appends without duplicates in infinite mode', async () => {
    const b = useProductBrowse(2)
    await b.setMode('infinite')
    get.mockResolvedValueOnce(page([item('a'), item('b')], 1, 2, 4))
    await b.initialLoad()
    get.mockResolvedValueOnce(page([item('b'), item('c')], 2, 2, 4))
    await b.loadMore()
    expect(b.items.value.map((i) => i.id)).toEqual(['a', 'b', 'c'])
    expect(b.endReached.value).toBe(true)
  })

  it('ignores stale responses (last query wins)', async () => {
    const b = useProductBrowse(2)
    let resolveFirst!: (v: unknown) => void
    const first = new Promise((res) => {
      resolveFirst = res
    })
    get.mockReturnValueOnce(first)
    const p1 = b.applyFilters({ search: 'phone', categoryId: '', sort: 'recommended' })
    get.mockResolvedValueOnce(page([item('z')], 1, 1, 1))
    await b.applyFilters({ search: 'watch', categoryId: '', sort: 'recommended' })
    resolveFirst(page([item('stale')], 1, 1, 1))
    await p1
    expect(b.items.value.map((i) => i.id)).toEqual(['z'])
  })

  it('switching modes resets the list and persists the choice', async () => {
    const b = useProductBrowse(2)
    get.mockResolvedValueOnce(page([item('a')], 1, 1, 1))
    await b.initialLoad()
    get.mockResolvedValueOnce(page([item('b')], 1, 1, 1))
    await b.setMode('infinite')
    expect(b.items.value.map((i) => i.id)).toEqual(['b'])
    expect(b.mode.value).toBe('infinite')
  })

  it('tracks first-load completion for skeleton display', async () => {
    const b = useProductBrowse(2)
    expect(b.hasLoaded.value).toBe(false)
    get.mockResolvedValueOnce(page([item('a')], 1, 1, 1))
    await b.initialLoad()
    expect(b.hasLoaded.value).toBe(true)
  })

  it('retry reloads after failure', async () => {    const b = useProductBrowse(2)
    get.mockRejectedValueOnce(new Error('down'))
    await b.initialLoad()
    expect(b.error.value).toBeTruthy()
    get.mockResolvedValueOnce(page([item('a')], 1, 1, 1))
    await b.retry()
    expect(b.items.value).toHaveLength(1)
    expect(b.error.value).toBe('')
  })
})
