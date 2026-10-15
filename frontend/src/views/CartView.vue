<script setup lang="ts">
import { onMounted } from 'vue'
import { useCartStore } from '@/stores/cart'
import CartItemRow from '@/components/CartItemRow.vue'
import { formatINR } from '@/utils/format'

const cart = useCartStore()

onMounted(() => {
  cart.fetch().catch(() => undefined)
})
</script>

<template>
  <div>
    <h1 class="h3 mb-3">Your cart</h1>
    <div v-if="cart.items.length === 0" class="stx-empty p-5 text-center">
      <h2 class="h5">Empty basket</h2>
      <p class="text-muted">Browse the catalog and add something you like.</p>
      <RouterLink class="btn stx-btn-accent" to="/products">Continue shopping</RouterLink>
    </div>
    <div v-else class="row g-4">
      <div class="col-lg-8 d-flex flex-column gap-2">
        <CartItemRow v-for="item in cart.items" :key="item.productId" :item="item" />
        <div><RouterLink class="btn btn-link ps-0" to="/products">Continue shopping</RouterLink></div>
      </div>
      <div class="col-lg-4">
        <div class="card">
          <div class="card-header fw-semibold">Order summary</div>
          <div class="card-body">
            <div class="d-flex justify-content-between"><span>Subtotal</span><span>{{ formatINR(cart.subTotal) }}</span></div>
            <div class="d-flex justify-content-between"><span>Shipping</span><span>{{ formatINR(cart.shipping) }}</span></div>
            <div class="d-flex justify-content-between"><span>Tax</span><span>{{ formatINR(cart.tax) }}</span></div>
            <hr />
            <div class="d-flex justify-content-between fs-5"><strong>Total</strong><strong>{{ formatINR(cart.grandTotal) }}</strong></div>
            <p class="text-muted small mt-2 mb-0">Shipping method can be changed at checkout.</p>
            <RouterLink class="btn btn-success w-100 mt-3" to="/checkout">Proceed to checkout</RouterLink>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
