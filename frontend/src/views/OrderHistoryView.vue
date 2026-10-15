<script setup lang="ts">
import { onMounted } from 'vue'
import { useOrdersStore } from '@/stores/orders'
import { formatDate, formatINR } from '@/utils/format'

const orders = useOrdersStore()

onMounted(() => {
  orders.fetchHistory().catch(() => undefined)
})
</script>

<template>
  <div>
    <h1 class="h3">Order history</h1>
    <div v-if="orders.history.length === 0" class="alert alert-info">No orders yet.</div>
    <div v-else class="table-responsive">
      <table class="table table-striped">
        <thead>
          <tr><th scope="col">Order #</th><th scope="col">Date</th><th scope="col">Status</th><th scope="col">Items</th><th scope="col">Total</th></tr>
        </thead>
        <tbody>
          <tr v-for="o in orders.history" :key="o.id">
            <td><RouterLink :to="`/account/orders/${o.id}`">{{ o.orderNumber }}</RouterLink></td>
            <td>{{ formatDate(o.orderDate) }}</td>
            <td>{{ o.status }}</td>
            <td>{{ o.itemCount }}</td>
            <td>{{ formatINR(o.total) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
