// TESTING NOTE: These e2e tests require a running backend API at http://localhost:5000
// (ASP.NET Core Minimal API proxied via Vite /api) AND the frontend dev server at
// http://localhost:5173 (started automatically by playwright webServer: `npm run dev`).
// Run with: npm run test:e2e. Seeded users: admin@yantra.local / Admin!123,
// customer@example.com / Customer!123.
import { test, expect } from '@playwright/test'

const CUSTOMER = { email: 'customer@example.com', password: 'Customer!123' }
const ADMIN = { email: 'admin@yantra.local', password: 'Admin!123' }

test.describe('smoke', () => {
  test('browse products, search/filter, product details', async ({ page }) => {
    await page.goto('/products')
    await expect(page.getByRole('heading', { name: /products/i })).toBeVisible()
    await page.locator('#pl-search').fill('phone')
    await page.locator('#pl-search').press('Enter')
    await expect(page).toHaveURL(/search=phone/)
    const cards = page.locator('[data-testid="product-card"]')
    await expect(cards.first()).toBeVisible({ timeout: 15000 })
    await cards.first().locator('a').first().click()
    await expect(page).toHaveURL(/\/products\//)
    await expect(page.locator('[data-testid="product-details"]')).toBeVisible({ timeout: 15000 })
  })

  test('add/update cart', async ({ page }) => {
    await page.goto('/products')
    const cards = page.locator('[data-testid="product-card"]')
    await expect(cards.first()).toBeVisible({ timeout: 15000 })
    await cards.first().locator('a').first().click()
    await expect(page.locator('[data-testid="product-details"]')).toBeVisible({ timeout: 15000 })
    const addBtn = page.getByRole('button', { name: /add to cart/i })
    if (await addBtn.isEnabled()) {
      await addBtn.click()
      await expect(page.locator('[data-testid="cart-drawer"]')).toBeVisible()
    }
    await page.goto('/cart')
    await expect(page.getByRole('heading', { name: /your cart/i })).toBeVisible()
  })

  test('register/login, checkout, order history', async ({ page }) => {
    const email = `e2e-${Date.now()}@example.com`
    await page.goto('/register')
    await page.locator('#reg-first').fill('E2E')
    await page.locator('#reg-last').fill('User')
    await page.locator('#reg-email').fill(email)
    await page.locator('#reg-password').fill('Password!123')
    await page.locator('[data-testid="register-form"] button[type="submit"]').click()
    await expect(page).toHaveURL(/\/($|\?)/, { timeout: 15000 })

    // Checkout requires cart items: add one product first.
    await page.goto('/products')
    const cards = page.locator('[data-testid="product-card"]')
    await expect(cards.first()).toBeVisible({ timeout: 15000 })
    await cards.first().locator('a').first().click()
    await expect(page.locator('[data-testid="product-details"]')).toBeVisible({ timeout: 15000 })
    const addBtn = page.getByRole('button', { name: /add to cart/i })
    await expect(addBtn).toBeEnabled({ timeout: 15000 })
    await addBtn.click()

    await page.goto('/checkout')
    await expect(page.getByRole('heading', { name: /checkout/i })).toBeVisible({ timeout: 15000 })
    await page.locator('#ship-fullName').fill('E2E User')
    await page.locator('#ship-line1').fill('1 Test Street')
    await page.locator('#ship-city').fill('Pune')
    await page.locator('#ship-state').fill('MH')
    await page.locator('#ship-postal').fill('411001')
    await page.locator('#ship-country').fill('India')
    await page.locator('[data-testid="checkout-form"] button[type="submit"]').click()
    await expect(page.locator('[data-testid="order-confirmation"]')).toBeVisible({ timeout: 20000 })

    await page.goto('/account/orders')
    await expect(page.getByRole('heading', { name: /order history/i })).toBeVisible()
  })

  test('login as seeded customer', async ({ page }) => {
    await page.goto('/login')
    await page.locator('#login-email').fill(CUSTOMER.email)
    await page.locator('#login-password').fill(CUSTOMER.password)
    await page.locator('[data-testid="login-form"] button[type="submit"]').click()
    await expect(page).toHaveURL(/\/($|\?)/, { timeout: 15000 })
  })

  test('admin dashboard + create product', async ({ page }) => {
    await page.goto('/login')
    await page.locator('#login-email').fill(ADMIN.email)
    await page.locator('#login-password').fill(ADMIN.password)
    await page.locator('[data-testid="login-form"] button[type="submit"]').click()
    await expect(page).toHaveURL(/\/($|\?)/, { timeout: 15000 })
    await page.goto('/admin')
    await expect(page.locator('[data-testid="admin-stats"]')).toBeVisible({ timeout: 15000 })
    await page.goto('/admin/products')
    await expect(page.locator('[data-testid="admin-products-table"]')).toBeVisible({ timeout: 15000 })
    await page.getByRole('button', { name: /create product/i }).click()
    await expect(page.locator('[data-testid="admin-product-form"]')).toBeVisible()
    const stamp = Date.now()
    await page.locator('#ap-name').fill(`E2E Product ${stamp}`)
    await page.locator('#ap-slug').fill(`e2e-product-${stamp}`)
    await page.locator('#ap-price').fill('199')
    await page.locator('#ap-stock').fill('10')
    await page.locator('[data-testid="admin-product-form"] button[type="submit"]').click()
    await expect(page.locator('[data-testid="admin-products-table"]')).toContainText(
      `E2E Product ${stamp}`,
      { timeout: 15000 }
    )
  })

  test('unauthorized admin blocked', async ({ page }) => {
    await page.goto('/login')
    await page.locator('#login-email').fill(CUSTOMER.email)
    await page.locator('#login-password').fill(CUSTOMER.password)
    await page.locator('[data-testid="login-form"] button[type="submit"]').click()
    await expect(page).toHaveURL(/\/($|\?)/, { timeout: 15000 })
    await page.goto('/admin')
    await expect(page).not.toHaveURL(/\/admin$/)
  })
})
