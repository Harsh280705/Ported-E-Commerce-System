<script setup lang="ts">
import AppHeader from '@/components/AppHeader.vue'
import AppFooter from '@/components/AppFooter.vue'
import CartDrawer from '@/components/CartDrawer.vue'
import NotificationToast from '@/components/NotificationToast.vue'
</script>

<template>
  <div class="d-flex flex-column min-vh-100">
    <AppHeader />
    <main class="stx-shell flex-grow-1 py-4">
      <RouterView />
    </main>
    <AppFooter />
    <CartDrawer />
    <NotificationToast />
  </div>
</template>
