#!/usr/bin/env python3
"""M04-012: rear correction aileron, armoured ribs and single X-axis hinge.

Unity scale 1 Blender unit = 1 metre, Blender Z-up, -Y points aft.
Mount plane Y=0, pivot at (0,-0.370,0.180); no gameplay changes.
Run:
  python3 generate_m04_012.py --self-test
  python3 generate_m04_012.py --static-obj --out build/M04-012
  blender -b --factory-startup --python-exit-code 1 --python generate_m04_012.py -- --out build/M04-012
Idempotent: only the dedicated M04-012 collection is replaced.
"""
from __future__ import annotations
import argparse
from collections import Counter
import json
import math
from pathlib import Path
import sys

ID="M04-012"
PREFIX="RRL_M04_012_"
COLLECTION=PREFIX+"REAR_CORRECTION_AILERON"
HINGE=(0.0,-.370,.180)
ANGLES=(-16.0,20.0)
FRAMES=121
FPS=30
MATERIALS={
    "Graphite_Armor":((.055,.073,.092,1),.38,.38,0),
    "Brushed_Titanium":((.49,.56,.63,1),.83,.28,0),
    "Oxide_Copper":((.64,.29,.10,1),.70,.33,0),
    "White_Ceramic":((.83,.85,.77,1),.16,.43,0),
    "Black_Elastomer":((.024,.029,.035,1),.04,.82,0),
    "Cyan_Status":((.035,.64,.91,1),.15,.32,2.1),
    "Amber_Limit":((.98,.39,.045,1),.21,.34,1.8),
}

def part(name,vertices,faces,material,group="FIXED",smooth=False):
    return dict(name=name,vertices=vertices,faces=faces,
                material=material,group=group,smooth=smooth)

def extrude(name,polygon,z0,z1,material,group="FIXED"):
    assert z1>z0 and len(polygon)>=3
    area=sum(polygon[i][0]*polygon[(i+1)%len(polygon)][1]-
             polygon[(i+1)%len(polygon)][0]*polygon[i][1]
             for i in range(len(polygon)))
    if area<0:polygon=list(reversed(polygon))
    n=len(polygon)
    v=[(x,y,z0) for x,y in polygon]+[(x,y,z1) for x,y in polygon]
    f=[]
    for i in range(1,n-1):
        f.extend(((0,i+1,i),(n,n+i,n+i+1)))
    for i in range(n):
        j=(i+1)%n
        f.extend(((i,j,n+j),(i,n+j,n+i)))
    return part(name,v,f,material,group)

def box(name,center,size,material,group="FIXED",bevel=0):
    x,y,z=center;dx,dy,dz=[s/2 for s in size]
    assert min(size)>0
    bevel=min(bevel,dx*.6,dy*.6)
    if bevel:
        poly=[(x-dx+bevel,y-dy),(x+dx-bevel,y-dy),
              (x+dx,y-dy+bevel),(x+dx,y+dy-bevel),
              (x+dx-bevel,y+dy),(x-dx+bevel,y+dy),
              (x-dx,y+dy-bevel),(x-dx,y-dy+bevel)]
    else:
        poly=[(x-dx,y-dy),(x+dx,y-dy),
              (x+dx,y+dy),(x-dx,y+dy)]
    return extrude(name,poly,z-dz,z+dz,material,group)

def cylinder_x(name,center,radius,length,material,group="FIXED",segments=32):
    assert radius>0 and length>0
    x,y,z=center
    v=[]
    for xx in (x-length/2,x+length/2):
        v.extend((xx,y+radius*math.cos(2*math.pi*i/segments),
                  z+radius*math.sin(2*math.pi*i/segments))
                 for i in range(segments))
    v.extend(((x-length/2,y,z),(x+length/2,y,z)))
    f=[]
    for i in range(segments):
        j=(i+1)%segments
        f.extend(((i,j,segments+j),(i,segments+j,segments+i),
                  (2*segments,j,i),
                  (2*segments+1,segments+i,segments+j)))
    return part(name,v,f,material,group,True)

def tube_x(name,center,outer,inner,length,material,segments=32):
    assert outer>inner>0 and length>0
    x,y,z=center;v=[]
    for radius,xx in ((outer,x-length/2),(outer,x+length/2),
                      (inner,x-length/2),(inner,x+length/2)):
        v.extend((xx,y+radius*math.cos(2*math.pi*i/segments),
                  z+radius*math.sin(2*math.pi*i/segments))
                 for i in range(segments))
    f=[]
    for i in range(segments):
        j=(i+1)%segments
        f.extend(((i,j,segments+j),(i,segments+j,segments+i),
                  (2*segments+i,3*segments+j,2*segments+j),
                  (2*segments+i,3*segments+i,3*segments+j),
                  (i,2*segments+i,2*segments+j),
                  (i,2*segments+j,j),
                  (segments+i,segments+j,3*segments+j),
                  (segments+i,3*segments+j,3*segments+i)))
    return part(name,v,f,material,"FIXED",True)

def to_local(p):
    assert p["group"]=="AILERON"
    hx,hy,hz=HINGE
    p["vertices"]=[(x-hx,y-hy,z-hz) for x,y,z in p["vertices"]]
    return p

def build():
    p=[];add=p.append
    add(box("REAR_SOCKET_PLATE",(0,-.025,0),(1.080,.050,.220),
            "Brushed_Titanium",bevel=.018))
    add(box("REAR_GASKET",(0,-.054,0),(.990,.009,.175),
            "Black_Elastomer",bevel=.008))
    add(box("ARMOUR_SPINE",(0,-.098,.038),(.940,.082,.118),
            "Graphite_Armor",bevel=.012))
    add(box("SPINE_COPPER_INLAY",(0,-.142,.046),(.820,.008,.019),
            "Oxide_Copper",bevel=.003))
    add(box("CROSSBAR",(0,-.205,.128),(.875,.041,.040),
            "Brushed_Titanium",bevel=.007))
    for side in (-1,1):
        tag="L" if side<0 else "R"
        add(box(tag+"_OUTRIGGER",(side*.515,-.228,.134),
                (.053,.290,.065),"Graphite_Armor",bevel=.008))
        add(box(tag+"_OUTRIGGER_COPPER",(side*.514,-.229,.169),
                (.015,.224,.009),"Oxide_Copper",bevel=.004))
        add(box(tag+"_INNER_STIFFENER",(side*.484,-.210,.135),
                (.013,.206,.035),"Brushed_Titanium",bevel=.003))
        add(tube_x(tag+"_HOLLOW_BEARING",(side*.492,HINGE[1],HINGE[2]),
                   .038,.022,.032,"Brushed_Titanium"))
        add(tube_x(tag+"_BEARING_RING",(side*.507,HINGE[1],HINGE[2]),
                   .042,.038,.008,"Oxide_Copper"))
        add(box(tag+"_AMBER_ENDSTOP",(side*.521,-.345,.177),
                (.018,.025,.013),"Amber_Limit",bevel=.003))
    for x in (-.30,0,.30):
        add(box("REAR_SPINE_VENT_"+str(x),(x,-.148,.103),
                (.135,.013,.008),"White_Ceramic",bevel=.003))
    moving=[]
    def mov(item):moving.append(item)
    mov(cylinder_x("ROTATING_AXLE",HINGE,.016,.940,
                   "Brushed_Titanium","AILERON"))
    wing=[
        ("CENTER_WING",[(-.160,-.055),(.160,-.055),
                        (.160,-.500),(-.160,-.500)]),
        ("LEFT_WING",[(-.160,-.055),(-.415,-.087),
                      (-.460,-.360),(-.377,-.490),(-.160,-.500)]),
        ("RIGHT_WING",[(.160,-.055),(.415,-.087),
                       (.460,-.360),(.377,-.490),(.160,-.500)]),
    ]
    for name,poly in wing:
        polygon=[(x,y+HINGE[1]) for x,y in poly]
        mov(extrude(name,polygon,HINGE[2]-.012,HINGE[2]+.012,
                    "Graphite_Armor","AILERON"))
    def mb(name,center,size,material,bevel=0):
        mov(box(name,(center[0],center[1]+HINGE[1],center[2]+HINGE[2]),
                size,material,"AILERON",bevel))
    mb("ROOT_SPAR",(0,-.075,.018),(.840,.030,.022),
       "Brushed_Titanium",.006)
    mb("ROOT_COPPER_TRACE",(0,-.079,.031),(.730,.011,.009),
       "Oxide_Copper",.003)
    mb("CENTER_SPINE",(0,-.270,.020),(.038,.350,.018),
       "Brushed_Titanium",.006)
    mb("CENTER_SPINE_COPPER",(0,-.270,.032),(.014,.300,.007),
       "Oxide_Copper",.002)
    for side in (-1,1):
        tag="L" if side<0 else "R"
        for i,x in enumerate((.205,.300,.378)):
            mb(tag+"_ARMOUR_RIB_"+str(i),(side*x,-.254,.021),
               (.016,.258,.018),"Brushed_Titanium",.004)
        mb(tag+"_TIP_CERAMIC",(side*.412,-.374,.022),
           (.052,.112,.012),"White_Ceramic",.008)
        mb(tag+"_CYAN_TRAIL",(side*.293,-.447,.029),
           (.125,.011,.009),"Cyan_Status",.003)
        mb(tag+"_TRAILING_ARMOUR",(side*.238,-.484,.016),
           (.120,.014,.017),"Brushed_Titanium",.004)
        mb(tag+"_ROOT_FASTENER",(side*.351,-.102,.034),
           (.026,.025,.008),"Oxide_Copper",.003)
    mb("CENTER_TRAILING_CERAMIC",(0,-.494,.017),
       (.260,.012,.012),"White_Ceramic",.004)
    mb("CENTER_TRAILING_GLOW",(0,-.499,.026),
       (.205,.006,.006),"Cyan_Status",.002)
    mb("UNDERSIDE_RIB",(0,-.260,-.023),
       (.025,.320,.017),"Brushed_Titanium",.004)
    for item in moving:add(to_local(item))
    return p

def sub(a,b):return tuple(x-y for x,y in zip(a,b))
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def dot(a,b):return sum(x*y for x,y in zip(a,b))

def mesh_check(p):
    v=p["vertices"];edges=Counter();direct=Counter();volume=0
    assert v and p["faces"]
    for f in p["faces"]:
        assert len(f)==3 and len(set(f))==3 and all(0<=i<len(v) for i in f)
        a,b,c=(v[i] for i in f)
        assert dot(cross(sub(b,a),sub(c,a)),cross(sub(b,a),sub(c,a)))>1e-18,p["name"]
        volume+=dot(a,cross(b,c))/6
        for i,j in ((f[0],f[1]),(f[1],f[2]),(f[2],f[0])):
            edges[tuple(sorted((i,j)))]+=1;direct[(i,j)]+=1
    assert all(n==2 for n in edges.values()),("nonmanifold",p["name"])
    assert all(direct[(j,i)]==n for (i,j),n in direct.items()),("winding",p["name"])
    assert volume>1e-11,("volume",p["name"],volume)
    return volume

def pose(p,angle):
    if p["group"]=="FIXED":return p["vertices"]
    a=math.radians(angle);c=math.cos(a);s=math.sin(a)
    hx,hy,hz=HINGE
    return [(hx+x,hy+c*y-s*z,hz+s*y+c*z) for x,y,z in p["vertices"]]

def bounds(v):
    return [(min(p[i] for p in v),max(p[i] for p in v)) for i in range(3)]
def sep(a,b):
    return max(max(a[k][0]-b[k][1],b[k][0]-a[k][1],0.) for k in range(3))

def validate(parts):
    names=set();vertices=0;triangles=0
    for p in parts:
        assert p["name"] not in names,p["name"]
        names.add(p["name"])
        assert p["material"] in MATERIALS
        assert p["group"] in ("FIXED","AILERON")
        assert all(math.isfinite(q) for v in p["vertices"] for q in v)
        mesh_check(p)
        vertices+=len(p["vertices"]);triangles+=len(p["faces"])
    fixed=[p for p in parts if p["group"]=="FIXED"]
    moving=[p for p in parts if p["group"]=="AILERON" and p["name"]!="ROTATING_AXLE"]
    assert len(MATERIALS)>=3 and len(moving)>=20
    pair_checks=0;minimum=float("inf")
    for i in range(73):
        angle=ANGLES[0]+(ANGLES[1]-ANGLES[0])*i/72
        for p in moving:
            a=bounds(pose(p,angle))
            for q in fixed:
                # Bearings are coaxial to the moving shaft, with explicit
                # 6mm radial bore clearance tested separately.
                if "BEARING" in q["name"]:continue
                d=sep(a,bounds(q["vertices"]))
                assert d>=.004-1e-9,("fixed collision",p["name"],q["name"],i,d)
                minimum=min(minimum,d);pair_checks+=1
    # Bore radius 22mm, axle radius 16mm; radial play 6mm.
    shaft=next(p for p in parts if p["name"]=="ROTATING_AXLE")
    axle_radius=max(math.hypot(y,z) for x,y,z in shaft["vertices"])
    assert abs(axle_radius-.016)<1e-8
    for side in ("L","R"):
        bearing=next(p for p in parts if p["name"]==side+"_HOLLOW_BEARING")
        radial=[math.hypot(y-HINGE[1],z-HINGE[2])
                for x,y,z in bearing["vertices"]]
        assert abs(min(radial)-.022)<1e-8
        assert min(radial)-axle_radius>=.00599
    rest=[v for p in parts for v in pose(p,0)]
    bb=bounds(rest)
    assert abs(bb[1][1])<1e-8,("rear mount Y=0",bb)
    assert bb[1][0]<-.85 and bb[1][0]>-1.0
    assert bb[0][0]>-.60 and bb[0][1]<.60
    return dict(id=ID,status="PASS",parts=len(parts),vertices=vertices,
                triangles=triangles,materials=len(MATERIALS),
                mount_plane="Y=0 rear socket; -Y aft",
                hinge_pivot_m=list(HINGE),motion_axis="local X",
                motion_range_deg=list(ANGLES),pose_samples=73,
                fixed_pair_checks=pair_checks,
                min_conservative_aabb_clearance_m=round(minimum,6),
                shaft_bearing_radial_clearance_m=.006,
                rest_bounds_m=bb,unit="1 Blender unit = 1 metre",
                blender="NOT RUN",unity="NOT RUN",
                collision_scope="AABB moving vs fixed non-bearing parts at 73 angles, plus radial bore/shaft geometry; excludes intentional same-assembly intersections and does not substitute Unity physics")

def static_export(parts,out,report):
    out.mkdir(parents=True,exist_ok=True)
    obj=out/(ID+"_rear_correction_aileron.obj")
    mtl=out/(ID+"_rear_correction_aileron.mtl")
    ml=[]
    for name,(color,metal,rough,emission) in MATERIALS.items():
        ml.extend((f"newmtl {name}",f"Kd {color[0]:.6f} {color[1]:.6f} {color[2]:.6f}",
                   f"Ks {metal:.6f} {metal:.6f} {metal:.6f}",
                   f"Ns {max(1,(1-rough)*800):.2f}",
                   f"Ke {color[0]*emission:.6f} {color[1]*emission:.6f} {color[2]*emission:.6f}",""))
    mtl.write_text("\n".join(ml)+"\n",encoding="utf8")
    lines=[f"mtllib {mtl.name}","# rear mount Y=0, -Y aft, rest pose"]
    offset=0
    for p in parts:
        lines.extend(("o "+p["name"],"usemtl "+p["material"]))
        for x,y,z in pose(p,0):lines.append(f"v {x:.8f} {y:.8f} {z:.8f}")
        for a,b,c in p["faces"]:lines.append(f"f {a+offset+1} {b+offset+1} {c+offset+1}")
        offset+=len(p["vertices"])
    obj.write_text("\n".join(lines)+"\n",encoding="utf8")
    (out/(ID+"_static_qa.json")).write_text(json.dumps(report,indent=2)+"\n",encoding="utf8")

def blender_export(parts,out):
    import bpy
    old=bpy.data.collections.get(COLLECTION)
    if old:
        for obj in tuple(old.objects):bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(old)
    col=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(col)
    scene=bpy.context.scene
    scene.unit_settings.system="METRIC";scene.unit_settings.scale_length=1
    scene.render.fps=FPS;scene.frame_start=1;scene.frame_end=FRAMES
    materials={}
    for name,(rgba,metal,rough,emission) in MATERIALS.items():
        m=bpy.data.materials.get(PREFIX+name) or bpy.data.materials.new(PREFIX+name)
        m.diffuse_color=rgba;m.use_nodes=True
        bsdf=m.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value=rgba
        bsdf.inputs["Metallic"].default_value=metal
        bsdf.inputs["Roughness"].default_value=rough
        bsdf.inputs["Emission Color"].default_value=rgba
        bsdf.inputs["Emission Strength"].default_value=emission
        materials[name]=m
    pivot=bpy.data.objects.new(PREFIX+"AILERON_HINGE_X",None)
    pivot.empty_display_type="ARROWS";pivot.empty_display_size=.07
    pivot.location=HINGE
    col.objects.link(pivot)
    for p in parts:
        mesh=bpy.data.meshes.new(PREFIX+p["name"]+"_MESH")
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.update()
        obj=bpy.data.objects.new(PREFIX+p["name"],mesh)
        col.objects.link(obj)
        mesh.materials.append(materials[p["material"]])
        if p["group"]=="AILERON":
            obj.parent=pivot
            obj.matrix_parent_inverse.identity()
        if p["smooth"]:
            for face in mesh.polygons:
                face.use_smooth=abs(face.normal.x)<.90
    pivot.rotation_mode="XYZ"
    for frame,deg in ((1,0),(22,12),(42,20),(62,0),(82,-16),(102,-8),(121,0)):
        pivot.rotation_euler=(math.radians(deg),0,0)
        pivot.keyframe_insert(data_path="rotation_euler",frame=frame,group="CORRECTION")
    if pivot.animation_data and pivot.animation_data.action:
        pivot.animation_data.action.name=PREFIX+"AILERON_CORRECTION"
        action=pivot.animation_data.action
        if hasattr(action,"fcurves"):
            for curve in action.fcurves:
                for key in curve.keyframe_points:key.interpolation="BEZIER"
    scene.frame_set(1)
    out.mkdir(parents=True,exist_ok=True)
    blend=out/(ID+"_rear_correction_aileron.blend")
    glb=out/(ID+"_rear_correction_aileron.glb")
    prev_selected=tuple(bpy.context.selected_objects)
    prev_active=bpy.context.view_layer.objects.active
    try:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in col.objects:ob.select_set(True)
        bpy.context.view_layer.objects.active=pivot
        bpy.ops.wm.save_as_mainfile(filepath=str(blend))
        bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
                                  use_selection=True,export_animations=True)
        assert blend.exists() and glb.exists()
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in prev_selected:
            if bpy.data.objects.get(ob.name) is ob:ob.select_set(True)
        if prev_active and bpy.data.objects.get(prev_active.name) is prev_active:
            bpy.context.view_layer.objects.active=prev_active
    return dict(blender="PASS",blend=str(blend),glb=str(glb))

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--self-test",action="store_true")
    ap.add_argument("--static-obj",action="store_true")
    ap.add_argument("--out",type=Path,default=Path("build/M04-012"))
    args=ap.parse_args(sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:])
    parts=build();report=validate(parts)
    if args.self_test:
        print(json.dumps(report,indent=2));return
    static_export(parts,args.out,report)
    if not args.static_obj:
        report.update(blender_export(parts,args.out))
        (args.out/(ID+"_blender_qa.json")).write_text(json.dumps(report,indent=2)+"\n",encoding="utf8")
    print(json.dumps(report,indent=2))
if __name__=="__main__":main()
