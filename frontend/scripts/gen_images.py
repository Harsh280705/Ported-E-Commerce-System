"""Deterministic product-image generator (Phase D).

For every product slug in the catalog, writes a stable, category-appropriate
SVG to frontend/public/images/products/<slug>.svg. Same slug always yields the
same image (no runtime randomness). Rerun any time; existing files are compared
and only rewritten when changed.

Usage (from repo root):
  python frontend/scripts/gen_images.py [--api http://localhost:8080] [--out frontend/public/images/products]
"""
import argparse
import hashlib
import html
import json
import os
import sys
import urllib.request

# Category -> (bg tint, accent, glyph kind)
PALETTE = {
    "fashion": ("#f3e8f5", "#7c3aed", "shirt"),
    "footwear": ("#e8eef7", "#1d4ed8", "shoe"),
    "home-kitchen": ("#fdf0e2", "#c2620a", "home"),
    "electronics": ("#e2e9f5", "#0f3d91", "phones"),
    "beauty": ("#fbe9f1", "#be185d", "bottle"),
    "sports": ("#e6f6ec", "#15803d", "ball"),
    "books": ("#f5edd c".replace(" ", ""), "#92400e", "book"),
    "_default": ("#eef1f4", "#475569", "box"),
}

GLYPHS = {
    "shirt": '<path d="M210 190 L260 160 Q300 185 340 160 L390 190 L360 230 L360 420 L240 420 L240 230 Z" fill="#ffffff" opacity="0.92"/>',
    "shoe": '<path d="M170 340 L170 300 Q230 300 260 260 L290 230 L300 260 L340 280 L430 300 L430 340 Q430 360 410 360 L190 360 Q170 360 170 340 Z" fill="#ffffff" opacity="0.92"/>',
    "home": '<path d="M300 170 L160 290 L190 290 L190 430 L270 430 L270 340 L330 340 L330 430 L410 430 L410 290 L440 290 Z" fill="#ffffff" opacity="0.92"/>',
    "phones": '<rect x="230" y="150" width="140" height="240" rx="26" fill="#ffffff" opacity="0.92"/><rect x="250" y="180" width="100" height="170" rx="8" fill="{accent}" opacity="0.55"/><circle cx="300" cy="372" r="8" fill="{accent}"/>',
    "bottle": '<rect x="270" y="230" width="60" height="170" rx="16" fill="#ffffff" opacity="0.92"/><rect x="282" y="180" width="36" height="52" rx="8" fill="#ffffff" opacity="0.92"/><rect x="282" y="262" width="36" height="106" rx="10" fill="{accent}" opacity="0.55"/>',
    "ball": '<circle cx="300" cy="300" r="110" fill="#ffffff" opacity="0.92"/><path d="M300 190 Q260 300 300 410 M190 300 Q300 260 410 300 M220 220 Q300 300 220 380 M380 220 Q300 300 380 380" stroke="{accent}" stroke-width="10" fill="none" opacity="0.6"/>',
    "book": '<path d="M300 200 Q230 175 170 200 L170 400 Q230 375 300 400 Q370 375 430 400 L430 200 Q370 175 300 200 Z" fill="#ffffff" opacity="0.92"/><line x1="300" y1="200" x2="300" y2="400" stroke="{accent}" stroke-width="8" opacity="0.6"/>',
    "box": '<rect x="190" y="220" width="220" height="170" rx="12" fill="#ffffff" opacity="0.92"/><line x1="300" y1="220" x2="300" y2="390" stroke="{accent}" stroke-width="10" opacity="0.6"/><rect x="250" y="195" width="100" height="30" rx="6" fill="#ffffff" opacity="0.92"/>',
}


def glyph(kind, accent):
    return GLYPHS.get(kind, GLYPHS["box"]).replace("{accent}", accent)


def svg_for(slug, name, category_slug, category_name):
    pal = PALETTE.get(category_slug, PALETTE["_default"])
    bg, accent, kind = pal
    h = int(hashlib.sha256(slug.encode()).hexdigest(), 16)
    rot = h % 360
    short = name if len(name) <= 34 else name[:33] + "\u2026"
    return (
        f'<svg xmlns="http://www.w3.org/2000/svg" width="600" height="600" viewBox="0 0 600 600">'
        f'<defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1">'
        f'<stop offset="0" stop-color="{bg}"/><stop offset="1" stop-color="#ffffff"/>'
        f'</linearGradient></defs>'
        f'<rect width="600" height="600" fill="url(#g)"/>'
        f'<g transform="rotate({rot} 300 300)" opacity="0.10">'
        f'<circle cx="300" cy="300" r="230" fill="{accent}"/>'
        f'<circle cx="300" cy="300" r="160" fill="{bg}"/></g>'
        f'{glyph(kind, accent)}'
        f'<rect x="0" y="478" width="600" height="122" fill="#111827" opacity="0.82"/>'
        f'<text x="300" y="524" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="27" font-weight="600" fill="#ffffff">{html.escape(short)}</text>'
        f'<text x="300" y="556" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="20" fill="#d1d5db">{html.escape(category_name or "")}</text>'
        f'</svg>'
    )


def fetch_products(api):
    items = []
    page = 1
    while True:
        url = f"{api}/api/products?page={page}&pageSize=60"
        with urllib.request.urlopen(url, timeout=30) as r:
            data = json.load(r)
        items.extend(data["items"])
        if page >= data["totalPages"]:
            break
        page += 1
    return items


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--api", default="http://localhost:8080")
    ap.add_argument("--out", default="frontend/public/images/products")
    args = ap.parse_args()

    root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    outdir = args.out if os.path.isabs(args.out) else os.path.join(root, args.out)
    os.makedirs(outdir, exist_ok=True)

    products = fetch_products(args.api)
    print(f"products in catalog: {len(products)}")
    with urllib.request.urlopen(f"{args.api}/api/categories", timeout=30) as r:
        name_to_slug = {c["name"]: c["slug"] for c in json.load(r)}
    cats = {}
    written = 0
    for p in products:
        slug = p["slug"]
        cname = p.get("categoryName") or ""
        cats[cname or "?"] = cats.get(cname or "?", 0) + 1
        content = svg_for(slug, p["name"], name_to_slug.get(cname, ""), cname)
        path = os.path.join(outdir, slug + ".svg")
        if os.path.exists(path):
            with open(path, encoding="utf-8") as fh:
                if fh.read() == content:
                    continue
        with open(path, "w", encoding="utf-8") as fh:
            fh.write(content)
        written += 1
    print(f"svg files written/updated: {written} in {outdir}")
    print("products per category:", json.dumps(cats, indent=1))


if __name__ == "__main__":
    sys.exit(main())
