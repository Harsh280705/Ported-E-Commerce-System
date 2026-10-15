"""Top up candidate pools for the 14 pending types, then download remaining photos.

Appends fresh Commons files (unseen titles, blocked-title filtered) using the
efficient generator=search path, then resumes gentle download (4 workers).
Safe to rerun: skips slugs already in manifest.json.
"""
import json
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from refill2 import gen_search

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(HERE)))
PIPE = os.path.join(ROOT, "pipeline")

BLOCK = ["naked", "nude", "porn", "selfie", "crowd", "protest", "meme",
         "screenshot", "logo", "diagram", "map ", "painting", "sculpture"]

QUERIES = {
    "Derby Shoes": ["derby shoe", "mens dress shoes", "oxford shoes", "leather shoes men"],
    "Formal Shirt": ["dress shirt", "white formal shirt", "office shirt man"],
    "Hair Oil": ["hair oil bottle", "coconut oil bottle", "ayurvedic oil"],
    "Hiking Boots": ["hiking boots", "trekking boots", "leather boots"],
    "Jeans": ["jeans", "denim pants", "blue jeans folded"],
    "Kajal": ["kajal", "kohl eyeliner", "eye makeup pencil"],
    "Kurta": ["kurta", "kurta men", "indian ethnic wear men"],
    "Lipstick Set": ["lipstick", "lipstick set", "red lipstick"],
    "Loafers": ["loafers", "moccasins shoes", "slip-on shoes men"],
    "Novel Pack": ["novels", "fiction books stack", "paperback books"],
    "Poetry Volume": ["poetry book", "poems book", "open book pages"],
    "Running Shoes": ["running shoes", "jogging shoes", "athletic shoes", "sports shoes"],
    "Sandals": ["sandals", "leather sandals", "sandals footwear pair"],
    "Sneakers": ["sneakers", "canvas shoes", "white sneakers"],
}

NEED = {"Derby Shoes": 5, "Formal Shirt": 2, "Hair Oil": 1, "Hiking Boots": 3,
        "Jeans": 1, "Kajal": 6, "Kurta": 4, "Lipstick Set": 2, "Loafers": 7,
        "Novel Pack": 12, "Poetry Volume": 18, "Running Shoes": 11,
        "Sandals": 13, "Sneakers": 6}


def blocked(title):
    return any(b in title.lower() for b in BLOCK)


def main():
    cands = json.load(open(os.path.join(PIPE, "candidates.json"), encoding="utf-8"))
    for t, queries in QUERIES.items():
        pool = cands[t]["files"]
        seen = {f["title"] for f in pool}
        fresh_titles = []
        for q in queries:
            # count = current pool size (all titles unique by construction)
            if len(pool) >= 120:
                break
            try:
                files = gen_search(q, limit=60)
            except Exception as e:
                print(t, q, "FAILED", str(e)[:100], flush=True)
                time.sleep(10)
                continue
            add = [f for f in files if f["title"] not in seen and not blocked(f["title"])]
            seen.update(f["title"] for f in add)
            fresh_titles.extend(f["title"] for f in add)
            pool.extend(add)
            print(f"{t} [{q}]: +{len(add)} pool={len(pool)}", flush=True)
            time.sleep(8)
            # stop when we have enough *fresh* files beyond what download consumed
            if len(pool) >= 90:
                break
        # Move fresh files to the head: the downloader consumes pools sequentially
        # from index 0, and heads were already assigned to finished products.
        fresh = [f for f in pool if f["title"] in set(fresh_titles)]
        rest = [f for f in pool if f["title"] not in set(fresh_titles)]
        cands[t]["files"] = fresh + rest
        print(f"{t}: reordered fresh_first={len(fresh)} rest={len(rest)}", flush=True)
    for t in QUERIES:
        cands[t]["files"] = [f for f in cands[t]["files"] if not blocked(f["title"])]
    json.dump(cands, open(os.path.join(PIPE, "candidates.json"), "w", encoding="utf-8"), indent=1)
    print("pools topped up")

    import download_assign as D
    D.WORKERS = 4
    D.RETRIES = 5
    try:
        existing = json.load(open(os.path.join(PIPE, "manifest.json"), encoding="utf-8"))
    except FileNotFoundError:
        existing = []
    D.download_all(existing)
    print("topup done")


if __name__ == "__main__":
    main()
