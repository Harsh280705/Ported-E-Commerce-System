<script setup lang="ts">
import { reactive, ref, watch, computed } from 'vue'
import type { AdminProductUpsert, CategoryDto } from '@/types'
import { isFallbackImage, PLACEHOLDER_IMAGE } from '@/utils/format'

const props = withDefaults(
  defineProps<{ modelValue: AdminProductUpsert; categories: CategoryDto[]; heading?: string }>(),
  { heading: 'Product' }
)
const emit = defineEmits<{
  (e: 'update:modelValue', v: AdminProductUpsert): void
  (e: 'submit'): void
  (e: 'cancel'): void
}>()

const form = reactive<AdminProductUpsert>({ ...props.modelValue })
const previewBroken = ref(false)
const usePlaceholder = ref(false)

watch(
  () => props.modelValue,
  (v) => {
    Object.assign(form, v)
    previewBroken.value = false
    usePlaceholder.value = false
  },
  { deep: true }
)
watch(
  () => form.imageUrl,
  () => {
    previewBroken.value = false
    usePlaceholder.value = false
  }
)

const previewSrc = computed(() =>
  !usePlaceholder.value && form.imageUrl ? form.imageUrl : PLACEHOLDER_IMAGE
)

function onPreviewError(event: Event) {
  previewBroken.value = true
  usePlaceholder.value = true
  const el = event.target as HTMLImageElement | null
  if (el && el.src !== PLACEHOLDER_IMAGE) el.src = PLACEHOLDER_IMAGE
}

function onPreviewLoad() {
  if (!usePlaceholder.value) previewBroken.value = false
}

const showFallbackNote = computed(() => isFallbackImage(form.imageUrl))

function submit() {
  emit('update:modelValue', { ...form })
  emit('submit')
}
</script>

<template>
  <form data-testid="admin-product-form" @submit.prevent="submit">
    <h2 class="h5 mb-3">{{ heading }}</h2>
    <div class="row g-3">
      <div class="col-md-6">
        <label for="ap-name" class="form-label">Name</label>
        <input id="ap-name" v-model="form.name" class="form-control" required maxlength="200" />
      </div>
      <div class="col-md-6">
        <label for="ap-slug" class="form-label">Slug (blank = auto from name)</label>
        <input id="ap-slug" v-model="form.slug" class="form-control" placeholder="auto-generated if blank" />
      </div>
      <div class="col-12">
        <label for="ap-desc" class="form-label">Description</label>
        <textarea id="ap-desc" v-model="form.description" class="form-control" rows="2" maxlength="2000"></textarea>
      </div>
      <div class="col-md-4">
        <label for="ap-price" class="form-label">Price (&#8377;)</label>
        <input id="ap-price" v-model.number="form.price" type="number" min="0" step="0.01" class="form-control" required />
      </div>
      <div class="col-md-4">
        <label for="ap-compare" class="form-label">Compare-at price (optional)</label>
        <input id="ap-compare" v-model.number="form.compareAtPrice" type="number" min="0" step="0.01" class="form-control" />
      </div>
      <div class="col-md-4">
        <label for="ap-stock" class="form-label">Stock</label>
        <input id="ap-stock" v-model.number="form.stock" type="number" min="0" max="100000" step="1" class="form-control" required />
      </div>
      <div class="col-md-8">
        <label for="ap-image" class="form-label">Image URL (site path like /images/… or https URL)</label>
        <input id="ap-image" v-model="form.imageUrl" class="form-control" maxlength="1000" placeholder="/images/products/example.svg" />
        <div v-if="showFallbackNote" class="form-text text-warning-emphasis">
          No product image set — the storefront will show the generic placeholder.
        </div>
        <div v-else-if="previewBroken" class="form-text text-danger">
          This image URL does not load. Shoppers will see the placeholder instead.
        </div>
        <div v-else class="form-text text-success">Custom image — preview shown.</div>
      </div>
      <div class="col-md-4">
        <label class="form-label">Preview</label>
        <div>
          <img
            :src="previewSrc"
            alt="Product image preview"
            class="stx-preview-img"
            @error="onPreviewError($event)"
            @load="onPreviewLoad"
          />
        </div>
      </div>
      <div class="col-md-6">
        <label for="ap-category" class="form-label">Category</label>
        <select id="ap-category" v-model="form.categoryId" class="form-select" required>
          <option value="">Select…</option>
          <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
        </select>
      </div>
      <div class="col-md-3 form-check ms-2 mt-4">
        <input id="ap-active" v-model="form.isActive" type="checkbox" class="form-check-input" />
        <label for="ap-active" class="form-check-label">Active (visible in store)</label>
      </div>
      <div class="col-md-2 form-check mt-4">
        <input id="ap-featured" v-model="form.isFeatured" type="checkbox" class="form-check-input" />
        <label for="ap-featured" class="form-check-label">Featured</label>
      </div>
    </div>
    <div class="d-flex gap-2 mt-3">
      <button class="btn btn-primary" type="submit">Save</button>
      <button class="btn btn-secondary" type="button" @click="emit('cancel')">Cancel</button>
    </div>
  </form>
</template>
