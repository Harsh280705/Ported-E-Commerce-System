<script setup lang="ts">
import { formatINR, formatDate } from '@/utils/format'

export interface AdminOrderRow {
  id: string
  orderNumber: string
  orderDate: string
  status: string
  total: number
  itemCount: number
}

defineProps<{ orders: AdminOrderRow[] }>()
const emit = defineEmits<{ (e: 'update-status', payload: { id: string; status: string }): void }>()

const statuses = ['Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled']
</script>

<template>
  <div class="table-responsive">
    <table class="table table-striped" data-testid="admin-order-table">
      <thead>
        <tr>
          <th scope="col">Order #</th>
          <th scope="col">Date</th>
          <th scope="col">Status</th>
          <th scope="col">Items</th>
          <th scope="col">Total</th>
          <th scope="col">Update</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="o in orders" :key="o.id">
          <td>{{ o.orderNumber }}</td>
          <td>{{ formatDate(o.orderDate) }}</td>
          <td>{{ o.status }}</td>
          <td>{{ o.itemCount }}</td>
          <td>{{ formatINR(o.total) }}</td>
          <td>
            <label :for="`status-${o.id}`" class="visually-hidden">Update status for {{ o.orderNumber }}</label>
            <select
              :id="`status-${o.id}`"
              :value="o.status"
              class="form-select form-select-sm"
              @change="emit('update-status', { id: o.id, status: ($event.target as HTMLSelectElement).value })"
            >
              <option v-for="s in statuses" :key="s" :value="s">{{ s }}</option>
            </select>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
