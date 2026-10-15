<script setup lang="ts">
</script>

<template>
  <footer class="stx-footer mt-5">
    <div class="stx-shell py-4">
      <div class="row g-4">
        <div class="col-md-4">
          <h6>Yantra Shop</h6>
          <p class="small mb-0">Quality products at honest prices. Browse categories, search the catalog, and check out securely.</p>
        </div>
        <div class="col-md-4">
          <h6>Shop</h6>
          <nav class="d-flex flex-column gap-1" aria-label="Footer shop">
            <RouterLink to="/products">All products</RouterLink>
            <RouterLink to="/cart">Your cart</RouterLink>
            <RouterLink to="/account/orders">Order history</RouterLink>
          </nav>
        </div>
        <div class="col-md-4">
          <h6>Account</h6>
          <nav class="d-flex flex-column gap-1" aria-label="Footer account">
            <RouterLink to="/login">Login</RouterLink>
            <RouterLink to="/register">Register</RouterLink>
            <RouterLink to="/account">Account dashboard</RouterLink>
          </nav>
        </div>
      </div>
      <hr class="my-3" />
      <div class="d-flex flex-column flex-md-row justify-content-between gap-2 small">
        <span>&copy; 2026 Yantra Shop. All rights reserved.</span>
        <span>Prices include taxes as shown at checkout.</span>
      </div>
    </div>
  </footer>
</template>
