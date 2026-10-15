import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from '@/App.vue'
import router from '@/router'
import 'bootstrap/dist/css/bootstrap.min.css'
import '@/assets/store.css'
import { useAuthStore } from '@/stores/auth'

const app = createApp(App)
const pinia = createPinia()
app.use(pinia)

// Restore the session (HttpOnly refresh cookie) BEFORE installing the router,
// so the initial navigation's auth guards see the restored state instead of
// racing it. Without this, a full-page load of a protected route (or a
// Playwright page.goto) bounces to /login even with a valid session.
// Note: plain promise chaining (no top-level await) to honor the build target.
const auth = useAuthStore(pinia)
auth
  .init()
  .catch(() => undefined)
  .finally(() => {
    app.use(router)
    app.mount('#app')
  })
