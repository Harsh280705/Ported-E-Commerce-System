"""Resume: top-up pools for pending types, then download gently (4 workers,
5 retries w/ backoff). Skips slugs already in manifest.json."""
import json
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from search_assign import type_of
from refill2 import gen_search
import download_assign as D

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(HERE)))
PIPE = os.path.join(ROOT, "pipeline")

BLOCK = ["naked", "nude", "porn", "selfie", "crowd", "protest", "meme",
         "screenshot", "logo", "diagram", "map ", "painting", "sculpture"]


def blocked(title):
    t = title.lower()
    return any(b in t for b in BLOCK)


def main():
    rows = json.load(open(os.path.join(PIPE, "catalog.json"), encoding="utf-8"))
    cands = json.load(open(os.path.join(PIPE, "candidates.json"), encoding="utf-8"))
    try:
        manifest = json.load(open(os.path.join(PIPE, "manifest.json"), encoding="utf-8"))
    except FileNotFoundError:
        manifest = []
    done = {m["slug"] for m in manifest}
    pending_by_type = {}
    for r in rows:
        if r["slug"] not in done:
            pending_by_type.setdefault(type_of(r["name"]), []).append(r)
    print(f"pending types: {len(pending_by_type)} pending products: {sum(len(v) for v in pending_by_type.values())}")

    for t, prods in sorted(pending_by_type.items(), key=lambda kv: -len(kv[1])):
        pool = cands[t]["files"]
        usable = [f for f in pool if not blocked(f["title"])]
        if len(usable) >= len(prods) + 25:
            print(f"{t}: pool ok ({len(usable)})")
            continue
        base_q = cands[t]["query"]
        for q in [base_q, base_q.split()[-1] + " product", "buy " + base_q]:
            try:
                files = gen_search(q, limit=60)
            except Exception as e:
                print(t, q, "FAILED", str(e)[:80])
                time.sleep(10)
                continue
            seen = {f["title"] for f in pool}
            add = [f for f in files if f["title"] not in seen and not blocked(f["title"])]
            pool.extend(add)
            print(f"{t} [{q}]: +{len(add)} pool={len(pool)}", flush=True)
            time.sleep(8)
            if len(pool) >= len(prods) + 40:
                break
    # drop blocked titles from pools
    for t, v in cands.items():
        v["files"] = [f for f in v["files"] if not blocked(f["title"])]
    json.dump(cands, open(os.path.join(PIPE, "candidates.json"), "w", encoding="utf-8"), indent=1)

    D.WORKERS = 4
    D.RETRIES = 5
    D.BLOCK = BLOCK
    try:
        existing = json.load(open(os.path.join(PIPE, "manifest.json"), encoding="utf-8"))
    except FileNotFoundError:
        existing = []
    D.download_all(existing)
    print("resume done")


if __name__ == "__main__":
    main()
