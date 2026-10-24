#!/usr/bin/env python3
"""M01-009 / Trójsegmentowa wyrzutnia rakiet — visual-only Blender model.

Unity convention: metres, +Y up, +Z muzzle/forward, +X right.
(0,0,0) is the centre of the physical top-mount socket at its lower face.
Three independent front hatches rotate about local X, -105 degrees outward.
Only this model's dedicated Blender collection is rebuilt on reruns.

Plain Python creates deterministic OBJ, MTL and JSON. With --blender,
run under Blender (bpy required) to also produce .blend, animated .glb/.fbx.
No gameplay logic, Unity scenes, prefabs or ProjectSettings are touched.
"""
import argparse
import json
import math
from pathlib import Path
import sys

ID = "M01-009"
STEM = "M01-009_tri_segment_rocket_launcher"
COLLECTION = "MODEL_M01_009"
LID_GROUPS = ("LID_A", "LID_B", "LID_C")
CELL_X = (-.44, 0., .44)
HINGE_Y, HINGE_Z = .590, .820
OPEN_DEG = -105.
MATERIALS = {
    "Graphite": ((.075,.091,.115),.39,.36,False),
    "Titanium": ((.37,.42,.46),.73,.26,False),
    "Ivory_Ceramic": ((.83,.85,.80),.10,.34,False),
    "Oxidized_Copper": ((.46,.23,.12),.72,.32,False),
    "Rubber": ((.035,.043,.049),.02,.88,False),
    "Signal_Cyan": ((.025,.73,.85),.12,.22,True),
    "Safety_Amber": ((.95,.47,.06),.28,.34,False),
    "Warhead_Red": ((.56,.07,.06),.34,.33,False),
}
PARTS = []


def add(name, material, verts, faces, group=None):
    assert name and material in MATERIALS
    assert group is None or group in LID_GROUPS
    assert all(len(v)==3 and all(math.isfinite(c) for c in v) for v in verts)
    PARTS.append(dict(name=name,material=material,verts=verts,faces=faces,group=group))


def prism(name, material, x,y,z, sx,sy,sz, bevel=0., group=None):
    """Manifold XY-chamfered extruded prism; bevel is in metres."""
    assert min(sx,sy,sz)>0 and 0<=bevel<min(sx,sy)/2
    hx,hy,hz=sx/2,sy/2,sz/2
    if bevel:
        xy=[(-hx+bevel,-hy),(hx-bevel,-hy),(hx,-hy+bevel),
            (hx,hy-bevel),(hx-bevel,hy),(-hx+bevel,hy),
            (-hx,hy-bevel),(-hx,-hy+bevel)]
    else:
        xy=[(-hx,-hy),(hx,-hy),(hx,hy),(-hx,hy)]
    n=len(xy)
    verts=[(x+a,y+b,z-hz) for a,b in xy]+[(x+a,y+b,z+hz) for a,b in xy]
    faces=[]
    for i in range(1,n-1):
        faces.extend(((0,i+1,i),(n,n+i,n+i+1)))
    for i in range(n):
        j=(i+1)%n
        faces.extend(((i,j,n+j),(i,n+j,n+i)))
    add(name,material,verts,faces,group)


def square_ring(name, material, x,y,z, outer=.405, inner=.315, depth=.065):
    """A single closed square annulus: the central bore is genuinely empty."""
    assert outer>inner>0 and depth>0
    def corners(half,zz):
        return [(x-half,y-half,zz),(x+half,y-half,zz),
                (x+half,y+half,zz),(x-half,y+half,zz)]
    v=(corners(outer/2,z-depth/2)+corners(outer/2,z+depth/2)
       +corners(inner/2,z-depth/2)+corners(inner/2,z+depth/2))
    faces=[]
    for i in range(4):
        j=(i+1)%4
        # Outer and inward-facing bore walls.
        faces.extend(((i,j,j+4),(i,j+4,i+4),
                      (i+8,j+12,j+8),(i+8,i+12,j+12)))
        # Front (+Z) and rear (-Z) annular strips.
        faces.extend(((i+4,j+4,j+12),(i+4,j+12,i+12),
                      (i,j+8,j),(i,i+8,j+8)))
    add(name,material,v,faces)


def cylinder(name,material,x,y,z,radius,length,n=20,axis="Z",group=None):
    assert radius>0 and length>0 and n>=8
    circ=[(radius*math.cos(2*math.pi*i/n),
           radius*math.sin(2*math.pi*i/n)) for i in range(n)]
    verts=[(a,b,-length/2) for a,b in circ]+[(a,b,length/2) for a,b in circ]
    verts.extend(((0.,0.,-length/2),(0.,0.,length/2)))
    faces=[]
    for i in range(n):
        j=(i+1)%n
        faces.extend(((i,j,j+n),(i,j+n,i+n),
                      (2*n,j,i),(2*n+1,i+n,j+n)))
    if axis=="X":
        verts=[(zv,yv,-xv) for xv,yv,zv in verts]
    else:
        assert axis=="Z"
    add(name,material,[(x+a,y+b,z+c) for a,b,c in verts],faces,group)


def cone(name,material,x,y,z,radius,length,n=20):
    assert radius>0 and length>0
    v=[(x+radius*math.cos(2*math.pi*i/n),
        y+radius*math.sin(2*math.pi*i/n),z-length/2) for i in range(n)]
    v.extend(((x,y,z-length/2),(x,y,z+length/2)))
    faces=[]
    for i in range(n):
        j=(i+1)%n
        faces.extend(((i,j,n+1),(n,j,i)))
    add(name,material,v,faces)


def build():
    PARTS.clear()
    # Physically continuous top socket: underside at y=0.005, upper face
    # at y=0.070 touches the lower chassis rail. Origin is mount pivot.
    prism("MOUNT_TOP_SOCKET_PLATE","Titanium",0,.0375,.295,
          .56,.065,.42,.024)
    prism("MOUNT_GASKET","Rubber",0,.009,.295,.48,.008,.34,.002)
    for i,x in enumerate((-.23,.23)):
        for j,z in enumerate((.145,.445)):
            cylinder("Mount_Bolt_%d_%d"%(i,j),"Oxidized_Copper",
                     x,.071,z,.021,.014,12,axis="Z")
    # Rear bulkhead, skeleton beams and three load-bearing separators.
    prism("Rear_Bulkhead","Graphite",0,.330,.065,1.42,.50,.135,.035)
    prism("Bottom_Load_Beam","Titanium",0,.114,.415,1.40,.088,.71,.024)
    prism("Top_Load_Beam","Graphite",0,.565,.410,1.40,.090,.71,.024)
    for side in (-1,1):
        prism("Side_Frame_"+str(side),"Graphite",side*.681,.345,.423,
              .082,.45,.70,.020)
        prism("Ceramic_Side_Armor_"+str(side),"Ivory_Ceramic",
              side*.737,.348,.390,.038,.315,.55,.010)
        prism("Copper_Side_Accent_"+str(side),"Oxidized_Copper",
              side*.761,.410,.390,.012,.022,.47,.004)
        prism("Longitudinal_Stiffener_"+str(side),"Titanium",
              side*.626,.165,.438,.038,.090,.66,.010)
    for i,x in enumerate((-.22,.22)):
        prism("Cell_Separator_"+str(i),"Titanium",x,.345,.430,
              .060,.374,.660,.014)
    # Three independent square rocket ports. Recessed inner frames and
    # coloured reticles make each of the three square targets legible.
    for i,x in enumerate(CELL_X):
        tag="ABC"[i]
        square_ring("Square_Port_"+tag,"Titanium",x,.345,.741,
                    outer=.405,inner=.310,depth=.067)
        square_ring("Inner_Ceramic_Lip_"+tag,"Ivory_Ceramic",
                    x,.345,.790,outer=.347,inner=.302,depth=.022)
        prism("Rocket_Cradle_"+tag,"Rubber",x,.185,.405,
              .235,.033,.470,.010)
        cylinder("Rocket_Casing_"+tag,"Graphite",x,.346,.375,
                 .079,.355,20)
        cylinder("Rocket_Band_"+tag,"Oxidized_Copper",x,.346,.475,
                 .081,.037,20)
        cone("Rocket_Ceramic_Nose_"+tag,"Ivory_Ceramic",x,.346,.620,
             .079,.180,20)
        for side in (-1,1):
            prism("Rocket_Fin_"+tag+str(side),"Safety_Amber",
                  x+side*.084,.344,.289,.045,.028,.104,.006)
        # Hinges are stationary; lids and their detailing follow the
        # three independent hinge empties (local X rotation).
        cylinder("Hinge_Pin_"+tag,"Oxidized_Copper",
                 x,HINGE_Y,HINGE_Z,.015,.367,16,axis="X")
        for side in (-1,1):
            prism("Hinge_Bracket_"+tag+str(side),"Titanium",
                  x+side*.168,.591,.775,.034,.048,.081,.007)
        group=LID_GROUPS[i]
        prism("Opening_Hatch_"+tag,"Ivory_Ceramic",
              x,.409,HINGE_Z,.335,.324,.030,.022,group)
        prism("Hatch_Inner_Armor_"+tag,"Graphite",
              x,.414,.840,.278,.250,.013,.016,group)
        # Three distinct square aim-markers on the front of the hatches.
        for side in (-1,1):
            prism("Target_Vertical_"+tag+str(side),"Signal_Cyan",
                  x+side*.101,.412,.849,.014,.177,.012,.004,group)
            prism("Target_Horizontal_"+tag+str(side),"Signal_Cyan",
                  x,.412+side*.085,.849,.205,.014,.012,.004,group)
        prism("Target_Centre_"+tag,"Safety_Amber",
              x,.412,.857,.046,.046,.014,.005,group)
        prism("Latch_Tab_"+tag,"Oxidized_Copper",
              x,.252,.846,.104,.024,.027,.005,group)
        for side in (-1,1):
            prism("Hatch_Rivet_"+tag+str(side),"Titanium",
                  x+side*.134,.542,.849,.018,.018,.012,.004,group)
    # Service rails and small dorsal recognition panel, behind the doors.
    prism("Dorsal_Spine","Titanium",0,.637,.345,.920,.055,.410,.016)
    for i,x in enumerate(CELL_X):
        prism("Dorsal_Identifier_"+str(i),"Safety_Amber",
              x,.674,.350,.168,.015,.084,.005)
    assert len({p["name"] for p in PARTS})==len(PARTS)
    return PARTS


def volume_and_edges(part):
    verts=part["verts"]
    directed={}
    volume=0.
    for a,b,c in part["faces"]:
        A,B,C=verts[a],verts[b],verts[c]
        ux,uy,uz=(B[k]-A[k] for k in range(3))
        vx,vy,vz=(C[k]-A[k] for k in range(3))
        nx,ny,nz=uy*vz-uz*vy,uz*vx-ux*vz,ux*vy-uy*vx
        assert nx*nx+ny*ny+nz*nz>1e-16,("degenerate",part["name"])
        volume+=(A[0]*nx+A[1]*ny+A[2]*nz)/6.
        for i,j in ((a,b),(b,c),(c,a)):
            directed[(i,j)]=directed.get((i,j),0)+1
    for (a,b),n in directed.items():
        assert n==1 and directed.get((b,a))==1,("nonmanifold",part["name"],a,b,n)
    assert volume>1e-9,("inverted volume",part["name"],volume)
    return volume


def rotated(v,pivot,theta):
    x,y,z=v
    px,py,pz=pivot
    yy=y-py
    zz=z-pz
    return (x,py+yy*math.cos(theta)-zz*math.sin(theta),
            pz+yy*math.sin(theta)+zz*math.cos(theta))


def aabb(vertices):
    return tuple((min(v[k] for v in vertices),max(v[k] for v in vertices))
                 for k in range(3))


def overlap(a,b):
    return all(a[k][0]<b[k][1]-1e-7 and b[k][0]<a[k][1]-1e-7
               for k in range(3))


def validate(parts):
    for p in parts:
        volume_and_edges(p)
    groups={g:[] for g in LID_GROUPS}
    fixed=[]
    for p in parts:
        if p["group"] is None:
            # Hinge pin/brackets intentionally contact their moving doors.
            # They are explicit joints, not clearance obstacles.
            if not p["name"].startswith(("Hinge_Pin_","Hinge_Bracket_")):
                fixed.append((p["name"],aabb(p["verts"])))
        else:
            groups[p["group"]].append(p)
    assert all(len(g)>=8 for g in groups.values())
    # Independent hatch groups are on separated X intervals.
    samples=0
    checks=0
    min_front_gap=float("inf")
    for i,(group,x) in enumerate(zip(LID_GROUPS,CELL_X)):
        pivot=(x,HINGE_Y,HINGE_Z)
        for step in range(43):
            theta=math.radians(OPEN_DEG)*step/42
            samples+=1
            for p in groups[group]:
                b=aabb([rotated(v,pivot,theta) for v in p["verts"]])
                for fixed_name,fb in fixed:
                    checks+=1
                    assert not overlap(b,fb),(group,p["name"],fixed_name,step)
                # All non-joint fixed front faces stop at z=0.801,
                # while opening hatch and reticle always swing forward.
                if p["name"].startswith("Opening_Hatch_"):
                    min_front_gap=min(min_front_gap,b[2][0]-.801)
    assert samples==129
    assert min_front_gap>0
    return dict(model=ID,status="PASS",parts=len(parts),
                vertices=sum(len(p["verts"]) for p in parts),
                triangles=sum(len(p["faces"]) for p in parts),
                materials=len({p["material"] for p in parts}),
                moving_hatches=3,poses_checked=samples,
                aabb_pairs_checked=checks,
                minimum_hatch_front_margin_m=round(min_front_gap,6),
                mount_pivot_m=[0,0,0],muzzle_axis="+Z",
                hinge_axis="local X",max_open_degrees=OPEN_DEG,
                geometry="MANIFOLD_POSITIVE_VOLUME",
                blender="NOT RUN",unity="NOT RUN")


def write_static(out,parts,report):
    out.mkdir(parents=True,exist_ok=True)
    mtl=["# M01-009 deterministic material library"]
    for name,(rgb,metal,rough,emission) in MATERIALS.items():
        mtl+=["newmtl "+name,"Kd "+" ".join("%.6f"%v for v in rgb),
              "Ka 0.000000 0.000000 0.000000",
              "Ks %.6f %.6f %.6f"%(metal,metal,metal),
              "Ns %.3f"%((1.-rough)*500.),
              "illum 2",""]
    (out/(STEM+".mtl")).write_text("\n".join(mtl)+"\n")
    obj=["# M01-009 static closed hatches; 1 unit = 1 metre",
         "mtllib "+STEM+".mtl"]
    idx=1
    for p in parts:
        obj+=["o "+p["name"],"usemtl "+p["material"]]
        for v in p["verts"]:
            obj.append("v %.8f %.8f %.8f"%v)
        for face in p["faces"]:
            obj.append("f "+" ".join(str(idx+i) for i in face))
        idx+=len(p["verts"])
    (out/(STEM+".obj")).write_text("\n".join(obj)+"\n")
    (out/(ID+"_qa.json")).write_text(json.dumps(report,indent=2,ensure_ascii=False)+"\n")


def build_blender(out,parts,report):
    try:
        import bpy
    except ImportError as exc:
        raise RuntimeError("bpy is unavailable; Blender export NOT RUN") from exc
    # Idempotent: remove only our own collection and objects.
    old=bpy.data.collections.get(COLLECTION)
    if old:
        for ob in tuple(old.objects):
            bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(old)
    collection=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(collection)
    mats={}
    for name,(rgb,metal,rough,emission) in MATERIALS.items():
        mat=bpy.data.materials.get("M01_009_"+name)
        if mat is None:
            mat=bpy.data.materials.new("M01_009_"+name)
        mat.diffuse_color=(*rgb,1.)
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value=(*rgb,1.)
        bsdf.inputs["Metallic"].default_value=metal
        bsdf.inputs["Roughness"].default_value=rough
        if emission:
            bsdf.inputs["Emission Color"].default_value=(*rgb,1.)
            bsdf.inputs["Emission Strength"].default_value=2.0
        else:
            bsdf.inputs["Emission Strength"].default_value=0.
        mats[name]=mat
    mount=bpy.data.objects.new("MOUNT_TOP_SOCKET",None)
    collection.objects.link(mount)
    mount.empty_display_type="ARROWS"
    pivots={}
    for group,x in zip(LID_GROUPS,CELL_X):
        ob=bpy.data.objects.new(group+"_HINGE_X",None)
        collection.objects.link(ob)
        ob.location=(x,HINGE_Y,HINGE_Z)
        ob.rotation_mode="XYZ"
        ob.parent=mount
        pivots[group]=ob
    for p in parts:
        group=p["group"]
        pivot=(CELL_X[LID_GROUPS.index(group)],HINGE_Y,HINGE_Z) if group else (0.,0.,0.)
        vertices=[tuple(v[k]-pivot[k] for k in range(3)) for v in p["verts"]]
        mesh=bpy.data.meshes.new(p["name"]+"_Mesh")
        mesh.from_pydata(vertices,[],p["faces"])
        mesh.update(calc_edges=True)
        mesh.materials.append(mats[p["material"]])
        ob=bpy.data.objects.new(p["name"],mesh)
        collection.objects.link(ob)
        ob.parent=pivots[group] if group else mount
        for poly in mesh.polygons:
            poly.use_smooth=False
    scene=bpy.context.scene
    old_state=(scene.frame_current,scene.frame_start,scene.frame_end,
               scene.render.fps,tuple(bpy.context.selected_objects),
               bpy.context.view_layer.objects.active)
    try:
        scene.frame_start=1
        scene.frame_end=72
        scene.render.fps=60
        for i,group in enumerate(LID_GROUPS):
            ob=pivots[group]
            offset=i*5
            keys=[(1,0.),(8+offset,0.),(24+offset,OPEN_DEG),
                  (40+offset,OPEN_DEG),(57+offset,0.),(72,0.)]
            for frame,angle in keys:
                ob.rotation_euler=(math.radians(angle),0.,0.)
                ob.keyframe_insert(data_path="rotation_euler",index=0,
                                   frame=frame,group="Hatch_open")
            if ob.animation_data and ob.animation_data.action:
                for fc in ob.animation_data.action.fcurves:
                    for key in fc.keyframe_points:
                        key.interpolation="BEZIER"
            ob.rotation_euler=(0.,0.,0.)
        scene.frame_set(1)
        bpy.ops.object.select_all(action="DESELECT")
        for ob in collection.objects:
            ob.select_set(True)
        bpy.context.view_layer.objects.active=mount
        bpy.ops.wm.save_as_mainfile(filepath=str(out/(STEM+".blend")))
        bpy.ops.export_scene.gltf(filepath=str(out/(STEM+"_animated.glb")),
                                  export_format="GLB",use_selection=True,
                                  export_animations=True)
        bpy.ops.export_scene.fbx(filepath=str(out/(STEM+"_animated.fbx")),
                                 use_selection=True,axis_forward="-Z",
                                 axis_up="Y",add_leaf_bones=False,
                                 bake_anim=True)
        for ext in (".blend","_animated.glb","_animated.fbx"):
            assert (out/(STEM+ext)).is_file()
        report["blender"]="EXPORTED"
        report["blender_version"]=bpy.app.version_string
        report["animation_frames"]=72
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in old_state[4]:
            if bpy.data.objects.get(ob.name) is ob:
                ob.select_set(True)
        if old_state[5] and bpy.data.objects.get(old_state[5].name) is old_state[5]:
            bpy.context.view_layer.objects.active=old_state[5]
        scene.frame_start,scene.frame_end,scene.render.fps=old_state[1:4]
        scene.frame_set(old_state[0])


def main():
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",type=Path,default=Path("build/M01-009"))
    parser.add_argument("--blender",action="store_true")
    parser.add_argument("--self-test",action="store_true")
    cfg=parser.parse_args(args)
    parts=build()
    report=validate(parts)
    if not cfg.self_test:
        cfg.out.mkdir(parents=True,exist_ok=True)
        write_static(cfg.out,parts,report)
        if cfg.blender:
            build_blender(cfg.out,parts,report)
            (cfg.out/(ID+"_qa.json")).write_text(
                json.dumps(report,indent=2,ensure_ascii=False)+"\n")
    print(json.dumps(report,ensure_ascii=False,sort_keys=True))


if __name__=="__main__":
    main()
