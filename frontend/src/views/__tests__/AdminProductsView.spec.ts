import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'

vi.mock('@/api/client', () => ({
  apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() }
}))

import { apiClient } from '@/api/client'
import AdminProductsView from '@/views/AdminProductsView.vue'

const get = apiClient.get as unknown as ReturnType<typeof vi.fn>

const rows = [
  { id: '1', name: 'Real Phone', slug: 'real-phone', price: 9999, stock: 5, inStock: true, imageUrl: '/images/products/real-phone.svg', categoryId: 'c1', categoryName: 'Electronics' },
  { id: '2', name: 'Old Case', slug: 'old-case', price: 499, stock: 0, inStock: false, imageUrl: '/images/placeholder.png', categoryId: 'c1', categoryName: 'Electronics' }
]

describe('AdminProductsView', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    get.mockImplementation((url: string) => {
      if (url === '/admin/categories') return Promise.resolve({ data: [{ id: 'c1', name: 'Electronics', slug: 'electronics' }] })
      return Promise.resolve({ data: { items: rows, page: 1, pageSize: 12, totalCount: 2, totalPages: 1 } })
    })
  })

  it('lists real database products with image status', async () => {
    const wrapper = mount(AdminProductsView, { global: { plugins: [createPinia()] } })
    await flushPromises()
    expect(wrapper.text()).toContain('Real Phone')
    expect(wrapper.text()).toContain('2 products in database')
    expect(wrapper.text()).toContain('Fallback image')
    expect(wrapper.text()).toContain('Custom image')
  })

  it('sends search text to the server', async () => {
    const wrapper = mount(AdminProductsView, { global: { plugins: [createPinia()] } })
    await flushPromises()
    const input = wrapper.find('#ap-search')
    await input.setValue('phone')
    await new Promise((r) => setTimeout(r, 450))
    await flushPromises()
    const calls = get.mock.calls.filter((c) => (c[0] as string) === '/admin/products')
    const last = calls[calls.length - 1][1] as { params: Record<string, unknown> }
    expect(last.params.search).toBe('phone')
  })
})
