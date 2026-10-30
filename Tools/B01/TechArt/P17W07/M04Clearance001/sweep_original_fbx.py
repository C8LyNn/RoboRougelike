#!/usr/bin/env python3
"""B01-30 M04-TriangleClearance-001 / real original FBX inter-nozzle triangle QA.

This never edits original M04 producer, Codex gameplay, FBX binary or Unity Assets.
Unlike prior M04's AABB-only checks it tests triangle-on-triangle overlap for EACH
of the six distinct pairs of moving nozzle assemblies over all 121 keyed frames.
BVH contact does not establish physical collider/mount/URP validity or continuous
between-frame collision freedom.
"""
from __future__ import annotations
import bpy
import csv
import hashlib
import json
import math
import struct
import sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ORIGINAL_FBX_SHA="a017534047b9db6a1001b32b186809b796d4e6562f1c9ea560042de97cf58cd3"
ORIGINAL_FBX_BYTES=2109580
ORIGINAL_SOURCE_HEAD="82ea17a6f964aa0cf08890c827b038ad9a0fc57f"
PIVOTS=tuple("N%02d_GIMBAL_PIVOT"%i for i in range(1,5))
FRAMES=range(1,122)
PAIRS=tuple((i,j) for i in range(4) for j in range(i+1,4))

def cli(name):
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    if name not in argv or argv.index(name)+1>=len(argv):
        raise ValueError("MISSING_ARG:"+name)
    return Path(argv[argv.index(name)+1]).resolve()

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()

def verify_binary(path,source_receipt):
    data=path.read_bytes()
    if (len(data)!=ORIGINAL_FBX_BYTES or sha(path)!=ORIGINAL_FBX_SHA or
        not data.startswith(b"Kaydara FBX Binary  \x00\x1a\x00")):
        raise ValueError("ORIGINAL_FBX_BINARY_DRIFT")
    receipt=json.loads(source_receipt.read_text())
    if (receipt.get("fbx_sha256")!=ORIGINAL_FBX_SHA or
        receipt.get("fbx_bytes")!=ORIGINAL_FBX_BYTES or
        receipt.get("source_original_owner_commit")!="eef02864b106546b200011a2f0286fe7d9a83a1f" or
        receipt.get("original_mesh_count")!=105):
        raise ValueError("ORIGINAL_FBX_RECEIPT_DRIFT")
    return receipt

def containing_pivot(obj):
    cursor=obj.parent
    seen=set()
    while cursor:
        if cursor.name in PIVOTS:return cursor.name
        if cursor.name in seen:raise ValueError("OBJECT_PARENT_CYCLE:"+obj.name)
        seen.add(cursor.name)
        cursor=cursor.parent
    return None

def groups_and_pivots(expected_meshes):
    meshes=[o for o in bpy.data.objects if o.type=="MESH"]
    if len(meshes)!=expected_meshes:raise ValueError("REIMPORTED_MESH_COUNT_NOT_105:"+str(len(meshes)))
    pivots=[]
    for name in PIVOTS:
        obj=bpy.data.objects.get(name)
        if obj is None or obj.type!="EMPTY" or not obj.animation_data or not obj.animation_data.action:
            raise ValueError("MISSING_REAL_FBX_PIVOT_OR_ACTION:"+name)
        pivots.append(obj)
    groups={name:[] for name in PIVOTS}
    for obj in meshes:
        owner=containing_pivot(obj)
        if owner:groups[owner].append(obj)
    for name,objects in groups.items():
        if len(objects)<16:raise ValueError("INCOMPLETE_MOVING_NOZZLE:"+name+":"+str(len(objects)))
    return meshes,pivots,groups

def verify_motion(pivots):
    scene=bpy.context.scene
    q=[]
    for frame in (1,31,91):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        q.append([p.matrix_world.to_quaternion().copy() for p in pivots])
    motion=[]
    for i,p in enumerate(pivots):
        forward=math.degrees(q[0][i].rotation_difference(q[1][i]).angle)
        backward=math.degrees(q[0][i].rotation_difference(q[2][i]).angle)
        if not (7.0<forward<15.0 and 7.0<backward<15.0):
            raise ValueError("STATIC_OR_INVALID_FBX_PIVOT:"+p.name+":%.3f/%.3f"%(forward,backward))
        motion.append({"pivot":p.name,"forward_deg":round(forward,4),"reverse_deg":round(backward,4)})
    return motion

def build_world_bvh(objects):
    vertices=[]
    tris=[]
    for ob in objects:
        mesh=ob.data
        if len(mesh.vertices)<3 or len(mesh.polygons)<1:raise ValueError("INVALID_IMPORTED_MESH:"+ob.name)
        mesh.calc_loop_triangles()
        matrix=ob.matrix_world
        start=len(vertices)
        vertices.extend([matrix @ v.co for v in mesh.vertices])
        tris.extend([tuple(start+v for v in t.vertices) for t in mesh.loop_triangles])
    if len(tris)<250:raise ValueError("MESH_TRIANGLE_LOSS")
    bbox=(
        tuple(min(float(v[k]) for v in vertices) for k in range(3)),
        tuple(max(float(v[k]) for v in vertices) for k in range(3)))
    return BVHTree.FromPolygons(vertices,tris,all_triangles=True),bbox,len(tris)

def aabb_lower_gap(first,second):
    lo_a,hi_a=first
    lo_b,hi_b=second
    d=[max(0.0,lo_a[k]-hi_b[k],lo_b[k]-hi_a[k]) for k in range(3)]
    return math.sqrt(sum(x*x for x in d))

def compare_pair(first,second):
    bvh_a,bounds_a,ta=first
    bvh_b,bounds_b,tb=second
    # broad phase only permits avoiding a narrow-phase operation when boxes are
    # strictly separated. For every potentially contacting pair use ACTUAL BVH.
    gap=aabb_lower_gap(bounds_a,bounds_b)
    overlapping=[] if gap>1e-8 else bvh_a.overlap(bvh_b)
    return len(overlapping),gap

def prove_negative_collision(pivots,groups):
    """Move one *actual FBX nozzle* onto another temporarily, then detect mesh overlap."""
    scene=bpy.context.scene
    scene.frame_set(1)
    bpy.context.view_layer.update()
    left=pivots[0]
    right=pivots[1]
    first=build_world_bvh(groups[left.name])
    second=build_world_bvh(groups[right.name])
    before_hits,_=compare_pair(first,second)
    if before_hits:raise ValueError("ORIGINAL_AT_REST_ALREADY_INTERSECTS")
    saved=left.location.copy()
    try:
        # In the actual imported FBX both assemblies have the same rest-pose
        # geometry. The extra 0.02m overlap avoids coplanar-only collision.
        target=right.matrix_world.translation.copy()
        source=left.matrix_world.translation.copy()
        delta=target-source
        parent=left.parent
        if parent:
            delta=parent.matrix_world.inverted().to_3x3() @ delta
        left.location=saved+delta+Vector((0.0,0.0,-0.02))
        bpy.context.view_layer.update()
        shifted=build_world_bvh(groups[left.name])
        hits,_=compare_pair(shifted,second)
        if hits<1:raise ValueError("FALSE_GREEN_INTERSECTING_ACTUAL_NOZZLE")
        return {"test":"real_imported_nozzle_moved_into_other","before_overlap_pairs":before_hits,
            "mutant_overlap_pairs":hits,"verdict":"EXPECTED_REJECTION_PASS"}
    finally:
        left.location=saved
        scene.frame_set(1)
        bpy.context.view_layer.update()

def verify_negative_binary(original,source_receipt,out):
    corrupt=out/"corrupted-fbx-negative.tmp"
    raw=bytearray(original.read_bytes())
    raw[-31]^=0x33
    corrupt.write_bytes(raw)
    try:
        try:
            verify_binary(corrupt,source_receipt)
        except ValueError as e:
            if "ORIGINAL_FBX_BINARY_DRIFT" not in str(e):raise
            return {"test":"corrupted_original_FBX_byte",
                    "verdict":"EXPECTED_REJECTION_PASS"}
        raise ValueError("FALSE_GREEN_CORRUPTED_FBX")
    finally:
        corrupt.unlink(missing_ok=True)

def snapshot_images(out,source_meshes):
    scene=bpy.context.scene
    scene.render.engine="BLENDER_WORKBENCH"
    scene.render.resolution_x=760
    scene.render.resolution_y=760
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format="PNG"
    scene.display.shading.color_type="MATERIAL"
    scene.display.shading.show_cavity=True
    scene.display.shading.light="STUDIO"
    camera_data=bpy.data.cameras.new("B01_30_CLEARANCE_QA_CAMERA")
    cam=bpy.data.objects.new("B01_30_CLEARANCE_QA_CAMERA",camera_data)
    scene.collection.objects.link(cam)
    scene.camera=cam
    cam.data.type="ORTHO"
    cam.data.ortho_scale=2.9
    results=[]
    for frame in (1,31,91):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        cam.location=(2.7,-3.2,-3.0)
        cam.rotation_euler=(Vector((0,0,-.5))-cam.location).to_track_quat("-Z","Y").to_euler()
        name="M04-CLEARANCE-REAL-FBX-FRAME%03d.png"%frame
        file=out/name
        scene.render.filepath=str(file)
        bpy.ops.render.render(write_still=True)
        data=file.read_bytes()
        if (data[:8]!=b"\x89PNG\r\n\x1a\n" or len(data)<9000 or
            tuple(struct.unpack(">II",data[16:24]))!=(760,760)):
            raise ValueError("REAL_IMPORTED_FBX_RENDER_NOT_VALID:"+name)
        results.append({"name":name,"frame":frame,"bytes":len(data),"sha256":sha(file)})
    return results

def main():
    fbx=cli("--fbx")
    receipt_path=cli("--receipt")
    out=cli("--out")
    out.mkdir(parents=True,exist_ok=True)
    receipt=verify_binary(fbx,receipt_path)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(fbx),use_anim=True)
    scene=bpy.context.scene
    scene.render.fps=30
    scene.frame_start=1
    scene.frame_end=121
    meshes,pivots,groups=groups_and_pivots(receipt["original_mesh_count"])
    motion=verify_motion(pivots)
    mut1=verify_negative_binary(fbx,receipt_path,out)
    mut2=prove_negative_collision(pivots,groups)
    rows=[]
    minima={(a,b):1e8 for a,b in PAIRS}
    counters={(a,b):0 for a,b in PAIRS}
    triangle_counts={}
    for frame in FRAMES:
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        bvh=[build_world_bvh(groups[p.name]) for p in pivots]
        if frame==1:
            triangle_counts={PIVOTS[i]:bvh[i][2] for i in range(4)}
        for i,j in PAIRS:
            hits,gap=compare_pair(bvh[i],bvh[j])
            if hits:
                raise ValueError("REAL_IMPORTED_NOZZLES_INTERSECT:frame%d:%s/%s:%d_pairs"%(
                    frame,PIVOTS[i],PIVOTS[j],hits))
            minima[(i,j)]=min(minima[(i,j)],gap)
            counters[(i,j)]+=1
            rows.append({"frame":frame,"nozzle_a":PIVOTS[i],"nozzle_b":PIVOTS[j],
                "real_triangle_intersection_count":hits,"conservative_AABB_gap_m":"%.6f"%gap})
    if len(rows)!=726 or len(minima)!=6 or not all(v==121 for v in counters.values()):
        raise ValueError("INCOMPLETE_121_FRAME_SWEEP")
    # Original machine-written CSV and report are the main actionable DATA asset.
    csvpath=out/"M04-original-121-frame-six-pair-triangle-sweep.csv"
    with csvpath.open("w",newline="") as f:
        writer=csv.DictWriter(f,fieldnames=list(rows[0]))
        writer.writeheader();writer.writerows(rows)
    previews=snapshot_images(out,meshes)
    report={
        "package_id":"B01-30-M04-TRIANGLE-CLEARANCE-001/R1",
        "role":"B01-30-TECHART",
        "upstream_pr":386,"upstream_immutable_head":ORIGINAL_SOURCE_HEAD,
        "fbx_binary_sha256":ORIGINAL_FBX_SHA,"fbx_binary_bytes":ORIGINAL_FBX_BYTES,
        "fbx_imported_meshes":len(meshes),
        "nozzle_triangle_counts_at_frame1":triangle_counts,
        "pivots":motion,"sampled_frames":121,
        "nozzle_pair_count":6,"narrow_phase_max_opportunity_checks":726,
        "actual_triangle_pair_intersections":0,
        "minimum_conservative_AABB_gap_m_by_pair":{
            PIVOTS[i]+"__"+PIVOTS[j]:round(minima[i,j],6) for i,j in PAIRS
        },
        "negative_real_moved_nozzle":mut2,
        "negative_corrupt_fbx_binary":mut1,
        "real_blender_rendered_FBX_previews":previews,
        "csv_name":csvpath.name,"csv_bytes":csvpath.stat().st_size,
        "csv_sha256":sha(csvpath),
        "status":"BLENDER_ACTUAL_IMPORTED_MESH_TRIANGLE_SWEEP_G0_OWNER_PASS",
        "limitations":[
            "Only distinct 4 moving nozzle assemblies (6 inter-nozzle pairs); fixed housing/bearing intra-group contacts excluded.",
            "BVH triangle overlap evaluated at EVERY INTEGER FRAME (121), not mathematically continuous sub-frame motion.",
            "Conservative AABB distance lower bound is NOT the exact minimum triangle distance.",
            "Blender FBX reimport proves no inter-nozzle triangle intersections in sampled poses, NOT native Unity Animator, mount socket collider, URP, PhysX or gameplay.",
            "Additional independent B01-40/60 QA is pending and this evidence does not replace it."
        ],
        "native_unity":"NOT_RUN","codex_integration":"NOT_SENT"
    }
    (out/"M04-real-triangle-sweep-qa.json").write_text(json.dumps(report,indent=2)+"\n")
    print("B01_30_M04_121_FRAME_726_REAL_TRIANGLE_NOZZLE_PAIR_CHECKS_PASS",
          json.dumps({"source_sha":ORIGINAL_FBX_SHA,"mesh_nodes":len(meshes),
                      "sampled_frames":121,"pairs":726,"intersections":0,
                      "min_gap_m":min(minima.values()),"negative_real_collision_hits":mut2["mutant_overlap_pairs"],
                      "previews":len(previews)},sort_keys=True))
if __name__=="__main__":main()
