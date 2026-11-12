"""#1239 Hero Benchmark Pass 3 (owner selection): review boards. usage: python3 h3boards.py  (after h3report.py)"""
import os, json
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import h3gen as G
PKG = G.PKG; H3 = os.path.join(PKG, "hero3"); R = os.path.join(H3, "renders"); OUT = os.path.join(H3, "boards"); REP = os.path.join(H3, "report")
fb = lambda n: ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", n); fr = lambda n: ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", n); BG = (20, 22, 26); YEL = (255, 208, 120); WH = (236, 236, 238); GR = (150, 156, 166); OLDC = (120, 170, 255); NEWC = (255, 160, 90)
SN = dict(A="A · Sleek\nFuturistic", B="B · Brutal\nIndustrial", C="C · Arcade", D="D · Scrap\nWasteland", E="E · Heroic\nToon")
REF = dict(A="06_A_Sleek_Futuristic_HighDetail.jpg", B="07_B_Brutal_Industrial_HighDetail.jpg", C="08_C_Arcade_Geometric_HighDetail.jpg", D="09_D_Scrap_Wasteland_HighDetail.jpg", E="10_E_Heroic_Toon_HighDetail.jpg")
TPL = dict(chassis="robot", Gun="działko", Wheel="koło", Battery="bateria"); SEL = json.load(open(os.path.join(H3, "shots.json")))["configs"]; T = "#1239 Hero Benchmark Pass 3 · "
FOOT = "OLD = Hero Pass 1 (PR #1270), NEW = Hero Pass 2 (PR #1274), HYBRID = wybór właściciela. Render: Blender 5.3 EEVEE z niezmienionych, wyeksportowanych GLB obu PR-ów; arena, kamera i światło jak w Hero Pass 2; toon i kontur (E) to look-dev sceny. Nie Unity."
def shot(s, name): return Image.open(os.path.join(R, G.LABEL[s], f"{name}_1080.jpg")).convert("RGB")
def stats(s): return json.load(open(os.path.join(R, G.LABEL[s], "scene_stats.json")))
def fit(im, w, h, bg=BG):
    k = min(w/im.width, h/im.height); im = im.resize((max(1, int(im.width*k)), max(1, int(im.height*k))), Image.LANCZOS); c = Image.new("RGB", (w, h), bg); c.paste(im, ((w - im.width)//2, (h - im.height)//2)); return c
def save(B, name):
    os.makedirs(OUT, exist_ok=True); p = os.path.join(OUT, name); B.save(p, quality=86, optimize=True); print(name, B.size, os.path.getsize(p)//1024, "KB")
def mixline(d, x, y, s, cfg="hybrid", f=13):
    """robot / działko / koło / bateria with OLD in blue and NEW in orange"""
    m = SEL[s][cfg]["mix"]
    for t in ("chassis", "Gun", "Wheel", "Battery"):
        a = TPL[t] + ": "; d.text((x, y), a, font=fr(f), fill=GR); x += d.textlength(a, font=fr(f)); b = dict(chassis=("STARY", "NOWY"), Gun=("STARE", "NOWE"), Wheel=("STARE", "NOWE"), Battery=("STARA", "NOWA"))[t][m[t] == "new"]; d.text((x, y), b, font=fb(f), fill=OLDC if m[t] == "old" else NEWC); x += d.textlength(b + "   ", font=fb(f))
    return x
def grid(name, title, cols, cw, ch, cell, foot=FOOT, rows=G.STYLES, sub=None, mix=True, lw=150):
    hh = 92; band = 24 if mix else 0; B = Image.new("RGB", (lw + len(cols)*cw, hh + len(rows)*(ch + band) + 34), BG); d = ImageDraw.Draw(B); d.text((16, 10), T + title, font=fb(26), fill=WH)
    if sub: d.text((16, 44), sub, font=fr(13), fill=GR)
    for c, lab in enumerate(cols): d.text((lw + c*cw + 10, 68), lab, font=fb(15), fill=YEL)
    for r, s in enumerate(rows):
        y = hh + r*(ch + band)
        for i, ln in enumerate(SN[s].split("\n")): d.text((10, y + band + 12 + i*20), ln, font=fb(14), fill=WH)
        if mix: d.text((lw + 10, y + 4), "HYBRID  ", font=fb(13), fill=WH); mixline(d, lw + 80, y + 4, s)
        for c in range(len(cols)): B.paste(fit(cell(s, c), cw - 4, ch - 4), (lw + c*cw + 2, y + band + 2))
    d.text((16, hh + len(rows)*(ch + band) + 8), foot, font=fr(12), fill=GR); save(B, name)
def crop(s, cfg, tag, w, h):
    im = shot(s, "dist_" + cfg); c = next(x for x in stats(s)["crops"]["dist_" + cfg] if x["tag"] == tag); x0, y0, x1, y1 = c["px_1080"]; cx, cy = int((x0 + x1)/2), int((y0 + y1)/2); return im.crop((cx - w//2, cy - h//2, cx + w//2, cy + h//2))
def luma(im): a = np.asarray(im, float)/255.0; return 0.2126*a[..., 0] + 0.7152*a[..., 1] + 0.0722*a[..., 2]
def readability(s, cfg, tag="hero_36m"):
    """inside the robot's box at gameplay distance: spread of luminance (structure that survives) and mean contrast against the floor around it"""
    im = shot(s, "dist_" + cfg); c = next(x for x in stats(s)["crops"]["dist_" + cfg] if x["tag"] == tag); x0, y0, x1, y1 = [int(v) for v in c["px_1080"]]; L = luma(im); ins = L[y0:y1, x0:x1]; pad = 30; ring = L[max(0, y0 - pad):y1 + pad, max(0, x0 - pad):x1 + pad]; fl = (ring.sum() - ins.sum())/(ring.size - ins.size)
    mask = np.abs(ins - fl) > 0.06; body = ins[mask] if mask.sum() > 50 else ins.ravel(); return dict(distance_m=c["distance_m"], luminance_std=round(float(body.std()), 3), p10=round(float(np.percentile(body, 10)), 3), p90=round(float(np.percentile(body, 90)), 3), floor=round(float(fl), 3), pixels=int(mask.sum()))
def sw(d, x, y, c, w=44, h=26, lab=None):
    d.rectangle([x, y, x + w, y + h], fill=tuple(int(round(v*255)) for v in c), outline=(70, 74, 82))
    if lab: d.text((x, y + h + 2), lab, font=fr(10), fill=GR)
def materials():
    sel = json.load(open(os.path.join(REP, "selection.json"))); pal = json.load(open(os.path.join(REP, "palette.json"))); cw, ch = 760, 428; pw = 1500; hh = 92; B = Image.new("RGB", (150 + cw + pw, hh + 5*ch + 34), BG); d = ImageDraw.Draw(B)
    d.text((16, 10), T + "spójność materiałów w robotach HYBRID (kolory zmierzone z modeli)", font=fb(26), fill=WH); d.text((16, 44), "Próbki: kolor farby stref (Pass 1) albo średni kolor tekseli strefy w zestawie tekstur modelu (Pass 2), sRGB. Δ = odległość koloru części od tej samej strefy palety kadłuba (0 = identyczny, >0,25 = widać gołym okiem).", font=fr(13), fill=GR)
    d.text((160, 68), "HYBRID — grupa na dachu (działko, baterie, deployer)", font=fb(15), fill=YEL); d.text((150 + cw + 16, 68), "paleta kadłuba i części: baza / akcent / ciemny / światło", font=fb(15), fill=YEL)
    for r, s in enumerate(G.STYLES):
        y = hh + r*ch; v = sel[s]; mc = v["material_coherence"]; m = SEL[s]["hybrid"]["mix"]
        for i, ln in enumerate(SN[s].split("\n")): d.text((10, y + 12 + i*20), ln, font=fb(14), fill=WH)
        B.paste(fit(shot(s, "top_hybrid"), cw - 4, ch - 4), (152, y + 2)); x0 = 150 + cw + 16; rows = [("kadłub", "old" if m["chassis"] == "old" else "new", mc["chassis_palette_srgb"], None)] + [(TPL[t], m[t], {k: z["srgb"] for k, z in pal[s][m[t]][t]["zones"].items()}, mc["parts"][t]) for t in ("Gun", "Wheel", "Battery")]
        for i, (lab, w, cols, co) in enumerate(rows):
            yy = y + 18 + i*98; d.text((x0, yy), lab, font=fb(15), fill=WH); d.text((x0, yy + 22), "STARE · Pass 1 · #1270" if w == "old" else "NOWE · Pass 2 · #1274", font=fb(12), fill=OLDC if w == "old" else NEWC)
            d.text((x0, yy + 40), "farba stref × tekstura kafelkowa" if w == "old" else ("farba stref (kadłub przemalowany)" if lab == "kadłub" else "unikalne UV, wypalone mapy"), font=fr(11), fill=GR)
            for k, zn in enumerate(("base", "accent", "dark", "light")):
                xx = x0 + 250 + k*250
                if zn in cols:
                    sw(d, xx, yy + 4, cols[zn], 70, 40); dl = co.get(zn + "_delta_srgb") if co else None
                    if dl is not None: d.text((xx + 80, yy + 6), "Δ %.2f" % dl, font=fb(15), fill=(255, 110, 100) if dl > 0.25 else (255, 208, 120) if dl > 0.12 else (130, 210, 140))
                    d.text((xx + 80, yy + 28), dict(base="baza", accent="akcent", dark="ciemny", light="światło")[zn], font=fr(11), fill=GR)
                else: d.text((xx, yy + 14), "—", font=fr(14), fill=GR)
        d.line([(150, y + ch - 1), (150 + cw + pw, y + ch - 1)], fill=(44, 48, 56))
    d.text((16, hh + 5*ch + 8), "C i E: wszystkie części z jednej iteracji (Pass 2) — Δ małe. A: stare działko i bateria mają inne światło (Δ 0,55) i nie mają pomarańczowego akcentu kadłuba (strefa „akcent” była w Pass 1 ciemną ramą). B: nowe działko i koło mają ciemniejszą stal i bledszą żółć niż stary kadłub i bateria. D: nowe koło jest blisko palety starego kadłuba. Kadłub B i D z Pass 1 nie ma jednego koloru światła — części świecą własnym.", font=fr(12), fill=GR); save(B, "06_materials_coherence.jpg")
def zero_brutal():
    V = [("b_ro_wo", "robot STARY · koło STARE"), ("b_ro_wn", "robot STARY · koło NOWE  ← WYBRANE"), ("b_rn_wo", "robot NOWY · koło STARE"), ("b_rn_wn", "robot NOWY · koło NOWE")]; cw, ch = 640, 360; cols = ["robot 3/4", "niska kamera", "koło przy kadłubie", "dach: nowe działko + stara bateria", "36 m, wycinek 1:1", "20 m, wycinek 1:1"]; hh = 92; lw = 250
    B = Image.new("RGB", (lw + 4*cw + 2*480, hh + 4*ch + 60), BG); d = ImageDraw.Draw(B); d.text((16, 10), T + "elementy z oceną 0 · Brutal: robot i koło (działko NOWE i bateria STARA są ustalone)", font=fb(26), fill=WH)
    d.text((16, 44), "Kryterium właściciela: spójność z nowym działkiem, pokazana na całym robocie. Liczba pod wycinkiem 36 m: rozrzut jasności wewnątrz sylwetki (im większy, tym więcej konstrukcji widać z dystansu).", font=fr(13), fill=GR); x = lw; res = {}
    for c, lab in enumerate(cols): d.text((x + 10, 68), lab, font=fb(15), fill=YEL); x += cw if c < 4 else 480
    for r, (cfg, lab) in enumerate(V):
        y = hh + r*ch; chosen = "WYBRANE" in lab; a, b = lab.split(" · "); d.text((10, y + 14), a, font=fb(15), fill=OLDC if "STARY" in a else NEWC); d.text((10, y + 38), b.replace("  ← WYBRANE", ""), font=fb(15), fill=OLDC if "STARE" in b else NEWC)
        if chosen: d.text((10, y + 66), "← WYBRANE (HYBRID)", font=fb(14), fill=(130, 220, 140))
        for c, n in enumerate(("hero_", "low_", "wheel_", "top_")): B.paste(fit(shot("B", n + cfg), cw - 4, ch - 4), (lw + c*cw + 2, y + 2))
        for c, tag in enumerate(("hero_36m", "hero_20m")): B.paste(fit(crop("B", cfg, tag, 476, 356), 476, 356), (lw + 4*cw + c*480 + 2, y + 2))
        res[cfg] = readability("B", cfg); d.text((lw + 4*cw + 12, y + ch - 26), "rozrzut jasności %.3f" % res[cfg]["luminance_std"], font=fb(14), fill=WH)
        if chosen: d.rectangle([lw, y, B.width - 1, y + ch - 1], outline=(130, 220, 140), width=3)
    d.text((16, hh + 4*ch + 8), "Robot: stary kadłub (oliwkowoszara płyta) zostawia działko i koła jako ciemne, obrobione bryły na jaśniejszym tle i jest z jednej rodziny materiałów co stara bateria; na nowym, czarnym kadłubie robot z 36 m zlewa się w jedną plamę.", font=fr(12), fill=GR)
    d.text((16, hh + 4*ch + 28), "Koło: nowe powtarza motywy nowego działka (kołnierz ze śrubami, bladożółta opaska, to samo zużycie krawędzi) — działko nie zostaje jedynym elementem z nowego potoku. " + FOOT.split("Render")[1].join(["Render", ""]), font=fr(12), fill=GR); save(B, "07a_zero_brutal_robot_wheel.jpg"); return res
def zero_arcade():
    V = [("c_bato", "bateria STARA"), ("c_batn", "bateria NOWA  ← WYBRANA")]; cw, ch = 720, 405; cols = ["robot 3/4", "niska kamera", "dach: działko między bateriami", "20 m, wycinek 1:1", "36 m, wycinek 1:1"]; hh = 92; lw = 210
    ref = Image.open(os.path.join(PKG, "hero2", "reference", REF["C"])).convert("RGB").crop((950, 535, 1200, 740)); B = Image.new("RGB", (lw + 3*cw + 2*480, hh + 2*ch + 300), BG); d = ImageDraw.Draw(B); d.text((16, 10), T + "elementy z oceną 0 · Arcade: bateria (robot, działko i koło NOWE są ustalone)", font=fb(26), fill=WH)
    d.text((16, 44), "Kryterium właściciela: wersja lepiej pasująca do całej konstrukcji; potem usunąć niespójności baterii bez komplikowania stylu.", font=fr(13), fill=GR); x = lw; res = {}
    for c, lab in enumerate(cols): d.text((x + 10, 68), lab, font=fb(15), fill=YEL); x += cw if c < 3 else 480
    for r, (cfg, lab) in enumerate(V):
        y = hh + r*ch; chosen = "WYBRANA" in lab; d.text((10, y + 14), lab.replace("  ← WYBRANA", ""), font=fb(15), fill=OLDC if "STARA" in lab else NEWC)
        if chosen: d.text((10, y + 40), "← WYBRANA (HYBRID)", font=fb(14), fill=(130, 220, 140))
        for c, n in enumerate(("hero_", "low_", "top_")): B.paste(fit(shot("C", n + cfg), cw - 4, ch - 4), (lw + c*cw + 2, y + 2))
        for c, tag in enumerate(("hero_20m", "hero_36m")): B.paste(fit(crop("C", cfg, tag, 476, 400), 476, ch - 4), (lw + 3*cw + c*480 + 2, y + 2))
        if chosen: d.rectangle([lw, y, B.width - 1, y + ch - 1], outline=(130, 220, 140), width=3)
    y = hh + 2*ch + 16; B.paste(ref.resize((300, 246), Image.LANCZOS), (lw, y)); d.text((lw, y + 250), "referencja High Detail (ilustracja): bateria", font=fr(11), fill=GR); pal = json.load(open(os.path.join(REP, "palette.json")))["C"]; x = lw + 340
    L = ["Stara bateria na nowym robocie wnosi trzy kolory spoza jego palety: łupkowoszarą podstawę (kadłub i koła są granatowe), bladą morelę zamiast nasyconego pomarańczu i morski kołpak zamiast cyjanu.",
         "Nowa bateria ma paletę robota (granat, pomarańcz, cyjan) i formę z referencji (sześcian z oknem i narożnikami) — pasuje lepiej do całości. Dlatego HYBRID Arcade = NEW we wszystkich czterech pozycjach.",
         "Jej niespójności wobec reszty stylu (do usunięcia w geometrii, po bramce):",
         "  1. osiem małych kostek narożnych na baterię — styl ma „mało części, każda duża”; referencja ma kilka dużych, sfazowanych klamer;",
         "  2. ostre naroża sześcianu obok ośmiokątnych, grubo fazowanych brył działka i piasty koła;",
         "  3. bateria wypełnia całą komórkę jako pełny sześcian (0,49 × 0,51 × 0,50 m) do wysokości działka — dwie baterie mają na dachu więcej masy niż broń między nimi; okna to 4,5 % powierzchni baterii, światło działka 2,4 %;",
         "  4. każda bateria ma tyle pomarańczu co działko (13–14 % powierzchni), ale rozbitego na 9 małych elementów; dwie baterie = dwa razy więcej akcentu niż na broni — akcent przestaje wskazywać uzbrojenie."]
    for i, ln in enumerate(L): d.text((x, y + 6 + i*30), ln, font=fb(14) if i == 2 else fr(14), fill=WH if i < 3 else (220, 222, 226))
    save(B, "07b_zero_arcade_battery.jpg")
def sleek_gun():
    g = json.load(open(os.path.join(REP, "sleek_gun.json"))); pal = json.load(open(os.path.join(REP, "palette.json")))["A"]; sil = Image.open(os.path.join(REP, "sleek_gun_silhouette.png")).convert("RGB"); W = 2400; cw = W//2; ih = 675; sh = int(sil.height*W/sil.width); B = Image.new("RGB", (W, 100 + ih + 40 + sh + 420), BG); d = ImageDraw.Draw(B)
    d.text((16, 10), T + "Sleek: dlaczego nowe działko wypadło gorzej i co utraciło", font=fb(26), fill=WH); d.text((16, 44), "Ten sam robot (nowy kadłub i koła), ta sama kamera i światło; różni się tylko działko. Sylwetki: rzut boczny siatek gry obu modeli w tej samej skali, grupy wg węzłów rigu.", font=fr(13), fill=GR)
    d.text((16, 76), "STARE działko — Hero Pass 1 (PR #1270) — wybrane do HYBRID", font=fb(15), fill=OLDC); d.text((cw + 16, 76), "NOWE działko — Hero Pass 2 (PR #1274)", font=fb(15), fill=NEWC); B.paste(fit(shot("A", "fgun_hybrid"), cw - 4, ih), (2, 100)); B.paste(fit(shot("A", "fgun_new"), cw - 4, ih), (cw + 2, 100)); y = 100 + ih + 20
    B.paste(sil.resize((W, sh), Image.LANCZOS), (0, y)); d.text((16, y + 6), "podstawa (Body)", font=fb(13), fill=(150, 160, 185)); d.text((170, y + 6), "wieża (Yaw)", font=fb(13), fill=(200, 205, 215)); d.text((290, y + 6), "kołyska z osłoną (Pitch)", font=fb(13), fill=(245, 245, 245)); d.text((490, y + 6), "lufa (cała, także pod osłoną)", font=fb(13), fill=(255, 150, 60)); y += sh + 16
    o, n = g["old"], g["new"]; lo = pal["old"]["Gun"]["zones"]; ln_ = pal["new"]["Gun"]["zones"]
    rows = [("pole sylwetki z boku (podstawa + broń)", "%.0f cm²" % o["side_area_cm2"]["total"], "%.0f cm²" % n["side_area_cm2"]["total"], "to samo — nowe działko NIE jest mniejsze; obrys kontraktu jest ten sam"),
            ("udział podstawy w sylwetce", "%.0f %%" % (o["base_share_of_side_silhouette"]*100), "%.0f %%" % (n["base_share_of_side_silhouette"]*100), "to samo — wrażenie „przysadzistości” nie bierze się z masy"),
            ("najwęższe miejsce podstawy („szyja”)", "%.0f mm" % (o["base_min_width_m"]*1000), "%.0f mm" % (n["base_min_width_m"]*1000), "utracona szyja: stożek ze ściętym wierzchem → dzwon; wieża zlewa się z cokołem"),
            ("lufa widoczna przed osłoną", "%.0f mm" % (o["barrel_free_length_m"]*1000), "%.0f mm" % (n["barrel_free_length_m"]*1000), "krótsza o %.0f %%; do tego przewężenie w środku i gruby wylot zamiast równej linii" % ((1 - n["barrel_free_length_m"]/o["barrel_free_length_m"])*100)),
            ("kontur", "proste i skosy, klin", "łuki, kopuła, hełm", "stare działko ma język form kadłuba (płaskie płyty, skosy); nowe jest obłe na kanciastym robocie"),
            ("powierzchnia świecąca", "%.1f %%" % (lo["light"]["area_share"]*100), "%.1f %%" % (ln_["light"]["area_share"]*100), "dwa ciągłe pierścienie i linia wzdłuż osłony → drobne szewrony i kreski; z dystansu światło znika"),
            ("kolory", "biel, grafit, czerń, 1 światło", "+ pomarańcz, + stal lufy", "akcent przecina linię lufy kołnierzem; ciemne elementy są rozproszone (szwy, rama, płetwy)"),
            ("ciemny „grot” wzdłuż osi", "stożek + tuleja w jednej linii", "rozbity na ramę i lufę", "stara broń czyta się jako jeden długi kierunek; nowa jako kula z rurką"),
            ("trójkąty LOD0", "17 364", "4 064", "detal nowego jest w normal mapie; faza 2-segmentowa → 1-segmentowa — w zbliżeniu krawędzie nowego są miększe")]
    d.text((16, y), "pomiar", font=fb(14), fill=YEL); d.text((520, y), "STARE", font=fb(14), fill=OLDC); d.text((800, y), "NOWE", font=fb(14), fill=NEWC); d.text((1080, y), "co to znaczy", font=fb(14), fill=YEL)
    for i, (a, b, c, e) in enumerate(rows): yy = y + 28 + i*30; d.text((16, yy), a, font=fr(14), fill=WH); d.text((520, yy), b, font=fb(14), fill=WH); d.text((800, yy), c, font=fb(14), fill=WH); d.text((1080, yy), e, font=fr(14), fill=(214, 216, 222))
    d.text((16, B.height - 56), "Wniosek: nowe działko nie straciło wielkości, tylko czytelność — kierunek (długa, ciągła linia lufy), rozdział cokół / wieża, ciągłe światło i zgodność języka form z kadłubem. Zyskało (i warto zachować): szewrony światła na osłonie i pomarańczowy akcent — oba są w referencji High Detail.", font=fb(13), fill=WH)
    d.text((16, B.height - 30), FOOT, font=fr(12), fill=GR); save(B, "08_sleek_gun_analysis.jpg")
def sleek_plus():
    cw, ch = 1200, 675; B = Image.new("RGB", (2*cw, 100 + 2*ch + 60), BG); d = ImageDraw.Draw(B); d.text((16, 10), T + "Sleek: propozycja HYBRID+ (jedno światło) — tylko w scenie, do decyzji", font=fb(26), fill=WH)
    d.text((16, 44), "HYBRID zostaje bez zmian. HYBRID+ pokazuje jedyną korektę materiału, którą uważam za bezsporną: stare działko i stara bateria świecą lodową bielą, kadłub i nowe koła cyjanem — tu stare części biorą kolor światła kadłuba. Geometria i farba bez zmian.", font=fr(13), fill=GR)
    d.text((16, 76), "HYBRID — modele dokładnie tak, jak w PR #1270 / #1274", font=fb(15), fill=YEL); d.text((cw + 16, 76), "HYBRID+ — propozycja: światło starych części w kolorze kadłuba", font=fb(15), fill=YEL)
    for r, n in enumerate(("top_", "hero_")):
        for c, cfg in enumerate(("hybrid", "hybrid_plus")): B.paste(fit(shot("A", n + cfg), cw - 4, ch - 4), (c*cw + 2, 100 + r*ch + 2))
    d.text((16, 100 + 2*ch + 12), "To jest podgląd w scenie renderu (nadpisanie koloru emisji materiału R4P_Glow), nie nowy asset. Jeśli zaakceptujesz, zmiana trafi do wariantu modeli w następnym kroku. " + FOOT, font=fr(12), fill=GR); save(B, "09_sleek_light_proposal.jpg")
def reference(s):
    ref = Image.open(os.path.join(PKG, "hero2", "reference", REF[s])).convert("RGB"); W = 2400; rh = int(ref.height*(W//2)/ref.width); B = Image.new("RGB", (W, 96 + max(rh, 675) + 380 + 40), BG); d = ImageDraw.Draw(B)
    d.text((16, 10), T + SN[s].replace("\n", " ") + " · referencja High Detail obok robota HYBRID", font=fb(26), fill=WH); d.text((16, 46), "REFERENCJA właściciela (wygenerowana ilustracja art-direction, nie model 3D)", font=fb(14), fill=YEL); d.text((W//2 + 16, 46), "HYBRID  ", font=fb(14), fill=YEL); mixline(d, W//2 + 96, 47, s)
    B.paste(ref.resize((W//2, rh), Image.LANCZOS), (0, 72)); B.paste(fit(shot(s, "hero_hybrid"), W//2, 675), (W//2, 72)); y = 72 + max(rh, 675)
    for i, (n, lab) in enumerate((("fgun_hybrid", "działko na pokładzie"), ("wheel_hybrid", "koło przy kadłubie"), ("top_hybrid", "dach: działko, baterie, deployer"), ("low_hybrid", "robot, niska kamera"))):
        B.paste(fit(shot(s, n), W//4 - 4, 336), (i*W//4 + 2, y + 24)); d.text((i*W//4 + 10, y + 4), lab, font=fb(14), fill=WH)
    d.text((16, y + 24 + 336 + 14), "Ograniczenia kontraktu R4 bez zmian: lufa w obrysie Ø80 mm, wieża 0,38 × 0,20 m, kadłub z klocków 0,52 m z warstwą osłon — proporcje ilustracji nie są osiągalne bez zmiany tych zasad. " + FOOT.split("Render")[1].join(["Render", ""]), font=fr(12), fill=GR); save(B, f"10_{s}_reference_hybrid.jpg")
if __name__ == "__main__":
    grid("01_hybrid_robots.jpg", "BRAMKA ARTYSTYCZNA · pięć robotów HYBRID", ["HYBRID — widok 3/4", "HYBRID — niska kamera"], 1200, 675, lambda s, c: shot(s, ("hero_hybrid", "low_hybrid")[c]), sub="Wybór właściciela złożony z historycznych modeli obu PR-ów, bez żadnej zmiany geometrii i materiałów. 32 części gry, ten sam blueprint co w Hero Pass 1 i 2.")
    grid("02_old_new_hybrid.jpg", "OLD / NEW / HYBRID · robot", ["OLD — Hero Pass 1 (PR #1270)", "NEW — Hero Pass 2 (PR #1274)", "HYBRID — wybór właściciela"], 960, 540, lambda s, c: shot(s, "hero_" + ("old", "new", "hybrid")[c]))
    for i, (v, lab) in enumerate((("top", "dach: działko, baterie, deployer"), ("fgun", "działko na pokładzie"), ("wheel", "koło przy kadłubie"))):
        grid(f"03{'abc'[i]}_close_{v}.jpg", "zbliżenie OLD / NEW / HYBRID · " + lab, ["OLD — Hero Pass 1", "NEW — Hero Pass 2", "HYBRID"], 960, 540, lambda s, c, v=v: shot(s, v + "_" + ("old", "new", "hybrid")[c]))
    tags = ["hero_20m", "hero_36m", "hero_60m"]
    grid("04_gameplay_distance.jpg", "kamera rozgrywki 1920×1080 i czytelność z dystansu (wycinki 1:1)", ["kadr 1080p HYBRID (pomniejszony)"] + [f"{t.split('_')[1]} · {m}" for t in tags for m in ("OLD", "NEW", "HYBRID")], 380, 280,
         lambda s, c: shot(s, "dist_hybrid") if c == 0 else crop(s, ("old", "new", "hybrid")[(c - 1) % 3], tags[(c - 1)//3], 376, 276))
    grid("05_geometry_clay.jpg", "spójność geometrii · jedna szara glina na wszystkim (sama siatka, bez normal map i farby)", ["robot · OLD", "robot · NEW", "robot · HYBRID", "dach · OLD", "dach · NEW", "dach · HYBRID"], 640, 360,
         lambda s, c: shot(s, ("clay_hero_", "clay_top_")[c//3] + ("old", "new", "hybrid")[c % 3]), foot="Glina pokazuje tylko geometrię siatki gry: fazy Pass 1 są prawdziwą geometrią (2–3 segmenty), detal Pass 2 jest w dużej części w normal mapie i tu go nie widać. " + FOOT)
    materials(); rb = zero_brutal(); zero_arcade(); sleek_gun(); sleek_plus()
    for s in G.STYLES: reference(s)
    rd = {s: {c: readability(s, c) for c in ("old", "new", "hybrid")} for s in G.STYLES}; json.dump(dict(note="luminance spread inside the robot silhouette at the 36 m robot of the gameplay camera (1080p)", brutal_variants=rb, old_new_hybrid=rd), open(os.path.join(REP, "distance_readability.json"), "w"), indent=1); print(rb)
