<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useUiStore } from '@/stores/ui'
import LoginForm from '@/components/LoginForm.vue'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const cart = useCartStore()
const ui = useUiStore()
const loading = ref(false)

async function onSubmit(payload: { email: string; password: string }) {
  loading.value = true
  try {
    await auth.login(payload.email, payload.password)
    await cart.mergeAfterLogin().catch(() => undefined)
    await cart.fetch().catch(() => undefined)
    const ret = typeof route.query.returnUrl === 'string' ? route.query.returnUrl : '/'
    router.push(ret)
  } catch {
    ui.notify('Login failed. Check your credentials.', 'danger')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="row justify-content-center">
    <div class="col-md-6 col-lg-5">
      <div class="card">
        <div class="card-body p-4">
          <h1 class="h3">Login</h1>
          <p class="text-muted small">Welcome back. Sign in to check out and track orders.</p>
          <LoginForm :loading="loading" @submit="onSubmit" />
          <p class="mt-3 mb-0">No account? <RouterLink to="/register">Register</RouterLink></p>
        </div>
      </div>
    </div>
  </div>
</template>
