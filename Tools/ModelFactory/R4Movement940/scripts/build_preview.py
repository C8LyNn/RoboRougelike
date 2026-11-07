"""Builds the MOVEMENT_11 owner preview from the real exported assets.

Usage:  python3 build_preview.py <R4Movement940 dir> <out dir>
Writes <out>/MOVEMENT_11_PREVIEW/ (index.html, offline.html, models.js, glb/, video/,
manifests/, thumbs/, silhouettes/, renders/, README.txt) and <out>/MOVEMENT_11_PREVIEW.zip.
models.js embeds every GLB as base64 and the viewer parses it in memory
(GLTFLoader.parse), so index.html works from file:// with no fetch and no GitHub API.
"""
import base64, glob, hashlib, io, json, os, shutil, sys, zipfile
from PIL import Image

ORDER = ["Wheel", "CrawlerTrack", "HoverEngine", "OmniDrive", "Suspension", "Booster",
         "JumpDrive", "Blink", "Gyroscope", "MagneticGrapple", "IndependentSteering"]
VIEWS = ["gameplay", "side", "front", "top"]


def sha(p):
    return hashlib.sha256(open(p, "rb").read()).hexdigest()


def img_uri(path, w, fmt="JPEG", sil=False):
    im = Image.open(path)
    if sil:
        im = im.convert("RGBA")
        bg = Image.new("RGBA", im.size, (255, 255, 255, 255))
        bg.alpha_composite(im)
        g = bg.convert("L")
        box = g.point(lambda v: 255 if v < 128 else 0).getbbox()
        if box:
            pad = 6
            box = (max(box[0] - pad, 0), max(box[1] - pad, 0), min(box[2] + pad, g.width), min(box[3] + pad, g.height))
            g = g.crop(box)
        s = max(g.size)
        sq = Image.new("L", (s, s), 255)
        sq.paste(g, ((s - g.width) // 2, (s - g.height) // 2))
        # black silhouette -> transparent PNG with black ink (viewer recolours via CSS background)
        a = sq.point(lambda v: 255 if v < 128 else 0).resize((w, w), Image.LANCZOS)
        out = Image.new("RGBA", (w, w), (0, 0, 0, 0))
        out.putalpha(a)
        im, fmt = out, "PNG"
    else:
        im = im.convert("RGB")
        im.thumbnail((w, w * 2), Image.LANCZOS)
    b = io.BytesIO()
    im.save(b, fmt, quality=82) if fmt == "JPEG" else im.save(b, fmt, optimize=True)
    return ("data:image/jpeg;base64," if fmt == "JPEG" else "data:image/png;base64,") + base64.b64encode(b.getvalue()).decode()


def main(src, out):
    root = os.path.join(out, "MOVEMENT_11_PREVIEW")
    if os.path.isdir(root):
        shutil.rmtree(root)
    for d in ["glb", "video", "manifests", "thumbs", "silhouettes", "renders"]:
        os.makedirs(os.path.join(root, d))
    sil = json.load(open(os.path.join(src, "compare", "silhouette_check.json")))
    models = []
    for k in ORDER:
        mid = "R4M_" + k
        d = os.path.join(src, mid)
        qa = json.load(open(os.path.join(d, mid + "_QA.json")))
        am = json.load(open(os.path.join(d, mid + "_animation_manifest.json")))
        glb = os.path.join(d, mid + ".glb")
        sw = qa["sweep"]
        gc = sw.get("ground_contact") if isinstance(sw.get("ground_contact"), dict) else None
        rd = os.path.join(d, "renders")
        hero = os.path.join(rd, mid + "_HERO_eevee.jpg")
        thumb_src = os.path.join(rd, mid + "_34front.jpg")
        shutil.copy(glb, os.path.join(root, "glb"))
        shutil.copy(os.path.join(d, mid + "_animation_manifest.json"), os.path.join(root, "manifests"))
        shutil.copy(os.path.join(d, mid + "_QA.json"), os.path.join(root, "manifests"))
        shutil.copy(os.path.join(rd, mid + "_timeline.mp4"), os.path.join(root, "video"))
        shutil.copy(hero, os.path.join(root, "renders"))
        for p in sorted(glob.glob(os.path.join(rd, "poses", "*.jpg"))):
            shutil.copy(p, os.path.join(root, "renders"))
        Image.open(thumb_src).convert("RGB").resize((128, 128), Image.LANCZOS).save(os.path.join(root, "thumbs", mid + "_128.png"))
        for v in VIEWS:
            shutil.copy(os.path.join(rd, "silhouette", f"{mid}_sil_{v}.png"), os.path.join(root, "silhouettes"))
        models.append({
            "id": mid, "key": k, "kind": qa["kind"], "game": qa["game_name"],
            "tris": qa["triangles_total"], "status": qa["status"], "unity": "UNITY_NOT_RUN",
            "glb_sha": sha(glb), "glb_bytes": os.path.getsize(glb),
            "fps": am["fps"], "range": am["frame_range"], "clips": am["clips"],
            "nodes": [n for n in qa["contract"] if n not in ("unity_axes",) and not n.endswith("_ROOT")],
            "ground_y": gc.get("runtime_y") if gc else None,
            "ground_dev_mm": gc.get("deviation_mm") if gc else None,
            "housing_in_cell": sw.get("housing_inside_mount_cell"),
            "housing_note": sw.get("housing_policy"),
            "cells": len(sw.get("moving_sweep", {}).get("cells", [])),
            "clash": qa["animation_clash"].get("collisions"),
            "fbx_worst": qa["fbx_reimport"].get("worst"),
            "glb_pass": qa["glb_reimport"].get("PASS"),
            "notes": qa.get("notes", []),
            "thumb": img_uri(thumb_src, 160),
            "hero": img_uri(hero, 640),
            "sil": {v: img_uri(os.path.join(rd, "silhouette", f"{mid}_sil_{v}.png"), 112, sil=True) for v in VIEWS},
            "glb": base64.b64encode(open(glb, "rb").read()).decode(),
        })
    pairs = [{"a": p.split("|")[0], "b": p.split("|")[1], **v} for p, v in sil["most_similar"]]
    data = {"models": models, "similar": pairs, "flagged": sil.get("flagged", []), "method": sil["method"]}
    with open(os.path.join(root, "models.js"), "w") as f:
        f.write("/* generated by build_preview.py from the real R4M_*.glb exports */\nwindow.R4M_DATA=")
        json.dump(data, f, separators=(",", ":"), ensure_ascii=False)
        f.write(";\n")
    here = os.path.dirname(os.path.abspath(__file__))
    for n in ["index.html", "offline.html", "README.txt", "start_preview.bat", "start_preview.sh"]:
        shutil.copy(os.path.join(here, "preview_src", n), root)
    shutil.copytree(os.path.join(here, "preview_src", "lib"), os.path.join(root, "lib"))
    # Artifact variant: same page, three.js from the jsDelivr CDN, no doctype/head wrapper
    src_html = open(os.path.join(root, "index.html"), encoding="utf-8").read()
    cdn = "https://cdn.jsdelivr.net/npm/three@0.128.0/"
    art = (src_html.replace('src="lib/three.min.js"', 'src="' + cdn + 'build/three.min.js"')
           .replace('src="lib/GLTFLoader.js"', 'src="' + cdn + 'examples/js/loaders/GLTFLoader.js"')
           .replace('src="lib/OrbitControls.js"', 'src="' + cdn + 'examples/js/controls/OrbitControls.js"')
           .replace("three.js r128 · lokalnie", "three.js r128 · CDN"))
    head_end = art.index("<title>")
    art = art[head_end:].replace("</head>\n<body>\n", "").replace("</body>\n</html>\n", "")
    os.makedirs(os.path.join(out, "artifact"), exist_ok=True)
    open(os.path.join(out, "artifact", "index.html"), "w", encoding="utf-8").write(art)
    shutil.copy(os.path.join(root, "models.js"), os.path.join(out, "artifact"))
    z = os.path.join(out, "MOVEMENT_11_PREVIEW.zip")
    if os.path.exists(z):
        os.remove(z)
    with zipfile.ZipFile(z, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        for dp, _, fs in os.walk(root):
            for fn in sorted(fs):
                p = os.path.join(dp, fn)
                zi = zipfile.ZipInfo(os.path.relpath(p, out), date_time=(2026, 11, 6, 0, 0, 0))
                zi.compress_type = zipfile.ZIP_DEFLATED
                if fn.endswith(".sh"):
                    zi.external_attr = 0o755 << 16
                zf.writestr(zi, open(p, "rb").read())
    print("models", len(models), "models.js", os.path.getsize(os.path.join(root, "models.js")), "zip", os.path.getsize(z), sha(z))


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
