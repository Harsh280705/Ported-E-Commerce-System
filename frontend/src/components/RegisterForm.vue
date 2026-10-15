<script setup lang="ts">
import { reactive, ref } from 'vue'
import { validateRegisterForm } from '@/utils/validators'

const props = withDefaults(defineProps<{ loading?: boolean }>(), { loading: false })
const emit = defineEmits<{
  (e: 'submit', payload: { email: string; password: string; firstName: string; lastName: string }): void
}>()

const form = reactive({ email: '', password: '', firstName: '', lastName: '' })
const errors = ref<Record<string, string>>({})

function onSubmit() {
  const result = validateRegisterForm({ ...form })
  errors.value = result.errors
  if (result.valid) emit('submit', { ...form })
}
</script>

<template>
  <form data-testid="register-form" novalidate @submit.prevent="onSubmit">
    <div class="row g-3">
      <div class="col-md-6">
        <label for="reg-first" class="form-label">First name</label>
        <input id="reg-first" v-model="form.firstName" class="form-control" :class="{ 'is-invalid': errors.firstName }" autocomplete="given-name" />
        <div v-if="errors.firstName" class="invalid-feedback">{{ errors.firstName }}</div>
      </div>
      <div class="col-md-6">
        <label for="reg-last" class="form-label">Last name</label>
        <input id="reg-last" v-model="form.lastName" class="form-control" :class="{ 'is-invalid': errors.lastName }" autocomplete="family-name" />
        <div v-if="errors.lastName" class="invalid-feedback">{{ errors.lastName }}</div>
      </div>
    </div>
    <div class="mb-3 mt-3">
      <label for="reg-email" class="form-label">Email</label>
      <input id="reg-email" v-model="form.email" type="email" class="form-control" :class="{ 'is-invalid': errors.email }" autocomplete="email" />
      <div v-if="errors.email" class="invalid-feedback">{{ errors.email }}</div>
    </div>
    <div class="mb-3">
      <label for="reg-password" class="form-label">Password</label>
      <input id="reg-password" v-model="form.password" type="password" class="form-control" :class="{ 'is-invalid': errors.password }" autocomplete="new-password" />
      <div v-if="errors.password" class="invalid-feedback">{{ errors.password }}</div>
    </div>
    <button class="btn btn-primary w-100" type="submit" :disabled="props.loading">
      {{ props.loading ? 'Creating account…' : 'Register' }}
    </button>
  </form>
</template>
