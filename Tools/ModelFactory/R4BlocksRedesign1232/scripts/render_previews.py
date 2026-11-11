"""#1232 pilot - preview renders of the REAL GLB files with three.js r128 in headless Chrome (SwiftShader).
These are viewer screenshots (same engine as preview/index.html), not Blender renders and not a Unity import.
usage: python3 render_previews.py [blocks] [robots] [game] [close]"""
import os, sys, tempfile, json
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import paths, layout, scenes, shapes
from render_shot import Renderer

CMP = os.path.join(paths.PKG, "compare"); os.makedirs(CMP, exist_ok=True)
FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
def font(s): return ImageFont.truetype(FONT, s)
def label(im, text, size=26, pad=10):
    d = ImageDraw.Draw(im, "RGBA"); f = font(size); w = d.textlength(text, font=f)
    d.rectangle([0, 0, w + 2*pad, size + 2*pad], fill=(12, 14, 18, 200)); d.text((pad, pad - 2), text, font=f, fill=(240, 242, 245, 255)); return im
def board(rows, path, gap=6, bg=(12, 14, 18), title=None):
    w = max(sum(im.width for im in r) + gap*(len(r) - 1) for r in rows); th = 54 if title else 0
    h = sum(max(im.height for im in r) for r in rows) + gap*(len(rows) - 1) + th
    B = Image.new("RGB", (w, h), bg); y = th
    if title: ImageDraw.Draw(B).text((14, 12), title, font=font(28), fill=(240, 242, 245))
    for r in rows:
        x = 0
        for im in r: B.paste(im, (x, y)); x += im.width + gap
        y += max(im.height for im in r) + gap
    B.save(path, quality=90); return B

def server_root():
    root = tempfile.mkdtemp(prefix="r4_1232_web_")
    os.symlink(paths.PKG, os.path.join(root, "pkg")); os.symlink(paths.REF, os.path.join(root, "ref")); os.symlink(os.path.join(paths.HERE, "render_web"), os.path.join(root, "web"))
    return root

VLAB = {"before": "PRZED — zatwierdzone #963", "A": "PO — A „Sleek”", "B": "PO — B „Industrial”"}
def T(x, y, z): return [1, 0, 0, x, 0, 1, 0, y, 0, 0, 1, z, 0, 0, 0, 1]

def do_blocks(R):
    cells = {}
    for kind in shapes.PILOT:
        for v in ("before", "A", "B"):
            sc = dict(parts=[dict(url=scenes.block_url(v, kind), matrix=T(0, 0, 0))], width=520, height=440, target=[0, -0.02, 0], camera=dict(pos=[1.55, 1.15, 1.9], fov=22),
                      groundY=-0.262, shadowExtent=1.2, **scenes.LOOK)
            p = os.path.join(CMP, "blocks", f"R4B_{kind}_{v}.jpg"); R.render(sc, p, ss=2); cells[(kind, v)] = Image.open(p)
    for v in ("before", "A", "B"):
        for kind in shapes.PILOT: label(cells[(kind, v)], f"{kind} · {VLAB[v].split(' — ')[1] if v != 'before' else '#963'}", 18, 7)
    board([[cells[(k, "A")] for k in shapes.PILOT], [cells[(k, "B")] for k in shapes.PILOT]], os.path.join(CMP, "contact_sheet_AB_2x5.jpg"),
          title="#1232 — wybór kierunku: góra A „Sleek”, dół B „Industrial” (ta sama kamera, światło, domyślne kolory R41)")
    board([[cells[(k, v)] for k in shapes.PILOT] for v in ("before", "A", "B")], os.path.join(CMP, "blocks_before_after_3x5.jpg"),
          title="#1232 — klocki przed / po: zatwierdzone #963 · A · B (ta sama skala, kamera i światło)")

SETS = {"R1_GlacisRover": [("form", [("before", "r41"), ("A", "r41"), ("B", "r41")]), ("paint", [("before", "ember"), ("A", "ember"), ("B", "steel")]), ("paint2", [("before", "cobalt"), ("A", "cobalt"), ("B", "bone")])],
        "R2_OutriggerSkiff": [("form", [("before", "r41"), ("A", "r41"), ("B", "r41")]), ("paint", [("before", "jade"), ("A", "jade"), ("B", "bone")])]}
LIVLAB = {"r41": "domyślne materiały R41", "ember": "malowanie „Ember”", "cobalt": "malowanie „Cobalt”", "jade": "malowanie „Jade”", "steel": "malowanie „Steel”", "bone": "malowanie „Bone”"}

def do_robots(R, views=("34front", "side", "34rear")):
    for rname, fn in layout.ROBOTS.items():
        Rb = fn()
        for setname, combos in SETS[rname]:
            for view in views:
                if setname == "paint2" and view != "34front": continue
                ims = []
                for v, liv in combos:
                    p = os.path.join(CMP, "robots", f"{rname}_{view}_{v}_{liv}.jpg")
                    info = R.render(scenes.robot_scene(Rb, v, liv, view, width=1280, height=800), p, ss=2)
                    ims.append(label(Image.open(p), f"{VLAB[v]} · {LIVLAB[liv]} · {info['tris']:,} tri".replace(",", " ")))
                board([[im.resize((960, 600), Image.LANCZOS) for im in ims]], os.path.join(CMP, f"{rname}_{setname}_{view}_before_A_B.jpg"),
                      title=f"#1232 — {rname}: ten sam robot, kamera i światło ({view})")

def do_game(R):
    for rname, fn in layout.ROBOTS.items():
        Rb = fn(); liv = {"before": "ember", "A": "ember", "B": "steel"} if rname.startswith("R1") else {"before": "jade", "A": "jade", "B": "bone"}
        ims, small = [], []
        for v in ("before", "A", "B"):
            p = os.path.join(CMP, "gameplay", f"{rname}_game_1280x720_{v}.jpg")
            R.render(scenes.robot_scene(Rb, v, liv[v], "game", width=1280, height=720), p, ss=1); im = Image.open(p)     # ss=1: native 1280x720, no supersampling
            p2 = os.path.join(CMP, "gameplay", f"{rname}_game_256px_{v}.png")
            R.render(scenes.robot_scene(Rb, v, liv[v], "game", width=256, height=144), p2, ss=1)
            ims.append(label(im.copy(), f"{VLAB[v]} · 1280×720 bez nadpróbkowania")); small.append(Image.open(p2))
        board([[im.resize((853, 480), Image.LANCZOS) for im in ims], [s.resize((853, 480), Image.NEAREST) for s in small]], os.path.join(CMP, f"{rname}_gameplay_scale_before_A_B.jpg"),
              title=f"#1232 — {rname} w skali rozgrywki: góra 1280×720, dół miniatura 256×144 px (powiększona bez wygładzania)")
        if rname.startswith("R1"):
            p = os.path.join(CMP, "gameplay", f"{rname}_game_1280x720_A_LOD1.jpg")
            info = R.render(scenes.robot_scene(Rb, "A", "ember", "game", lod=True, width=1280, height=720), p, ss=1)
            board([[label(Image.open(os.path.join(CMP, "gameplay", f"{rname}_game_1280x720_A.jpg")), "A · LOD0"), label(Image.open(p), f"A · LOD1 ({info['tris']:,} tri w scenie)".replace(",", " "))]],
                  os.path.join(CMP, f"{rname}_gameplay_LOD0_vs_LOD1.jpg"), title="#1232 — LOD0 vs LOD1 w skali rozgrywki (1280×720)")

def do_close(R):
    Rb = layout.rover(); ims = []
    for v, liv in (("before", "ember"), ("A", "ember"), ("B", "steel")):
        p = os.path.join(CMP, "closeup", f"R1_closeup_{v}.jpg")
        R.render(scenes.robot_scene(Rb, v, liv, "close", width=1100, height=820), p, ss=2); ims.append(label(Image.open(p), VLAB[v]))
    board([[im.resize((825, 615), Image.LANCZOS) for im in ims]], os.path.join(CMP, "R1_closeup_panel_continuity_before_A_B.jpg"), title="#1232 — zbliżenie: ciągłość paneli, krawędzie, powtarzalność (ten sam kadr)")
    # tiling wall: 6 x 4 cubes + wedge row + half-block cap, per-variant
    W = layout.Robot("wall")
    for x in range(-3, 3):
        for y in range(0, 3): W.block("Cube", (x, y, 0), paint="accent" if (x in (-1, 0) and y == 1) else "hull")
        W.block("Wedge", (x, 3, 0), {"down": "full", "back": "full"}, paint="hull"); W.block("HalfBlock", (x, 0, 1), {"down": "full"}, paint="belly")
    ims = []
    for v, liv in (("before", "ember"), ("A", "ember"), ("B", "steel")):
        sc = dict(parts=scenes.robot_parts(W, v, liv), width=900, height=820, target=[-0.28, 0.75, 0.1], camera=dict(pos=[2.6, 1.9, 5.4], fov=24), groundY=-0.262, shadowExtent=3.2, **scenes.LOOK)
        p = os.path.join(CMP, "closeup", f"tiling_wall_{v}.jpg"); R.render(sc, p, ss=2); ims.append(label(Image.open(p), VLAB[v]))
    board([[im.resize((720, 656), Image.LANCZOS) for im in ims]], os.path.join(CMP, "tiling_wall_before_A_B.jpg"), title="#1232 — powtarzalność: ściana 6×3 Cube + rząd Wedge + rząd HalfBlock")

if __name__ == "__main__":
    what = sys.argv[1:] or ["blocks", "robots", "game", "close"]
    R = Renderer(server_root())
    if "blocks" in what: do_blocks(R)
    if "robots" in what: do_robots(R)
    if "game" in what: do_game(R)
    if "close" in what: do_close(R)
    R.close(); print("renders done")
