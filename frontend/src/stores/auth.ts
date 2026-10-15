import { defineStore } from 'pinia'
import { apiClient, setAccessToken } from '@/api/client'
import type { AuthResponse, UserDto } from '@/types'

interface AuthState {
  user: UserDto | null
  accessToken: string
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    user: null,
    accessToken: ''
  }),
  getters: {
    isAuthenticated: (s): boolean => !!s.accessToken && !!s.user,
    isAdmin: (s): boolean => s.user?.role === 'Admin'
  },
  actions: {
    applyAuth(data: AuthResponse) {
      this.user = data.user
      this.accessToken = data.accessToken
      setAccessToken(data.accessToken)
    },
    clearAuth() {
      this.user = null
      this.accessToken = ''
      setAccessToken('')
    },
    async login(email: string, password: string): Promise<UserDto> {
      const res = await apiClient.post<AuthResponse>('/auth/login', { email, password })
      this.applyAuth(res.data)
      return res.data.user
    },
    async register(payload: {
      email: string
      password: string
      firstName: string
      lastName: string
    }): Promise<UserDto> {
      const res = await apiClient.post<AuthResponse>('/auth/register', payload)
      this.applyAuth(res.data)
      return res.data.user
    },
    async logout(): Promise<void> {
      try {
        await apiClient.post('/auth/logout')
      } catch {
        // ignore - still clear local state
      }
      this.clearAuth()
    },
    async refresh(): Promise<UserDto | null> {
      try {
        const res = await apiClient.post<AuthResponse>('/auth/refresh', {})
        this.applyAuth(res.data)
        return res.data.user
      } catch {
        this.clearAuth()
        return null
      }
    },
    async init(): Promise<void> {
      await this.refresh()
    },
    async fetchMe(): Promise<UserDto | null> {
      try {
        const res = await apiClient.get<UserDto>('/auth/me')
        this.user = res.data
        return res.data
      } catch {
        return null
      }
    }
  }
})
