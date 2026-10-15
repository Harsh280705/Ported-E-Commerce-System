<script setup lang="ts">
import { onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useOrdersStore } from '@/stores/orders'
import { useUiStore } from '@/stores/ui'
import { apiClient } from '@/api/client'
import type { AddressDto } from '@/types'
import AddressForm from '@/components/AddressForm.vue'
import { reactive, ref } from 'vue'

const auth = useAuthStore()
const cart = useCartStore()
const orders = useOrdersStore()
const ui = useUiStore()

const profile = reactive({ firstName: '', lastName: '' })
const addresses = ref<AddressDto[]>([])
const editing = ref<AddressDto | null>(null)
const emptyAddress: AddressDto = {
  fullName: '',
  line1: '',
  line2: '',
  city: '',
  state: '',
  postalCode: '',
  country: 'India',
  phone: '',
  isDefault: false
}
const draft = ref<AddressDto>({ ...emptyAddress })

onMounted(async () => {
  if (auth.user) {
    profile.firstName = auth.user.firstName
    profile.lastName = auth.user.lastName
  }
  await Promise.all([
    loadAddresses(),
    orders.fetchHistory().catch(() => undefined),
    cart.fetch().catch(() => undefined)
  ])
})

async function loadAddresses() {
  try {
    const res = await apiClient.get<AddressDto[]>('/account/addresses')
    addresses.value = res.data ?? []
  } catch {
    addresses.value = []
  }
}

async function saveProfile() {
  try {
    await apiClient.put('/account/profile', { ...profile })
    ui.notify('Profile updated', 'success')
    await auth.fetchMe()
  } catch {
    ui.notify('Failed to update profile', 'danger')
  }
}

function startCreate() {
  editing.value = null
  draft.value = { ...emptyAddress }
}

function startEdit(a: AddressDto) {
  editing.value = a
  draft.value = { ...a }
}

async function saveAddress() {
  try {
    if (editing.value?.id) {
      await apiClient.put(`/account/addresses/${editing.value.id}`, draft.value)
    } else {
      await apiClient.post('/account/addresses', draft.value)
    }
    ui.notify('Address saved', 'success')
    editing.value = null
    draft.value = { ...emptyAddress }
    await loadAddresses()
  } catch {
    ui.notify('Failed to save address', 'danger')
  }
}

async function removeAddress(id?: string) {
  if (!id) return
  if (!confirm('Delete this address?')) return
  try {
    await apiClient.delete(`/account/addresses/${id}`)
    ui.notify('Address deleted', 'success')
    await loadAddresses()
  } catch {
    ui.notify('Failed to delete address', 'danger')
  }
}
</script>

<template>
  <div>
    <h1 class="h3 mb-3">Account dashboard</h1>
    <div class="row g-4">
      <div class="col-lg-4">
        <div class="card mb-3">
          <div class="card-header fw-semibold">Profile</div>
          <div class="card-body">
            <p class="text-muted small">{{ auth.user?.email }} · {{ auth.user?.role }}</p>
            <div class="mb-3">
              <label for="profile-first" class="form-label">First name</label>
              <input id="profile-first" v-model="profile.firstName" class="form-control" />
            </div>
            <div class="mb-3">
              <label for="profile-last" class="form-label">Last name</label>
              <input id="profile-last" v-model="profile.lastName" class="form-control" />
            </div>
            <button class="btn stx-btn-primary" type="button" @click="saveProfile">Save profile</button>
          </div>
        </div>
        <div class="card">
          <div class="card-header fw-semibold">At a glance</div>
          <div class="card-body d-flex gap-3">
            <div><div class="fs-5 fw-bold">{{ orders.history.length }}</div><div class="small text-muted">Orders</div></div>
            <div><div class="fs-5 fw-bold">{{ cart.itemCount }}</div><div class="small text-muted">Cart items</div></div>
            <div><div class="fs-5 fw-bold">{{ addresses.length }}</div><div class="small text-muted">Addresses</div></div>
          </div>
        </div>
      </div>
      <div class="col-lg-4">
        <div class="card">
          <div class="card-header d-flex justify-content-between align-items-center">
            <span class="fw-semibold">Addresses</span>
            <button class="btn btn-sm btn-outline-primary" type="button" @click="startCreate">Add</button>
          </div>
          <div class="card-body">
            <div v-if="addresses.length === 0" class="text-muted small mb-2">No saved addresses yet.</div>
            <div v-for="a in addresses" :key="a.id" class="border rounded p-2 mb-2">
              <div class="fw-semibold">{{ a.fullName }} <span v-if="a.isDefault" class="badge bg-success ms-1">Default</span></div>
              <div class="small text-muted">{{ a.line1 }}, {{ a.city }}, {{ a.state }} {{ a.postalCode }}</div>
              <div class="d-flex gap-2 mt-2">
                <button class="btn btn-sm btn-outline-secondary" type="button" @click="startEdit(a)">Edit</button>
                <button class="btn btn-sm btn-outline-danger" type="button" @click="removeAddress(a.id)">Delete</button>
              </div>
            </div>
            <div class="border rounded p-2 mt-2">
              <h2 class="h6">{{ editing ? 'Edit address' : 'New address' }}</h2>
              <AddressForm v-model="draft" prefix="acct" />
              <div class="d-flex gap-2 mt-2">
                <button class="btn btn-primary btn-sm" type="button" @click="saveAddress">Save address</button>
                <button v-if="editing" class="btn btn-link btn-sm" type="button" @click="startCreate">Cancel</button>
              </div>
            </div>
          </div>
        </div>
      </div>
      <div class="col-lg-4">
        <div class="card">
          <div class="card-header d-flex justify-content-between align-items-center">
            <span class="fw-semibold">Recent orders</span>
            <RouterLink class="btn btn-sm btn-link" to="/account/orders">View all</RouterLink>
          </div>
          <div class="card-body">
            <div v-if="orders.history.length === 0" class="text-muted small">No orders yet.</div>
            <ul v-else class="list-unstyled mb-0 d-flex flex-column gap-2">
              <li v-for="o in orders.history.slice(0, 5)" :key="o.id" class="border-bottom pb-2">
                <RouterLink :to="`/account/orders/${o.id}`">{{ o.orderNumber }}</RouterLink>
                <div class="small text-muted">{{ o.status }} · {{ o.itemCount }} items</div>
              </li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
