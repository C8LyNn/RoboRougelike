"""#1182 preview: a working three.js viewer of the REAL exported GLBs - the 20 connectors and the 5 assemblies in which they are
mounted on the earlier R4 blocks / weapons / movement parts / wheels (not proxies).
usage: python3 build_viewer1182.py <library root> <three.js vendor dir (three.min.js, GLTFLoader.js, OrbitControls.js)> <out dir>
writes <out>/CONNECTORS_1182_PREVIEW/ (offline: GLBs as lazily loaded base64 .js, three.js r128 in lib/), its .zip, and
<out>/artifact/ (same page and GLB .js files, three.js r128 from jsDelivr)."""
import base64, glob, json, os, shutil, sys, zipfile
from PIL import Image, ImageDraw, ImageFont

SRC, VENDOR, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from make_manifests import ORDER, PILOT

SCENE_PL = {"two_robots": "Dwa roboty: pancerz zwykły vs na prętach", "long_mast": "Maszt broni STRUT_LONG", "braced_mast": "Maszt z zastrzałem ROD_DIAG",
            "arc_outrigger": "Wysięgniki STRUT_ARC z kołami (w tym lustro)", "cross_deck": "Pokład STRUT_CROSS + zderzak ROD_FRAME"}


def board(out):
    tiles = []
    for pid in ORDER:
        p = os.path.join(SRC, "R4C_" + pid, "renders", f"R4C_{pid}_board.jpg"); tiles.append((pid, Image.open(p).convert("RGB").resize((350, 350), Image.LANCZOS)))
    cols = 5; rows = 4; S = Image.new("RGB", (350*cols, 350*rows + 40), (38, 40, 44)); d = ImageDraw.Draw(S)
    try: f = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 15); f2 = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 17)
    except Exception: f = f2 = ImageFont.load_default()
    for i, (pid, im) in enumerate(tiles):
        x, y = (i % cols)*350, (i//cols)*350 + 40; S.paste(im, (x, y)); d.text((x + 8, y + 6), pid + ("  [pilot]" if pid in PILOT else ""), fill=(235, 235, 235), font=f)
    d.text((10, 10), "R4C #1182 - 20 łączników w jednej skali (ta sama kamera ortho 4,4 m, siatka 0,55 m)", fill=(240, 240, 240), font=f2)
    S.save(out, quality=86)


def data():
    parts = []
    for pid in ORDER:
        mid = "R4C_" + pid; cm = json.load(open(os.path.join(SRC, mid, mid + "_connector_manifest.json"), encoding="utf-8"))
        mm = json.load(open(os.path.join(SRC, mid, mid + "_model_manifest.json"), encoding="utf-8"))
        parts.append({"id": mid, "pid": pid, "family": cm["family"], "pilot": cm["pilot"], "desc": cm["description"], "cells": cm["occupied_cells"],
                      "ports": [{k: p[k] for k in ("id", "cell", "face", "normal", "position_m", "type")} for p in cm["connection_ports"]],
                      "sockets": {k: v["id"] for k, v in cm["sockets"].items()}, "tris": mm["triangles"], "status": mm["status"],
                      "rot": {k: cm["rotations"]["check"][k] for k in ("PASS", "distinct_orientations", "mirror_equals_a_rotation")},
                      "compat": cm["compat"], "proposal": cm["proposal"], "glb": mid + ".glb", "bytes": os.path.getsize(os.path.join(SRC, mid, mid + ".glb"))})
    scenes = []
    for s in SCENE_PL:
        r = json.load(open(os.path.join(SRC, "assemblies", s, f"ASM_{s}_assembly.json"), encoding="utf-8"))
        models = sorted({v["model"] for v in r["parts"].values()})
        scenes.append({"id": s, "title": SCENE_PL[s], "note": r["note"], "summary": r["summary"], "PASS": r["PASS"], "models": models,
                       "adjacent": r["adjacent_not_connected"], "graph": {g: {"anchor": v["anchor"], "parts": v["parts"], "single_point_links": [b["link"] for b in v["single_point_links"]]}
                                                                          for g, v in r["graph"].items()},
                       "glb": f"ASM_{s}.glb", "bytes": os.path.getsize(os.path.join(SRC, "assemblies", s, f"ASM_{s}.glb"))})
    cmx = json.load(open(os.path.join(SRC, "compat", "compat_all.json"), encoding="utf-8"))
    return {"parts": parts, "scenes": scenes, "standard": cmx["standard_measured"], "partners": len(cmx["partners"])}


PAGE = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "viewer1182_template.html"), encoding="utf-8").read()


def main():
    root = os.path.join(OUT, "CONNECTORS_1182_PREVIEW"); art = os.path.join(OUT, "artifact")
    for d in (root, art):
        if os.path.isdir(d): shutil.rmtree(d)
    os.makedirs(os.path.join(root, "lib")); os.makedirs(os.path.join(root, "glb")); os.makedirs(os.path.join(art, "glb"))
    for f in ("three.min.js", "GLTFLoader.js", "OrbitControls.js"): shutil.copy(os.path.join(VENDOR, f), os.path.join(root, "lib", f))
    D = data(); js = "window.DATA=" + json.dumps(D, ensure_ascii=False, separators=(",", ":")) + ";"
    glbs = [(p["glb"], os.path.join(SRC, p["id"], p["glb"])) for p in D["parts"]] + [(s["glb"], os.path.join(SRC, "assemblies", s["id"], s["glb"])) for s in D["scenes"]]
    for name, p in glbs:
        b = base64.b64encode(open(p, "rb").read()).decode()
        open(os.path.join(root, "glb", name + ".js"), "w").write(f'window.GLB_B64=window.GLB_B64||{{}};window.GLB_B64["{name}"]="{b}";')
        shutil.copy(os.path.join(root, "glb", name + ".js"), os.path.join(art, "glb", name + ".js"))   # artifacts serve no .glb type
    board(os.path.join(root, "board_all20.jpg")); shutil.copy(os.path.join(root, "board_all20.jpg"), os.path.join(art, "board_all20.jpg"))
    local_libs = '<script src="lib/three.min.js"></script><script src="lib/GLTFLoader.js"></script><script src="lib/OrbitControls.js"></script>'
    cdn_libs = ('<script src="https://cdn.jsdelivr.net/npm/three@0.128.0/build/three.min.js"></script>'
                '<script src="https://cdn.jsdelivr.net/npm/three@0.128.0/examples/js/loaders/GLTFLoader.js"></script>'
                '<script src="https://cdn.jsdelivr.net/npm/three@0.128.0/examples/js/controls/OrbitControls.js"></script>')
    open(os.path.join(root, "data.js"), "w", encoding="utf-8").write(js); open(os.path.join(art, "data.js"), "w", encoding="utf-8").write(js)
    open(os.path.join(root, "index.html"), "w", encoding="utf-8").write("<!doctype html>\n<html lang=\"pl\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">"
                                                                         + PAGE.replace("<!--LIBS-->", local_libs).replace("__MODE__", "local") + "</html>\n")
    open(os.path.join(art, "index.html"), "w", encoding="utf-8").write(PAGE.replace("<!--LIBS-->", cdn_libs).replace("__MODE__", "local"))
    open(os.path.join(root, "README.txt"), "w", encoding="utf-8").write(
        "Podgląd #1182: otwórz index.html w przeglądarce (działa offline z pliku, three.js r128 w lib/).\n"
        "Modele to prawdziwe GLB z Tools/ModelFactory/R4ConnectorRods1182 (łączniki i konstrukcje z wcześniejszymi modelami).\n")
    zp = os.path.join(OUT, "CONNECTORS_1182_PREVIEW.zip")
    with zipfile.ZipFile(zp, "w", zipfile.ZIP_DEFLATED) as z:
        for p in sorted(glob.glob(os.path.join(root, "**", "*"), recursive=True)):
            if os.path.isfile(p): z.write(p, os.path.relpath(p, OUT))
    print("viewer", len(glbs), "glbs", os.path.getsize(zp)//1024, "KB zip")


if __name__ == "__main__":
    main()
