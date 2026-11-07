"""Builds the BLOCKS owner preview from the real exported R4B_*.glb blocks and the compat vehicle.

Usage:  python3 build_preview_blocks.py <R4Blocks dir> <out dir>
Writes <out>/BLOCKS_PREVIEW/, <out>/BLOCKS_PREVIEW.zip and <out>/artifact/ (CDN variant of the page).
Blocks are static: each gets one pseudo clip "Static" (frame 1). GLBs are embedded as base64 in models.js.
"""
import base64, hashlib, io, json, os, shutil, sys, zipfile
from PIL import Image

ORDER = ["Cube", "Wedge", "Tetra", "InnerCorner", "CornerWedge", "RoundEdge", "RoundCorner", "HalfBlock",
         "HalfWedge", "Cylinder", "Beam2", "Beam3", "LongWedge", "Plate2x2", "TrussCube",
         "Pyramid", "Dome", "Cone", "HexColumn", "OctColumn", "RoundTop", "Arch", "Step", "CoveEdge",
         "CornerL3", "Tee4", "Slab2x2", "BigCube2", "Plate1x2", "WideWedge"]


def sha(p):
    return hashlib.sha256(open(p, "rb").read()).hexdigest()


def img_uri(path, w):
    im = Image.open(path).convert("RGB"); im.thumbnail((w, w * 2), Image.LANCZOS)
    b = io.BytesIO(); im.save(b, "JPEG", quality=82); return "data:image/jpeg;base64," + base64.b64encode(b.getvalue()).decode()


def glb_tris(p):
    import struct
    b = open(p, "rb").read(); n = struct.unpack("<I", b[12:16])[0]; j = json.loads(b[20:20 + n])
    return sum(j["accessors"][pr["indices"]]["count"] // 3 for m in j["meshes"] for pr in m["primitives"] if "indices" in pr)


def entry(mid, key, game, style, tris, glb, thumb, poses, cellsN, ports, nodes, notes, status="BLENDER_PASS"):
    t = img_uri(thumb, 160)
    return {"id": mid, "key": key, "kind": "Block", "game": game, "version": "", "style": style, "tris": tris, "status": status,
            "unity": "UNITY_NOT_RUN", "glb_sha": sha(glb), "glb_bytes": os.path.getsize(glb), "fps": 24, "range": [1, 1],
            "clips": [{"name": "Static", "start": 1, "end": 1, "loop": True}], "nodes": nodes, "ground_y": None, "ground_dev_mm": None,
            "housing_in_cell": True, "housing_note": None, "cells": ports, "cellsN": cellsN, "clash": 0,
            "fbx_worst": {"geom_mm": 0.0}, "glb_pass": True, "notes": notes, "poses": poses, "thumb": t, "hero": t,
            "sil": {v: t for v in ("gameplay", "side", "front", "top")}, "glb": base64.b64encode(open(glb, "rb").read()).decode()}


def main(src, out):
    root = os.path.join(out, "BLOCKS_PREVIEW")
    if os.path.isdir(root): shutil.rmtree(root)
    for d in ["glb", "manifests", "thumbs", "renders", "compare"]: os.makedirs(os.path.join(root, d))
    models = []
    for k in ORDER:
        mid = "R4B_" + k; d = os.path.join(src, mid); q = json.load(open(os.path.join(d, mid + "_QA.json"), encoding="utf-8"))
        glb = os.path.join(d, mid + ".glb"); shutil.copy(glb, os.path.join(root, "glb"))
        shutil.copy(os.path.join(d, mid + "_QA.json"), os.path.join(root, "manifests"))
        poses = []
        for v in ("34front", "34rear", "under"):
            f = f"{mid}_{v}.jpg"; shutil.copy(os.path.join(d, "renders", f), os.path.join(root, "renders")); poses.append(f)
        th = os.path.join(d, "renders", mid + "_34front.jpg"); t = Image.open(th).convert("RGB")
        t.crop(((t.width - t.height)//2, 0, (t.width + t.height)//2, t.height)).resize((128, 128), Image.LANCZOS).save(os.path.join(root, "thumbs", mid + "_128.png"))
        full = sum(1 for p in q["ports"] if p["kind"] == "full")
        models.append(entry(mid, k, q["description"], f"ContactMask (komórka kotwicy) = {q['contact_mask_local_anchor']} · porty pełne {full}, częściowe {len(q['ports']) - full}",
                            q["mesh"]["triangles"], glb, th, poses, len(q["cells"]), len(q["ports"]),
                            ["Socket_Mount"] + sorted({f"Port_{p['dir']}" for p in q["ports"]}), [q["note"]] if q.get("note") else []))
    cj = json.load(open(os.path.join(src, "compat", "compat_assembly.json"), encoding="utf-8"))
    cg = os.path.join(src, "compat", "R4_Compat_Vehicle.glb"); shutil.copy(cg, os.path.join(root, "glb"))
    for v in ("34front", "side", "34rear"): shutil.copy(os.path.join(src, "compat", f"compat_assembly_{v}.jpg"), os.path.join(root, "compare"))
    shutil.copy(os.path.join(src, "compat", "compat_assembly.json"), os.path.join(root, "compare"))
    models.append(entry("R4_Compat_Vehicle", "Pojazd testowy", "klocki + R4W_Basic (PR #939) + 4× R4M_Wheel i R4M_Booster (PR #941)",
                        f"test zgodności: {cj['n_pairs']} par portów, wynik {'PASS' if cj.get('PASS') else 'FAIL'}",
                        glb_tris(cg), cg, os.path.join(src, "compat", "compat_assembly_34front.jpg"),
                        [], len(cj['layout']), cj['n_pairs'], ["statyczny GLB, bez animacji"],
                        ["Złożenie testowe: prawdziwe GLB broni i ruchu z PR #939 / #941 osadzone na portach klocków."],
                        "PASS" if cj.get("PASS") else "FAIL"))
    m = models[-1]; m["poses"] = []
    for v in ("34front", "side", "34rear"):
        shutil.copy(os.path.join(src, "compat", f"compat_assembly_{v}.jpg"), os.path.join(root, "renders", f"R4_Compat_Vehicle_{v}.jpg")); m["poses"].append(f"R4_Compat_Vehicle_{v}.jpg")
    data = {"models": models[:-1], "similar": [], "flagged": [], "method": "n/d (klocki)"}
    with open(os.path.join(root, "models.js"), "w", encoding="utf-8") as f:   # split in two files: each stays well under 16 MB
        f.write("/* generated by build_preview_blocks.py from the real R4B_*.glb exports */\nwindow.R4M_DATA=")
        json.dump(data, f, separators=(",", ":"), ensure_ascii=False); f.write(";\n")
    with open(os.path.join(root, "models_compat.js"), "w", encoding="utf-8") as f:
        f.write("/* compat test vehicle (static GLB) */\nif (window.R4M_DATA) window.R4M_DATA.models.push(")
        json.dump(models[-1], f, separators=(",", ":"), ensure_ascii=False); f.write(");\n")
    here = os.path.dirname(os.path.abspath(__file__))
    for n in ["index.html", "offline.html", "README.txt", "start_preview.bat", "start_preview.sh"]: shutil.copy(os.path.join(here, "preview_src", n), root)
    shutil.copytree(os.path.join(here, "preview_src", "lib"), os.path.join(root, "lib"))
    html = open(os.path.join(root, "index.html"), encoding="utf-8").read(); cdn = "https://cdn.jsdelivr.net/npm/three@0.128.0/"
    art = (html.replace('src="lib/three.min.js"', 'src="' + cdn + 'build/three.min.js"')
           .replace('src="lib/GLTFLoader.js"', 'src="' + cdn + 'examples/js/loaders/GLTFLoader.js"')
           .replace('src="lib/OrbitControls.js"', 'src="' + cdn + 'examples/js/controls/OrbitControls.js"')
           .replace("three.js r128 · lokalnie", "three.js r128 · CDN"))
    art = art[art.index("<title>"):].replace("</head>\n<body>\n", "").replace("</body>\n</html>\n", "")
    os.makedirs(os.path.join(out, "artifact"), exist_ok=True)
    open(os.path.join(out, "artifact", "index.html"), "w", encoding="utf-8").write(art)
    shutil.copy(os.path.join(root, "models.js"), os.path.join(out, "artifact"))
    shutil.copy(os.path.join(root, "models_compat.js"), os.path.join(out, "artifact"))
    z = os.path.join(out, "BLOCKS_PREVIEW.zip")
    if os.path.exists(z): os.remove(z)
    with zipfile.ZipFile(z, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        for dp, _, fs in os.walk(root):
            for fn in sorted(fs):
                p = os.path.join(dp, fn); zi = zipfile.ZipInfo(os.path.relpath(p, out), date_time=(2026, 11, 7, 0, 0, 0))
                zi.compress_type = zipfile.ZIP_DEFLATED
                if fn.endswith(".sh"): zi.external_attr = 0o755 << 16
                zf.writestr(zi, open(p, "rb").read())
    print("models", len(models), "models.js", os.path.getsize(os.path.join(root, "models.js")), "zip", os.path.getsize(z), sha(z))


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
