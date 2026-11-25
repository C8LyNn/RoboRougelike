"""RoboRougelike six-style series 1: put a KEPT model into the series folder unchanged (file copies) and check it against the contract.
RUNS INSIDE BLENDER. usage: blender -b --factory-startup --python s6keep.py -- SRC_DIR TEX_DIR|- OUT_DIR CID NOTE"""
import bpy, sys, os, json, shutil, hashlib
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np, s6defs as D
a = sys.argv[sys.argv.index("--") + 1:]; SRC, TEX, OUT, CID, NOTE = a[:5]; C = D.CONTRACT[CID]
os.makedirs(OUT, exist_ok=True); copied = []
for f in sorted(os.listdir(SRC)):
    p = os.path.join(SRC, f)
    if os.path.isfile(p) and f.startswith(CID) and f.split(".")[-1] in ("glb", "fbx", "blend", "json", "txt"):
        d = os.path.join(OUT, f if not f.endswith("_QA.json") else f.replace("_QA.json", "_QA_source.json")); shutil.copyfile(p, d); copied.append(os.path.basename(d))
if TEX != "-" and os.path.isdir(TEX):
    os.makedirs(os.path.join(OUT, "textures"), exist_ok=True)
    for f in os.listdir(TEX):
        if f.startswith(CID): shutil.copyfile(os.path.join(TEX, f), os.path.join(OUT, "textures", f)); copied.append("textures/" + f)
bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene; bpy.ops.import_scene.gltf(filepath=os.path.join(OUT, CID + ".glb"))
objs = {o.name.split(".")[0]: o for o in bpy.data.objects}; req = [CID + "_" + n for n in C["nodes"] if n != "MuzzleFlash"] + list(C["empties"])
miss = [n for n in req if n not in objs]; viol = []
for n, v in C["nodes"].items():
    o = objs.get(CID + "_" + n)
    if o is None or o.type != "MESH" or not v.get("bounds"): continue
    V = np.array([o.matrix_world @ x.co for x in o.data.vertices]); l, h = V.min(0), V.max(0); ex = max(float((np.array(v["bounds"][0]) - l).max()), float((h - np.array(v["bounds"][1])).max()))
    if ex > 0.003: viol.append(dict(node=n, exceed_mm=round(ex*1000, 1)))
fr = [list(ac.frame_range) for ac in bpy.data.actions]; extra = sorted(n for n, o in objs.items() if o.type == "MESH" and n not in [CID + "_" + k for k in C["nodes"]])
T = dict(cid=CID, status="KEPT", note=NOTE, source_dir=SRC, files=copied, required_nodes_missing=miss, node_bounds_violations=viol, extra_mesh_nodes=extra, actions=fr,
         contract_ok=(not miss and not viol), sha={f: hashlib.sha256(open(os.path.join(OUT, f), "rb").read()).hexdigest()[:16] for f in copied if f.endswith((".glb", ".fbx"))}, unity="UNITY_NOT_RUN")
json.dump(T, open(os.path.join(OUT, CID + "_QA.json"), "w"), indent=1); print("[S6K] KEPT", CID, json.dumps({k: T[k] for k in ("required_nodes_missing", "node_bounds_violations", "extra_mesh_nodes", "contract_ok")}), flush=True)
