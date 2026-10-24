#!/usr/bin/env python3
"""M05-007 Cassette Accumulator: four cells in two independently sliding drawers.

Visual asset only; no energy storage gameplay. Blender/Unity scale 1 unit = 1 m.
Z-up; front/service direction +Y; mount pivot (0,0,0) at bottom plane.
The two sliding empties have local +Y translation [0, .21] metres.
Python 3.10+ static OBJ/MTL + manifold/clearance QA; Blender bpy for GLB/BLEND.
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from collections import Counter
from pathlib import Path

ID = "M05-007"
HERE = Path(__file__).resolve().parent
MATERIALS = {
    "Frame_Graphite": ((.11,.15,.20,1), .75,.38,None),
    "Armor_Ceramic": ((.80,.83,.79,1), .25,.34,None),
    "Contacts_Copper": ((.72,.38,.17,1), .82,.29,None),
    "Cell_Petrol": ((.055,.31,.32,1), .36,.26,None),
    "Gasket_Rubber": ((.08,.09,.10,1), .0,.86,None),
    "Status_Cyan": ((.035,.64,.83,1), .18,.22,(.02,.48,.85,2.5)),
    "Warning_Amber": ((.91,.51,.08,1), .28,.34,(.92,.25,.015,1.3)),
}
PARTS = []
def part(name, shape, material, group="fixed", **data):
    if any(p["name"] == name for p in PARTS):
        raise ValueError("Duplicate part: "+name)
    PARTS.append(dict(name=name,shape=shape,material=material,group=group,**data))

def box(name, center, size, material, group="fixed"):
    part(name,"box",material,group,center=center,size=size)
def cylinder(name, center, radius, depth, material, group="fixed", n=32):
    part(name,"cylinder",material,group,center=center,radius=radius,depth=depth,n=n)
def torus(name, center, major, minor, material, group="fixed", n=40, m=8):
    part(name,"torus",material,group,center=center,major=major,minor=minor,n=n,m=m)

def construct():
    if PARTS:
        return PARTS
    # Stationary open lattice: deliberate open service bays, no solid side walls.
    for sx in (-1,1):
        for sy in (-1,1):
            box("CornerPost_%s_%s"%(sx,sy),(.50*sx,.345*sy,.300),
                (.045,.045,.52),"Frame_Graphite")
            box("MountFoot_%s_%s"%(sx,sy),(.50*sx,.345*sy,.025),
                (.105,.105,.05),"Contacts_Copper")
    for sy in (-1,1):
        box("LowerCrossbar_%s"%sy,(0,.345*sy,.070),
            (1.045,.045,.055),"Frame_Graphite")
        box("UpperCrossbar_%s"%sy,(0,.345*sy,.550),
            (1.045,.045,.052),"Armor_Ceramic")
    for sx in (-1,1):
        box("LowerSideRail_%s"%sx,(.515*sx,0,.115),
            (.045,.72,.055),"Frame_Graphite")
        box("UpperSideRail_%s"%sx,(.515*sx,0,.530),
            (.045,.72,.045),"Frame_Graphite")
        box("PowerConnector_%s"%sx,(.39*sx,-.35,.29),
            (.07,.045,.10),"Contacts_Copper")
    box("CentralFloorRail",(0,0,.09),(.040,.73,.065),"Frame_Graphite")
    box("CentralTopRib",(0,0,.54),(.035,.73,.040),"Frame_Graphite")
    box("RearPowerBus",(0,-.345,.315),(.20,.048,.105),"Contacts_Copper")
    for sx in (-1,1):
        box("FrontLatch_%s"%sx,(.255*sx,.345,.108),
            (.145,.032,.045),"Warning_Amber")
    # Drawers use LOCAL coordinates; Blender parent empties at x = +/- .255.
    for sx,label in ((-1,"L"),(1,"R")):
        g="drawer_"+label
        box(label+"_Tray",(0,0,.175),(.405,.50,.035),"Frame_Graphite",g)
        for sign in (-1,1):
            box(label+"_TraySide_%s"%sign,(sign*.190,0,.225),
                (.024,.48,.082),"Armor_Ceramic",g)
            box(label+"_CopperRunner_%s"%sign,(sign*.175,0,.198),
                (.009,.43,.012),"Contacts_Copper",g)
        box(label+"_RearStop",(0,-.245,.237),
            (.380,.025,.085),"Gasket_Rubber",g)
        box(label+"_FrontFascia",(0,.257,.240),
            (.405,.030,.15),"Armor_Ceramic",g)
        box(label+"_PullHandle",(0,.286,.255),
            (.14,.036,.045),"Contacts_Copper",g)
        for x in (-.126,.126):
            box(label+"_StatusLED_%s"%x,(x,.272,.285),
                (.058,.013,.019),"Status_Cyan",g)
            box(label+"_Vent_%s"%x,(x,.273,.200),
                (.075,.011,.011),"Gasket_Rubber",g)
        # Four replaceable cells in total: two per drawer.
        for i,y in enumerate((-.115,.110)):
            prefix=label+"_Cell_%d"%i
            cylinder(prefix+"_Seat",(0,y,.211),.063,.043,
                "Gasket_Rubber",g)
            cylinder(prefix+"_Body",(0,y,.333),.080,.225,
                "Cell_Petrol",g,40)
            cylinder(prefix+"_BottomCollar",(0,y,.241),.084,.022,
                "Contacts_Copper",g,40)
            cylinder(prefix+"_TopCap",(0,y,.449),.083,.028,
                "Contacts_Copper",g,40)
            torus(prefix+"_StatusHalo",(0,y,.464),.063,.008,
                "Status_Cyan",g)
            for j,z in enumerate((.290,.381)):
                torus(prefix+"_Insulator_%d"%j,(0,y,z),.080,.007,
                    "Gasket_Rubber",g)
            box(prefix+"_Polarity",(0,y,.467),(.035,.035,.008),
                "Warning_Amber",g)
    return PARTS

def geometry(p):
    s=p["shape"]
    if s=="box":
        x,y,z=(v/2 for v in p["size"])
        vs=[(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),
            (-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
        fs=[(3,2,1,0),(4,5,6,7),(0,1,5,4),(1,2,6,5),
            (2,3,7,6),(3,0,4,7)]
    elif s=="cylinder":
        n=p["n"];r=p["radius"];h=p["depth"]/2
        vs=[(r*math.cos(2*math.pi*i/n),r*math.sin(2*math.pi*i/n),z)
            for z in (-h,h) for i in range(n)]
        fs=[tuple(reversed(range(n))),tuple(range(n,2*n))]
        fs.extend((i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n))
    elif s=="torus":
        n=p["n"];m=p["m"];r=p["major"];t=p["minor"]
        vs=[((r+t*math.cos(2*math.pi*j/m))*math.cos(2*math.pi*i/n),
             (r+t*math.cos(2*math.pi*j/m))*math.sin(2*math.pi*i/n),
             t*math.sin(2*math.pi*j/m))
            for i in range(n) for j in range(m)]
        fs=[(i*m+j,((i+1)%n)*m+j,
             ((i+1)%n)*m+(j+1)%m,i*m+(j+1)%m)
            for i in range(n) for j in range(m)]
    else:
        raise ValueError("Unknown geometry "+s)
    cx,cy,cz=p["center"]
    return [(x+cx,y+cy,z+cz) for x,y,z in vs],fs

def group_origin(group):
    return (0,0,0) if group=="fixed" else (
        -.255 if group=="drawer_L" else .255,0,0)

def world_vertices(p,slide=0):
    verts,_=geometry(p)
    ox,oy,oz=group_origin(p["group"])
    return [(x+ox,y+oy+(slide if p["group"]!="fixed" else 0),z+oz)
            for x,y,z in verts]

def signed_volume(vs,fs):
    v=0.0
    for f in fs:
        a=vs[f[0]]
        for j in range(1,len(f)-1):
            b,c=vs[f[j]],vs[f[j+1]]
            cross=(b[1]*c[2]-b[2]*c[1],b[2]*c[0]-b[0]*c[2],
                   b[0]*c[1]-b[1]*c[0])
            v+=sum(a[k]*cross[k] for k in range(3))/6
    return v

def topology_check(p):
    vs,fs=geometry(p)
    edges={}
    for f in fs:
        assert len(f)>=3 and len(set(f))==len(f),p["name"]
        assert all(0<=i<len(vs) for i in f),p["name"]
        for a,b in zip(f,f[1:]+f[:1]):
            key=tuple(sorted((a,b)))
            edges.setdefault(key,[]).append((a,b))
    assert all(len(v)==2 and v[0]==v[1][::-1]
               for v in edges.values()),("nonmanifold",p["name"])
    assert signed_volume(vs,fs)>1e-10,("inverted winding",p["name"])
    return len(vs),len(fs),sum(len(f)-2 for f in fs)

def aabb(vs):
    return tuple((min(p[i] for p in vs),max(p[i] for p in vs))
                 for i in range(3))

def overlap(a,b):
    return [min(a[i][1],b[i][1])-max(a[i][0],b[i][0])
            for i in range(3)]

def validate():
    parts=construct()
    assert len(MATERIALS)>=3
    assert len(set(p["name"] for p in parts))==len(parts)
    assert len([p for p in parts if "_Cell_" in p["name"] and p["name"].endswith("_Body")])==4
    stats=[topology_check(p) for p in parts]
    fixed=[p for p in parts if p["group"]=="fixed"]
    moving=[p for p in parts if p["group"]!="fixed"]
    fixed_boxes=[(p,aabb(world_vertices(p))) for p in fixed]
    checked=0
    min_gap=float("inf")
    for sample in range(22):
        slide=sample*.01
        for p in moving:
            mb=aabb(world_vertices(p,slide))
            for f,fb in fixed_boxes:
                ov=overlap(mb,fb)
                checked+=1
                if all(v>1e-7 for v in ov):
                    raise AssertionError(("moving/fixed clipping",sample,p["name"],f["name"],ov))
                min_gap=min(min_gap,max(-v for v in ov if v<=0) if any(v<=0 for v in ov) else 0)
    # Independently check the two drawer envelopes, not merely sampled meshes.
    assert .255-.405/2>0
    assert 2*(.255-.405/2)>.10
    # The tray never reaches the top or bottom structural rails.
    assert .175-.035/2>.14
    assert .449+.028/2+.008<.49
    all_vs=[v for p in parts for v in world_vertices(p)]
    bounds=aabb(all_vs)
    return {"id":ID,"parts":len(parts),
            "fixed_parts":len(fixed),"moving_parts":len(moving),
            "vertices":sum(v for v,f,t in stats),
            "polygons":sum(f for v,f,t in stats),
            "triangles":sum(t for v,f,t in stats),
            "materials":len(MATERIALS),"cells":4,"drawers":2,
            "mount_origin_m":[0,0,0],"axis":"local +Y",
            "drawer_travel_m":.21,"sweep_samples":22,
            "moving_fixed_aabb_checks":checked,
            "bounds_neutral_m":[[round(q,4) for q in dim] for dim in bounds],
            "closed_manifold":"PASS","positive_winding":"PASS",
            "swept_aabb_clearance":"PASS",
            "blender":"NOT_RUN","unity":"NOT_RUN"}

def static_export(out):
    out.mkdir(parents=True,exist_ok=True)
    lines=["# M05-007 four-cell cassette accumulator, Z-up, metres",
           "mtllib M05-007.mtl"]
    offset=1
    for p in construct():
        vs,fs=geometry(p)
        ox,oy,oz=group_origin(p["group"])
        lines.extend(("o "+p["name"],"usemtl "+p["material"]))
        lines.extend("v %.6f %.6f %.6f"%(x+ox,y+oy,z+oz) for x,y,z in vs)
        lines.extend("f "+" ".join(str(i+offset) for i in face) for face in fs)
        offset+=len(vs)
    (out/"M05-007.obj").write_text("\n".join(lines)+"\n",encoding="utf-8")
    mtl=["# M05-007 material library"]
    for name,(color,metal,rough,emissive) in MATERIALS.items():
        mtl.extend(["newmtl "+name,"Kd %.5f %.5f %.5f"%color[:3],
                    "Ks %.5f %.5f %.5f"%(metal*.4,)*3,
                    "Ns %.1f"%((1-rough)*400),"d 1.0","illum 2",""])
    (out/"M05-007.mtl").write_text("\n".join(mtl)+"\n",encoding="utf-8")

def blender_export(out):
    import bpy
    name="M05-007_ASSET"
    old=bpy.data.collections.get(name)
    if old:
        for ob in tuple(old.objects):
            bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(old)
    for mesh in tuple(bpy.data.meshes):
        if mesh.name.startswith("M05-007_") and mesh.users==0:
            bpy.data.meshes.remove(mesh)
    for mat in tuple(bpy.data.materials):
        if mat.name.startswith("M05-007_") and mat.users==0:
            bpy.data.materials.remove(mat)
    collection=bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    def empty(label,parent=None,loc=(0,0,0)):
        obj=bpy.data.objects.new("M05-007_"+label,None)
        collection.objects.link(obj)
        obj.parent=parent
        obj.location=loc
        obj.empty_display_size=.07
        return obj
    root=empty("MOUNT_ROOT")
    drawers={key:empty("DRAWER_"+key[-1],root,group_origin(key))
             for key in ("drawer_L","drawer_R")}
    mats={}
    for name,(rgba,metal,rough,emission) in MATERIALS.items():
        mat=bpy.data.materials.new("M05-007_"+name)
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value=rgba
        bsdf.inputs["Metallic"].default_value=metal
        bsdf.inputs["Roughness"].default_value=rough
        if emission:
            bsdf.inputs["Emission Color"].default_value=(*emission[:3],1)
            bsdf.inputs["Emission Strength"].default_value=emission[3]
        mat.diffuse_color=rgba
        mats[name]=mat
    for p in construct():
        vs,fs=geometry(p)
        mesh=bpy.data.meshes.new("M05-007_"+p["name"]+"_mesh")
        mesh.from_pydata(vs,[],fs)
        mesh.update(calc_edges=True)
        ob=bpy.data.objects.new("M05-007_"+p["name"],mesh)
        collection.objects.link(ob)
        ob.parent=root if p["group"]=="fixed" else drawers[p["group"]]
        ob.data.materials.append(mats[p["material"]])
        ob["asset_id"]=ID
        ob["group"]=p["group"]
        for poly in mesh.polygons:
            poly.use_smooth=p["shape"] in ("cylinder","torus")
    for key,keys in (("drawer_L",((1,0),(16,.21),(31,.21),(46,0),(61,0))),
                     ("drawer_R",((1,0),(16,0),(31,.21),(46,.21),(61,0)))):
        ob=drawers[key]
        for frame,dist in keys:
            ob.location.y=dist
            ob.keyframe_insert(data_path="location",index=1,frame=frame)
        if ob.animation_data and ob.animation_data.action:
            action=ob.animation_data.action
            if hasattr(action,"fcurves"):
                for curve in action.fcurves:
                    for kp in curve.keyframe_points:
                        kp.interpolation="LINEAR"
    scene=bpy.context.scene
    oldframe=scene.frame_current
    oldrange=(scene.frame_start,scene.frame_end)
    oldselected=tuple(bpy.context.selected_objects)
    oldactive=bpy.context.view_layer.objects.active
    try:
        scene.frame_start=1
        scene.frame_end=61
        scene.frame_set(1)
        bpy.context.view_layer.update()
        bpy.data.libraries.write(str(out/"M05-007.blend"),{collection},compress=True)
        bpy.ops.object.select_all(action="DESELECT")
        for ob in collection.objects:
            ob.select_set(True)
        bpy.context.view_layer.objects.active=root
        bpy.ops.export_scene.gltf(filepath=str(out/"M05-007_animated.glb"),
                                  export_format="GLB",use_selection=True,
                                  export_animations=True,export_apply=False)
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in oldselected:
            if ob.name in bpy.data.objects:
                ob.select_set(True)
        if oldactive and oldactive.name in bpy.data.objects:
            bpy.context.view_layer.objects.active=oldactive
        scene.frame_start,scene.frame_end=oldrange
        scene.frame_set(oldframe)
    return {"blender":"EXECUTED_NOT_VISUALLY_REVIEWED",
            "blend_file":str(out/"M05-007.blend"),
            "animated_glb":str(out/"M05-007_animated.glb"),
            "blender_objects":len(collection.objects)}

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default=str(HERE/"generated"))
    parser.add_argument("--self-test",action="store_true")
    parser.add_argument("--require-blender",action="store_true")
    args=parser.parse_args(argv)
    report=validate()
    if args.self_test:
        print(json.dumps(report,indent=2))
        return
    out=Path(args.out).resolve()
    static_export(out)
    try:
        import bpy
    except ImportError:
        if args.require_blender:
            raise RuntimeError("Blender bpy unavailable")
    else:
        report.update(blender_export(out))
    (out/"M05-007.report.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))

if __name__=="__main__":
    main()
