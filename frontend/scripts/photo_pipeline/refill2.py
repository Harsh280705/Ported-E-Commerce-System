"""Phase 1c: alternate queries for stubborn types via generator=search (1 call each)."""
import json
import os
import time
import urllib.parse
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(HERE)))
PIPE = os.path.join(ROOT, "pipeline")
UA = {"User-Agent": "YantraShop-Training/1.0 (course project; contact: training local)"}

ALT = {
    "Table Lamp": ["lamp", "desk lamp"],
    "Saree": ["sari", "indian saree"],
    "Moisturizer": ["cosmetic cream", "skin cream"],
    "Mouse": ["optical mouse"],
    "Trousers": ["pants trousers", "chino pants"],
    "Serum": ["serum cosmetics", "cosmetic bottle"],
    "Kettle": ["teakettle", "tea kettle"],
    "Cookware Set": ["cooking pot", "saucepan"],
    "Speaker": ["loudspeaker", "speaker cabinet"],
}


def gen_search(query, limit=60):
    params = {"action": "query", "format": "json", "generator": "search",
              "gsrsearch": f"{query} filetype:bitmap", "gsrnamespace": 6,
              "gsrlimit": limit, "prop": "imageinfo",
              "iiprop": "url|size|extmetadata", "iiurlwidth": 640}
    url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode(params)
    req = urllib.request.Request(url, headers=UA)
    with urllib.request.urlopen(req, timeout=90) as r:
        out = json.load(r)
    res = []
    for pid, pg in (out.get("query", {}).get("pages", {}) or {}).items():
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


def main():
    cands = json.load(open(os.path.join(PIPE, "candidates.json"), encoding="utf-8"))
    for t, queries in ALT.items():
        have = len(cands.get(t, {}).get("files", []))
        if have >= 40:
            print(t, "already", have)
            continue
        if t not in cands:
            cands[t] = {"query": queries[0], "need": 0, "files": []}
        seen = {f["title"] for f in cands[t]["files"]}
        for q in queries:
            try:
                files = gen_search(q)
            except Exception as e:
                print(t, q, "FAILED", str(e)[:100], flush=True)
                time.sleep(10)
                continue
            added = [f for f in files if f["title"] not in seen]
            seen.update(f["title"] for f in added)
            cands[t]["files"].extend(added)
            print(f"{t} [{q}]: +{len(added)} total={len(cands[t]['files'])}", flush=True)
            time.sleep(8)
            if len(cands[t]["files"]) >= 60:
                break
    json.dump(cands, open(os.path.join(PIPE, "candidates.json"), "w", encoding="utf-8"), indent=1)
    thin = {t: len(v["files"]) for t, v in cands.items() if len(v["files"]) < 40}
    print("thin types (<40):", thin)


if __name__ == "__main__":
    main()
