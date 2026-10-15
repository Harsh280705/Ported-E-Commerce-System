<script setup lang="ts">
import { useCartStore } from '@/stores/cart'
import { formatINR } from '@/utils/format'

const cart = useCartStore()
</script>

<template>
  <div class="card" data-testid="order-summary">
    <div class="card-header">Order summary</div>
    <div class="card-body">
      <div v-for="item in cart.items" :key="item.productId" class="d-flex justify-content-between small">
        <span>{{ item.productName }} × {{ item.quantity }}</span>
        <span>{{ formatINR(item.lineTotal) }}</span>
      </div>
      <hr />
      <div class="d-flex justify-content-between"><span>Subtotal</span><span>{{ formatINR(cart.subTotal) }}</span></div>
      <div class="d-flex justify-content-between"><span>Shipping</span><span>{{ formatINR(cart.shipping) }}</span></div>
      <div class="d-flex justify-content-between"><span>Tax</span><span>{{ formatINR(cart.tax) }}</span></div>
      <hr />
      <div class="d-flex justify-content-between">
        <strong>Total</strong><strong>{{ formatINR(cart.grandTotal) }}</strong>
      </div>
    </div>
  </div>
</template>
