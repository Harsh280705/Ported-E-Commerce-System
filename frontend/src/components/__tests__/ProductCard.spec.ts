import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createRouter, createWebHistory } from 'vue-router'
import { createPinia, setActivePinia } from 'pinia'
import ProductCard from '@/components/ProductCard.vue'

vi.mock('@/stores/cart', () => ({
  useCartStore: () => ({ add: vi.fn().mockResolvedValue(undefined) })
}))
vi.mock('@/stores/ui', () => ({
  useUiStore: () => ({ notify: vi.fn(), setMiniCartOpen: vi.fn() })
}))

const product = {
  id: 'p1',
  name: 'Test Phone',
  slug: 'test-phone',
  description: 'A phone',
  price: 9999,
  compareAtPrice: 12999,
  discountPct: 23,
  stock: 5,
  inStock: true,
  imageUrl: '/images/products/test-phone.svg',
  categoryId: 'c1',
  categoryName: 'Phones'
}

function makeRouter() {
  return createRouter({
    history: createWebHistory(),
    routes: [{ path: '/', name: 'home', component: { template: '<div />' } }]
  })
}

describe('ProductCard', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('renders name, price and link to details', async () => {
    const router = makeRouter()
    router.push('/')
    await router.isReady()
    const wrapper = mount(ProductCard, {
      props: { product },
      global: { plugins: [router] }
    })
    expect(wrapper.text()).toContain('Test Phone')
    expect(wrapper.text()).toContain('Phones')
    const link = wrapper.find('a[href="/products/p1"], a')
    expect(link.exists()).toBe(true)
  })

  it('shows out of stock badge when not in stock', async () => {
    const router = makeRouter()
    router.push('/')
    await router.isReady()
    const wrapper = mount(ProductCard, {
      props: { product: { ...product, inStock: false } },
      global: { plugins: [router] }
    })
    expect(wrapper.text()).toContain('Out of stock')
  })

  it('renders the product image with lazy loading', async () => {
    const router = makeRouter()
    router.push('/')
    await router.isReady()
    const wrapper = mount(ProductCard, {
      props: { product },
      global: { plugins: [router] }
    })
    const img = wrapper.find('img')
    expect(img.attributes('src')).toBe('/images/products/test-phone.svg')
    expect(img.attributes('loading')).toBe('lazy')
  })

  it('disables Add to cart when out of stock', async () => {
    const router = makeRouter()
    router.push('/')
    await router.isReady()
    const wrapper = mount(ProductCard, {
      props: { product: { ...product, inStock: false } },
      global: { plugins: [router] }
    })
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Add to cart'))
    expect(btn).toBeTruthy()
    expect((btn!.element as HTMLButtonElement).disabled).toBe(true)
  })
})
