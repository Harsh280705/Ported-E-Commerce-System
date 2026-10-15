export function formatINR(value: number | null | undefined): string {
  const n = typeof value === 'number' && Number.isFinite(value) ? value : 0
  return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR' }).format(n)
}

export function formatDate(value: string | Date | null | undefined): string {
  if (!value) return ''
  const d = typeof value === 'string' ? new Date(value) : value
  if (Number.isNaN(d.getTime())) return ''
  return d.toLocaleString('en-IN')
}

export const PLACEHOLDER_IMAGE = '/placeholder.svg'

/** True when a product image is a generic fallback rather than real product art. */
export function isFallbackImage(url: string | null | undefined): boolean {
  if (!url || !url.trim()) return true
  const u = url.trim().toLowerCase()
  return (
    u.endsWith('/placeholder.svg') ||
    u.endsWith('/images/placeholder.png') ||
    u.includes('picsum.photos') ||
    u.includes('placehold.co') ||
    u.includes('via.placeholder')
  )
}

export function onImageError(event: Event): void {
  const el = event.target as HTMLImageElement | null
  if (el && !el.dataset.fallback) {
    el.dataset.fallback = '1'
    el.src = PLACEHOLDER_IMAGE
  }
}
