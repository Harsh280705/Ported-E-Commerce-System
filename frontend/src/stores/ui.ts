import { defineStore } from 'pinia'
import type { ToastItem } from '@/types'

interface UiState {
  loading: boolean
  toasts: ToastItem[]
  miniCartOpen: boolean
}

function uid(): string {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`
}

export const useUiStore = defineStore('ui', {
  state: (): UiState => ({ loading: false, toasts: [], miniCartOpen: false }),
  actions: {
    setLoading(v: boolean) {
      this.loading = v
    },
    notify(message: string, type: ToastItem['type'] = 'info'): string {
      const id = uid()
      this.toasts.push({ id, message, type })
      return id
    },
    dismiss(id: string) {
      this.toasts = this.toasts.filter((t) => t.id !== id)
    },
    setMiniCartOpen(v: boolean) {
      this.miniCartOpen = v
    }
  }
})
