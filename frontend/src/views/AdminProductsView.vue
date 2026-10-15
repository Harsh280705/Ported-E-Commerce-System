<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import { apiClient } from '@/api/client'
import type { AdminProductUpsert, CategoryDto, ProductCardDto } from '@/types'
import AdminProductForm from '@/components/AdminProductForm.vue'
import Pagination from '@/components/Pagination.vue'
import { useUiStore } from '@/stores/ui'
import { formatINR, isFallbackImage } from '@/utils/format'

interface AdminPage {
  items: ProductCardDto[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

const ui = useUiStore()
const products = ref<ProductCardDto[]>([])
const categories = ref<CategoryDto[]>([])
const page = ref(1)
const totalPages = ref(0)
const totalCount = ref(0)
const pageSize = 12
const search = ref('')
const categoryId = ref('')
const sort = ref('newest')
const loading = ref(false)
const showForm = ref(false)
const editingId = ref<string | null>(null)
const confirmDelete = ref<ProductCardDto | null>(null)
let debounce: ReturnType<typeof setTimeout> | null = null

const draft = ref<AdminProductUpsert>({
  name: '',
  slug: '',
  description: '',
  price: 0,
  compareAtPrice: null,
  stock: 0,
  imageUrl: '',
  categoryId: '',
  isActive: true,
  isFeatured: false
})

async function load(p = 1) {
  loading.value = true
  try {
    const params: Record<string, string | number> = { page: p, pageSize }
    if (search.value) params.search = search.value
    if (categoryId.value) params.categoryId = categoryId.value
    if (sort.value) params.sort = sort.value
    const res = await apiClient.get<AdminPage>('/admin/products', { params })
    products.value = res.data.items ?? []
    page.value = res.data.page ?? p
    totalPages.value = res.data.totalPages ?? 0
    totalCount.value = res.data.totalCount ?? 0
  } catch {
    ui.notify('Failed to load products', 'danger')
  } finally {
    loading.value = false
  }
}

async function loadCategories() {
  try {
    const res = await apiClient.get<CategoryDto[]>('/admin/categories')
    categories.value = res.data ?? []
  } catch {
    categories.value = []
  }
}

function onSearchInput() {
  if (debounce) clearTimeout(debounce)
  debounce = setTimeout(() => load(1), 350)
}

function resetFilters() {
  search.value = ''
  categoryId.value = ''
  sort.value = 'newest'
  load(1)
}

onMounted(() => {
  loadCategories().catch(() => undefined)
  load(1).catch(() => undefined)
})

onUnmounted(() => {
  if (debounce) clearTimeout(debounce)
})

function startCreate() {
  editingId.value = null
  draft.value = {
    name: '',
    slug: '',
    description: '',
    price: 0,
    compareAtPrice: null,
    stock: 0,
    imageUrl: '',
    categoryId: categories.value[0]?.id ?? '',
    isActive: true,
    isFeatured: false
  }
  showForm.value = true
}

async function startEdit(p: ProductCardDto) {
  editingId.value = p.id
  type Detail = ProductCardDto & { isActive?: boolean; isFeatured?: boolean }
  let detail: Detail = p
  try {
    const res = await apiClient.get<Detail>(`/products/${p.id}`)
    detail = res.data
  } catch {
    // fall back to the row data (admin list may include inactive items the public endpoint hides)
  }
  draft.value = {
    name: detail.name,
    slug: detail.slug,
    description: detail.description ?? '',
    price: detail.price,
    compareAtPrice: detail.compareAtPrice ?? null,
    stock: detail.stock,
    imageUrl: (detail.imageUrl ?? '').includes('placeholder') || (detail.imageUrl ?? '').includes('picsum') ? '' : (detail.imageUrl ?? ''),
    categoryId: detail.categoryId,
    isActive: detail.isActive ?? true,
    isFeatured: detail.isFeatured ?? false
  }
  showForm.value = true
}

async function save() {
  try {
    if (editingId.value) {
      await apiClient.put(`/admin/products/${editingId.value}`, draft.value)
    } else {
      await apiClient.post('/admin/products', draft.value)
    }
    ui.notify('Product saved', 'success')
    showForm.value = false
    await load(editingId.value ? page.value : 1)
  } catch {
    ui.notify('Failed to save product. Check validation and try again.', 'danger')
  }
}

async function remove() {
  if (!confirmDelete.value) return
  try {
    await apiClient.delete(`/admin/products/${confirmDelete.value.id}`)
    ui.notify('Product deleted', 'success')
    confirmDelete.value = null
    await load(page.value)
  } catch {
    ui.notify('Cannot delete: product has orders or sits in a cart. Deactivate it instead.', 'danger')
  }
}

function categoryName(id: string): string {
  return categories.value.find((c) => c.id === id)?.name ?? ''
}
</script>

<template>
  <div>
    <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
      <div>
        <h1 class="h3 mb-0">Product catalog</h1>
        <p class="text-muted small mb-0" data-testid="admin-count">{{ totalCount }} products in database</p>
      </div>
      <button class="btn stx-btn-accent" type="button" @click="startCreate">Create product</button>
    </div>

    <div class="stx-toolbar p-3 mb-3">
      <div class="row g-3">
        <div class="col-md-4">
          <label for="ap-search" class="form-label">Search</label>
          <input id="ap-search" v-model="search" class="form-control" placeholder="Name, description or SKU…" @input="onSearchInput" />
        </div>
        <div class="col-md-4">
          <label for="ap-category" class="form-label">Category</label>
          <select id="ap-category" v-model="categoryId" class="form-select" @change="load(1)">
            <option value="">All categories</option>
            <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>
        <div class="col-md-3">
          <label for="ap-sort" class="form-label">Sort</label>
          <select id="ap-sort" v-model="sort" class="form-select" @change="load(1)">
            <option value="newest">Newest</option>
            <option value="name_asc">Name A–Z</option>
            <option value="name_desc">Name Z–A</option>
            <option value="price_asc">Price low to high</option>
            <option value="price_desc">Price high to low</option>
          </select>
        </div>
        <div class="col-md-1 d-flex align-items-end">
          <button class="btn btn-outline-secondary w-100" type="button" @click="resetFilters">Reset</button>
        </div>
      </div>
    </div>

    <AdminProductForm
      v-if="showForm"
      v-model="draft"
      :categories="categories"
      :heading="editingId ? 'Edit product' : 'Create product'"
      class="my-3 border rounded p-3 bg-white"
      @submit="save"
      @cancel="showForm = false"
    />

    <div v-if="loading" class="text-muted">Loading products…</div>
    <div v-else-if="products.length === 0" class="stx-empty p-5 text-center">
      <h2 class="h5">No products match</h2>
      <p class="text-muted mb-0">Adjust the search or reset the filters.</p>
    </div>
    <div v-else class="table-responsive">
      <table class="table table-striped stx-table" data-testid="admin-products-table">
        <thead><tr><th scope="col">Image</th><th scope="col">Name</th><th scope="col">Category</th><th scope="col">Price</th><th scope="col">Stock</th><th scope="col">Status</th><th scope="col">Actions</th></tr></thead>
        <tbody>
          <tr v-for="p in products" :key="p.id">
            <td><img :src="p.imageUrl || '/placeholder.svg'" :alt="p.name" class="stx-thumb" loading="lazy" /></td>
            <td>{{ p.name }}<div class="text-muted small">{{ p.slug }}</div></td>
            <td>{{ p.categoryName || categoryName(p.categoryId) }}</td>
            <td>{{ formatINR(p.price) }}</td>
            <td>{{ p.stock }}</td>
            <td>
              <span v-if="isFallbackImage(p.imageUrl)" class="badge bg-warning text-dark">Fallback image</span>
              <span v-else class="badge bg-success-subtle text-success border">Custom image</span>
            </td>
            <td class="d-flex gap-2">
              <button class="btn btn-sm btn-outline-secondary" type="button" @click="startEdit(p)">Edit</button>
              <button class="btn btn-sm btn-outline-danger" type="button" @click="confirmDelete = p">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <Pagination
      v-if="!loading && totalPages > 1"
      :page="page"
      :total-pages="totalPages"
      class="mt-3"
      @change="load($event)"
    />

    <div v-if="confirmDelete" class="modal d-block" tabindex="-1" role="dialog" aria-label="Confirm delete">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header"><h2 class="h5 mb-0">Delete product?</h2></div>
          <div class="modal-body">
            <p>Delete <strong>{{ confirmDelete.name }}</strong> permanently? Products with order history cannot be deleted — deactivate them instead.</p>
          </div>
          <div class="modal-footer">
            <button class="btn btn-secondary" type="button" @click="confirmDelete = null">Cancel</button>
            <button class="btn btn-danger" type="button" @click="remove">Delete</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
