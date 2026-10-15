<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useCartStore } from '@/stores/cart'
import { useOrdersStore } from '@/stores/orders'
import { useUiStore } from '@/stores/ui'
import type { AddressDto, OrderDto, PaymentMethod, ShippingMethod } from '@/types'
import CheckoutForm from '@/components/CheckoutForm.vue'
import OrderSummary from '@/components/OrderSummary.vue'
import { formatINR } from '@/utils/format'

const router = useRouter()
const cart = useCartStore()
const orders = useOrdersStore()
const ui = useUiStore()

const shipping = reactive<AddressDto>({
  fullName: '',
  line1: '',
  line2: '',
  city: '',
  state: '',
  postalCode: '',
  country: 'India',
  phone: ''
})
const placed = ref<OrderDto | null>(null)
const placing = ref(false)
const placeError = ref('')

onMounted(() => {
  cart.fetch().catch(() => undefined)
})

async function place(payload: {
  shipping: AddressDto
  billing: AddressDto | null
  same: boolean
  shippingMethod: ShippingMethod
  paymentMethod: PaymentMethod
}) {
  placing.value = true
  placeError.value = ''
  try {
    const order = await orders.placeOrder({
      shippingAddress: payload.shipping,
      // Backend requires an explicit billing snapshot; "same as shipping"
      // is a UI convenience, so send a copy of the shipping address.
      billingAddress: payload.billing ?? payload.shipping,
      same: payload.same,
      shippingMethod: payload.shippingMethod,
      paymentMethod: payload.paymentMethod
    })
    placed.value = order
    await cart.fetch().catch(() => undefined)
    ui.notify(`Order ${order.orderNumber} placed`, 'success')
  } catch {
    placeError.value = 'Could not place the order. Check stock, addresses, and try again.'
    ui.notify('Failed to place order', 'danger')
  } finally {
    placing.value = false
  }
}

function goToOrder() {
  if (placed.value) router.push(`/account/orders/${placed.value.id}`)
}
</script>

<template>
  <div>
    <h1 class="h3 mb-3">Checkout</h1>
    <div v-if="placed" class="alert alert-success" data-testid="order-confirmation">
      <h2 class="h5">Thank you! Your order is confirmed.</h2>
      <p class="mb-1">Order number: <strong>{{ placed.orderNumber }}</strong></p>
      <p class="mb-1">Items: <strong>{{ placed.items.reduce((n, i) => n + i.quantity, 0) }}</strong> · Total: <strong>{{ formatINR(placed.total) }}</strong></p>
      <p class="mb-2 text-muted small">A confirmation is saved in your order history.</p>
      <div class="d-flex gap-2">
        <button class="btn btn-primary" type="button" @click="goToOrder">View order details</button>
        <RouterLink class="btn btn-outline-secondary" to="/products">Continue shopping</RouterLink>
      </div>
    </div>
    <div v-else-if="cart.isEmpty" class="stx-empty p-5 text-center">
      <h2 class="h5">Nothing to check out yet</h2>
      <p class="text-muted">Your cart is empty. Add some products first.</p>
      <RouterLink class="btn stx-btn-accent" to="/products">Browse products</RouterLink>
    </div>
    <div v-else class="row g-4">
      <div class="col-lg-7">
        <div class="card mb-3">
          <div class="card-body">
            <CheckoutForm :shipping="shipping" @submit="place($event)" />
          </div>
        </div>
        <p v-if="placing" class="text-muted mt-2">Placing your order…</p>
        <p v-if="placeError" class="alert alert-danger mt-2">{{ placeError }}</p>
      </div>
      <div class="col-lg-5">
        <OrderSummary />
      </div>
    </div>
  </div>
</template>
