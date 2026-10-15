import type { CategoryDto } from '@/types'

/**
 * Representative genuine photographs for each category. These reuse the
 * category-matching product photos already shipped in
 * frontend/public/images/photos/*.webp (served by nginx), so no extra
 * assets are downloaded or duplicated.
 */
export const CATEGORY_IMAGE_BY_SLUG: Record<string, string> = {
  fashion: '/images/photos/banarasi-silk-saree.webp',
  footwear: '/images/photos/running-shoes-pro.webp',
  'home-kitchen': '/images/photos/non-stick-cookware-set.webp',
  electronics: '/images/photos/bluetooth-speaker.webp',
  beauty: '/images/photos/herbal-face-wash.webp',
  sports: '/images/photos/cricket-bat-english.webp',
  books: '/images/photos/fiction-bestseller-pack.webp'
}

/** Legacy category paths with no corresponding static asset (return HTML). */
const LEGACY_CATEGORY_IMAGES = new Set(
  [
    '/images/fashion.png',
    '/images/footwear.png',
    '/images/home.png',
    '/images/tech.png',
    '/images/beauty.png',
    '/images/sports.png',
    '/images/books.png',
    '/images/placeholder.png'
  ].map((s) => s.toLowerCase())
)

/**
 * Resolve a servable image URL for a category card. Returns the API-provided
 * image when it points at a real static asset; otherwise falls back to the
 * category's representative photograph. Never returns an empty string, so
 * callers always render a real image (the global onImageError handler stays
 * as a last resort).
 */
export function resolveCategoryImageUrl(category: Pick<CategoryDto, 'slug' | 'imageUrl'>): string {
  const raw = (category.imageUrl ?? '').trim()
  if (raw) {
    const lower = raw.toLowerCase()
    const isServableAsset =
      !LEGACY_CATEGORY_IMAGES.has(lower) &&
      (lower.startsWith('/images/photos/') ||
        lower.startsWith('/images/products/') ||
        lower.startsWith('http://') ||
        lower.startsWith('https://'))
    if (isServableAsset) return raw
  }
  return CATEGORY_IMAGE_BY_SLUG[category.slug] ?? '/images/photos/banarasi-silk-saree.webp'
}
