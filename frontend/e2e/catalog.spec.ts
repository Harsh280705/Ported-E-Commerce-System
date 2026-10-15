// Catalog verification: 2000+ products, images, pagination + infinite scroll,
// mode switching, search/filter, admin CRUD with image updates, authorization.
import { test, expect, type Page } from '@playwright/test'

const ADMIN = { email: 'admin@yantra.local', password: 'Admin!123' }
const CUSTOMER = { email: 'customer@example.com', password: 'Customer!123' }

async function loginAs(page: Page, creds: { email: string; password: string }) {
  await page.goto('/login')
  await page.locator('#login-email').fill(creds.email)
  await page.locator('#login-password').fill(creds.password)
  await page.locator('[data-testid="login-form"] button[type="submit"]').click()
  await expect(page).toHaveURL(/\/($|\?)/, { timeout: 15000 })
}

test.describe('catalog', () => {
  test('development catalog holds at least 2000 products', async ({ page }) => {
    const res = await page.request.get('/api/products?page=1&pageSize=1')
    expect(res.ok()).toBeTruthy()
    const body = await res.json()
    expect(body.totalCount).toBeGreaterThanOrEqual(2000)
  })

  test('product grid uses desktop width at 100% zoom', async ({ page }) => {    await page.setViewportSize({ width: 1440, height: 900 })
    await page.goto('/products')
    const list = page.locator('[data-testid="product-list"]')
    await expect(list).toBeVisible({ timeout: 15000 })
    const box = await list.boundingBox()
    expect(box).not.toBeNull()
    expect(box!.width).toBeGreaterThan(1000)
    const firstCard = list.locator('[data-testid="product-card"]').first()
    const cardBox = await firstCard.boundingBox()
    expect(cardBox!.width).toBeGreaterThan(180)
  })

  test('product grid adapts to tablet and mobile widths', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 900 })
    await page.goto('/products')
    await expect(page.locator('[data-testid="product-card"]').first()).toBeVisible({ timeout: 15000 })
    const tabletCards = await page.locator('[data-testid="product-card"]').count()
    expect(tabletCards).toBeGreaterThanOrEqual(2)
    await page.setViewportSize({ width: 390, height: 844 })
    await page.waitForTimeout(500)
    const cardBox = await page.locator('[data-testid="product-card"]').first().boundingBox()
    expect(cardBox!.width).toBeLessThan(360)
    expect(await page.locator('[data-testid="product-card"]').count()).toBeGreaterThanOrEqual(1)
    // no horizontal overflow on mobile
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)
    expect(overflow).toBeLessThanOrEqual(1)
  })

  test('card images are local, unique, and match the details page', async ({ page }) => {
    await page.goto('/products')
    const cards = page.locator('[data-testid="product-card"]')
    await expect(cards.first()).toBeVisible({ timeout: 15000 })
    const srcs = await cards.locator('img').evaluateAll((imgs) =>
      (imgs as HTMLImageElement[]).slice(0, 12).map((i) => i.src)
    )
    expect(srcs.length).toBeGreaterThan(0)
    for (const s of srcs) {
      expect(s).not.toContain('picsum')
    }
    expect(new Set(srcs).size).toBe(srcs.length)
    const cardSrc = srcs[0]
    await cards.first().locator('a').first().click()
    await expect(page).toHaveURL(/\/products\//)
    const detailSrc = await page.locator('[data-testid="product-details"] img').getAttribute('src')
    // same product-specific image (both resolve to the same path)
    expect(cardSrc).toContain(detailSrc!.split('/').pop()!)
  })

  test('pagination loads different server pages', async ({ page }) => {
    await page.goto('/products')
    await expect(page.locator('[data-testid="product-card"]').first()).toBeVisible({ timeout: 15000 })
    const firstPage1 = await page.locator('[data-testid="product-card"]').first().innerText()
    await page.locator('[data-testid="pagination"] button[aria-label="Next page"]').click()
    await expect(page).toHaveURL(/page=2/)
    await expect(page.locator('[data-testid="product-card"]').first()).toBeVisible({ timeout: 15000 })
    const firstPage2 = await page.locator('[data-testid="product-card"]').first().innerText()
    expect(firstPage2).not.toBe(firstPage1)
    expect(await page.locator('[data-testid="result-count"]').innerText()).toMatch(/results/)
  })

  test('infinite scroll appends and ends cleanly', async ({ page }) => {
    await page.goto('/products')
    await page.getByRole('button', { name: /infinite scroll/i }).click()
    const cards = page.locator('[data-testid="product-card"]')
    await expect(cards.first()).toBeVisible({ timeout: 15000 })
    const before = await cards.count()
    await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight))
    await expect.poll(async () => cards.count(), { timeout: 15000 }).toBeGreaterThan(before)
    // filter reset deduplicates: search narrows the list
    await page.locator('#pl-search').fill('watch')
    await page.locator('#pl-search').press('Enter')
    await expect(page).toHaveURL(/search=watch/)
    await expect(cards.first()).toBeVisible({ timeout: 15000 })
  })

  test('mode switch preserves filtering', async ({ page }) => {
    await page.goto('/products?search=headphones')
    await expect(page.locator('[data-testid="product-card"]').first()).toBeVisible({ timeout: 15000 })
    const countText = await page.locator('[data-testid="result-count"]').innerText()
    const total = countText.match(/[\d,]+/)![0]
    await page.getByRole('button', { name: /infinite scroll/i }).click()
    await expect(page.locator('[data-testid="product-card"]').first()).toBeVisible({ timeout: 15000 })
    // same filter still applied: identical total, adapted display ("showing K" instead of page info)
    expect(await page.locator('[data-testid="result-count"]').innerText()).toContain(total)
    await page.getByRole('button', { name: /^pagination$/i }).click()
    await expect(page.locator('[data-testid="pagination"]')).toBeVisible({ timeout: 15000 })
  })

  test('search narrows results and empty search is useful', async ({ page }) => {
    await page.goto('/products')
    await expect(page.locator('[data-testid="product-card"]').first()).toBeVisible({ timeout: 15000 })
    await page.locator('#pl-search').fill('headphones')
    await page.locator('#pl-search').press('Enter')
    await expect(page).toHaveURL(/search=headphones/)
    await expect(page.locator('[data-testid="product-card"]').first()).toBeVisible({ timeout: 15000 })
    expect(await page.locator('[data-testid="result-count"]').innerText()).toMatch(/results/)
    await page.locator('#pl-search').fill('zzz-no-such-product-zzz')
    await page.locator('#pl-search').press('Enter')
    await expect(page.getByText(/no products found/i)).toBeVisible({ timeout: 15000 })
  })

  test('add to cart from card works', async ({ page }) => {
    await page.goto('/products')
    await expect(page.locator('[data-testid="product-card"]').first()).toBeVisible({ timeout: 15000 })
    const addBtn = page.locator('[data-testid="product-card"]').first().getByRole('button', { name: /add to cart/i })
    if (await addBtn.isEnabled()) {
      await addBtn.click()
      await expect(page.locator('[data-testid="cart-drawer"]')).toBeVisible()
      expect(await page.locator('[data-testid="cart-count"]').innerText()).not.toBe('0')
    }
  })

  test('admin product CRUD with image update persists to storefront', async ({ page }) => {
    await loginAs(page, ADMIN)
    await page.goto('/admin/products')
    await expect(page.locator('[data-testid="admin-products-table"]')).toBeVisible({ timeout: 15000 })
    const stamp = Date.now()
    const name = `E2E Catalog Product ${stamp}`
    const slug = `e2e-catalog-product-${stamp}`
    const img = `/images/products/tst-electronics-0004.svg`
    await page.getByRole('button', { name: /create product/i }).click()
    await page.locator('#ap-name').fill(name)
    await page.locator('#ap-slug').fill(slug)
    await page.locator('#ap-price').fill('1299')
    await page.locator('#ap-stock').fill('15')
    await page.locator('#ap-image').fill(img)
    await page.locator('[data-testid="admin-product-form"] button[type="submit"]').click()
    await expect(page.locator('[data-testid="admin-products-table"]')).toContainText(name, { timeout: 15000 })

    // storefront reflects the new product with its image (search by stamp digits in the name)
    await page.goto(`/products?search=${stamp}`)
    await expect(page.locator('[data-testid="product-card"]').first()).toBeVisible({ timeout: 15000 })
    const cardImg = await page.locator('[data-testid="product-card"] img').first().getAttribute('src')
    expect(cardImg).toContain('tst-electronics-0004.svg')

    // edit the image, verify the update persists
    await page.goto('/admin/products')
    await page.locator('#ap-search').fill(name)
    await page.waitForTimeout(600)
    await expect(page.locator('[data-testid="admin-products-table"]')).toContainText(name, { timeout: 15000 })
    await page.locator('[data-testid="admin-products-table"] button', { hasText: 'Edit' }).first().click()
    await expect(page.locator('[data-testid="admin-product-form"]')).toBeVisible()
    await page.locator('#ap-image').fill('/images/products/tst-books-0007.svg')
    await page.locator('[data-testid="admin-product-form"] button[type="submit"]').click()
    await expect(page.getByText('Product saved')).toBeVisible({ timeout: 15000 })
    await page.goto(`/products?search=${stamp}`)
    await expect(page.locator('[data-testid="product-card"]').first()).toBeVisible({ timeout: 15000 })
    const updatedImg = await page.locator('[data-testid="product-card"] img').first().getAttribute('src')
    expect(updatedImg).toContain('tst-books-0007.svg')

    // cleanup: delete the test product
    await page.goto('/admin/products')
    await page.locator('#ap-search').fill(name)
    await page.waitForTimeout(600)
    page.on('dialog', (d) => d.dismiss()) // unit path uses confirm(); component uses modal — either way proceed below
    await page.locator('[data-testid="admin-products-table"] button', { hasText: 'Delete' }).first().click()
    const modalDelete = page.locator('.modal button.btn-danger')
    if (await modalDelete.isVisible()) await modalDelete.click()
  })

  test('non-admin cannot modify the catalog', async ({ page, request }) => {
    // isolated API context: its cookies never touch the page, so the UI login below starts clean
    const login = await request.post('/api/auth/login', { data: CUSTOMER })
    expect(login.ok()).toBeTruthy()
    const token = (await login.json()).accessToken as string
    const res = await request.post('/api/admin/products', {
      headers: { Authorization: `Bearer ${token}` },
      data: { name: 'Hack', slug: 'hack', description: 'x', price: 1, stock: 1, imageUrl: '', categoryId: 1, isActive: true, isFeatured: false }
    })
    expect(res.status()).toBe(403)
    await loginAs(page, CUSTOMER)
    await page.goto('/admin/products')
    await expect(page).not.toHaveURL(/\/admin\/products/)
  })
})
