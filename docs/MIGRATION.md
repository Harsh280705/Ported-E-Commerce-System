# Migration inventory — Legacy (MVC 5 / EF 6) → Modern (Vue 3 + .NET 10 Minimal API)

Source inspected read-only: `Week 3 … / Legacy E-Commerce`
(`Ecommerce.Core`, `Ecommerce.Data`, `Ecommerce.Services`, `Ecommerce.Web`, `db/*.sql`).
No file in the legacy tree was created, modified, or deleted during this project.

## Legacy capabilities discovered

| # | Legacy feature | Where it lived | Business rules preserved |
|---|---|---|---|
| 1 | Category tree (parent/child), nav badges with product counts | `CatalogService.GetCategoryTree`, `Categories` table (`ParentCategoryId`, `DisplayOrder`, `IsActive`) | Flattened to active categories with `productCount`; child counts rolled into parents in legacy, kept as direct counts |
| 2 | Product search (name/description/SKU), category filter incl. child categories, sort (`price_asc/desc`, `name_asc/desc`, `newest`, `recommended`), pagination (12/page, max 60, deterministic secondary key) | `CatalogService.SearchProducts/ApplySort`, `Mapping.ToCard` | Same search fields, same sort keys, same defaults; pageSize cap 60; `recommended` = featured → newest |
| 3 | Product detail by slug-or-id, images, variants, discount %, related items | `GetProductDetail`, `ProductImages`, `ProductVariants` | Detail by id; single `ImageUrl`; `CompareAtPrice` → `DiscountPct`; variants dropped (documented gap) |
| 4 | Featured / new-arrival / discounted rails | `GetFeatured/GetNewArrivals/GetDiscounted` | `IsFeatured` + `recommended`/`newest` sorts cover the same merchandising |
| 5 | Session cart (`CartSessionId`), AJAX mini-cart, qty 1–99, server-side stock + price revalidation | `CartService`, `CartSession`, `CartItems` table | Persisted `Cart`/`CartItem` rows keyed by `CustomerId` or `AnonymousId` (`X-Cart-Id`); same 1–99 limits; prices always re-read from DB |
| 6 | Multi-step checkout, idempotency key, server-side total recompute, stock decrement, cart clear | `CheckoutService.PlaceOrder` | Same flow in `OrderService` inside a DB transaction; duplicate `IdempotencyKey` returns the original order |
| 7 | Shipping tiers: standard 5.99 (free ≥ 75), express 12.99 (free ≥ 150), priority 24.99 (free ≥ 200); tax 8 % | `PricingRules` (`Mapping.cs`) | Copied verbatim into `Ecommerce.Core.PricingRules` |
| 8 | Payment methods `cod`, `card_demo`, `upi_demo`, `netbanking_demo` (demo, no gateway) | `CheckoutService` allow-lists | Same allow-list; orders stay in `Placed` state (never marked paid) |
| 9 | Order number `LE-yyyyMMdd-xxxxxx`, status `Placed`, address snapshot on order, order history by e-mail | `Order`/`OrderLine` tables | Order number `NE-…` (new-system prefix); full shipping/billing snapshots + `ProductName`/`UnitPrice` snapshots on lines; history per customer id |
| 10 | Identity registration/login, `Customer` row linked by e-mail, Admin role via `AspNetUserRoles` | `AccountController`, `AdminBootstrap`, `IsAdmin` SQL | Self-contained `Customer` (email + password hash, `Role` column); JWT + refresh tokens replace cookie auth |
| 11 | Admin dashboard metrics (products, orders, units sold, recorded sales, pending/completed, low/out-of-stock, recent orders, best sellers, 14-day sales) | `AdminService.GetDashboard` | Dashboard endpoint returns real counts: products, orders, customers, low/out-of-stock, pending, revenue; per-day/best-seller series intentionally trimmed (documented gap) |
| 12 | Admin stock update (0–100000), product activate/deactivate | `UpdateStock`, `SetActive` | Full product CRUD + category CRUD + order-status flow with forward-only transitions |
| 13 | Saved customer addresses, default address | `Addresses` table | Full address CRUD with exactly-one-default invariant |
| 14 | Demo catalog (~100–1500 generated products, 8 root + child categories) | `db/seed*.sql` | Deterministic C# seeder: 7 categories, 30 products, 1 admin, 1 customer; idempotent, never wipes user data |

## Feature → new-implementation map

| Legacy | Vue page(s) | Pinia store(s) | API endpoint(s) | Backend service |
|---|---|---|---|---|
| Catalog browse/search/filter/sort | `HomeView`, `ProductListView`, `ProductDetailsView` | `catalogStore` | `GET /api/categories`, `GET /api/products`, `GET /api/products/{id}` | `CatalogService` |
| Session cart + mini-cart | `CartView`, `CartDrawer`, `AppHeader` | `cartStore` | `GET/POST/PUT/DELETE /api/cart…`, `POST /api/cart/merge` | `CartService` |
| Checkout + confirmation | `CheckoutView` (`CheckoutForm` + `OrderSummary`) | `orderStore`, `cartStore` | `POST /api/orders` | `OrderService` |
| Order history/details | `OrderHistoryView`, `OrderDetailsView` | `orderStore` | `GET /api/orders`, `GET /api/orders/{id}` | `OrderService` |
| Register/login/account | `LoginView`, `RegisterView`, `AccountDashboardView` | `authStore` | `/api/auth/*`, `/api/account/*` | `AuthService`, `AccountService` |
| Admin dashboard/products/orders | `AdminDashboardView`, `AdminProductsView`, `AdminOrdersView` | `catalogStore`, `orderStore` | `/api/admin/*` | `AdminService` |

## Deliberate architectural changes

1. **Session cart → persisted cart + anonymous id.** The legacy cart lived in server session state. The new cart is a database row addressed by `X-Cart-Id` (guest) or `CustomerId` (login), merged on login with stock revalidation and 99-unit capping.
2. **Cookie auth → JWT + rotating refresh tokens.** Short-lived (15 min) signed access tokens; opaque refresh tokens persisted with expiry, revocation, rotation, and reuse detection.
3. **EF6/`DbContext` in controllers → Minimal API + services + DTOs.** No entity is ever serialized; `TypedResults` + `ProblemDetails` everywhere.
4. **Mono/SQL scripts → EF Core migrations + C# seeder.** Schema is code (`Migrations/InitialCreate`); seeding is repeatable and additive.
5. **Razor/jQuery → Vue 3 SPA** served by Nginx with history-mode fallback; API proxied under `/api/`.

## Known gaps vs legacy

- **Product variants** (size/colour) and **multi-image galleries** were dropped; the new model keeps one image per product. Both can be re-added as `ProductImage`/`ProductVariant` tables without breaking the API contract (extend DTOs).
- **Category hierarchy** is flattened (no parent/child table); child-category filtering from legacy search is not reproduced.
- **Guest (anonymous) checkout** is not offered: `POST /api/orders` requires authentication (assignment requirement). Legacy allowed guest orders by e-mail.
- **14-day sales series / best-seller lists** on the admin dashboard are reduced to core counts + revenue.
- Prices are stored in INR-style decimals as in legacy seed data; no currency conversion.
