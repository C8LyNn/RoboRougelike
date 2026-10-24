#!/usr/bin/env python3
"""M03-009: real perforated side armor and a finer independent wire lattice.

Unity 6 units: metres. X=vehicle longitudinal, Y=outward side normal,
Z=up. Root pivot (0,0,0) on the REAR mounting plane, normal +Y.
Four rear-facing physical through-bored mounting lugs and four SNAP empties.
This is a visual-only static model; it does not change vehicle damage logic.

python generate_m03_009.py --self-test
python generate_m03_009.py --static-obj --out build/M03-009
blender -b --factory-startup --python-exit-code 1 --python generate_m03_009.py -- --out build/M03-009
"""
from __future__ import annotations
import argparse
from collections import Counter
import json
import math
from pathlib import Path
import sys

ID="M03-009"
PREFIX="RRL_M03_009_"
STEM="M03-009_perforated_side_armor"
COLLECTION=PREFIX+"PERFORATED_SIDE_ARMOR"
MATERIALS={
    "GraphiteFrame":((.075,.091,.108,1),.72,.36),
    "PerforatedSteel":((.32,.37,.40,1),.82,.27),
    "CopperMesh":((.46,.28,.16,1),.67,.32),
    "TitaniumMount":((.62,.67,.70,1),.84,.25),
    "AmberWarning":((.98,.42,.035,1),.26,.28),
}
PANEL_X=.390
PANEL_Z=.280
FRONT_Y=(.080,.110)
MESH_Y=(.055,.071)
FRAME_Y=(.035,.110)
LUG_X=.518
LUG_Z=.315
LUG_OUTER=.043
LUG_BORE=.019
MOUNT_POINTS=[(sx*LUG_X,0.,sz*LUG_Z)
              for sx in (-1,1) for sz in (-1,1)]


def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])

def sub(a,b):
    return tuple(x-y for x,y in zip(a,b))

def dot(a,b):
    return sum(x*y for x,y in zip(a,b))

def mesh_builder(name,material,role):
    vertices=[]
    faces=[]
    index={}
    def vertex(p):
        p=tuple(round(float(v),9) for v in p)
        if p not in index:
            index[p]=len(vertices)
            vertices.append(p)
        return index[p]
    def quad(points,normal):
        ids=[vertex(p) for p in points]
        a,b,c=[vertices[i] for i in ids[:3]]
        n=cross(sub(b,a),sub(c,a))
        assert dot(n,n)>1e-16,(name,"zero-area quad",points)
        if dot(n,normal)<0:
            ids.reverse()
        faces.extend(((ids[0],ids[1],ids[2]),
                      (ids[0],ids[2],ids[3])))
    def finish():
        return dict(name=name,vertices=vertices,faces=faces,
                    material=material,role=role)
    return quad,finish


def box(name,centre,size,material,role="structure"):
    x,y,z=centre
    hx,hy,hz=(v/2 for v in size)
    assert min(size)>0
    q,finish=mesh_builder(name,material,role)
    x0,x1=x-hx,x+hx
    y0,y1=y-hy,y+hy
    z0,z1=z-hz,z+hz
    q([(x0,y0,z0),(x0,y0,z1),(x0,y1,z1),(x0,y1,z0)],(-1,0,0))
    q([(x1,y0,z0),(x1,y1,z0),(x1,y1,z1),(x1,y0,z1)],(1,0,0))
    q([(x0,y0,z0),(x1,y0,z0),(x1,y0,z1),(x0,y0,z1)],(0,-1,0))
    q([(x0,y1,z0),(x0,y1,z1),(x1,y1,z1),(x1,y1,z0)],(0,1,0))
    q([(x0,y0,z0),(x0,y1,z0),(x1,y1,z0),(x1,y0,z0)],(0,0,-1))
    q([(x0,y0,z1),(x1,y0,z1),(x1,y1,z1),(x0,y1,z1)],(0,0,1))
    return finish()


def perforated(name,nx,nz,half_x,half_z,opening_x,opening_z,
               ymin,ymax,material):
    """ONE welded, watertight mesh with real rectangular through-holes.

    Each cell is a ring of four quads. Shared outer grid corners are
    indexed once; internal seams have two opposing faces, not doubled
    overlapping walls. Only the outer boundary and hole edges get walls.
    """
    assert 0<opening_x<2*half_x/nx
    assert 0<opening_z<2*half_z/nz
    q,finish=mesh_builder(name,material,"perforated")
    dx=2*half_x/nx
    dz=2*half_z/nz
    for iz in range(nz):
        for ix in range(nx):
            x0=-half_x+ix*dx
            x1=x0+dx
            z0=-half_z+iz*dz
            z1=z0+dz
            cx=(x0+x1)/2
            cz=(z0+z1)/2
            hx=opening_x/2
            hz=opening_z/2
            O=[(x0,z0),(x1,z0),(x1,z1),(x0,z1)]
            H=[(cx-hx,cz-hz),(cx+hx,cz-hz),
               (cx+hx,cz+hz),(cx-hx,cz+hz)]
            for k in range(4):
                j=(k+1)%4
                for y,n in ((ymax,(0,1,0)),(ymin,(0,-1,0))):
                    q([(O[k][0],y,O[k][1]),(O[j][0],y,O[j][1]),
                       (H[j][0],y,H[j][1]),(H[k][0],y,H[k][1])],n)
                normals=((0,0,1),(-1,0,0),(0,0,-1),(1,0,0))
                q([(H[k][0],ymin,H[k][1]),(H[j][0],ymin,H[j][1]),
                   (H[j][0],ymax,H[j][1]),(H[k][0],ymax,H[k][1])],
                  normals[k])
            if iz==0:
                q([(x0,ymin,z0),(x1,ymin,z0),(x1,ymax,z0),(x0,ymax,z0)],
                  (0,0,-1))
            if iz==nz-1:
                q([(x0,ymin,z1),(x0,ymax,z1),(x1,ymax,z1),(x1,ymin,z1)],
                  (0,0,1))
            if ix==0:
                q([(x0,ymin,z0),(x0,ymax,z0),(x0,ymax,z1),(x0,ymin,z1)],
                  (-1,0,0))
            if ix==nx-1:
                q([(x1,ymin,z0),(x1,ymin,z1),(x1,ymax,z1),(x1,ymax,z0)],
                  (1,0,0))
    return finish()


def ring(name,cx,cz,ymin,ymax,outer,inner,material,role="mount"):
    """Closed annular solid with a real unobstructed Y-axis bore."""
    assert 0<inner<outer and ymax>ymin
    q,finish=mesh_builder(name,material,role)
    n=32
    for i in range(n):
        a=2*math.pi*i/n
        b=2*math.pi*(i+1)/n
        def pt(radius,y,theta):
            return (cx+radius*math.cos(theta),y,
                    cz+radius*math.sin(theta))
        for y,normal in ((ymin,(0,-1,0)),(ymax,(0,1,0))):
            q([pt(outer,y,a),pt(outer,y,b),
               pt(inner,y,b),pt(inner,y,a)],normal)
        radial=(math.cos((a+b)/2),0,math.sin((a+b)/2))
        q([pt(outer,ymin,a),pt(outer,ymax,a),
           pt(outer,ymax,b),pt(outer,ymin,b)],radial)
        q([pt(inner,ymin,a),pt(inner,ymin,b),
           pt(inner,ymax,b),pt(inner,ymax,a)],
          tuple(-v for v in radial))
    return finish()


def cylinder(name,cx,cz,ymin,ymax,radius,material):
    q,finish=mesh_builder(name,material,"fastener")
    n=16
    center_bottom=(cx,ymin,cz)
    center_top=(cx,ymax,cz)
    # Cap triangles via a tiny quad-free dedicated face insertion:
    # use a two-radius closed annulus with an inner radius of 1mm instead
    # of zero-radius fan, preserving strict non-degenerate topology.
    return ring(name,cx,cz,ymin,ymax,radius,.001,material,"fastener")


def build_parts():
    parts=[]
    add=parts.append
    # The coarse 6x4 steel plate and finer 12x8 copper-colored backing
    # are independent actual perforated manifolds separated by 9mm.
    add(perforated("Outer_Steel_24_Through_Holes",6,4,PANEL_X,PANEL_Z,
                   .082,.090,*FRONT_Y,"PerforatedSteel"))
    add(perforated("Inner_Mesh_96_Through_Holes",12,8,PANEL_X,PANEL_Z,
                   .041,.042,*MESH_Y,"CopperMesh"))
    # Four non-overlapping mitred-by-contact frame bars. Side bars meet
    # panel edges along x=+/-390mm, upper bars at z=+/-280mm.
    add(box("Frame_Left",(-.430,.0725,0),(.080,.075,.720),"GraphiteFrame"))
    add(box("Frame_Right",(.430,.0725,0),(.080,.075,.720),"GraphiteFrame"))
    add(box("Frame_Top",(0,.0725,.320),(.780,.075,.080),"GraphiteFrame"))
    add(box("Frame_Bottom",(0,.0725,-.320),(.780,.075,.080),"GraphiteFrame"))
    for sx in (-1,1):
        for sz in (-1,1):
            tag=("L" if sx<0 else "R")+("_B" if sz<0 else "_T")
            # Bored collar sits entirely OUTSIDE the panel and frame.
            add(ring("Mount_Bore_"+tag,sx*LUG_X,sz*LUG_Z,0,.035,
                     LUG_OUTER,LUG_BORE,"TitaniumMount"))
            # Bridge contacts frame's x end face and collar's y top face.
            add(box("Mount_Bridge_"+tag,(sx*.494,.055,sz*LUG_Z),
                    (.048,.040,.038),"GraphiteFrame"))
    # Fasteners on the outward frame face; ring-shaped with real recess.
    for sz in (-1,1):
        for ix,x in enumerate((-.26,0.,.26)):
            add(cylinder("Frame_Rivet_%s_%d"%("B" if sz<0 else "T",ix),
                         x,sz*.320,.110,.122,.014,"TitaniumMount"))
    for sx in (-1,1):
        add(cylinder("Frame_Rivet_Side_%s"%("L" if sx<0 else "R"),
                     sx*.430,0.,.110,.122,.014,"TitaniumMount"))
        for sz in (-1,1):
            add(box("Amber_Side_Indicator_%s_%s"%(
                "L" if sx<0 else "R","B" if sz<0 else "T"),
                (sx*.430,.113,sz*.205),(.034,.006,.052),"AmberWarning",
                role="accent"))
    return parts


def validate(parts):
    assert len(parts)==26,len(parts)
    names=[p["name"] for p in parts]
    assert len(names)==len(set(names))
    assert set(p["material"] for p in parts)==set(MATERIALS)
    total_v=total_f=0
    for p in parts:
        v=p["vertices"]
        f=p["faces"]
        assert v and f
        edges=Counter()
        volume=0.
        for tri in f:
            a,b,c=[v[i] for i in tri]
            n=cross(sub(b,a),sub(c,a))
            assert dot(n,n)>1e-17,(p["name"],"degenerate",tri)
            volume+=dot(a,cross(b,c))/6
            for u,w in ((tri[0],tri[1]),(tri[1],tri[2]),(tri[2],tri[0])):
                edges[(u,w)]+=1
        assert all(count==1 and edges.get((w,u))==1
                   for (u,w),count in edges.items()),(p["name"],"open or reversed")
        assert volume>1e-12,(p["name"],"negative volume",volume)
        total_v+=len(v)
        total_f+=len(f)
    # Conservative AABB broad-phase collision gate. Separate visual
    # components are assembled by flush contacts, never by interpenetrating
    # volumes. This deliberately rejects positive overlap on all 3 axes.
    boxes=[]
    for p in parts:
        bounds=[(min(v[k] for v in p["vertices"]),
                 max(v[k] for v in p["vertices"])) for k in range(3)]
        boxes.append((p["name"],bounds))
    pair_checks=0
    for i,(name_a,a) in enumerate(boxes):
        for name_b,b in boxes[i+1:]:
            overlap=[min(a[k][1],b[k][1])-max(a[k][0],b[k][0])
                     for k in range(3)]
            assert min(overlap)<=1e-8,(
                "unintended volumetric overlap",name_a,name_b,overlap)
            pair_checks+=1
    assert pair_checks==325
    assert sum("Mount_Bore_" in n for n in names)==4
    assert sum("Mount_Bridge_" in n for n in names)==4
    assert sum("Frame_Rivet_" in n for n in names)==8
    assert sum("Amber_Side_Indicator_" in n for n in names)==4
    assert all(abs(x)==LUG_X and y==0 and abs(z)==LUG_Z
               for x,y,z in MOUNT_POINTS)
    # Mount bore is physically open along the entire 35mm Y extrusion.
    # Neither the panel nor frame reaches the lug centre at |X|=.518.
    for sx in (-1,1):
        for sz in (-1,1):
            lug=next(p for p in parts if p["name"]==
                     "Mount_Bore_"+("L" if sx<0 else "R")+
                     ("_B" if sz<0 else "_T"))
            radial=[math.hypot(v[0]-sx*LUG_X,v[2]-sz*LUG_Z)
                    for v in lug["vertices"]]
            assert abs(min(radial)-LUG_BORE)<1e-8
            assert abs(max(radial)-LUG_OUTER)<1e-8
    # Two separate perforated sheets, no volume overlap, 9mm air gap.
    assert MESH_Y[1]<FRONT_Y[0]
    assert abs(FRONT_Y[0]-MESH_Y[1]-.009)<1e-8
    return dict(id=ID,result="PASS",parts=len(parts),vertices=total_v,
                triangles=total_f,materials=len(MATERIALS),
                outer_real_through_holes=24,inner_real_through_holes=96,
                through_bored_mounts=4,snap_points=len(MOUNT_POINTS),
                no_volume_overlap_pair_checks=pair_checks,
                sheet_air_gap_m=.009,static_model=True,
                scale="1 unit = 1 metre",
                pivot=[0,0,0],mount_normal=[0,1,0],
                bounds_m=[[-.561,0,-.360],[.561,.122,.360]],
                blender="NOT RUN",unity="NOT RUN")


def static_obj(parts,out):
    out.mkdir(parents=True,exist_ok=True)
    obj=out/(STEM+".obj")
    mtl=out/(STEM+".mtl")
    lines=["# M03-009; 1 unit = 1 metre; +Y outward; pivot at (0,0,0)",
           "mtllib "+mtl.name]
    offset=1
    for p in parts:
        lines.extend(["o "+p["name"],"usemtl "+p["material"],"s off"])
        lines.extend("v %.9f %.9f %.9f"%v for v in p["vertices"])
        lines.extend("f %d %d %d"%(a+offset,b+offset,c+offset)
                     for a,b,c in p["faces"])
        offset+=len(p["vertices"])
    obj.write_text("\n".join(lines)+"\n",encoding="utf-8")
    matlines=[]
    for name,(rgba,metallic,rough) in MATERIALS.items():
        matlines.extend(["newmtl "+name,
                         "Kd %.6f %.6f %.6f"%tuple(rgba[:3]),
                         "Ks %.6f %.6f %.6f"%(metallic,metallic,metallic),
                         "Ns %.6f"%((1-rough)*100),
                         "illum 2",""])
    mtl.write_text("\n".join(matlines)+"\n",encoding="utf-8")
    return dict(backend="python-stdlib",blender="NOT RUN",unity="NOT RUN",
                obj=obj.name,mtl=mtl.name)


def blender_export(parts,out):
    import bpy
    out.mkdir(parents=True,exist_ok=True)
    # Only clean this agent's own collection; never clear a shared scene.
    old=bpy.data.collections.get(COLLECTION)
    if old is not None:
        for ob in tuple(old.objects):
            bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(old)
    collection=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(collection)
    root=bpy.data.objects.new(PREFIX+"ROOT_PIVOT",None)
    collection.objects.link(root)
    root.empty_display_type="PLAIN_AXES"
    root.empty_display_size=.12
    mats={}
    for name,(rgba,metallic,rough) in MATERIALS.items():
        full=PREFIX+name
        mat=bpy.data.materials.get(full)
        if mat is None:
            mat=bpy.data.materials.new(full)
        mat.diffuse_color=rgba
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value=rgba
            bsdf.inputs["Metallic"].default_value=metallic
            bsdf.inputs["Roughness"].default_value=rough
        mats[name]=mat
    for p in parts:
        mesh=bpy.data.meshes.new(PREFIX+p["name"]+"_MESH")
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.update(calc_edges=True)
        obj=bpy.data.objects.new(PREFIX+p["name"],mesh)
        collection.objects.link(obj)
        obj.parent=root
        obj.data.materials.append(mats[p["material"]])
        obj["M03_ID"]=ID
        obj["role"]=p["role"]
        for poly in mesh.polygons:
            poly.use_smooth=False
    for i,(x,y,z) in enumerate(MOUNT_POINTS):
        empty=bpy.data.objects.new(PREFIX+"SNAP_%02d"%i,None)
        collection.objects.link(empty)
        empty.parent=root
        empty.location=(x,y,z)
        empty.empty_display_type="ARROWS"
        empty.empty_display_size=.045
        empty["mount_axis"]="-Y"
        empty["bore_radius_m"]=LUG_BORE
    selected_before=tuple(bpy.context.selected_objects)
    active_before=bpy.context.view_layer.objects.active
    try:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in collection.objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active=root
        glb=out/(STEM+".glb")
        fbx=out/(STEM+".fbx")
        blend=out/(STEM+".blend")
        bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
                                  use_selection=True,export_animations=False)
        bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
                                 axis_forward="-Z",axis_up="Y",
                                 add_leaf_bones=False,bake_anim=False)
        bpy.ops.wm.save_as_mainfile(filepath=str(blend))
        assert all(p.exists() and p.stat().st_size>1024
                   for p in (glb,fbx,blend))
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in selected_before:
            if bpy.data.objects.get(ob.name) is ob:
                ob.select_set(True)
        if active_before and bpy.data.objects.get(active_before.name) is active_before:
            bpy.context.view_layer.objects.active=active_before
    return dict(backend="blender-bpy",blender="EXPORTED",unity="NOT RUN",
                glb=glb.name,fbx=fbx.name,blend=blend.name)


def main():
    args_in=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="./build/M03-009")
    parser.add_argument("--self-test",action="store_true")
    parser.add_argument("--static-obj",action="store_true")
    args=parser.parse_args(args_in)
    parts=build_parts()
    report=validate(parts)
    if args.self_test:
        print(json.dumps(report,indent=2))
        return
    out=Path(args.out).resolve()
    if args.static_obj:
        result=static_obj(parts,out)
    else:
        try:
            import bpy  # noqa: F401
        except ImportError:
            result=static_obj(parts,out)
        else:
            result=blender_export(parts,out)
    report.update(result)
    out.mkdir(parents=True,exist_ok=True)
    (out/(STEM+"_manifest.json")).write_text(
        json.dumps(report,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2,ensure_ascii=False))


if __name__=="__main__":
    main()
