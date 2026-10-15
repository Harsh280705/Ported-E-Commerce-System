"""Step 1: dump full catalog (id, slug, name, category) to pipeline/catalog.json."""
import json
import os
import urllib.request

API = "http://localhost:8080"
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(HERE)))
OUT = os.path.join(ROOT, "pipeline")


def get(url):
    with urllib.request.urlopen(url, timeout=60) as r:
        return json.load(r)


def main():
    os.makedirs(OUT, exist_ok=True)
    first = get(f"{API}/api/products?page=1&pageSize=60")
    total_pages = first["totalPages"]
    items = list(first["items"])
    for p in range(2, total_pages + 1):
        data = get(f"{API}/api/products?page={p}&pageSize=60")
        items.extend(data["items"])
        print(f"page {p}/{total_pages}", flush=True)
    cats = {c["name"]: c["slug"] for c in get(f"{API}/api/categories")}
    rows = [
        {"id": i["id"], "slug": i["slug"], "name": i["name"],
         "category": i.get("categoryName") or "",
         "catslug": cats.get(i.get("categoryName") or "", ""),
         "image": i.get("imageUrl") or ""}
        for i in items
    ]
    with open(os.path.join(OUT, "catalog.json"), "w", encoding="utf-8") as fh:
        json.dump(rows, fh, indent=1)
    kinds = {}
    for r in rows:
        kinds[r["image"].split("?")[0].rsplit(".", 1)[-1].lower()] = kinds.get(r["image"].split("?")[0].rsplit(".", 1)[-1].lower(), 0) + 1
    print(f"products={len(rows)} categories={len(cats)} extensions={kinds}")


if __name__ == "__main__":
    main()
