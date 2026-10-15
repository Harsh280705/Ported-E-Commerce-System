"""Phase 1: map product types -> Commons file candidates with license metadata.

Reads pipeline/catalog.json, writes pipeline/candidates.json:
  {type_key: {query, category, files: [{title, pageurl, thumb, width, height, license, artist}]}}
"""
import json
import os
import time
import urllib.parse
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(HERE)))
PIPE = os.path.join(ROOT, "pipeline")
UA = {"User-Agent": "YantraShop-Training/1.0 (course project; contact: training local)"}

# product-type phrase (matched in product name) -> Commons search query
TYPE_QUERIES = {
    # fashion
    "Kurta": "kurta", "Formal Shirt": "men formal shirt", "Jeans": "denim jeans",
    "T-Shirt": "t-shirt", "Saree": "saree", "Jacket": "jacket clothing",
    "Trousers": "trousers", "Blazer": "blazer", "Dress": "dress clothing",
    "Sweater": "sweater",
    # footwear
    "Running Shoes": "running shoes", "Sneakers": "sneakers", "Derby Shoes": "derby shoes",
    "Hiking Boots": "hiking boots", "Sandals": "sandals footwear", "Loafers": "loafers shoes",
    # home-kitchen
    "Cookware Set": "cookware", "Dinner Set": "dinner set plates", "Bedsheet": "bedsheet bed",
    "Table Lamp": "table lamp", "Pressure Cooker": "pressure cooker", "Kettle": "kettle",
    "Cushion Cover": "cushions sofa", "Wall Clock": "wall clock",
    # electronics
    "Headphones": "headphones", "Speaker": "bluetooth speaker", "Charger": "phone charger",
    "Smart Watch": "smartwatch", "Earbuds": "earbuds", "Power Bank": "power bank",
    "Keyboard": "computer keyboard", "Mouse": "computer mouse",
    # beauty
    "Face Wash": "face wash", "Lipstick Set": "lipstick", "Hair Oil": "hair oil bottle",
    "Kajal": "kajal eyeliner", "Moisturizer": "moisturizer cream", "Shampoo": "shampoo bottle",
    "Serum": "face serum bottle", "Compact": "compact powder makeup",
    # sports
    "Yoga Mat": "yoga mat", "Dumbbell Set": "dumbbells", "Cricket Bat": "cricket bat",
    "Football": "football", "Badminton Racket": "badminton racket", "Gym Bag": "gym bag",
    "Skipping Rope": "skipping rope", "Tennis Balls": "tennis balls",
    # books
    "Novel Pack": "novels books", "Story Collection": "story books", "Exam Guide": "books study",
    "Cookbook": "cookbook", "Biography": "biography book", "Poetry Volume": "poetry books",
    "Atlas": "atlas book", "Grammar Book": "books stack", "Dictionary": "dictionary book",
    "Bestseller": "bestselling books", "Prep Guide": "exam preparation books",
    # originals extras
    "Shirt": "shirt", "Phone": "smartphone", "Lamp": "lamp", "Novel": "novel book",
    "Shoes": "shoes", "Boots": "boots", "Watch": "wrist watch",
}


def api(params):
    url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode(params)
    req = urllib.request.Request(url, headers=UA)
    for attempt in range(4):
        try:
            with urllib.request.urlopen(req, timeout=60) as r:
                return json.load(r)
        except Exception as e:
            print("  retry", params.get("srsearch", params.get("titles", "?"))[:40], e, flush=True)
            time.sleep(2 + attempt * 2)
    return {}


def search_files(query, limit=50):
    out = api({"action": "query", "format": "json", "list": "search",
               "srsearch": f"{query} filetype:bitmap", "srnamespace": 6, "srlimit": limit})
    return [x["title"] for x in out.get("query", {}).get("search", [])]


def imageinfo(titles):
    out = api({"action": "query", "format": "json", "prop": "imageinfo",
               "iiprop": "url|size|extmetadata", "iiurlwidth": 640,
               "titles": "|".join(titles)})
    pages = out.get("query", {}).get("pages", {})
    res = []
    for pid, pg in pages.items():
        if pid == "-1" or "imageinfo" not in pg:
            continue
        ii = pg["imageinfo"][0]
        meta = ii.get("extmetadata", {})
        res.append({
            "title": pg["title"],
            "pageurl": ii.get("descriptionurl", ""),
            "thumb": ii.get("thumburl", "") or ii.get("url", ""),
            "width": ii.get("width", 0), "height": ii.get("height", 0),
            "license": (meta.get("LicenseShortName", {}) or {}).get("value", ""),
            "artist": (meta.get("Artist", {}) or {}).get("value", "")[:200],
        })
    return res


def type_of(name):
    for phrase in sorted(TYPE_QUERIES, key=len, reverse=True):
        if phrase.lower() in name.lower():
            return phrase
    return None


def main():
    rows = json.load(open(os.path.join(PIPE, "catalog.json"), encoding="utf-8"))
    need = {}
    for r in rows:
        t = type_of(r["name"])
        need.setdefault(t or "UNMATCHED:" + r["name"], []).append(r["slug"])
    print(f"distinct types: {len(need)}")
    unmatched = [k for k in need if k.startswith("UNMATCHED")]
    for u in unmatched:
        print("  UNMATCHED:", u, need[u][:2])

    cands = {}
    for t, slugs in sorted(need.items(), key=lambda kv: -len(kv[1])):
        if t.startswith("UNMATCHED"):
            continue
        q = TYPE_QUERIES[t]
        titles = search_files(q)
        files = imageinfo(titles) if titles else []
        cands[t] = {"query": q, "need": len(slugs), "files": files}
        print(f"{t}: need={len(slugs)} found={len(files)}", flush=True)
        time.sleep(0.5)
    json.dump(cands, open(os.path.join(PIPE, "candidates.json"), "w", encoding="utf-8"), indent=1)
    print("wrote candidates.json")


if __name__ == "__main__":
    main()
