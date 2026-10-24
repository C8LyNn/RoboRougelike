#!/usr/bin/env python3
"""M04-009 — internal 3-axis gyroscopic stabilizer for Unity 6.

Z-up Blender coordinates; 1 unit = 1 metre. Origin is the bottom-centre
mounting face of the chassis floor plate, Z=0. Gimbal centre Z=0.380m.
Local rotation hierarchy: OUTER_Z -> MIDDLE_Y -> INNER_X. The assembly is
a visual-only module; it does not implement gyroscopic gameplay physics.

  python3 generate_m04_009.py --self-test
  python3 generate_m04_009.py --static-obj --out build/M04-009
  blender -b --factory-startup --python-exit-code 1 --python \
    generate_m04_009.py -- --out build/M04-009
"""
from __future__ import annotations

import argparse
from collections import Counter
import json
import math
from pathlib import Path
import sys

MODEL = "M04-009"
NS = "RRL_M04_009_"
COLLECTION = NS + "GYROSCOPE"
CENTRE_Z = .380
FRAMES = 121
FPS = 30
MATERIALS = {
    "Graphite_Armor": ((.063,.080,.100,1), .80,.30,0),
    "Satin_Titanium": ((.46,.51,.56,1), .76,.27,0),
    "Brushed_Copper": ((.62,.28,.095,1), .72,.33,0),
    "Ivory_Ceramic": ((.81,.80,.70,1), .20,.39,0),
    "Dark_Elastomer": ((.035,.042,.049,1), .05,.77,0),
    "Cyan_Magnetic": ((.025,.77,.96,1), .25,.25,3.2),
    "Amber_Status": ((1.0,.43,.07,1), .24,.33,2.0),
    "Core_Glass": ((.16,.34,.44,1), .22,.18,.7),
}
BOX_FACES = [
    (0,3,2),(0,2,1),(4,5,6),(4,6,7),
    (0,1,5),(0,5,4),(1,2,6),(1,6,5),
    (2,3,7),(2,7,6),(3,0,4),(3,4,7)
]
# The rings have disjoint spherical radial shells, so three nested
# articulated axes cannot intersect at ANY orientation.
RING_RADIAL_SHELLS = {
    "OUTER": (.211,.263),
    "MIDDLE": (.172,.215),
    "INNER": (.0,.168),
}

def part(name, vertices, faces, material, group="STATIC", smooth=False):
    return dict(name=name, vertices=vertices, faces=faces, material=material,
                group=group, smooth=smooth)

def box(name, centre, size, material, group="STATIC"):
    x,y,z=centre
    a,b,c=(d/2 for d in size)
    assert min(size)>0
    vertices=[
        (x-a,y-b,z-c),(x+a,y-b,z-c),(x+a,y+b,z-c),(x-a,y+b,z-c),
        (x-a,y-b,z+c),(x+a,y-b,z+c),(x+a,y+b,z+c),(x-a,y+b,z+c)]
    return part(name,vertices,BOX_FACES,material,group)

def torus(name, major, minor, axis, material, group, segments=48, sides=12):
    assert major>minor>0 and segments>=12 and sides>=8
    vertices=[]
    for i in range(segments):
        u=2*math.pi*i/segments
        for j in range(sides):
            v=2*math.pi*j/sides
            rad=major+minor*math.cos(v)
            x,y,z=rad*math.cos(u),rad*math.sin(u),minor*math.sin(v)
            if axis=="Y": x,y,z=x,-z,y
            elif axis=="X": x,y,z=z,y,-x
            else: assert axis=="Z"
            vertices.append((x,y,z))
    faces=[]
    for i in range(segments):
        for j in range(sides):
            a=i*sides+j
            b=((i+1)%segments)*sides+j
            c=((i+1)%segments)*sides+(j+1)%sides
            d=i*sides+(j+1)%sides
            faces.extend(((a,b,c),(a,c,d)))
    return part(name,vertices,faces,material,group,True)

def cylinder_z(name, centre, radius, height, material, group="STATIC", segments=24):
    x,y,z=centre
    vertices=[]
    for zz in (z-height/2,z+height/2):
        for i in range(segments):
            a=2*math.pi*i/segments
            vertices.append((x+radius*math.cos(a),y+radius*math.sin(a),zz))
    vertices.extend(((x,y,z-height/2),(x,y,z+height/2)))
    faces=[]
    for i in range(segments):
        j=(i+1)%segments
        faces.extend(((i,j,segments+j),(i,segments+j,segments+i),
                      (2*segments,j,i),(2*segments+1,segments+i,segments+j)))
    return part(name,vertices,faces,material,group,True)

def uv_sphere(name, radius, material, group, rings=14, segments=28):
    vertices=[(0,0,radius)]
    for k in range(1,rings):
        phi=math.pi*k/rings
        for j in range(segments):
            theta=2*math.pi*j/segments
            vertices.append((radius*math.sin(phi)*math.cos(theta),
                             radius*math.sin(phi)*math.sin(theta),
                             radius*math.cos(phi)))
    bottom=len(vertices)
    vertices.append((0,0,-radius))
    faces=[]
    for j in range(segments):
        n=(j+1)%segments
        faces.append((0,1+j,1+n))
    for k in range(rings-2):
        a=1+k*segments
        b=a+segments
        for j in range(segments):
            n=(j+1)%segments
            faces.extend(((a+j,b+j,b+n),(a+j,b+n,a+n)))
    a=1+(rings-2)*segments
    for j in range(segments):
        n=(j+1)%segments
        faces.append((bottom,a+n,a+j))
    return part(name,vertices,faces,material,group,True)

def build():
    parts=[]
    add=parts.append
    # Only the bottom of this floor plate touches the internal chassis socket.
    add(box("MOUNT_FLOOR", (0,0,.018), (.570,.570,.036),"Satin_Titanium"))
    add(box("MOUNT_GASKET", (0,0,.043), (.518,.518,.014),"Dark_Elastomer"))
    add(box("LOWER_TRAY", (0,0,.067), (.540,.540,.034),"Graphite_Armor"))
    for sx in (-1,1):
        for sy in (-1,1):
            suffix=("%s_%s" % ("P" if sx>0 else "N","P" if sy>0 else "N"))
            x,y=sx*.276,sy*.276
            add(box("CORNER_PYLON_"+suffix,(x,y,.384),
                    (.034,.034,.600),"Graphite_Armor"))
            add(box("CORNER_TITANIUM_"+suffix,(x,y,.392),
                    (.012,.012,.558),"Satin_Titanium"))
            add(box("FOOT_BOLT_"+suffix,(sx*.233,sy*.233,.087),
                    (.026,.026,.010),"Brushed_Copper"))
            add(box("STATUS_LED_"+suffix,(x,y,.698),
                    (.023,.023,.012),"Amber_Status"))
    # Roof and floor cage rails are outside the swept gyroscopic envelope.
    for sx in (-1,1):
        for z,label in ((.710,"TOP"),(.101,"LOW")):
            add(box("RAIL_X_%s_%s"%(label,sx),(sx*.276,0,z),
                    (.028,.575,.025),"Satin_Titanium"))
    for sy in (-1,1):
        for z,label in ((.710,"TOP"),(.101,"LOW")):
            add(box("RAIL_Y_%s_%s"%(label,sy),(0,sy*.276,z),
                    (.575,.028,.025),"Satin_Titanium"))
    for sx in (-1,1):
        add(box("COIL_BASE_"+str(sx),(sx*.250,0,.098),
                (.034,.120,.020),"Brushed_Copper"))
    # 3-axis spherical nesting: each ring's radius is strictly separated
    # from adjacent rings, including decorative studs and trim.
    add(torus("OUTER_GIMBAL_Z",.236,.014,"Z",
              "Satin_Titanium","OUTER"))
    add(torus("OUTER_COPPER_EDGE",.247,.004,"Z",
              "Brushed_Copper","OUTER"))
    for i in range(8):
        a=2*math.pi*i/8
        x,y=.238*math.cos(a),.238*math.sin(a)
        add(box("OUTER_EMITTER_%02d"%i,(x,y,0),
                (.016,.016,.017),"Cyan_Magnetic" if i%2==0 else
                "Ivory_Ceramic","OUTER"))
    add(torus("MIDDLE_GIMBAL_Y",.190,.014,"Y",
              "Graphite_Armor","MIDDLE"))
    add(torus("MIDDLE_COPPER_EDGE",.201,.004,"Y",
              "Brushed_Copper","MIDDLE"))
    for i in range(6):
        a=2*math.pi*i/6
        x,z=.190*math.cos(a),.190*math.sin(a)
        add(box("MIDDLE_SENSOR_%02d"%i,(x,0,z),
                (.015,.016,.015),"Ivory_Ceramic" if i%2 else
                "Cyan_Magnetic","MIDDLE"))
    add(torus("INNER_GIMBAL_X",.145,.012,"X",
              "Satin_Titanium","INNER"))
    add(torus("INNER_CERAMIC_EDGE",.154,.004,"X",
              "Ivory_Ceramic","INNER"))
    for i in range(4):
        a=math.pi*i/2
        y,z=.146*math.cos(a),.146*math.sin(a)
        add(box("INNER_FIELD_NODE_%02d"%i,(0,y,z),
                (.017,.015,.015),"Cyan_Magnetic","INNER"))
    add(uv_sphere("CORE_GLASS_SPHERE",.084,"Core_Glass","INNER"))
    add(torus("CORE_EQUATOR",.088,.005,"X",
              "Brushed_Copper","INNER",32,10))
    add(uv_sphere("CORE_LUMEN",.048,"Cyan_Magnetic","INNER",10,20))
    for s in (-1,1):
        add(box("CORE_SPOKE_Y_"+str(s),(0,s*.111,0),
                (.018,.086,.018),"Brushed_Copper","INNER"))
        add(box("CORE_SPOKE_Z_"+str(s),(0,0,s*.111),
                (.018,.018,.086),"Brushed_Copper","INNER"))
    return parts

def sub(a,b): return tuple(x-y for x,y in zip(a,b))
def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def dot(a,b): return sum(x*y for x,y in zip(a,b))
def rot_z(p,a):
    c,s=math.cos(a),math.sin(a);x,y,z=p
    return (c*x-s*y,s*x+c*y,z)
def rot_y(p,a):
    c,s=math.cos(a),math.sin(a);x,y,z=p
    return (c*x+s*z,y,-s*x+c*z)
def rot_x(p,a):
    c,s=math.cos(a),math.sin(a);x,y,z=p
    return (x,c*y-s*z,s*y+c*z)

def angles(t):
    return (math.radians(38)*math.sin(2*math.pi*t),
            math.radians(28)*math.sin(4*math.pi*t+.4),
            2*math.pi*t)

def pose_vertex(p,vertex,t):
    if p["group"]=="STATIC": return vertex
    yaw,pitch,roll=angles(t)
    v=vertex
    if p["group"]=="INNER": v=rot_x(v,roll)
    if p["group"] in ("INNER","MIDDLE"): v=rot_y(v,pitch)
    v=rot_z(v,yaw)
    return (v[0],v[1],v[2]+CENTRE_Z)

def check_mesh(p):
    vertices=p["vertices"]
    edges=Counter();winding=Counter();volume=0.
    for f in p["faces"]:
        assert len(f)==3 and len(set(f))==3
        assert all(0<=i<len(vertices) for i in f)
        a,b,c=(vertices[i] for i in f)
        n=cross(sub(b,a),sub(c,a))
        assert dot(n,n)>1e-17,("degenerate",p["name"])
        volume+=dot(a,cross(b,c))/6
        for u,v in ((f[0],f[1]),(f[1],f[2]),(f[2],f[0])):
            edges[tuple(sorted((u,v)))]+=1
            winding[(u,v)]+=1
    assert all(n==2 for n in edges.values()),("non-manifold",p["name"])
    assert all(winding[(v,u)]==n for (u,v),n in winding.items()),("winding",p["name"])
    assert volume>1e-11,("negative-volume",p["name"],volume)
    assert all(all(math.isfinite(x) for x in v) for v in vertices)
    return volume

def validate(parts):
    names=set();triangles=0;vertices=0
    for p in parts:
        assert p["name"] not in names,p["name"]
        names.add(p["name"])
        assert p["material"] in MATERIALS
        check_mesh(p)
        triangles+=len(p["faces"]);vertices+=len(p["vertices"])
    assert len(MATERIALS)>=3
    assert sum(p["group"]=="OUTER" for p in parts)==10
    assert sum(p["group"]=="MIDDLE" for p in parts)==8
    assert sum(p["group"]=="INNER" for p in parts)==13
    bounds=[[float("inf"),-float("inf")] for _ in range(3)]
    for p in parts:
        for v in p["vertices"]:
            v=pose_vertex(p,v,0)
            for k in range(3):
                bounds[k][0]=min(bounds[k][0],v[k])
                bounds[k][1]=max(bounds[k][1],v[k])
    assert abs(bounds[2][0])<1e-9,("floor mounting plane",bounds)
    assert bounds[2][1]<.750
    assert bounds[0][0]>-.31 and bounds[0][1]<.31
    assert bounds[1][0]>-.31 and bounds[1][1]<.31
    # The radial-shell invariant proves that no ring group can intersect
    # another, for all angles (not merely sampled frames).
    radial={}
    for group in ("OUTER","MIDDLE","INNER"):
        r=[math.sqrt(dot(v,v)) for p in parts if p["group"]==group
           for v in p["vertices"]]
        radial[group]=(min(r),max(r))
    assert radial["OUTER"][0]-radial["MIDDLE"][1]>.004,radial
    assert radial["MIDDLE"][0]-radial["INNER"][1]>.004,radial
    # Conservative ALL-ANGLE cage certification: every moving vertex is
    # inside the same bounding sphere for all orthogonal rotations. The
    # minimum centre-to-AABB distance minus that sphere's outer radius
    # lower-bounds clearance for every frame, including unsampled poses.
    static_boxes=[p for p in parts if p["group"]=="STATIC"]
    fixed_bounds=[]
    for p in static_boxes:
        fixed_bounds.append([(min(v[k] for v in p["vertices"]),
                              max(v[k] for v in p["vertices"]))
                             for k in range(3)])
    centre=(0,0,CENTRE_Z)
    moving=[p for p in parts if p["group"]!="STATIC"]
    max_radius=max(math.sqrt(dot(v,v)) for p in moving for v in p["vertices"])
    clearance=min(
        math.sqrt(sum(max(b[k][0]-centre[k],0,
                          centre[k]-b[k][1])**2 for k in range(3)))-
        max_radius for b in fixed_bounds)
    assert clearance>.008,("cage swept-sphere clearance",clearance)
    # Independently sweep all 121 animated poses and check every moving
    # vertex retains its local radius under the complete nested hierarchy.
    checks=0
    for frame in range(FRAMES):
        t=frame/(FRAMES-1)
        for p in moving:
            for v in p["vertices"]:
                xyz=pose_vertex(p,v,t)
                dist=math.dist(xyz,centre)
                assert abs(dist-math.sqrt(dot(v,v)))<1e-8, (
                    "bad nested rotation",p["name"],frame,dist)
                checks+=1
    return dict(asset=MODEL,status="PASS",parts=len(parts),vertices=vertices,
                triangles=triangles,materials=len(MATERIALS),
                moving_groups={"OUTER":10,"MIDDLE":8,"INNER":13},
                pivot_m=[0,0,0],mount="inside chassis, bottom floor, +Z up",
                gyro_centre_m=[0,0,CENTRE_Z],
                axes=["outer local Z","middle local Y","inner local X"],
                max_yaw_deg=38,max_pitch_deg=28,roll_deg=360,
                animation_frames=FRAMES,fps=FPS,
                radial_shells_m={k:[round(x,6) for x in v]
                                 for k,v in radial.items()},
                bounds_rest_m=bounds,
                sampled_moving_vertex_checks=checks,
                certified_fixed_cage_boxes=len(fixed_bounds),
                min_cage_clearance_m=round(clearance,6),
                qa_scope="watertight winding + all-angle radial-shell separation + conservative swept sphere versus every fixed cage box + 121-frame vertex rotation check",
                blender="NOT RUN",unity="NOT RUN")

def export_obj(parts,folder):
    folder.mkdir(parents=True,exist_ok=True)
    mtl=MODEL+"_gyroscope.mtl";obj=MODEL+"_gyroscope.obj"
    mtl_lines=[]
    for name,(rgba,metal,rough,emission) in MATERIALS.items():
        mtl_lines.extend(("newmtl "+name,
            "Kd %.6f %.6f %.6f"%rgba[:3],
            "Ks %.4f %.4f %.4f"%(metal,metal,metal),
            "Ns %.3f"%((1-rough)*800),
            "Ke %.4f %.4f %.4f"%tuple(c*emission for c in rgba[:3]),
            "illum 2",""))
    (folder/mtl).write_text("\n".join(mtl_lines),encoding="utf-8")
    lines=["# Rest-pose OBJ; animation and pivot empties only in GLB",
           "mtllib "+mtl]
    offset=1
    for p in parts:
        lines.extend(("o "+p["name"],"usemtl "+p["material"]))
        lines.extend("v %.8f %.8f %.8f"%pose_vertex(p,v,0)
                     for v in p["vertices"])
        lines.extend("f %d %d %d"%(a+offset,b+offset,c+offset)
                     for a,b,c in p["faces"])
        offset+=len(p["vertices"])
    (folder/obj).write_text("\n".join(lines)+"\n",encoding="utf-8")
    return dict(backend="python-static",obj=obj,mtl=mtl,blender="NOT RUN",
                glb="NOT RUN")

def blender_export(parts,folder):
    import bpy
    # Namespaced cleanup, safe for reruns in an artist's existing .blend.
    old=bpy.data.collections.get(COLLECTION)
    if old:
        for ob in tuple(old.objects): bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(old)
    for mesh in tuple(bpy.data.meshes):
        if mesh.name.startswith(NS) and mesh.users==0:
            bpy.data.meshes.remove(mesh)
    for action in tuple(bpy.data.actions):
        if action.name.startswith(NS) and action.users==0:
            bpy.data.actions.remove(action)
    collection=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(collection)
    root=bpy.data.objects.new(NS+"FLOOR_MOUNT_ROOT",None)
    collection.objects.link(root)
    root.empty_display_type="PLAIN_AXES"
    root.empty_display_size=.075
    root["mount_plane"]="Z=0 bottom face"
    root["metres_per_unit"]=1.0
    pivots={}
    parent=root
    for group,axis in (("OUTER","Z"),("MIDDLE","Y"),("INNER","X")):
        pivot=bpy.data.objects.new(NS+group+"_"+axis+"_PIVOT",None)
        collection.objects.link(pivot)
        pivot.parent=parent
        pivot.location=(0,0,CENTRE_Z) if group=="OUTER" else (0,0,0)
        pivot.empty_display_type="ARROWS"
        pivot.empty_display_size=.080
        pivot["rotation_axis"]="LOCAL_"+axis
        pivots[group]=pivot
        parent=pivot
    mats={}
    for name,(rgba,metal,rough,emission) in MATERIALS.items():
        m=bpy.data.materials.get(NS+name) or bpy.data.materials.new(NS+name)
        m.use_nodes=True
        bsdf=m.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value=rgba
        bsdf.inputs["Metallic"].default_value=metal
        bsdf.inputs["Roughness"].default_value=rough
        if emission:
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value=rgba
            elif "Emission" in bsdf.inputs:
                bsdf.inputs["Emission"].default_value=rgba
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value=emission
        m.diffuse_color=rgba
        mats[name]=m
    for p in parts:
        mesh=bpy.data.meshes.new(NS+p["name"]+"_MESH")
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.update()
        obj=bpy.data.objects.new(NS+p["name"],mesh)
        collection.objects.link(obj)
        obj.parent=root if p["group"]=="STATIC" else pivots[p["group"]]
        mesh.materials.append(mats[p["material"]])
        for poly in mesh.polygons:
            poly.use_smooth=p["smooth"]
    scene=bpy.context.scene
    scene.frame_start=1;scene.frame_end=FRAMES;scene.render.fps=FPS
    for frame in range(1,FRAMES+1,10):
        yaw,pitch,roll=angles((frame-1)/(FRAMES-1))
        for group,axis,angle in (("OUTER",2,yaw),("MIDDLE",1,pitch),
                                 ("INNER",0,roll)):
            pivot=pivots[group]
            pivot.rotation_euler[axis]=angle
            pivot.keyframe_insert(data_path="rotation_euler",frame=frame,
                                  group="GYRO_"+group)
    scene.frame_set(1)
    bpy.context.view_layer.update()
    folder.mkdir(parents=True,exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for ob in collection.objects: ob.select_set(True)
    bpy.context.view_layer.objects.active=root
    blend=folder/(MODEL+"_gyroscope.blend")
    glb=folder/(MODEL+"_gyroscope.glb")
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
    ap.add_argument("--out",default="build/M04-009")
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
        export=export_obj(parts,folder)
    else:
        try: import bpy
        except ImportError: export=export_obj(parts,folder)
        else: export=blender_export(parts,folder)
    report.update(export)
    folder.mkdir(parents=True,exist_ok=True)
    (folder/(MODEL+"_manifest.json")).write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))

if __name__=="__main__":
    main()
