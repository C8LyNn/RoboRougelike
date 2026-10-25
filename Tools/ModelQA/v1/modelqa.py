#!/usr/bin/env python3
"""Independent asset audit: read-only Git objects, OBJ geometry and posed mesh AABBs.
Never executes a generator, imports into Unity, merges, or marks Blender as PASS.
Works on a local Git clone after fetching pinned PR refs from catalog.json.
Python 3.10+ standard library only. See README and limitations.
"""
from __future__ import annotations
import argparse
import hashlib
import html
import json
import math
import pathlib
import re
import subprocess
import sys
from collections import Counter
from itertools import product

HERE = pathlib.Path(__file__).resolve().parent
PREFIX = "Tools/ModelFactory"

def git(repo, *args):
    p = subprocess.run(["git", "-C", str(repo), *args], capture_output=True)
    if p.returncode:
        raise FileNotFoundError("git " + " ".join(args[:2]) + ": " +
                                p.stderr.decode("utf-8", "replace")[:220])
    return p.stdout

def file_at(repo, ref, name):
    if not re.fullmatch(r"[a-f0-9]{40}", ref):
        raise ValueError("An immutable 40-hex SHA is required")
    if ".." in name or name.startswith("/") or "\x00" in name:
        raise ValueError("Unsafe model path")
    return git(repo, "show", ref + ":" + name)

def files_at(repo, ref, directory):
    raw = git(repo, "ls-tree", "-r", "-z", "--name-only", ref, "--", directory)
    return [x.decode("utf-8") for x in raw.split(b"\0") if x]

def sha256(raw):
    return hashlib.sha256(raw).hexdigest()

def source_status(old, new, output_names, require_generated=True):
    """An unchanged export is stale relative to a different generator,
    unless a rebuild is proven to produce identical canonical bytes.
    This conservative classification is PROVENANCE_STALE, not mesh corruption."""
    changed = old["generator"]["git_blob_sha"] != new["generator"]["git_blob_sha"]
    out = {}
    for name in output_names:
        a,b = old["exports"].get(name), new["exports"].get(name)
        if b is None: out[name] = "MISSING_IN_FIX"
        elif a is None: out[name] = "NEW_EXPORT_PRESENT_NEEDS_GEOMETRIC_QA"
        elif changed and a["git_blob_sha"] == b["git_blob_sha"]:
            out[name] = "UNCHANGED_AFTER_SOURCE_FIX_STALE_RISK"
        elif a["git_blob_sha"] != b["git_blob_sha"]:
            out[name] = "CHANGED_EXPORT_PARITY_NOT_YET_PROVEN"
        else: out[name] = "SAME_GENERATOR_SAME_EXPORT"
    return out

def primitive_vertices(part):
    x,y,z = (float(a) for a in part["center"])
    if part["shape"] == "box":
        size = [float(a)/2 for a in part["size"]]
        return [(x+a,y+b,z+c) for a,b,c in product(*[(-s,s) for s in size])]
    if part["shape"] == "cylinder":
        n=int(part["segments"]); r=float(part["radius"])
        half=float(part["depth"])/2
        if n < 3 or not math.isfinite(r) or r <= 0 or half <= 0:
            raise ValueError("Invalid cylinder primitive")
        res=[]
        for side in (-1,1):
            for i in range(n):
                theta=i*2*math.pi/n
                a,b = r*math.cos(theta),r*math.sin(theta)
                loc = {"X":(side*half,a,b),"Y":(b,side*half,a),
                       "Z":(a,b,side*half)}[part["axis"]]
                res.append((loc[0]+x,loc[1]+y,loc[2]+z))
        return res
    raise ValueError("Unsupported primitive: " + str(part["shape"]))

def bounds(points, tilt, pivot, angle):
    c,s=math.cos(math.radians(angle)),math.sin(math.radians(angle))
    lower=[float("inf")]*3; upper=[-float("inf")]*3
    for x,y,z in points:
        if tilt: x,y,z = x+pivot[0], y*c-z*s+pivot[1], y*s+z*c+pivot[2]
        for i,v in enumerate((x,y,z)):
            lower[i]=min(lower[i],v); upper[i]=max(upper[i],v)
    return lower,upper

def overlap_volume(a,b):
    return math.prod(max(0.0,min(a[1][k],b[1][k])-
                         max(a[0][k],b[0][k])) for k in range(3))

def sweep_json(spec, step=0.5):
    """Conservative broadphase: 0 positive AABBs is safe AT sampled poses.
    A positive AABB alone is not a triangle-triangle collision proof.
    Blender depsgraph, arbitrary interpolation and between-frame guarantees NOT_RUN.
    """
    if not 0 < step <= 5: raise ValueError("Invalid sample step")
    pivot=spec["hinge_pivot"]
    base=[p for p in spec["parts"] if p["parent"]=="base"]
    moving=[p for p in spec["parts"] if p["parent"]=="tilt"]
    static=[(p["name"],bounds(primitive_vertices(p),False,pivot,0)) for p in base]
    moving_verts=[(p["name"],primitive_vertices(p)) for p in moving]
    lo,hi = spec["tilt_degrees"]
    count=int(round((hi-lo)/step))
    if abs(lo+count*step-hi)>1e-8:raise ValueError("step must divide tilt range")
    collisions=Counter(); first=[]; checked=0; positive=0
    for i in range(count+1):
        angle=lo+step*i
        moved=[(name,bounds(verts,True,pivot,angle)) for name,verts in moving_verts]
        for bn,bb in static:
            for mn,mb in moved:
                checked+=1
                vol=overlap_volume(bb,mb)
                if vol>1e-10:
                    positive+=1
                    collisions[bn+" / "+mn]+=1
                    if len(first)<12: first.append({"static":bn,"moving":mn,
                                                    "angle_deg":angle,
                                                    "aabb_overlap_m3":round(vol,12)})
    return {"sample_step_degrees":step,"sample_count":count+1,
            "source_primitive_parts":len(spec["parts"]),
            "static_parts":len(base),"animated_parts":len(moving),
            "pairs_checked":checked,"positive_aabb_pairs":positive,
            "distinct_overlap_pairs":len(collisions),
            "top_pairs":collisions.most_common(12),"examples":first,
            "status":"BROADPHASE_CLEAR_SAMPLED" if positive==0 else
                     "BROADPHASE_OVERLAP_NARROWPHASE_REQUIRED",
            "narrowphase":"NOT_RUN","blender_depsgraph":"NOT_RUN",
            "continuity_between_samples":"NOT_PROVEN"}

def inspect_obj(raw):
    """Checks actual text OBJ bytes, face indices, finite vertices and area.
    Does not infer physical welds from disconnected mesh islands."""
    names=[]; vertices=[]; triangles=0; faces=0; bad=0; materials=set()
    current="UNNAMED"; objs={}
    def tri_area(a,b,c):
        u=[b[i]-a[i] for i in range(3)]
        v=[c[i]-a[i] for i in range(3)]
        cross=[u[1]*v[2]-u[2]*v[1],
               u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]
        return math.sqrt(sum(x*x for x in cross))/2
    for line in raw.decode("utf-8").splitlines():
        if line.startswith("o "):
            current=line[2:].strip();names.append(current);objs.setdefault(current,0)
        elif line.startswith("usemtl "):materials.add(line.split(None,1)[1])
        elif line.startswith("v "):
            values=tuple(float(v) for v in line.split()[1:4])
            if len(values)!=3 or not all(math.isfinite(v) for v in values):
                raise ValueError("Nonfinite or invalid OBJ vertex")
            vertices.append(values);objs[current]=objs.get(current,0)+1
        elif line.startswith("f "):
            ids=[int(a.split("/")[0]) for a in line.split()[1:]]
            if len(ids)<3:bad+=1;continue
            for idx in ids:
                if idx == 0 or abs(idx)>len(vertices): raise ValueError("Invalid OBJ face index")
            points=[vertices[n-1] if n>0 else vertices[n] for n in ids]
            faces+=1
            for i in range(1,len(points)-1):
                triangles+=1
                if len({ids[0],ids[i],ids[i+1]})<3 or tri_area(points[0],points[i],points[i+1])<1e-12:bad+=1
    return {"sha256":sha256(raw),"objects":len(objs),"vertices":len(vertices),
            "faces":faces,"triangles":triangles,"degenerate_triangles":bad,
            "materials":len(materials),"named_components":len(set(names)),
            "status":"STATIC_MESH_BASIC_PASS" if vertices and faces and bad==0 else
                     "STATIC_MESH_BASIC_FAIL"}

def git_meta(repo, commit, directory, gen_name, extensions):
    filelist=files_at(repo,commit,directory)
    gen=directory+"/"+gen_name
    def obj(p):
        return {"git_blob_sha":git(repo,"rev-parse",commit+":"+p).decode().strip(),
                "bytes":len(file_at(repo,commit,p)),
                "sha256":sha256(file_at(repo,commit,p))}
    return {"ref":commit,
            "generator":obj(gen) if gen in filelist else None,
            "exports":{p.removeprefix(directory+"/"):obj(p) for p in filelist
                       if pathlib.PurePosixPath(p).suffix.lower() in extensions},
            "available_paths":[p.removeprefix(directory+"/") for p in filelist]}

def check_source_flags(repo, original, fixed, source_path, originals, repairs):
    a=file_at(repo,original,source_path).decode("utf-8")
    b=file_at(repo,fixed,source_path).decode("utf-8")
    return {"original_expected_patterns":{p:p in a for p in originals},
            "fixed_expected_patterns":{p:p in b for p in repairs},
            "old_sha256":sha256(a.encode()),"new_sha256":sha256(b.encode()),
            "generator_changed":a!=b}

def model_compare(repo, model_id, orig, fix):
    model=model_id.lower().replace("-","_")
    prefix=PREFIX+"/"+model_id[:3]+"/"+model_id
    generator="generate_"+model+".py"
    old=git_meta(repo,orig,prefix,generator,{".obj",".glb",".fbx",".blend",".mtl"})
    new=git_meta(repo,fix,prefix,generator,{".obj",".glb",".fbx",".blend",".mtl"})
    result={"model":model_id,"original_sha":orig,"fix_sha":fix,
            "source_original":old,"source_fixed":new,
            "export_status":source_status(old,new,[]),
            "blender":"NOT_RUN","unity":"NOT_RUN",
            "real_binary_narrowphase":"NOT_RUN"}
    if model_id=="M02-001":
        names=["generated/M02-001_segmented_offroad_wheel."+e for e in ("obj","mtl","glb")]
        result["export_status"]=source_status(old,new,names)
        result["source_structure"]=check_source_flags(repo,orig,fix,prefix+"/"+generator,
              ['cylinder_x("Axle_Hub"', 'cylinder_x(f"Hub_Cap_'],
              ['ring("Axle_Hub"', 'ring(f"Hub_Cap_', 'REFERENCE_SHAFT_RADIUS'])
        result["status"]="FAIL_STALE_COMMITTED_EXPORTS" if any(
          v=="UNCHANGED_AFTER_SOURCE_FIX_STALE_RISK" for v in result["export_status"].values()) else "RECHECK_BINARY"
    elif model_id=="M05-002":
        geometry=prefix+"/geometry.json"
        a,b=(json.loads(file_at(repo,ref,geometry)) for ref in (orig,fix))
        result["geometry_sha256"]={"old":sha256(file_at(repo,orig,geometry)),
                                    "new":sha256(file_at(repo,fix,geometry))}
        result["sampled_aabb"]={"before":sweep_json(a), "after":sweep_json(b)}
        # independent OBJ parser checks actual committed mesh files; not animated GLB.
        result["static_obj"]={"before":inspect_obj(file_at(repo,orig,prefix+"/M05-002.obj")),
                              "after":inspect_obj(file_at(repo,fix,prefix+"/M05-002.obj"))}
        result["source_structure"]=check_source_flags(repo,orig,fix,prefix+"/"+generator,
              ["bpy.ops.object.delete(use_global=False)"],["swept_clearance", "bpy.data.libraries.write"])
        good=(result["sampled_aabb"]["before"]["positive_aabb_pairs"]>0 and
              result["sampled_aabb"]["after"]["positive_aabb_pairs"]==0 and
              result["static_obj"]["after"]["degenerate_triangles"]==0)
        result["status"]="SAMPLED_REPAIR_CLEAR_BLENDER_PENDING" if good else "RECHECK_GEOMETRY"
    elif model_id=="M01-003":
        result["source_structure"]=check_source_flags(repo,orig,fix,prefix+"/"+generator,
              ['Shield_Guide_',],['Shield_Slider_Shoe_', 'GUIDE_SPAN_X', 'SHOE_HEIGHT'])
        result["status"]="SOURCE_REPAIR_EXPORT_NOT_COMMITTED" if not any(
           f.endswith((".obj",".glb",".fbx",".blend")) for f in new["available_paths"]) else "RECHECK_MESH"
    return result

def catalog_inventory(catalog):
    models=catalog["models"]
    ids=[m["id"] for m in models]
    assert len(ids)==70 and len(set(ids))==70
    expect={f"M{k:02d}-{j:03d}" for k in range(1,6) for j in range(1,15)}
    assert set(ids)==expect
    return {"models_declared":70,"series_counts":dict(Counter(s[:3] for s in ids)),
            "with_fix_pr":sum(bool(m.get("fix_pr")) for m in models),
            "status":"CATALOG_METADATA_CONSISTENT_NOT_70_MESHES_VERIFIED"}

def make_html(result):
    style="body{font-family:system-ui;max-width:1100px;margin:auto;padding:24px}th,td{padding:9px;border-bottom:1px solid #bbb;text-align:left} .fail{font-weight:700;color:#ba2828}pre{white-space:pre-wrap}"
    lines=[f"<html><meta charset='utf-8'><title>MODEL-QA-AUTO-001</title><style>{style}</style>",
           "<h1>MODEL-QA-AUTO-001 / independent evidence</h1>",
           "<p>READ ONLY. Geometry and provenance are distinct from Blender, visual or Unity PASS.</p>",
           "<table><tr><th>Model</th><th>Old</th><th>Fix</th><th>Verdict</th></tr>"]
    for m in result["pilot"]:
        lines.append("<tr><td>"+html.escape(m["model"])+
                     "</td><td><code>"+m["original_sha"][:12]+
                     "</code></td><td><code>"+m["fix_sha"][:12]+
                     "</code></td><td>"+html.escape(m["status"])+"</td></tr>")
    lines.append("</table><h2>Evidence JSON</h2><pre>"+
                 html.escape(json.dumps(result,ensure_ascii=False,indent=2))+"</pre></html>")
    return "\n".join(lines)+"\n"

def main(argv=None):
    ap=argparse.ArgumentParser()
    ap.add_argument("--repo",default=".")
    ap.add_argument("--catalog",default=str(HERE/"catalog.json"))
    ap.add_argument("--output",default="modelqa_report")
    args=ap.parse_args(argv)
    repo=pathlib.Path(args.repo)
    catalog=json.loads(pathlib.Path(args.catalog).read_text(encoding="utf-8"))
    overview=catalog_inventory(catalog)
    pilot=[model_compare(repo,m["id"],m["original_sha"],m["fix_sha"])
           for m in catalog["models"] if m["id"] in ("M02-001","M05-002","M01-003")]
    out={"version":"MODEL-QA-AUTO-001-R1","issue":160,"source":"actual immutable PR Git blobs",
         "inventory":overview,"pilot":pilot,
         "blender_binary_export":"NOT_RUN", "unity_import":"NOT_RUN",
         "human_visual_review":"NOT_RUN",
         "qa_ranking":sorted([{"id":m["model"],"status":m["status"],
                               "risk_points":{"M02-001":100,"M05-002":60,"M01-003":70}[m["model"]]}
                              for m in pilot],key=lambda x:-x["risk_points"])}
    path=pathlib.Path(args.output);path.mkdir(parents=True,exist_ok=True)
    (path/"results.json").write_text(json.dumps(out,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    (path/"report.html").write_text(make_html(out),encoding="utf-8")
    print(json.dumps({"catalog":overview,"pilot":[{"id":m["model"],"status":m["status"],
                         "aabb_before":m.get("sampled_aabb",{}).get("before",{}).get("positive_aabb_pairs"),
                         "aabb_after":m.get("sampled_aabb",{}).get("after",{}).get("positive_aabb_pairs")}
                         for m in pilot],"report":str(path)},indent=2))
    return 0

if __name__=="__main__": sys.exit(main())
