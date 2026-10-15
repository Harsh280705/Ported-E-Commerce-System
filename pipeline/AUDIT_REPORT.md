# Product-Image Audit Report — 2026-10-14

## Scope
Full catalog of the Port Legacy E-Commerce storefront (`http://localhost:8080`).
Every product inspected via the live API (`/api/products`, all 34 pages) and
cross-checked against `pipeline/catalog.json` (slug sets identical, 2030 = 2030).

## Results (measured, not estimated)

| Metric | Value |
|---|---|
| Total products inspected | 2030 |
| Products with assigned images | 2030 |
| Valid photographs (WebP, served HTTP 200) | 2030 |
| Unique image files (`frontend/public/images/photos/*.webp`) | 2030 |
| Unique SHA-256 content hashes | 2030 (0 duplicate groups) |
| Unique perceptual hashes (dHash, exact) | 2030 (0 duplicate groups) |
| Near-duplicate pairs (Hamming ≤ 3) | 0 |
| Unique `ImageUrl` values in live API | 2030 |
| Duplicate photographs across products | 0 |
| Broken image references | 0 |
| Placeholders / emoji / SVG art remaining in DB | 0 |
| Products without verified photographs | 0 |
| Products with recognized type matching name+category | 2030 |
| Unresolved image-source / licensing issues | 0 |

Breakdown of assignment status: `photo-title-match` 1331, `photo-keyword-sourced` 699.
The catalog consists of synthetic test products, so images are genuine photographs
of the same product type (representative), not depictions of a specific
manufacturer/model — recorded per-entry in the manifest.

## Sources & licensing
- Source: Wikimedia Commons API (`commons.wikimedia.org`), bitmap files only,
  blocked-title filter (adult/meme/logo/diagram/painting/sculpture terms).
- Every manifest entry records `source` (Commons file page URL), `license`
  (e.g. CC BY-SA 2.0/3.0/4.0, CC BY 2.0, Public domain), `artist`, and the
  search `query` used. 2030/2030 entries carry license + source metadata.
- Files stored locally: WebP quality 72, max dimension 640px, total ~72 MB
  (avg ~37 KB, max ~142 KB). No oversized originals, no duplicate files.

## Artifacts
- `pipeline/catalog.json` — id/slug/name/category/image dump of all 2030 products
- `pipeline/candidates.json` — per-type Commons candidate pools with license metadata
- `pipeline/manifest.json` — deterministic slug → photo mapping (id, slug, name,
  category, type, file, source, license, artist, sha256, phash, dimensions, status)
- `pipeline/update_imageurls.sql` — the exact image-field-only update applied to the DB
- `frontend/public/images/photos/*.webp` — 2030 unique photo files (shipped in the
  nginx image via the existing `public/` → `dist/` build step)

## Verification performed
- Full-catalog API audit: 2030/2030 `ImageUrl` = `/images/photos/<slug>.webp`, all unique.
- Manifest ↔ live DB comparison: 0 mismatches.
- HTTP spot check: 3 products × 7 categories = 21/21 return 200, `image/webp`,
  valid WebP magic bytes, through Nginx on the deployed stack.
- Refetch stability: repeated page fetch returns identical image mapping (deterministic).
- Content dedupe: SHA-256 + dHash exact + pairwise Hamming ≤ 3 scan — all clean.
- `dotnet test` (backend): 56/56 passed.
- `npm run test:unit -- --run` (frontend): 50/50 passed.
- `docker compose build api nginx && docker compose up -d`: healthy; DB container
  and `portecommerce_mssql_data` volume untouched (no reset, no data loss).
- Post-restart API audit confirms seeders did not overwrite photo assignments.

## Code changes (minimal, image-related only)
- `backend/Ecommerce.Infrastructure/TestCatalogSeeder.cs` — new test-catalog rows map
  to `/images/photos/<slug>.webp`; `NormalizeLegacyImageUrlsAsync` also normalizes
  legacy `/images/products/*.svg` placeholder art to the photo path (admin custom
  URLs untouched). Existing rows are still never modified on reseed.
- `backend/Ecommerce.Infrastructure/SeedData.cs` — demo rows map to the photo path.
- `backend/Ecommerce.Tests/TestCatalogSeederTests.cs` — assertion updated to `/images/photos/`.
- `nginx/nginx.conf` — static-asset cache rule now includes `webp|avif`.
- `frontend/scripts/photo_pipeline/topup_pending.py` — repeatable top-up script used
  for the final 91 products (pools + unique assignment + manifest regeneration).
- Coverage for future product slugs: rerun `dump_catalog.py` →
  `resume.py` (generic pending handler) → apply new statements from
  `update_imageurls.sql`.

## Preservation confirmations
- No UI, pricing, stock, description, category, cart, checkout, auth, admin, migration,
  compose, or test-logic changes (one image-path test assertion updated as required).
- Database not deleted/recreated; product count unchanged at 2030.
- Legacy directory `Week 3 (5 October to 11 October)\Legacy E-Commerce` not modified
  (no reads beyond listing, no writes).
- Note: the project directory is not a git repository, so no git baseline exists;
  audit was performed against the live API + database instead.
