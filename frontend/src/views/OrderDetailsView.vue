<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useOrdersStore } from '@/stores/orders'
import { formatINR, formatDate } from '@/utils/format'

const props = defineProps<{ id: string }>()
const orders = useOrdersStore()
const error = ref('')

onMounted(async () => {
  try {
    await orders.fetchDetail(props.id)
  } catch {
    error.value = 'Order not found.'
  }
})
</script>

<template>
  <div>
    <h1 class="h3">Order details</h1>
    <p v-if="error" class="alert alert-danger">{{ error }}</p>
    <div v-else-if="orders.current" class="card">
      <div class="card-body">
        <p><strong>Order:</strong> {{ orders.current.orderNumber }}</p>
        <p><strong>Date:</strong> {{ formatDate(orders.current.orderDate) }}</p>
        <p><strong>Status:</strong> {{ orders.current.status }}</p>
        <table class="table">
          <thead><tr><th scope="col">Product</th><th scope="col">Qty</th><th scope="col">Price</th><th scope="col">Total</th></tr></thead>
          <tbody>
            <tr v-for="i in orders.current.items" :key="i.productId">
              <td>{{ i.productName }}</td>
              <td>{{ i.quantity }}</td>
              <td>{{ formatINR(i.unitPrice) }}</td>
              <td>{{ formatINR(i.lineTotal) }}</td>
            </tr>
          </tbody>
        </table>
        <p><strong>Total:</strong> {{ formatINR(orders.current.total) }}</p>
      </div>
    </div>
  </div>
</template>
