#!/usr/bin/env python3
"""M04-007 — four-emitter hover motor, underside frame mount, visual-only.

Python 3.10+ (no third-party dependencies):
  python3 generate_m04_007.py --self-test --output-dir /tmp/m04-007
Blender 3.4+ (bpy):
  blender -b --factory-startup --python generate_m04_007.py -- --output-dir /tmp/m04-007 --require-blender

1 unit = 1 metre. Blender Z up; mounting face is Z=0, underside is -Z.
Four separate emission disks pulse in XY scale and Z translation with 90-degree
phase offsets. No thrust mechanics, physics changes, game scene edits or Unity
dependencies are introduced. Generated OBJ is the neutral geometry, while
the animated GLB includes the independently phased four pulse roots.
"""
from __future__ import annotations

import argparse
from collections import Counter
import json
import math
from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
ID = "M04-007"
COLLECTION = "M04-007_ASSET"


def spec_read():
    spec = json.loads((HERE / "geometry.json").read_text(encoding="utf-8"))
    assert spec["id"] == ID
    assert spec["mount"] == "underside_frame"
    assert spec["mount_origin_m"] == [0, 0, 0]
    assert len(spec["emitter_centers_xy_m"]) == 4
    assert len(spec["materials"]) >= 3
    return spec


def signed_volume(vertices, faces):
    volume = 0.0
    for face in faces:
        a = vertices[face[0]]
        for k in range(1, len(face)-1):
            b, c = vertices[face[k]], vertices[face[k+1]]
            volume += (a[0]*(b[1]*c[2]-b[2]*c[1]) +
                       a[1]*(b[2]*c[0]-b[0]*c[2]) +
                       a[2]*(b[0]*c[1]-b[1]*c[0]))/6.0
    return volume


def outward(verts, faces):
    volume = signed_volume(verts, faces)
    assert abs(volume) > 1e-11, "Zero-volume mesh"
    return verts, ([tuple(reversed(f)) for f in faces] if volume < 0 else faces)


def box(center, size, angle=0.0):
    sx, sy, sz = (v/2 for v in size)
    vertices = [(-sx,-sy,-sz),(sx,-sy,-sz),(sx,sy,-sz),(-sx,sy,-sz),
                (-sx,-sy,sz),(sx,-sy,sz),(sx,sy,sz),(-sx,sy,sz)]
    c, s = math.cos(angle), math.sin(angle)
    vertices = [(center[0]+c*x-s*y,center[1]+s*x+c*y,center[2]+z)
                for x,y,z in vertices]
    faces = [(3,2,1,0),(4,5,6,7),(0,1,5,4),
             (1,2,6,5),(2,3,7,6),(3,0,4,7)]
    return outward(vertices,faces)


def annulus(cx,cy,profile,segments=48):
    """Watertight revolved radial-z section, all radii strictly positive."""
    assert len(profile)>=4 and min(r for r,z in profile)>0
    vertices=[(cx+r*math.cos(math.tau*j/segments),
               cy+r*math.sin(math.tau*j/segments),z)
              for r,z in profile for j in range(segments)]
    faces=[]
    for i in range(len(profile)):
        nxt=(i+1)%len(profile)
        for j in range(segments):
            k=(j+1)%segments
            a=i*segments+j
            b=i*segments+k
            c=nxt*segments+k
            d=nxt*segments+j
            faces.extend(((a,b,c),(a,c,d)))
    return outward(vertices,faces)


def disk(cx,cy,radius,z0,z1,segments=48):
    """Closed cylinder with planar triangulated caps; no fake solid torus."""
    assert radius>0 and z1>z0
    verts=[(cx+radius*math.cos(math.tau*j/segments),
            cy+radius*math.sin(math.tau*j/segments),z)
           for z in (z0,z1) for j in range(segments)]
    verts.extend(((cx,cy,z0),(cx,cy,z1)))
    faces=[]
    for j in range(segments):
        k=(j+1)%segments
        faces.extend(((j,k,segments+k),(j,segments+k,segments+j),
                      (2*segments,k,j),(2*segments+1,segments+j,segments+k)))
    return outward(verts,faces)


def build(spec):
    parts=[]
    def add(name,mat,geom,pod=None,moving=False,smooth=False):
        v,f=geom
        parts.append(dict(name=name,material=mat,vertices=v,faces=f,
                          pod=pod,moving=moving,smooth=smooth))

    # The top of this plate is exactly the underside mounting plane Z=0.
    add("chassis_mount_plate","graphite",box((0,0,-.050),(1.50,1.40,.100)))
    add("chassis_inset_armor","titanium",box((0,0,-.108),(1.29,1.19,.034)))
    add("central_controller","ceramic",box((0,0,-.114),(.34,.28,.075)))
    add("controller_status","ion_cyan",box((0,0,-.155),(.21,.10,.009)))
    for sx in (-1,1):
        for sy in (-1,1):
            add("mount_fastener_%s_%s"%(sx,sy),"copper",
                disk(sx*.68,sy*.60,.023,-.130,-.097,20))
            add("frame_corner_rubber_%s_%s"%(sx,sy),"rubber",
                box((sx*.68,sy*.60,-.081),(.11,.11,.040)))

    for i,(cx,cy) in enumerate(spec["emitter_centers_xy_m"]):
        tag="pod_%02d"%i
        a=math.atan2(cy,cx)
        # Spider struts run from central controller to each lower pod.
        add(tag+"_radial_brace","titanium",
            box((cx*.50,cy*.50,-.148),(.60,.064,.049),a))
        add(tag+"_mount_standoff","copper",disk(cx,cy,.052,-.189,-.092,24),pod=i)
        # Open circular housings: the emitter is visibly recessed, never
        # intersecting its stationary inner wall throughout the pulse.
        add(tag+"_housing","graphite",
            annulus(cx,cy,[(.218,-.304),(.288,-.304),
                           (.288,-.172),(.218,-.172)]),pod=i,smooth=True)
        add(tag+"_outer_bezel","titanium",
            annulus(cx,cy,[(.263,-.225),(.298,-.225),
                           (.298,-.181),(.263,-.181)]),pod=i,smooth=True)
        add(tag+"_ceramic_liner","ceramic",
            annulus(cx,cy,[(.207,-.289),(.225,-.289),
                           (.225,-.217),(.207,-.217)]),pod=i,smooth=True)
        add(tag+"_ion_trace","ion_cyan",
            annulus(cx,cy,[(.235,-.309),(.249,-.309),
                           (.249,-.299),(.235,-.299)]),pod=i,smooth=True)
        add(tag+"_thermal_amber","amber",
            annulus(cx,cy,[(.267,-.174),(.282,-.174),
                           (.282,-.168),(.267,-.168)]),pod=i,smooth=True)
        for j in range(8):
            theta=math.tau*j/8
            rr=.319
            add(tag+"_copper_fin_%02d"%j,"copper",
                box((cx+rr*math.cos(theta),cy+rr*math.sin(theta),-.248),
                    (.068,.027,.042),theta),pod=i)
        # Three connected layers travel together as one pulse root.
        add(tag+"_pulse_core","titanium",
            disk(cx,cy,.178,-.294,-.270),pod=i,moving=True,smooth=True)
        add(tag+"_pulse_ceramic","ceramic",
            disk(cx,cy,.148,-.301,-.289),pod=i,moving=True,smooth=True)
        add(tag+"_pulse_ion","ion_cyan",
            disk(cx,cy,.105,-.310,-.299),pod=i,moving=True,smooth=True)
    names=[p["name"] for p in parts]
    assert len(names)==len(set(names))
    return parts


def bounds(vertices):
    return [(min(v[k] for v in vertices),max(v[k] for v in vertices))
            for k in range(3)]


def boxes_overlap(a,b):
    return all(min(a[k][1],b[k][1])-max(a[k][0],b[k][0])>1e-8
               for k in range(3))


def pulse(spec,pod,frame):
    phase=math.radians(spec["animation"]["phase_offsets_deg"][pod])
    t=math.tau*(frame-1)/spec["animation"]["period_frames"]
    wave=math.sin(t+phase)
    scale=1+spec["animation"]["radial_scale_amplitude"]*wave
    dz=spec["animation"]["vertical_displacement_amplitude_m"]*wave
    return scale,dz


def moving_vertices(spec,part,frame):
    if not part["moving"]:
        return part["vertices"]
    pod=part["pod"]
    cx,cy=spec["emitter_centers_xy_m"][pod]
    scale,dz=pulse(spec,pod,frame)
    return [(cx+(x-cx)*scale,cy+(y-cy)*scale,z+dz)
            for x,y,z in part["vertices"]]


def mesh_qa(part):
    verts,faces=part["vertices"],part["faces"]
    edges=Counter()
    triangles=0
    for face in faces:
        assert len(face)>=3 and len(set(face))==len(face),part["name"]
        assert all(0<=i<len(verts) for i in face)
        a=verts[face[0]]
        for k in range(1,len(face)-1):
            b,c=verts[face[k]],verts[face[k+1]]
            u=[b[j]-a[j] for j in range(3)]
            v=[c[j]-a[j] for j in range(3)]
            cross=(u[1]*v[2]-u[2]*v[1],
                   u[2]*v[0]-u[0]*v[2],
                   u[0]*v[1]-u[1]*v[0])
            assert sum(x*x for x in cross)>1e-16,(part["name"],face)
            triangles+=1
        for a,b in zip(face,face[1:]+face[:1]):
            edges[tuple(sorted((a,b)))]+=1
    assert edges and all(v==2 for v in edges.values()),part["name"]
    assert signed_volume(verts,faces)>1e-10,part["name"]
    return triangles


def qa(spec,parts):
    fixed=[p for p in parts if not p["moving"]]
    moving=[p for p in parts if p["moving"]]
    assert len(moving)==12 and len(fixed)>50
    assert all(p["material"] in spec["materials"] for p in parts)
    assert all(all(math.isfinite(v) for v in xyz)
               for p in parts for xyz in p["vertices"])
    triangles=sum(mesh_qa(p) for p in parts)
    # AABB checks are conservative; for the annular fixed housing a
    # separate radial inner-bore test proves that the moving disk remains
    # in free space, not inside the solid annulus. Do not whitelist rings
    # without verifying their actual minimum radial extent.
    checks=0
    radial_checks=0
    min_gap=1e9
    for frame in range(1,82):
        mboxes=[]
        for p in moving:
            transformed=moving_vertices(spec,p,frame)
            mboxes.append((p,bounds(transformed),transformed))
        for p,mb,mverts in mboxes:
            pod=p["pod"]
            cx,cy=spec["emitter_centers_xy_m"][pod]
            maxrad=max(math.hypot(v[0]-cx,v[1]-cy) for v in mverts)
            for other in fixed:
                checks+=1
                if not boxes_overlap(mb,bounds(other["vertices"])):
                    continue
                if other["pod"]==pod:
                    minrad=min(math.hypot(v[0]-cx,v[1]-cy)
                               for v in other["vertices"])
                    gap=minrad-maxrad
                    assert gap>=.010,(frame,p["name"],other["name"],gap)
                    radial_checks+=1
                    min_gap=min(min_gap,gap)
                    continue
                raise AssertionError(("Fixed-movable AABB intersection",
                                      frame,p["name"],other["name"]))
        for k,(p,mb,_) in enumerate(mboxes):
            for q,qb,_ in mboxes[k+1:]:
                if p["pod"]==q["pod"]:
                    continue  # same emitter's connected layers
                checks+=1
                assert not boxes_overlap(mb,qb),(frame,p["name"],q["name"])
    assert radial_checks>0
    # Symmetry, range and explicit true mounting plane.
    xs=sorted(set(x for x,y in spec["emitter_centers_xy_m"]))
    ys=sorted(set(y for x,y in spec["emitter_centers_xy_m"]))
    assert xs==[-.45,.45] and ys==[-.4,.4]
    assert max(v[2] for p in parts for v in p["vertices"])<=1e-9
    return {
      "id":ID,"status":"PASS_STATIC","units":"metres",
      "mount":"underside_frame","mount_origin_m":[0,0,0],
      "emitter_count":4,"moving_pulse_roots":4,"animated_meshes":12,
      "parts":len(parts),"vertices":sum(len(p["vertices"]) for p in parts),
      "triangles":triangles,"materials":len(spec["materials"]),
      "closed_manifold_meshes":"PASS","positive_signed_volume":"PASS",
      "pulse_sweep_frames":81,"kinematic_aabb_pairs_checked":checks,
      "annular_radial_separation_checks":radial_checks,
      "min_verified_moving_radial_clearance_m":round(min_gap,6),
      "moving_collision_test":"PASS_CONSERVATIVE_AABB_PLUS_RING_RADIAL",
      "blender":"NOT_RUN","glb":"NOT_RUN","unity_import":"NOT_RUN",
      "unity_editmode":"NOT_RUN","unity_playmode":"NOT_RUN",
      "visual_review":"NOT_RUN"
    }


def write_static(spec,parts,out):
    out.mkdir(parents=True,exist_ok=True)
    obj=["# M04-007 quad hover; Blender Z-up; metres; underside mount Z=0",
         "mtllib M04-007.mtl"]
    offset=1
    for p in parts:
        obj.extend(("o "+p["name"],"usemtl "+p["material"]))
        obj.extend("v %.7f %.7f %.7f"%v for v in p["vertices"])
        obj.extend("f "+" ".join(str(offset+i) for i in face)
                   for face in p["faces"])
        offset+=len(p["vertices"])
    (out/"M04-007.obj").write_text("\n".join(obj)+"\n",encoding="utf-8")
    mtl=["# M04-007 7 visual materials"]
    for name,info in spec["materials"].items():
        r,g,b,_=info["color"]
        mtl.extend(("newmtl "+name,"Ka 0.04 0.04 0.04",
                    "Kd %.5f %.5f %.5f"%(r,g,b),
                    "Ks 0.30 0.30 0.30",
                    "Ns %.1f"%(50+150*info["metallic"]),
                    "d 1.0","illum 2",""))
    (out/"M04-007.mtl").write_text("\n".join(mtl)+"\n",encoding="utf-8")


def blender_export(spec,parts,out):
    import bpy
    # Safe repeated execution: remove ONLY the dedicated M04-007 collection.
    owned=bpy.data.collections.get(COLLECTION)
    if owned:
        for obj in tuple(owned.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(owned)
    owned=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(owned)
    mats={}
    for key,info in spec["materials"].items():
        name=ID+"_"+key
        mat=bpy.data.materials.get(name)
        if mat is None:
            mat=bpy.data.materials.new(name)
        mat.use_nodes=True
        shader=mat.node_tree.nodes.get("Principled BSDF")
        if shader:
            shader.inputs["Base Color"].default_value=tuple(info["color"])
            shader.inputs["Metallic"].default_value=info["metallic"]
            shader.inputs["Roughness"].default_value=info["roughness"]
            if "emission" in info:
                socket=shader.inputs.get("Emission Color") or shader.inputs.get("Emission")
                if socket:
                    socket.default_value=tuple(info["emission"])
                strength=shader.inputs.get("Emission Strength")
                if strength:
                    strength.default_value=info["emission_strength"]
        mat.diffuse_color=tuple(info["color"])
        mats[key]=mat
    root=bpy.data.objects.new(ID+"_MOUNT_ROOT",None)
    owned.objects.link(root)
    root.empty_display_type="PLAIN_AXES"
    root.empty_display_size=.11
    root["mount"]="underside_Z0"
    root["units"]="metres"
    roots={}
    for i,(cx,cy) in enumerate(spec["emitter_centers_xy_m"]):
        pulse_root=bpy.data.objects.new(ID+"_EMITTER_PULSE_%02d"%i,None)
        owned.objects.link(pulse_root)
        pulse_root.parent=root
        roots[i]=pulse_root
        pulse_root.empty_display_size=.05
        pulse_root["phase_degrees"]=spec["animation"]["phase_offsets_deg"][i]
        for frame in spec["animation"]["frames"]:
            scale,dz=pulse(spec,i,frame)
            pulse_root.location=(cx,cy,-.282+dz)
            pulse_root.scale=(scale,scale,1)
            pulse_root.keyframe_insert(data_path="location",frame=frame)
            pulse_root.keyframe_insert(data_path="scale",frame=frame)
        if pulse_root.animation_data and pulse_root.animation_data.action:
            action=pulse_root.animation_data.action
            if hasattr(action,"fcurves"):
                for curve in action.fcurves:
                    for key in curve.keyframe_points:
                        key.interpolation="LINEAR"
    for p in parts:
        if p["moving"]:
            cx,cy=spec["emitter_centers_xy_m"][p["pod"]]
            pivot=(cx,cy,-.282)
            verts=[(v[0]-pivot[0],v[1]-pivot[1],v[2]-pivot[2])
                   for v in p["vertices"]]
            parent=roots[p["pod"]]
        else:
            verts=p["vertices"]
            parent=root
        mesh=bpy.data.meshes.new(ID+"_"+p["name"]+"_mesh")
        mesh.from_pydata(verts,[],p["faces"])
        mesh.update(calc_edges=True)
        ob=bpy.data.objects.new(ID+"_"+p["name"],mesh)
        owned.objects.link(ob)
        ob.parent=parent
        ob.data.materials.append(mats[p["material"]])
        ob["model_id"]=ID
        ob["moving"]=p["moving"]
        for polygon in mesh.polygons:
            polygon.use_smooth=p["smooth"]
    scene=bpy.context.scene
    prev=(scene.frame_current,scene.frame_start,scene.frame_end,
          tuple(bpy.context.selected_objects),bpy.context.view_layer.objects.active)
    try:
        scene.frame_start=1
        scene.frame_end=81
        scene.render.fps=60
        scene.frame_set(1)
        bpy.context.view_layer.update()
        assert len(owned.objects)==len(parts)+5
        out.mkdir(parents=True,exist_ok=True)
        bpy.data.libraries.write(str(out/"M04-007.blend"),{owned},compress=True)
        bpy.ops.object.select_all(action="DESELECT")
        for ob in owned.objects:
            ob.select_set(True)
        bpy.context.view_layer.objects.active=root
        bpy.ops.export_scene.gltf(
            filepath=str(out/"M04-007_animated.glb"),
            export_format="GLB",use_selection=True,
            export_animations=True,export_apply=False)
    finally:
        frame,start,end,selected,active=prev
        bpy.ops.object.select_all(action="DESELECT")
        for ob in selected:
            if ob.name in bpy.data.objects:
                ob.select_set(True)
        if active is not None and active.name in bpy.data.objects:
            bpy.context.view_layer.objects.active=active
        scene.frame_start=start
        scene.frame_end=end
        scene.frame_set(frame)


def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--self-test",action="store_true")
    parser.add_argument("--require-blender",action="store_true")
    parser.add_argument("--output-dir",default=str(HERE/"generated"))
    opts=parser.parse_args(argv)
    spec=spec_read()
    parts=build(spec)
    report=qa(spec,parts)
    out=Path(opts.output_dir).resolve()
    write_static(spec,parts,out)
    if not opts.self_test:
        try:
            import bpy  # noqa: F401
        except ImportError:
            if opts.require_blender:
                raise RuntimeError("Blender bpy is not available")
        else:
            blender_export(spec,parts,out)
            report["blender"]="EXECUTED"
            report["glb"]="EXPORTED_NOT_UNITY_VERIFIED"
    (out/"M04-007.static-qa.json").write_text(
        json.dumps(report,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2,ensure_ascii=False))


if __name__=="__main__":
    main()
