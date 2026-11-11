"""step 4: boards. (a) wave boards from the BLENDER renders (front / side / 3-4 of every module, default R41 colours);
(b) three.js r128 previews of the REAL exported GLB files (headless Chrome, same renderer and look as #1232 / #1235):
    modules mounted on a deck of approved-style A_Sleek cubes with a sample livery, and the full family catalogue.
(b) are viewer screenshots, not Blender renders and not a Unity import. usage: python3 render_modules.py [boards] [deck] [catalog]"""
import os, sys, tempfile, json
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import mkit as K, paths_m as P, modules as MD
import scenes
from render_shot import Renderer
CMP = os.path.join(P.PKG, "compare"); os.makedirs(CMP, exist_ok=True)
FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
def font(s): return ImageFont.truetype(FONT, s)
def label(im, text, size=18, pad=6):
    d = ImageDraw.Draw(im, "RGBA"); f = font(size); w = d.textlength(text, font=f)
    d.rectangle([0, 0, w + 2*pad, size + 2*pad], fill=(12, 14, 18, 210)); d.text((pad, pad - 2), text, font=f, fill=(240, 242, 245, 255)); return im
def board(rows, path, gap=6, bg=(12, 14, 18), title=None, sub=None):
    w = max(sum(im.width for im in r) + gap*(len(r) - 1) for r in rows); th = (54 if title else 0) + (30 if sub else 0)
    h = sum(max(im.height for im in r) for r in rows) + gap*(len(rows) - 1) + th
    B = Image.new("RGB", (w, h), bg); y = th; d = ImageDraw.Draw(B)
    if title: d.text((14, 12), title, font=font(28), fill=(240, 242, 245))
    if sub: d.text((14, 52), sub, font=font(17), fill=(170, 176, 186))
    for r in rows:
        x = 0
        for im in r: B.paste(im, (x, y)); x += im.width + gap
        y += max(im.height for im in r) + gap
    B.save(path, quality=88); return B
GROUP_T = {"A": "A — podstawowe", "B": "B — ruch i sterowanie", "C": "C — bonusy", "D": "D — walka / energia / obrona", "E": "E — informacja / ekonomia / pasywne", "F": "F — wyrzutniki (wersje na robocie)", "T": "pułapki rozstawiane w świecie"}
WAVE_G = {1: "AB", 2: "CD", 3: "EFT"}
def names(groups): return [n for n in sorted(MD.ALL, key=lambda k: MD.ALL[k]["id"]) if MD.ALL[n]["group"] in groups]
def ID(n): return ("R4D_" if MD.ALL[n]["group"] == "T" else "R4X_") + n
def num(n): d = MD.ALL[n]; return ("T%d" % (d["id"] - 100)) if d["group"] == "T" else "%02d" % d["id"]

def do_boards():
    for w, gs in WAVE_G.items():
        tiles = []
        for n in names(gs):
            i = ID(n); rd = os.path.join(P.mdir(i), "renders"); qa = json.load(open(os.path.join(P.mdir(i), i + "_QA.json")))
            big = Image.open(os.path.join(rd, i + "_34front.jpg")).resize((500, 380), Image.LANCZOS); fr = Image.open(os.path.join(rd, i + "_front.jpg")).resize((249, 189), Image.LANCZOS)
            sd = Image.open(os.path.join(rd, i + "_side.jpg")).resize((249, 189), Image.LANCZOS)
            t = Image.new("RGB", (500, 380 + 2 + 189), (12, 14, 18)); t.paste(big, (0, 0)); t.paste(fr, (0, 382)); t.paste(sd, (251, 382))
            label(t, f"{num(n)} {n}", 18); d = ImageDraw.Draw(t, "RGBA"); txt = f"{qa['triangles']} / {qa['triangles_lod1']} tris · kontakty: {qa['contact_count']}" + (" · anim" if qa["animation"] else "")
            d.rectangle([0, 352, 500, 380], fill=(12, 14, 18, 190)); d.text((8, 357), txt, font=font(15), fill=(200, 205, 214)); d.text((6, 386), "front", font=font(13), fill=(200, 205, 214)); d.text((257, 386), "bok", font=font(13), fill=(200, 205, 214))
            tiles.append(t)
        cols = 4 if len(tiles) <= 16 else 6
        while len(tiles) % cols: tiles.append(Image.new("RGB", (500, 571), (12, 14, 18)))
        board([tiles[k:k + cols] for k in range(0, len(tiles), cols)], os.path.join(CMP, f"wave{w}_board.jpg"), title=f"R4 Sleek — moduły 1×1×1 — fala {w}: " + " + ".join(GROUP_T[g] for g in gs),
              sub="Rendery Blender (EEVEE, to samo światło i kamera co klocki A_Sleek), domyślne kolory R41 + akcent emisyjny. Duży kadr 3/4, poniżej front i bok.")
        print("board", w, len(names(gs)))

def server_root():
    root = tempfile.mkdtemp(prefix="r4x_web_")
    os.symlink(P.PKG, os.path.join(root, "pkg")); os.symlink(P.PILOT, os.path.join(root, "pilot")); os.symlink(os.path.join(P.PILOT, "scripts", "render_web"), os.path.join(root, "web")); return root
def url(i, lod=False): return "/pkg/" + os.path.relpath(P.glb(i, lod), P.PKG).replace(os.sep, "/")
CUBE = "/pilot/A_Sleek/R4B_Cube/R4B_Cube.glb"
def T(x, y, z): return [1, 0, 0, x, 0, 1, 0, y, 0, 0, 1, z, 0, 0, 0, 1]
def paint_for(liv):
    p = scenes.LIVERY[liv]["hull"]; return dict(p) if p else None

def deck_scene(ns, liv="ember", cols=8, w=1900, h=1000):
    """modules standing on (or hanging under / bolted in front of) approved-style A_Sleek cubes: the mount face decides where the cube goes"""
    parts = []; G = 0.55; rows = (len(ns) + cols - 1)//cols; hull = paint_for(liv); acc = scenes.LIVERY[liv]["accent"]
    for k, n in enumerate(ns):
        x = (k % cols)*G*2; z = (k//cols)*G*2.2; d = MD.ALL[n]; i = ID(n)
        if d["group"] == "T":
            parts.append(dict(url=url(i), matrix=T(x, -0.26 - 0.55 + 0.275 + 0.0, z), paint=acc))
            continue
        m = K.build(d["fn"], True); mount = m["ports"][0]["dir"]; n3 = K.DIRS[mount]; c = np.array([n3[0], n3[2], -n3[1]])*G
        parts.append(dict(url=url(i), matrix=T(x, 0, z), paint=acc))
        parts.append(dict(url=CUBE, matrix=T(x + c[0], c[1], z + c[2]), paint=hull))
        if mount != "down":
            for yy in (-0.55,): parts.append(dict(url=CUBE, matrix=T(x + c[0], yy, z + c[2] + (0.55 if mount == "up" else 0)), paint=hull)) if mount != "up" else None
    tx = (min(len(ns), cols) - 1)*G; tz = (rows - 1)*G*1.1; span = max(min(len(ns), cols)*G*2, rows*G*2.2)
    dv = np.array([0.55, 0.62, 0.72]); dv /= np.linalg.norm(dv); dist = span*1.25 + 2.0
    return dict(parts=parts, width=w, height=h, target=[tx, -0.1, tz], camera=dict(pos=[tx + dv[0]*dist, dv[1]*dist, tz + dv[2]*dist], fov=24), groundY=-0.26 - 0.55 + 0.275 - 0.002, shadowExtent=span*0.8 + 1, **scenes.LOOK)

def do_deck(R):
    for w, gs in WAVE_G.items():
        ns = names(gs); liv = {1: "ember", 2: "cobalt", 3: "jade"}[w]; p = os.path.join(CMP, f"wave{w}_on_blocks.jpg")
        info = R.render(deck_scene(ns, liv, cols=8 if len(ns) <= 18 else 9, h=1000 if len(ns) <= 18 else 1250), p, ss=2)
        im = Image.open(p); label(im, f"fala {w}: realne GLB modułów na klockach A_Sleek (malowanie przykładowe, three.js) — {info['tris']} tris w kadrze", 22, 9); im.save(p, quality=90); print("deck", w, info)

def do_catalog(R):
    cells = []
    for n in sorted(MD.ALL, key=lambda k: MD.ALL[k]["id"]):
        i = ID(n); p = os.path.join(CMP, "tiles", i + ".jpg"); d = MD.ALL[n]; env = np.array(d.get("env", [[-0.26]*3, [0.26]*3])); cz = float(env[0][2] + env[1][2])/2
        dv = np.array([0.62, 0.50, 0.64]); dv /= np.linalg.norm(dv)
        sc = dict(parts=[dict(url=url(i), matrix=T(0, 0, 0), paint=paint_for("ember"))], width=400, height=340, target=[0, cz - 0.02, 0], camera=dict(pos=(np.array([0, cz, 0]) + dv*2.25).tolist(), fov=22),
                  groundY=float(env[0][2]) - 0.002, shadowExtent=1.2, **scenes.LOOK)
        R.render(sc, p, ss=2); cells.append(label(Image.open(p).resize((320, 272), Image.LANCZOS), f"{num(n)} {n}", 13, 5))
    cols = 9
    while len(cells) % cols: cells.append(Image.new("RGB", (320, 272), (12, 14, 18)))
    board([cells[k:k + cols] for k in range(0, len(cells), cols)], os.path.join(CMP, "catalog_family.jpg"), title="R4 Sleek — biblioteka modułów 1×1×1: 61 modułów + 6 pułapek rozstawianych",
          sub="Realne pliki GLB w three.js, malowanie przykładowe (strefy Base / Secondary jak w klockach A_Sleek), akcent emisyjny per moduł.")
    print("catalog", len(MD.ALL))

if __name__ == "__main__":
    what = sys.argv[1:] or ["boards", "deck", "catalog"]
    if "boards" in what: do_boards()
    if "deck" in what or "catalog" in what:
        R = Renderer(server_root())
        if "deck" in what: do_deck(R)
        if "catalog" in what: do_catalog(R)
