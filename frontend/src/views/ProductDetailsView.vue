<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { apiClient } from '@/api/client'
import type { ProductDetailDto } from '@/types'
import ProductDetails from '@/components/ProductDetails.vue'
import LoadingSpinner from '@/components/LoadingSpinner.vue'

const route = useRoute()
const id = route.params.id as string
const product = ref<ProductDetailDto | null>(null)
const loading = ref(false)
const error = ref('')

onMounted(async () => {
  loading.value = true
  try {
    const res = await apiClient.get<ProductDetailDto>(`/products/${id}`)
    product.value = res.data
  } catch {
    error.value = 'Product not found. It may have been removed or deactivated.'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div>
    <nav class="stx-crumb mb-3" aria-label="Breadcrumb">
      <RouterLink to="/">Home</RouterLink> &rsaquo;
      <RouterLink to="/products">Products</RouterLink>
      <template v-if="product"> &rsaquo; {{ product.name }}</template>
    </nav>
    <LoadingSpinner v-if="loading" label="Loading product…" />
    <div v-else-if="error" class="stx-empty p-5 text-center">
      <p class="alert alert-danger d-inline-block">{{ error }}</p>
      <div><RouterLink class="btn stx-btn-primary mt-2" to="/products">Back to catalog</RouterLink></div>
    </div>
    <ProductDetails v-else-if="product" :product="product" />
  </div>
</template>
