#!/usr/bin/env python3
"""M04-010 — dual-deck centrifugal stabilizer for RoboRougelike / Unity 6.

Z-up Blender, metres, origin at the BOTTOM face of the TOP-SOCKET mount.
Two balanced counterweight decks rotate about their own local Z pivots,
in opposite directions. This is a VISUAL asset, not a gameplay mechanic.

Run:
 python3 generate_m04_010.py --self-test
 python3 generate_m04_010.py --static-obj --out build/M04-010
 blender -b --factory-startup --python-exit-code 1 --python \
     generate_m04_010.py -- --out build/M04-010

Rerunning bpy removes only the M04-010 namespaced collection and unused
meshes/actions/materials, never other scene objects.
"""
from __future__ import annotations

import argparse
from collections import Counter
import json
import math
from pathlib import Path
import sys

ID = "M04-010"
NS = "RRL_M04_010_"
COLLECTION = NS + "CENTRIFUGAL_STABILIZER"
LOW_Z = .280
HIGH_Z = .510
FRAMES = 121
FPS = 30
MAX_ROTATIONS = 1.0
MATERIALS = {
    "Graphite_Armor": ((.054,.069,.083,1.), .78,.33,0.),
    "Satin_Titanium": ((.47,.54,.60,1.), .82,.26,0.),
    "Tungsten_Weights": ((.19,.21,.24,1.), .84,.29,0.),
    "Brushed_Copper": ((.70,.32,.095,1.), .75,.34,0.),
    "Ivory_Ceramic": ((.83,.82,.72,1.), .18,.39,0.),
    "Rubber_Gasket": ((.025,.034,.044,1.), .03,.78,0.),
    "Cyan_Field": ((.025,.72,1.,1.), .28,.27,3.0),
    "Amber_Status": ((1.,.38,.045,1.), .28,.31,2.3),
}
GROUP_Z = {"LOW":LOW_Z, "HIGH":HIGH_Z}
BOX_FACES = [
    (0,3,2),(0,2,1),(4,5,6),(4,6,7),
    (0,1,5),(0,5,4),(1,2,6),(1,6,5),
    (2,3,7),(2,7,6),(3,0,4),(3,4,7)
]

def part(name, vertices, faces, material, group="STATIC", smooth=False):
    return dict(name=name,vertices=vertices,faces=faces,material=material,
                group=group,smooth=smooth)

def box(name, centre, size, material, group="STATIC", angle=0.):
    """Bevelled octagonal box: 8 XY corners, capped and triangulated."""
    x,y,z=centre
    a,b,h=(d/2 for d in size)
    assert min(size)>0
    bevel=min(a,b)*.24
    xy=[(-a+bevel,-b),(a-bevel,-b),(a,-b+bevel),
        (a,b-bevel),(a-bevel,b),(-a+bevel,b),
        (-a,b-bevel),(-a,-b+bevel)]
    c,s=math.cos(angle),math.sin(angle)
    xy=[(x+c*u-s*v,y+s*u+c*v) for u,v in xy]
    vertices=[(u,v,z-h) for u,v in xy]+[(u,v,z+h) for u,v in xy]
    faces=[]
    for i in range(1,7):
        faces.extend(((0,i+1,i),(8,8+i,8+i+1)))
    for i in range(8):
        j=(i+1)%8
        faces.extend(((i,j,8+j),(i,8+j,8+i)))
    return part(name,vertices,faces,material,group)

def torus(name, centre, major, minor, material, group="STATIC",
          segments=48, sides=12):
    """Closed, outward-wound torus about local Z."""
    assert major>minor>0
    cx,cy,cz=centre
    vertices=[]
    for i in range(segments):
        u=2*math.pi*i/segments
        for j in range(sides):
            v=2*math.pi*j/sides
            r=major+minor*math.cos(v)
            vertices.append((cx+r*math.cos(u),cy+r*math.sin(u),
                             cz+minor*math.sin(v)))
    faces=[]
    for i in range(segments):
        for j in range(sides):
            a=i*sides+j
            b=((i+1)%segments)*sides+j
            c=((i+1)%segments)*sides+(j+1)%sides
            d=i*sides+(j+1)%sides
            faces.extend(((a,b,c),(a,c,d)))
    return part(name,vertices,faces,material,group,True)

def cylinder(name, centre, radius, height, material,
             group="STATIC", segments=48):
    x,y,z=centre
    vertices=[]
    for dz in (-height/2,height/2):
        for i in range(segments):
            a=2*math.pi*i/segments
            vertices.append((x+radius*math.cos(a),
                             y+radius*math.sin(a),z+dz))
    vertices.extend(((x,y,z-height/2),(x,y,z+height/2)))
    faces=[]
    for i in range(segments):
        j=(i+1)%segments
        faces.extend(((i,j,segments+j),(i,segments+j,segments+i),
                      (2*segments,j,i),
                      (2*segments+1,segments+i,segments+j)))
    return part(name,vertices,faces,material,group,True)

def build():
    parts=[]
    add=parts.append
    # Top-socket interface: all geometry at Z>=0, 0 is the mating face.
    add(box("TOP_SOCKET_MOUNT",(0,0,.025),(.780,.780,.050),"Satin_Titanium"))
    add(box("MOUNT_GASKET",(0,0,.058),(.708,.708,.018),"Rubber_Gasket"))
    add(box("ARMOR_PLINTH",(0,0,.088),(.742,.742,.043),"Graphite_Armor"))
    add(cylinder("CENTRAL_STATOR_SPINDLE",(0,0,.371),.022,.538,
                 "Satin_Titanium"))
    add(cylinder("SPINDLE_BASE",(0,0,.136),.056,.065,"Brushed_Copper"))
    # Bearings are hollow toroidal collars, never solid discs crossing
    # the spinning spindle.
    for name,z in (("LOW",.185),("MID",.396),("TOP",.628)):
        add(torus("SPINDLE_BEARING_"+name,(0,0,z),.041,.010,
                  "Brushed_Copper"))
    # Four distant, continuous cage pylons: no interference with either
    # rotor's conservative swept cylinder, at ANY angle.
    for sx in (-1,1):
        for sy in (-1,1):
            tag=("P" if sx>0 else "N")+("P" if sy>0 else "N")
            x,y=sx*.330,sy*.330
            add(box("CAGE_PYLON_"+tag,(x,y,.369),
                    (.038,.038,.558),"Graphite_Armor"))
            add(box("CAGE_TITANIUM_STRIP_"+tag,(x,y,.370),
                    (.012,.012,.490),"Satin_Titanium"))
            add(box("BASE_BOLT_"+tag,(sx*.343,sy*.343,.118),
                    (.030,.030,.018),"Brushed_Copper"))
            add(box("TOP_STATUS_"+tag,(x,y,.651),
                    (.022,.022,.014),"Amber_Status"))
    # Each static outer safety ring is attached to all four pylons by
    # diagonal braces, rather than floating around the moving mass.
    for deck,z in (("LOW",LOW_Z),("HIGH",HIGH_Z)):
        add(torus("SAFETY_RING_"+deck,(0,0,z),.402,.012,
                  "Satin_Titanium"))
        add(torus("SAFETY_CYAN_"+deck,(0,0,z+.015),.402,.004,
                  "Cyan_Field",segments=48,sides=8))
        for sx in (-1,1):
            for sy in (-1,1):
                a=math.atan2(sy,sx)
                x,y=sx*.301,sy*.301
                tag=("P" if sx>0 else "N")+("P" if sy>0 else "N")
                add(box("SAFETY_BRACE_"+deck+"_"+tag,(x,y,z),
                        (.116,.020,.025),"Brushed_Copper",angle=a))
    # Stator inspection shelf between two rotor decks.
    add(torus("MID_FIELD_GUARD",(0,0,.397),.380,.009,
              "Ivory_Ceramic"))
    add(torus("TOP_CAGE_LIP",(0,0,.658),.440,.011,
              "Satin_Titanium"))
    for deck,z in GROUP_Z.items():
        # Group vertices are LOCAL to the deck pivot, not absolute Z.
        add(torus("ROTATING_HUB_"+deck,(0,0,0),.048,.014,
                  "Satin_Titanium",deck))
        add(torus("ROTOR_OUTER_RING_"+deck,(0,0,0),.334,.011,
                  "Graphite_Armor",deck))
        add(torus("ROTOR_COPPER_INLAY_"+deck,(0,0,.009),.334,.004,
                  "Brushed_Copper",deck,48,8))
        # Two opposed solid counterweights are connected to a common
        # annular hub, the outer flywheel ring, and four visible bridges.
        for sign in (-1,1):
            side="P" if sign>0 else "N"
            add(box("WEIGHT_ARM_"+deck+"_"+side,
                    (sign*.145,0,0),(.190,.044,.042),
                    "Satin_Titanium",deck))
            add(box("TUNGSTEN_COUNTERWEIGHT_"+deck+"_"+side,
                    (sign*.225,0,0),(.156,.114,.105),
                    "Tungsten_Weights",deck))
            add(box("WEIGHT_ARMOR_FACE_"+deck+"_"+side,
                    (sign*.225,0,.054),(.130,.087,.010),
                    "Graphite_Armor",deck))
            add(box("WEIGHT_CERAMIC_LABEL_"+deck+"_"+side,
                    (sign*.225,0,.061),(.092,.028,.006),
                    "Ivory_Ceramic",deck))
            add(box("WEIGHT_CYAN_LED_"+deck+"_"+side,
                    (sign*.225,0,.065),(.058,.012,.006),
                    "Cyan_Field",deck))
            add(box("RING_JOIN_"+deck+"_"+side,
                    (sign*.318,0,0),(.053,.025,.026),
                    "Satin_Titanium",deck))
            add(box("OUTER_COPPER_CAP_"+deck+"_"+side,
                    (sign*.304,0,.0),(.009,.084,.070),
                    "Brushed_Copper",deck))
            # Four small fasteners per mass, all attached to its top face.
            for sy in (-1,1):
                add(cylinder("WEIGHT_BOLT_"+deck+"_"+side+"_"+str(sy),
                             (sign*.265,sy*.036,.058),.007,.012,
                             "Brushed_Copper",deck,16))
        # Two balancing marker strips are fixed to the rotor ring itself.
        for sy in (-1,1):
            add(box("RIM_AMBER_MARKER_"+deck+"_"+str(sy),
                    (0,sy*.334,.012),(.045,.016,.010),
                    "Amber_Status",deck))
    return parts

def sub(a,b): return tuple(x-y for x,y in zip(a,b))
def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],
            a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])
def dot(a,b): return sum(x*y for x,y in zip(a,b))

def check_mesh(p):
    vertices=p["vertices"]
    edges=Counter();directions=Counter();volume=0.
    for f in p["faces"]:
        assert len(f)==3 and len(set(f))==3,(p["name"],f)
        assert all(0<=i<len(vertices) for i in f),p["name"]
        a,b,c=(vertices[i] for i in f)
        n=cross(sub(b,a),sub(c,a))
        assert dot(n,n)>1e-17,("degenerate",p["name"])
        volume+=dot(a,cross(b,c))/6
        for u,v in ((f[0],f[1]),(f[1],f[2]),(f[2],f[0])):
            edges[tuple(sorted((u,v)))]+=1
            directions[(u,v)]+=1
    assert edges and all(n==2 for n in edges.values()),("manifold",p["name"])
    assert all(directions[(v,u)]==n for (u,v),n in directions.items()),(
        "winding",p["name"])
    assert volume>1e-12,("signed-volume",p["name"],volume)
    assert all(all(math.isfinite(x) for x in v) for v in vertices)
    return volume

def transform(p,v,t):
    if p["group"]=="STATIC": return v
    phase=2*math.pi*t*(1 if p["group"]=="LOW" else -1)
    c,s=math.cos(phase),math.sin(phase)
    x,y,z=v
    return (c*x-s*y,s*x+c*y,z+GROUP_Z[p["group"]])

def interval(p,axis):
    v=[x[axis] for x in p["vertices"]]
    return min(v),max(v)

def validate(parts):
    names=set();vertex_count=0;triangle_count=0
    for p in parts:
        assert p["name"] not in names,p["name"]
        names.add(p["name"])
        assert p["material"] in MATERIALS
        assert p["group"] in ("STATIC","LOW","HIGH")
        check_mesh(p)
        vertex_count+=len(p["vertices"])
        triangle_count+=len(p["faces"])
    assert len(MATERIALS)>=3
    assert sum(p["group"]=="LOW" for p in parts)==21
    assert sum(p["group"]=="HIGH" for p in parts)==21
    statics=[p for p in parts if p["group"]=="STATIC"]
    clearance=float("inf");fixed_tests=0;rotation_checks=0
    swept={}
    for group,z in GROUP_Z.items():
        moving=[p for p in parts if p["group"]==group]
        vertices=[v for p in moving for v in p["vertices"]]
        rmin=min(math.hypot(x,y) for x,y,_ in vertices)
        rmax=max(math.hypot(x,y) for x,y,_ in vertices)
        zmin=z+min(v[2] for v in vertices)
        zmax=z+max(v[2] for v in vertices)
        assert rmin>.029 and rmax<.352,(group,rmin,rmax)
        assert .21<zmin<zmax<.590,(group,zmin,zmax)
        swept[group]=dict(r_min_m=round(rmin,6),r_max_m=round(rmax,6),
                          z_min_m=round(zmin,6),z_max_m=round(zmax,6))
        for fixed in statics:
            a,b=interval(fixed,2)
            z_gap=max(a-zmax,zmin-b,0.)
            if z_gap>0:
                sep=z_gap
            elif fixed["name"]=="CENTRAL_STATOR_SPINDLE":
                # Cylindrical spindle is centered on the Z axis.
                spindle_max=max(math.hypot(x,y) for x,y,_ in fixed["vertices"])
                sep=rmin-spindle_max
            else:
                # AABB distance to the Z axis underestimates the true
                # minimum radial distance of the fixed geometry.
                (x0,x1),(y0,y1)=(interval(fixed,k) for k in (0,1))
                nearest_x=max(x0,0.,-x1)
                nearest_y=max(y0,0.,-y1)
                sep=math.hypot(nearest_x,nearest_y)-rmax
            assert sep>.003,(group,fixed["name"],sep)
            clearance=min(clearance,sep)
            fixed_tests+=1
        # Independent opposing decks have disjoint vertical envelopes.
        for frame in range(FRAMES):
            t=frame/(FRAMES-1)
            for p in moving:
                for v in p["vertices"]:
                    world=transform(p,v,t)
                    assert abs(math.hypot(*world[:2])-math.hypot(*v[:2]))<1e-9
                    assert abs(world[2]-(z+v[2]))<1e-9
                    rotation_checks+=1
    assert swept["LOW"]["z_max_m"]+.02<swept["HIGH"]["z_min_m"]
    mount=min(transform(p,v,0)[2] for p in parts for v in p["vertices"])
    assert abs(mount)<1e-9
    return dict(asset=ID,status="PASS",parts=len(parts),
                vertices=vertex_count,triangles=triangle_count,
                materials=len(MATERIALS),moving_groups={"LOW":21,"HIGH":21},
                pivot_m=[0,0,0],mount="TOP_SOCKET, bottom face Z=0",
                axis="two counter-rotating local Z pivots",
                rotor_centres_m={"LOW":[0,0,LOW_Z],"HIGH":[0,0,HIGH_Z]},
                animation_frames=FRAMES,fps=FPS,
                rotor_turns={"LOW":1,"HIGH":-1},
                swept_envelopes=swept,
                checked_fixed_pairs=fixed_tests,
                sampled_rotating_vertex_checks=rotation_checks,
                conservative_min_static_clearance_m=round(clearance,6),
                qa_scope="watertight signed winding; two disjoint rotor Z envelopes; conservative swept radial cylinder vs all fixed geometry; 121 pose radial invariance",
                blender="NOT RUN",unity="NOT RUN",visual_inspection="NOT RUN")

def export_obj(parts,folder):
    folder.mkdir(parents=True,exist_ok=True)
    mtl=ID+"_centrifugal_stabilizer.mtl"
    obj=ID+"_centrifugal_stabilizer.obj"
    lines=[]
    for name,(rgba,metal,rough,emission) in MATERIALS.items():
        lines.extend(("newmtl "+name,
            "Kd %.6f %.6f %.6f"%rgba[:3],
            "Ks %.4f %.4f %.4f"%(metal,metal,metal),
            "Ns %.3f"%((1-rough)*800),
            "Ke %.4f %.4f %.4f"%tuple(c*emission for c in rgba[:3]),
            "d %.4f"%rgba[3],"illum 2",""))
    (folder/mtl).write_text("\n".join(lines),encoding="utf-8")
    lines=["# Rest-pose static OBJ. Animated rig exported separately as GLB.",
           "mtllib "+mtl]
    offset=1
    for p in parts:
        lines.extend(("o "+p["name"],"usemtl "+p["material"]))
        lines.extend("v %.8f %.8f %.8f"%transform(p,v,0) for v in p["vertices"])
        lines.extend("f %d %d %d"%(a+offset,b+offset,c+offset)
                     for a,b,c in p["faces"])
        offset+=len(p["vertices"])
    (folder/obj).write_text("\n".join(lines)+"\n",encoding="utf-8")
    return dict(backend="python-static-obj",obj=obj,mtl=mtl,
                blender="NOT RUN",glb="NOT RUN",unity="NOT RUN")

def blender_export(parts,folder):
    import bpy
    # Namespaced cleanup; never delete unowned Blender scene contents.
    old=bpy.data.collections.get(COLLECTION)
    if old:
        for ob in tuple(old.objects):
            bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(old)
    for mesh in tuple(bpy.data.meshes):
        if mesh.name.startswith(NS) and mesh.users==0:
            bpy.data.meshes.remove(mesh)
    for action in tuple(bpy.data.actions):
        if action.name.startswith(NS) and action.users==0:
            bpy.data.actions.remove(action)
    coll=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(coll)
    root=bpy.data.objects.new(NS+"TOP_SOCKET_ROOT",None)
    coll.objects.link(root)
    root.empty_display_type="PLAIN_AXES"
    root.empty_display_size=.075
    root["mount"]="TOP_SOCKET / Z=0 bottom mating plane"
    root["metres_per_unit"]=1.0
    pivots={}
    for group,z in GROUP_Z.items():
        pivot=bpy.data.objects.new(NS+group+"_SPIN_Z",None)
        coll.objects.link(pivot)
        pivot.parent=root
        pivot.location=(0,0,z)
        pivot.empty_display_type="ARROWS"
        pivot.empty_display_size=.08
        pivot["axis"]="LOCAL_Z"
        pivot["rotation_direction"]=("CCW" if group=="LOW" else "CW")
        pivots[group]=pivot
    mats={}
    for name,(rgba,metal,rough,emission) in MATERIALS.items():
        mat=bpy.data.materials.get(NS+name) or bpy.data.materials.new(NS+name)
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value=rgba
        bsdf.inputs["Metallic"].default_value=metal
        bsdf.inputs["Roughness"].default_value=rough
        if emission:
            key="Emission Color" if "Emission Color" in bsdf.inputs else "Emission"
            if key in bsdf.inputs: bsdf.inputs[key].default_value=rgba
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value=emission
        mat.diffuse_color=rgba
        mats[name]=mat
    for p in parts:
        mesh=bpy.data.meshes.new(NS+p["name"]+"_MESH")
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.update()
        obj=bpy.data.objects.new(NS+p["name"],mesh)
        coll.objects.link(obj)
        obj.parent=root if p["group"]=="STATIC" else pivots[p["group"]]
        mesh.materials.append(mats[p["material"]])
        for poly in mesh.polygons:
            poly.use_smooth=p["smooth"]
    scene=bpy.context.scene
    scene.frame_start=1
    scene.frame_end=FRAMES
    scene.render.fps=FPS
    # Quarter-turn keys avoid GLB quaternion shortest-path collapsing a
    # 360-degree spin into a static pose.
    for frame in (1,31,61,91,121):
        t=(frame-1)/(FRAMES-1)
        for group,sign in (("LOW",1),("HIGH",-1)):
            pivot=pivots[group]
            pivot.rotation_euler[2]=sign*2*math.pi*t
            pivot.keyframe_insert(data_path="rotation_euler",frame=frame,
                                  group="SPIN_"+group)
    for pivot in pivots.values():
        if pivot.animation_data and pivot.animation_data.action:
            action=pivot.animation_data.action
            action.name=NS+pivot.name.rsplit("_",2)[-2]+"_SPIN"
            for fc in action.fcurves:
                for key in fc.keyframe_points:
                    key.interpolation="LINEAR"
    scene.frame_set(1)
    bpy.context.view_layer.update()
    folder.mkdir(parents=True,exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for ob in coll.objects: ob.select_set(True)
    bpy.context.view_layer.objects.active=root
    blend=folder/(ID+"_centrifugal_stabilizer.blend")
    glb=folder/(ID+"_centrifugal_stabilizer.glb")
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
                              use_selection=True,export_animations=True)
    assert blend.exists() and blend.stat().st_size>1000
    assert glb.exists() and glb.stat().st_size>1000
    return dict(backend="blender-bpy",blend=blend.name,glb=glb.name,
                blender="EXPORTED",unity="NOT RUN")

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    ap=argparse.ArgumentParser()
    ap.add_argument("--out",default="build/M04-010")
    ap.add_argument("--self-test",action="store_true")
    ap.add_argument("--static-obj",action="store_true")
    args=ap.parse_args(argv)
    parts=build()
    report=validate(parts)
    if args.self_test:
        print(json.dumps(report,indent=2))
        return
    folder=Path(args.out).resolve()
    if args.static_obj:
        exported=export_obj(parts,folder)
    else:
        try: import bpy
        except ImportError: exported=export_obj(parts,folder)
        else: exported=blender_export(parts,folder)
    report.update(exported)
    folder.mkdir(parents=True,exist_ok=True)
    (folder/(ID+"_manifest.json")).write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))

if __name__=="__main__":
    main()
