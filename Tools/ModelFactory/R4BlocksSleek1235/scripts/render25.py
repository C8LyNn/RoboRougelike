"""#1235 - preview renders of the REAL GLB files with three.js r128 in headless Chrome (SwiftShader), same renderer and look as the #1232 pilot.
These are viewer screenshots, not Blender renders and not a Unity import. Blender renders of every block are in A_Sleek/*/renders.
usage: python3 render25.py [blocks] [catalog] [robot:<name>] [game:<name>] [garage:<name>]"""
import os, sys, tempfile
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import paths25, layout25 as L, scenes25
from render_shot import Renderer

CMP = os.path.join(paths25.PKG, "compare"); os.makedirs(CMP, exist_ok=True)
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
    root = tempfile.mkdtemp(prefix="r4_1235_web_")
    os.symlink(paths25.PKG, os.path.join(root, "pkg")); os.symlink(paths25.PILOT, os.path.join(root, "pilot")); os.symlink(paths25.REF, os.path.join(root, "ref"))
    os.symlink(os.path.join(paths25.PILOT, "scripts", "render_web"), os.path.join(root, "web")); return root
def T(x, y, z): return [1, 0, 0, x, 0, 1, 0, y, 0, 0, 1, z, 0, 0, 0, 1]
VLAB = {"before": "PRZED — zatwierdzone #963", "A": "PO — A „Sleek”"}
WAVE_T = {1: "fala 1 — wypukłe / proste", 2: "fala 2 — obłe", 3: "fala 3 — wklęsłe", 4: "fala 4 — tunel, łuk wklęsły, kratownica"}
EMBER = scenes25.LIVERY["ember"]["hull"]

def block_scene(kind, variant, paint=None, w=520, h=440):
    cells = np.array(L.INFO[kind]["cells"], float)*0.55; lo, hi = cells.min(0) - 0.26, cells.max(0) + 0.26; c = (lo + hi)/2; ext = float(np.linalg.norm(hi - lo))
    d = np.array([0.62, 0.46, 0.64]); d = d/np.linalg.norm(d); part = dict(url=scenes25.block_url(variant, kind), matrix=T(0, 0, 0))
    if paint: part["paint"] = {"Armor_Gunmetal": paint["R4P_Base"]} if variant == "before" else paint
    return dict(parts=[part], width=w, height=h, target=[float(c[0]), float(c[1]) - 0.02, float(c[2])], camera=dict(pos=(c + d*(ext*2.35 + 0.35)).tolist(), fov=22),
                groundY=float(lo[1]) - 0.002, shadowExtent=ext*0.9 + 0.4, **scenes25.LOOK)

def do_blocks(R):
    os.makedirs(os.path.join(CMP, "blocks"), exist_ok=True)
    for w, kinds in L.WAVES.items():
        rows = []
        for i in range(0, len(kinds), 4):
            row = []
            for kind in kinds[i:i + 4]:
                for v in ("before", "A"):
                    p = os.path.join(CMP, "blocks", f"R4B_{kind}_{v}.jpg"); R.render(block_scene(kind, v), p, ss=2)
                    row.append(label(Image.open(p).resize((390, 330), Image.LANCZOS), f"{kind} · {'#963' if v == 'before' else 'A'}", 16, 6))
            rows.append(row)
        board(rows, os.path.join(CMP, f"wave{w}_blocks_before_after.jpg"), title=f"#1235 — {WAVE_T[w]}: zatwierdzone #963 → A „Sleek” (ta sama kamera, światło, domyślne kolory R41)")

def do_catalog(R):
    cells = []
    for kind in L.ALL30:
        p = os.path.join(CMP, "blocks", f"R4B_{kind}_A_paint.jpg"); R.render(block_scene(kind, "A", EMBER, 440, 380), p, ss=2)
        cells.append(label(Image.open(p).resize((330, 285), Image.LANCZOS), kind + (" · pilot" if kind in L.PILOT else ""), 15, 6))
    board([cells[i:i + 6] for i in range(0, 30, 6)], os.path.join(CMP, "catalog_30_A_Sleek.jpg"), title="#1235 — katalog 30 klocków A „Sleek” (5 z pilota #1234 + 25 nowych), malowanie przykładowe")

LIV = {"R1_GlacisRover": "ember", "R2_OutriggerSkiff": "jade", "R3_Leviathan": "cobalt"}
def do_robot(R, rname):
    Rb = L.ROBOTS[rname](); liv = LIV[rname]; os.makedirs(os.path.join(CMP, "robots"), exist_ok=True)
    for setname, lv, views in (("paint", liv, ("34front", "side", "34rear")), ("form", "r41", ("34front",))):
        for view in views:
            ims = []
            for v in ("before", "A"):
                p = os.path.join(CMP, "robots", f"{rname}_{view}_{v}_{lv}.jpg")
                info = R.render(scenes25.robot_scene(Rb, v, lv, view, width=1280, height=800), p, ss=2)
                ims.append(label(Image.open(p), f"{VLAB[v]} · {'domyślne materiały R41' if lv == 'r41' else 'malowanie „' + lv.capitalize() + '”'} · {info['tris']:,} tri".replace(",", " ")))
            board([[im.resize((1120, 700), Image.LANCZOS) for im in ims]], os.path.join(CMP, f"{rname}_{setname}_{view}_before_A.jpg"), title=f"#1235 — {Rb.title}: ten sam układ, kamera i światło ({view})")

GARAGE = {"R1_GlacisRover": (0.6, 2.0, 1.5), "R2_OutriggerSkiff": (1.5, 1.2, 0.5), "R3_Leviathan": (1.2, 3.2, -1.5)}
def do_garage(R, rname):
    Rb = L.ROBOTS[rname](); liv = LIV[rname]; ims = []; os.makedirs(os.path.join(CMP, "garage"), exist_ok=True)
    for v in ("before", "A"):
        p = os.path.join(CMP, "garage", f"{rname}_garage_{v}.jpg")
        R.render(scenes25.robot_scene(Rb, v, liv, "garage", focus=GARAGE[rname], width=1200, height=820), p, ss=2); ims.append(label(Image.open(p), VLAB[v]))
    board([[im.resize((1050, 718), Image.LANCZOS) for im in ims]], os.path.join(CMP, f"{rname}_garage_close_before_A.jpg"), title=f"#1235 — {rname}: zbliżenie garażowe (ten sam kadr)")

def do_game(R, rname):
    Rb = L.ROBOTS[rname](); liv = LIV[rname]; ims, small = [], []; os.makedirs(os.path.join(CMP, "gameplay"), exist_ok=True)
    for v, lod in (("before", False), ("A", False), ("A", True)):
        tag = v + ("_LOD1" if lod else "")
        p = os.path.join(CMP, "gameplay", f"{rname}_game_1280x720_{tag}.jpg")
        info = R.render(scenes25.robot_scene(Rb, v, liv, "game", lod=lod, width=1280, height=720), p, ss=1)                      # native 1280x720, no supersampling
        p2 = os.path.join(CMP, "gameplay", f"{rname}_game_256px_{tag}.png"); R.render(scenes25.robot_scene(Rb, v, liv, "game", lod=lod, width=256, height=144), p2, ss=1)
        ims.append(label(Image.open(p).copy(), f"{VLAB[v]}{' · LOD1' if lod else ''} · 1280×720 · {info['tris']:,} tri".replace(",", " "), 22)); small.append(Image.open(p2))
    board([[im.resize((853, 480), Image.LANCZOS) for im in ims], [s.resize((853, 480), Image.NEAREST) for s in small]], os.path.join(CMP, f"{rname}_gameplay_scale_before_A_LOD1.jpg"),
          title=f"#1235 — {rname} w skali rozgrywki: góra 1280×720 bez nadpróbkowania, dół miniatura 256×144 px (powiększona bez wygładzania)")

if __name__ == "__main__":
    what = sys.argv[1:]; R = Renderer(server_root())
    for w in what:
        if w == "blocks": do_blocks(R)
        elif w == "catalog": do_catalog(R)
        elif w.startswith("robot:"): do_robot(R, w[6:])
        elif w.startswith("garage:"): do_garage(R, w[7:])
        elif w.startswith("game:"): do_game(R, w[5:])
        print("done", w, flush=True)
    R.close(); print("renders done")
