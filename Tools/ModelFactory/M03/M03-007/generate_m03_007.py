#!/usr/bin/env python3
"""M03-007: vertical T bracket, deterministic OBJ/MTL and optional Blender GLB/BLEND.

1 unit = 1 metre. Pivot (0,0,0) is the top-of-chassis mount centre; mount
normal +Z, visible front -Y. Re-runs replace only MODEL_M03_007 collection.
"""
from __future__ import annotations
import argparse
from collections import Counter
import json
import math
from pathlib import Path
import sys

NAME="M03-007_vertical_T_bracket"
COLLECTION="MODEL_M03_007"
MATERIALS={
    "Frame_Graphite":((.13,.16,.20,1),.75,.42),
    "Brushed_Titanium":((.57,.63,.69,1),.85,.31),
    "Ceramic_Ivory":((.79,.82,.77,1),.24,.33),
    "Copper_Fasteners":((.64,.38,.23,1),.82,.37),
    "Amber_Indicator":((.98,.59,.11,1),.31,.32),
}
BOLT_X=(-.34,.34)
BORE_R=.028
SHAFT_R=.019
SEGMENTS=32

def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def sub(a,b):return tuple(x-y for x,y in zip(a,b))
def dot(a,b):return sum(x*y for x,y in zip(a,b))
def volume(v,f):return sum(dot(v[a],cross(v[b],v[c]))/6 for a,b,c in f)
def solid(name,material,vertices,faces,category="structure"):
    if volume(vertices,faces)<0:faces=[(a,c,b) for a,b,c in faces]
    return dict(name=name,material=material,vertices=vertices,faces=faces,category=category)
def prism_xz(name,outline,y0,y1,material,category="structure"):
    assert len(outline)>=3 and y1>y0
    n=len(outline)
    vertices=[(x,y0,z) for x,z in outline]+[(x,y1,z) for x,z in outline]
    faces=[]
    for i in range(1,n-1):
        faces.extend(((0,i+1,i),(n,n+i,n+i+1)))
    for i in range(n):
        j=(i+1)%n
        faces.extend(((i,j,n+j),(i,n+j,n+i)))
    return solid(name,material,vertices,faces,category)
def chamfer_rect(cx,cz,hx,hz,c):
    assert 0<c<min(hx,hz)
    return [(cx-hx+c,cz-hz),(cx+hx-c,cz-hz),
            (cx+hx,cz-hz+c),(cx+hx,cz+hz-c),
            (cx+hx-c,cz+hz),(cx-hx+c,cz+hz),
            (cx-hx,cz+hz-c),(cx-hx,cz-hz+c)]
def annulus(name,axis,center,lo,hi,outer,inner,material):
    """Genuine through-bore, not a texture or a hidden filled cylinder."""
    assert axis in ("Y","Z") and hi>lo and outer>inner>0
    cx,cy,cz=center
    def pt(ax,r,t):
        return (cx+r*math.cos(t),cy+r*math.sin(t),ax) if axis=="Z" else (
                cx+r*math.cos(t),ax,cz+r*math.sin(t))
    profile=[(lo,inner),(lo,outer),(hi,outer),(hi,inner)]
    vertices=[pt(a,r,2*math.pi*j/SEGMENTS) for a,r in profile
              for j in range(SEGMENTS)]
    faces=[]
    for i in range(4):
        k=(i+1)%4
        for j in range(SEGMENTS):
            t=(j+1)%SEGMENTS
            a,b=i*SEGMENTS+j,i*SEGMENTS+t
            c,d=k*SEGMENTS+t,k*SEGMENTS+j
            faces.extend(((a,b,c),(a,c,d)))
    return solid(name,material,vertices,faces,"bored_mount")
def cylinder(name,axis,center,lo,hi,r,material,segments=SEGMENTS):
    assert axis in ("Y","Z") and hi>lo and r>0
    cx,cy,cz=center
    def pt(ax,t):
        return (cx+r*math.cos(t),cy+r*math.sin(t),ax) if axis=="Z" else (
                cx+r*math.cos(t),ax,cz+r*math.sin(t))
    vertices=[pt(ax,2*math.pi*j/segments) for ax in (lo,hi)
              for j in range(segments)]
    vertices.extend([(cx,cy,lo) if axis=="Z" else (cx,lo,cz),
                     (cx,cy,hi) if axis=="Z" else (cx,hi,cz)])
    faces=[]
    for j in range(segments):
        k=(j+1)%segments
        faces.extend(((j,k,segments+k),(j,segments+k,segments+j),
                      (2*segments,k,j),(2*segments+1,segments+j,segments+k)))
    return solid(name,material,vertices,faces,"fastener")

def build():
    parts=[];add=parts.append
    # Both mounting beams terminate before bolt bores; bosses overlap the
    # beams outside their bores. Four shafts have 9 mm radial clearance.
    add(prism_xz("Foot_Crossbar",chamfer_rect(0,.056,.289,.039,.013),
                 -.118,.118,"Frame_Graphite"))
    for i,x in enumerate(BOLT_X):
        add(annulus(f"Foot_Boss_Bored_{i}","Z",(x,0,0),
                    .014,.097,.081,BORE_R,"Brushed_Titanium"))
        add(cylinder(f"Foot_Bolt_Shaft_{i}","Z",(x,0,0),
                     .018,.107,SHAFT_R,"Copper_Fasteners"))
        add(cylinder(f"Foot_Bolt_Head_{i}","Z",(x,0,0),
                     .094,.123,.045,"Copper_Fasteners",12))
    add(prism_xz("Vertical_Web",chamfer_rect(0,.335,.090,.270,.022),
                 -.080,.080,"Frame_Graphite"))
    add(prism_xz("Upper_T_Crossbar",chamfer_rect(0,.658,.289,.068,.019),
                 -.108,.108,"Frame_Graphite"))
    for i,x in enumerate(BOLT_X):
        add(annulus(f"Top_Boss_Bored_{i}","Y",(x,0,.657),
                    -.117,.117,.082,BORE_R,"Brushed_Titanium"))
        add(cylinder(f"Top_Bolt_Shaft_{i}","Y",(x,0,.657),
                     -.115,.115,SHAFT_R,"Copper_Fasteners"))
        add(cylinder(f"Top_Bolt_Head_{i}","Y",(x,0,.657),
                     -.145,-.114,.043,"Copper_Fasteners",12))
    add(prism_xz("Gusset_Left",[(-.260,.588),(-.076,.445),(-.076,.600)],
                 -.070,.070,"Brushed_Titanium"))
    add(prism_xz("Gusset_Right",[(.260,.588),(.076,.600),(.076,.445)],
                 -.070,.070,"Brushed_Titanium"))
    add(prism_xz("Ceramic_Stem_Cheek",chamfer_rect(0,.355,.058,.158,.017),
                 -.096,-.076,"Ceramic_Ivory","cladding"))
    for i,sign in enumerate((-1,1)):
        add(prism_xz(f"Top_Ceramic_Panel_{i}",
                     chamfer_rect(sign*.169,.655,.076,.042,.011),
                     -.124,-.104,"Ceramic_Ivory","cladding"))
        add(prism_xz(f"Amber_Tip_Marker_{i}",
                     chamfer_rect(sign*.225,.662,.014,.025,.006),
                     -.130,-.121,"Amber_Indicator","accent"))
    add(prism_xz("Amber_Stem_Indicator",
                 chamfer_rect(0,.362,.013,.048,.004),
                 -.102,-.091,"Amber_Indicator","accent"))
    for i,sign in enumerate((-1,1)):
        add(prism_xz(f"Foot_Wear_Pad_{i}",
                     chamfer_rect(sign*.172,.057,.058,.026,.008),
                     -.131,-.114,"Brushed_Titanium","cladding"))
        add(prism_xz(f"Rear_Web_Rib_{i}",
                     chamfer_rect(sign*.073,.344,.014,.176,.005),
                     .077,.100,"Brushed_Titanium"))
    return parts

def validate(parts):
    names=set();total_v=total_f=0;bounds={}
    for p in parts:
        assert p["name"] not in names and p["material"] in MATERIALS,p["name"]
        names.add(p["name"]);v=p["vertices"];f=p["faces"];edges=Counter()
        assert v and f
        for xyz in v:assert len(xyz)==3 and all(math.isfinite(t) for t in xyz)
        for a,b,c in f:
            assert len({a,b,c})==3 and 0<=min(a,b,c) and max(a,b,c)<len(v)
            A,B,C=(v[j] for j in (a,b,c))
            normal=cross(sub(B,A),sub(C,A))
            assert dot(normal,normal)>1e-15,p["name"]
            for e in ((a,b),(b,c),(c,a)):edges[tuple(sorted(e))]+=1
        assert edges and all(n==2 for n in edges.values()),p["name"]
        assert volume(v,f)>1e-9,(p["name"],volume(v,f))
        bounds[p["name"]]=[[min(x[k] for x in v),max(x[k] for x in v)]
                            for k in range(3)]
        total_v+=len(v);total_f+=len(f)
    assert len(parts)==27 and len(MATERIALS)==5
    assert abs(BORE_R-SHAFT_R-.009)<1e-10
    assert bounds["Foot_Crossbar"][0][1]<min(abs(x)-BORE_R for x in BOLT_X)
    assert bounds["Upper_T_Crossbar"][0][1]<min(abs(x)-BORE_R for x in BOLT_X)
    assert bounds["Foot_Crossbar"][2][0]>=.017
    assert bounds["Vertical_Web"][2][0]>.06
    assert bounds["Vertical_Web"][2][1]>=.605
    assert bounds["Upper_T_Crossbar"][2][0]<=.60
    assert max(abs(v[0]) for p in parts for v in p["vertices"])<=.43
    return dict(id="M03-007",model="Uchwyt pionowy T",units="metres",
                parts=len(parts),vertices=total_v,triangles=total_f,
                materials=len(MATERIALS),foot_bores=2,top_bores=2,
                bolt_shafts=4,visible_bolt_heads=4,
                shaft_radial_clearance_m=.009,
                pivot=[0,0,0],mount_normal=[0,0,1],
                foot_bolt_centers=[[x,0,0] for x in BOLT_X],
                top_bolt_centers=[[x,0,.657] for x in BOLT_X],
                all_parts_closed_manifold=True,positive_signed_volume=True,
                animation="NOT_REQUIRED",blender="NOT_RUN",unity="NOT_RUN")

def export_static(parts,out):
    out.mkdir(parents=True,exist_ok=True)
    lines=["# M03-007: Z-up metres, mount pivot (0,0,0)",
           "mtllib "+NAME+".mtl"]
    offset=1
    for p in parts:
        lines.extend(("o "+p["name"],"usemtl "+p["material"]))
        lines.extend("v "+" ".join(f"{x:.8f}" for x in v)
                     for v in p["vertices"])
        lines.extend("f "+" ".join(str(offset+i) for i in f)
                     for f in p["faces"])
        offset+=len(p["vertices"])
    (out/(NAME+".obj")).write_text("\n".join(lines)+"\n",encoding="utf-8")
    mtl=[]
    for name,(rgba,metal,rough) in MATERIALS.items():
        mtl.extend(["newmtl "+name,
                    "Kd "+" ".join(f"{x:.5f}" for x in rgba[:3]),
                    "Ks "+" ".join(f"{metal*.5:.5f}" for _ in range(3)),
                    f"Ns {(1-rough)*400:.2f}","d 1.0","illum 2",""])
    (out/(NAME+".mtl")).write_text("\n".join(mtl)+"\n",encoding="utf-8")

def export_blender(parts,out):
    import bpy
    scene=bpy.context.scene
    old_frame=scene.frame_current
    old_selected=tuple(bpy.context.selected_objects)
    old_active=bpy.context.view_layer.objects.active
    collection=bpy.data.collections.get(COLLECTION)
    if collection:
        for ob in list(collection.objects):
            data=ob.data if ob.type=="MESH" else None
            bpy.data.objects.remove(ob,do_unlink=True)
            if data is not None and data.users==0:bpy.data.meshes.remove(data)
        bpy.data.collections.remove(collection)
    collection=bpy.data.collections.new(COLLECTION)
    scene.collection.children.link(collection)
    root=bpy.data.objects.new("M03_007_TOP_MOUNT_PIVOT",None)
    collection.objects.link(root);root.empty_display_size=.06
    mats={}
    for name,(rgba,metal,rough) in MATERIALS.items():
        key="M03_007_"+name
        mat=bpy.data.materials.get(key)
        if mat is None:mat=bpy.data.materials.new(key)
        mat.use_nodes=True
        shader=mat.node_tree.nodes.get("Principled BSDF")
        if shader:
            shader.inputs["Base Color"].default_value=rgba
            shader.inputs["Metallic"].default_value=metal
            shader.inputs["Roughness"].default_value=rough
        mat.diffuse_color=rgba;mats[name]=mat
    for p in parts:
        mesh=bpy.data.meshes.new("M03_007_"+p["name"]+"_mesh")
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.update(calc_edges=True)
        ob=bpy.data.objects.new("M03_007_"+p["name"],mesh)
        collection.objects.link(ob);ob.parent=root
        ob.data.materials.append(mats[p["material"]])
        for poly in mesh.polygons:poly.use_smooth=False
    try:
        scene.frame_set(1)
        bpy.ops.object.select_all(action="DESELECT")
        for ob in collection.objects:ob.select_set(True)
        bpy.context.view_layer.objects.active=root
        bpy.ops.export_scene.gltf(filepath=str(out/(NAME+".glb")),
            export_format="GLB",use_selection=True,export_animations=False)
        bpy.data.libraries.write(str(out/(NAME+".blend")),{collection},compress=True)
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in old_selected:
            if ob.name in bpy.data.objects:ob.select_set(True)
        if old_active is not None and old_active.name in bpy.data.objects:
            bpy.context.view_layer.objects.active=old_active
        scene.frame_set(old_frame)

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    ap=argparse.ArgumentParser()
    ap.add_argument("--out",default="build/M03-007")
    ap.add_argument("--self-test",action="store_true")
    args=ap.parse_args(argv)
    parts=build();qa=validate(parts)
    if not args.self_test:
        out=Path(args.out);export_static(parts,out)
        try:import bpy
        except ImportError:pass
        else:
            export_blender(parts,out)
            qa["blender"]="EXPORTED_BPY"
        (out/"M03-007_static_qa.json").write_text(
            json.dumps(qa,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(qa,indent=2))
if __name__=="__main__":main()
