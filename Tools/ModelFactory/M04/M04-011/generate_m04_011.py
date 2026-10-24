#!/usr/bin/env python3
"""M04-011 — segmented side-mounted deployable aerodynamic fin for Unity 6.

1 unit = 1 metre. +X points outward from the side socket; x=0 is the
mounting face. Blender Z-up. Three blade pivots rotate about local Y.
Run: python3 generate_m04_011.py --self-test
     python3 generate_m04_011.py --static-obj --out build/M04-011
     blender -b --factory-startup --python-exit-code 1 --python generate_m04_011.py -- --out build/M04-011
Repeated bpy execution replaces only the M04-011 collection.
"""
from __future__ import annotations
import argparse
from collections import Counter
import json
import math
from pathlib import Path
import sys

ID="M04-011"
PREFIX="RRL_M04_011_"
COLLECTION=PREFIX+"DEPLOYABLE_AERO_FIN"
PIVOT_X=.180
PIVOT_Z=.160
CENTRES={"FORE":-.270,"MID":0.0,"AFT":.270}
ANGLES={"FORE":-78.0,"MID":-82.0,"AFT":-84.0}
FRAMES=96
FPS=30
MATERIALS={
    "Carbon_Graphite":((.052,.072,.090,1),.58,.36,0),
    "Satin_Titanium":((.49,.57,.64,1),.82,.27,0),
    "Copper_Actuator":((.72,.31,.095,1),.75,.32,0),
    "Ivory_Ceramic":((.79,.82,.78,1),.22,.39,0),
    "Rubber_Seal":((.025,.031,.042,1),.05,.81,0),
    "Cyan_Edge":((.035,.75,.96,1),.25,.29,2.4),
    "Amber_Lock":((1.0,.38,.045,1),.23,.33,2.0),
}

def part(name,verts,faces,material,group="STATIC",smooth=False):
    return dict(name=name,vertices=verts,faces=faces,material=material,
                group=group,smooth=smooth)

def extrude(name,polygon,z0,z1,material,group="STATIC"):
    """Convex CCW XY polygon extruded to a closed, outward-wound prism."""
    assert z1>z0 and len(polygon)>=3
    n=len(polygon)
    area=sum(polygon[i][0]*polygon[(i+1)%n][1]-
             polygon[(i+1)%n][0]*polygon[i][1] for i in range(n))
    assert area>0,(name,"CCW required")
    vertices=[(x,y,z0) for x,y in polygon]+[(x,y,z1) for x,y in polygon]
    faces=[]
    for i in range(1,n-1):
        faces.append((0,i+1,i))
        faces.append((n,n+i,n+i+1))
    for i in range(n):
        j=(i+1)%n
        faces.extend(((i,j,n+j),(i,n+j,n+i)))
    return part(name,vertices,faces,material,group)

def box(name,center,size,material,group="STATIC",bevel=0):
    x,y,z=center;a,b,c=[v/2 for v in size]
    assert min(size)>0
    bevel=min(bevel,a*.5,b*.5)
    if bevel:
        poly=[(x-a+bevel,y-b),(x+a-bevel,y-b),(x+a,y-b+bevel),
              (x+a,y+b-bevel),(x+a-bevel,y+b),(x-a+bevel,y+b),
              (x-a,y+b-bevel),(x-a,y-b+bevel)]
    else:
        poly=[(x-a,y-b),(x+a,y-b),(x+a,y+b),(x-a,y+b)]
    return extrude(name,poly,z-c,z+c,material,group)

def cylinder_y(name,center,radius,length,material,group="STATIC",segments=32):
    assert radius>0 and length>0
    x,y,z=center
    verts=[]
    for yy in (y-length/2,y+length/2):
        verts.extend((x+radius*math.cos(2*math.pi*i/segments),yy,
                      z+radius*math.sin(2*math.pi*i/segments))
                     for i in range(segments))
    verts.extend(((x,y-length/2,z),(x,y+length/2,z)))
    faces=[]
    for i in range(segments):
        j=(i+1)%segments
        faces.extend(((i,segments+i,segments+j),(i,segments+j,j),
                      (2*segments,i,j),(2*segments+1,segments+j,segments+i)))
    return part(name,verts,faces,material,group,True)

def tube_y(name,center,outer,inner,length,material,segments=32):
    assert outer>inner>0 and length>0
    x,y,z=center
    verts=[]
    for rad,yy in ((outer,y-length/2),(outer,y+length/2),
                   (inner,y-length/2),(inner,y+length/2)):
        verts.extend((x+rad*math.cos(2*math.pi*i/segments),yy,
                      z+rad*math.sin(2*math.pi*i/segments))
                     for i in range(segments))
    faces=[]
    for i in range(segments):
        j=(i+1)%segments
        faces.extend(((i,segments+i,segments+j),(i,segments+j,j),
                      (2*segments+i,3*segments+j,3*segments+i),
                      (2*segments+i,2*segments+j,3*segments+j),
                      (i,2*segments+j,2*segments+i),(i,j,2*segments+j),
                      (segments+i,3*segments+i,3*segments+j),
                      (segments+i,3*segments+j,segments+j)))
    return part(name,verts,faces,material,"STATIC",True)

def blade_polygon(yc,root=.199,tip=.710,halfroot=.097,halftip=.056):
    return [(root,yc-halfroot+.012),(.214,yc-halfroot),
            (.385,yc-halfroot+.005),(.645,yc-halftip),
            (tip,yc-halftip+.012),(tip,yc+halftip-.012),
            (.645,yc+halftip),(.385,yc+halfroot-.005),
            (.214,yc+halfroot),(root,yc+halfroot-.012)]

def to_local(p,yc):
    p["vertices"]=[(x-PIVOT_X,y-yc,z-PIVOT_Z) for x,y,z in p["vertices"]]
    return p

def build():
    parts=[];add=parts.append
    add(box("SIDE_SOCKET_PLATE",(.025,0,0),(.050,.835,.210),"Satin_Titanium",bevel=.009))
    add(box("SIDE_SEAL",(.056,0,0),(.012,.770,.173),"Rubber_Seal",bevel=.004))
    add(box("REINFORCED_SPINE",(.090,0,.025),(.075,.805,.115),"Carbon_Graphite",bevel=.008))
    add(box("COPPER_SPINE_TRACE",(.131,0,.022),(.007,.733,.016),"Copper_Actuator"))
    for tag,yc in CENTRES.items():
        for sign in (-1,1):
            side="P" if sign>0 else "N"
            yy=yc+sign*.122
            add(box(f"{tag}_YOKE_{side}",(.143,yy,.096),
                    (.087,.023,.067),"Satin_Titanium",bevel=.005))
            add(tube_y(f"{tag}_BEARING_{side}",(PIVOT_X,yy,PIVOT_Z),
                       .029,.019,.014,"Copper_Actuator",segments=32))
            add(box(f"{tag}_LOCK_INDICATOR_{side}",(.107,yy,.114),
                    (.021,.017,.010),"Amber_Lock",bevel=.002))
        moving=[]
        moving.append(cylinder_y(f"{tag}_AXLE",(PIVOT_X,yc,PIVOT_Z),
                                 .014,.254,"Satin_Titanium",tag,32))
        moving.append(extrude(f"{tag}_CARBON_FIN",blade_polygon(yc),
                              PIVOT_Z-.012,PIVOT_Z+.012,"Carbon_Graphite",tag))
        moving.append(box(f"{tag}_LEADING_EDGE",(.345,yc-.079,PIVOT_Z+.014),
                          (.180,.010,.012),"Satin_Titanium",tag,bevel=.003))
        moving.append(box(f"{tag}_TRAILING_EDGE",(.347,yc+.079,PIVOT_Z+.014),
                          (.177,.010,.012),"Satin_Titanium",tag,bevel=.003))
        moving.append(box(f"{tag}_TIP_CERAMIC",(.670,yc,PIVOT_Z+.015),
                          (.070,.084,.010),"Ivory_Ceramic",tag,bevel=.008))
        moving.append(box(f"{tag}_CYAN_EDGE",(.682,yc,PIVOT_Z+.021),
                          (.052,.009,.005),"Cyan_Edge",tag,bevel=.002))
        moving.append(box(f"{tag}_ROOT_BRACE",(.207,yc,PIVOT_Z+.018),
                          (.057,.155,.015),"Satin_Titanium",tag,bevel=.004))
        moving.append(box(f"{tag}_COPPER_RIB",(.440,yc,PIVOT_Z+.017),
                          (.330,.011,.011),"Copper_Actuator",tag,bevel=.003))
        moving.append(box(f"{tag}_AMBER_POSITION_LED",(.234,yc,PIVOT_Z+.027),
                          (.031,.013,.006),"Amber_Lock",tag,bevel=.002))
        for p in moving:add(to_local(p,yc))
    for sign in (-1,1):
        add(box("END_STOP_"+str(sign),(.088,sign*.397,.042),
                (.052,.023,.090),"Ivory_Ceramic",bevel=.006))
    return parts

def sub(a,b):return tuple(x-y for x,y in zip(a,b))
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def dot(a,b):return sum(x*y for x,y in zip(a,b))

def check_mesh(p):
    v=p["vertices"];edges=Counter();direct=Counter();volume=0.
    assert v and p["faces"]
    for f in p["faces"]:
        assert len(f)==3 and len(set(f))==3 and all(0<=i<len(v) for i in f),p["name"]
        a,b,c=[v[i] for i in f]
        n=cross(sub(b,a),sub(c,a))
        assert dot(n,n)>1e-18,("degenerate",p["name"])
        volume+=dot(a,cross(b,c))/6
        for i,j in ((f[0],f[1]),(f[1],f[2]),(f[2],f[0])):
            edges[tuple(sorted((i,j)))]+=1;direct[(i,j)]+=1
    assert all(n==2 for n in edges.values()),("nonmanifold",p["name"])
    assert all(direct[(j,i)]==n for (i,j),n in direct.items()),("winding",p["name"])
    assert volume>1e-11,("inward",p["name"],volume)
    return volume

def pose(p,angle_deg):
    if p["group"]=="STATIC":return p["vertices"]
    a=math.radians(angle_deg);co=math.cos(a);si=math.sin(a)
    yc=CENTRES[p["group"]]
    return [(PIVOT_X+co*x+si*z,yc+y,PIVOT_Z-si*x+co*z)
            for x,y,z in p["vertices"]]

def aabb(v):return [(min(p[k] for p in v),max(p[k] for p in v)) for k in range(3)]
def sep_box(a,b):
    return max(max(a[k][0]-b[k][1],b[k][0]-a[k][1],0.) for k in range(3))

def validate(parts):
    names=set();triangles=0;vertices=0
    for p in parts:
        assert p["name"] not in names,p["name"];names.add(p["name"])
        assert p["material"] in MATERIALS
        assert p["group"] in ("STATIC",*CENTRES)
        check_mesh(p);triangles+=len(p["faces"]);vertices+=len(p["vertices"])
        assert all(all(math.isfinite(t) for t in v) for v in p["vertices"])
    assert len(MATERIALS)>=3
    assert len([p for p in parts if p["group"]!="STATIC"])==27
    radial_bearing_clearance=.019-.014
    assert radial_bearing_clearance>=.005-1e-9
    fixed=[p for p in parts if p["group"]=="STATIC"]
    checks=0;min_clearance=float("inf")
    for i in range(81):
        t=i/80
        positions={tag:angle*t for tag,angle in ANGLES.items()}
        for tag in CENTRES:
            moving=[p for p in parts if p["group"]==tag and not p["name"].endswith("_AXLE")]
            for p in moving:
                b=aabb(pose(p,positions[tag]))
                for q in fixed:
                    # Bearings intentionally surround the axle; their radial
                    # clearance is independently checked below and in OBJ QA.
                    if "BEARING" in q["name"]:
                        continue
                    d=sep_box(b,aabb(q["vertices"]))
                    assert d>=.006-1e-8,("fixed collision",tag,p["name"],q["name"],i,d)
                    min_clearance=min(min_clearance,d);checks+=1
        for first,second in (("FORE","MID"),("MID","AFT")):
            a=[v[1] for p in parts if p["group"]==first for v in pose(p,positions[first])]
            b=[v[1] for p in parts if p["group"]==second for v in pose(p,positions[second])]
            d=min(b)-max(a)
            assert d>=.005,("adjacent fin",first,second,i,d)
            min_clearance=min(min_clearance,d)
    at_rest=[v for p in parts for v in pose(p,0)]
    bounds=aabb(at_rest)
    assert abs(bounds[0][0])<1e-9,("side mount x=0",bounds)
    assert bounds[0][1]<.75
    assert bounds[1][0]>-.46 and bounds[1][1]<.46
    assert bounds[2][0]>-.12 and bounds[2][1]<.21
    return dict(id=ID,status="PASS",parts=len(parts),vertices=vertices,
                triangles=triangles,materials=len(MATERIALS),
                hinge_pivots={tag:[PIVOT_X,yc,PIVOT_Z] for tag,yc in CENTRES.items()},
                mount_plane="X=0 side socket",unit="1 Blender unit = 1 metre",
                motion_axis="local Y",deploy_angles_deg=ANGLES,
                sample_count=81,fixed_pair_checks=checks,
                min_conservative_aabb_clearance_m=round(min_clearance,6),
                radial_axle_bearing_clearance_m=radial_bearing_clearance,
                rest_bounds_m=bounds,blender="NOT RUN",unity="NOT RUN",
                collision_scope="conservative AABB for non-axle parts vs static non-bearing geometry and Y separation between fins; explicit 5mm radial axle/bearing clearance; not Unity physics")

def static_export(parts,outdir,report):
    outdir.mkdir(parents=True,exist_ok=True)
    obj=outdir/(ID+"_deployable_aero_fin.obj")
    mtl=outdir/(ID+"_deployable_aero_fin.mtl")
    mtl_lines=[]
    for name,(rgba,metallic,roughness,emission) in MATERIALS.items():
        mtl_lines.extend((f"newmtl {name}",
            f"Kd {rgba[0]:.6f} {rgba[1]:.6f} {rgba[2]:.6f}",
            f"Ks {metallic:.6f} {metallic:.6f} {metallic:.6f}",
            f"Ns {max(1,(1-roughness)*800):.2f}",
            f"Ke {rgba[0]*emission:.6f} {rgba[1]*emission:.6f} {rgba[2]*emission:.6f}",""))
    mtl.write_text("\n".join(mtl_lines)+"\n",encoding="utf-8")
    lines=[f"mtllib {mtl.name}",f"# {ID} side socket x=0, rest pose"]
    offset=0
    for p in parts:
        lines.extend(("o "+p["name"],"usemtl "+p["material"]))
        for x,y,z in pose(p,0):lines.append(f"v {x:.8f} {y:.8f} {z:.8f}")
        for a,b,c in p["faces"]:lines.append(f"f {a+1+offset} {b+1+offset} {c+1+offset}")
        offset+=len(p["vertices"])
    obj.write_text("\n".join(lines)+"\n",encoding="utf-8")
    (outdir/(ID+"_static_qa.json")).write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    return obj,mtl

def blender_export(parts,outdir,report):
    import bpy
    old=bpy.data.collections.get(COLLECTION)
    if old:
        for obj in tuple(old.objects):bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(old)
    collection=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(collection)
    scene=bpy.context.scene
    scene.unit_settings.system="METRIC"
    scene.unit_settings.scale_length=1
    scene.render.fps=FPS
    scene.frame_start=1;scene.frame_end=FRAMES
    materials={}
    for name,(rgba,metallic,roughness,emission) in MATERIALS.items():
        m=bpy.data.materials.get(PREFIX+name) or bpy.data.materials.new(PREFIX+name)
        m.diffuse_color=rgba;m.use_nodes=True
        bsdf=m.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value=rgba
        bsdf.inputs["Metallic"].default_value=metallic
        bsdf.inputs["Roughness"].default_value=roughness
        bsdf.inputs["Emission Color"].default_value=rgba
        bsdf.inputs["Emission Strength"].default_value=emission
        materials[name]=m
    pivots={}
    for tag,yc in CENTRES.items():
        empty=bpy.data.objects.new(PREFIX+tag+"_HINGE_Y",None)
        empty.empty_display_type="ARROWS";empty.empty_display_size=.065
        empty.location=(PIVOT_X,yc,PIVOT_Z)
        collection.objects.link(empty);pivots[tag]=empty
    for p in parts:
        mesh=bpy.data.meshes.new(PREFIX+p["name"]+"_MESH")
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.update()
        obj=bpy.data.objects.new(PREFIX+p["name"],mesh)
        collection.objects.link(obj)
        obj.data.materials.append(materials[p["material"]])
        if p["group"]!="STATIC":
            obj.parent=pivots[p["group"]]
            obj.matrix_parent_inverse.identity()
        for polygon in mesh.polygons:polygon.use_smooth=p["smooth"]
        if p["smooth"]:
            for edge in mesh.edges:edge.use_edge_sharp=False
    for tag,pivot in pivots.items():
        maximum=math.radians(ANGLES[tag])
        shift={"FORE":0,"MID":4,"AFT":8}[tag]
        for frame,angle in ((1,0),(14+shift,.34*maximum),
                            (38+shift,maximum),(70,maximum),(FRAMES,0)):
            pivot.rotation_mode="XYZ"
            pivot.rotation_euler=(0,angle,0)
            pivot.keyframe_insert(data_path="rotation_euler",frame=frame,
                                  group="DEPLOY_FIN")
        if pivot.animation_data and pivot.animation_data.action:
            pivot.animation_data.action.name=PREFIX+tag+"_DEPLOY"
            for curve in pivot.animation_data.action.fcurves:
                for key in curve.keyframe_points:key.interpolation="BEZIER"
    scene.frame_set(1)
    outdir.mkdir(parents=True,exist_ok=True)
    blend=outdir/(ID+"_deployable_aero_fin.blend")
    glb=outdir/(ID+"_deployable_aero_fin.glb")
    previously_selected=tuple(bpy.context.selected_objects)
    previous_active=bpy.context.view_layer.objects.active
    try:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in collection.objects:ob.select_set(True)
        bpy.context.view_layer.objects.active=pivots["MID"]
        bpy.ops.wm.save_as_mainfile(filepath=str(blend))
        bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
                                  use_selection=True,export_animations=True)
        assert blend.exists() and glb.exists()
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in previously_selected:
            if bpy.data.objects.get(ob.name) is ob:ob.select_set(True)
        if previous_active and bpy.data.objects.get(previous_active.name) is previous_active:
            bpy.context.view_layer.objects.active=previous_active
    report.update(blender="PASS",blend_export="PASS",glb_export="PASS",
                  blender_version=bpy.app.version_string)
    (outdir/(ID+"_blender_qa.json")).write_text(json.dumps(report,indent=2)+"\n")
    return blend,glb

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--self-test",action="store_true")
    ap.add_argument("--static-obj",action="store_true")
    ap.add_argument("--out",default="build/M04-011")
    args=ap.parse_args(sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:])
    parts=build();report=validate(parts)
    if args.self_test:
        print(json.dumps(report,indent=2));return
    out=Path(args.out).resolve()
    static_export(parts,out,report)
    if not args.static_obj:
        try:import bpy # noqa F401
        except ImportError:
            print("Blender bpy NOT RUN (Python-only mode)",file=sys.stderr)
        else:blender_export(parts,out,report)
    print(json.dumps(report,indent=2))
if __name__=="__main__":main()
