<script setup lang="ts">
import { reactive, ref } from 'vue'
import { validateLoginForm } from '@/utils/validators'

const props = withDefaults(defineProps<{ loading?: boolean }>(), { loading: false })
const emit = defineEmits<{ (e: 'submit', payload: { email: string; password: string }): void }>()

const form = reactive({ email: '', password: '' })
const errors = ref<Record<string, string>>({})

function onSubmit() {
  const result = validateLoginForm(form.email, form.password)
  errors.value = result.errors
  if (result.valid) emit('submit', { ...form })
}
</script>

<template>
  <form data-testid="login-form" novalidate @submit.prevent="onSubmit">
    <div class="mb-3">
      <label for="login-email" class="form-label">Email</label>
      <input
        id="login-email"
        v-model="form.email"
        type="email"
        class="form-control"
        :class="{ 'is-invalid': errors.email }"
        autocomplete="email"
      />
      <div v-if="errors.email" class="invalid-feedback">{{ errors.email }}</div>
    </div>
    <div class="mb-3">
      <label for="login-password" class="form-label">Password</label>
      <input
        id="login-password"
        v-model="form.password"
        type="password"
        class="form-control"
        :class="{ 'is-invalid': errors.password }"
        autocomplete="current-password"
      />
      <div v-if="errors.password" class="invalid-feedback">{{ errors.password }}</div>
    </div>
    <button class="btn btn-primary w-100" type="submit" :disabled="props.loading">
      {{ props.loading ? 'Signing in…' : 'Login' }}
    </button>
  </form>
</template>
