export function isRequired(value: unknown): boolean {
  if (value === null || value === undefined) return false
  if (typeof value === 'string') return value.trim().length > 0
  if (Array.isArray(value)) return value.length > 0
  return true
}

export function isEmail(value: string): boolean {
  if (!value || typeof value !== 'string') return false
  const re = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/
  return re.test(value.trim())
}

export function minLength(value: string, n: number): boolean {
  if (typeof value !== 'string') return false
  return value.length >= n
}

export interface ValidationResult {
  valid: boolean
  errors: Record<string, string>
}

export function validateLoginForm(email: string, password: string): ValidationResult {
  const errors: Record<string, string> = {}
  if (!isRequired(email)) errors.email = 'Email is required'
  else if (!isEmail(email)) errors.email = 'Enter a valid email address'
  if (!isRequired(password)) errors.password = 'Password is required'
  else if (!minLength(password, 6)) errors.password = 'Password must be at least 6 characters'
  return { valid: Object.keys(errors).length === 0, errors }
}

export function validateRegisterForm(data: {
  email: string
  password: string
  firstName: string
  lastName: string
}): ValidationResult {
  const errors: Record<string, string> = {}
  if (!isRequired(data.firstName)) errors.firstName = 'First name is required'
  if (!isRequired(data.lastName)) errors.lastName = 'Last name is required'
  if (!isRequired(data.email)) errors.email = 'Email is required'
  else if (!isEmail(data.email)) errors.email = 'Enter a valid email address'
  if (!isRequired(data.password)) errors.password = 'Password is required'
  else if (!minLength(data.password, 6)) errors.password = 'Password must be at least 6 characters'
  return { valid: Object.keys(errors).length === 0, errors }
}

export function validateAddress(data: {
  fullName: string
  line1: string
  city: string
  state: string
  postalCode: string
  country: string
}): ValidationResult {
  const errors: Record<string, string> = {}
  if (!isRequired(data.fullName)) errors.fullName = 'Full name is required'
  if (!isRequired(data.line1)) errors.line1 = 'Address line 1 is required'
  if (!isRequired(data.city)) errors.city = 'City is required'
  if (!isRequired(data.state)) errors.state = 'State is required'
  if (!isRequired(data.postalCode)) errors.postalCode = 'Postal code is required'
  if (!isRequired(data.country)) errors.country = 'Country is required'
  return { valid: Object.keys(errors).length === 0, errors }
}
