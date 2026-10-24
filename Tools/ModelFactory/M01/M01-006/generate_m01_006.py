#!/usr/bin/env python3
"""M01-006 Ceramic Needle Launcher — visual-only procedural Blender model.

Unity 1 unit = 1 metre; top mount at local origin; +Y forward, +Z up.
The five-needle carriage translates along +Y over a 0.18 m stroke.
No changes to gameplay, WPN mechanics, scenes or ProjectSettings.
"""
from __future__ import annotations
import argparse
from collections import Counter
import json
import math
import sys
from pathlib import Path

ID = "M01-006"
PREFIX = "M01_006_"
MOUNT_PIVOT = (0.0, 0.0, 0.0)
RAIL_TRAVEL = 0.18
MATERIALS = {
    "Ceramic_Ivory": ((.82,.85,.82,1),.05,.31,0),
    "Ceramic_White": ((.94,.95,.90,1),.02,.25,0),
    "Graphite": ((.095,.112,.133,1),.43,.52,0),
    "Titanium": ((.35,.42,.47,1),.81,.30,0),
    "Copper": ((.67,.33,.16,1),.77,.28,0),
    "Cyan_Glow": ((.08,.78,.92,1),.23,.24,2),
    "Amber_Warning": ((.93,.55,.13,1),.28,.38,.35),
}

def sub(a,b): return tuple(x-y for x,y in zip(a,b))
def dot(a,b): return sum(x*y for x,y in zip(a,b))
def cross(a,b): return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])

def solid(name,verts,polygons,material,moving=False,smooth=False):
    """Triangulate convex closed primitive, correcting outward winding."""
    center=tuple(sum(v[i] for v in verts)/len(verts) for i in range(3))
    faces=[]
    for face in polygons:
        a,b,c=(verts[face[i]] for i in range(3))
        normal=cross(sub(b,a),sub(c,a))
        mid=tuple(sum(verts[k][i] for k in face)/len(face) for i in range(3))
        if dot(normal,sub(mid,center))<0:face=tuple(reversed(face))
        for j in range(1,len(face)-1):faces.append((face[0],face[j],face[j+1]))
    return dict(name=name,material=material,vertices=verts,faces=faces,moving=moving,smooth=smooth)

def box(name,center,size,material,moving=False):
    cx,cy,cz=center;a,b,c=(v/2 for v in size)
    vs=[(cx+x,cy+y,cz+z) for x,y,z in
        [(-a,-b,-c),(a,-b,-c),(a,b,-c),(-a,b,-c),(-a,-b,c),(a,-b,c),(a,b,c),(-a,b,c)]]
    return solid(name,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],material,moving)

def prism_y(name,outline,y0,y1,material,moving=False):
    n=len(outline)
    vs=[(x,y,z) for y in (y0,y1) for x,z in outline]
    quads=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]
    quads += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return solid(name,vs,quads,material,moving)

def cylinder(name,axis,center,radius,length,material,moving=False,segments=20):
    cx,cy,cz=center;vs=[]
    for sign in (-1,1):
        for i in range(segments):
            t=2*math.pi*i/segments;u=radius*math.cos(t);v=radius*math.sin(t)
            if axis=="Z":vs.append((cx+u,cy+v,cz+sign*length/2))
            elif axis=="Y":vs.append((cx+u,cy+sign*length/2,cz+v))
            else:vs.append((cx+sign*length/2,cy+u,cz+v))
    vs.extend([(cx,cy,cz-length/2) if axis=="Z" else (cx,cy-length/2,cz) if axis=="Y" else (cx-length/2,cy,cz),
               (cx,cy,cz+length/2) if axis=="Z" else (cx,cy+length/2,cz) if axis=="Y" else (cx+length/2,cy,cz)])
    n=segments;polys=[]
    for i in range(n):
        j=(i+1)%n
        polys.extend([(i,j,n+j,n+i),(2*n,j,i),(2*n+1,n+i,n+j)])
    return solid(name,vs,polys,material,moving,True)

def cone_y(name,center,base_radius,length,material,moving=False,segments=16):
    cx,cy,cz=center;y0=cy-length/2;y1=cy+length/2
    vs=[(cx+base_radius*math.cos(2*math.pi*i/segments),y0,
         cz+base_radius*math.sin(2*math.pi*i/segments)) for i in range(segments)]
    vs += [(cx,y0,cz),(cx,y1,cz)]
    polys=[]
    for i in range(segments):
        j=(i+1)%segments
        polys.extend([(segments,i,j),(i,segments+1,j)])
    return solid(name,vs,polys,material,moving)

def build():
    parts=[];add=parts.append
    add(cylinder("Mount_Disc","Z",(0,0,.027),.145,.054,"Titanium",segments=32))
    add(box("Mount_Key",(0,.005,.061),(.23,.21,.045),"Graphite"))
    add(box("Riser",(0,.045,.136),(.19,.26,.135),"Titanium"))
    for x in (-.105,.105):
        for y in (-.105,.105):
            add(cylinder(f'Mount_Bolt_{"L" if x<0 else "R"}_{"A" if y<0 else "F"}',
                         "Z",(x,y,.055),.014,.012,"Copper",segments=12))
    add(box("Breech_Core",(0,.095,.314),(.55,.76,.215),"Graphite"))
    add(box("Top_Spine",(0,.095,.425),(.30,.61,.044),"Titanium"))
    add(box("Rear_Bulkhead",(0,-.288,.334),(.55,.046,.23),"Titanium"))
    for sign,side in [(-1,"L"),(1,"R")]:
        outline=[(sign*.269,.232),(sign*.355,.240),(sign*.395,.312),
                 (sign*.368,.417),(sign*.297,.455),(sign*.265,.416)]
        add(prism_y(f"Ceramic_Side_Shell_{side}",outline,-.238,.394,"Ceramic_Ivory"))
        add(box(f"Copper_Side_Inlay_{side}",(sign*.399,.110,.329),(.012,.48,.032),"Copper"))
        add(box(f"Cooling_Emitter_{side}",(sign*.409,.113,.361),(.012,.31,.013),"Cyan_Glow"))
        for k,y in enumerate((-.155,.022,.200)):
            profile=[(sign*.327,.426),(sign*.382,.441),(sign*.427,.532),(sign*.358,.502)]
            add(prism_y(f"Sharp_Rib_{side}_{k}",profile,y,y+.033,"Ceramic_White"))
        add(box(f"Static_Guide_{side}",(sign*.177,.235,.547),(.074,.72,.145),"Titanium"))
        add(box(f"Guide_Top_Accent_{side}",(sign*.190,.243,.627),(.029,.65,.010),"Amber_Warning"))
        add(box(f"Guide_Pillar_{side}",(sign*.183,-.112,.448),(.065,.12,.058),"Titanium"))
        add(box(f"Guide_Pillar_Front_{side}",(sign*.183,.390,.448),(.065,.10,.058),"Titanium"))
        add(box(f"Nose_Side_Plate_{side}",(sign*.310,.444,.325),(.105,.125,.14),"Ceramic_White"))
        for k,y in enumerate((-.13,.015,.16,.305)):
            add(box(f"Side_Vent_{side}_{k}",(sign*.408,y,.284),(.009,.048,.017),"Graphite"))
    add(box("Rear_Copper_Actuator",(0,-.333,.337),(.16,.045,.11),"Copper"))
    add(cylinder("Rear_Pressure_Cap","Y",(0,-.368,.334),.053,.041,"Titanium",segments=24))
    add(box("Warning_Top_Stripe",(0,-.204,.456),(.205,.048,.012),"Amber_Warning"))
    add(box("Extending_Carriage",(0,.275,.534),(.260,.590,.058),"Graphite",True))
    add(box("Carriage_Ceramic_Top",(0,.252,.575),(.248,.505,.029),"Ceramic_White",True))
    add(box("Carriage_Glow_Spine",(0,.253,.593),(.022,.465,.009),"Cyan_Glow",True))
    add(box("Carriage_Rear_Stop",(0,-.026,.567),(.245,.055,.070),"Copper",True))
    add(box("Carriage_Front_Bridge",(0,.556,.561),(.25,.034,.085),"Titanium",True))
    for k,x in enumerate((-.104,-.052,0,.052,.104)):
        add(cylinder(f"Needle_Base_Collar_{k}","Y",(x,.467,.614),.019,.092,"Copper",True,segments=16))
        add(cylinder(f"Needle_Ceramic_Shaft_{k}","Y",(x,.579,.614),.013,.155,"Ceramic_White",True,segments=16))
        add(cone_y(f"Needle_Tapered_Point_{k}",(x,.724,.614),.014,.135,"Ceramic_White",True,segments=16))
    return parts

def bounds(vs,dy=0):
    return [(min(v[i]+(dy if i==1 else 0) for v in vs),
             max(v[i]+(dy if i==1 else 0) for v in vs)) for i in range(3)]

def validate(parts):
    names=set();vertices=0;triangles=0
    for p in parts:
        assert p["name"] not in names;names.add(p["name"])
        assert p["material"] in MATERIALS
        vs=p["vertices"];edges=Counter();volume=0
        for v in vs:assert all(math.isfinite(x) for x in v)
        for face in p["faces"]:
            assert len(face)==3 and len(set(face))==3
            assert all(0<=i<len(vs) for i in face)
            a,b,c=(vs[i] for i in face);n=cross(sub(b,a),sub(c,a))
            assert dot(n,n)>1e-16,p["name"]
            volume+=dot(a,cross(b,c))/6
            for i,j in zip(face,face[1:]+face[:1]):edges[tuple(sorted((i,j)))]+=1
        assert edges and all(n==2 for n in edges.values()),p["name"]
        assert volume>1e-9,(p["name"],volume)
        vertices+=len(vs);triangles+=len(p["faces"])
    static=[p for p in parts if not p["moving"]]
    moving=[p for p in parts if p["moving"]]
    checked=0
    for step in range(37):
        dy=RAIL_TRAVEL*step/36
        for s in static:
            sb=bounds(s["vertices"])
            for m in moving:
                mb=bounds(m["vertices"],dy)
                overlaps=[min(sb[i][1],mb[i][1])-max(sb[i][0],mb[i][0]) for i in range(3)]
                assert not all(x>1e-7 for x in overlaps),(s["name"],m["name"],step,overlaps)
                checked+=1
    assert len([p for p in parts if p["name"].startswith("Needle_Tapered_Point_")])==5
    return dict(id=ID,status="PASS_STATIC",parts=len(parts),static_parts=len(static),
        moving_parts=len(moving),vertices=vertices,triangles=triangles,materials=len(MATERIALS),
        needles=5,unit="meter",mount_pivot_m=MOUNT_PIVOT,forward_axis="+Y",up_axis="+Z",
        rail_translation_axis="+Y",rail_extension_m=RAIL_TRAVEL,animation_pose_samples=37,
        aabb_pair_checks=checked,static_moving_collision_count=0,
        manifold_components=len(parts),positive_signed_volume_parts=len(parts),
        blender_export="NOT_RUN",unity_import="NOT_RUN",visual_review="NOT_RUN")

def static_export(parts,out):
    out.mkdir(parents=True,exist_ok=True)
    obj=out/"M01-006_ceramic_needle_launcher.obj"
    mtl=out/"M01-006_ceramic_needle_launcher.mtl"
    lines=["# M01-006; Unity 1 unit = 1 m; +Y forward; +Z up; pivot origin",f"mtllib {mtl.name}"]
    offset=1
    for p in parts:
        lines.extend([f'o {p["name"]}',f'usemtl {p["material"]}'])
        lines.extend(f'v {x:.7f} {y:.7f} {z:.7f}' for x,y,z in p["vertices"])
        lines.extend("f "+" ".join(str(i+offset) for i in face) for face in p["faces"])
        offset+=len(p["vertices"])
    obj.write_text("\n".join(lines)+"\n",encoding="utf-8")
    mats=[]
    for name,(rgba,metal,rough,emission) in MATERIALS.items():
        mats.extend([f"newmtl {name}",f"Kd {rgba[0]:.5f} {rgba[1]:.5f} {rgba[2]:.5f}",
                     f"Ks {metal*.5:.5f} {metal*.5:.5f} {metal*.5:.5f}",
                     f"Ns {(1-rough)*400:.2f}",
                     f"Ke {emission*rgba[0]:.5f} {emission*rgba[1]:.5f} {emission*rgba[2]:.5f}",
                     "d 1.0","illum 2",""])
    mtl.write_text("\n".join(mats)+"\n",encoding="utf-8")
    return {"obj":str(obj),"mtl":str(mtl)}

def blender_export(parts,out):
    import bpy
    name="M01-006_ASSET"
    owned=bpy.data.collections.get(name)
    if owned:
        for ob in tuple(owned.objects):bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(owned)
    owned=bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(owned)
    def empty(name,parent=None):
        obj=bpy.data.objects.new(PREFIX+name,None)
        owned.objects.link(obj);obj.parent=parent;obj.empty_display_size=.07
        return obj
    mount=empty("MOUNT_TOP_Z0")
    carriage=empty("RAIL_EXTEND_Y",mount)
    mats={}
    for name,(rgba,metal,rough,emit) in MATERIALS.items():
        mat=bpy.data.materials.get(PREFIX+name)
        if mat is None:mat=bpy.data.materials.new(PREFIX+name)
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value=rgba
            bsdf.inputs["Metallic"].default_value=metal
            bsdf.inputs["Roughness"].default_value=rough
            if emit:
                bsdf.inputs["Emission Color"].default_value=rgba
                bsdf.inputs["Emission Strength"].default_value=emit
        mat.diffuse_color=rgba;mats[name]=mat
    for p in parts:
        mesh=bpy.data.meshes.new(PREFIX+p["name"]+"_mesh")
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.update(calc_edges=True)
        obj=bpy.data.objects.new(PREFIX+p["name"],mesh)
        owned.objects.link(obj)
        obj.parent=carriage if p["moving"] else mount
        mesh.materials.append(mats[p["material"]])
        for polygon in mesh.polygons:polygon.use_smooth=p["smooth"]
    for frame,travel in [(1,0),(9,.06),(17,.12),(25,.18),(31,.18),(39,.12),(47,.06),(55,0)]:
        carriage.location.y=travel
        carriage.keyframe_insert(data_path="location",index=1,frame=frame)
    if carriage.animation_data and carriage.animation_data.action:
        try:
            for fc in carriage.animation_data.action.fcurves:
                for key in fc.keyframe_points:key.interpolation="LINEAR"
        except AttributeError:pass
    scene=bpy.context.scene
    old_start,old_end,old_frame,old_fps=scene.frame_start,scene.frame_end,scene.frame_current,scene.render.fps
    selected=list(bpy.context.selected_objects)
    active=bpy.context.view_layer.objects.active
    try:
        scene.frame_start=1;scene.frame_end=55;scene.render.fps=60;scene.frame_set(1)
        bpy.context.view_layer.update()
        assert len(owned.objects)==len(parts)+2
        bpy.data.libraries.write(str(out/"M01-006_ceramic_needle_launcher.blend"),{owned},compress=True)
        bpy.ops.object.select_all(action="DESELECT")
        for obj in owned.objects:obj.select_set(True)
        bpy.context.view_layer.objects.active=mount
        bpy.ops.export_scene.gltf(filepath=str(out/"M01-006_ceramic_needle_launcher_animated.glb"),
                                  export_format="GLB",use_selection=True,export_animations=True)
        bpy.ops.export_scene.fbx(filepath=str(out/"M01-006_ceramic_needle_launcher_animated.fbx"),
                                 use_selection=True,axis_forward="-Y",axis_up="Z",
                                 add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False)
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in selected:
            if ob.name in bpy.data.objects:ob.select_set(True)
        if active and active.name in bpy.data.objects:bpy.context.view_layer.objects.active=active
        scene.frame_start,scene.frame_end,scene.render.fps=old_start,old_end,old_fps
        scene.frame_set(old_frame)
    return {"blender_export":"PASS",
            "fbx":str(out/"M01-006_ceramic_needle_launcher_animated.fbx"),
            "glb":str(out/"M01-006_ceramic_needle_launcher_animated.glb"),
            "blend":str(out/"M01-006_ceramic_needle_launcher.blend")}

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    ap=argparse.ArgumentParser()
    ap.add_argument("--out",default="build/M01-006")
    ap.add_argument("--self-test",action="store_true")
    args=ap.parse_args(argv)
    parts=build();report=validate(parts)
    print(json.dumps(report,ensure_ascii=False,indent=2))
    if args.self_test:return
    out=Path(args.out).resolve();out.mkdir(parents=True,exist_ok=True)
    report.update(static_export(parts,out))
    try:import bpy
    except ImportError:pass
    else:report.update(blender_export(parts,out))
    (out/"M01-006_qa.json").write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,ensure_ascii=False,indent=2))

if __name__=="__main__":main()
