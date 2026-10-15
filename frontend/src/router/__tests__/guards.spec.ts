import { describe, it, expect } from 'vitest'
import { checkAccess } from '@/router'

describe('route guards', () => {
  it('blocks unauthenticated access to auth routes', () => {
    const res = checkAccess(
      { meta: { requiresAuth: true }, fullPath: '/checkout' } as never,
      { isAuthenticated: false, isAdmin: false }
    )
    expect(res.allowed).toBe(false)
    expect(res.redirect).toBe('/login')
    expect(res.query).toEqual({ returnUrl: '/checkout' })
  })

  it('blocks guests-only routes when authenticated and preserves returnUrl', () => {
    const res = checkAccess(
      {
        meta: { requiresGuest: true },
        fullPath: '/login',
        query: { returnUrl: '/checkout' }
      } as never,
      { isAuthenticated: true, isAdmin: false }
    )
    expect(res.allowed).toBe(false)
    expect(res.redirect).toBe('/checkout')
  })

  it('blocks non-admin from admin routes', () => {
    const res = checkAccess(
      { meta: { requiresAuth: true, requiresAdmin: true }, fullPath: '/admin' } as never,
      { isAuthenticated: true, isAdmin: false }
    )
    expect(res.allowed).toBe(false)
    expect(res.redirect).toBe('/')
  })

  it('redirects guests on admin routes to login', () => {
    const res = checkAccess(
      { meta: { requiresAuth: true, requiresAdmin: true }, fullPath: '/admin/products' } as never,
      { isAuthenticated: false, isAdmin: false }
    )
    expect(res.allowed).toBe(false)
    expect(res.redirect).toBe('/login')
  })

  it('allows admin to admin routes', () => {
    const res = checkAccess(
      { meta: { requiresAuth: true, requiresAdmin: true }, fullPath: '/admin' } as never,
      { isAuthenticated: true, isAdmin: true }
    )
    expect(res.allowed).toBe(true)
  })
})
