"""Phase 1b: refill candidate pools for types with too few files."""
import json
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from search_assign import search_files, imageinfo, TYPE_QUERIES

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(HERE)))
PIPE = os.path.join(ROOT, "pipeline")

RETRY = ["Table Lamp", "Saree", "Moisturizer", "Hair Oil", "Mouse", "Trousers",
         "Serum", "Kettle", "Face Wash", "Cookware Set", "Dinner Set", "Yoga Mat"]


def main():
    cands = json.load(open(os.path.join(PIPE, "candidates.json"), encoding="utf-8"))
    for t in RETRY:
        if t not in cands:
            print(t, "not in candidates, skipping")
            continue
        have = len(cands[t]["files"])
        if have >= 80:
            print(t, "already", have)
            continue
        q = TYPE_QUERIES[t]
        titles = search_files(q, limit=100)
        time.sleep(3)
        files = imageinfo(titles) if titles else []
        seen = {f["title"] for f in cands[t]["files"]}
        added = [f for f in files if f["title"] not in seen]
        cands[t]["files"].extend(added)
        print(f"{t}: had={have} added={len(added)} total={len(cands[t]['files'])}", flush=True)
        time.sleep(3)
    json.dump(cands, open(os.path.join(PIPE, "candidates.json"), "w", encoding="utf-8"), indent=1)
    thin = {t: len(v["files"]) for t, v in cands.items() if len(v["files"]) < 40}
    print("thin types (<40):", thin)


if __name__ == "__main__":
    main()
