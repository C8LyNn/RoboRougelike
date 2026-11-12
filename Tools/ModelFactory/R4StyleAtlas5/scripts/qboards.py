"""#1239 Visual Polish Pass 1: BEFORE / AFTER and game-scale boards from the Blender scene renders (polish1/renders/<Style>/<shot>_<res>.jpg).
usage: python3 qboards.py"""
import os, json
from PIL import Image, ImageDraw, ImageFont
import astyle as ST
PKG = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); R = os.path.join(PKG, "polish1", "renders"); OUT = os.path.join(PKG, "polish1", "boards")
fb = lambda n: ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", n); fr = lambda n: ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", n)
SN = dict(A="A · Sleek Futuristic", B="B · Brutal Industrial", C="C · Arcade", D="D · Scrap Wasteland", E="E · Heroic Toon"); BG = (20, 22, 26)
def img(s, shot, res=1080): return Image.open(os.path.join(R, ST.LABEL[s], f"{shot}_{res}.jpg")).convert("RGB")
def stats(s): return json.load(open(os.path.join(R, ST.LABEL[s], "scene_stats.json")))["shots"]
def grid(name, title, sub, cols, cw, ch, res=1080, crop=None, lw=150):
    """rows = styles, cols = [(label, shot)]"""
    hh = 104; B = Image.new("RGB", (lw + len(cols)*cw, hh + 5*ch + 34), BG); d = ImageDraw.Draw(B); d.text((16, 10), "#1239 Visual Polish Pass 1 · " + title, font=fb(28), fill=(240, 240, 240)); d.text((16, 48), sub, font=fr(16), fill=(170, 176, 186))
    for c, (lab, _) in enumerate(cols): d.text((lw + c*cw + 10, 78), lab, font=fb(17), fill=(255, 208, 120))
    for r, s in enumerate(ST.STYLES):
        for i, ln in enumerate(SN[s].split(" · ")): d.text((10, hh + r*ch + 12 + i*20), ln, font=fb(15), fill=(230, 230, 232))
        for c, (_, shot) in enumerate(cols):
            im = img(s, shot, res)
            if crop: W, H = im.size; im = im.crop((int(crop[0]*W), int(crop[1]*H), int(crop[2]*W), int(crop[3]*H)))
            im = im.resize((cw - 4, ch - 4), Image.LANCZOS if im.size[0] > cw else Image.NEAREST); B.paste(im, (lw + c*cw + 2, hh + r*ch + 2))
            st = stats(s).get(shot, {}); d.text((lw + c*cw + 10, hh + r*ch + ch - 24), f"{st.get('pieces', '?')} części · {st.get('triangles', 0)//1000} tys. tris", font=fr(13), fill=(235, 235, 235), stroke_width=2, stroke_fill=(0, 0, 0))
    d.text((16, hh + 5*ch + 8), "Blender 5.3 EEVEE, import wyeksportowanych GLB; ta sama arena, słońce, niebo i kamera dla pięciu stylów. BEFORE = pilot 2 bez zmian. AFTER = części po polish + kosmetyczna warstwa kadłuba (propozycja). Nie Unity.", font=fr(13), fill=(140, 146, 156))
    os.makedirs(OUT, exist_ok=True); p = os.path.join(OUT, name); B.save(p, quality=86, optimize=True); print(p, B.size, os.path.getsize(p)//1024, "KB")
if __name__ == "__main__":
    ba = lambda k: [("BEFORE — pilot 2", k + "_before"), ("AFTER — polish 1", k + "_after")]
    grid("01_before_after_wheeled.jpg", "robot kołowy, 28 części", "identyczna konstrukcja, kamera i światło · klatki 1920×1080 pomniejszone do 960×540", ba("2_wheeled"), 960, 540)
    grid("02_before_after_hover.jpg", "robot poduszkowy, 27 części", "identyczna konstrukcja, kamera i światło · klatki 1920×1080 pomniejszone do 960×540", ba("3_hover"), 960, 540)
    grid("03_before_after_parts.jpg", "zbliżenie: klocek, broń, koło, silnik", "każda część na klocku-rdzeniu · klatki 1920×1080 pomniejszone do 960×540", ba("1_parts"), 960, 540)
    grid("04_before_after_gameplay.jpg", "kamera rozgrywki: gracz, trzech przeciwników, boss 96 części, przeszkody", "klatki 1920×1080 pomniejszone do 960×540", ba("6_gameplay"), 960, 540)
    grid("05_scale_ladder_after.jpg", "test skali (AFTER): 28 → 96 → 196 części → kamera rozgrywki", "klatki 1920×1080 pomniejszone do 640×360", [("28 części", "2_wheeled_after"), ("96 części", "4_robot100_after"), ("196 części", "5_robot200_after"), ("kamera rozgrywki", "6_gameplay_after")], 640, 360)
    grid("06_gameplay_720_pixels.jpg", "kamera rozgrywki w 1280×720 — piksele 1:1", "wycinek środka kadru bez skalowania: tyle widać, gdy robot zajmuje małą część ekranu", ba("6_gameplay"), 644, 364, res=720, crop=(0.25, 0.22, 0.75, 0.72))
    grid("07_gameplay_1080_distant.jpg", "kamera rozgrywki w 1920×1080 — dalecy przeciwnicy, piksele 1:1", "wycinek górnej części kadru bez skalowania", ba("6_gameplay"), 724, 364, res=1080, crop=(0.3125, 0.12, 0.6875, 0.4533))
