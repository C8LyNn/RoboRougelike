#!/usr/bin/env python3
"""M03-010 — stepped five-tier segmented frontal armor (visual only).

Unity 6: metres, X = vehicle width, Y = forward/outward normal, Z = up.
Root pivot (0,0,0) on the four mount bores' rear plane, +Y forward.
No gameplay, physics, Unity project or scene modifications.

python generate_m03_010.py --self-test
python generate_m03_010.py --static-obj --out build/M03-010
blender -b --factory-startup --python-exit-code 1 --python generate_m03_010.py -- --out build/M03-010
"""
from __future__ import annotations
import argparse
from collections import Counter
import json
import math
from pathlib import Path
import sys

ID="M03-010"
PREFIX="RRL_M03_010_"
STEM="M03-010_stepped_segment_armor"
COLLECTION=PREFIX+"STEPPED_SEGMENT_ARMOR"
MATERIALS={
    "GraphiteBacker":((.055,.069,.086,1),.69,.38),
    "TitaniumStep":((.25,.31,.36,1),.84,.28),
    "CeramicTile":((.68,.73,.70,1),.22,.41),
    "CopperLip":((.52,.285,.115,1),.77,.30),
    "AmberWarning":((.98,.45,.025,1),.22,.35),
    "TitaniumMount":((.48,.54,.58,1),.86,.25),
}
TIER_Z=(-.28,-.14,0.,.14,.28)
TIER_RISER_FRONT=(.076,.088,.100,.112,.124)
BACKER_Y=(.030,.050)
RISER_BACK=.050
MOUNT_X=.530
MOUNT_Z=.280
MOUNT_OUTER=.038
MOUNT_BORE=.020
MOUNT_POINTS=[(sx*MOUNT_X,0.,sz*MOUNT_Z)
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


def chamfered_prism(name,cx,cz,half_x,half_z,corner,ymin,ymax,
                    material,role):
    """Closed, outward-wound octagonal prism with clipped XZ corners."""
    assert 0<corner<min(half_x,half_z) and ymin<ymax
    contour=[
        (-half_x+corner,-half_z),(half_x-corner,-half_z),
        (half_x,-half_z+corner),(half_x,half_z-corner),
        (half_x-corner,half_z),(-half_x+corner,half_z),
        (-half_x,half_z-corner),(-half_x,-half_z+corner)]
    v=[(cx+x,ymin,cz+z) for x,z in contour]
    v.extend((cx+x,ymax,cz+z) for x,z in contour)
    faces=[]
    def tri(a,b,c,expected):
        n=cross(sub(v[b],v[a]),sub(v[c],v[a]))
        assert dot(n,n)>1e-18,(name,"degenerate",a,b,c)
        if dot(n,expected)<0:
            b,c=c,b
        faces.append((a,b,c))
    for i in range(1,7):
        tri(0,i,i+1,(0,-1,0))
        tri(8,8+i,8+i+1,(0,1,0))
    for i in range(8):
        j=(i+1)%8
        ax,az=contour[i]
        bx,bz=contour[j]
        normal=(bz-az,0,ax-bx)
        tri(i,j,8+j,normal)
        tri(i,8+j,8+i,normal)
    return dict(name=name,vertices=v,faces=faces,material=material,role=role)


def build_parts():
    parts=[]
    add=parts.append
    # Continuous backing has 20mm thickness and supports each of the
    # five disconnected armor tiles on a shared flush mounting plane.
    add(box("Graphite_Backplate",(0,.040,0),(.940,.020,.740),
            "GraphiteBacker","structure"))
    for index,cz in enumerate(TIER_Z):
        face=TIER_RISER_FRONT[index]
        tag="%02d"%(index+1)
        # Main steel riser contacts backer at Y=.050, but its face rises
        # 12mm per successive band, creating a true 3D stair silhouette.
        add(chamfered_prism("Step_Riser_"+tag,0,cz,.435,.060,.014,
                           RISER_BACK,face,"TitaniumStep","armor_support"))
        # Ceramic overlay is inset from the step edges and mounted flush.
        # It occupies the upper part of each segment, not the copper lip.
        add(chamfered_prism("Ceramic_Overlay_"+tag,0,cz+.014,
                           .405,.037,.010,face,face+.013,
                           "CeramicTile","armor_face"))
        # Exposed lower copper strip; separated from ceramic by 11mm
        # in Z. This is a solid inset metal edge, not an emissive decal.
        add(chamfered_prism("Copper_Step_Lip_"+tag,0,cz-.044,
                           .389,.008,.003,face,face+.007,
                           "CopperLip","armor_trim"))
    for sx,label in ((-1,"L"),(1,"R")):
        # Narrow fixed lateral protection rails are separated from all
        # tier prisms by 9mm in X and rest on the graphite backplate.
        add(box("Side_Rail_"+label,(sx*.455,.064,0),
                (.022,.028,.660),"GraphiteBacker","side_rail"))
        for n,z in enumerate((-.250,0.,.250)):
            add(box("Amber_Rail_Marker_"+label+"_"+str(n),
                    (sx*.455,.081,z),(.016,.006,.045),
                    "AmberWarning","accent"))
        for sz,position in ((-1,"B"),(1,"T")):
            tag=label+"_"+position
            # True 20mm-radius through-bored mount at the rear pivot
            # plane; outer ring is supported by a flush bridge.
            add(ring("Mount_Bore_"+tag,sx*MOUNT_X,sz*MOUNT_Z,
                     0,.030,MOUNT_OUTER,MOUNT_BORE,
                     "TitaniumMount","mount"))
            # Bridge touches backing at x=+/-470mm and mount's top
            # surface at Y=30mm. It does not obstruct the bore.
            add(box("Mount_Bridge_"+tag,(sx*.487,.040,sz*MOUNT_Z),
                    (.034,.020,.034),"TitaniumStep","mount_bridge"))
    return parts


def validate(parts):
    assert len(parts)==32,len(parts)
    names=[p["name"] for p in parts]
    assert len(names)==len(set(names))
    assert set(p["material"] for p in parts)==set(MATERIALS)
    total_v=total_f=0
    bounds={}
    for p in parts:
        verts=p["vertices"]
        faces=p["faces"]
        assert verts and faces
        directed=Counter()
        volume=0.
        for a,b,c in faces:
            A,B,C=verts[a],verts[b],verts[c]
            n=cross(sub(B,A),sub(C,A))
            assert dot(n,n)>1e-17,(p["name"],"degenerate")
            volume+=dot(A,cross(B,C))/6
            for i,j in ((a,b),(b,c),(c,a)):
                directed[(i,j)]+=1
        assert all(count==1 and directed.get((j,i))==1
                   for (i,j),count in directed.items()),(
                       p["name"],"nonmanifold/reversed")
        assert volume>1e-12,(p["name"],"inverted/zero volume",volume)
        bounds[p["name"]]=[(min(v[k] for v in verts),
                            max(v[k] for v in verts)) for k in range(3)]
        total_v+=len(verts)
        total_f+=len(faces)
    # Independent solids must only contact on boundary surfaces;
    # no positive 3D AABB overlap between any two components.
    pair_checks=0
    for i,name in enumerate(names):
        for other in names[i+1:]:
            a,b=bounds[name],bounds[other]
            overlap=[min(a[k][1],b[k][1])-max(a[k][0],b[k][0])
                     for k in range(3)]
            assert min(overlap)<=1e-9,(
                name,other,"unintended overlap",overlap)
            pair_checks+=1
    assert pair_checks==496
    backer=bounds["Graphite_Backplate"]
    assert abs(backer[1][1]-RISER_BACK)<1e-9
    for i,cz in enumerate(TIER_Z):
        tag="%02d"%(i+1)
        step=bounds["Step_Riser_"+tag]
        ceramic=bounds["Ceramic_Overlay_"+tag]
        lip=bounds["Copper_Step_Lip_"+tag]
        assert abs(step[1][0]-backer[1][1])<1e-9
        assert abs(step[1][1]-TIER_RISER_FRONT[i])<1e-9
        assert abs(ceramic[1][0]-step[1][1])<1e-9
        assert abs(lip[1][0]-step[1][1])<1e-9
        assert abs(ceramic[1][1]-step[1][1]-.013)<1e-9
        assert abs(lip[1][1]-step[1][1]-.007)<1e-9
        assert ceramic[2][0]-lip[2][1]>=.0109
        assert abs((step[2][0]+step[2][1])/2-cz)<1e-9
    for sx,label in ((-1,"L"),(1,"R")):
        rail=bounds["Side_Rail_"+label]
        assert abs(rail[1][0]-backer[1][1])<1e-9
        for n in range(3):
            marker=bounds["Amber_Rail_Marker_"+label+"_"+str(n)]
            assert abs(marker[1][0]-rail[1][1])<1e-9
        for sz,position in ((-1,"B"),(1,"T")):
            tag=label+"_"+position
            mount=next(p for p in parts if p["name"]=="Mount_Bore_"+tag)
            bridge=bounds["Mount_Bridge_"+tag]
            rim=bounds[mount["name"]]
            assert abs(rim[1][1]-bridge[1][0])<1e-9
            assert abs(bridge[1][1]-backer[1][1])<1e-9
            # Flush backer side contact and physical annulus/bridge
            # overlap in XZ at the shared Y=30mm plane.
            assert abs(bridge[0][0 if sx==1 else 1]-
                       backer[0][1 if sx==1 else 0])<1e-9
            assert (min(rim[0][1],bridge[0][1])-
                    max(rim[0][0],bridge[0][0]))>.010
            radii=[math.hypot(v[0]-sx*MOUNT_X,v[2]-sz*MOUNT_Z)
                   for v in mount["vertices"]]
            assert abs(min(radii)-MOUNT_BORE)<1e-8
            assert abs(max(radii)-MOUNT_OUTER)<1e-8
            # Bridge stays at least 25mm from bore centre in X.
            assert min(abs(bridge[0][0]-sx*MOUNT_X),
                       abs(bridge[0][1]-sx*MOUNT_X))>=.025-1e-9
    return dict(id=ID,result="PASS",parts=len(parts),
                vertices=total_v,triangles=total_f,
                materials=len(MATERIALS),segments=5,
                stepped_riser_depths_m=list(TIER_RISER_FRONT),
                ceramic_overlays=5,copper_lips=5,
                bored_mounts=4,snap_points=4,
                no_volume_overlap_pair_checks=pair_checks,
                static_model=True,animation="NOT REQUIRED",
                scale="1 unit = 1 metre",
                pivot=[0,0,0],mount_normal=[0,1,0],
                bounds_m=[[-.568,0,-.370],[.568,.137,.370]],
                blender="NOT RUN",unity="NOT RUN")


def static_obj(parts,out):
    out.mkdir(parents=True,exist_ok=True)
    obj=out/(STEM+".obj")
    mtl=out/(STEM+".mtl")
    lines=["# M03-010; 1 unit = 1 metre; +Y outward; pivot at (0,0,0)",
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
    # Removing objects does not delete their now-orphaned Mesh datablocks.
    # Clean ONLY this model's unused datablocks, never other scene assets.
    for mesh in tuple(bpy.data.meshes):
        if mesh.name.startswith(PREFIX) and mesh.users==0:
            bpy.data.meshes.remove(mesh)
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
    parser.add_argument("--out",default="./build/M03-010")
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
