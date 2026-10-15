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
import { useCartStore } from '@/stores/cart'

const get = apiClient.get as unknown as ReturnType<typeof vi.fn>
const post = apiClient.post as unknown as ReturnType<typeof vi.fn>
const put = apiClient.put as unknown as ReturnType<typeof vi.fn>
const del = apiClient.delete as unknown as ReturnType<typeof vi.fn>

const sampleCart = {
  items: [
    { productId: 'p1', productName: 'Phone', slug: 'phone', imageUrl: null, quantity: 2, unitPrice: 100, lineTotal: 200, stockAvailable: 10 },
    { productId: 'p2', productName: 'Case', slug: 'case', imageUrl: null, quantity: 1, unitPrice: 50, lineTotal: 50, stockAvailable: 5 }
  ],
  subTotal: 250,
  shipping: 40,
  tax: 25,
  grandTotal: 315,
  itemCount: 3
}

describe('cart store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('fetch populates totals and getters', async () => {
    get.mockResolvedValueOnce({ data: sampleCart })
    const store = useCartStore()
    await store.fetch()
    expect(store.items).toHaveLength(2)
    expect(store.subTotal).toBe(250)
    expect(store.itemCount).toBe(3)
    expect(store.subTotalValue).toBe(250)
    expect(store.itemCountValue).toBe(3)
    expect(store.grandTotal).toBe(315)
  })

  it('add posts productId and quantity', async () => {
    post.mockResolvedValueOnce({ data: sampleCart })
    const store = useCartStore()
    await store.add('p1', 2)
    expect(post).toHaveBeenCalledWith('/cart/items', { productId: 'p1', quantity: 2 })
    expect(store.itemCount).toBe(3)
  })

  it('update puts quantity', async () => {
    put.mockResolvedValueOnce({ data: { ...sampleCart, itemCount: 5 } })
    const store = useCartStore()
    await store.update('p1', 4)
    expect(put).toHaveBeenCalledWith('/cart/items/p1', { quantity: 4 })
    expect(store.itemCount).toBe(5)
  })

  it('remove deletes item', async () => {
    del.mockResolvedValueOnce({ data: { ...sampleCart, items: [], itemCount: 0, subTotal: 0, grandTotal: 0 } })
    const store = useCartStore()
    store.setCart(sampleCart)
    await store.remove('p1')
    expect(del).toHaveBeenCalledWith('/cart/items/p1')
    expect(store.items).toHaveLength(0)
  })

  it('mergeAfterLogin posts to /cart/merge', async () => {
    post.mockResolvedValueOnce({ data: sampleCart })
    const store = useCartStore()
    await store.mergeAfterLogin()
    expect(post).toHaveBeenCalledWith('/cart/merge')
    expect(store.items).toHaveLength(2)
  })

  it('clear resets to empty', async () => {
    del.mockResolvedValueOnce({})
    const store = useCartStore()
    store.setCart(sampleCart)
    await store.clear()
    expect(store.items).toHaveLength(0)
    expect(store.subTotal).toBe(0)
    expect(store.itemCount).toBe(0)
  })
})
