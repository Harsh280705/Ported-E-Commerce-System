import { createRouter, createWebHistory, type RouteLocationNormalized } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

export interface AccessCheck {
  allowed: boolean
  redirect?: string
  query?: Record<string, string>
}

export function checkAccess(
  to: Pick<RouteLocationNormalized, 'meta' | 'fullPath'>,
  auth: { isAuthenticated: boolean; isAdmin: boolean }
): AccessCheck {
  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return { allowed: false, redirect: '/login', query: { returnUrl: to.fullPath } }
  }
  if (to.meta.requiresGuest && auth.isAuthenticated) {
    const ret = (to as unknown as { query?: Record<string, string> }).query?.returnUrl
    return { allowed: false, redirect: typeof ret === 'string' ? ret : '/' }
  }
  if (to.meta.requiresAdmin) {
    if (!auth.isAuthenticated) {
      return { allowed: false, redirect: '/login', query: { returnUrl: to.fullPath } }
    }
    if (!auth.isAdmin) {
      return { allowed: false, redirect: '/' }
    }
  }
  return { allowed: true }
}

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'home', component: () => import('@/views/HomeView.vue') },
    { path: '/products', name: 'products', component: () => import('@/views/ProductListView.vue') },
    {
      path: '/products/:id',
      name: 'product-details',
      component: () => import('@/views/ProductDetailsView.vue'),
      props: true
    },
    { path: '/cart', name: 'cart', component: () => import('@/views/CartView.vue') },
    {
      path: '/checkout',
      name: 'checkout',
      component: () => import('@/views/CheckoutView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/LoginView.vue'),
      meta: { requiresGuest: true }
    },
    {
      path: '/register',
      name: 'register',
      component: () => import('@/views/RegisterView.vue'),
      meta: { requiresGuest: true }
    },
    {
      path: '/account',
      name: 'account',
      component: () => import('@/views/AccountDashboardView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/account/orders',
      name: 'order-history',
      component: () => import('@/views/OrderHistoryView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/account/orders/:id',
      name: 'order-details',
      component: () => import('@/views/OrderDetailsView.vue'),
      meta: { requiresAuth: true },
      props: true
    },
    {
      path: '/admin',
      name: 'admin',
      component: () => import('@/views/AdminDashboardView.vue'),
      meta: { requiresAuth: true, requiresAdmin: true }
    },
    {
      path: '/admin/products',
      name: 'admin-products',
      component: () => import('@/views/AdminProductsView.vue'),
      meta: { requiresAuth: true, requiresAdmin: true }
    },
    {
      path: '/admin/orders',
      name: 'admin-orders',
      component: () => import('@/views/AdminOrdersView.vue'),
      meta: { requiresAuth: true, requiresAdmin: true }
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'not-found',
      component: () => import('@/views/NotFoundView.vue')
    }
  ]
})

router.beforeEach((to) => {
  const auth = useAuthStore()
  const result = checkAccess(to, {
    isAuthenticated: auth.isAuthenticated,
    isAdmin: auth.isAdmin
  })
  if (!result.allowed && result.redirect) {
    return { path: result.redirect, query: result.query ?? {} }
  }
  return true
})

export default router
