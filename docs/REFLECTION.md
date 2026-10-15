# Reflection

Migrating the Week-3 MVC 5 / EF 6 store to a Vue 3 SPA + .NET 10 Minimal API was
mostly an exercise in **state relocation**: the legacy system kept its hardest
state (session cart, cookie auth, server-rendered pages) in the web server's
memory, and every replacement had to put that state somewhere honest — the
database, a signed token, or an explicit client store.

The session cart was the most instructive piece. Legacy `CartSessionId` rows look
almost like the new `Cart` table, but the semantics differ: the new cart must
survive login, logout, and full-page reloads, be addressable by two kinds of
owner (anonymous id vs customer id), and merge without duplicating or overselling.
The merge rule finally chosen — fold guest lines in, cap at `min(stock, 99)`,
delete the guest cart — is three lines of code that took a disproportionate share
of design time, because "don't lose the shopper's items, don't sell what you
don't have" is a genuine conflict you resolve with policy, not cleverness. The
transactional checkout (re-read prices, idempotency key, snapshots, stock
decrement, cart clear in one transaction) is the direct descendant of the legacy
`PlaceOrder`, minus variants, plus real FK discipline.

Authentication taught the sharpest lesson. Cookie auth "just worked" in MVC
because server and session were one thing; splitting them forces you to decide
where every bit of trust lives. JWT access in memory plus rotating refresh tokens
in an HttpOnly cookie is textbook, but two production-grade bugs still slipped
through: a config-placeholder fallback that silently diverged signing and
validation keys (caught only by hitting the deployed stack, never by unit tests),
and a frontend race where Vue Router's initial navigation outran the startup
silent refresh (caught only by Playwright `page.goto`, never by clicking around
the SPA). Both reinforced the same rule: verify the running system, not the
source code. E2E against the real Docker+Nginx deployment earned its keep.

Trade-offs of SPA + REST are the usual ones, felt concretely. Decoupling let the
backend be tested with 52 fast integration tests and the frontend with 38 unit
tests plus a real browser suite — impossible with Razor views. The costs are
duplicated validation (FluentValidation server-side, hand-rolled client-side),
duplicated types (C# DTOs vs TypeScript interfaces, kept in sync by hand), and a
heavier deployment (three containers, CORS, proxy headers, history fallback).
For a storefront with admin back-office needs, the trade is worth it; for a
purely content-driven catalog, server rendering would be simpler.

Deployment-wise, Docker Compose with health-gated startup (`db` → `api` →
`nginx`) plus "migrate-and-seed on boot, additive only" proved robust across a
dozen rebuilds; the named volume survived every restart. The main friction was
Windows-path builds (long directory names vs NuGet restore) and the one
Swashbuckle/EF-OpenAPI version clash, solved by dropping the incompatible package
for the framework's built-in OpenAPI document.

Testing lessons: per-class isolated SQLite databases plus `HandleCookies=false`
clients made refresh-rotation assertions meaningful; the 401-interceptor's
`window.location` fallback is deliberately crude (dead session ⇒ clean reload).
Coverage targets were met where they matter (Core 92 %, stores 82 %) rather than
chased globally.

Future work, explicitly not done: product variants and image galleries (schema
ready to extend), guest checkout, refresh-token reuse alarms, HTTPS termination
with a real certificate, and CI. The demo video was not recorded; `DEMO_SCRIPT.md`
is the prepared walkthrough.
