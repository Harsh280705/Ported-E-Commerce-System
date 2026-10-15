<script setup lang="ts">
import type { ProductCardDto } from '@/types'
import ProductCard from '@/components/ProductCard.vue'
import LoadingSpinner from '@/components/LoadingSpinner.vue'

withDefaults(
  defineProps<{ products: ProductCardDto[]; loading?: boolean }>(),
  { loading: false }
)
</script>

<template>
  <div>
    <div v-if="loading" class="stx-grid" aria-hidden="true">
      <div v-for="n in 8" :key="n" class="stx-skeleton"></div>
    </div>
    <div v-else class="stx-grid" data-testid="product-list">
      <ProductCard v-for="p in products" :key="p.id" :product="p" />
    </div>
    <div v-if="!loading && products.length === 0" class="stx-empty p-5 text-center">
      <h2 class="h5">No products found</h2>
      <p class="text-muted mb-0">Try a different search term or clear your filters.</p>
    </div>
  </div>
</template>
