import { defineStore } from 'pinia'
import { apiClient } from '@/api/client'
import type { CartDto } from '@/types'

interface CartState {
  items: CartDto['items']
  subTotal: number
  shipping: number
  tax: number
  grandTotal: number
  itemCount: number
}

function emptyState(): CartState {
  return { items: [], subTotal: 0, shipping: 0, tax: 0, grandTotal: 0, itemCount: 0 }
}

export const useCartStore = defineStore('cart', {
  state: (): CartState => emptyState(),
  getters: {
    subTotalValue: (s): number => s.subTotal,
    itemCountValue: (s): number => s.itemCount,
    isEmpty: (s): boolean => s.items.length === 0
  },
  actions: {
    setCart(dto: CartDto) {
      this.items = dto.items ?? []
      this.subTotal = dto.subTotal ?? 0
      this.shipping = dto.shipping ?? 0
      this.tax = dto.tax ?? 0
      this.grandTotal = dto.grandTotal ?? 0
      this.itemCount = dto.itemCount ?? 0
    },
    async fetch(): Promise<void> {
      const res = await apiClient.get<CartDto>('/cart')
      this.setCart(res.data)
    },
    async add(productId: string, quantity = 1): Promise<void> {
      const res = await apiClient.post<CartDto>('/cart/items', { productId, quantity })
      this.setCart(res.data)
    },
    async update(productId: string, quantity: number): Promise<void> {
      const res = await apiClient.put<CartDto>(`/cart/items/${productId}`, { quantity })
      this.setCart(res.data)
    },
    async remove(productId: string): Promise<void> {
      const res = await apiClient.delete<CartDto>(`/cart/items/${productId}`)
      this.setCart(res.data)
    },
    async clear(): Promise<void> {
      await apiClient.delete('/cart')
      Object.assign(this, emptyState())
    },
    async mergeAfterLogin(): Promise<void> {
      // X-Cart-Id header is attached automatically by the api client
      const res = await apiClient.post<CartDto>('/cart/merge')
      this.setCart(res.data)
    }
  }
})
