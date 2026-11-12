"""#1239 comparison boards from the BLENDER renders of the atlas (same camera / light for every style):
 compare/5way/<Id>.jpg      rows shaded / grayscale / no colour, no texture (clay) / wireframe / silhouette, columns A..E
 compare/sheet_<family>.jpg one shaded row per asset (256 px thumbnails)
 compare/style_metrics.json per asset: pairwise silhouette IoU of the 3/4 mask, triangles, mesh shells -> how different the styles really are
usage: python3 aboards.py [family|all]"""
import os, sys, json, itertools
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageOps
import astyle as ST
PKG = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); ATL = os.path.join(PKG, "atlas"); CMP = os.path.join(PKG, "compare")
FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
def font(s): return ImageFont.truetype(FONT, s)
def ids():
    out = {}
    for cid in sorted(os.listdir(os.path.join(ATL, ST.LABEL["A"]))):
        qs = {}
        for s in ST.STYLES:
            p = os.path.join(ATL, ST.LABEL[s], cid, cid + "_QA.json")
            if os.path.exists(p): qs[s] = json.load(open(p))
        if len(qs) == 5: out[cid] = qs
    return out
def R(cid, s, tag): return os.path.join(ATL, ST.LABEL[s], cid, "renders", f"{cid}_{tag}")
def five_way(cid, qs, W=420, Hh=320):
    rows = [("shaded", None), ("grayscale", None), ("bez koloru i tekstur", None), ("wireframe", None), ("sylwetka", None)]; B = Image.new("RGB", (150 + 5*(W + 4), 46 + 5*(Hh + 4)), (12, 14, 18)); d = ImageDraw.Draw(B)
    d.text((10, 10), f"{cid} — pięć stylów, ta sama kamera i światło", font=font(22), fill=(240, 242, 245)); masks = {}
    for c, s in enumerate(ST.STYLES):
        sh = Image.open(R(cid, s, "34.jpg")).convert("RGB"); bgc = Image.open(R(cid, s, "clay.jpg")).convert("RGB"); wire = Image.open(R(cid, s, "wire.jpg")).convert("RGB")
        a = np.array(Image.open(R(cid, s, "silhouette.png")).convert("L")) < 128; masks[s] = a
        sil = Image.fromarray(np.where(a[..., None], np.uint8(18), np.uint8(232)).repeat(3, axis=2))
        for r, im in enumerate((sh, ImageOps.grayscale(sh).convert("RGB"), bgc, wire, sil)):
            x = 150 + c*(W + 4); y = 46 + r*(Hh + 4); B.paste(im.resize((W, Hh), Image.LANCZOS), (x, y))
            if r == 0: d.rectangle([x, y, x + W, y + 24], fill=(12, 14, 18)); d.text((x + 6, y + 3), f"{s} — {ST.NAME[s]}  ·  {qs[s]['triangles']} tris", font=font(15), fill=(240, 242, 245))
    for r, (lab, _) in enumerate(rows): d.text((8, 46 + r*(Hh + 4) + Hh//2 - 8), lab, font=font(14), fill=(190, 196, 206))
    os.makedirs(os.path.join(CMP, "5way"), exist_ok=True); B.save(os.path.join(CMP, "5way", cid + ".jpg"), quality=86)
    iou = {a + b: round(float((masks[a] & masks[b]).sum()/max((masks[a] | masks[b]).sum(), 1)), 3) for a, b in itertools.combinations(ST.STYLES, 2)}
    return dict(silhouette_iou_34=iou, mean_iou=round(float(np.mean(list(iou.values()))), 3), max_iou=max(iou.values()), triangles={s: qs[s]["triangles"] for s in ST.STYLES},
                shells={s: len(qs[s]["meshes"]) for s in ST.STYLES}, mesh_parts={s: sum(1 for _ in qs[s]["meshes"]) for s in ST.STYLES})
def sheet(fam, items):
    T = 256; Hh = 195; B = Image.new("RGB", (230 + 5*(T + 4), 40 + len(items)*(Hh + 4)), (12, 14, 18)); d = ImageDraw.Draw(B)
    for c, s in enumerate(ST.STYLES): d.text((230 + c*(T + 4) + 6, 10), f"{s} {ST.NAME[s]}", font=font(15), fill=(240, 242, 245))
    for r, cid in enumerate(items):
        d.text((8, 40 + r*(Hh + 4) + Hh//2 - 8), cid[:26], font=font(13), fill=(200, 205, 214))
        for c, s in enumerate(ST.STYLES): B.paste(Image.open(R(cid, s, "34.jpg")).resize((T, Hh), Image.LANCZOS), (230 + c*(T + 4), 40 + r*(Hh + 4)))
    B.save(os.path.join(CMP, f"sheet_{fam}.jpg"), quality=84)
if __name__ == "__main__":
    A = ids(); met = {}; fams = {}
    for cid, qs in A.items(): met[cid] = five_way(cid, qs); fams.setdefault(qs["A"]["family"], []).append(cid)
    for fam, items in fams.items():
        for k in range(0, len(items), 16): sheet(fam + ("" if len(items) <= 16 else f"_{k//16 + 1}"), items[k:k + 16])
    pairs = {}
    for m in met.values():
        for k, v in m["silhouette_iou_34"].items(): pairs.setdefault(k, []).append(v)
    summary = dict(assets=len(met), mean_pair_iou={k: round(float(np.mean(v)), 3) for k, v in pairs.items()}, overall_mean_iou=round(float(np.mean([m["mean_iou"] for m in met.values()])), 3),
                   note="IoU of the 3/4-view silhouette masks between two styles of the SAME asset: 1.0 = identical outline. Blocks are bound to the approved solid, so their IoU is high by contract.")
    json.dump(dict(summary=summary, assets=met), open(os.path.join(CMP, "style_metrics.json"), "w"), indent=1); print(json.dumps(summary, indent=1))
