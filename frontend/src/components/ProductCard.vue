<script setup lang="ts">
import type { ProductCardDto } from '@/types'
import { formatINR, onImageError, PLACEHOLDER_IMAGE } from '@/utils/format'
import { useCartStore } from '@/stores/cart'
import { useUiStore } from '@/stores/ui'
import { ref } from 'vue'

const props = defineProps<{ product: ProductCardDto }>()
const cart = useCartStore()
const ui = useUiStore()
const adding = ref(false)

async function addToCart() {
  if (adding.value) return
  adding.value = true
  try {
    await cart.add(props.product.id, 1)
    ui.notify(`Added "${props.product.name}" to cart`, 'success')
    ui.setMiniCartOpen(true)
  } catch {
    ui.notify('Could not add to cart. Check stock and try again.', 'danger')
  } finally {
    adding.value = false
  }
}
</script>

<template>
  <div class="stx-card" data-testid="product-card">
    <RouterLink
      :to="`/products/${product.id}`"
      :aria-label="`View ${product.name}`"
      class="stx-card-img"
    >
      <img
        :src="product.imageUrl || PLACEHOLDER_IMAGE"
        :alt="product.name"
        loading="lazy"
        width="600"
        height="600"
        @error="onImageError"
      />
    </RouterLink>
    <div class="stx-card-body">
      <div class="stx-card-cat">{{ product.categoryName }}</div>
      <div class="stx-card-name">
        <RouterLink :to="`/products/${product.id}`">
          {{ product.name }}
        </RouterLink>
      </div>
      <div class="d-flex align-items-center gap-2 flex-wrap">
        <span class="stx-price">{{ formatINR(product.price) }}</span>
        <small v-if="product.compareAtPrice" class="stx-was">
          {{ formatINR(product.compareAtPrice) }}
        </small>
        <span v-if="product.discountPct" class="badge bg-success">{{ product.discountPct }}% off</span>
      </div>
      <div class="d-flex align-items-center gap-2">
        <span v-if="product.inStock" class="badge bg-success-subtle text-success border">In stock</span>
        <span v-else class="badge bg-danger">Out of stock</span>
        <small v-if="product.inStock && product.stock <= 10" class="text-warning-emphasis">Only {{ product.stock }} left</small>
      </div>
      <div class="stx-card-actions">
        <button
          class="btn btn-sm stx-btn-accent flex-grow-1"
          type="button"
          :disabled="!product.inStock || adding"
          @click="addToCart"
        >
          {{ adding ? 'Adding…' : 'Add to cart' }}
        </button>
        <RouterLink class="btn btn-sm btn-outline-secondary" :to="`/products/${product.id}`">Details</RouterLink>
      </div>
    </div>
  </div>
</template>
