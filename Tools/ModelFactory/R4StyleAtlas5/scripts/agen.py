"""#1239 step 1: asset x style -> mesh_source/<Style>/<Id>.json (geometry LOD0 per node + LOD1, interface, materials, clips, tracks).
usage: python3 agen.py pilot|all|Id,Id [styles]"""
import json, os, sys, numpy as np
import akit as K, astyle as ST, adefs as AD, aanim as AN
OUT = os.path.join(K.PKG, "mesh_source")
def pack(m, style, pivot=(0, 0, 0), env=None):
    """faces get the atlas UV: tri-planar world mapping (one tile = one 0.52 m block face); E: v = height in the envelope (painted light ramp)"""
    V = np.round(m["V"] - np.asarray(pivot, float), 6); faces = []; lo, hi = (env[0][2], env[1][2]) if env else (-0.26, 0.26)
    for f in m["faces"]:
        P = m["V"][f["v"]]; n = np.abs(np.mean(f["n"], axis=0)); ax = int(np.argmax(n)); a, b = [(1, 2), (0, 2), (0, 1)][ax]
        uv = np.stack([P[:, a]/0.52 + 0.5, P[:, b]/0.52 + 0.5], axis=1)
        if ST.UV_MODE[style] == "height": uv = np.stack([(P[:, 0] + P[:, 1])/0.52 + 0.5, np.clip((P[:, 2] - lo)/(hi - lo), 0.02, 0.98)], axis=1)
        faces.append(dict(v=f["v"], n=np.round(np.array(f["n"]), 5).tolist(), uv=np.round(uv, 5).tolist(), zone=ST.ZONE[style][f["group"]]))
    return dict(verts=V.tolist(), faces=faces)
def _merge(ms):
    V = []; F = []; off = 0
    for m in ms: V.append(m["V"]); F += [dict(v=[i + off for i in f["v"]], n=f["n"], uv=f["uv"], group=f["group"]) for f in m["faces"]]; off += len(m["V"])
    return dict(V=np.concatenate(V), faces=F)
def _bl(v): return [float(v[0]), float(-v[2]), float(v[1])]
def gen(cid, style):
    a = AD.ASSETS[cid]; m = K.build(a["fn"], style, False, a["env"], cid, bool(a.get("source_clips"))); l = K.build(a["fn"], style, True, a["env"], cid, bool(a.get("source_clips"))); st = K.stats(m); ls = K.stats(l)
    assert st["ok"] and ls["ok"], (cid, style)
    fam = a["family"]; nodes = {n: dict(pivot=d["pivot"], parent=d.get("parent"), anim=d.get("anim")) for n, d in m["nodes"].items()}; names = a.get("names", {})
    ports = a.get("ports") or m["ports"]; inv = AD.INV.get(cid, {})
    if fam == "connector":
        ports = [dict(id=p["id"], dir=p["dir"], kind=p["kind"], cell=p["cell"], center=_bl(p["position_m"]), normal=_bl(p["normal"]), half_m=float(p["half"][0])*K.G*0.85) for p in inv["ports"]]
    src = dict(id=cid, style=style, style_label=ST.LABEL[style], family=fam, envelope=a["env"], body=a.get("body") or cid + "_Body", node_names={n: names.get(n, f"{cid}_{n}") for n in nodes}, nodes=nodes, fx=a.get("fx", []),
               ports=ports, mount=(ports[0]["dir"] if ports and fam not in ("world_trap",) else None), empties=m["empties"], ref=list(a["ref"]) if a.get("ref") else None, world_object=fam == "world_trap",
               materials={z: dict(rgb=list(ST.MAT[style][z][0]), metallic=ST.MAT[style][z][1], roughness=ST.MAT[style][z][2]) for z in ST.MAT[style]}, glow=a["glow"], glow_strength=ST.GLOW_STRENGTH[style], presets=ST.PRESETS[style],
               interface=a.get("interface"), source=a.get("source"), meshes={n: pack(mm, style, (0, 0, 0) if n == "Body" else m["nodes"][n]["pivot"], a["env"]) for n, mm in m["by_node"].items() if mm is not None}, texture=ST.TEXTURE[style],
               lod1=pack(_merge([mm for n, mm in l["by_node"].items() if mm is not None and n not in a.get("fx", [])]), style, (0, 0, 0), a["env"]), stats=dict(triangles=st["triangles"], shells=st["shells"]), style_fallbacks=m["fallbacks"])
    if a.get("source_clips"):                                   # approved rig: the functional animation is transferred from the source GLB in Blender
        cl = a["source_clips"]; src["animation"] = dict(mode="transfer", clips=cl, frames=max(c["end"] for c in cl), events={c["name"] + "_Start": c["start"] for c in cl if c["name"] != "Idle"},
                                                        warped_clips=[c["name"] for c in cl if c["name"] in AN.WARPABLE and not c.get("loop")] if style != "A" else [])
    else:
        cs = AN.clip_set(fam, bool(nodes)); dd = next((e["dir"] for e in m["empties"] if e["name"] == "Deploy_Out"), (0, 0, 1)); seed = (sum(map(ord, cid)) % 97)/7.0
        reach = float(np.abs(np.array(a["env"])).max()); F, tr = AN.tracks(style, cs, {n: d["anim"] for n, d in nodes.items()}, dd, seed, min(1.0, 0.3/reach))
        tr = {n: dict(loc=np.round(t["loc"], 6).tolist(), quat=np.round(t["quat"], 6).tolist(), scl=np.round(t["scl"], 5).tolist()) for n, t in tr.items()}
        src["animation"] = dict(mode="procedural", set=cs, clips=[dict(name=c[0], start=c[1], end=c[2], loop=c[3]) for c in AN.CLIPS[cs]], frames=F, events=AN.EVENTS[cs], tracks=tr, warped_clips=[])
    d = os.path.join(OUT, ST.LABEL[style]); os.makedirs(d, exist_ok=True); json.dump(src, open(os.path.join(d, cid + ".json"), "w"), separators=(",", ":"))
    return st["triangles"], ls["triangles"]
if __name__ == "__main__":
    arg = sys.argv[1]; ids = AD.PHASE[arg] if arg in AD.PHASE else (list(AD.ASSETS) if arg == "all" else arg.split(",")); styles = list(sys.argv[2]) if len(sys.argv) > 2 else ST.STYLES
    for c in ids:
        row = []
        for s in styles:
            try: row.append(gen(c, s))
            except Exception as ex: row.append("FAIL " + s + " " + repr(ex)[:80])
        print(c, row, flush=True)
