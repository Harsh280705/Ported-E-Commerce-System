<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { apiClient } from '@/api/client'
import type { AdminDashboardDto, CategoryDto, OrderSummaryDto } from '@/types'
import { formatINR, formatDate } from '@/utils/format'

const stats = ref<AdminDashboardDto | null>(null)
const categories = ref(0)
const recent = ref<OrderSummaryDto[]>([])
const loading = ref(false)
const error = ref('')

onMounted(async () => {
  loading.value = true
  try {
    const [dash, cats, ords] = await Promise.all([
      apiClient.get<AdminDashboardDto>('/admin/dashboard'),
      apiClient.get<CategoryDto[]>('/admin/categories'),
      apiClient.get<OrderSummaryDto[]>('/admin/orders')
    ])
    stats.value = dash.data
    categories.value = (cats.data ?? []).length
    recent.value = (ords.data ?? []).slice(0, 5)
  } catch {
    error.value = 'Could not load dashboard data.'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div>
    <h1 class="h3 mb-3">Admin dashboard</h1>
    <div v-if="loading" class="text-muted">Loading dashboard…</div>
    <div v-else-if="error" class="alert alert-danger">{{ error }}</div>
    <template v-else-if="stats">
      <div class="row g-3" data-testid="admin-stats">
        <div class="col-6 col-md-3"><div class="card stx-statcard"><div class="card-body"><div class="text-muted small">Products</div><div class="fs-4 fw-bold">{{ stats.totalProducts }}</div></div></div></div>
        <div class="col-6 col-md-3"><div class="card stx-statcard"><div class="card-body"><div class="text-muted small">Categories</div><div class="fs-4 fw-bold">{{ categories }}</div></div></div></div>
        <div class="col-6 col-md-3"><div class="card stx-statcard"><div class="card-body"><div class="text-muted small">Orders</div><div class="fs-4 fw-bold">{{ stats.totalOrders }}</div></div></div></div>
        <div class="col-6 col-md-3"><div class="card stx-statcard"><div class="card-body"><div class="text-muted small">Customers</div><div class="fs-4 fw-bold">{{ stats.totalCustomers }}</div></div></div></div>
        <div class="col-6 col-md-3"><div class="card stx-statcard"><div class="card-body"><div class="text-muted small">Revenue</div><div class="fs-4 fw-bold">{{ formatINR(stats.totalRevenue) }}</div></div></div></div>
        <div class="col-6 col-md-3"><div class="card stx-statcard warn"><div class="card-body"><div class="text-muted small">Low stock</div><div class="fs-4 fw-bold">{{ stats.lowStockProducts }}</div></div></div></div>
        <div class="col-6 col-md-3"><div class="card stx-statcard bad"><div class="card-body"><div class="text-muted small">Out of stock</div><div class="fs-4 fw-bold">{{ stats.outOfStockProducts }}</div></div></div></div>
        <div class="col-6 col-md-3"><div class="card stx-statcard warn"><div class="card-body"><div class="text-muted small">Pending orders</div><div class="fs-4 fw-bold">{{ stats.pendingOrders }}</div></div></div></div>
      </div>
      <h2 class="h5 mt-4">Recent orders</h2>
      <div v-if="recent.length === 0" class="stx-empty p-4 text-center text-muted">No orders yet.</div>
      <ul v-else class="list-group">
        <li v-for="o in recent" :key="o.id" class="list-group-item d-flex justify-content-between flex-wrap gap-2">
          <span>{{ o.orderNumber }} — {{ o.status }} · {{ formatDate(o.orderDate) }}</span>
          <span class="fw-semibold">{{ formatINR(o.total) }}</span>
        </li>
      </ul>
      <div class="d-flex gap-2 mt-3 flex-wrap">
        <RouterLink class="btn stx-btn-primary" to="/admin/products">Manage products</RouterLink>
        <RouterLink class="btn btn-outline-primary" to="/admin/orders">Manage orders</RouterLink>
      </div>
    </template>
  </div>
</template>
