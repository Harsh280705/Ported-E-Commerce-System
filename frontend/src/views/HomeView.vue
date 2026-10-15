<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useCatalogStore } from '@/stores/catalog'
import { apiClient } from '@/api/client'
import type { ProductCardDto } from '@/types'
import ProductList from '@/components/ProductList.vue'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import { onImageError } from '@/utils/format'
import { resolveCategoryImageUrl } from '@/utils/categoryImages'

const catalog = useCatalogStore()
const featured = ref<ProductCardDto[]>([])
const newest = ref<ProductCardDto[]>([])
const totalProducts = ref(0)
const loading = ref(false)

onMounted(async () => {
  loading.value = true
  try {
    await catalog.fetchCategories()
    const [feat, fresh, count] = await Promise.all([
      apiClient.get<{ items: ProductCardDto[] }>('/products', { params: { page: 1, pageSize: 8, sort: 'recommended' } }),
      apiClient.get<{ items: ProductCardDto[] }>('/products', { params: { page: 1, pageSize: 8, sort: 'newest' } }),
      apiClient.get<{ totalCount: number }>('/products', { params: { page: 1, pageSize: 1 } })
    ])
    featured.value = feat.data.items ?? []
    newest.value = fresh.data.items ?? []
    totalProducts.value = count.data.totalCount ?? 0
  } catch {
    featured.value = []
    newest.value = []
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div>
    <section class="stx-hero p-4 p-md-5 mb-4" aria-label="Hero">
      <div class="row align-items-center g-4">
        <div class="col-lg-7">
          <h1 class="display-5">Everything you need, in one shop</h1>
          <p class="fs-5 mb-3">Browse {{ totalProducts.toLocaleString('en-IN') }} products across {{ catalog.categories.length }} categories — fashion, electronics, home essentials and more.</p>
          <div class="d-flex gap-2 flex-wrap">
            <RouterLink class="btn btn-accent btn-lg" to="/products">Shop all products</RouterLink>
            <RouterLink class="btn btn-outline-light btn-lg" to="/cart">View cart</RouterLink>
          </div>
        </div>
        <div class="col-lg-5">
          <div class="row g-2 text-center">
            <div class="col-4"><div class="stx-stat p-3"><div class="fs-4 fw-bold">{{ totalProducts.toLocaleString('en-IN') }}</div><div class="small">Products</div></div></div>
            <div class="col-4"><div class="stx-stat p-3"><div class="fs-4 fw-bold">{{ catalog.categories.length }}</div><div class="small">Categories</div></div></div>
            <div class="col-4"><div class="stx-stat p-3"><div class="fs-4 fw-bold">8% tax</div><div class="small">No hidden fees</div></div></div>
          </div>
        </div>
      </div>
    </section>

    <section aria-label="Categories" class="mb-4">
      <div class="d-flex justify-content-between align-items-center mb-2">
        <h2 class="h4 mb-0">Shop by category</h2>
        <RouterLink to="/products" class="small">View all</RouterLink>
      </div>
      <div class="row row-cols-2 row-cols-md-4 row-cols-xl-7 g-3">
        <div v-for="c in catalog.categories" :key="c.id" class="col">
          <RouterLink
            :to="{ path: '/products', query: { categoryId: c.id } }"
            class="stx-catcard text-decoration-none text-dark d-block"
          >
            <img
              :src="resolveCategoryImageUrl(c)"
              :alt="c.name"
              loading="lazy"
              @error="onImageError"
            />
            <div class="p-2">
              <div class="fw-semibold">{{ c.name }}</div>
              <div class="small text-muted">{{ c.productCount ?? 0 }} products</div>
            </div>
          </RouterLink>
        </div>
      </div>
    </section>

    <section aria-label="Featured products" class="mb-4">
      <div class="d-flex justify-content-between align-items-center mb-2">
        <h2 class="h4 mb-0">Featured products</h2>
        <RouterLink to="/products" class="small">View all</RouterLink>
      </div>
      <LoadingSpinner v-if="loading" />
      <ProductList v-else :products="featured" />
    </section>

    <section aria-label="New arrivals" class="mb-2">
      <div class="d-flex justify-content-between align-items-center mb-2">
        <h2 class="h4 mb-0">New arrivals</h2>
        <RouterLink :to="{ path: '/products', query: { sort: 'newest' } }" class="small">View all</RouterLink>
      </div>
      <LoadingSpinner v-if="loading" />
      <ProductList v-else :products="newest" />
    </section>
  </div>
</template>
