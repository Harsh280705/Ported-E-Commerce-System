import { describe, it, expect } from 'vitest'
import { isEmail, isRequired, minLength, validateLoginForm, validateRegisterForm } from '@/utils/validators'
import { formatINR, isFallbackImage } from '@/utils/format'

describe('validators', () => {
  it('validates email addresses', () => {
    expect(isEmail('user@example.com')).toBe(true)
    expect(isEmail('bad')).toBe(false)
    expect(isEmail('')).toBe(false)
  })

  it('checks required and minLength', () => {
    expect(isRequired('x')).toBe(true)
    expect(isRequired('  ')).toBe(false)
    expect(minLength('abcdef', 6)).toBe(true)
    expect(minLength('abc', 6)).toBe(false)
  })

  it('rejects invalid login form', () => {
    const r = validateLoginForm('bad', '123')
    expect(r.valid).toBe(false)
    expect(r.errors.email).toBeTruthy()
  })

  it('rejects invalid register form', () => {
    const r = validateRegisterForm({ email: '', password: '', firstName: '', lastName: '' })
    expect(r.valid).toBe(false)
    expect(Object.keys(r.errors).length).toBeGreaterThan(0)
  })
})

describe('formatINR', () => {
  it('formats currency with INR symbol', () => {
    const s = formatINR(12999)
    expect(s).toContain('₹')
    expect(s).toContain('12,999')
  })
})

describe('isFallbackImage', () => {
  it('flags placeholders and random-photo providers', () => {
    expect(isFallbackImage(null)).toBe(true)
    expect(isFallbackImage('')).toBe(true)
    expect(isFallbackImage('/placeholder.svg')).toBe(true)
    expect(isFallbackImage('/images/placeholder.png')).toBe(true)
    expect(isFallbackImage('https://picsum.photos/seed/x/600/600')).toBe(true)
  })

  it('accepts real product art', () => {
    expect(isFallbackImage('/images/products/tst-electronics-0004.svg')).toBe(false)
    expect(isFallbackImage('https://cdn.example.com/img/phone.jpg')).toBe(false)
  })
})
