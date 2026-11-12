"""#1239 Hero Benchmark Pass: review boards from hero1/renders. usage: python3 hboards.py"""
import os, json
from PIL import Image, ImageDraw, ImageFont
import astyle as ST
PKG = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); H1 = os.path.join(PKG, "hero1"); R = os.path.join(H1, "renders"); OUT = os.path.join(H1, "boards")
fb = lambda n: ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", n); fr = lambda n: ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", n)
SN = dict(A="A · Sleek\nDestiny 2 / AC VI", B="B · Brutal\nheavy industrial", C="C · Arcade\nAstroneer", D="D · Scrap\nCrossout", E="E · Toon\nOverwatch / Roboquest"); BG = (20, 22, 26)
FOOT = "Blender 5.3 EEVEE z wyeksportowanych GLB; ta sama arena, światło i kamera. BEFORE = Visual Polish Pass 2. AFTER = hero pass. Cieniowanie toon i kontur (E), głębia ostrości i światło kontrowe są look-devem renderu — gra musi je odtworzyć. Nie Unity."
def img(s, shot, res=1080): return Image.open(os.path.join(R, ST.LABEL[s], f"{shot}_{res}.jpg")).convert("RGB")
def stats(s): return json.load(open(os.path.join(R, ST.LABEL[s], "scene_stats.json")))
def grid(name, title, cols, cw, ch, cell):
    lw, hh = 170, 96; B = Image.new("RGB", (lw + len(cols)*cw, hh + 5*ch + 34), BG); d = ImageDraw.Draw(B); d.text((16, 10), "#1239 Hero Benchmark Pass · " + title, font=fb(28), fill=(240, 240, 240))
    for c, lab in enumerate(cols): d.text((lw + c*cw + 10, 66), lab, font=fb(16), fill=(255, 208, 120))
    for r, s in enumerate(ST.STYLES):
        for i, ln in enumerate(SN[s].split("\n")): d.text((10, hh + r*ch + 12 + i*20), ln, font=fb(14) if i == 0 else fr(13), fill=(230, 230, 232))
        for c in range(len(cols)): B.paste(cell(s, c).resize((cw - 4, ch - 4), Image.LANCZOS), (lw + c*cw + 2, hh + r*ch + 2))
    d.text((16, hh + 5*ch + 8), FOOT, font=fr(12), fill=(140, 146, 156)); os.makedirs(OUT, exist_ok=True); p = os.path.join(OUT, name); B.save(p, quality=86, optimize=True); print(name, B.size, os.path.getsize(p)//1024, "KB")
def crop(s, mode, tag, w=300, h=220):
    im = img(s, "7_distance_" + mode); c = next(x for x in stats(s)["crops"]["7_distance_" + mode] if x["tag"] == tag); x0, y0, x1, y1 = c["px_1080"]; cx, cy = int((x0 + x1)/2), int((y0 + y1)/2); return im.crop((cx - w//2, cy - h//2, cx + w//2, cy + h//2))
if __name__ == "__main__":
    ba = ["BEFORE — polish 2 (ten sam blueprint)", "AFTER — hero pass"]; two = lambda n: (lambda s, c: img(s, n + ("before", "after")[c]))
    grid("01_hero_robot.jpg", "hero robot (32 części rozgrywki, ten sam blueprint po obu stronach)", ba, 960, 540, two("1_hero_"))
    grid("02_hero_silhouette.jpg", "hero robot: sylwetka z boku i z niskiej kamery", ["bok · BEFORE", "bok · AFTER", "niska kamera · BEFORE", "niska kamera · AFTER"], 720, 405, lambda s, c: img(s, ("1b_hero_side_", "1c_hero_low_")[c//2] + ("before", "after")[c % 2]))
    grid("03_weapon.jpg", "broń — Basic Gun", ba, 960, 540, two("2_weapon_"))
    grid("04_drive.jpg", "napęd — Wheel", ba, 960, 540, two("3_drive_"))
    grid("05_energy.jpg", "moduł energetyczny — Battery", ba, 960, 540, two("4_energy_"))
    grid("06_materials.jpg", "zbliżenia materiałów (AFTER)", ["skos pancerza i burta", "dziób: zderzak, skosy narożne", "mechanika broni"], 800, 450, lambda s, c: img(s, ("5_material_hull_after", "5b_material_prow_after", "6_material_mech_after")[c]))
    tags = ["hero_20m", "hero_36m", "hero_60m"]
    grid("07_distance.jpg", "z dystansu: kamera rozgrywki i wycinki 1:1 (klatka 1920×1080)", ["kadr AFTER (pomniejszony)"] + [f"{t.split('_')[1]} · {m}" for t in tags for m in ("before", "after")], 440, 320,
         lambda s, c: img(s, "7_distance_after").resize((440, 248)) if c == 0 else crop(s, ("before", "after")[(c - 1) % 2], tags[(c - 1)//2], 440, 320))
