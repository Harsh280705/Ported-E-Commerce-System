<script setup lang="ts">
import { reactive } from 'vue'
import type { AddressDto, PaymentMethod, ShippingMethod } from '@/types'
import AddressForm from '@/components/AddressForm.vue'

const props = withDefaults(
  defineProps<{
    shipping: AddressDto
    billing?: AddressDto | null
    same?: boolean
    shippingMethod?: ShippingMethod
    paymentMethod?: PaymentMethod
  }>(),
  { billing: null, same: true, shippingMethod: 'standard', paymentMethod: 'cod' }
)

const emit = defineEmits<{
  (e: 'submit', payload: { shipping: AddressDto; billing: AddressDto | null; same: boolean; shippingMethod: ShippingMethod; paymentMethod: PaymentMethod }): void
}>()

const state = reactive({
  shipping: { ...props.shipping },
  billing: { ...(props.billing ?? { ...props.shipping }) },
  same: props.same,
  shippingMethod: props.shippingMethod as ShippingMethod,
  paymentMethod: props.paymentMethod as PaymentMethod
})

function submit() {
  emit('submit', {
    shipping: { ...state.shipping },
    billing: state.same ? null : { ...state.billing },
    same: state.same,
    shippingMethod: state.shippingMethod,
    paymentMethod: state.paymentMethod
  })
}
</script>

<template>
  <form data-testid="checkout-form" @submit.prevent="submit">
    <h2 class="h5">Shipping address</h2>
    <AddressForm v-model="state.shipping" prefix="ship" />
    <div class="form-check my-3">
      <input id="same-billing" v-model="state.same" type="checkbox" class="form-check-input" />
      <label for="same-billing" class="form-check-label">Billing same as shipping</label>
    </div>
    <div v-if="!state.same">
      <h2 class="h5">Billing address</h2>
      <AddressForm v-model="state.billing" prefix="bill" />
    </div>
    <div class="row g-3 mt-1">
      <div class="col-md-6">
        <label for="shipping-method" class="form-label">Shipping method</label>
        <select id="shipping-method" v-model="state.shippingMethod" class="form-select">
          <option value="standard">Standard</option>
          <option value="express">Express</option>
          <option value="priority">Priority</option>
        </select>
      </div>
      <div class="col-md-6">
        <label for="payment-method" class="form-label">Payment method</label>
        <select id="payment-method" v-model="state.paymentMethod" class="form-select">
          <option value="cod">Cash on delivery</option>
          <option value="card_demo">Card (demo)</option>
          <option value="upi_demo">UPI (demo)</option>
          <option value="netbanking_demo">Netbanking (demo)</option>
        </select>
      </div>
    </div>
    <slot name="summary"></slot>
    <button class="btn btn-success mt-3" type="submit">Place order</button>
  </form>
</template>
