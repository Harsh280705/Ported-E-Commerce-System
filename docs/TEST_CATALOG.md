# Test catalog & product images

## 2,000-product test catalog

`backend/Ecommerce.Infrastructure/TestCatalogSeeder.cs` generates a deterministic
catalog of **2,000** products (`tst-` slugs) across the 7 seeded categories, plus
normalizes legacy random-photo URLs to local art. It is **idempotent** (reruns
insert only missing rows; verified: second run inserts 0) and **opt-in only**:

```powershell
# From the project root (recreates api with the flag; compose default is off)
$env:SEED_TEST_CATALOG="true"
docker compose --project-name portecommerce up -d api
docker compose --project-name portecommerce logs api | Select-String "Test catalog seeding"
# Steady state afterwards (flag defaults to false):
Remove-Item Env:\SEED_TEST_CATALOG
docker compose --project-name portecommerce up -d api
```

Rules: names/slugs unique (`{Descriptor} {Material} {Type} {nnnn}` + `tst-{cat}-{nnnn}`),
prices per-category ranges with paise precision, stock 0–180 (~5% zero-stock),
`decimal(18,2)`, batches of 500, existing rows never modified/deleted, customers and
orders untouched. Never enabled automatically in production (`SeedTestCatalog`
defaults to `false`; `docker compose down -v` is still forbidden).

Verified 2026-10-14: before 32 products → inserted 2000 → total 2030 (30 seed +
2000 test; 2 transient E2E artifacts removed afterwards), 7 categories,
0 duplicate slugs, rerun inserts 0.

## Product images (deterministic local art)

Root cause of the old defect: seed data used `https://picsum.photos/seed/{slug}`
(random landscape/stock photos, unrelated to products).

Replacement: `frontend/scripts/gen_images.py` writes one stable SVG per product
slug to `frontend/public/images/products/<slug>.svg` (600×600, category palette
+ product-type glyph + name/category text). Same slug ⇒ byte-identical file.
Images ship inside the Nginx image (no external dependency, no licensing risk,
works offline), served as static files with 7-day immutable caching.

```powershell
# Regenerate after seeding (or any time; unchanged files are skipped):
python frontend/scripts/gen_images.py --api http://localhost:8080
```

- Products reference `/images/products/<slug>.svg` (saved `ImageUrl`, rendered
  verbatim by cards and detail pages; `object-fit: cover`, 1:1 aspect, lazy load,
  dimensions reserved to avoid layout shift; single-shot `onImageError` fallback
  to `/placeholder.svg` with loop guard).
- Admins can set any site-relative or http(s) URL (validated server-side);
  the admin table badges rows still on fallback art (`isFallbackImage`:
  empty/placeholder/picsum/placehold.co); the edit form previews the image and
  warns on broken URLs.
- Verified 2026-10-14: 2030/2030 products with local image refs, 0 external,
  0 fallback, 2030/2030 unique SVG files (SHA-256), 2.0 MB total, HTTP 200 via
  Nginx, card/detail `src` equality asserted in E2E, stability across
  pagination/sort/refresh covered by determinism (content hash per slug).

Limitation (honest): artwork is generated vector product art, not vendor
photography — chosen deliberately for offline reliability, zero licensing risk,
and per-product uniqueness at 2,000 scale.
