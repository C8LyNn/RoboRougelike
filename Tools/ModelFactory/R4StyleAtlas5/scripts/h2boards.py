"""#1239 Hero Benchmark Pass 2: review boards. usage: python3 h2boards.py"""
import os, json
from PIL import Image, ImageDraw, ImageFont
import astyle as ST
PKG = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); H2 = os.path.join(PKG, "hero2"); R = os.path.join(H2, "renders"); V = os.path.join(H2, "variants"); PV = os.path.join(H2, "preview"); OUT = os.path.join(H2, "boards")
fb = lambda n: ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", n); fr = lambda n: ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", n); BG = (20, 22, 26)
SN = dict(A="A · Sleek\nFuturistic", B="B · Brutal\nIndustrial", C="C · Arcade", D="D · Scrap\nWasteland", E="E · Heroic\nToon")
REF = dict(A="06_A_Sleek_Futuristic_HighDetail.jpg", B="07_B_Brutal_Industrial_HighDetail.jpg", C="08_C_Arcade_Geometric_HighDetail.jpg", D="09_D_Scrap_Wasteland_HighDetail.jpg", E="10_E_Heroic_Toon_HighDetail.jpg")
CID = dict(Gun="R4W_Basic", Wheel="R4M_WheelM_Side_Offroad", Battery="R4X_Battery"); PL = dict(Gun="broń — Basic Gun", Wheel="napęd — Wheel", Battery="moduł energetyczny — Battery")
F_ART = "Render artystyczny: Blender 5.3 EEVEE z wyeksportowanych GLB; światło kluczowe + kontrowe, głębia ostrości; cieniowanie toon i kontur (E) są look-devem sceny. Nie Unity."
F_NEU = "Render neutralny: jedno białe słońce, szare niebo, bez światła kontrowego, bez głębi ostrości, bez shadera toon. Blender 5.3 EEVEE z wyeksportowanych GLB. Nie Unity."
def shot(s, name, res=1080): return Image.open(os.path.join(R, ST.LABEL[s], f"{name}_{res}.jpg")).convert("RGB")
def vr(s, typ, view, root=V): c = CID[typ]; return Image.open(os.path.join(root, ST.LABEL[s], c, "renders", f"{c}_{view}.jpg")).convert("RGB")
def pv(s, typ, kind): return Image.open(os.path.join(PV, ST.LABEL[s], f"{CID[typ]}_{kind}.jpg")).convert("RGB")
def fit(im, w, h, bg=BG):
    k = min(w/im.width, h/im.height); im = im.resize((max(1, int(im.width*k)), max(1, int(im.height*k))), Image.LANCZOS); c = Image.new("RGB", (w, h), bg); c.paste(im, ((w - im.width)//2, (h - im.height)//2)); return c
def save(B, name):
    os.makedirs(OUT, exist_ok=True); p = os.path.join(OUT, name); B.save(p, quality=86, optimize=True); print(name, B.size, os.path.getsize(p)//1024, "KB")
def grid(name, title, cols, cw, ch, cell, foot, rows=ST.STYLES, rowlab=None):
    lw, hh = 150, 92; B = Image.new("RGB", (lw + len(cols)*cw, hh + len(rows)*ch + 34), BG); d = ImageDraw.Draw(B); d.text((16, 10), "#1239 Hero Benchmark Pass 2 · " + title, font=fb(26), fill=(240, 240, 240))
    for c, lab in enumerate(cols): d.text((lw + c*cw + 10, 64), lab, font=fb(15), fill=(255, 208, 120))
    for r, s in enumerate(rows):
        for i, ln in enumerate((rowlab or SN)[s].split("\n")): d.text((10, hh + r*ch + 12 + i*20), ln, font=fb(14), fill=(230, 230, 232))
        for c in range(len(cols)):
            try: B.paste(fit(cell(s, c), cw - 4, ch - 4), (lw + c*cw + 2, hh + r*ch + 2))
            except FileNotFoundError as e: d.text((lw + c*cw + 10, hh + r*ch + 10), "brak: " + os.path.basename(str(e.filename)), font=fr(12), fill=(255, 90, 90))
    d.text((16, hh + len(rows)*ch + 8), foot, font=fr(12), fill=(140, 146, 156)); save(B, name)
def reference(s):
    ref = Image.open(os.path.join(H2, "reference", REF[s])).convert("RGB"); W = 2400; rh = int(ref.height*(W//2)/ref.width); B = Image.new("RGB", (W, 70 + max(rh, 675) + 420 + 40), BG); d = ImageDraw.Draw(B)
    d.text((16, 12), f"#1239 Hero Benchmark Pass 2 · {SN[s].replace(chr(10), ' ')} · referencja obok modelu", font=fb(26), fill=(240, 240, 240))
    d.text((16, 46), "REFERENCJA właściciela (wygenerowana ilustracja art-direction, nie model 3D)", font=fb(14), fill=(255, 208, 120)); d.text((W//2 + 16, 46), "MODEL 3D tej iteracji (render z wyeksportowanych GLB, robot z prawdziwych części gry)", font=fb(14), fill=(255, 208, 120))
    B.paste(ref.resize((W//2, rh), Image.LANCZOS), (0, 70)); B.paste(fit(shot(s, "1_hero_after"), W//2, 675), (W//2, 70)); y = 70 + max(rh, 675)
    for i, (n, lab) in enumerate((("2_weapon_after", "Basic Gun"), ("3_drive_after", "Wheel"), ("4_energy_after", "Battery"), ("1c_hero_low_after", "robot, niska kamera"))):
        B.paste(fit(shot(s, n), W//4 - 4, 336), (i*W//4 + 2, y + 24)); d.text((i*W//4 + 10, y + 4), lab, font=fb(14), fill=(230, 230, 232))
    d.text((16, y + 24 + 336 + 30), "Ograniczenie kontraktu R4: lufa mieści się w zatwierdzonym obrysie Ø80 mm, wieża w 0,38 × 0,20 m nad podstawą 0,52 × 0,28 m; kadłub to klocki 0,52 m z warstwą osłon — proporcje ilustracji nie są osiągalne bez zmiany tych zasad. Rendery: Blender EEVEE z GLB, nie Unity.", font=fr(12), fill=(140, 146, 156))
    save(B, f"01_{s}_reference.jpg")
if __name__ == "__main__":
    for s in ST.STYLES: reference(s)
    ba = ["BEFORE — Hero Pass 1", "AFTER — Hero Pass 2"]
    for i, (n, lab) in enumerate((("1_hero_", "robot (32 części gry, ten sam blueprint)"), ("2_weapon_", PL["Gun"]), ("3_drive_", PL["Wheel"]), ("4_energy_", PL["Battery"]))):
        grid(f"02{'abcd'[i]}_before_after_{n[2:-1]}.jpg", "BEFORE / AFTER · " + lab, ba, 960, 540, lambda s, c, n=n: shot(s, n + ("before", "after")[c]), F_ART)
    grid("03_neutral.jpg", "render neutralny, bez efektownego światła (BEFORE / AFTER)", ["robot B", "robot A", "broń B", "broń A", "napęd B", "napęd A", "energia B", "energia A"], 480, 270,
         lambda s, c: shot(s, "n" + ("1_hero_", "2_weapon_", "3_drive_", "4_energy_")[c//2] + ("before", "after")[c % 2]), F_NEU)
    for t in ("Gun", "Wheel", "Battery"):
        grid(f"04_views_{t.lower()}.jpg", "widoki front / bok / 3/4 / góra · " + PL[t], ["front", "bok", "3/4", "góra"], 560, 420, lambda s, c, t=t: vr(s, t, ("front", "side", "34", "top")[c]), "Rendery techniczne z pbuild (to samo światło i kamera dla każdego stylu), z wyeksportowanych modeli.")
        grid(f"06_forms_{t.lower()}.jpg", "geometria a tekstura · " + PL[t], ["źródło high-poly (glina)", "siatka gry (glina)", "siatka gry (siatka)", "siatka gry + tekstury", "LOD1 + tekstury"], 520, 390,
             lambda s, c, t=t: pv(s, t, ("hi_clay", "lo_clay", "lo_wire", "lo", "lod1")[c]), "Kolumny 1–3: sama geometria. Kolumna 4: ta sama siatka z wypalonymi mapami (normal z high-poly, AO, zużycie wg krawędzi i zagłębień). Światło neutralne.")
        grid(f"08_mechanisms_{t.lower()}.jpg", "działanie mechanizmów (klatki zatwierdzonych klipów) · " + PL[t], ["klatka 1", "klatka 2", "klatka 3", "klatka 4"], 560, 420, lambda s, c, t=t: vr(s, t, "anim%d" % (c + 1)), "Klatki z osi czasu klipów (te same klipy, zdarzenia i czasy co w zatwierdzonym assecie; węzły mechanizmów z Visual Polish Pass 2).")
    grid("05_materials.jpg", "zbliżenia materiałów (AFTER)", ["burta: koło i skos pancerza", "dziób robota", "broń na pokładzie"], 800, 450, lambda s, c: shot(s, ("5_material_hull_after", "5b_material_prow_after", "6_material_mech_after")[c]), F_ART)
    st = lambda s: json.load(open(os.path.join(R, ST.LABEL[s], "scene_stats.json")))
    def crop(s, mode, tag, w, h):
        im = shot(s, "7_distance_" + mode); c = next(x for x in st(s)["crops"]["7_distance_" + mode] if x["tag"] == tag); x0, y0, x1, y1 = c["px_1080"]; cx, cy = int((x0 + x1)/2), int((y0 + y1)/2); return im.crop((cx - w//2, cy - h//2, cx + w//2, cy + h//2))
    tags = ["hero_20m", "hero_36m", "hero_60m"]
    grid("07_gameplay_distance.jpg", "kamera rozgrywki 1920×1080 i czytelność z dystansu (wycinki 1:1)", ["kadr 1080p AFTER (pomniejszony)"] + [f"{t.split('_')[1]} · {m}" for t in tags for m in ("before", "after")], 440, 320,
         lambda s, c: shot(s, "7_distance_after") if c == 0 else crop(s, ("before", "after")[(c - 1) % 2], tags[(c - 1)//2], 440, 320), F_ART)
    def texrow(s, c):
        t = ("Gun", "Wheel", "Battery")[c//4]; k = ("base", "normal", "orm", "emission")[c % 4]; return Image.open(os.path.join(H2, "textures", ST.LABEL[s], f"{CID[t]}_{k}" + (".png" if k == "normal" else ".jpg"))).convert("RGB")
    grid("09_textures.jpg", "zestawy tekstur (unikalne UV; 2048² broń i koło, 1024² bateria): base / normal / ORM / emisja", [f"{t} · {k}" for t in ("broń", "koło", "bateria") for k in ("base", "normal", "ORM", "emisja")], 300, 300, texrow, "Mapy wypalone z modelu high-poly i złożone per model: zużycie krawędzi, brud w zagłębieniach, rdza i odpryski zależą od geometrii tej części, nie od kafelka.")
