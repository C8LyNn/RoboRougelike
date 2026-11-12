"""#1239 Visual Polish Pass 2: review boards from the Blender scene renders (polish2/renders) and the per-variant animation frames. usage: python3 rboards.py"""
import os, json
from PIL import Image, ImageDraw, ImageFont
import astyle as ST
PKG = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); P2 = os.path.join(PKG, "polish2"); R = os.path.join(P2, "renders"); OUT = os.path.join(P2, "boards"); VAR = os.path.join(P2, "variants")
fb = lambda n: ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", n); fr = lambda n: ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", n)
SN = dict(A="A · Sleek Futuristic", B="B · Brutal Industrial", C="C · Arcade", D="D · Scrap Wasteland", E="E · Heroic Toon"); BG = (20, 22, 26); FOOT = "Blender 5.3 EEVEE, import wyeksportowanych GLB; ta sama arena, słońce, niebo i kamera dla pięciu stylów. BEFORE = Visual Polish Pass 1. AFTER = pass 2 + warstwa kadłuba z zestawu kafli (manifest). Nie Unity."
def img(s, shot, res=1080): return Image.open(os.path.join(R, ST.LABEL[s], f"{shot}_{res}.jpg")).convert("RGB")
def stats(s): return json.load(open(os.path.join(R, ST.LABEL[s], "scene_stats.json")))
def frame(title, sub, ncol, cw, ch, heads, lw=150, foot=FOOT, nrow=5):
    hh = 104; B = Image.new("RGB", (lw + ncol*cw, hh + nrow*ch + 34), BG); d = ImageDraw.Draw(B); d.text((16, 10), "#1239 Visual Polish Pass 2 · " + title, font=fb(28), fill=(240, 240, 240)); d.text((16, 48), sub, font=fr(16), fill=(170, 176, 186))
    for c, lab in enumerate(heads): d.text((lw + c*cw + 10, 78), lab, font=fb(16), fill=(255, 208, 120))
    d.text((16, hh + nrow*ch + 8), foot, font=fr(13), fill=(140, 146, 156)); return B, d, hh
def save(B, name): os.makedirs(OUT, exist_ok=True); p = os.path.join(OUT, name); B.save(p, quality=86, optimize=True); print(name, B.size, os.path.getsize(p)//1024, "KB")
def ba(name, title, sub, key, crop=None):
    cw, ch = 960, 540; B, d, hh = frame(title, sub, 2, cw, ch, ["BEFORE — polish 1", "AFTER — polish 2"])
    for r, s in enumerate(ST.STYLES):
        for i, ln in enumerate(SN[s].split(" · ")): d.text((10, hh + r*ch + 12 + i*20), ln, font=fb(15), fill=(230, 230, 232))
        for c, mode in enumerate(("before", "after")):
            im = img(s, f"{key}_{mode}")
            if crop: W, H = im.size; im = im.crop((int(crop[0]*W), int(crop[1]*H), int(crop[2]*W), int(crop[3]*H)))
            B.paste(im.resize((cw - 4, ch - 4), Image.LANCZOS), (150 + c*cw + 2, hh + r*ch + 2)); st = stats(s)["shots"][f"{key}_{mode}"]
            d.text((150 + c*cw + 10, hh + r*ch + ch - 24), f"{st['pieces']} części · {st['triangles']//1000} tys. tris" + (f" · {st['hull_fragments']} fragmentów kadłuba" if st.get("hull_fragments") else ""), font=fr(13), fill=(235, 235, 235), stroke_width=2, stroke_fill=(0, 0, 0))
    save(B, name)
def distant():
    cw, ch = 264, 200; tags = [c["tag"] for c in stats("A")["crops"]["7_distant_after"]]; B, d, hh = frame("dalecy przeciwnicy — piksele 1:1 z klatki 1920×1080", "każdy pojazd w całości w kadrze; okno 260×196 px wycięte bez skalowania wokół rzutu jego obrysu · górny wiersz stylu = BEFORE, dolny = AFTER", len(tags), cw, ch, [t.replace("_", " · ") for t in tags], nrow=10)
    for r, s in enumerate(ST.STYLES):
        for i, ln in enumerate(SN[s].split(" · ")): d.text((10, hh + 2*r*ch + 12 + i*20), ln, font=fb(15), fill=(230, 230, 232))
        for k, mode in enumerate(("before", "after")):
            im = img(s, "7_distant_" + mode); cr = {c["tag"]: c for c in stats(s)["crops"]["7_distant_" + mode]}; d.text((10, hh + (2*r + k)*ch + 60), mode.upper(), font=fr(13), fill=(170, 176, 186))
            for c, t in enumerate(tags):
                x0, y0, x1, y1 = cr[t]["px_1080"]; cx, cy = int((x0 + x1)/2), int((y0 + y1)/2); assert cr[t]["fully_inside_frame"], (s, t)
                B.paste(im.crop((cx - 130, cy - 98, cx + 130, cy + 98)), (150 + c*cw + 2, hh + (2*r + k)*ch + 2))
                if k == 1: d.text((150 + c*cw + 8, hh + (2*r + k)*ch + ch - 22), f"{int(x1 - x0)}×{int(y1 - y0)} px · {cr[t]['distance_m']:.0f} m", font=fr(12), fill=(235, 235, 235), stroke_width=2, stroke_fill=(0, 0, 0))
    save(B, "08_distant_1to1.jpg")
def destroy():
    sc = json.load(open(os.path.join(P2, "shots.json")))["scenarios"]; rep = json.load(open(os.path.join(P2, "hull_layer", "destruction_test.json")))["scenarios"]; cw, ch = 520, 360
    B, d, hh = frame("niszczenie części pod wielokomórkowymi płytami", "ten sam pojazd testowy (29 części); usunięte części znikają razem ze swoimi fragmentami, sąsiednie kafle zamykają krawędź, odsłonięte ściany pokazują ciemny rdzeń (bez odbudowy pancerza do naprawy)", len(sc), cw, ch, [x["title"] for x in sc],
                     foot="Wycinek środka klatki 1920×1080. Liczby pod kadrem pochodzą z testu generatora (hull_layer/destruction_test.json): usunięte fragmenty / kafle z nową maską / wynik.")
    for r, s in enumerate(ST.STYLES):
        for i, ln in enumerate(SN[s].split(" · ")): d.text((10, hh + r*ch + 12 + i*20), ln, font=fb(15), fill=(230, 230, 232))
        for c, x in enumerate(sc):
            im = img(s, "8_destroy_" + x["name"]); W, H = im.size; B.paste(im.crop((int(0.22*W), int(0.14*H), int(0.78*W), int(0.83*H))).resize((cw - 4, ch - 4), Image.LANCZOS), (150 + c*cw + 2, hh + r*ch + 2)); t = rep[x["name"]][s]
            d.text((150 + c*cw + 8, hh + r*ch + ch - 22), f"−{t['fragments_removed']} fragm. · {t['tiles_reshaped']} kafli zmieniło maskę · {'PASS' if t['PASS'] else 'FAIL'}", font=fr(12), fill=(235, 235, 235), stroke_width=2, stroke_fill=(0, 0, 0))
    save(B, "09_destruction.jpg")
def anim(typ, cid, n):
    qa = {s: json.load(open(os.path.join(VAR, ST.LABEL[s], cid, cid + "_QA.json"))) for s in ST.STYLES}; bf = qa["A"]["board_frames"]; cw, ch = 400, 304
    B, d, hh = frame(f"mechanizmy w ruchu · {typ}", "klatki rzeczywistej osi czasu pliku; pod kadrem lista węzłów-mechanizmów tego wariantu (ruch napisany osobno dla stylu)", len(bf), cw, ch, [f"{b['clip']} · klatka {b['frame']}" for b in bf],
                     foot="Rendery sceny źródłowej wariantu (Blender EEVEE, bez sceny testowej). Zdarzenia, gniazda i czasy klipów bez zmian względem źródła; mechanizmy to dodatkowe węzły kosmetyczne.")
    for r, s in enumerate(ST.STYLES):
        for i, ln in enumerate(SN[s].split(" · ")): d.text((10, hh + r*ch + 12 + i*20), ln, font=fb(15), fill=(230, 230, 232))
        for c in range(len(bf)): B.paste(Image.open(os.path.join(VAR, ST.LABEL[s], cid, "renders", f"{cid}_anim{c + 1}.jpg")).convert("RGB").resize((cw - 4, ch - 4), Image.LANCZOS), (150 + c*cw + 2, hh + r*ch + 2))
        d.text((150 + 8, hh + r*ch + ch - 22), "mechanizmy: " + ", ".join(sorted(qa[s].get("mechanisms") or {})), font=fr(12), fill=(235, 235, 235), stroke_width=2, stroke_fill=(0, 0, 0))
    save(B, f"10{n}_mechanisms_{typ}.jpg")
if __name__ == "__main__":
    sub = "identyczna konstrukcja, kamera i światło · klatki 1920×1080 pomniejszone do 960×540"
    ba("01_models_gun_wheel_hover.jpg", "pojedyncze modele: działko, koło, silnik", sub, "1a_models"); ba("02_models_battery_deployer_trap.jpg", "pojedyncze modele: bateria, wyrzutnik, pułapka", sub, "1b_models")
    ba("03_buggy.jpg", "robot kołowy „buggy”, 29 części", sub, "2_buggy"); ba("04_skimmer.jpg", "robot poduszkowy „skimmer”, 36 części", sub, "3_skimmer"); ba("05_crawler_100.jpg", "robot „crawler”, 103 części", sub, "4_crawler")
    ba("06_dreadnought_200.jpg", "robot „dreadnought”, 208 części", sub, "5_dreadnought"); ba("07_gameplay.jpg", "kamera rozgrywki: gracz, trzech przeciwników, crawler w tle", sub, "6_gameplay")
    distant(); destroy()
    for n, (typ, cid) in zip("abcdef", (("Gun", "R4W_Basic"), ("Wheel", "R4M_WheelM_Side_Offroad"), ("Hover", "R4M_HoverEngine"), ("Battery", "R4X_Battery"), ("Deployer", "R4X_ExplosiveTrapDeployer"), ("Trap", "R4D_ExplosiveTrap"))): anim(typ, cid, n)
