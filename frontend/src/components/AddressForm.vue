<script setup lang="ts">
import type { AddressDto } from '@/types'

const props = withDefaults(defineProps<{ modelValue: AddressDto; prefix?: string }>(), {
  prefix: 'addr'
})
const emit = defineEmits<{ (e: 'update:modelValue', v: AddressDto): void }>()

function update<K extends keyof AddressDto>(key: K, value: AddressDto[K]) {
  emit('update:modelValue', { ...props.modelValue, [key]: value })
}
</script>

<template>
  <div class="row g-3">
    <div class="col-12">
      <label :for="`${prefix}-fullName`" class="form-label">Full name</label>
      <input
        :id="`${prefix}-fullName`"
        :value="modelValue.fullName"
        class="form-control"
        autocomplete="name"
        @input="update('fullName', ($event.target as HTMLInputElement).value)"
      />
    </div>
    <div class="col-12">
      <label :for="`${prefix}-line1`" class="form-label">Address line 1</label>
      <input
        :id="`${prefix}-line1`"
        :value="modelValue.line1"
        class="form-control"
        autocomplete="street-address"
        @input="update('line1', ($event.target as HTMLInputElement).value)"
      />
    </div>
    <div class="col-12">
      <label :for="`${prefix}-line2`" class="form-label">Address line 2 (optional)</label>
      <input
        :id="`${prefix}-line2`"
        :value="modelValue.line2 ?? ''"
        class="form-control"
        @input="update('line2', ($event.target as HTMLInputElement).value)"
      />
    </div>
    <div class="col-md-6">
      <label :for="`${prefix}-city`" class="form-label">City</label>
      <input :id="`${prefix}-city`" :value="modelValue.city" class="form-control" @input="update('city', ($event.target as HTMLInputElement).value)" />
    </div>
    <div class="col-md-6">
      <label :for="`${prefix}-state`" class="form-label">State</label>
      <input :id="`${prefix}-state`" :value="modelValue.state" class="form-control" @input="update('state', ($event.target as HTMLInputElement).value)" />
    </div>
    <div class="col-md-4">
      <label :for="`${prefix}-postal`" class="form-label">Postal code</label>
      <input :id="`${prefix}-postal`" :value="modelValue.postalCode" class="form-control" @input="update('postalCode', ($event.target as HTMLInputElement).value)" />
    </div>
    <div class="col-md-4">
      <label :for="`${prefix}-country`" class="form-label">Country</label>
      <input :id="`${prefix}-country`" :value="modelValue.country" class="form-control" @input="update('country', ($event.target as HTMLInputElement).value)" />
    </div>
    <div class="col-md-4">
      <label :for="`${prefix}-phone`" class="form-label">Phone (optional)</label>
      <input :id="`${prefix}-phone`" :value="modelValue.phone ?? ''" class="form-control" @input="update('phone', ($event.target as HTMLInputElement).value)" />
    </div>
  </div>
</template>
