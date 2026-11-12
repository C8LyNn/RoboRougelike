"""#1239 Hero Benchmark Pass 3: numbers behind the boards and the README (numpy + PIL on the exported GLB files, no Blender).
 selection   hero3/report/selection.json     the five HYBRID robots as data: per part type the historical file, its PR, sha256, triangles, pipeline
 palette     hero3/report/palette.json       measured colours of every model (zone paint factors of Pass 1; mean texel colour per zone of the authored
                                             texture sets of Pass 2; light colour) and, per HYBRID robot, how far each mixed-in part is from the chassis
 sleek gun   hero3/report/sleek_gun.json + sleek_gun_silhouette.png   side / front silhouettes of the old and the new Sleek gun, area per rig group
usage: python3 h3report.py"""
import os, sys, io, json, hashlib
import numpy as np
from PIL import Image, ImageDraw
import h3gen as G
PKG = G.PKG; sys.path.insert(0, os.path.join(os.path.dirname(PKG), "R4BlocksRedesign1232", "scripts")); import glbio
CID = dict(Gun="R4W_Basic", Wheel="R4M_WheelM_Side_Offroad", Battery="R4X_Battery", Cube="R4B_Cube", Deployer="R4X_ExplosiveTrapDeployer"); PASS = dict(old="hero1", new="hero2"); PR = dict(old="#1270", new="#1274"); REP = os.path.join(PKG, "hero3", "report")
ZN = dict(R4P_Base="base", R4P_Secondary="accent", R4P_Trim="dark", R4P_Metal="metal", R4P_Wear="wear", R4P_Glow="light")
def g2b(P): P = np.asarray(P); return np.stack([P[..., 0], -P[..., 2], P[..., 1]], axis=-1)
def path(which, s, typ): return os.path.join(PKG, PASS[which], "variants", G.LABEL[s], CID[typ], CID[typ] + ".glb")
def lin2srgb(c): c = np.clip(np.asarray(c, float), 0, 1); return np.where(c <= 0.0031308, c*12.92, 1.055*c**(1/2.4) - 0.055)
def srgb2lin(c): c = np.asarray(c, float); return np.where(c <= 0.04045, c/12.92, ((c + 0.055)/1.055)**2.4)
def images(j, bin_):
    out = []
    for im in j.get("images", []):
        bv = j["bufferViews"][im["bufferView"]]; out.append(np.asarray(Image.open(io.BytesIO(bin_[bv.get("byteOffset", 0):bv.get("byteOffset", 0) + bv["byteLength"]])).convert("RGB"), float)/255.0)
    return out
def measure(which, s, typ):
    """per paint zone: displayed colour (sRGB 0..1), area share; light colour. Pass 1: factor x tiling texture is approximated by the factor (the
    tiling textures are neutral wear maps); Pass 2: area-weighted mean of the base-colour texels under the zone's triangles"""
    p = path(which, s, typ); j, bin_ = glbio.read_json_bin(p); ims = images(j, bin_); m = glbio.load(p); Z = {}; area = {}
    def tex(i): return ims[j["textures"][i]["source"]] if i is not None else None
    for me in j["meshes"]:
        for pr in me["primitives"]:
            mt = j["materials"][pr["material"]]; z = mt["name"]; pb = mt.get("pbrMetallicRoughness", {}); P = glbio.accessor(j, bin_, pr["attributes"]["POSITION"]).astype(float); I = glbio.accessor(j, bin_, pr["indices"]).astype(int).reshape(-1, 3)
            if len(I) == 0 or np.ptp(P, axis=0).max() < 1e-4: continue
            T = P[I]; ar = 0.5*np.linalg.norm(np.cross(T[:, 1] - T[:, 0], T[:, 2] - T[:, 0]), axis=1); fac = np.array(pb.get("baseColorFactor", [1, 1, 1, 1])[:3]); bt = pb.get("baseColorTexture"); em = np.array(mt.get("emissiveFactor", [0, 0, 0]), float); et = mt.get("emissiveTexture")
            if which == "new" and bt is not None and "TEXCOORD_0" in pr["attributes"]:
                UV = glbio.accessor(j, bin_, pr["attributes"]["TEXCOORD_0"]).astype(float)[I].mean(1); img = tex(bt["index"]); h, w = img.shape[:2]; px = img[np.clip((UV[:, 1] % 1.0*h).astype(int), 0, h - 1), np.clip((UV[:, 0] % 1.0*w).astype(int), 0, w - 1)]; col = srgb2lin(px)*fac
                if et is not None:
                    e = tex(et["index"]); ep = srgb2lin(e[np.clip((UV[:, 1] % 1.0*h*e.shape[0]//h).astype(int), 0, e.shape[0] - 1), np.clip((UV[:, 0] % 1.0*w*e.shape[1]//w).astype(int), 0, e.shape[1] - 1)]); lit = ep.max(1) > 0.25
                    if lit.any(): d = Z.setdefault("R4P_Glow", [np.zeros(3), 0.0]); d[0] += (ep[lit]*ar[lit, None]).sum(0); d[1] += ar[lit].sum(); area["R4P_Glow"] = area.get("R4P_Glow", 0) + ar[lit].sum(); col = col[~lit]; ar = ar[~lit]
            else: col = np.repeat((em if z == "R4P_Glow" and em.max() > 0 else fac)[None], len(ar), 0)
            if len(ar): d = Z.setdefault(z, [np.zeros(3), 0.0]); d[0] += (col*ar[:, None]).sum(0); d[1] += ar.sum(); area[z] = area.get(z, 0) + ar.sum()
    tot = sum(area.values()); out = {}
    for z, (c, a) in Z.items():
        if a > 0: out[ZN.get(z, z)] = dict(srgb=[round(float(x), 3) for x in lin2srgb(c/a)], linear=[round(float(x), 4) for x in c/a], area_share=round(float(area[z]/tot), 3))
    return dict(zones=out, triangles=m.tri_count(), pipeline="Pass 2: unique UV, baked normal / AO, authored base + ORM + emission" if which == "new" else "Pass 1: zone paint x tiling texture, real bevels, vertex shading", images=len(ims), file_mb=round(os.path.getsize(p)/1e6, 2))
def dist(a, b): return round(float(np.linalg.norm(np.array(a) - np.array(b))), 3)
def selection():
    S = json.load(open(os.path.join(PKG, "hero3", "shots.json"))); C = S["configs"]; out = {}; pal = {}
    for s in G.STYLES:
        mix = C[s]["hybrid"]["mix"]; parts = {}; pal[s] = {w: {t: measure(w, s, t) for t in ("Gun", "Wheel", "Battery")} for w in ("old", "new")}
        for typ in ("Gun", "Wheel", "Battery"):
            w = mix[typ]; p = path(w, s, typ); parts[typ] = dict(choice=w, source=PASS[w], pr=PR[w], owner_score=G.SCORES[s][typ], decided_here=typ in G.ZERO.get(s, {}), file=os.path.relpath(p, PKG).replace(os.sep, "/"), sha256=hashlib.sha256(open(p, "rb").read()).hexdigest(), triangles=pal[s][w][typ]["triangles"], triangles_other=pal[s]["new" if w == "old" else "old"][typ]["triangles"])
        for typ in ("Cube", "Deployer"):
            p = path("old", s, typ); parts[typ] = dict(choice="geometry of Pass 1 (the only one), palette of " + PASS[mix["chassis"]], source="hero1", pr="#1270", file=os.path.relpath(p, PKG).replace(os.sep, "/"), sha256=hashlib.sha256(open(p, "rb").read()).hexdigest(), triangles=glbio.load(p).tri_count())
        cnt = dict(Cube=23, Wheel=4, Gun=2, Battery=2, Deployer=1); kit = json.load(open(os.path.join(PKG, "hero1", "renders", G.LABEL[s], "scene_stats.json")))["shots"]["1_hero_after"]["hull_layer_triangles"]
        out[s] = dict(style=G.LABEL[s], chassis=dict(choice=mix["chassis"], owner_score=G.SCORES[s]["chassis"], decided_here="chassis" in G.ZERO.get(s, {}), meaning="cube + deployer + hull kit (hero1/hull_kit geometry) painted in the palette of " + PASS[mix["chassis"]]), parts=parts, identical_to=("NEW (Hero Pass 2)" if all(v == "new" for v in mix.values()) else None),
                      robot_triangles=dict(hybrid=sum(cnt[t]*parts[t]["triangles"] for t in cnt) + kit, hull_layer=kit), proposal_plus=C[s].get("hybrid_plus", {}).get("harm"))
        ch = mix["chassis"]; ref = {}                                                      # chassis reference colours: the palette the cubes / hull kit are painted in
        src = json.load(open(os.path.join(REP, "chassis_palette.json")))[s][ch]
        co = {}
        for typ in ("Gun", "Wheel", "Battery"):
            z = pal[s][mix[typ]][typ]["zones"]; co[typ] = dict(source=PASS[mix[typ]], same_pass_as_chassis=mix[typ] == ch, **{k + "_delta_srgb": dist(z[k]["srgb"], src[k]) for k in ("base", "accent", "dark", "light") if k in z and k in src}, light_srgb=z.get("light", {}).get("srgb"))
        out[s]["material_coherence"] = dict(chassis_palette_srgb=src, parts=co, passes_mixed=sorted({PASS[v] for v in mix.values()}))
    json.dump(out, open(os.path.join(REP, "selection.json"), "w"), indent=1); json.dump(pal, open(os.path.join(REP, "palette.json"), "w"), indent=1); return out
def chassis_palette():
    """palettes of the two passes for the kept structural parts (hstyle = Pass 1, h2pal = Pass 2), as displayed colours"""
    import hstyle as HS, h2pal as H2; out = {}
    for s in G.STYLES:
        o = HS.MAT[s]; n = H2.PAL[s]; g1 = HS.GLOW_RGB[s]                                 # Pass 1 has one signature light only for Sleek and Toon; elsewhere every part keeps its own
        out[s] = dict(old=dict(base=o["R4P_Base"][0], accent=o["R4P_Secondary"][0], dark=o["R4P_Trim"][0], metal=o["R4P_Metal"][0], **({"light": g1} if g1 else {})), new=dict(base=n["base"][0], accent=n["sec"][0], dark=n["dark"][0], metal=n["metal"][0], light=n["glow"][0]))
        out[s] = {k: {z: [round(float(x), 3) for x in lin2srgb(c)] for z, c in v.items()} for k, v in out[s].items()}
    json.dump(out, open(os.path.join(REP, "chassis_palette.json"), "w"), indent=1)
GROUP = dict(Base="base", Yaw="turret", Pitch="cradle", Ring="cradle", VentL="cradle", VentR="cradle", Barrel="barrel"); GCOL = dict(base=(120, 130, 150), turret=(200, 205, 215), cradle=(245, 245, 245), barrel=(255, 150, 60))
def sleek_gun():
    PX = 1500.0; X0, X1, Z0, Z1 = -0.66, 0.30, -0.28, 0.26; W = int((X1 - X0)*PX); H = int((Z1 - Z0)*PX); res = {}; tiles = []
    for which in ("old", "new"):
        m = glbio.load(path(which, "A", "Gun")); masks = {g: [Image.new("1", (W, H)), Image.new("1", (int(0.56*PX), H))] for g in GCOL}; tr = {}
        for pr in m.prims:
            g = GROUP.get(pr["node"].split("R4W_Basic_")[-1]); 
            if g is None or np.ptp(pr["pos"], axis=0).max() < 1e-4: continue
            V = g2b(pr["pos"]); tr[g] = tr.get(g, 0) + len(pr["idx"]); ds, df = ImageDraw.Draw(masks[g][0]), ImageDraw.Draw(masks[g][1])
            for t in pr["idx"]:
                ds.polygon([((V[i][1] - X0)*PX, (Z1 - V[i][2])*PX) for i in t], fill=1); df.polygon([((V[i][0] + 0.28)*PX, (Z1 - V[i][2])*PX) for i in t], fill=1)
        A = {g: np.asarray(masks[g][0], bool) for g in GCOL}; F = {g: np.asarray(masks[g][1], bool) for g in GCOL}; weapon = A["turret"] | A["cradle"] | A["barrel"]; fw = F["turret"] | F["cradle"] | F["barrel"]; a = lambda M: round(float(M.sum()/PX**2*1e4), 1)
        free = A["barrel"] & ~(A["cradle"] | A["turret"]); cols = np.where(free.any(0))[0]; cw = (A["cradle"] | A["barrel"]); th = cw.sum(0)/PX; fwd = slice(0, int((-0.10 - X0)*PX))
        rows = np.where(A["base"].any(1))[0]; wid = A["base"].sum(1)/PX; waist = float(wid[rows].min()); body = np.where(A["base"].any(1))[0]
        res[which] = dict(pass_=PASS[which], triangles=tr, side_area_cm2=dict(base=a(A["base"]), weapon=a(weapon), barrel_free=a(free), total=a(A["base"] | weapon)), front_area_cm2=dict(base=a(F["base"]), weapon=a(fw)),
                          base_share_of_side_silhouette=round(float(A["base"].sum()/(A["base"] | weapon).sum()), 3), base_fill_of_its_box=round(float(A["base"].sum()/(np.ptp(np.where(A["base"].any(0))[0])*np.ptp(rows))), 3), base_min_width_m=round(waist, 3), base_max_width_m=round(float(wid.max()), 3),
                          barrel_free_length_m=round(float(len(cols)/PX), 3), barrel_free_mean_height_m=round(float(free.sum(0)[cols].mean()/PX), 3), weapon_forward_mean_height_m=round(float(th[fwd][th[fwd] > 0].mean()), 3), weapon_forward_max_height_m=round(float(th[fwd].max()), 3))
        im = Image.new("RGB", (W, H), (24, 26, 32)); px = np.asarray(im).copy()
        for g in ("base", "turret", "cradle", "barrel"): px[A[g]] = GCOL[g]
        tiles.append(Image.fromarray(px))
    o, n = res["old"], res["new"]; res["delta"] = {k: round(n[k] - o[k], 3) for k in o if isinstance(o[k], float)}; res["units"] = "side view = plane of the barrel axis; areas in cm2 at 1:1; base = fixed pedestal (Body), weapon = turret + cradle + barrel"
    S = Image.new("RGB", (W*2 + 30, H), (24, 26, 32)); S.paste(tiles[0], (0, 0)); S.paste(tiles[1], (W + 30, 0)); S.save(os.path.join(REP, "sleek_gun_silhouette.png")); json.dump(res, open(os.path.join(REP, "sleek_gun.json"), "w"), indent=1); return res
if __name__ == "__main__":
    os.makedirs(REP, exist_ok=True); chassis_palette(); sel = selection(); g = sleek_gun()
    for s, v in sel.items(): print(s, v["chassis"]["choice"], {t: p["choice"] if t in ("Gun", "Wheel", "Battery") else "-" for t, p in v["parts"].items()}, v["robot_triangles"], v["material_coherence"]["parts"])
    print(json.dumps(g, indent=0)[:2500])
