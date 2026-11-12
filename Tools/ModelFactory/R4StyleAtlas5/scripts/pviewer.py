"""#1239 pilot 2, step 4: offline interactive preview + robot comparison boards.
 pilot2/preview/R4_ATLAS5_PILOT2_PREVIEW.zip = index.html + models.js: the REAL exported GLB files of the 40 variants (LOD0 + LOD1) embedded as
 base64; the embedded texture images are taken out of every GLB and stored ONCE (same bytes), the viewer hands them back to the loader.
 Robot boards (pilot2/boards/09_*, 10_*): the viewer itself, driven headless (three.js r128, SwiftShader) - identical robot constructions in the
 five styles: full materials, neutral material, wireframe, black silhouette. usage: python3 pviewer.py [--no-boards]"""
import os, sys, json, base64, hashlib, shutil, zipfile, struct, math, io, time
import numpy as np
import astyle as ST, pstyle as PS, pdefs as PD
PKG = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); P2 = os.path.join(PKG, "pilot2"); VAR = os.path.join(P2, "variants"); OUT = os.path.join(P2, "preview"); HERE = os.path.dirname(os.path.abspath(__file__))
MF = os.path.dirname(PKG); LIB = os.path.join(MF, "R4BlocksRedesign1232", "scripts", "render_web", "lib"); SRC = os.path.join(HERE, "viewer_src_pilot2"); G = 0.55
def strip_images(data):
    """GLB -> (GLB without embedded image bytes, {image name: png bytes}); images become external uris '<name>.png'"""
    jl = struct.unpack("<I", data[12:16])[0]; j = json.loads(data[20:20 + jl]); b0 = 20 + jl; bl = struct.unpack("<I", data[b0:b0 + 4])[0]; bin_ = data[b0 + 8:b0 + 8 + bl]; imgs = {}
    drop = {}
    for im in j.get("images", []):
        if "bufferView" in im:
            bv = j["bufferViews"][im["bufferView"]]; raw = bin_[bv.get("byteOffset", 0):bv.get("byteOffset", 0) + bv["byteLength"]]; name = im.get("name", "img") + ".png"; imgs[name] = raw; drop[im["bufferView"]] = name
            im.pop("bufferView"); im.pop("mimeType", None); im["uri"] = name
    nb = bytearray(); remap = {}; nv = []
    for i, bv in enumerate(j.get("bufferViews", [])):
        if i in drop: continue
        raw = bin_[bv.get("byteOffset", 0):bv.get("byteOffset", 0) + bv["byteLength"]]
        while len(nb) % 4: nb.append(0)
        b2 = dict(bv); b2["byteOffset"] = len(nb); nb += raw; remap[i] = len(nv); nv.append(b2)
    while len(nb) % 4: nb.append(0)
    j["bufferViews"] = nv; j["buffers"] = [{"byteLength": len(nb)}]
    for a in j.get("accessors", []):
        if "bufferView" in a: a["bufferView"] = remap[a["bufferView"]]
    js = json.dumps(j, separators=(",", ":")).encode()
    while len(js) % 4: js += b" "
    out = b"glTF" + struct.pack("<II", 2, 12 + 8 + len(js) + 8 + len(nb)) + struct.pack("<I", len(js)) + b"JSON" + js + struct.pack("<I", len(nb)) + b"BIN\x00" + bytes(nb)
    t0 = None
    for an in j.get("animations", []):
        for sm in an["samplers"]:
            mn = j["accessors"][sm["input"]].get("min", [0])[0]; t0 = mn if t0 is None else min(t0, mn)
    return out, imgs, (t0 or 0.0)
def mat(tx, ty, tz, yaw=0.0):
    """Blender placement (m, yaw about +Z in degrees) -> row-major three.js matrix (x, z, -y)"""
    a = math.radians(yaw); c, s = math.cos(a), math.sin(a); return [c, 0, s, tx, 0, 1, 0, tz, -s, 0, c, -ty, 0, 0, 0, 1]
def robots():
    """two identical robot constructions built ONLY from the eight pilot types (cells of the 0.55 m grid; x right, -y forward, z up)"""
    def cell(x, j, z, yaw=0, c=(0.5, -1.0)): return mat((x - c[0])*G, (-j - c[1])*G, z*G, yaw)
    w = [dict(type="Cube", matrix=cell(x, j, 0)) for x in (0, 1) for j in (0, 1, 2)]
    w += [dict(type="Wheel", matrix=cell(2, j, 0)) for j in (0, 2)] + [dict(type="Wheel", matrix=cell(-1, j, 0, 180)) for j in (0, 2)]
    w += [dict(type="Gun", matrix=cell(0, 2, 1)), dict(type="Battery", matrix=cell(0, 1, 1)), dict(type="Deployer", matrix=cell(0, 0, 1)), dict(type="Strut", matrix=cell(1, 0, 1))]
    w += [dict(type="Trap", matrix=mat(0.1, 1.45, -0.39))]
    c2 = (0.5, -0.5); h = [dict(type="Cube", matrix=cell(x, j, 0, 0, c2)) for x in (0, 1) for j in (0, 1)] + [dict(type="Hover", matrix=cell(x, j, -1, 0, c2)) for x in (0, 1) for j in (0, 1)]
    h += [dict(type="Gun", matrix=cell(0, 1, 1, 0, c2)), dict(type="Battery", matrix=cell(1, 1, 1, 0, c2)), dict(type="Deployer", matrix=cell(0, 0, 1, 0, c2)), dict(type="Cube", matrix=cell(1, 0, 1, 0, c2))]
    return {"robot_wheeled": dict(name="Robot kołowy (6 klocków, 4 koła, działko, bateria, wyrzutnik, STRUT, pułapka)", parts=w, span=3.3, cy=0.25, groundY=-0.392, pieces=len(w)),
            "robot_hover": dict(name="Robot poduszkowy (5 klocków, 4 silniki, działko, bateria, wyrzutnik)", parts=h, span=2.3, cy=0.0, groundY=-1.05, pieces=len(h))}
DEFCLIP = dict(Gun="Fire", Wheel="Roll", Hover="Lift", Deployer="Deploy", Trap="Triggered"); ROBOCLIP = dict(Gun="AimYaw", Wheel="Roll", Hover="Lift")
def build():
    stage = os.path.join(OUT, "_stage"); shutil.rmtree(stage, ignore_errors=True); os.makedirs(os.path.join(stage, "lib")); models = {}; tex = {}; src = {}; types = {}; tagH = {}
    for t in PD.ORDER:
        a = PD.TYPES[t]; cid = a["cid"]; env = np.array(a["env"]); var = {}; t0 = {}; qa0 = None
        for s in ST.STYLES:
            d = os.path.join(VAR, ST.LABEL[s], cid); qa = json.load(open(os.path.join(d, cid + "_QA.json"))); qa0 = qa0 or qa
            for pre, fn in (("", cid + ".glb"), ("L/", cid + "_LOD1.glb")):
                raw = open(os.path.join(d, fn), "rb").read(); g, im, tz = strip_images(raw); models[f"{pre}{s}/{cid}"] = base64.b64encode(g).decode()
                src[f"{pre}{s}/{cid}"] = dict(file=os.path.relpath(os.path.join(d, fn), PKG).replace(os.sep, "/"), sha256=hashlib.sha256(raw).hexdigest(), bytes=len(raw), embedded_bytes_without_images=len(g))
                for n, b in im.items():
                    ref = open(os.path.join(P2, "textures", n), "rb").read() if os.path.exists(os.path.join(P2, "textures", n)) else None
                    src[f"{pre}{s}/{cid}"].setdefault("images", {})[n] = dict(sha256=hashlib.sha256(b).hexdigest(), same_bytes_as_package_texture=(ref == b)); tex.setdefault(n, b)
                if not pre: t0[s] = round(tz, 6)
            var[s] = dict(tris=qa["triangles"], lod=qa["triangles_lod1"], parts=qa.get("parts"), concept=PD.CONCEPT[t][s])
        cl = [dict(name=c["name"], start=c["start"], end=c["end"]) for c in qa0["animation"]["clips"]]; names = [c["name"] for c in cl]
        types[t] = dict(id=cid, name=PD.NAME_PL[t], frames=qa0["animation"]["frames"], clips=cl, defaultClip=DEFCLIP.get(t) if DEFCLIP.get(t) in names else "cała oś czasu", robotClip=ROBOCLIP.get(t, "Idle") if ROBOCLIP.get(t, "Idle") in names else None,
                        t0=t0, variants=var, span=round(float(np.linalg.norm(env[1][:2] - env[0][:2]))*0.9 + 0.3, 3), cx=round(float(env[0][0] + env[1][0])/2, 4), cz=round(float(env[0][1] + env[1][1])/2, 4), cy=round(float(env[0][2] + env[1][2])/2, 4),
                        groundY=round(float(env[0][2]) - 0.002, 4), world=a["family"] == "world_trap")
        tagH[t] = round(float(env[1][2] - env[0][2])/2 + 0.14, 3)
    rb = robots(); tagH.update({"robot_wheeled": 1.15, "robot_hover": 1.2})
    data = dict(types=types, order=PD.ORDER, robots=rb, styles={s: f"{s} · {ST.NAME[s]}" for s in ST.STYLES}, character={s: PS.CHARACTER_PL[s] for s in ST.STYLES}, tagH=tagH)
    with open(os.path.join(stage, "models.js"), "w", encoding="utf-8") as f:
        f.write("window.R4_DATA=" + json.dumps(data, separators=(",", ":"), ensure_ascii=False) + ";\nwindow.R4_TEX=" + json.dumps({n: "data:image/png;base64," + base64.b64encode(b).decode() for n, b in tex.items()}, separators=(",", ":")) + ";\nwindow.R4_MODELS=" + json.dumps(models, separators=(",", ":")) + ";\n")
    for fn in os.listdir(LIB): shutil.copy(os.path.join(LIB, fn), os.path.join(stage, "lib", fn))
    for fn in os.listdir(SRC): shutil.copy(os.path.join(SRC, fn), os.path.join(stage, fn))
    z = os.path.join(OUT, "R4_ATLAS5_PILOT2_PREVIEW.zip")
    with zipfile.ZipFile(z, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        for root, _, files in os.walk(stage):
            for fn in sorted(files):
                fp = os.path.join(root, fn); zi = zipfile.ZipInfo(os.path.relpath(fp, stage).replace(os.sep, "/"), date_time=(2026, 11, 12, 0, 0, 0)); zi.compress_type = zipfile.ZIP_DEFLATED; zf.writestr(zi, open(fp, "rb").read())
    json.dump(dict(models=src, shared_textures=sorted(tex), robots={k: dict(name=v["name"], pieces=v["pieces"], parts=[p["type"] for p in v["parts"]]) for k, v in rb.items()}), open(os.path.join(OUT, "embedded_models.json"), "w"), indent=1, ensure_ascii=False)
    print("viewer:", z, os.path.getsize(z), "bytes;", len(models), "GLB embedded;", len(tex), "shared textures; models.js", os.path.getsize(os.path.join(stage, "models.js")))
    return stage, rb
def boards(stage, rb):
    import subprocess, socket
    from playwright.sync_api import sync_playwright
    from PIL import Image, ImageDraw, ImageFont
    s = socket.socket(); s.bind(("127.0.0.1", 0)); port = s.getsockname()[1]; s.close()
    srv = subprocess.Popen(["python3", "-m", "http.server", str(port), "--bind", "127.0.0.1"], cwd=stage, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL); time.sleep(0.8); rep = {}
    fb = lambda n: ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", n); fr = lambda n: ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", n)
    try:
        with sync_playwright() as pw:
            br = pw.chromium.launch(executable_path="/usr/bin/google-chrome", args=["--no-sandbox", "--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"])
            W, H = 2200, 620; pg = br.new_page(viewport={"width": W, "height": H}); errs = []; pg.on("console", lambda m: errs.append(m.text[:200]) if m.type == "error" else None); pg.on("pageerror", lambda e: errs.append(str(e)[:300]))
            pg.goto(f"http://127.0.0.1:{port}/index.html"); pg.wait_for_function("window.ready===true", timeout=120000); pg.evaluate("() => { window.R4P.pause(true); window.R4P.chrome(false); }")
            def snap(st):
                info = pg.evaluate("s => window.R4P.set(s)", st); pg.wait_for_timeout(300); pg.evaluate("s => window.R4P.set(s)", st)
                png = base64.b64decode(pg.evaluate("() => document.querySelector('canvas').toDataURL('image/png').split(',')[1]")); return Image.open(io.BytesIO(png)).convert("RGB"), info
            for k, (key, title) in enumerate((("robot_wheeled", "robot kołowy"), ("robot_hover", "robot poduszkowy"))):
                rows = []
                for mode, lab in (("mat", "Pełne materiały i tekstury"), ("clay", "Neutralny materiał"), ("wire", "Wireframe"), ("sil", "Czarna sylwetka")):
                    im, info = snap(dict(scene=key, mode=mode, cam="34", time=0.9, anim=False)); rows.append((lab, im)); rep.setdefault(key, {})[mode] = info
                im2, _ = snap(dict(scene=key, mode="mat", cam="side", time=0.9, anim=False)); rows.insert(1, ("Pełne materiały · bok", im2))
                B = Image.new("RGB", (W + 150, 96 + len(rows)*H), (24, 26, 30)); d = ImageDraw.Draw(B); d.text((16, 10), f"#1239 pilot 2 · identyczna konstrukcja w pięciu stylach · {title}", font=fb(30), fill=(240, 240, 240))
                d.text((16, 52), f"{rb[key]['pieces']} elementów wyłącznie z ośmiu typów pilota, te same komórki siatki i gniazda w każdym stylu · rendery: podgląd three.js r128 z wyeksportowanych GLB (nie Unity)", font=fr(17), fill=(170, 176, 186))
                for c, s_ in enumerate(ST.STYLES): d.text((150 + int(W*(0.5 + (c - 2)*0.172)) - 90, 74), f"{s_} · {ST.NAME[s_]}", font=fb(17), fill=(255, 208, 120))
                for r, (lab, im) in enumerate(rows):
                    B.paste(im, (150, 96 + r*H))
                    for i, ln in enumerate(lab.replace(" i ", " i\n").replace(" · ", "\n· ").split("\n")): d.text((10, 96 + r*H + 14 + i*20), ln, font=fb(14), fill=(225, 225, 228))
                p = os.path.join(P2, "boards", f"{9 + k:02d}_Robot_{key.split('_')[1]}.jpg"); B.save(p, quality=84, optimize=True); print(p, rep[key]["mat"])
            # smoke test of the single-type scenes of the viewer (every type, every mode renders without errors)
            for t in PD.ORDER:
                for mode in ("mat", "sil"): rep.setdefault("types", {})[t + "/" + mode] = pg.evaluate("s => window.R4P.set(s)", dict(scene=t, mode=mode, cam="34", time=None, anim=True))
            im, _ = snap(dict(scene="Gun", mode="mat", cam="34", time=None, anim=True, clip="Fire")); pg.set_viewport_size({"width": 1700, "height": 900}); pg.evaluate("() => window.R4P.chrome(true)"); pg.evaluate("s => window.R4P.set(s)", dict(scene="Gun")); pg.evaluate("() => window.R4P.pause(false)"); pg.wait_for_timeout(6000); pg.screenshot(path=os.path.join(OUT, "viewer_screenshot.jpg"), quality=86, type="jpeg")
            rep["console_errors"] = errs; br.close()
    finally: srv.terminate()
    json.dump(rep, open(os.path.join(OUT, "viewer_selftest.json"), "w"), indent=1); print("viewer self-test: console errors", len(rep["console_errors"]), rep["console_errors"][:3])
if __name__ == "__main__":
    stage, rb = build()
    if "--no-boards" not in sys.argv: boards(stage, rb)
    if "--keep-stage" not in sys.argv: shutil.rmtree(stage)
