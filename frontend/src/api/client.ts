import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'

const CART_KEY = 'cart_id'

function newCartId(): string {
  try {
    if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
      return (crypto as Crypto).randomUUID()
    }
  } catch {
    // fall through
  }
  return `guest-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`
}

export function getCartId(): string {
  try {
    let id = localStorage.getItem(CART_KEY)
    if (!id) {
      id = newCartId()
      localStorage.setItem(CART_KEY, id)
    }
    return id
  } catch {
    return newCartId()
  }
}

export function ensureCartId(): string {
  return getCartId()
}

// In-memory access token (never persisted to localStorage per requirements)
let accessTokenMemory = ''

export function setAccessToken(token: string): void {
  accessTokenMemory = token || ''
}

export function getAccessToken(): string {
  return accessTokenMemory
}

export const apiClient = axios.create({
  baseURL: '/api',
  withCredentials: true
})

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  if (accessTokenMemory) {
    config.headers = config.headers ?? ({} as never)
    ;(config.headers as Record<string, string>)['Authorization'] = `Bearer ${accessTokenMemory}`
  }
  try {
    const cartId = getCartId()
    if (cartId) {
      config.headers = config.headers ?? ({} as never)
      ;(config.headers as Record<string, string>)['X-Cart-Id'] = cartId
    }
  } catch {
    // ignore storage errors
  }
  return config
})

interface RetryableConfig extends InternalAxiosRequestConfig {
  _retry?: boolean
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as RetryableConfig | undefined
    const status = error.response?.status
    const url = original?.url ?? ''
    if (status === 401 && original && !original._retry && !url.includes('/auth/refresh')) {
      original._retry = true
      try {
        // Direct axios call (not apiClient) to avoid interceptor recursion
        const refreshRes = await axios.post('/api/auth/refresh', {}, { withCredentials: true })
        const data = refreshRes.data as { accessToken?: string; user?: unknown }
        if (data?.accessToken) {
          setAccessToken(data.accessToken)
          ;(original.headers as Record<string, string>)['Authorization'] =
            `Bearer ${data.accessToken}`
          return apiClient(original)
        }
      } catch {
        // fall through to logout handling
      }
      setAccessToken('')
      try {
        const { useAuthStore } = await import('@/stores/auth')
        const auth = useAuthStore()
        auth.$patch({ user: null, accessToken: '' })
      } catch {
        // store may not be installed in unit tests
      }
      if (typeof window !== 'undefined' && window.location.pathname !== '/login') {
        window.location.href = '/login'
      }
    }
    return Promise.reject(error)
  }
)

export default apiClient
