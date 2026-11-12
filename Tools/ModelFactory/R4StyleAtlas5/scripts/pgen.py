"""#1239 pilot 2, step 1: type x style -> pilot2/mesh_source/<Style>/<Id>.json. Same file format as agen.py (the atlas builder contract), but
every style is built from ITS OWN construction (pdefs.P[type][style]) and carries the pilot material / texture set (pstyle).
usage: python3 pgen.py [Type,..] [styles]"""
import json, os, sys, numpy as np
import akit as K, astyle as ST, adefs as AD, aanim as AN, pdefs as PD, pstyle as PS
ROOT = os.path.join(K.PKG, "pilot2"); OUT = os.path.join(ROOT, "mesh_source")
def pack(m, style, pivot, env):
    V = np.round(m["V"] - np.asarray(pivot, float), 6); faces = []; lo, hi = env[0][2], env[1][2]; mode = PS.TEXTURES[style]["uv"]
    for f in m["faces"]:
        P = m["V"][f["v"]]; n = np.abs(np.mean(f["n"], axis=0)); ax = int(np.argmax(n)); a, b = [(1, 2), (0, 2), (0, 1)][ax]
        uv = np.stack([P[:, a]/0.52 + 0.5, P[:, b]/0.52 + 0.5], axis=1) if mode == "triplanar" else np.stack([(P[:, 0] + P[:, 1])/0.52 + 0.5, np.clip((P[:, 2] - lo)/(hi - lo), 0.02, 0.98)], axis=1)
        faces.append(dict(v=f["v"], n=np.round(np.array(f["n"]), 5).tolist(), uv=np.round(uv, 5).tolist(), zone=PS.ZONE[style][f["group"]]))
    return dict(verts=V.tolist(), faces=faces)
def _merge(ms):
    V = []; F = []; off = 0
    for m in ms: V.append(m["V"]); F += [dict(v=[i + off for i in f["v"]], n=f["n"], group=f["group"]) for f in m["faces"]]; off += len(m["V"])
    return dict(V=np.concatenate(V), faces=F)
def board_frames(clips):
    """frames shown on the animation boards: two moments of every non-idle clip, first four"""
    cl = [c for c in clips if c["name"] != "Idle"]; out = [(c["name"], int(round((c["start"] + c["end"])/2))) for c in cl][:4]
    for c in cl:                                                  # fewer than four clips: a second and third moment of each
        for u in (0.25, 0.8):
            f = int(round(c["start"] + u*(c["end"] - c["start"])))
            if len(out) < 4 and f not in [x[1] for x in out]: out.append((c["name"], f))
    return [dict(clip=n, frame=f) for n, f in sorted(out, key=lambda x: x[1])]
def gen(typ, style):
    a = PD.TYPES[typ]; cid = a["cid"]; fn = PD.P[typ][style]; rig = bool(a.get("source_clips")); kw = (style, )
    m = K.build(fn, style, False, a["env"], cid, rig, a.get("planes"), True); l = K.build(fn, style, True, a["env"], cid, rig, a.get("planes"), True); st = K.stats(m); ls = K.stats(l)
    assert st["ok"] and ls["ok"], (cid, style)
    fam = a["family"]; nodes = {n: dict(pivot=d["pivot"], parent=d.get("parent"), anim=d.get("anim")) for n, d in m["nodes"].items()}; names = a.get("names", {}); ports = a.get("ports") or m["ports"]; inv = AD.INV.get(cid, {})
    if fam == "connector":
        ports = [dict(id=p["id"], dir=p["dir"], kind=p["kind"], cell=p["cell"], center_bl=agen_bl(p["position_m"]), normal_bl=agen_bl(p["normal"]), half_m=float(p["half"][0])*K.G*0.85) for p in inv["ports"]]
    fx = a.get("fx", [])
    src = dict(id=cid, type=typ, style=style, style_label=ST.LABEL[style], family=fam, envelope=a["env"], body=a.get("body") or cid + "_Body", node_names={n: names.get(n, f"{cid}_{n}") for n in nodes}, nodes=nodes, fx=fx,
               ports=ports, mount=(ports[0]["dir"] if ports and fam not in ("world_trap",) else None), empties=m["empties"], ref=list(a["ref"]) if a.get("ref") else None, world_object=fam == "world_trap",
               zone_order=PS.ZONE_ORDER, materials={z: dict(rgb=list(v[0]), metallic=v[1], roughness=v[2]) for z, v in PS.MAT[style].items()}, glow=a["glow"], glow_strength=PS.GLOW_STRENGTH[style], textures=PS.TEXTURES[style],
               character=PS.CHARACTER[style], construction=PD.CONCEPT[typ][style], interface=a.get("interface"), source=a.get("source"), node_bounds=a.get("nb"),
               meshes={n: pack(mm, style, (0, 0, 0) if n == "Body" else m["nodes"][n]["pivot"], a["env"]) for n, mm in m["by_node"].items() if mm is not None},
               lod1=pack(_merge([mm for n, mm in l["by_node"].items() if mm is not None and n not in fx]), style, (0, 0, 0), a["env"]), stats=dict(triangles=st["triangles"], shells=st["shells"], parts=len(m["part_stats"])), style_fallbacks=m["fallbacks"])
    if rig:
        cl = a["source_clips"]; src["animation"] = dict(mode="transfer", clips=cl, frames=max(c["end"] for c in cl), events={c["name"] + "_Start": c["start"] for c in cl if c["name"] != "Idle"},
                                                        warped_clips=[c["name"] for c in cl if c["name"] in AN.WARPABLE and not c.get("loop")] if style != "A" else [])
    else:
        cs = AN.clip_set(fam, bool(nodes)); dd = next((e["dir"] for e in m["empties"] if e["name"] == "Deploy_Out"), (0, 0, 1)); seed = (sum(map(ord, cid)) % 97)/7.0
        reach = float(np.abs(np.array(a["env"])).max()); F, tr = AN.tracks(style, cs, {n: d["anim"] for n, d in nodes.items()}, dd, seed, min(1.0, 0.2/reach))
        tr = {n: dict(loc=np.round(t["loc"], 6).tolist(), quat=np.round(t["quat"], 6).tolist(), scl=np.round(t["scl"], 5).tolist()) for n, t in tr.items()}
        src["animation"] = dict(mode="procedural", set=cs, clips=[dict(name=c[0], start=c[1], end=c[2], loop=c[3]) for c in AN.CLIPS[cs]], frames=F, events=AN.EVENTS[cs], tracks=tr, warped_clips=[])
    src["board_frames"] = board_frames(src["animation"]["clips"])
    d = os.path.join(OUT, ST.LABEL[style]); os.makedirs(d, exist_ok=True); json.dump(src, open(os.path.join(d, cid + ".json"), "w"), separators=(",", ":"))
    return st["triangles"], ls["triangles"], len(m["part_stats"])
def agen_bl(v): return [float(v[0]), float(-v[2]), float(v[1])]
if __name__ == "__main__":
    types = sys.argv[1].split(",") if len(sys.argv) > 1 and sys.argv[1] != "*" else PD.ORDER; styles = list(sys.argv[2]) if len(sys.argv) > 2 else ST.STYLES
    for t in types:
        row = []
        for s in styles:
            try: row.append(gen(t, s))
            except Exception as ex: row.append("FAIL " + s + " " + repr(ex)[:120])
        print(t, PD.CID[t], row, flush=True)
