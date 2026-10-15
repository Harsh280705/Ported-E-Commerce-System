# API reference

Base URL (production): `http://localhost:8080` → API under `/api/`.
OpenAPI JSON: `/openapi/v1.json` · Swagger UI (dev): `/swagger` · Health: `/health`, `/health/db`.
All bodies are JSON. Errors are RFC 9457 `ProblemDetails`
(`type/title/status/detail`, validation errors under `errors`).
Auth: `Authorization: Bearer <accessToken>`; guest-cart identity via `X-Cart-Id` header.

## Auth — anonymous unless noted

| Method | Path | Body → 200/201 |
|---|---|---|
| POST | `/api/auth/register` | `{email,password(8+,upper,lower,digit),firstName,lastName}` → 201 `AuthResponse`, `Location: /api/auth/me` |
| POST | `/api/auth/login` | `{email,password}` → 200 `AuthResponse` |
| POST | `/api/auth/refresh` | `{refreshToken?}` (or HttpOnly cookie) → 200 `AuthResponse` (rotates) |
| POST | `/api/auth/logout` | auth · revokes presented refresh token → 204 |
| GET | `/api/auth/me` | auth → 200 `UserDto{id,email,firstName,lastName,role}` |

`AuthResponse = {accessToken, expiresAt, user}`. Statuses: 400 validation,
401 bad credentials/expired token, 409 e-mail taken. Auth endpoints are rate
limited (20 req/min/IP; 1000 in `Testing`).

## Catalog — public

- `GET /api/categories` → `[{id,name,slug,description,imageUrl,productCount}]`
- `GET /api/products?page=1&pageSize=12&categoryId&search&sort` →
  `{items:[{id,name,slug,description,price,compareAtPrice,discountPct,stock,inStock,imageUrl,categoryId,categoryName}],page,pageSize,totalCount,totalPages}`.
  `sort ∈ recommended|price_asc|price_desc|name_asc|name_desc|newest` (default `recommended`);
  search matches name/description/SKU (case-insensitive); `pageSize` clamped to 1–60.
- `GET /api/products/{id}` → detail DTO or 404.

## Cart — guest (`X-Cart-Id`) or auth

- `GET /api/cart` → `CartDto{items:[{productId,productName,slug,imageUrl,quantity,unitPrice,lineTotal,stockAvailable}],subTotal,shipping,tax,grandTotal,itemCount}`
- `POST /api/cart/items {productId,quantity 1–99}` → 200 `CartDto` (404 unknown/inactive product, 409 out of stock / exceeds stock)
- `PUT /api/cart/items/{productId} {quantity 0–99}` → 200 `CartDto` (0 removes)
- `DELETE /api/cart/items/{productId}` → 200 `CartDto`
- `DELETE /api/cart` → 204
- `POST /api/cart/merge` (auth) → folds the `X-Cart-Id` guest cart into the user cart → 200 `CartDto`

## Orders & checkout — auth

- `POST /api/orders` →
  `{shippingAddress{fullName,line1,line2,city,state,postalCode,country,phone},billingAddress,same,shippingAddressId?,billingAddressId?,shippingMethod(standard|express|priority),paymentMethod(cod|card_demo|upi_demo|netbanking_demo),idempotencyKey?}`
  → 201 `OrderDto`, `Location: /api/orders/{id}`. 400 empty cart / bad address / bad
  method, 409 insufficient stock. Totals, shipping (5.99/12.99/24.99 with free
  thresholds 75/150/200) and 8 % tax are computed server-side.
- `GET /api/orders` → `[{id,orderNumber,orderDate,status,total,itemCount}]`
- `GET /api/orders/{id}` → `OrderDto{id,orderNumber,orderDate,status,subTotal,shippingCost,taxAmount,total,shippingMethod,paymentMethod,shippingAddress,billingAddress,items:[{productId,productName,quantity,unitPrice,lineTotal}]}` (403 for other customers' orders)

## Account — auth

- `GET /api/account/profile` → `ProfileDto` · `PUT /api/account/profile {firstName,lastName}` → 200
- `GET /api/account/addresses` → list (default first)
- `POST /api/account/addresses {fullName,line1,line2,city,state,postalCode,country,phone,isDefault}` → 201 + `Location`
- `PUT /api/account/addresses/{id}` → 200 · `DELETE …` → 204 (404 for foreign ids;
  deleting the default promotes another address)

## Admin — `Admin` role (403 otherwise)

- `GET /api/admin/dashboard` → `{totalProducts,totalOrders,totalCustomers,lowStockProducts,outOfStockProducts,pendingOrders,totalRevenue}` (live queries)
- `GET /api/admin/products?page&pageSize&categoryId&search&sort` · `POST` (201) · `PUT /api/admin/products/{id}` ·
  `DELETE` (204; 409 when historical orders/carts reference the product — deactivate instead)
  Body: `{name,slug?,description,price,compareAtPrice?,stock,imageUrl,categoryId,isActive,isFeatured}`.
  `imageUrl` must be empty, a site-relative path (`/...`), or an http(s) URL (400 otherwise);
  blank/placeholder art is flagged as fallback in the admin UI.
- `GET/POST /api/admin/categories` · `PUT/DELETE /api/admin/categories/{id}`
  (409 when products exist in the category)
- `GET /api/admin/orders` → history list
- `PUT /api/admin/orders/{id}/status {status}` → forward-only transitions
  (`Placed→Processing→Shipped→Delivered→Completed`, plus `Cancelled` from
  `Placed/Processing`, `Refunded` from shipped states); backward moves → 409.

## Conventions

201 responses carry `Location`; deletes return 204; validation failures 400 with
field errors; auth failures 401; forbidden 403; missing 404; stock/state conflicts
409; auth rate-limit breaches 429. Prices/totals are `decimal(18,2)`; timestamps UTC.
