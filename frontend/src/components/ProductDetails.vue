<script setup lang="ts">
import { ref } from 'vue'
import type { ProductDetailDto } from '@/types'
import { formatINR, onImageError, PLACEHOLDER_IMAGE } from '@/utils/format'
import { useCartStore } from '@/stores/cart'
import { useUiStore } from '@/stores/ui'

const props = defineProps<{ product: ProductDetailDto }>()
const cart = useCartStore()
const ui = useUiStore()
const qty = ref(1)
const adding = ref(false)

const maxQty = () => Math.min(Math.max(props.product.stock, 1), 99)

async function addToCart() {
  const q = Math.floor(Number(qty.value) || 0)
  if (q < 1 || q > maxQty()) {
    ui.notify(`Choose a quantity between 1 and ${maxQty()}.`, 'warning')
    return
  }
  adding.value = true
  try {
    await cart.add(props.product.id, q)
    ui.notify(`Added ${q} × "${props.product.name}" to cart`, 'success')
    ui.setMiniCartOpen(true)
  } catch {
    ui.notify('Could not add to cart. The item may be out of stock.', 'danger')
  } finally {
    adding.value = false
  }
}
</script>

<template>
  <div class="row g-4" data-testid="product-details">
    <div class="col-md-6">
      <div class="stx-detail-img">
        <img
          :src="product.imageUrl || PLACEHOLDER_IMAGE"
          :alt="product.name"
          @error="onImageError"
        />
      </div>
    </div>
    <div class="col-md-6">
      <h1 class="h3">{{ product.name }}</h1>
      <p class="text-muted">{{ product.categoryName }} · SKU {{ product.id }}</p>
      <p>{{ product.description }}</p>
      <div class="d-flex align-items-center gap-2 mb-2">
        <strong class="fs-3">{{ formatINR(product.price) }}</strong>
        <small v-if="product.compareAtPrice" class="text-muted text-decoration-line-through fs-6">
          {{ formatINR(product.compareAtPrice) }}
        </small>
        <span v-if="product.discountPct" class="badge bg-success">{{ product.discountPct }}% off</span>
      </div>
      <p class="text-muted small mb-3">Tax and shipping calculated at checkout.</p>
      <p>
        <span v-if="product.inStock" class="badge bg-success">In stock ({{ product.stock }} available)</span>
        <span v-else class="badge bg-danger">Out of stock</span>
      </p>
      <div class="d-flex gap-2 align-items-center flex-wrap">
        <label for="pd-qty" class="form-label mb-0">Qty</label>
        <input
          id="pd-qty"
          v-model.number="qty"
          type="number"
          min="1"
          :max="maxQty()"
          class="form-control"
          style="max-width: 100px"
        />
        <button
          class="btn stx-btn-accent"
          :disabled="!product.inStock || adding"
          type="button"
          @click="addToCart"
        >
          {{ adding ? 'Adding…' : 'Add to cart' }}
        </button>
        <RouterLink class="btn btn-outline-secondary" to="/cart">View cart</RouterLink>
      </div>
    </div>
  </div>
</template>
