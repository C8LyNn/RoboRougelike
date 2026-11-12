"""post-export hygiene for a variants / renders folder: removes local absolute paths of the build machine from FBX texture references (same-length
byte substitution), from JPG metadata (re-encode) and from text logs, then refreshes the file hashes in *_QA.json. usage: python3 pscrub.py DIR USERNAME"""
import os, sys, json, hashlib, re
from PIL import Image
def main(root, user):
    ub = user.encode(); n = dict(jpg=0, fbx=0, text=0, qa=0)
    for d, _, files in os.walk(root):
        for fn in files:
            p = os.path.join(d, fn); b = open(p, "rb").read()
            if ub not in b: continue
            if fn.lower().endswith(".jpg"): Image.open(p).convert("RGB").save(p, quality=90); n["jpg"] += 1
            elif fn.lower().endswith(".fbx"): open(p, "wb").write(b.replace(ub, b"u"*len(ub))); n["fbx"] += 1
            elif fn.lower().endswith((".json", ".log", ".md", ".csv", ".txt")):
                t = b.decode("utf-8", "replace"); t = re.sub(r"[A-Za-z]:[\\/]+Users[\\/]+" + re.escape(user) + r"[\\/]+[^\\/\s\"']+", "<WORKDIR>", t).replace(user, "<user>"); open(p, "w", encoding="utf-8").write(t); n["text"] += 1
    for d, _, files in os.walk(root):
        for fn in files:
            if not fn.endswith("_QA.json"): continue
            qp = os.path.join(d, fn); q = json.load(open(qp, encoding="utf-8")); F = {}
            for f in sorted(os.listdir(d)):
                fp = os.path.join(d, f)
                if os.path.isfile(fp) and not f.endswith(".json"): F[f] = {"sha256": hashlib.sha256(open(fp, "rb").read()).hexdigest(), "bytes": os.path.getsize(fp)}
            q["files"] = F; q["post_export_note"] = "after the Blender step: local absolute paths removed from FBX texture references (same-length byte substitution) and from JPG metadata; the hashes describe the delivered files"
            if os.path.isdir(os.path.join(d, "renders")): q["renders"] = sorted(os.listdir(os.path.join(d, "renders")))
            json.dump(q, open(qp, "w", encoding="utf-8"), indent=1); n["qa"] += 1
    left = sum(1 for d, _, files in os.walk(root) for fn in files if not fn.endswith(".blend") and ub in open(os.path.join(d, fn), "rb").read()); print("scrubbed", n, "files still containing the name:", left)
if __name__ == "__main__": main(sys.argv[1], sys.argv[2])
