# -*- coding: utf-8 -*-
"""#1239 Hero Benchmark Pass 4: review boards. LEFT = the direction preferred in survey 1, RIGHT = the new Pass 4 model; same camera, scale and light.
usage: python3 h4boards.py"""
import os, json, textwrap
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import h4gen as G, h4notes, h4pal
PKG = G.PKG; H4 = os.path.join(PKG, "hero4"); R = os.path.join(H4, "renders"); OUT = os.path.join(H4, "boards")
fb = lambda n: ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", n); fr = lambda n: ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", n)
BG = (20, 22, 26); YEL = (255, 208, 120); WH = (236, 236, 238); GR = (150, 156, 166); LC = (120, 170, 255); RC = (130, 220, 140); T = "#1239 Hero Benchmark Pass 4 · "
SN = dict(A="A · Sleek", B="B · Brutal", C="C · Arcade", D="D · Scrap", E="E · Heroic Toon"); CID = dict(Gun="R4W_Basic", Wheel="R4M_WheelM_Side_Offroad", Battery="R4X_Battery")
PL = dict(old="Pass 1 (PR #1270)", new="Pass 2 (PR #1274)"); CAT = dict(Gun="działko", Wheel="koło", Battery="bateria", chassis="kadłub")
FOOT = "LEWA = kierunek preferowany w ankiecie 1 (wyeksportowany model Pass 1 / Pass 2). PRAWA = NOWY model Pass 4. Ta sama arena, kamera, skala i światło (Blender 5.3 EEVEE, rendery z wyeksportowanych GLB). Toon i kontur stylu E to look-dev sceny. Nie Unity."
def shot(s, name):
    p = os.path.join(R, G.LABEL[s], name + "_1080.jpg"); return Image.open(p).convert("RGB") if os.path.exists(p) else Image.new("RGB", (1920, 1080), (70, 20, 20))
def stats(s): return json.load(open(os.path.join(R, G.LABEL[s], "scene_stats.json")))
def fit(im, w, h):
    k = min(w/im.width, h/im.height); im = im.resize((max(1, int(im.width*k)), max(1, int(im.height*k))), Image.LANCZOS); c = Image.new("RGB", (w, h), BG); c.paste(im, ((w - im.width)//2, (h - im.height)//2)); return c
def cropc(im, fx=0.5, fy=0.5, k=0.7): w, h = im.size; cw, ch = int(w*k), int(h*k); x = int(min(max(0, w*fx - cw/2), w - cw)); y = int(min(max(0, h*fy - ch/2), h - ch)); return im.crop((x, y, x + cw, y + ch))
def save(B, name): os.makedirs(OUT, exist_ok=True); p = os.path.join(OUT, name); B.save(p, quality=86, optimize=True); print(name, B.size, os.path.getsize(p)//1024, "KB")
def wrap(d, x, y, text, font, fill, width, lh):
    for ln in textwrap.wrap(text, width): d.text((x, y), ln, font=font, fill=fill); y += lh
    return y
def rows_for(cat):
    """[(style, set, left cfg, right cfg, baseline)] - one row per proposal of the category"""
    out = []
    for s in G.STYLES:
        sv = G.SURVEY[s][cat]
        if isinstance(sv, tuple): out += [(s, "p", "L", "R", sv[0]), (s, "q", "Lq", "Rq", sv[1])]
        else: out.append((s, "p", "L", "R", sv))
    return out
def robots():
    out = []
    for s in G.STYLES: out.append((s, "p", "L", "R")); out += [(s, "q", "Lq", "Rq")] if G.ties(s) else []
    return out
def rowlabel(d, x, y, s, set_, tie, extra=None):
    d.text((x, y), SN[s], font=fb(15), fill=WH)
    if tie: d.text((x, y + 22), "propozycja " + set_.upper(), font=fb(13), fill=YEL)
    if extra: wrap(d, x, y + 44, extra, fr(11), GR, 24, 14)
def category(cat, view, name, title, extra_views=()):
    rows = rows_for(cat); cw, ch = 880, 495; ew, eh = 500, 281; lw = 170; tw = 640; hh = 96; W = lw + 2*cw + len(extra_views)*2*ew + tw; B = Image.new("RGB", (W, hh + len(rows)*ch + 34), BG); d = ImageDraw.Draw(B)
    d.text((16, 10), T + title, font=fb(26), fill=WH); d.text((16, 46), "Każdy wiersz: wzorzec z ankiety (lewa) i nowy model zbudowany z tego kierunku (prawa). Remis w ankiecie = dwie propozycje P i Q, każda obok własnego wzorca.", font=fr(13), fill=GR)
    d.text((lw + 10, 72), "LEWA — preferowany kierunek z ankiety 1", font=fb(15), fill=LC); d.text((lw + cw + 10, 72), "PRAWA — NOWY model Pass 4", font=fb(15), fill=RC)
    for i, (v, lab) in enumerate(extra_views): d.text((lw + 2*cw + i*2*ew + 10, 72), lab + " — lewa / prawa", font=fb(13), fill=YEL)
    d.text((W - tw + 14, 72), "co zostało ze wzorca · co jest nowe", font=fb(15), fill=YEL)
    for r, (s, set_, lc, rc, base) in enumerate(rows):
        y = hh + r*ch; tie = isinstance(G.SURVEY[s][cat], tuple); rowlabel(d, 10, y + 12, s, set_, tie, "wzorzec: " + PL[base])
        B.paste(fit(shot(s, view + "_" + lc), cw - 4, ch - 4), (lw + 2, y + 2)); B.paste(fit(shot(s, view + "_" + rc), cw - 4, ch - 4), (lw + cw + 2, y + 2))
        for i, (v, lab) in enumerate(extra_views):
            for k, c in enumerate((lc, rc)): B.paste(fit(shot(s, v + "_" + c), ew - 4, eh - 4), (lw + 2*cw + (2*i + k)*ew + 2, y + 2 + (ch - eh)//2))
        keep, new = h4notes.N[(cat, s, set_)]; x = W - tw + 14; yy = y + 18; d.text((x, yy), "ZACHOWANE ZE WZORCA", font=fb(12), fill=LC); yy = wrap(d, x, yy + 18, keep, fr(13), WH, 84, 17) + 10
        d.text((x, yy), "NOWE W PASS 4", font=fb(12), fill=RC); yy = wrap(d, x, yy + 18, new, fr(13), WH, 84, 17)
        d.line([(0, y + ch - 1), (W, y + ch - 1)], fill=(44, 48, 56))
    d.text((16, hh + len(rows)*ch + 8), FOOT, font=fr(12), fill=GR); save(B, name)
def whole():
    rb = robots(); cw, ch = 800, 450; lw = 170; hh = 96; cols = ["LEWA — 3/4", "PRAWA — 3/4", "LEWA — niska kamera", "PRAWA — niska kamera"]; B = Image.new("RGB", (lw + 4*cw, hh + len(rb)*ch + 34), BG); d = ImageDraw.Draw(B)
    d.text((16, 10), T + "cały robot: preferowany kierunek z ankiety (lewa) i nowe modele Pass 4 (prawa)", font=fb(26), fill=WH)
    d.text((16, 46), "LEWA: części i paleta kadłuba wskazane w ankiecie. PRAWA: nowe działko, koła, baterie i nowy pancerz kadłuba. Rdzeń klocka i deployer to w obu kolumnach zatwierdzone modele Pass 1. Brutal i Arcade mają po dwa roboty (propozycje P i Q remisów).", font=fr(13), fill=GR)
    for c, lab in enumerate(cols): d.text((lw + c*cw + 10, 72), lab, font=fb(15), fill=LC if c % 2 == 0 else RC)
    for r, (s, set_, lc, rc) in enumerate(rb):
        y = hh + r*ch; t = G.ties(s); rowlabel(d, 10, y + 12, s, set_, bool(t), ("remis: " + ", ".join(CAT[x] for x in t) + (" — wzorzec " + ("stary" if set_ == "p" else "nowy"))) if t else None)
        for c, n in enumerate(("hero_" + lc, "hero_" + rc, "low_" + lc, "low_" + rc)): B.paste(fit(shot(s, n), cw - 4, ch - 4), (lw + c*cw + 2, y + 2))
    d.text((16, hh + len(rb)*ch + 8), FOOT, font=fr(12), fill=GR); save(B, "01_robots_left_right.jpg")
def hull():
    rows = rows_for("chassis"); cw, ch = 640, 360; lw = 170; tw = 560; hh = 96; views = [("cube", "pojedynczy klocek z własnymi płytami"), ("panels", "płyty na boku kadłuba"), ("prow", "dziób: zderzak, policzki, rampa")]; W = lw + 6*cw + tw; B = Image.new("RGB", (W, hh + len(rows)*ch + 34), BG); d = ImageDraw.Draw(B)
    d.text((16, 10), T + "kadłub: nowy modułowy pancerz (warstwa odwracalna) — lewa wzorzec, prawa Pass 4", font=fb(26), fill=WH)
    d.text((16, 46), "Reguły warstwy kadłuba są bez zmian (te same nazwy siatek, ramki i obrysy): każda płyta ma właściciela — klocek — i znika razem z nim. Rdzeń klocka pod pancerzem to zatwierdzony model. Lewa: geometria zestawu Pass 1 w palecie wskazanej w ankiecie.", font=fr(13), fill=GR)
    for i, (v, lab) in enumerate(views): d.text((lw + i*2*cw + 10, 72), lab + " — lewa / prawa", font=fb(14), fill=YEL)
    d.text((W - tw + 14, 72), "co zostało · co jest nowe", font=fb(15), fill=YEL)
    for r, (s, set_, lc, rc, base) in enumerate(rows):
        y = hh + r*ch; tie = isinstance(G.SURVEY[s]["chassis"], tuple); rowlabel(d, 10, y + 12, s, set_, tie, "wzorzec: paleta " + PL[base])
        for i, (v, lab) in enumerate(views):
            for k, c in enumerate((lc, rc)): B.paste(fit(shot(s, v + "_" + c), cw - 4, ch - 4), (lw + (2*i + k)*cw + 2, y + 2))
        keep, new = h4notes.N[("chassis", s, set_)]; x = W - tw + 14; yy = y + 14; d.text((x, yy), "ZACHOWANE", font=fb(12), fill=LC); yy = wrap(d, x, yy + 18, keep, fr(12), WH, 80, 16) + 8; d.text((x, yy), "NOWE W PASS 4", font=fb(12), fill=RC); wrap(d, x, yy + 18, new, fr(12), WH, 80, 16)
    d.text((16, hh + len(rows)*ch + 8), FOOT, font=fr(12), fill=GR); save(B, "05_hull_armour.jpg")
def crop(s, cfg, tag, w, h):
    im = shot(s, "dist_" + cfg); st = stats(s).get("crops", {}).get("dist_" + cfg)
    if not st: return Image.new("RGB", (w, h), (70, 20, 20))
    c = next(x for x in st if x["tag"] == tag); x0, y0, x1, y1 = c["px_1080"]; cx, cy = int((x0 + x1)/2), int((y0 + y1)/2); return im.crop((cx - w//2, cy - h//2, cx + w//2, cy + h//2))
def distance():
    rb = robots(); cw, ch = 420, 300; lw = 170; fw = 640; hh = 96; tags = ["hero_20m", "hero_36m", "hero_60m"]; B = Image.new("RGB", (lw + fw + 6*cw, hh + len(rb)*ch + 34), BG); d = ImageDraw.Draw(B)
    d.text((16, 10), T + "czytelność z dystansu rozgrywki: kamera gry 1920×1080, wycinki 1:1", font=fb(26), fill=WH); d.text((16, 46), "Trzy takie same roboty w odległości ok. 20, 36 i 60 m od kamery rozgrywki. Wycinki nie są powiększane.", font=fr(13), fill=GR)
    d.text((lw + 10, 72), "kadr 1080p, PRAWA (pomniejszony)", font=fb(14), fill=YEL)
    for i, t in enumerate(tags):
        for k, (lab, col) in enumerate((("LEWA", LC), ("PRAWA", RC))): d.text((lw + fw + (2*i + k)*cw + 10, 72), t.split("_")[1] + " · " + lab, font=fb(14), fill=col)
    for r, (s, set_, lc, rc) in enumerate(rb):
        y = hh + r*ch; rowlabel(d, 10, y + 12, s, set_, bool(G.ties(s))); B.paste(fit(shot(s, "dist_" + rc), fw - 4, ch - 4), (lw + 2, y + 2))
        for i, t in enumerate(tags):
            for k, c in enumerate((lc, rc)): B.paste(crop(s, c, t, cw - 4, ch - 4), (lw + fw + (2*i + k)*cw + 2, y + 2))
    d.text((16, hh + len(rb)*ch + 8), FOOT, font=fr(12), fill=GR); save(B, "06_gameplay_distance.jpg")
def clay():
    rb = robots(); cw, ch = 800, 450; lw = 170; hh = 96; cols = [("clay_", "robot"), ("clayprow_", "dziób")]; rb2 = [x for x in rb if x[1] == "p" or "chassis" in G.ties(x[0])]; B = Image.new("RGB", (lw + 4*cw, hh + len(rb2)*ch + 34), BG); d = ImageDraw.Draw(B)
    d.text((16, 10), T + "sama geometria: jedna szara glina na wszystkim (bez tekstur i normal map)", font=fb(26), fill=WH); d.text((16, 46), "Pokazuje, co jest prawdziwą siatką gry. Detal wypalony w normal mapie (nity, spoiny, żłobienia) tu nie istnieje — zostają bryły, fazy i sylwetka.", font=fr(13), fill=GR)
    for i, (v, lab) in enumerate(cols):
        for k, (side, col) in enumerate((("LEWA", LC), ("PRAWA", RC))): d.text((lw + (2*i + k)*cw + 10, 72), side + " — " + lab, font=fb(15), fill=col)
    for r, (s, set_, lc, rc) in enumerate(rb2):
        y = hh + r*ch; rowlabel(d, 10, y + 12, s, set_, bool(G.ties(s)))
        for i, (v, lab) in enumerate(cols):
            for k, c in enumerate((lc, rc)): B.paste(fit(shot(s, v + c), cw - 4, ch - 4), (lw + (2*i + k)*cw + 2, y + 2))
    d.text((16, hh + len(rb2)*ch + 8), FOOT, font=fr(12), fill=GR); save(B, "07_geometry_clay.jpg")
def sources(cat, name):
    """the modelling itself: source high-poly, game mesh wireframe, game mesh with its baked texture set, LOD1 - previews written by h4model.py"""
    rows = rows_for(cat); cw, ch = 640, 480; lw = 170; hh = 96; cols = [("hi_clay", "model źródłowy high-poly"), ("lo_wire", "siatka gry — wireframe"), ("lo", "siatka gry z wypalonymi teksturami"), ("lo_back", "tył"), ("lod1", "LOD1")]; B = Image.new("RGB", (lw + len(cols)*cw, hh + len(rows)*ch + 34), BG); d = ImageDraw.Draw(B)
    d.text((16, 10), T + "pliki źródłowe · " + CAT[cat] + ": model high-poly → siatka gry → wypalenie → LOD1", font=fb(26), fill=WH); d.text((16, 46), "Podglądy zapisane przez skrypt budujący model (neutralne światło, bez sceny). Źródła: hero4/source*/…_SOURCE.blend (kolekcje SOURCE_highpoly, GAME_lowpoly, LOD1).", font=fr(13), fill=GR)
    for c, (k, lab) in enumerate(cols): d.text((lw + c*cw + 10, 72), lab, font=fb(15), fill=YEL)
    rep = {}
    for r, (s, set_, lc, rc, base) in enumerate(rows):
        y = hh + r*ch; q = "_q" if set_ == "q" else ""; rp = os.path.join(H4, "report" + q, G.LABEL[s] + "_" + CID[cat] + ".json"); t = json.load(open(rp)) if os.path.exists(rp) else {}
        rowlabel(d, 10, y + 12, s, set_, isinstance(G.SURVEY[s][cat], tuple), ("źródło %s tr.\ngra %s tr.\nLOD1 %s tr." % (t.get("tris_hi_total", "?"), t.get("tris_lo_total", "?"), t.get("tris_lod1", "?"))))
        for c, (k, lab) in enumerate(cols):
            p = os.path.join(H4, "preview" + q, G.LABEL[s], CID[cat] + "_" + k + ".jpg"); B.paste(fit(Image.open(p).convert("RGB") if os.path.exists(p) else Image.new("RGB", (1200, 900), (70, 20, 20)), cw - 4, ch - 4), (lw + c*cw + 2, y + 2))
    d.text((16, hh + len(rows)*ch + 8), "Liczby trójkątów z raportu budowy. Tekstury: base, ORM, normal, emission 2048 px na model.", font=fr(12), fill=GR); save(B, name)
def anim():
    """frames of the REAL exported animation rendered by pbuild (board_frames of the approved clip timeline)"""
    rows = [(c, s, set_) for c in ("Gun", "Wheel", "Battery") for (s, set_, _, _, _) in rows_for(c)]; cw, ch = 480, 360; lw = 230; hh = 96; B = Image.new("RGB", (lw + 5*cw, hh + len(rows)*ch + 34), BG); d = ImageDraw.Draw(B)
    d.text((16, 10), T + "animacje: klatki prawdziwej, wyeksportowanej animacji nowych modeli (pbuild, Blender)", font=fb(26), fill=WH); d.text((16, 46), "Klipy, zdarzenia i tory mechanizmów są te same co w zatwierdzonych modelach (kontrakt). Zmieniła się geometria mechanizmów, żeby ten sam ruch był czytelniejszy.", font=fr(13), fill=GR)
    for c, lab in enumerate(("poza spoczynkowa 3/4", "klatka 1", "klatka 2", "klatka 3", "klatka 4")): d.text((lw + c*cw + 10, 72), lab, font=fb(15), fill=YEL)
    for r, (cat, s, set_) in enumerate(rows):
        y = hh + r*ch; q = "_q" if set_ == "q" else ""; dd = os.path.join(H4, "variants" + q, G.LABEL[s], CID[cat]); qa = json.load(open(os.path.join(dd, CID[cat] + "_QA.json"))) if os.path.exists(os.path.join(dd, CID[cat] + "_QA.json")) else {}
        d.text((10, y + 12), SN[s], font=fb(15), fill=WH); d.text((10, y + 34), CAT[cat] + (" · propozycja " + set_.upper() if isinstance(G.SURVEY[s][cat], tuple) else ""), font=fb(13), fill=YEL)
        for i, bf in enumerate((qa.get("board_frames") or [])[:4]): d.text((10, y + 60 + i*16), "%d: %s, klatka %s" % (i + 1, bf.get("clip"), bf.get("frame")), font=fr(11), fill=GR)
        for c, k in enumerate(("34", "anim1", "anim2", "anim3", "anim4")):
            p = os.path.join(dd, "renders", CID[cat] + "_" + k + ".jpg"); B.paste(fit(Image.open(p).convert("RGB") if os.path.exists(p) else Image.new("RGB", (800, 600), (70, 20, 20)), cw - 4, ch - 4), (lw + c*cw + 2, y + 2))
    d.text((16, hh + len(rows)*ch + 8), "Test Blendera po stronie generatora. Unity: UNITY_NOT_RUN.", font=fr(12), fill=GR); save(B, "09_animation_frames.jpg")
def kitsheets():
    S = json.load(open(os.path.join(H4, "shots.json")))["p4kit"]; items = [(s, k, lab) for s in G.STYLES for k, lab in S[s].items()]; cw, ch = 1200, 750; B = Image.new("RGB", (2*cw, 96 + 3*ch + 34), BG); d = ImageDraw.Draw(B)
    d.text((16, 10), T + "pancerz kadłuba: arkusze zestawów (wszystkie siatki warstwy, siatka gry z wypalonymi teksturami)", font=fb(26), fill=WH); d.text((16, 46), "16 płyt na maskę sąsiedztwa (× warianty zużycia w stylach B i D), uszczelka, błotnik, rampa, policzek, cztery dzioby. Pliki: hero4/hull_kit/<styl>/R4H_HullKit.glb / .fbx / _SOURCE.blend.", font=fr(13), fill=GR)
    for i, (s, k, lab) in enumerate(items):
        x, y = (i % 2)*cw, 96 + (i//2)*ch; p = os.path.join(H4, "hull_kit", lab, "hullkit_sheet.jpg"); B.paste(fit(Image.open(p).convert("RGB") if os.path.exists(p) else Image.new("RGB", (2400, 1500), (70, 20, 20)), cw - 4, ch - 4), (x + 2, y + 2)); info = json.load(open(os.path.join(H4, "hull_kit", lab, "hullkit.json"))) if os.path.exists(os.path.join(H4, "hull_kit", lab, "hullkit.json")) else {}
        d.text((x + 14, y + 10), SN[s] + (" · propozycja " + k.upper() if len(S[s]) > 1 else "") + " — %s siatek, %s trójkątów w zestawie" % (info.get("pieces", "?"), info.get("triangles_total", "?")), font=fb(15), fill=WH)
    save(B, "08d_hull_kit_sheets.jpg")
if __name__ == "__main__":
    whole(); category("Gun", "gun", "02_gun.jpg", "działko: lewa wzorzec z ankiety, prawa nowy model", (("deck", "na pokładzie robota"),)); category("Wheel", "wheel", "03_wheel.jpg", "koło: lewa wzorzec z ankiety, prawa nowy model"); category("Battery", "bat", "04_battery.jpg", "bateria: lewa wzorzec z ankiety, prawa nowy model")
    hull(); distance(); clay(); sources("Gun", "08a_source_gun.jpg"); sources("Wheel", "08b_source_wheel.jpg"); sources("Battery", "08c_source_battery.jpg"); kitsheets(); anim()
