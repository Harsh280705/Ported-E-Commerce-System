<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useUiStore } from '@/stores/ui'
import RegisterForm from '@/components/RegisterForm.vue'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const cart = useCartStore()
const ui = useUiStore()
const loading = ref(false)

async function onSubmit(payload: { email: string; password: string; firstName: string; lastName: string }) {
  loading.value = true
  try {
    await auth.register(payload)
    await cart.mergeAfterLogin().catch(() => undefined)
    await cart.fetch().catch(() => undefined)
    const ret = typeof route.query.returnUrl === 'string' ? route.query.returnUrl : '/'
    router.push(ret)
  } catch {
    ui.notify('Registration failed. Email may already be in use.', 'danger')
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
          <h1 class="h3">Create your account</h1>
          <p class="text-muted small">Your guest cart is kept when you register.</p>
          <RegisterForm :loading="loading" @submit="onSubmit" />
          <p class="mt-3 mb-0">Have an account? <RouterLink to="/login">Login</RouterLink></p>
        </div>
      </div>
    </div>
  </div>
</template>
