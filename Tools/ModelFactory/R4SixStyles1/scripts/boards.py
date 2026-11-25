"""Compose the review boards. Every picture on them is a render of the delivered GLB files (s6render.py); the boards only crop,
scale, place and label. usage: python3 boards.py RENDERS_DIR OUT_DIR"""
import os, json, sys
from PIL import Image, ImageDraw, ImageFont, ImageOps
R = sys.argv[1] if len(sys.argv) > 1 else "renders"; OUT = sys.argv[2] if len(sys.argv) > 2 else "boards"; os.makedirs(OUT, exist_ok=True)
STY = [("1", "01 Clean Futuristic"), ("2", "02 Brutal Industrial"), ("3", "03 Arcade Low-Poly"), ("4", "04 Gritty Industrial"), ("5", "05 Comic Outline"), ("6", "06 R41 Modular Tech")]
EL = [("gun", "Działo", "R4W_Basic"), ("wheel", "Koło", "R4M_Wheel"), ("dep", "Wyrzutnik", "R4X_FlyingDroneDeployer"), ("drone", "Dron", "FlyingDrone")]
_H = os.path.dirname(os.path.abspath(__file__)); _SP = [q for q in (os.path.join(_H, "status.json"), os.path.join(_H, "..", "status.json")) if os.path.exists(q)][0]
STATUS = json.load(open(_SP, encoding="utf-8"))
COL = {"ZACHOWANY": (120, 200, 255), "PRZEROBIONY": (255, 200, 90), "NOWY": (120, 230, 140)}
BG = (22, 23, 26); FG = (235, 236, 238); MUT = (150, 154, 162); LINE = (60, 62, 68)


def font(sz, bold=True):
    for f in ("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf" if bold else "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",):
        try: return ImageFont.truetype(f, sz)
        except Exception: pass
    return ImageFont.load_default()


def img(name, grey=False):
    p = os.path.join(R, name)
    if not os.path.exists(p) and name.endswith(".png"): p = p[:-4] + ".jpg"   # renders/ in the repository holds jpg copies
    if not os.path.exists(p): return None
    im = Image.open(p)
    if im.mode == "RGBA": b = Image.new("RGB", im.size, (170, 172, 176)); b.paste(im, mask=im.split()[3]); im = b
    im = im.convert("RGB")
    return ImageOps.grayscale(im).convert("RGB") if grey else im


def centre_crop(im, f):
    w, h = im.size; cw, ch = int(w*f), int(h*f); return im.crop(((w - cw)//2, (h - ch)//2, (w - cw)//2 + cw, (h - ch)//2 + ch))


def tag(d, x, y, st):
    c = COL[st]; d.rectangle((x, y, x + 14*len(st) + 22, y + 32), fill=(18, 18, 20)); d.text((x + 9, y + 5), st, fill=c, font=font(19))


def wrap(d, xy, text, fnt, fill, width):
    x, y = xy; line = ""
    for w in text.split(" "):
        t = (line + " " + w).strip()
        if d.textlength(t, font=fnt) > width and line: d.text((x, y), line, fill=fill, font=fnt); y += fnt.size + 6; line = w
        else: line = t
    if line: d.text((x, y), line, fill=fill, font=fnt); y += fnt.size + 6
    return y


# ------------------------------------------------------------------ board 1: 6 styles x 4 elements (+ drone crop row)
def board_compare(name, grey=False, labels=True, cell=560, mode="color", status=True, before=False, title="", sub=""):
    rows = [(e, a, b, None) for (e, a, b) in EL] + ([] if before else [("drone", "Dron", "wycinek 2x z tego samego renderu", 0.5)])
    LW = 260 if labels else 0; HH = 130 if labels else 0; FH = 46 if labels else 0
    S = Image.new("RGB", (LW + 6*cell, HH + len(rows)*cell + FH), BG); d = ImageDraw.Draw(S)
    if labels:
        d.text((22, 16), title, fill=FG, font=font(36)); d.text((22, 64), sub, fill=MUT, font=font(19, False))
        for i, (s, lab) in enumerate(STY): d.text((LW + i*cell + 14, HH - 36), lab, fill=FG, font=font(23))
    for r, (el, a, b, crop) in enumerate(rows):
        y = HH + r*cell
        if labels:
            d.text((18, y + 22), a, fill=FG, font=font(26)); wrap(d, (18, y + 58), b, font(17, False), MUT, LW - 30)
        for i, (s, lab) in enumerate(STY):
            x = LW + i*cell
            if before: im = img("before_%s_%s.jpg" % (el, s), grey)
            else:
                fn = "el_%s_%s_%s.jpg" % (el, s, mode)
                if mode == "clay" and s == "5": fn = "el_%s_5_nooutline.jpg" % el      # the greyscale test compares form, not the outline
                im = img(fn, grey)
            if im is None:
                d.rectangle((x + 6, y + 6, x + cell - 6, y + cell - 6), outline=(80, 80, 86), width=2)
                if labels: d.text((x + 40, y + cell//2 - 30), "brak modelu", fill=MUT, font=font(28)); d.text((x + 40, y + cell//2 + 8), "(nie istnieje w repo ani w grze)", fill=MUT, font=font(16, False))
                continue
            if crop: im = centre_crop(im, crop)
            S.paste(im.resize((cell - 4, cell - 4), Image.LANCZOS), (x + 2, y + 2))
            if status and labels and not crop: tag(d, x + 10, y + cell - 46, STATUS[el][s]["status"])
    if labels and before: d.text((22, S.height - 34), "Render istniejących plików GLB z repozytorium (gałęzie art/1239-atlas5-hero4, art/938, art/940, art/1239-atlas5-w4), ta sama kamera co na planszy po zmianach.", fill=MUT, font=font(16, False))
    elif labels: d.text((22, S.height - 34), "Render z dostarczonych plików GLB. Ta sama kamera (2,3 m, 50 mm), światło, tło i skala w każdym wierszu i kolumnie; dron w klatce zawisu (Hover). "
                      "ZACHOWANY = oryginał bez zmian geometrii, PRZEROBIONY = istniejący model rozwinięty, NOWY = zbudowany od zera.", fill=MUT, font=font(16, False))
    S.save(os.path.join(OUT, name), quality=90); return S.size


# ------------------------------------------------------------------ board 2: player view
def board_player(name="02_perspektywa_gracza.jpg"):
    TW, TH = 900, 506; CW, CH = 560, 420; KW = 700; KH = 500
    W = 60 + 3*(TW + 30); rowh = TH + CH + KH + 120; H = 150 + 2*rowh + 60
    S = Image.new("RGB", (W, H), BG); d = ImageDraw.Draw(S)
    d.text((30, 18), "Perspektywa gracza - ten sam pojazd w sześciu stylach", fill=FG, font=font(36))
    d.text((30, 68), "Pojazd: 3x3 kostki pancerza + 3 kostki podwozia, 4 koła R4M_Wheel (lewe odbite lustrzanie), działo R4W_Basic z przodu, wyrzutnik na kadłubie z tyłu, dron po starcie (Hover).", fill=MUT, font=font(18, False))
    d.text((30, 96), "Kamera gracza: 1920x1080, obiektyw 32 mm, pojazd ok. 20 m od kamery. Środkowy pasek = wycinek 1:1 pikseli z tej klatki (rozmiar jak w grze). Dolny pasek = bliski widok 3/4 tego samego układu.", fill=MUT, font=font(18, False))
    for i, (s, lab) in enumerate(STY):
        cx = 30 + (i % 3)*(TW + 30); cy = 150 + (i//3)*rowh
        d.text((cx, cy), lab, fill=FG, font=font(24))
        full = img("veh_player_%s.png" % s)
        if full:
            S.paste(full.resize((TW, TH), Image.LANCZOS), (cx, cy + 36)); sx = TW/1920; d.rectangle((cx + 560*sx, cy + 36 + 560*sx, cx + 1120*sx, cy + 36 + 980*sx), outline=(255, 210, 90), width=2)
            S.paste(full.crop((560, 560, 1120, 980)), (cx + (TW - CW)//2, cy + 46 + TH))
            d.text((cx, cy + 50 + TH), "1:1", fill=(255, 210, 90), font=font(20))
        k = img("veh_close_%s.jpg" % s)
        if k: S.paste(k.resize((KW, KH), Image.LANCZOS), (cx + (TW - KW)//2, cy + 56 + TH + CH))
    S.save(os.path.join(OUT, name), quality=90); return S.size


# ------------------------------------------------------------------ board 3: deployer + drone pair, top view, launch
def board_pair(name="03_wyrzutnik_i_dron.jpg"):
    CW = 520; TOP = 250; W = 300 + 6*CW; H = TOP + 6*CW + 30
    S = Image.new("RGB", (W, H), BG); d = ImageDraw.Draw(S)
    SHARED = {"1": "Wspólna cecha: wtopiony wentylator w gładkiej skorupie z cyjanowym pierścieniem - pierścień gniazda powtarza pierścienie dysz drona.",
              "2": "Wspólna cecha: kwadratowa rama z profili z żółtymi narożnikami - klatka wyrzutnika i rama H drona z tych samych belek.",
              "3": "Wspólna cecha: heksagon + limonkowy trójkąt - sześciokątny pokład i maszt drona, trójkąt na gnieździe i statecznik drona.",
              "4": "Wspólna cecha: bęben/beczka z żebrami, obejmą i taśmą - pół-beczka wyrzutnika i bęben korpusu drona.",
              "5": "Wspólna cecha: gruby torus-bulaj - pokrywa gniazda i ogromne oko drona to ta sama forma.",
              "6": "Wspólna cecha: gunmetalowy ośmiokąt + czerwony pas + pomarańczowe oko (cechy R4.1) na obu."}
    d.text((30, 18), "Wyrzutnik + dron: wspólna cecha konstrukcyjna, gniazdo z góry, sekwencja startu", fill=FG, font=font(36))
    d.text((30, 68), "Wiersze: para (dron zadokowany + dron po starcie), widok z góry na gniazdo startowe, start: zadokowany / unosi się / odlatuje, ta sama para w szarości.", fill=MUT, font=font(18, False))
    for i, (s, lab) in enumerate(STY): d.text((300 + i*CW + 12, 116), lab, fill=FG, font=font(22)); wrap(d, (300 + i*CW + 12, 150), SHARED[s], font(16, False), MUT, CW - 30)
    rows = [("Para", lambda s: img("pair_%s_color.jpg" % s), 0.0), ("Z góry", lambda s: img("pair_%s_top.jpg" % s), 0.0),
            ("Start 1/3", lambda s: img("launch_%s_0.jpg" % s), 0.0), ("Start 2/3", lambda s: img("launch_%s_1.jpg" % s), 0.0),
            ("Start 3/3", lambda s: img("launch_%s_2.jpg" % s), 0.0), ("Szarość", lambda s: img("pair_%s_clay_nooutline.jpg" % s if s == "5" else "pair_%s_clay.jpg" % s, True), 0.0)]
    SH = {"Para": "dron zadokowany na gnieździe i ten sam dron w powietrzu", "Z góry": "gniazdo startowe musi być czytelne z góry",
          "Start 1/3": "klatka Docked", "Start 2/3": "klatka Launch", "Start 3/3": "klatka Fly", "Szarość": "bez koloru (05 bez konturu)"}
    for r, (lab, fn, _) in enumerate(rows):
        y = TOP + r*CW; d.text((24, y + 20), lab, fill=FG, font=font(26)); wrap(d, (24, y + 58), SH[lab], font(17, False), MUT, 250)
        for i, (s, _l) in enumerate(STY):
            im = fn(s)
            if im is None: continue
            w, h = im.size; sc = (CW - 6)/max(w, h); im = im.resize((int(w*sc), int(h*sc)), Image.LANCZOS)
            S.paste(im, (300 + i*CW + (CW - im.width)//2, y + (CW - im.height)//2))
    S.save(os.path.join(OUT, name), quality=90); return S.size


# ------------------------------------------------------------------ board 4: R4.1 family
def board_r41(name="04_rodzina_R41.jpg"):
    a = img("r41_family.jpg"); b = img("r41_family_clay.jpg"); W = a.width + 60; H = 190 + a.height + b.height + 260
    S = Image.new("RGB", (W, H), BG); d = ImageDraw.Draw(S)
    d.text((30, 18), "06 R41 Modular Tech - stare modele obok nowych z tej samej rodziny", fill=FG, font=font(36))
    d.text((30, 68), "Od lewej: działo R4W_Basic (oryginał R4, PR #939, ZACHOWANY), koło R4M_Wheel (oryginał R4, PR #941, ZACHOWANY), wyrzutnik (PRZEROBIONY), dron (NOWY).", fill=MUT, font=font(19, False))
    S.paste(a, (30, 120)); S.paste(b, (30, 130 + a.height)); y = 150 + a.height + b.height
    f = ["1. Gunmetalowe ośmiokątne/fazowane płyty ze szczelinami, żaluzjami i śrubami (cokół działa, osłona wyrzutnika, korpus drona).",
         "2. Poziomy czerwony pas drużyny + żółte znaczniki ostrzegawcze na czarnym kwadratowym cokole z narożnymi zaczepami.",
         "3. Pomarańczowy toczony pierścień-\"oko\" na bocznej ścianie / piaście / płycie mocowania (działo, koło, wyrzutnik, dron)."]
    d.text((30, y), "Trzy powtarzane cechy R4.1 (z modeli R41_Turret_v6.1 / R4W_Basic / R4M_Wheel) przeniesione na brakujące elementy:", fill=FG, font=font(22)); y += 40
    for t in f: d.text((30, y), t, fill=MUT, font=font(19, False)); y += 34
    S = S.crop((0, 0, W, y + 20)); S.save(os.path.join(OUT, name), quality=90); return S.size


# ------------------------------------------------------------------ board 5: 05 with / without outline
def board_outline(name="05_kontur_z_i_bez.jpg"):
    C = 520; W = 280 + 4*C; H = 150 + 3*C + 40 + 460
    S = Image.new("RGB", (W, H), BG); d = ImageDraw.Draw(S)
    d.text((30, 18), "05 Comic Outline - pełny kolor z konturem i cieniem vs bez konturu", fill=FG, font=font(36))
    d.text((30, 68), "Kontur: odwrócona powłoka 7,5 mm, materiał R4P_Outline (back-face culling). Sprawdzenie: czy kontur nie zalewa piasty koła ani szczeliny dron-gniazdo.", fill=MUT, font=font(18, False))
    for i, (e, a, b) in enumerate(EL): d.text((280 + i*C + 12, 112), a, fill=FG, font=font(24))
    rows = [("z konturem", lambda e: img("el_%s_5_color.jpg" % e)), ("kolor\nbez konturu", lambda e: img("el_%s_5_nooutline_color.jpg" % e)), ("szarość\nbez konturu", lambda e: img("el_%s_5_nooutline.jpg" % e))]
    rows = [r for r in rows if any(r[1](e) is not None for e, _, _ in EL)]
    for r, (lab, fn) in enumerate(rows):
        y = 150 + r*C
        for k, line in enumerate(lab.split("\n")): d.text((24, y + 20 + 32*k), line, fill=FG, font=font(24))
        for i, (e, a, b) in enumerate(EL):
            im = fn(e)
            if im is None: continue
            if e == "drone": im = centre_crop(im, 0.5)
            S.paste(im.resize((C - 4, C - 4), Image.LANCZOS), (280 + i*C + 2, y + 2))
    y = 160 + len(rows)*C; d.text((24, y), "Pojazd z perspektywy gracza, wycinek 1:1", fill=FG, font=font(22))
    for i, f in enumerate(("veh_player_5.png", "veh_player_5_nooutline.png")):
        im = img(f)
        if im: S.paste(im.crop((560, 560, 1120, 980)), (280 + i*620, y + 36)); d.text((280 + i*620, y + 36 + 424), ("z konturem", "bez konturu")[i], fill=MUT, font=font(18, False))
    S = S.crop((0, 0, W, y + 36 + 460)); S.save(os.path.join(OUT, name), quality=90); return S.size


def board_before_after(name="00_przed_i_po.jpg", cell=380):
    LW = 270; HH = 140; rows = []
    for e, a, b in EL: rows += [(e, a, "przed", True), (e, a, "po", False)]
    S = Image.new("RGB", (LW + 6*cell, HH + len(rows)*cell + 50), BG); d = ImageDraw.Draw(S)
    d.text((22, 16), "Przed i po - te same pozycje, ta sama kamera", fill=FG, font=font(36))
    d.text((22, 64), "Wiersz \"przed\" = najlepszy istniejący model (repo). Wiersz \"po\" = dostarczony model z oznaczeniem: ZACHOWANY / PRZEROBIONY / NOWY.", fill=MUT, font=font(19, False))
    for i, (s_, lab) in enumerate(STY): d.text((LW + i*cell + 12, HH - 34), lab, fill=FG, font=font(21))
    for r, (el, a, w, bef) in enumerate(rows):
        y = HH + r*cell
        if not bef: d.line((LW, y + cell, LW + 6*cell, y + cell), fill=(90, 92, 100), width=3)
        d.text((18, y + 22), a if bef else "", fill=FG, font=font(26)); d.text((18, y + (60 if bef else 22)), w, fill=(255, 210, 90) if not bef else MUT, font=font(22))
        for i, (s_, lab) in enumerate(STY):
            x = LW + i*cell; im = img(("before_%s_%s.jpg" % (el, s_)) if bef else ("el_%s_%s_color.jpg" % (el, s_)))
            if im is None:
                d.rectangle((x + 6, y + 6, x + cell - 6, y + cell - 6), outline=(80, 80, 86), width=2); d.text((x + 30, y + cell//2 - 16), "brak modelu", fill=MUT, font=font(24)); continue
            if el == "drone": im = centre_crop(im, 0.6)
            S.paste(im.resize((cell - 4, cell - 4), Image.LANCZOS), (x + 2, y + 2))
            if not bef: tag(d, x + 8, y + cell - 42, STATUS[el][s_]["status"])
    d.text((22, S.height - 36), "Dron: wycinek 0,6 kadru (dron jest mały w kadrze o wspólnej skali). Działo 03 i koło 03: poprzednie wersje C_Arcade były w szarości bliźniacze z E_Toon - zbudowane od nowa.", fill=MUT, font=font(16, False))
    S.save(os.path.join(OUT, name), quality=90); return S.size


# ------------------------------------------------------------------ moodboard TEST 128 PX: every object cropped and scaled to 128 px, shown 2x (nearest)
def board_128(name="06_test_128px.jpg"):
    CROP = {"gun": 0.78, "wheel": 0.80, "dep": 0.72, "drone": 0.42}; Z = 2; C = 128*Z + 8; LW = 200; HH = 150
    rows = [("veh", "Pojazd")] + [(e, a) for (e, a, b) in EL]; BW = LW + 6*C
    S = Image.new("RGB", (BW, HH + 2*(len(rows)*C + 60) + 20), BG); d = ImageDraw.Draw(S)
    d.text((22, 16), "TEST 128 PX (moodboard): obiekt przeskalowany do 128 px, pokazany 2x", fill=FG, font=font(30))
    wrap(d, (22, 60), "Kryteria z moodboardu: 01 smukły kadłub i światło · 02 masywne krawędzie i tłoki · 03 prosty układ kolorowych klocków · 04 zarysowane płyty, różne koła, chaotyczna sylwetka · 05 komiksowa kreska widoczna bez koloru · 06 modułowość czytelna bez barwy.", font(16, False), MUT, BW - 44)
    for i, (s_, lab) in enumerate(STY): d.text((LW + i*C + 6, HH - 34), lab[:2] + " " + lab[3:].split()[0], fill=FG, font=font(20))
    for blk, grey in enumerate((False, True)):
        y0 = HH + blk*(len(rows)*C + 60)
        d.text((22, y0 + 6), "w szarości" if grey else "w kolorze", fill=(255, 210, 90), font=font(22))
        for r, (el, lab) in enumerate(rows):
            y = y0 + 40 + r*C; d.text((22, y + 50), lab, fill=FG, font=font(22))
            for i, (s_, _l) in enumerate(STY):
                if el == "veh": im = img("veh_close_%s.jpg" % s_, grey)
                else: im = img("el_%s_%s_color.jpg" % (el, s_), grey)
                if im is None: continue
                if el == "veh":
                    w, h = im.size; cw = int(w*0.56); cx0 = int(w*0.08); cy0 = int(h*0.16); im = im.crop((cx0, cy0, cx0 + cw, cy0 + cw))
                else: im = centre_crop(im, CROP[el])
                im = im.resize((128, 128), Image.LANCZOS).resize((128*Z, 128*Z), Image.NEAREST)
                S.paste(im, (LW + i*C + 4, y + 4))
    S.save(os.path.join(OUT, name), quality=92); return S.size


# ------------------------------------------------------------------ board 7: every model from five sides (sense review)
def board_insp(el, name):
    INS = os.path.join(R, "inspekcja") if os.path.isdir(os.path.join(R, "inspekcja")) else os.path.join(os.path.dirname(R.rstrip("/")), "insp"); C = 400; LW = 300; HH = 120
    views = [("color", "przód 3/4"), ("back", "tył 3/4"), ("side", "bok zewn."), ("sideL", "bok wewn."), ("top", "z góry")]
    S = Image.new("RGB", (LW + 5*C, HH + 6*C + 20), BG); d = ImageDraw.Draw(S)
    lab = {e: a for (e, a, b) in EL}[el]
    d.text((22, 16), "%s - każdy model z pięciu stron (przegląd sensu)" % lab, fill=FG, font=font(32))
    d.text((22, 60), "Po kontroli s6sense: nic nie wisi w powietrzu, ruchome części nie przechodzą przez resztę, dron leży na gnieździe.", fill=MUT, font=font(17, False))
    for i, (v, t) in enumerate(views): d.text((LW + i*C + 10, HH - 32), t, fill=FG, font=font(20))
    for r, (s_, sl) in enumerate(STY):
        y = HH + r*C; d.text((18, y + 20), sl, fill=FG, font=font(20)); tag(d, 18, y + 56, STATUS[el][s_]["status"])
        for i, (v, t) in enumerate(views):
            fn_ = os.path.join(R, "el_%s_%s_color.jpg" % (el, s_)) if v == "color" else os.path.join(INS, "insp_%s_%s_%s.jpg" % (el, s_, v))
            if not os.path.exists(fn_): continue
            im = Image.open(fn_).convert("RGB"); w, h = im.size; k = 0.62 if el != "drone" else 0.46
            im = im.crop((int(w*(1 - k)/2), int(h*(1 - k)/2), int(w*(1 + k)/2), int(h*(1 + k)/2)))
            S.paste(im.resize((C - 4, C - 4), Image.LANCZOS), (LW + i*C + 2, y + 2))
    S.save(os.path.join(OUT, name), quality=88); return S.size


if __name__ == "__main__":
    print(board_before_after())
    print(board_compare("01_porownanie_6_stylow.jpg", title="RoboRougelike - seria 1: cztery elementy w sześciu stylach",
                        sub="Kolumny: styl w stałej kolejności. Wiersze: element w tej samej pozycji. Ostatni wiersz: ten sam dron, wycinek 2x z tego samego renderu."))
    print(board_compare("01g_szarosc_bez_podpisow.jpg", grey=True, labels=False, cell=420, mode="clay", status=False))
    print(board_compare("01b_przed.jpg", cell=420, status=False, before=True, title="Stan wyjściowy (przed zmianami): najlepsze istniejące modele",
                        sub="Działo: Pass 4 (#1294) A/B/C/D/E + oryginał R4 (#939). Koło: Pass 4 R4M_WheelM_Side_Offroad (inna część) + oryginał R4M_Wheel (#941). Wyrzutnik: atlas A-E + 1x1. Dron: nie istnieje."))
    print(board_player()); print(board_pair()); print(board_r41()); print(board_outline()); print(board_128())
    for el, nm in (("gun", "07a_inspekcja_dzialo.jpg"), ("wheel", "07b_inspekcja_kolo.jpg"), ("dep", "07c_inspekcja_wyrzutnik.jpg"), ("drone", "07d_inspekcja_dron.jpg")): print(board_insp(el, nm))
