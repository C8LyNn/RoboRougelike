"""step 3: independent numpy tests on the EXPORTED GLB files (no Blender). usage: python3 tests_modules.py
 M1 file / budget     GLB + LOD1 load, triangle counts equal the Blender QA record, materials are the five paint zones
 M2 envelope          rest-pose geometry inside the declared envelope (module: 0.52 m block, world trap: one cell footprint on the ground)
 M3 contacts          every declared Port_* empty exists at its place; approved patch_test ruler on the exported triangles (LOD0 and LOD1)
 M4 mount             Socket_Mount on the declared mount face, axis = face normal (modules only)
 M5 neighbour gap     module placed on / under / beside an A_Sleek Cube in the mount direction: 30 mm grid gap kept, no intersection
 M6 silhouette        orthographic front / side / top masks of everything above the shared mount plinth (z > -0.185 m);
                      a model FAILS if ALL three views overlap another model by IoU > 0.80
 M7 animation         modules with movable nodes carry an animation in the GLB; node names match the manifest"""
import json, os, sys, itertools
import numpy as np
from PIL import Image, ImageDraw
import mkit as K, paths_m as P, glbio, raycast
G, H = 0.55, 0.26
ZONES = set(K.ZONE_ORDER)
def g2b(Pn): Pn = np.asarray(Pn); return np.stack([Pn[..., 0], -Pn[..., 2], Pn[..., 1]], axis=-1)        # file (x, y, z) -> Blender (x, -z, y)
def load(ID, lod=False):
    m = glbio.load(P.glb(ID, lod)); T = g2b(m.tris()); return m, T
def masks(T, env, n=72):
    out = []
    for (a, b) in ((0, 2), (1, 2), (0, 1)):
        im = Image.new("1", (n, n), 0); d = ImageDraw.Draw(im)
        for t in T:
            pts = [((p[a] - env[0][a])/(env[1][a] - env[0][a])*(n - 1), (1 - (p[b] - env[0][b])/(env[1][b] - env[0][b]))*(n - 1)) for p in t]; d.polygon(pts, fill=1)
        out.append(np.array(im, bool))
    return out
def main():
    ids = P.ids(); rep = {}; M = {}; ok_all = True
    cube = glbio.load(P.cube_glb()); clo, chi = [g2b(x) for x in cube.bounds()]; clo, chi = np.minimum(clo, chi), np.maximum(clo, chi)
    for ID in ids:
        qa = json.load(open(os.path.join(P.mdir(ID), ID + "_QA.json"))); m, T = load(ID); ml, TL = load(ID, True); r = {}
        env = np.array(qa["envelope_m"]); 
        jm = {x["name"] for x in m.json.get("materials", [])}
        r["M1"] = dict(tris=len(T), tris_lod1=len(TL), qa_tris=qa["triangles"], materials=sorted(jm), PASS=len(T) == qa["triangles"] and len(TL) == qa["triangles_lod1"] and len(TL) < len(T) and jm <= ZONES)
        V = T.reshape(-1, 3); VL = TL.reshape(-1, 3); out = max(float((env[0] - V).max()), float((V - env[1]).max()), float((env[0] - VL).max()), float((VL - env[1]).max()))
        r["M2"] = dict(bounds=[V.min(0).round(4).tolist(), V.max(0).round(4).tolist()], outside_mm=round(max(out, 0)*1000, 3), PASS=out <= 0.0005)
        pt = []; emp = {k.split(".")[0]: np.array(v[0]) for k, v in m.empties.items()}
        for p in qa["ports_proposal"]:
            name = f"Port_{p['dir']}_000" + ("" if p["kind"] == "full" else "_part"); u, v, n = K.frame(K.DIRS[p["dir"]]); want = n*H + (u*p["offset"][0] + v*p["offset"][1])*G
            have = name in emp; dev = float(np.linalg.norm(g2b(emp[name][:3, 3]) - want))*1000 if have else None
            a = raycast.patch_test(T, p["cell"], p["dir"], p["offset"], p["half"]); b = raycast.patch_test(TL, p["cell"], p["dir"], p["offset"], p["half"]); need = 80 if p["kind"] == "full" else 65
            good = have and dev < 0.05 and all(x["protruding"] == 0 and x["missing"] == 0 and x["flush_pct"] >= need for x in (a, b))
            pt.append(dict(port=name, empty_dev_mm=None if dev is None else round(dev, 4), flush_pct=a["flush_pct"], lod1_flush_pct=b["flush_pct"], PASS=bool(good)))
        r["M3"] = dict(ports=pt, PASS=all(x["PASS"] for x in pt))
        if not qa["world_object"]:
            n = K.DIRS[qa["mount_proposal"]]; s = emp.get("Socket_Mount")
            if s is None: r["M4"] = dict(PASS=False, why="no Socket_Mount")
            else:
                pos = g2b(s[:3, 3]); zax = g2b(s[:3, 1]);      # Blender local +Z of the empty = glTF local +Y
                r["M4"] = dict(pos=pos.round(4).tolist(), axis=zax.round(3).tolist(), PASS=bool(np.linalg.norm(pos - n*H) < 1e-4 and zax @ n > 0.9999))
            c = n*G; gap = float(np.max(np.maximum(clo + c - V.max(0), V.min(0) - (chi + c)))); r["M5"] = dict(neighbour="A_Sleek Cube at " + qa["mount_proposal"], gap_mm=round(gap*1000, 2), PASS=abs(gap - 0.03) < 0.0006 or gap > 0.03)
        else: r["M4"] = dict(PASS=True, note="world object: no mount socket"); r["M5"] = dict(PASS=True, note="world object: stands on the ground (z = 0)", ground_z_mm=round(float(V[:, 2].min())*1000, 3))
        if qa["world_object"]: r["M5"]["PASS"] = abs(float(V[:, 2].min())) < 0.0005
        an = len(m.json.get("animations", [])); nodes = sorted(qa["movable_nodes"]); names = {x.get("name", "") for x in m.json["nodes"]}
        r["M7"] = dict(movable_nodes=nodes, glb_animations=an, PASS=(an >= 1 if qa["animation"] else an == 0) and all(f"{ID}_{x}" in names for x in nodes))
        TS = T if qa["world_object"] else T[T.mean(1)[:, 2] > -0.185]              # silhouettes are compared ABOVE the shared mount plinth
        M[ID] = masks(TS, env); rep[ID] = r
    for ID in ids:
        best = (0.0, None, None)
        for o in ids:
            if o == ID: continue
            iou = [float((a & b).sum()/max((a | b).sum(), 1)) for a, b in zip(M[ID], M[o])]
            if min(iou) > best[0]: best = (min(iou), o, [round(x, 3) for x in iou])
        rep[ID]["M6"] = dict(closest=best[1], min_view_iou=round(best[0], 3), iou_front_side_top=best[2], fill_front=round(float(M[ID][0].mean()), 3), PASS=best[0] <= 0.80)
    rows = []
    for ID in ids:
        r = rep[ID]; ok = all(r[k]["PASS"] for k in r); ok_all &= ok; r["PASS"] = ok
        rows.append(f"| {ID} | " + " | ".join("PASS" if r[k]["PASS"] else "**FAIL**" for k in ("M1", "M2", "M3", "M4", "M5", "M6", "M7")) + f" | {r['M1']['tris']} / {r['M1']['tris_lod1']} | {len(r['M3']['ports'])} | {r['M6']['closest'][4:]} {r['M6']['min_view_iou']:.2f} |")
    os.makedirs(os.path.join(P.PKG, "tests"), exist_ok=True)
    json.dump(dict(result="PASS" if ok_all else "FAIL", count=len(ids), modules=rep), open(os.path.join(P.PKG, "tests", "module_tests.json"), "w"), indent=1)
    npass = sum(1 for i in ids if rep[i]["PASS"])
    with open(os.path.join(P.PKG, "tests", "TEST_SUMMARY.md"), "w") as f:
        f.write(f"# Testy na wyeksportowanych plikach GLB (numpy, bez Blendera)\n\nWynik: **{npass}/{len(ids)} PASS**. Unity: UNITY_NOT_RUN, Windows player: WINDOWS_PLAYER_NOT_RUN.\n\n")
        f.write(__doc__.split("\n", 1)[1] + "\n\n| model | M1 | M2 | M3 | M4 | M5 | M6 | M7 | tris LOD0 / LOD1 | kontakty | najbliższa sylwetka (min IoU z 3 rzutów) |\n|---|---|---|---|---|---|---|---|---|---|---|\n" + "\n".join(rows) + "\n")
    print(f"{npass}/{len(ids)} PASS"); [print("FAIL", i, {k: v for k, v in rep[i].items() if isinstance(v, dict) and not v["PASS"]}) for i in ids if not rep[i]["PASS"]]
if __name__ == "__main__": main()
