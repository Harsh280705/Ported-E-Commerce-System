"""Fetch candidate pools for late-added types."""
import json
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from search_assign import search_files, imageinfo, TYPE_QUERIES

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(HERE)))
PIPE = os.path.join(ROOT, "pipeline")

for t in ["Bestseller", "Prep Guide"]:
    cands = json.load(open(os.path.join(PIPE, "candidates.json"), encoding="utf-8"))
    titles = search_files(TYPE_QUERIES[t], limit=80)
    time.sleep(3)
    files = imageinfo(titles)
    cands[t] = {"query": TYPE_QUERIES[t], "need": 1, "files": files}
    json.dump(cands, open(os.path.join(PIPE, "candidates.json"), "w", encoding="utf-8"), indent=1)
    print(t, len(files), flush=True)
    time.sleep(3)
