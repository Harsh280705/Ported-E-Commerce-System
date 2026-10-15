<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { apiClient } from '@/api/client'
import AdminOrderTable, { type AdminOrderRow } from '@/components/AdminOrderTable.vue'
import { useUiStore } from '@/stores/ui'

const ui = useUiStore()
const orders = ref<AdminOrderRow[]>([])

async function load() {
  const res = await apiClient.get<AdminOrderRow[]>('/admin/orders')
  orders.value = res.data ?? []
}

onMounted(() => {
  load().catch(() => ui.notify('Failed to load orders', 'danger'))
})

async function updateStatus(payload: { id: string; status: string }) {
  try {
    await apiClient.put(`/admin/orders/${payload.id}/status`, { status: payload.status })
    ui.notify('Order status updated', 'success')
    await load()
  } catch {
    ui.notify('Failed to update status', 'danger')
  }
}
</script>

<template>
  <div>
    <h1 class="h3">Admin orders</h1>
    <AdminOrderTable :orders="orders" @update-status="updateStatus" />
  </div>
</template>
