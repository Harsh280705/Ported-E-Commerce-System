<script setup lang="ts">
import { useCartStore } from '@/stores/cart'
import { useUiStore } from '@/stores/ui'
import { formatINR } from '@/utils/format'
import CartItemRow from '@/components/CartItemRow.vue'

const cart = useCartStore()
const ui = useUiStore()

function close() {
  ui.setMiniCartOpen(false)
}
</script>

<template>
  <div
    class="offcanvas offcanvas-end"
    :class="{ show: ui.miniCartOpen }"
    :style="{ visibility: ui.miniCartOpen ? 'visible' : 'hidden' }"
    tabindex="-1"
    aria-label="Mini cart"
    data-testid="cart-drawer"
  >
    <div class="offcanvas-header border-bottom">
      <h5 class="offcanvas-title">Cart ({{ cart.itemCount }})</h5>
      <button type="button" class="btn-close" aria-label="Close cart" @click="close"></button>
    </div>
    <div class="offcanvas-body">
      <div v-if="cart.items.length === 0" class="text-center py-4">
        <p class="text-muted">Your cart is empty.</p>
        <RouterLink class="btn btn-sm stx-btn-accent" to="/products" @click="close">Browse products</RouterLink>
      </div>
      <div v-else class="d-flex flex-column gap-2">
        <CartItemRow v-for="item in cart.items" :key="item.productId" :item="item" />
        <hr />
        <div class="d-flex justify-content-between">
          <span>Subtotal</span>
          <strong>{{ formatINR(cart.subTotal) }}</strong>
        </div>
        <div class="d-flex justify-content-between text-muted small">
          <span>Shipping + tax</span>
          <span>{{ formatINR(cart.shipping + cart.tax) }}</span>
        </div>
        <div class="d-flex justify-content-between fs-5">
          <strong>Total</strong>
          <strong>{{ formatINR(cart.grandTotal) }}</strong>
        </div>
        <RouterLink class="btn stx-btn-primary" to="/cart" @click="close">View cart</RouterLink>
        <RouterLink class="btn btn-success" to="/checkout" @click="close">Checkout</RouterLink>
      </div>
    </div>
  </div>
</template>
