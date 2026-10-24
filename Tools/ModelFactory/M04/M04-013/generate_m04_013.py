#!/usr/bin/env python3
"""M04-013 Ringed Water Propeller, visual-only Unity 6 asset.

Scale: 1 unit = 1 metre. X right, Y up, +Z aft (propeller thrust axis).
MOUNT_REAR_SOCKET origin (0,0,0), rear frame mating plane Z=0.
ROTOR_Z pivot (0,0,0.65), continuous rotation around +Z.
Run in Python 3.10+ for static OBJ/MTL, or Blender 4.0+ for BLEND/GLB/FBX.
No gameplay or physics logic.
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from pathlib import Path

ID = "M04-013"
N = 32
FPS = 30
PIVOT_Z = .65
DUCT_INNER = .315
DUCT_OUTER = .382
BLADE_TIP = .278
BLADE_COUNT = 5
MATS = {
    "Hull_Graphite": ((.065,.082,.105,1), .70,.37,(0,0,0),0),
    "Titanium_Brushed": ((.43,.51,.56,1), .78,.27,(0,0,0),0),
    "Ceramic_Ivory": ((.79,.83,.79,1), .22,.34,(0,0,0),0),
    "Bronze_Blades": ((.56,.29,.10,1), .78,.30,(0,0,0),0),
    "Rubber_Seals": ((.023,.029,.037,1), .04,.83,(0,0,0),0),
    "Cyan_Status": ((.10,.60,.94,1), .15,.30,(.04,.56,1),1.8),
    "Amber_Service": ((.98,.41,.045,1), .25,.34,(1,.25,.015),.5),
}
KEYS = ((1,0),(31,90),(61,180),(91,270),(121,360))

def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])
def sub(a,b):
    return tuple(x-y for x,y in zip(a,b))
def dot(a,b):
    return sum(x*y for x,y in zip(a,b))
def norm(a):
    d=math.sqrt(dot(a,a))
    assert d>1e-12
    return tuple(x/d for x in a)
def part(name,mat,v,f,moving=False):
    return dict(name=name,material=mat,vertices=v,faces=f,moving=moving)

def cylinder(name,mat,x,y,z0,z1,r,n=N,moving=False):
    assert z1>z0 and r>0 and n>=8
    v=[(x+r*math.cos(2*math.pi*j/n),y+r*math.sin(2*math.pi*j/n),z)
       for z in (z0,z1) for j in range(n)]
    v += [(x,y,z0),(x,y,z1)]
    f=[]
    for j in range(n):
        k=(j+1)%n
        f.extend(((j,k,n+k),(j,n+k,n+j),
                  (2*n,k,j),(2*n+1,n+j,n+k)))
    return part(name,mat,v,f,moving)

def annulus(name,mat,x,y,z0,z1,inner,outer,n=N,moving=False):
    assert z1>z0 and 0<inner<outer
    rings=((z0,inner),(z0,outer),(z1,outer),(z1,inner))
    v=[(x+r*math.cos(2*math.pi*j/n),y+r*math.sin(2*math.pi*j/n),z)
       for z,r in rings for j in range(n)]
    f=[]
    for i in range(4):
        nxt=(i+1)%4
        for j in range(n):
            k=(j+1)%n
            a=i*n+j; b=i*n+k; c=nxt*n+k; d=nxt*n+j
            f.extend(((a,b,c),(a,c,d)))
    return part(name,mat,v,f,moving)

def strut(name,mat,a,b,r,n=12):
    """Closed cylinder with arbitrary axial direction, for load-bearing braces."""
    axis=norm(sub(b,a))
    ref=(0,1,0) if abs(axis[1])<.85 else (1,0,0)
    u=norm(cross(ref,axis))
    w=cross(axis,u)
    v=[tuple(p[k]+r*(math.cos(2*math.pi*j/n)*u[k]+
                     math.sin(2*math.pi*j/n)*w[k]) for k in range(3))
       for p in (a,b) for j in range(n)]
    v += [a,b]
    f=[]
    for j in range(n):
        k=(j+1)%n
        f.extend(((j,k,n+k),(j,n+k,n+j),
                  (2*n,k,j),(2*n+1,n+j,n+k)))
    return part(name,mat,v,f)

def blade(i):
    """Twisted, swept, closed four-sided airfoil, not a flat fan triangle."""
    phi0=2*math.pi*i/BLADE_COUNT
    v=[]
    for j in range(8):
        t=j/7
        r=.085+(.278-.085)*t
        phi=phi0 + .17*t + .06*t*t
        cr, sr=math.cos(phi),math.sin(phi)
        tx,ty=-sr,cr
        chord=.118-.047*t
        pitch=math.radians(38-21*t)
        zc=.655+.011*math.sin(math.pi*t)
        thick=.011-.004*t
        # loop: leading upper, trailing upper, trailing lower, leading lower
        for tangent,upper in ((-1,1),(1,1),(1,-1),(-1,-1)):
            offset=tangent*chord/2
            v.append((r*cr+offset*tx,r*sr+offset*ty,
                      zc+offset*math.tan(pitch)+upper*thick/2))
    f=[]
    for j in range(7):
        for k in range(4):
            a=4*j+k; b=4*(j+1)+k; c=4*(j+1)+(k+1)%4
            d=4*j+(k+1)%4
            f.extend(((a,b,c),(a,c,d)))
    f.extend(((0,1,2),(0,2,3)))
    end=28
    f.extend(((end,end+2,end+1),(end,end+3,end+2)))
    return part("Rotor_Twisted_Blade_%02d"%(i+1),"Bronze_Blades",v,f,True)

def build():
    p=[]; add=p.append
    # Real annular rear mounting plate, with open water passage.
    add(annulus("Mount_Rear_Annular_Plate","Titanium_Brushed",
                0,0,0,.060,.095,.245))
    add(annulus("Mount_Seal","Rubber_Seals",0,0,.060,.076,.09,.190))
    add(cylinder("Drive_Shaft","Titanium_Brushed",0,0,.076,.560,.044))
    add(annulus("Drive_Bearing_Race","Bronze_Blades",0,0,.085,.133,.047,.081))
    # Structural braces connect mounting flange to the outer duct.
    for i in range(4):
        a=math.pi/4+i*math.pi/2
        c,s=math.cos(a),math.sin(a)
        add(strut("Duct_Load_Brace_%d"%(i+1),"Hull_Graphite",
                  (.174*c,.174*s,.040),(.344*c,.344*s,.500),.023))
        add(cylinder("Mount_Bolt_%d"%(i+1),"Amber_Service",
                     .205*c,.205*s,-.008,.068,.013,12))
    # Open annular shroud; no solid disk obstructs the water stream.
    add(annulus("Main_Duct_Annulus","Hull_Graphite",0,0,.435,.880,
                DUCT_INNER,DUCT_OUTER,48))
    add(annulus("Duct_Front_Ceramic_Lip","Ceramic_Ivory",
                0,0,.432,.460,.312,.382,48))
    add(annulus("Duct_Aft_Titanium_Lip","Titanium_Brushed",
                0,0,.857,.890,.312,.382,48))
    add(annulus("Duct_Mid_Copper_Cooling_Ring","Bronze_Blades",
                0,0,.570,.590,.376,.396,48))
    add(annulus("Duct_Seal_Forward","Rubber_Seals",
                0,0,.460,.475,.314,.327,48))
    add(annulus("Duct_Seal_Aft","Rubber_Seals",
                0,0,.840,.855,.314,.327,48))
    # Narrow external cooling fins seated into duct wall.
    for i in range(8):
        a=2*math.pi*i/8
        c,s=math.cos(a),math.sin(a)
        add(strut("Outer_Heat_Fin_%02d"%(i+1),"Titanium_Brushed",
                  (.378*c,.378*s,.495),(.378*c,.378*s,.815),.010,10))
    # Propeller rotor. Hub meets shaft end at Z=.560 without shaft overlap.
    add(cylinder("Rotor_Main_Hub","Titanium_Brushed",
                 0,0,.560,.737,.098,32,True))
    add(cylinder("Rotor_Spinner","Ceramic_Ivory",
                 0,0,.737,.795,.067,32,True))
    add(annulus("Rotor_Copper_Collar","Bronze_Blades",
                0,0,.605,.625,.098,.114,32,True))
    add(annulus("Rotor_Cyan_Indicator","Cyan_Status",
                0,0,.720,.735,.098,.112,32,True))
    for i in range(BLADE_COUNT):
        add(blade(i))
    # Rear integrated protective grille. Radial spokes are BEHIND blades,
    # and terminate in the shroud; no grille geometry in rotor sweep.
    add(annulus("Grille_Rear_Ring","Titanium_Brushed",
                0,0,.885,.926,.300,.370,48))
    add(cylinder("Grille_Centre_Cap","Ceramic_Ivory",
                 0,0,.890,.940,.072,32))
    for i in range(8):
        a=2*math.pi*i/8
        c,s=math.cos(a),math.sin(a)
        add(strut("Grille_Radial_Spoke_%02d"%(i+1),"Titanium_Brushed",
                  (.064*c,.064*s,.908),(.331*c,.331*s,.908),.012,10))
    # Two status strips, flush with shroud wall, readable without bloom.
    for i,a in enumerate((math.pi/4,5*math.pi/4),1):
        c,s=math.cos(a),math.sin(a)
        add(strut("Service_Light_%d"%i,"Cyan_Status",
                  (.384*c,.384*s,.640),(.384*c,.384*s,.755),.009,10))
    return p

def bounds(p):
    return [(min(v[k] for v in p["vertices"]),
             max(v[k] for v in p["vertices"])) for k in range(3)]

def validate(parts):
    names=set(); triangles=0; vertices=0; moving=0; minvol=1e99
    for p in parts:
        assert p["name"] not in names,("duplicate",p["name"])
        names.add(p["name"])
        assert p["material"] in MATS
        assert p["vertices"] and p["faces"]
        vertices+=len(p["vertices"])
        moving+=bool(p["moving"])
        edges={}; volume=0.
        for v in p["vertices"]:
            assert all(math.isfinite(q) for q in v)
        for a,b,c in p["faces"]:
            assert len({a,b,c})==3
            assert 0<=min(a,b,c) and max(a,b,c)<len(p["vertices"])
            A,B,C=(p["vertices"][q] for q in (a,b,c))
            normal=cross(sub(B,A),sub(C,A))
            assert dot(normal,normal)>1e-17,("degenerate",p["name"])
            volume+=dot(A,cross(B,C))/6.
            for q,r in ((a,b),(b,c),(c,a)):
                edge=tuple(sorted((q,r)))
                edges[edge]=edges.get(edge,0)+1
            triangles+=1
        assert all(count==2 for count in edges.values()),("nonmanifold",p["name"])
        assert volume>1e-10,("inverted normals",p["name"],volume)
        minvol=min(minvol,volume)
    assert len(MATS)>=3 and len(parts)>=40
    assert moving==BLADE_COUNT+4
    assert len(names)==len(parts)
    assert {f"Rotor_Twisted_Blade_{i:02d}" for i in range(1,6)}.issubset(names)
    assert all(p["moving"]==p["name"].startswith("Rotor_") for p in parts)
    assert KEYS[0]==(1,0) and KEYS[-1]==(121,360)
    # Every moving vertex sweeps a circle of constant radius about Z.
    # Check the ACTUAL mesh vertex positions for every blade at all
    # frames (not just AABBs at the two endpoint angles).
    rotor=[p for p in parts if p["moving"]]
    rotor_max_r=max(math.hypot(v[0],v[1]) for p in rotor for v in p["vertices"])
    rotor_max_z=max(v[2] for p in rotor for v in p["vertices"])
    rotor_min_z=min(v[2] for p in rotor for v in p["vertices"])
    radial_gap=DUCT_INNER-rotor_max_r
    axial_gap=.885-rotor_max_z
    assert radial_gap>.018,(radial_gap,"blade to duct")
    assert axial_gap>.070,(axial_gap,"blade to rear grille")
    assert rotor_min_z>=.559
    assert min(v[2] for p in parts for v in p["vertices"])>=-.009
    sample_count=0
    for frame in range(1,122):
        angle=2*math.pi*(frame-1)/120
        c,s=math.cos(angle),math.sin(angle)
        for p in rotor:
            for x,y,z in p["vertices"]:
                X=c*x-s*y;Y=s*x+c*y
                assert math.hypot(X,Y)<DUCT_INNER-.018
                assert z<.885-.070
                sample_count+=1
    assert sample_count>100000
    return dict(asset_id=ID,parts=len(parts),vertices=vertices,
        triangles=triangles,materials=len(MATS),moving_meshes=moving,
        blade_count=BLADE_COUNT,radial_clearance_m=round(radial_gap,5),
        grille_axial_clearance_m=round(axial_gap,5),
        motion_vertex_checks=sample_count,animation_frames=121,
        mount="MOUNT_REAR_SOCKET (0,0,0), rear plane Z=0, +Z aft",
        rotor_pivot=[0,0,PIVOT_Z],rotor_axis="+Z",fps=FPS,
        manifold="PASS",face_winding="PASS",
        export_blender="NOT RUN",unity_import="NOT RUN",
        unity_editmode="NOT RUN",unity_playmode="NOT RUN",
        visual_inspection="NOT RUN")

def export_obj(parts,out):
    stem="M04-013_ringed_water_propeller"
    obj=out/(stem+".obj");mtl=out/(stem+".mtl")
    lines=["# M04-013 rear mounting Z=0; +Z aft; static pose",
           "mtllib "+mtl.name]
    offset=0
    for p in parts:
        lines += ["o "+p["name"],"usemtl "+p["material"]]
        lines += ["v %.8f %.8f %.8f"%v for v in p["vertices"]]
        lines += ["f %d %d %d"%(offset+a+1,offset+b+1,offset+c+1)
                  for a,b,c in p["faces"]]
        offset+=len(p["vertices"])
    obj.write_text("\n".join(lines)+"\n",encoding="utf-8")
    mlines=["# Static preview: Blender GLB/FBX contains full PBR/emission"]
    for name,(rgba,metal,rough,emission,power) in MATS.items():
        mlines += ["newmtl "+name,
                   "Kd %.6f %.6f %.6f"%rgba[:3],
                   "Ks %.6f %.6f %.6f"%(metal,metal,metal),
                   "Ns %.3f"%((1-rough)*1000),
                   "Ke %.6f %.6f %.6f"%tuple(min(1,q*power) for q in emission),
                   "d %.3f"%rgba[3],"illum 2",""]
    mtl.write_text("\n".join(mlines)+"\n",encoding="utf-8")
    return [obj.name,mtl.name]

def export_blender(parts,out):
    import bpy
    coll=bpy.data.collections.get("MODEL_M04_013")
    if coll:
        for ob in list(coll.objects):
            bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(coll)
    coll=bpy.data.collections.new("MODEL_M04_013")
    bpy.context.scene.collection.children.link(coll)
    mount=bpy.data.objects.new("MOUNT_REAR_SOCKET",None)
    coll.objects.link(mount)
    rotor=bpy.data.objects.new("ROTOR_Z",None)
    coll.objects.link(rotor)
    rotor.parent=mount
    rotor.location=(0,0,PIVOT_Z)
    mount.empty_display_size=.065
    rotor.empty_display_size=.060
    mats={}
    for name,(rgba,metal,rough,emission,power) in MATS.items():
        key="M04_013_"+name
        mat=bpy.data.materials.get(key)
        if mat is None:
            mat=bpy.data.materials.new(key)
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value=rgba
            bsdf.inputs["Metallic"].default_value=metal
            bsdf.inputs["Roughness"].default_value=rough
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value=(*emission,1)
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value=power
        mat.diffuse_color=rgba
        mats[name]=mat
    for p in parts:
        offset=PIVOT_Z if p["moving"] else 0.
        verts=[(x,y,z-offset) for x,y,z in p["vertices"]]
        mesh=bpy.data.meshes.new("M04_013_"+p["name"])
        mesh.from_pydata(verts,[],p["faces"])
        mesh.update(calc_edges=True)
        ob=bpy.data.objects.new("M04_013_"+p["name"],mesh)
        coll.objects.link(ob)
        ob.parent=rotor if p["moving"] else mount
        mesh.materials.append(mats[p["material"]])
        for poly in mesh.polygons:
            poly.use_smooth=False
    for frame,angle in KEYS:
        rotor.rotation_euler[2]=math.radians(angle)
        rotor.keyframe_insert(data_path="rotation_euler",index=2,frame=frame)
    if rotor.animation_data and rotor.animation_data.action:
        try:
            for curve in rotor.animation_data.action.fcurves:
                for key in curve.keyframe_points:
                    key.interpolation="LINEAR"
        except AttributeError:
            pass # Blender 4.4+ layered action API; sampling still exported.
    scene=bpy.context.scene
    scene.frame_start=1;scene.frame_end=121;scene.render.fps=FPS
    scene.frame_set(1)
    bpy.ops.object.select_all(action="DESELECT")
    for ob in coll.objects:
        ob.select_set(True)
    bpy.context.view_layer.objects.active=mount
    blend=out/"M04-013_ringed_water_propeller.blend"
    glb=out/"M04-013_ringed_water_propeller.glb"
    fbx=out/"M04-013_ringed_water_propeller.fbx"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
        use_selection=True,export_animations=True)
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
        axis_forward="-Z",axis_up="Y",add_leaf_bones=False,
        bake_anim=True,bake_anim_use_all_actions=False)
    assert all(p.exists() and p.stat().st_size>1000 for p in (blend,glb,fbx))
    return dict(export_blender="PASS (files created; Unity not run)",
        blender_files=[p.name for p in (blend,glb,fbx)])

def main():
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M04-013")
    parser.add_argument("--self-test",action="store_true")
    opt=parser.parse_args(args)
    pieces=build();report=validate(pieces)
    if opt.self_test:
        print(json.dumps(report,indent=2));return
    out=Path(opt.out).resolve();out.mkdir(parents=True,exist_ok=True)
    report["static_files"]=export_obj(pieces,out)
    try:
        import bpy
    except ImportError:
        report["backend"]="plain-python-static-obj"
    else:
        report["backend"]="Blender bpy"
        report.update(export_blender(pieces,out))
    (out/"M04-013_manifest.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))

if __name__=="__main__":
    main()
