"""#1239 pilot 2: review boards for the artistic acceptance + silhouette metrics. Works on the Blender renders of pilot2/variants.
 step "prep":   clay PNG (transparent) -> <Id>_clay_<view>.jpg + 1-bit black silhouette <Id>_sil_<view>.png (the PNG is removed)
 step "boards": pilot2/boards/<NN>_<Type>.jpg - five variants in the IDENTICAL camera: full materials 3/4, front, side; neutral material;
                wireframe; black silhouettes front / side / 3-4; four frames of the real animation.  00_overview_*.jpg: all 8 types.
                pilot2/compare/silhouette_metrics.json: pairwise silhouette IoU per view, next to the IoU of the previous (parametric) atlas.
usage: python3 pboards.py [prep] [boards]"""
import os, sys, json, textwrap, itertools
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import astyle as ST, pstyle as PS
PKG = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); P2 = os.path.join(PKG, "pilot2"); VAR = os.path.join(P2, "variants"); OUT = os.path.join(P2, "boards")
FB = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"; FR = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
def fb(s): return ImageFont.truetype(FB, s)
def fr(s): return ImageFont.truetype(FR, s)
ORDER = ["Cube", "Strut", "Gun", "Wheel", "Hover", "Battery", "Deployer", "Trap"]
CID = dict(Cube="R4B_Cube", Strut="R4C_STRUT_STANDARD", Gun="R4W_Basic", Wheel="R4M_WheelM_Side_Offroad", Hover="R4M_HoverEngine", Battery="R4X_Battery", Deployer="R4X_ExplosiveTrapDeployer", Trap="R4D_ExplosiveTrap")
SNAME = dict(A="A · Sleek Futuristic", B="B · Brutal Industrial", C="C · Arcade", D="D · Scrap Wasteland", E="E · Heroic Toon")
VIEWPL = {"front": "przód", "side": "bok", "34": "3/4"}
def rdir(s, cid): return os.path.join(VAR, ST.LABEL[s], cid, "renders")
def prep():
    n = 0
    for s in ST.STYLES:
        for cid in CID.values():
            d = rdir(s, cid)
            for v in ("front", "side", "34"):
                p = os.path.join(d, f"{cid}_clay_{v}.png")
                if not os.path.exists(p): continue
                im = Image.open(p).convert("RGBA"); a = np.array(im)[..., 3]; bg = Image.new("RGB", im.size, (232, 233, 235)); bg.paste(im, mask=im.split()[3]); bg.save(os.path.join(d, f"{cid}_clay_{v}.jpg"), quality=88)
                Image.fromarray(np.where(a > 96, 0, 255).astype(np.uint8)).convert("1").save(os.path.join(d, f"{cid}_sil_{v}.png"), optimize=True); os.remove(p); n += 1
    print("prep:", n, "clay renders converted")
def mask(s, cid, v): return np.array(Image.open(os.path.join(rdir(s, cid), f"{cid}_sil_{v}.png")).convert("L")) < 128
def metrics():
    prev = json.load(open(os.path.join(PKG, "compare", "style_metrics.json")))["assets"]; out = {}
    for t in ORDER:
        cid = CID[t]; r = {}
        for v in ("front", "side", "34"):
            M = {s: mask(s, cid, v) for s in ST.STYLES}; r[v] = {a + b: round(float((M[a] & M[b]).sum()/max(1, (M[a] | M[b]).sum())), 3) for a, b in itertools.combinations(ST.STYLES, 2)}
        allv = [x for v in r.values() for x in v.values()]; qa = {s: json.load(open(os.path.join(VAR, ST.LABEL[s], cid, cid + "_QA.json"))) for s in ST.STYLES}
        out[t] = dict(id=cid, silhouette_iou=r, mean_iou_34=round(float(np.mean(list(r["34"].values()))), 3), max_iou_34=max(r["34"].values()), mean_iou_all_views=round(float(np.mean(allv)), 3), max_iou_all_views=max(allv),
                      previous_atlas_mean_iou_34=prev.get(cid, {}).get("mean_iou"), previous_atlas_max_iou_34=prev.get(cid, {}).get("max_iou"),
                      triangles={s: qa[s]["triangles"] for s in ST.STYLES}, triangles_lod1={s: qa[s]["triangles_lod1"] for s in ST.STYLES}, parts={s: qa[s].get("parts") for s in ST.STYLES}, mesh_objects={s: qa[s]["mesh_objects"] for s in ST.STYLES})
    dev = [t for t in ORDER if t != "Cube"]
    summ = dict(note="IoU of black silhouettes rendered in the identical camera (1 = identical outline). The cube shares the mount-standard outline on purpose. A metric is evidence, not the artistic acceptance.",
                mean_iou_34_devices=round(float(np.mean([out[t]["mean_iou_34"] for t in dev])), 3), previous_atlas_mean_iou_34_devices=round(float(np.mean([out[t]["previous_atlas_mean_iou_34"] for t in dev])), 3),
                mean_iou_34_all=round(float(np.mean([out[t]["mean_iou_34"] for t in ORDER])), 3), previous_atlas_mean_iou_34_all=round(float(np.mean([out[t]["previous_atlas_mean_iou_34"] for t in ORDER])), 3))
    os.makedirs(os.path.join(P2, "compare"), exist_ok=True); json.dump(dict(summary=summ, types=out), open(os.path.join(P2, "compare", "silhouette_metrics.json"), "w"), indent=1); return summ, out
CW, CH, LW = 400, 304, 150
def cell(path, gray_bg=None):
    im = Image.open(path).convert("RGB") if os.path.exists(path) else Image.new("RGB", (CW, CH), (90, 30, 30)); return im.resize((CW, CH), Image.LANCZOS)
def board(t, met, concept, name_pl):
    cid = CID[t]; qa = {s: json.load(open(os.path.join(VAR, ST.LABEL[s], cid, cid + "_QA.json"))) for s in ST.STYLES}; bf = qa["A"]["board_frames"]
    rows = [("Pełne materiały\ni tekstury · 3/4", "{c}_34.jpg"), ("Pełne materiały\n· przód", "{c}_front.jpg"), ("Pełne materiały\n· bok", "{c}_side.jpg"), ("Neutralny\nmateriał · 3/4", "{c}_clay_34.jpg"), ("Wireframe · 3/4", "{c}_wire.jpg"),
            ("Sylwetka · przód", "{c}_sil_front.png"), ("Sylwetka · bok", "{c}_sil_side.png"), ("Sylwetka · 3/4", "{c}_sil_34.png")] + [(f"Animacja\n{b['clip']}\nklatka {b['frame']}", "{c}_anim%d.jpg" % (i + 1)) for i, b in enumerate(bf)]
    HH = 210; W = LW + 5*CW; Hh = HH + len(rows)*CH + 46; B = Image.new("RGB", (W, Hh), (24, 26, 30)); d = ImageDraw.Draw(B)
    d.text((16, 12), f"#1239 pilot 2 · {name_pl} · {cid}", font=fb(30), fill=(240, 240, 240)); m = met[t]
    d.text((16, 52), f"pięć niezależnych konstrukcji · identyczna kamera · IoU sylwetek 3/4: średnio {m['mean_iou_34']:.2f} (poprzedni atlas: {m['previous_atlas_mean_iou_34']:.2f}) · status: BLENDER " + "/".join(qa[s]["status"].replace("BLENDER_", "") for s in ST.STYLES) + " · odbiór artystyczny: OCZEKUJE", font=fr(17), fill=(170, 176, 186))
    for c, s in enumerate(ST.STYLES):
        x = LW + c*CW; d.text((x + 8, 86), SNAME[s], font=fb(20), fill=(255, 208, 120))
        d.text((x + 8, 112), f"{qa[s]['triangles']} tris · LOD1 {qa[s]['triangles_lod1']} · {qa[s].get('parts')} części", font=fr(13), fill=(150, 156, 166))
        for i, ln in enumerate(textwrap.wrap(concept[t][s], 52)[:5]): d.text((x + 8, 132 + i*15), ln, font=fr(12), fill=(205, 208, 214))
    for r, (lab, pat) in enumerate(rows):
        y = HH + r*CH
        for i, ln in enumerate(lab.split("\n")): d.text((10, y + 12 + i*19), ln, font=fb(14), fill=(225, 225, 228))
        for c, s in enumerate(ST.STYLES): B.paste(cell(os.path.join(rdir(s, cid), pat.format(c=cid))), (LW + c*CW, y))
        d.line([(0, y), (W, y)], fill=(24, 26, 30), width=2)
    for c in range(1, 5): d.line([(LW + c*CW, HH), (LW + c*CW, Hh - 46)], fill=(24, 26, 30), width=2)
    d.text((16, Hh - 34), "Rendery: Blender 5.3 EEVEE, scena źródłowa wariantu (ta, z której wykonano eksport FBX / GLB); to samo światło i kamera dla pięciu stylów. Klatki animacji = rzeczywiste klatki osi czasu. Unity: UNITY_NOT_RUN.", font=fr(13), fill=(140, 146, 156))
    os.makedirs(OUT, exist_ok=True); p = os.path.join(OUT, f"{ORDER.index(t) + 1:02d}_{t}.jpg"); B.save(p, quality=84, optimize=True); return p
def overview(tag, pat, title, name_pl, cw=380):
    ch = int(cw*CH/CW); W = 190 + 5*cw; B = Image.new("RGB", (W, 70 + 8*ch), (24, 26, 30)); d = ImageDraw.Draw(B); d.text((16, 10), "#1239 pilot 2 · " + title, font=fb(26), fill=(240, 240, 240))
    for c, s in enumerate(ST.STYLES): d.text((190 + c*cw + 8, 44), SNAME[s], font=fb(17), fill=(255, 208, 120))
    for r, t in enumerate(ORDER):
        for i, ln in enumerate(textwrap.wrap(name_pl[t], 16)[:4]): d.text((10, 70 + r*ch + 12 + i*18), ln, font=fb(14), fill=(225, 225, 228))
        for c, s in enumerate(ST.STYLES): B.paste(cell(os.path.join(rdir(s, CID[t]), pat.format(c=CID[t]))).resize((cw, ch), Image.LANCZOS), (190 + c*cw, 70 + r*ch))
    p = os.path.join(OUT, f"00_overview_{tag}.jpg"); B.save(p, quality=84, optimize=True); return p
if __name__ == "__main__":
    steps = sys.argv[1:] or ["prep", "boards"]
    if "prep" in steps: prep()
    if "boards" in steps:
        import pdefs as PD
        summ, met = metrics(); print(json.dumps(summ, indent=1))
        for t in ORDER: print(board(t, met, PD.CONCEPT, PD.NAME_PL[t]), met[t]["mean_iou_34"], "prev", met[t]["previous_atlas_mean_iou_34"])
        for tag, pat, title in (("full", "{c}_34.jpg", "pełne materiały i tekstury · 8 typów × 5 stylów"), ("clay", "{c}_clay_34.jpg", "neutralny, jednolity materiał"), ("wire", "{c}_wire.jpg", "wireframe"), ("silhouette", "{c}_sil_34.png", "czarne sylwetki 3/4")):
            print(overview(tag, pat, title, PD.NAME_PL))
