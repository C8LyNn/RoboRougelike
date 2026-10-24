#!/usr/bin/env python3
"""M04-002: coaxial counter-rotating side rotor for RoboRougelike.

Blender: blender -b --python generate_m04_002.py -- --out ./build/M04-002
CPython: python generate_m04_002.py --out ./build/M04-002

Both modes create deterministic OBJ/MTL and a manifest. Blender additionally
exports an animated GLB with two independently rotating 48-frame assemblies.
Visual-only asset; 1 unit = 1 m. Mount at X=0, outward +X; spin axis +X.
"""
from __future__ import annotations
import argparse
import json
import math
from pathlib import Path
import sys

try:
    import bpy
except ImportError:
    bpy = None

MODEL = "M04-002"
ROOT = "RRL_M04_002_ContraRotor"
TAU = 2 * math.pi
FRAMES = (1, 49)
ROTOR_AXIAL = (0.280, 0.520)
ROTOR_RADIAL_MAX = 0.345
RING_INNER = 0.386
AXIAL_BLADE_HALF_EXTENT = 0.025
MATERIALS = {
    "graphite": ((0.058, 0.073, 0.086, 1), 0.75, 0.35, 0),
    "titanium": ((0.34, 0.39, 0.42, 1), 0.82, 0.28, 0),
    "ceramic": ((0.78, 0.79, 0.74, 1), 0.12, 0.34, 0),
    "copper": ((0.58, 0.23, 0.10, 1), 0.8, 0.3, 0),
    "rubber": ((0.025, 0.035, 0.039, 1), 0.05, 0.84, 0),
    "cyan_emission": ((0.03, 0.68, 0.86, 1), 0.25, 0.28, 2.8),
    "amber_emission": ((0.92, 0.31, 0.035, 1), 0.20, 0.35, 1.8),
}
PARTS = []


def add(name, vertices, faces, material, rotor=None):
    assert vertices and faces and material in MATERIALS
    assert all(len(f) >= 3 and all(0 <= i < len(vertices) for i in f) for f in faces)
    PARTS.append((name, vertices, faces, material, rotor))


def box(name, center, size, mat, rotor=None):
    x,y,z=center; a,b,c=(v/2 for v in size)
    v=[(x-a,y-b,z-c),(x+a,y-b,z-c),(x+a,y+b,z-c),(x-a,y+b,z-c),
       (x-a,y-b,z+c),(x+a,y-b,z+c),(x+a,y+b,z+c),(x-a,y+b,z+c)]
    f=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    add(name,v,f,mat,rotor)


def cylinder_x(name, x, y, z, radius, length, mat, rotor=None, segments=48):
    v=[]
    for xx in (x-length/2,x+length/2):
        for j in range(segments):
            a=TAU*j/segments
            v.append((xx,y+radius*math.cos(a),z+radius*math.sin(a)))
    f=[tuple(reversed(range(segments))),tuple(range(segments,2*segments))]
    for j in range(segments):
        k=(j+1)%segments
        f.append((j,k,segments+k,segments+j))
    add(name,v,f,mat,rotor)


def torus_x(name,x,major,minor,mat,segments=72,tube_segments=12):
    v=[]
    for i in range(segments):
        a=TAU*i/segments
        for j in range(tube_segments):
            b=TAU*j/tube_segments
            r=major+minor*math.cos(b)
            v.append((x+minor*math.sin(b),r*math.cos(a),r*math.sin(a)))
    f=[]
    for i in range(segments):
        for j in range(tube_segments):
            a=i*tube_segments+j
            b=((i+1)%segments)*tube_segments+j
            c=((i+1)%segments)*tube_segments+(j+1)%tube_segments
            d=i*tube_segments+(j+1)%tube_segments
            f.append((a,b,c,d))
    add(name,v,f,mat)


def rod(name,p,q,radius,mat,segments=12):
    dx=[q[i]-p[i] for i in range(3)]
    norm=math.sqrt(sum(t*t for t in dx)); axis=[t/norm for t in dx]
    trial=(0,0,1) if abs(axis[2])<0.9 else (0,1,0)
    cross=lambda a,b:(a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
    u=cross(axis,trial); lu=math.sqrt(sum(t*t for t in u));u=[t/lu for t in u]
    w=cross(axis,u)
    v=[]
    for c in (p,q):
        for i in range(segments):
            a=TAU*i/segments
            v.append(tuple(c[k]+radius*(u[k]*math.cos(a)+w[k]*math.sin(a)) for k in range(3)))
    f=[tuple(reversed(range(segments))),tuple(range(segments,2*segments))]
    for i in range(segments):
        j=(i+1)%segments
        f.append((i,j,segments+j,segments+i))
    add(name,v,f,mat)


def blade(name,rotor_index,angle,mat):
    """Closed tapered, pitched blade, spinning around +X."""
    center=ROTOR_AXIAL[rotor_index]
    stations=((0.102,0.085,-0.005),(0.166,0.112,0.002),
              (0.249,0.087,0.011),(0.340,0.044,0.018))
    v=[]
    for r,chord,pitch in stations:
        for side,thickness in ((-1,-1),(1,-1),(-1,1),(1,1)):
            tangent=side*chord/2
            v.append((center+pitch+thickness*0.006,
                      r*math.cos(angle)-tangent*math.sin(angle),
                      r*math.sin(angle)+tangent*math.cos(angle)))
    f=[(0,1,3,2),(12,14,15,13)]
    for i in range(len(stations)-1):
        a=4*i;b=4*(i+1)
        f.extend(((a,b,b+1,a+1),(a+2,a+3,b+3,b+2),
                  (a,a+2,b+2,b),(a+1,b+1,b+3,a+3)))
    add(name,v,f,mat,rotor_index)


def build():
    assert ROTOR_RADIAL_MAX < RING_INNER-0.03
    assert ROTOR_AXIAL[1]-ROTOR_AXIAL[0] > 2*AXIAL_BLADE_HALF_EXTENT+0.10
    box("SideMount_160mm",(.027,0,0),(.054,.232,.232),"graphite")
    box("MountInset",(.059,0,0),(.012,.168,.168),"titanium")
    for sy in (-1,1):
        for sz in (-1,1):
            cylinder_x(f"Bolt_{sy}_{sz}",.067,sy*.088,sz*.088,.013,.012,"copper",segments=16)
    cylinder_x("StationaryAxle",.355,0,0,.038,.520,"titanium")
    cylinder_x("AxleCollarRear",.142,0,0,.073,.064,"graphite")
    cylinder_x("AxleCollarFront",.628,0,0,.072,.060,"graphite")
    for i in range(4):
        a=TAU*i/4+TAU/8
        y,z=math.cos(a),math.sin(a)
        rod(f"Support_{i}",(.105,y*.080,z*.080),(.191,y*.400,z*.400),.020,"titanium")
        cylinder_x(f"OuterRivet_{i}",.192,y*.400,z*.400,.025,.035,"copper",segments=16)
    for i,x in enumerate((.214,.404,.590)):
        torus_x(f"Guard_{i}",x,.414,.028,"graphite")
    torus_x("OuterCopperCrown",.405,.475,.024,"copper")
    torus_x("CyanHalo",.405,.461,.007,"cyan_emission")
    torus_x("InnerCeramicHalo",.405,.372,.012,"ceramic")
    torus_x("InnerSignal",.405,.358,.004,"amber_emission")
    for i in range(8):
        a=TAU*i/8
        y,z=math.cos(a)*.444,math.sin(a)*.444
        rod(f"AxialRib_{i}",(.228,y,z),(.576,y,z),.011,"titanium")
    for rotor in (0,1):
        x=ROTOR_AXIAL[rotor]
        cylinder_x(f"Hub_{rotor}",x,0,0,.091,.046,"copper" if rotor==0 else "ceramic",rotor)
        cylinder_x(f"HubRing_{rotor}",x+.027,0,0,.063,.010,"graphite",rotor)
        cylinder_x(f"RotorCore_{rotor}",x-.025,0,0,.045,.010,"titanium",rotor)
        count=(5,7)[rotor]
        for i in range(count):
            blade(f"Blade_{rotor}_{i:02d}",rotor,TAU*i/count+(0.0 if rotor==0 else TAU/14),
                  "titanium" if rotor==0 else "ceramic")
        for i in range(count):
            a=TAU*i/count
            y,z=.077*math.cos(a),.077*math.sin(a)
            cylinder_x(f"HubStud_{rotor}_{i}",x+.032,y,z,.010,.009,
                       "amber_emission" if rotor==0 else "cyan_emission",rotor,segments=12)
    return PARTS


def obj_mtl(out):
    out.mkdir(parents=True,exist_ok=True)
    obj=out/(MODEL+".obj"); mtl=out/(MODEL+".mtl")
    lines=[f"# {MODEL} / 1 unit = 1 meter / X=0 side mount",f"mtllib {MODEL}.mtl"]
    offset=0
    for name,verts,faces,mat,_ in PARTS:
        lines += [f"o {name}",f"usemtl {mat}"]
        lines += ["v %.6f %.6f %.6f" % v for v in verts]
        for f in faces:
            lines.append("f "+" ".join(str(i+offset+1) for i in f))
        offset+=len(verts)
    obj.write_text("\n".join(lines)+"\n",encoding="utf-8")
    lines=[]
    for name,(rgba,metal,rough,em) in MATERIALS.items():
        lines.extend([f"newmtl {name}","Kd %.5f %.5f %.5f" % rgba[:3],
                      "Ks %.5f %.5f %.5f" % ((metal,)*3),
                      "Ns %.2f" % ((1-rough)*100),"illum 2",""])
    mtl.write_text("\n".join(lines),encoding="utf-8")
    return obj,mtl


def export_blender(out):
    if bpy is None: return None
    scene=bpy.context.scene
    scene.unit_settings.system="METRIC";scene.unit_settings.scale_length=1.0
    scene.frame_start=FRAMES[0];scene.frame_end=FRAMES[1]-1;scene.render.fps=24
    old=bpy.data.collections.get(ROOT)
    if old:
        for ob in list(old.objects):bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(old)
    col=bpy.data.collections.new(ROOT);scene.collection.children.link(col)
    mats={}
    for name,(rgba,metal,rough,em) in MATERIALS.items():
        mat=bpy.data.materials.get(ROOT+"_"+name) or bpy.data.materials.new(ROOT+"_"+name)
        mat.use_nodes=True
        p=mat.node_tree.nodes.get("Principled BSDF")
        p.inputs["Base Color"].default_value=rgba
        p.inputs["Metallic"].default_value=metal
        p.inputs["Roughness"].default_value=rough
        if em:
            socket=p.inputs.get("Emission Color") or p.inputs.get("Emission")
            if socket:socket.default_value=rgba
            socket=p.inputs.get("Emission Strength")
            if socket:socket.default_value=em
        mat.diffuse_color=rgba;mats[name]=mat
    pivots=[]
    for i,x in enumerate(ROTOR_AXIAL):
        pivot=bpy.data.objects.new(f"Spin_{i}_{'CW' if i==0 else 'CCW'}",None)
        pivot.location=(x,0,0);pivot.empty_display_type="CIRCLE";pivot.empty_display_size=.09
        col.objects.link(pivot);pivots.append(pivot)
    for name,verts,faces,mat,rotor in PARTS:
        mesh=bpy.data.meshes.new(name+"_mesh");mesh.from_pydata(verts,[],faces);mesh.update()
        ob=bpy.data.objects.new(name,mesh);col.objects.link(ob);ob.data.materials.append(mats[mat])
        if rotor is not None:
            ob.parent=pivots[rotor]
            ob.matrix_parent_inverse=pivots[rotor].matrix_world.inverted()
        for poly in mesh.polygons:
            poly.use_smooth=len(poly.vertices)==4 and ("Guard" in name or "Halo" in name or "Hub" in name)
    for i,p in enumerate(pivots):
        p.rotation_mode="XYZ"
        for frame,turn in ((FRAMES[0],0),(FRAMES[1],TAU*(1 if i==0 else -1))):
            p.rotation_euler[0]=turn;p.keyframe_insert(data_path="rotation_euler",index=0,frame=frame)
        if p.animation_data and p.animation_data.action:
            action=p.animation_data.action
            curves=getattr(action,"fcurves",None)
            if (curves is None or len(curves)==0) and hasattr(action,"layers") and len(action.layers):
                strips=action.layers[0].strips
                if len(strips) and hasattr(strips[0],"channelbags") and len(strips[0].channelbags):
                    curves=strips[0].channelbags[0].fcurves
            if curves is not None:
                for fc in curves:
                    for kp in fc.keyframe_points:kp.interpolation="LINEAR"
    scene.frame_set(FRAMES[0])
    bpy.ops.object.select_all(action="DESELECT")
    for ob in col.objects:
        ob.select_set(True)
    bpy.context.view_layer.objects.active=next(ob for ob in col.objects if ob.type=="MESH")
    bpy.ops.export_scene.gltf(filepath=str(out/(MODEL+".glb")),export_format="GLB",
                              use_selection=True, export_animations=True)
    return str(out/(MODEL+".glb"))


def main():
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser();parser.add_argument("--out",default="./build/M04-002")
    ns=parser.parse_args(args);out=Path(ns.out).resolve()
    build();obj,mtl=obj_mtl(out);glb=export_blender(out)
    report={"id":MODEL,"name":"Wirnik podwójny przeciwsobny",
            "source":"procedural deterministic geometry","parts":len(PARTS),
            "vertices":sum(len(x[1]) for x in PARTS),
            "faces":sum(len(x[2]) for x in PARTS),
            "materials":list(MATERIALS),"axis_blender":"+X",
            "mount_plane_x_m":0,"rotor_centers_x_m":list(ROTOR_AXIAL),
            "rotation_1":"+360 degrees / 48 frames","rotation_2":"-360 degrees / 48 frames",
            "minimum_nominal_radial_clearance_m":round(RING_INNER-ROTOR_RADIAL_MAX,4),
            "minimum_nominal_axial_clearance_m":round(ROTOR_AXIAL[1]-ROTOR_AXIAL[0]-2*AXIAL_BLADE_HALF_EXTENT,4),
            "obj":obj.name,"mtl":mtl.name,"glb":Path(glb).name if glb else None,
            "blender_executed":bpy is not None,"unity_import_tested":False,
            "note":"Clearances are conservative design bounds; full dynamic collision QA pending Blender/Unity."}
    (out/(MODEL+".manifest.json")).write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))


if __name__=="__main__":main()
