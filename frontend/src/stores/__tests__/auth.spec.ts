import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'

vi.mock('@/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn()
  },
  setAccessToken: vi.fn(),
  getAccessToken: vi.fn(() => ''),
  getCartId: vi.fn(() => 'test-cart-id'),
  ensureCartId: vi.fn(() => 'test-cart-id')
}))

import { apiClient } from '@/api/client'
import { useAuthStore } from '@/stores/auth'

const mockedPost = apiClient.post as unknown as ReturnType<typeof vi.fn>
const mockedGet = apiClient.get as unknown as ReturnType<typeof vi.fn>

describe('auth store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('login stores user and token', async () => {
    const user = { id: '1', email: 'a@b.com', firstName: 'A', lastName: 'B', role: 'Customer' }
    mockedPost.mockResolvedValueOnce({ data: { accessToken: 'tok123', expiresAt: 'x', user } })
    const store = useAuthStore()
    const result = await store.login('a@b.com', 'secret123')
    expect(result).toEqual(user)
    expect(store.user).toEqual(user)
    expect(store.accessToken).toBe('tok123')
    expect(store.isAuthenticated).toBe(true)
    expect(store.isAdmin).toBe(false)
    expect(mockedPost).toHaveBeenCalledWith('/auth/login', { email: 'a@b.com', password: 'secret123' })
  })

  it('isAdmin is true for Admin role', async () => {
    const user = { id: '2', email: 'admin@yantra.local', firstName: 'Ad', lastName: 'Min', role: 'Admin' }
    mockedPost.mockResolvedValueOnce({ data: { accessToken: 'adm', expiresAt: 'x', user } })
    const store = useAuthStore()
    await store.login('admin@yantra.local', 'Admin!123')
    expect(store.isAdmin).toBe(true)
  })

  it('register posts to /auth/register', async () => {
    const user = { id: '3', email: 'n@n.com', firstName: 'N', lastName: 'E', role: 'Customer' }
    mockedPost.mockResolvedValueOnce({ data: { accessToken: 't', expiresAt: 'x', user } })
    const store = useAuthStore()
    await store.register({ email: 'n@n.com', password: 'secret123', firstName: 'N', lastName: 'E' })
    expect(mockedPost).toHaveBeenCalledWith('/auth/register', {
      email: 'n@n.com',
      password: 'secret123',
      firstName: 'N',
      lastName: 'E'
    })
  })

  it('logout clears state even when API fails', async () => {
    const store = useAuthStore()
    store.user = { id: '1', email: 'a@b.com', firstName: 'A', lastName: 'B', role: 'Customer' }
    store.accessToken = 'tok'
    mockedPost.mockRejectedValueOnce(new Error('network'))
    await store.logout()
    expect(store.user).toBeNull()
    expect(store.accessToken).toBe('')
  })

  it('refresh failure clears auth', async () => {
    mockedPost.mockRejectedValueOnce(new Error('401'))
    const store = useAuthStore()
    const result = await store.refresh()
    expect(result).toBeNull()
    expect(store.isAuthenticated).toBe(false)
  })

  it('fetchMe sets user', async () => {
    const user = { id: '9', email: 'me@x.com', firstName: 'M', lastName: 'E', role: 'Customer' }
    mockedGet.mockResolvedValueOnce({ data: user })
    const store = useAuthStore()
    store.accessToken = 'tok'
    const result = await store.fetchMe()
    expect(result).toEqual(user)
    expect(store.user).toEqual(user)
  })
})
