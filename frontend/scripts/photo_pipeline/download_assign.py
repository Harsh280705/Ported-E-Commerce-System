"""Phase 2: download candidates, content-hash dedupe, assign 1 unique photo per
product, save WebP locally, emit manifest + SQL update script."""
import hashlib
import io
import json
import os
import sys
import threading
import time
import urllib.request
from concurrent.futures import ThreadPoolExecutor

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from search_assign import type_of

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(HERE)))
PIPE = os.path.join(ROOT, "pipeline")
PHOTO_DIR = os.path.join(ROOT, "frontend", "public", "images", "photos")
UA = {"User-Agent": "YantraShop-Training/1.0 (course project; contact: training local)"}
WORKERS = 10
RETRIES = 3
BLOCK = []


def dhash(img):
    g = img.convert("L").resize((9, 8), Image.LANCZOS)
    px = list(g.getdata())
    bits = 0
    for y in range(8):
        for x in range(8):
            bits = (bits << 1) | (1 if px[y * 9 + x] > px[y * 9 + x + 1] else 0)
    return bits


def ham(a, b):
    return bin(a ^ b).count("1")


def fetch(url):
    req = urllib.request.Request(url, headers=UA)
    last = None
    for attempt in range(RETRIES + 2):
        try:
            with urllib.request.urlopen(req, timeout=60) as r:
                return r.read()
        except Exception as e:
            last = e
            time.sleep(3 + attempt * 3)
    raise last


def process_candidate(slug, cand):
    """Returns (webp_bytes, sha, dh, meta) or raises."""
    raw = fetch(cand["thumb"])
    img = Image.open(io.BytesIO(raw)).convert("RGB")
    img.thumbnail((640, 640), Image.LANCZOS)
    buf = io.BytesIO()
    img.save(buf, "WEBP", quality=72, method=4)
    data = buf.getvalue()
    return data, hashlib.sha256(data).hexdigest(), dhash(img), (img.width, img.height)


def download_all(existing_manifest=None):
    rows = json.load(open(os.path.join(PIPE, "catalog.json"), encoding="utf-8"))
    cands = json.load(open(os.path.join(PIPE, "candidates.json"), encoding="utf-8"))
    os.makedirs(PHOTO_DIR, exist_ok=True)
    manifest = list(existing_manifest or [])
    done = {m["slug"] for m in manifest}
    rows = [r for r in rows if r["slug"] not in done]

    by_type = {}
    for r in rows:
        t = type_of(r["name"])
        assert t and t in cands, f"no candidates for {r['name']}"
        by_type.setdefault(t, []).append(r)

    used_sha = {m["sha256"] for m in manifest}
    used_dh_list = [int(m["phash"], 16) for m in manifest]
    used_dh = set(used_dh_list)
    lock = threading.Lock()
    sql = [f"UPDATE Products SET ImageUrl = '{m['file']}' WHERE Slug = '{m['slug']}';" for m in manifest]
    stats = {"ok": len(manifest), "dup_sha": 0, "dup_dh": 0, "failed": 0, "unresolved": []}

    def is_dup(sha, dh):
        if sha in used_sha:
            return "sha"
        for od in used_dh_list:
            if ham(dh, od) <= 3:
                return "dh"
        return None

    def claim(sha, dh):
        used_sha.add(sha)
        used_dh.add(dh)
        used_dh_list.append(dh)

    def assign_one(prod, cand):
        try:
            data, sha, dh, (w, h) = process_candidate(prod["slug"], cand)
        except Exception:
            with lock:
                stats["failed"] += 1
            return None
        with lock:
            dup = is_dup(sha, dh)
            if dup:
                stats["dup_" + dup] += 1
                return "dup"
            claim(sha, dh)
        path = os.path.join(PHOTO_DIR, prod["slug"] + ".webp")
        with open(path, "wb") as fh:
            fh.write(data)
        title = cand["title"].lower()
        kw = any(w in title for w in prod["name"].lower().split()[:4])
        with lock:
            manifest.append({
                "id": prod["id"], "slug": prod["slug"], "name": prod["name"],
                "category": prod["category"], "type": type_of(prod["name"]),
                "file": f"/images/photos/{prod['slug']}.webp",
                "source": cand["pageurl"], "license": cand.get("license", ""),
                "artist": cand.get("artist", ""), "sha256": sha,
                "phash": f"{dh:016x}", "width": w, "height": h,
                "query": cands[type_of(prod["name"])]["query"],
                "status": "photo-title-match" if kw else "photo-keyword-sourced",
            })
            sql.append(
                f"UPDATE Products SET ImageUrl = '/images/photos/{prod['slug']}.webp' WHERE Slug = '{prod['slug']}';")
            stats["ok"] += 1
        return "ok"

    for t, prods in sorted(by_type.items(), key=lambda kv: -len(kv[1])):
        pool = list(cands[t]["files"])
        print(f"{t}: {len(prods)} products, {len(pool)} candidates", flush=True)
        idx = 0
        pending = list(prods)
        with ThreadPoolExecutor(max_workers=WORKERS) as ex:
            while pending and idx < len(pool):
                batch = []
                k = 0
                while k < len(pending) and len(batch) < WORKERS and idx < len(pool):
                    batch.append((pending[k], pool[idx]))
                    k += 1
                    idx += 1
                futs = {ex.submit(assign_one, p, c): (p, c) for p, c in batch}
                done = []
                for f, (p, c) in futs.items():
                    try:
                        res = f.result()
                    except Exception:
                        res = None
                    if res == "ok":
                        done.append(p)
                for p in done:
                    pending.remove(p)
        if pending:
            stats["unresolved"].extend(p["slug"] for p in pending)
            print(f"  UNRESOLVED {len(pending)}", flush=True)
        print(f"  done ok={stats['ok']} dup_sha={stats['dup_sha']} dup_dh={stats['dup_dh']} failed={stats['failed']}", flush=True)

    manifest.sort(key=lambda m: m["slug"])
    json.dump(manifest, open(os.path.join(PIPE, "manifest.json"), "w", encoding="utf-8"), indent=1)
    with open(os.path.join(PIPE, "update_imageurls.sql"), "w", encoding="utf-8") as fh:
        fh.write("BEGIN TRANSACTION;\n")
        fh.write("\n".join(sql) + "\n")
        fh.write("COMMIT;\n")
    print(f"manifest={len(manifest)} sql={len(sql)} unresolved={len(stats['unresolved'])}")


def main():
    download_all()


if __name__ == "__main__":
    main()
