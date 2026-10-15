<script setup lang="ts">
import type { CartItemDto } from '@/types'
import { formatINR, onImageError, PLACEHOLDER_IMAGE } from '@/utils/format'
import { useCartStore } from '@/stores/cart'
import { useUiStore } from '@/stores/ui'

const props = defineProps<{ item: CartItemDto }>()
const cart = useCartStore()
const ui = useUiStore()

async function setQty(qty: number) {
  try {
    if (qty <= 0) {
      await cart.remove(props.item.productId)
    } else {
      await cart.update(props.item.productId, qty)
    }
  } catch {
    ui.notify('Failed to update cart', 'danger')
  }
}

async function remove() {
  try {
    await cart.remove(props.item.productId)
  } catch {
    ui.notify('Failed to remove item', 'danger')
  }
}
</script>

<template>
  <div class="d-flex gap-2 align-items-center border rounded p-2" data-testid="cart-item-row">
    <img
      :src="item.imageUrl || PLACEHOLDER_IMAGE"
      :alt="item.productName"
      width="64"
      height="64"
      class="rounded border"
      @error="onImageError"
    />
    <div class="flex-grow-1">
      <div class="fw-semibold">{{ item.productName }}</div>
      <div class="text-muted small">{{ formatINR(item.unitPrice) }} each</div>
      <div class="d-flex align-items-center gap-2 mt-1">
        <label :for="`qty-${item.productId}`" class="visually-hidden">Quantity</label>
        <input
          :id="`qty-${item.productId}`"
          :value="item.quantity"
          type="number"
          min="0"
          :max="item.stockAvailable || 99"
          class="form-control form-control-sm"
          style="max-width: 80px"
          @change="setQty(Number(($event.target as HTMLInputElement).value))"
        />
        <button class="btn btn-sm btn-outline-danger" type="button" @click="remove">Remove</button>
      </div>
    </div>
    <strong>{{ formatINR(item.lineTotal) }}</strong>
  </div>
</template>
