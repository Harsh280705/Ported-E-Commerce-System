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
import { useOrdersStore, newIdempotencyKey } from '@/stores/orders'

const get = apiClient.get as unknown as ReturnType<typeof vi.fn>
const post = apiClient.post as unknown as ReturnType<typeof vi.fn>

describe('orders store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('generates unique idempotency keys', () => {
    const a = newIdempotencyKey()
    const b = newIdempotencyKey()
    expect(a).toBeTruthy()
    expect(b).toBeTruthy()
    expect(a).not.toBe(b)
  })

  it('placeOrder attaches idempotencyKey', async () => {
    const order = { id: 'o1', orderNumber: 'ORD-1', total: 100 }
    post.mockResolvedValueOnce({ data: order })
    const store = useOrdersStore()
    const result = await store.placeOrder({
      shippingAddress: {
        fullName: 'A B',
        line1: '1 St',
        city: 'Pune',
        state: 'MH',
        postalCode: '411001',
        country: 'India'
      },
      shippingMethod: 'standard',
      paymentMethod: 'cod'
    })
    expect(result).toEqual(order)
    expect(post).toHaveBeenCalledWith(
      '/orders',
      expect.objectContaining({ idempotencyKey: expect.any(String) })
    )
    expect(store.current).toEqual(order)
  })

  it('fetchHistory and fetchDetail work', async () => {
    get.mockResolvedValueOnce({ data: [{ id: 'o1', orderNumber: 'ORD-1' }] })
    const store = useOrdersStore()
    await store.fetchHistory()
    expect(store.history).toHaveLength(1)
    get.mockResolvedValueOnce({ data: { id: 'o1', orderNumber: 'ORD-1' } })
    const detail = await store.fetchDetail('o1')
    expect(detail.orderNumber).toBe('ORD-1')
    expect(get).toHaveBeenCalledWith('/orders/o1')
  })
})
