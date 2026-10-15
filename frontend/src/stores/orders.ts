import { defineStore } from 'pinia'
import { apiClient } from '@/api/client'
import type { CreateOrderRequest, OrderDto, OrderSummaryDto } from '@/types'

interface OrdersState {
  history: OrderSummaryDto[]
  current: OrderDto | null
}

function uuid(): string {
  try {
    if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
      return (crypto as Crypto).randomUUID()
    }
  } catch {
    // fall through
  }
  return `${Date.now()}-${Math.random().toString(36).slice(2)}`
}

export function newIdempotencyKey(): string {
  return uuid()
}

export const useOrdersStore = defineStore('orders', {
  state: (): OrdersState => ({ history: [], current: null }),
  actions: {
    async placeOrder(payload: CreateOrderRequest): Promise<OrderDto> {
      const body: CreateOrderRequest = {
        ...payload,
        idempotencyKey: payload.idempotencyKey || uuid()
      }
      const res = await apiClient.post<OrderDto>('/orders', body)
      this.current = res.data
      return res.data
    },
    async fetchHistory(): Promise<void> {
      const res = await apiClient.get<OrderSummaryDto[]>('/orders')
      this.history = res.data ?? []
    },
    async fetchDetail(id: string): Promise<OrderDto> {
      const res = await apiClient.get<OrderDto>(`/orders/${id}`)
      this.current = res.data
      return res.data
    }
  }
})
