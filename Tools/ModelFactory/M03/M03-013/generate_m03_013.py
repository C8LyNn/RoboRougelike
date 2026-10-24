#!/usr/bin/env python3
"""M03-013 lower sill guard / osłona progu dolnego, Unity 6 visual asset.

1 unit = 1 metre; local +Y up, +Z vehicle forward, X across chassis.
MOUNT_BOTTOM_FRAME pivot (0,0,0): top of four bored mounting collars.
No animation (catalog: brak). No gameplay, prefabs or Unity settings.
Plain Python exports real OBJ/MTL. Blender bpy additionally exports GLB,
FBX and BLEND with a correctly oriented Y-up source root.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

ID = "M03-013"
SEGMENTS = 32
PLATE_TOP = -0.090
PLATE_BOTTOM = -0.135
HOLE_RADIUS = 0.032
MOUNT_X = 0.195
MOUNT_Z = 0.500
RIVET_X = 0.215
RIVET_Z = (0.150, 0.780)
RIVET_HEAD_RADIUS = 0.042
SIDE_RAIL_INNER_X = 0.270
MATERIALS = {
    # name: (RGBA linear, metallic, roughness)
    "Graphite_Steel": ((0.075,0.091,0.113,1),0.79,0.36),
    "Ceramic_Wear": ((0.73,0.78,0.79,1),0.18,0.29),
    "Copper_Rivets": ((0.54,0.245,0.095,1),0.77,0.29),
    "Rubber_Edge": ((0.028,0.038,0.049,1),0.04,0.86),
    "Safety_Amber": ((0.96,0.40,0.055,1),0.28,0.34),
}

def mesh(name, material, vertices, faces, smooth=False):
    return dict(name=name, material=material, vertices=vertices,
                faces=faces, smooth=smooth)

def chamfer_box(name, material, cx, cy, cz, sx, sy, sz, cut):
    """Closed 8-corner chamfered XZ prism; +Y is the top."""
    assert min(sx,sy,sz)>0 and 0<cut<min(sx,sz)*.5
    a,b=sx/2,sz/2
    contour=[(-a+cut,-b),(a-cut,-b),(a,-b+cut),(a,b-cut),
             (a-cut,b),(-a+cut,b),(-a,b-cut),(-a,-b+cut)]
    v=[(cx+x,cy+y,cz+z) for y in (-sy/2,sy/2) for x,z in contour]
    v.extend(((cx,cy-sy/2,cz),(cx,cy+sy/2,cz)))
    f=[]
    for j in range(8):
        k=(j+1)%8
        f.extend(((j,8+j,k),(8+j,8+k,k)))
        f.extend(((16,j,k),(17,8+k,8+j)))
    return mesh(name,material,v,f)

def ring_y(name, material, cx, cz, y0, y1, inner, outer,
           segments=SEGMENTS, rect_half=None):
    """Watertight Y-axis ring. Optional rectangular outer perimeter.

    The rectangle is radially sampled against its exact boundary,
    leaving a real open circular bore (not a decal or dark cylinder).
    """
    assert 0<inner<outer and y1>y0 and segments>=16
    if rect_half:
        hx,hz=rect_half
        assert min(hx,hz)>outer
    contours=[]
    for y,radius,outer_loop in ((y0,outer,True),(y0,inner,False),
                                (y1,outer,True),(y1,inner,False)):
        loop=[]
        for j in range(segments):
            a=2*math.pi*j/segments
            u,w=math.cos(a),math.sin(a)
            r=min(hx/max(abs(u),1e-12),
                  hz/max(abs(w),1e-12)) if rect_half and outer_loop else radius
            loop.append((cx+r*u,y,cz+r*w))
        contours.extend(loop)
    f=[]
    for j in range(segments):
        k=(j+1)%segments
        ob,ib,ot,it=j,segments+j,2*segments+j,3*segments+j
        ob2,ib2,ot2,it2=k,segments+k,2*segments+k,3*segments+k
        # bottom faces -Y, top faces +Y
        f.extend(((ob,ob2,ib),(ib,ob2,ib2)))
        f.extend(((ot,it,ot2),(it,it2,ot2)))
        # outer wall points out, inner wall points into the bore
        f.extend(((ob,ot,ob2),(ot,ot2,ob2)))
        f.extend(((ib,ib2,it),(it,ib2,it2)))
    return mesh(name,material,contours,f)

def turned_y(name,material,cx,cz,profile,segments=SEGMENTS):
    """Closed lathed rivet head / shank, with rounded shoulder profile."""
    assert len(profile)>=2
    assert all(r>0 for y,r in profile)
    assert all(profile[i+1][0]>profile[i][0]
               for i in range(len(profile)-1))
    v=[]
    for y,r in profile:
        for j in range(segments):
            a=2*math.pi*j/segments
            v.append((cx+r*math.cos(a),y,cz+r*math.sin(a)))
    v.extend(((cx,profile[0][0],cz),(cx,profile[-1][0],cz)))
    bottom_center=len(v)-2
    top_center=len(v)-1
    f=[]
    for i in range(len(profile)-1):
        for j in range(segments):
            k=(j+1)%segments
            a=i*segments+j
            b=i*segments+k
            c=(i+1)*segments+j
            d=(i+1)*segments+k
            f.extend(((a,c,b),(c,d,b)))
    last=(len(profile)-1)*segments
    for j in range(segments):
        k=(j+1)%segments
        f.extend(((bottom_center,j,k),
                  (top_center,last+k,last+j)))
    return mesh(name,material,v,f,True)

def build():
    p=[]
    add=p.append
    # Main central armor strip: side panels butt-joint along X, with
    # separately bored 4 tiles (not a solid plate hiding fake holes).
    add(chamfer_box("Central_Armor_Spine","Graphite_Steel",
                    0,(PLATE_TOP+PLATE_BOTTOM)/2,0,
                    .240,.045,1.800,.010))
    for side,label in ((-1,"L"),(1,"R")):
        x=side*MOUNT_X
        for sign,word in ((-1,"Aft"),(1,"Fore")):
            z=sign*MOUNT_Z
            add(ring_y("Bored_Mount_Tile_"+label+"_"+word,
                       "Graphite_Steel",x,z,PLATE_BOTTOM,PLATE_TOP,
                       HOLE_RADIUS,.047,rect_half=(.075,.170)))
            add(ring_y("Raised_Collar_"+label+"_"+word,
                       "Copper_Rivets",x,z,PLATE_TOP,0.,
                       HOLE_RADIUS,.050))
        for index,(z0,z1) in enumerate(((-.900,-.670),
                                         (-.330,.330),
                                         (.670,.900)),1):
            add(chamfer_box("Sill_Filler_"+label+"_"+str(index),
                            "Graphite_Steel",x,
                            (PLATE_TOP+PLATE_BOTTOM)/2,(z0+z1)/2,
                            .150,.045,z1-z0,.006))
        # Rubber strip joins the full length of the side panel along
        # the exact X=.270 side boundary; no buried/floating ornament.
        add(chamfer_box("Side_Impact_Edge_"+label,"Rubber_Edge",
                        side*.2925,-.1225,0,.045,.070,1.60,.012))
        # Eight heavy rivets in the solid filler regions, avoiding
        # the four real through-bores.
        for sign,word in ((-1,"Aft"),(1,"Fore")):
            for zabs in RIVET_Z:
                z=sign*zabs
                suffix=label+"_"+word+"_"+str(int(zabs*1000))
                add(turned_y("Rivet_Shank_"+suffix,"Graphite_Steel",
                             side*RIVET_X,z,
                             ((-.155,.020),(-.135,.020))))
                add(turned_y("Heavy_Rivet_Head_"+suffix,"Copper_Rivets",
                             side*RIVET_X,z,
                             ((-.183,.028),(-.179,RIVET_HEAD_RADIUS),
                              (-.159,RIVET_HEAD_RADIUS),(-.155,.030))))
        add(chamfer_box("Ceramic_Skid_"+label,"Ceramic_Wear",
                        side*.0825,-.1575,0,.050,.045,1.48,.012))
    add(chamfer_box("Centre_Ceramic_Wear","Ceramic_Wear",
                    0,-.1575,0,.050,.045,1.24,.012))
    for sign,word in ((-1,"Aft"),(1,"Fore")):
        add(chamfer_box("Amber_Underside_Marker_"+word,
                        "Safety_Amber",0,-.145,sign*.785,
                        .055,.020,.090,.008))
    return p

def sub(a,b):
    return tuple(x-y for x,y in zip(a,b))

def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])

def dot(a,b):
    return sum(x*y for x,y in zip(a,b))

def bounds(p):
    return [(min(v[k] for v in p["vertices"]),
             max(v[k] for v in p["vertices"])) for k in range(3)]

def validate(parts):
    names=set()
    verts=tris=0
    for p in parts:
        assert p["name"] not in names,p["name"]
        names.add(p["name"])
        assert p["material"] in MATERIALS
        assert p["vertices"] and p["faces"]
        verts+=len(p["vertices"])
        tris+=len(p["faces"])
        edges={}
        signed_volume=0.
        for v in p["vertices"]:
            assert all(math.isfinite(c) for c in v)
        for a,b,c in p["faces"]:
            assert min(a,b,c)>=0 and max(a,b,c)<len(p["vertices"])
            assert len({a,b,c})==3
            A,B,C=(p["vertices"][i] for i in (a,b,c))
            n=cross(sub(B,A),sub(C,A))
            assert dot(n,n)>1e-16,(p["name"],"degenerate")
            signed_volume+=dot(A,cross(B,C))/6
            for u,w in ((a,b),(b,c),(c,a)):
                key=tuple(sorted((u,w)))
                edges.setdefault(key,[]).append((u,w))
        assert all(len(v)==2 and v[0]==v[1][::-1]
                   for v in edges.values()),(p["name"],"open/winding")
        assert signed_volume>1e-10,(p["name"],"inward normals",signed_volume)
    assert len(parts)==38,(len(parts),names)
    assert len(MATERIALS)>=3
    by={p["name"]:p for p in parts}
    # Four real bores must run through both tile and raised collar,
    # aligned to 1 m spacing along vehicle Z (front/rear at +/-0.5 m).
    for label,x in (("L",-MOUNT_X),("R",MOUNT_X)):
        for word,z in (("Aft",-MOUNT_Z),("Fore",MOUNT_Z)):
            tile=by["Bored_Mount_Tile_"+label+"_"+word]
            collar=by["Raised_Collar_"+label+"_"+word]
            assert abs(bounds(tile)[1][1]-bounds(collar)[1][0])<1e-8
            for part in (tile,collar):
                assert part["name"].startswith(("Bored_","Raised_"))
                assert min(math.hypot(v[0]-x,v[2]-z)
                           for v in part["vertices"])>=HOLE_RADIUS-1e-9
            assert abs(bounds(collar)[1][1])<1e-9
    # Contact chain: metal body -> shank -> head, and metal -> wear
    # runners. Clearance to side rails and real mount holes is positive.
    for side,label in ((-1,"L"),(1,"R")):
        edge=bounds(by["Side_Impact_Edge_"+label])[0]
        assert abs((edge[0] if side>0 else edge[1])-
                   side*SIDE_RAIL_INNER_X)<1e-8
        runner=bounds(by["Ceramic_Skid_"+label])
        assert abs(runner[1][1]-PLATE_BOTTOM)<1e-8
        for word,sgn in (("Aft",-1),("Fore",1)):
            for zabs in RIVET_Z:
                suffix=label+"_"+word+"_"+str(int(zabs*1000))
                stem=bounds(by["Rivet_Shank_"+suffix])
                head=bounds(by["Heavy_Rivet_Head_"+suffix])
                assert abs(stem[1][1]-PLATE_BOTTOM)<1e-8
                assert abs(stem[1][0]-head[1][1])<1e-8
                assert abs(sgn*zabs-sgn*MOUNT_Z)>.17
                assert abs(side*RIVET_X)+RIVET_HEAD_RADIUS < SIDE_RAIL_INNER_X
    assert abs(bounds(by["Centre_Ceramic_Wear"])[1][1]-PLATE_BOTTOM)<1e-8
    assert all(abs(bounds(by["Amber_Underside_Marker_"+word])[1][1]-
                   PLATE_BOTTOM)<1e-8 for word in ("Aft","Fore"))
    assert max(bounds(p)[1][1] for p in parts)<=1e-9
    assert min(bounds(p)[1][0] for p in parts)>=-.183-1e-9
    return {
        "asset_id":ID,"name":"Osłona progu dolnego",
        "parts":len(parts),"vertices":verts,"triangles":tris,
        "materials":len(MATERIALS),
        "pivot":"MOUNT_BOTTOM_FRAME at (0,0,0); Y up, +Z forward",
        "snap_ports":[[x,0.,z] for x in (-MOUNT_X,MOUNT_X)
                      for z in (-MOUNT_Z,MOUNT_Z)],
        "actual_bored_mounts":4,"heavy_rivet_heads":8,
        "rubber_side_edges":2,"ceramic_wear_runners":3,
        "animation":"NOT REQUIRED (static)",
        "mesh_topology":"PASS: watertight, oriented edges, positive volume",
        "contact_geometry":"PASS: analytic attachment/clearance assertions",
        "visual_review":"NOT RUN",
        "blender_export":"NOT RUN","unity_import":"NOT RUN",
        "unity_editmode":"NOT RUN","unity_playmode":"NOT RUN"}

def export_obj(parts,out):
    stem="M03-013_lower_sill_guard"
    obj=out/(stem+".obj")
    mtl=out/(stem+".mtl")
    lines=["# M03-013 actual Y-up static geometry; 1 unit = 1 metre",
           "mtllib "+mtl.name]
    offset=0
    for p in parts:
        lines.extend(("o "+p["name"],"usemtl "+p["material"]))
        for x,y,z in p["vertices"]:
            lines.append("v %.9f %.9f %.9f"%(x,y,z))
        for a,b,c in p["faces"]:
            lines.append("f %d %d %d"%(offset+a+1,offset+b+1,offset+c+1))
        offset+=len(p["vertices"])
    obj.write_text("\n".join(lines)+"\n",encoding="utf-8")
    lines=["# Static MTL approximates Blender PBR values"]
    for name,(rgba,metal,rough) in MATERIALS.items():
        lines.extend(("newmtl "+name,
                      "Kd %.6f %.6f %.6f"%rgba[:3],
                      "Ks %.6f %.6f %.6f"%(metal,metal,metal),
                      "Ns %.3f"%((1-rough)*1000),
                      "d %.3f"%rgba[3],"illum 2",""))
    mtl.write_text("\n".join(lines)+"\n",encoding="utf-8")
    return [str(obj),str(mtl)]

def blender_export(parts,out):
    import bpy
    col=bpy.data.collections.get("MODEL_M03_013")
    if col:
        for ob in list(col.objects):
            data=ob.data
            bpy.data.objects.remove(ob,do_unlink=True)
            if data and hasattr(data,"users") and data.users==0:
                if isinstance(data,bpy.types.Mesh):
                    bpy.data.meshes.remove(data)
        bpy.data.collections.remove(col)
    col=bpy.data.collections.new("MODEL_M03_013")
    bpy.context.scene.collection.children.link(col)
    root=bpy.data.objects.new("MOUNT_BOTTOM_FRAME",None)
    col.objects.link(root)
    # Source is Unity Y-up. Rotate into Blender Z-up for preview;
    # Blender's GLB exporter maps it back to glTF Y-up.
    root.rotation_euler[0]=math.pi/2
    root.empty_display_size=.065
    mats={}
    for name,(rgba,metal,rough) in MATERIALS.items():
        mat=bpy.data.materials.get("M03_013_"+name)
        if mat is None:
            mat=bpy.data.materials.new("M03_013_"+name)
        mat.use_nodes=True
        shader=mat.node_tree.nodes.get("Principled BSDF")
        if shader:
            shader.inputs["Base Color"].default_value=rgba
            shader.inputs["Metallic"].default_value=metal
            shader.inputs["Roughness"].default_value=rough
        mat.diffuse_color=rgba
        mats[name]=mat
    for p in parts:
        data=bpy.data.meshes.new("M03_013_"+p["name"])
        data.from_pydata(p["vertices"],[],p["faces"])
        data.update(calc_edges=True)
        assert not data.validate(verbose=False,clean_customdata=False),p["name"]
        ob=bpy.data.objects.new("M03_013_"+p["name"],data)
        col.objects.link(ob)
        ob.parent=root
        data.materials.append(mats[p["material"]])
        for face in data.polygons:
            face.use_smooth=bool(p["smooth"])
    for x in (-MOUNT_X,MOUNT_X):
        for z in (-MOUNT_Z,MOUNT_Z):
            suffix=("L" if x<0 else "R")+("_Aft" if z<0 else "_Fore")
            snap=bpy.data.objects.new("SNAP_BOTTOM_"+suffix,None)
            col.objects.link(snap)
            snap.parent=root
            snap.location=(x,0,z)
            snap.empty_display_size=.035
    bpy.ops.object.select_all(action="DESELECT")
    for ob in col.objects:
        ob.select_set(True)
    bpy.context.view_layer.objects.active=root
    fbx=out/"M03-013_lower_sill_guard.fbx"
    glb=out/"M03-013_lower_sill_guard.glb"
    blend=out/"M03-013_lower_sill_guard.blend"
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
                             axis_forward="-Z",axis_up="Y",
                             add_leaf_bones=False,bake_anim=False)
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
                              use_selection=True,export_animations=False)
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    return {"backend":"bpy","blender_export":"PASS: files generated",
            "glb":str(glb),"fbx":str(fbx),"blend":str(blend),
            "blender_meshes":len(parts),"blender_snaps":4}

def main():
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M03-013")
    parser.add_argument("--self-test",action="store_true")
    opts=parser.parse_args(args)
    parts=build()
    report=validate(parts)
    if opts.self_test:
        print(json.dumps(report,indent=2))
        return
    out=Path(opts.out)
    out.mkdir(parents=True,exist_ok=True)
    report["static_files"]=export_obj(parts,out)
    try:
        import bpy
    except ImportError:
        report["backend"]="plain-python-OBJ-MTL"
    else:
        report.update(blender_export(parts,out))
    (out/"M03-013_manifest.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))

if __name__=="__main__":
    main()
