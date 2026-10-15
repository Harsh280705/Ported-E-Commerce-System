<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useCatalogStore } from '@/stores/catalog'
import { useUiStore } from '@/stores/ui'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()
const cart = useCartStore()
const catalog = useCatalogStore()
const ui = useUiStore()

const q = ref('')

onMounted(async () => {
  try {
    await catalog.fetchCategories()
  } catch {
    // ignore — nav still renders
  }
  try {
    await cart.fetch()
  } catch {
    // ignore — guest cart starts empty
  }
})

const count = computed(() => cart.itemCount)
// Active navigation is derived from the current route (name + query), not
// from a bare query check: the home route '/' carries no categoryId, so a
// query-only check wrongly kept "All products" highlighted on the landing
// page. Route names come from the Vue Router configuration ('home' | 'products').
const isProductsRoute = computed(() => route.name === 'products')
const activeCat = computed(() =>
  isProductsRoute.value ? String(route.query.categoryId ?? '') : ''
)
const isAllProductsActive = computed(() => isProductsRoute.value && !activeCat.value)
const isHomeRoute = computed(() => route.name === 'home')

function isCategoryActive(id: string | number): boolean {
  return isProductsRoute.value && activeCat.value !== '' && activeCat.value === String(id)
}

function search() {
  router.push({ path: '/products', query: { search: q.value || undefined } })
}

async function doLogout() {
  await auth.logout()
  await cart.fetch().catch(() => undefined)
  router.push('/')
}
</script>

<template>
  <div class="stx-topbar d-none d-md-block">
    <div class="stx-shell d-flex justify-content-between py-1">
      <span>Free standard shipping on orders over &#8377;75 &middot; Secure checkout</span>
      <span>
        <template v-if="auth.isAuthenticated">Signed in as {{ auth.user?.email }}</template>
        <template v-else>Welcome to Yantra Shop</template>
      </span>
    </div>
  </div>
  <header class="stx-header">
    <div class="stx-shell d-flex align-items-center gap-3 py-2">
      <RouterLink class="stx-brand" to="/" :aria-current="isHomeRoute ? 'page' : undefined">Yantra&nbsp;Shop</RouterLink>
      <form class="d-none d-md-flex flex-grow-1 stx-search" role="search" @submit.prevent="search">
        <label for="site-search" class="visually-hidden">Search products</label>
        <input
          id="site-search"
          v-model="q"
          class="form-control"
          type="search"
          placeholder="Search products, brands and categories"
          aria-label="Search products"
        />
        <button class="btn" type="submit">Search</button>
      </form>
      <div class="d-flex align-items-center gap-2 ms-auto">
        <template v-if="auth.isAdmin">
          <RouterLink class="btn btn-sm btn-outline-light d-none d-sm-inline" to="/admin">Admin</RouterLink>
        </template>
        <template v-if="auth.isAuthenticated">
          <RouterLink class="btn btn-sm btn-outline-light d-none d-sm-inline" to="/account">Account</RouterLink>
          <button class="btn btn-sm btn-outline-light d-none d-sm-inline" type="button" @click="doLogout">Logout</button>
        </template>
        <template v-else>
          <RouterLink class="btn btn-sm btn-outline-light d-none d-sm-inline" to="/login">Login</RouterLink>
          <RouterLink class="btn btn-sm btn-warning d-none d-sm-inline" to="/register">Register</RouterLink>
        </template>
        <button
          class="btn btn-sm btn-light stx-cartbtn"
          type="button"
          aria-label="Open mini cart"
          @click="ui.setMiniCartOpen(true)"
        >
          Cart
          <span
            v-if="count > 0"
            class="badge rounded-pill bg-danger stx-count"
            data-testid="cart-count"
          >
            {{ count }}
          </span>
        </button>
        <button
          class="btn btn-sm btn-outline-light d-lg-none"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#mobileNav"
          aria-controls="mobileNav"
          aria-expanded="false"
          aria-label="Toggle navigation"
        >
          Menu
        </button>
      </div>
    </div>
    <div class="d-md-none stx-shell pb-2">
      <form class="d-flex stx-search" role="search" @submit.prevent="search">
        <label for="site-search-m" class="visually-hidden">Search products</label>
        <input
          id="site-search-m"
          v-model="q"
          class="form-control"
          type="search"
          placeholder="Search products"
          aria-label="Search products"
        />
        <button class="btn" type="submit">Go</button>
      </form>
    </div>
    <nav class="stx-catnav d-none d-lg-block" aria-label="Categories">
      <div class="stx-shell d-flex gap-1 py-1 overflow-auto">
        <RouterLink to="/products" :class="{ active: isAllProductsActive }">All products</RouterLink>
        <RouterLink
          v-for="c in catalog.categories"
          :key="c.id"
          :to="{ path: '/products', query: { categoryId: c.id } }"
          :class="{ active: isCategoryActive(c.id) }"
        >
          {{ c.name }}
        </RouterLink>
      </div>
    </nav>
    <div id="mobileNav" class="collapse d-lg-none stx-catnav">
      <div class="stx-shell d-flex flex-column py-2">
        <RouterLink to="/">Home</RouterLink>
        <RouterLink to="/products">All products</RouterLink>
        <RouterLink
          v-for="c in catalog.categories"
          :key="c.id"
          :to="{ path: '/products', query: { categoryId: c.id } }"
        >
          {{ c.name }}
        </RouterLink>
        <RouterLink to="/cart">Cart</RouterLink>
        <RouterLink to="/account">Account</RouterLink>
        <template v-if="auth.isAuthenticated">
          <RouterLink to="/account/orders">Orders</RouterLink>
          <a href="#" @click.prevent="doLogout">Logout</a>
        </template>
        <template v-else>
          <RouterLink to="/login">Login</RouterLink>
          <RouterLink to="/register">Register</RouterLink>
        </template>
      </div>
    </div>
  </header>
</template>
