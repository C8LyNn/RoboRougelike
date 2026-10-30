#!/usr/bin/env python3
"""B01-31 original SPLIT-COUPLER-CONCEPT3D-001 authored low-poly 3D art.
Builds an ACTUAL glTF 2.0 GLB (two independently pivotable mesh nodes),
a physical OBJ/MTL interchange, three-view true geometry review boards.
Not a gameplay collider, Unity prefab, Save/Load bridge or PhysX proof.
No external art/media. Original 3D triangle geometry authored algorithmically.
"""
from __future__ import annotations
import argparse
from collections import defaultdict
import hashlib
import io
import json
import math
from pathlib import Path
import struct
import sys
import zipfile
from PIL import Image, ImageDraw, ImageFont

PACKAGE="B01-31-SPLIT-COUPLER-CONCEPT3D-001"
VERSION="P17-W16-R1"
NAME="B0131SplitCouplerConcept"
SIDES=("LeftConnectorPivot","RightConnectorPivot")
PIVOTS=(-0.20,0.20)
MATERIALS=[
 ("graphite_shell",(52,67,83),0.7,0.45),
 ("rubber_cable",(30,43,58),0.05,0.85),
 ("hazard_amber",(244,155,76),0.3,0.42),
 ("exposed_copper",(220,155,84),0.85,0.3),
 ("insulation_cyan",(88,186,215),0.2,0.55),
 ("strain_collar",(105,121,136),0.55,0.5),
]
SHA=lambda b:hashlib.sha256(b).hexdigest()
def ensure(condition,reason):
    if not condition: raise ValueError("B0131_SPLIT_COUPLER_SOURCE_RED "+reason)

def vecsub(a,b):return (a[0]-b[0],a[1]-b[1],a[2]-b[2])
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def norm(v):
    length=math.sqrt(sum(x*x for x in v))
    ensure(length>1e-9 and math.isfinite(length),"degenerate triangle geometry")
    return tuple(x/length for x in v)

def add_tri(out,material,a,b,c):
    normal=norm(cross(vecsub(b,a),vecsub(c,a)))
    ensure(all(math.isfinite(t) and abs(t)<=2 for pt in (a,b,c) for t in pt),
           "source mesh nonfinite or unexpected scale")
    out[material].append(((a,normal),(b,normal),(c,normal)))

def cylinder(out,material,x0,x1, radius,cy=0.0,cz=0.0,sides=12):
    ensure(x1>x0+1e-5 and 0<radius<0.4 and sides in (8,12,16),
           "original closed cylinder dimensions")
    rings=[]
    for x in (x0,x1):
        rings.append([(x,cy+radius*math.cos(t*2*math.pi/sides),
                       cz+radius*math.sin(t*2*math.pi/sides)) for t in range(sides)])
    for i in range(sides):
        j=(i+1)%sides
        a,b=rings[0][i],rings[0][j]
        c,d=rings[1][j],rings[1][i]
        add_tri(out,material,a,c,b)
        add_tri(out,material,a,d,c)
        add_tri(out,material,(x0,cy,cz),b,a)
        add_tri(out,material,(x1,cy,cz),d,c)

def source_meshes():
    result=[]
    for side in (0,1):
        o=defaultdict(list)
        sign=-1 if side==0 else 1
        # Main disconnected connector housing and the cable behind it.
        if side==0:
            body=(-0.39,-0.05)
            cable=(-0.83,-0.39)
            collar=(-0.43,-0.32)
            strain=(-0.54,-0.39)
            pin=(-0.05,0.07)
            rib_positions=(-0.30,-0.22,-0.14)
            end_band=(-0.095,-0.05)
        else:
            body=(0.05,0.39)
            cable=(0.39,0.83)
            collar=(0.32,0.43)
            strain=(0.39,0.54)
            pin=(-0.07,0.05)
            rib_positions=(0.14,0.22,0.30)
            end_band=(0.05,0.095)
        cylinder(o,1,*cable,0.09, sides=16)
        cylinder(o,0,*body,0.153, sides=16)
        cylinder(o,5,*collar,0.190,sides=16)
        cylinder(o,5,*strain,0.127,sides=12)
        cylinder(o,2,*end_band,0.160,sides=12)
        for index,x in enumerate(rib_positions):
            m=4 if index==1 else 5
            cylinder(o,m,x-0.013,x+0.013,0.161,sides=12)
        # Four visibly exposed, independent copper conductor tips each side,
        # retracted from shared world centre: nearest ends stay >=0.26m apart.
        for y,z in ((-0.067,-0.067),(-0.067,0.067),
                    (0.067,-0.067),(0.067,0.067)):
            cylinder(o,3,*pin,0.021,y,z,sides=8)
        # Two visible non-authoritative printed bands on the rubber jacket.
        for x in ((-0.68,-0.61) if side==0 else (0.61,0.68)):
            cylinder(o,4,x-0.01,x+0.01,0.095,sides=12)
        # No geometry crosses the centre gap. Material color is not an allow state.
        result.append(dict(o))
    return result

def binary_4pad(b,pad=b"\0"):
    return b+pad*((-len(b))%4)

def create_glb(source):
    gltf={
      "asset":{"version":"2.0","generator":PACKAGE+"/"+VERSION,
               "extras":{"purpose":"original disconnected coupler concept-only; no gameplay/physics guarantee"}},
      "scene":0,"scenes":[{"name":"B0131-Split-Coupler-Preview","nodes":[0,1]}],
      "nodes":[{"name":SIDES[i],"mesh":i,"translation":[PIVOTS[i],0,0],
                "extras":{"concept_owner":"B01-31","preview_only":True,
                          "candidate_pivot_role":"left" if i==0 else "right"}}
               for i in (0,1)],
      "materials":[{"name":name,
                    "pbrMetallicRoughness":{
                        "baseColorFactor":[*(c/255 for c in color),1],
                        "metallicFactor":metal,
                        "roughnessFactor":rough},
                    "doubleSided":True}
                   for name,color,metal,rough in MATERIALS],
      "meshes":[],"buffers":[],"bufferViews":[],"accessors":[]
    }
    buffer=bytearray()
    def view(payload,target):
        nonlocal buffer
        buffer+=b"\0"*((-len(buffer))%4)
        off=len(buffer);buffer+=payload
        idx=len(gltf["bufferViews"])
        gltf["bufferViews"].append({"buffer":0,"byteOffset":off,
                                    "byteLength":len(payload),"target":target})
        return idx
    for side_index,materials in enumerate(source):
        primitives=[]
        for m in sorted(materials):
            tris=materials[m]
            positions=[]; normals=[]
            for triangle in tris:
                for xyz,normal in triangle:
                    positions.extend(xyz);normals.extend(normal)
            vertex_count=len(positions)//3
            ensure(vertex_count>0 and vertex_count%3==0
                   and vertex_count<65536,"real triangle vertex buffer")
            position_bytes=struct.pack("<"+"f"*len(positions),*positions)
            normal_bytes=struct.pack("<"+"f"*len(normals),*normals)
            index_bytes=struct.pack("<"+"H"*vertex_count,*range(vertex_count))
            pview=view(position_bytes,34962)
            nview=view(normal_bytes,34962)
            iview=view(index_bytes,34963)
            mins=[min(positions[axis::3]) for axis in range(3)]
            maxs=[max(positions[axis::3]) for axis in range(3)]
            pacc=len(gltf["accessors"])
            gltf["accessors"].append({"bufferView":pview,"componentType":5126,
                                      "count":vertex_count,"type":"VEC3",
                                      "min":mins,"max":maxs})
            nacc=len(gltf["accessors"])
            gltf["accessors"].append({"bufferView":nview,"componentType":5126,
                                      "count":vertex_count,"type":"VEC3"})
            iacc=len(gltf["accessors"])
            gltf["accessors"].append({"bufferView":iview,"componentType":5123,
                                      "count":vertex_count,"type":"SCALAR"})
            primitives.append({"attributes":{"POSITION":pacc,"NORMAL":nacc},
                               "indices":iacc,"material":m,"mode":4})
        gltf["meshes"].append({"name":SIDES[side_index]+"Mesh",
                              "primitives":primitives})
    gltf["buffers"].append({"byteLength":len(buffer)})
    json_chunk=binary_4pad(json.dumps(gltf,sort_keys=True,separators=(",",":")).encode(),b" ")
    bin_chunk=binary_4pad(bytes(buffer))
    length=12+8+len(json_chunk)+8+len(bin_chunk)
    glb=(struct.pack("<4sII",b"glTF",2,length)+
         struct.pack("<I4s",len(json_chunk),b"JSON")+json_chunk+
         struct.pack("<I4s",len(bin_chunk),b"BIN\0")+bin_chunk)
    ensure(len(glb)==length,"actual GLB2 header and chunk length")
    return glb,gltf

def create_obj(source):
    out=["# Original "+PACKAGE+" "+VERSION,
         "# Unit meters / Y up. OBJ groups are physically separated; GLB preserves independent pivots.",
         "mtllib "+NAME+".mtl"]
    global_count=0
    for side_index,materials in enumerate(source):
        out.append("o "+SIDES[side_index])
        for material in sorted(materials):
            out.append("g "+SIDES[side_index]+"_"+MATERIALS[material][0])
            out.append("usemtl "+MATERIALS[material][0])
            for triangle in materials[material]:
                vertices=[]
                for point,normal in triangle:
                    world=(point[0]+PIVOTS[side_index],point[1],point[2])
                    out.append("v "+" ".join(f"{v:.7f}" for v in world))
                    out.append("vn "+" ".join(f"{v:.7f}" for v in normal))
                    global_count+=1
                    vertices.append(global_count)
                out.append("f "+" ".join(f"{v}//{v}" for v in vertices))
    mtl=["# Original B01-31 procedural concept PBR-compatible diffuse fallback"]
    for name,color,metal,rough in MATERIALS:
        mtl.extend(["newmtl "+name,
                    "Kd "+" ".join(f"{c/255:.7f}" for c in color),
                    "Ka 0.020 0.020 0.020",
                    "Ks "+" ".join(f"{metal*0.65:.5f}" for _ in range(3)),
                    "Ns "+f"{20+60*(1-rough):.5f}",
                    "d 1.0"])
    return ("\n".join(out)+"\n").encode(),("\n".join(mtl)+"\n").encode()

def preview(source,theme):
    # Actual geometry projected as flat-shaded triangle faces. Real 3D review:
    # no stock/AI fabricated screenshots, no Unity render.
    assert theme in ("dark","light")
    bg=(21,30,43) if theme=="dark" else (221,230,238)
    text=(238,243,249) if theme=="dark" else (34,46,61)
    border=(82,105,124) if theme=="dark" else (153,171,187)
    out=Image.new("RGB",(1800,700),bg)
    d=ImageDraw.Draw(out)
    font=ImageFont.load_default()
    settings=((0.50,0.38,"ISOMETRIC"),(0.0,0.12,"SIDE PROFILE"),
              (-0.66,0.58,"REVERSE 3D"))
    for panel,(yaw,pitch,label) in enumerate(settings):
        xbase=panel*600
        d.rounded_rectangle((xbase+14,62,xbase+586,672),radius=18,
                            outline=border,width=3)
        d.text((xbase+30,24),label+" / ART PREVIEW / NOT UNITY",fill=text,font=font)
        # Project all actual 3D source triangles and sort far to near in transformed Z.
        triangles=[]
        for side,materials in enumerate(source):
            for material,tset in materials.items():
                matcolor=MATERIALS[material][1]
                for tri in tset:
                    projected=[]
                    for pt,_ in tri:
                        x,y,z=pt
                        x+=PIVOTS[side]
                        xx=math.cos(yaw)*x+math.sin(yaw)*z
                        zz=-math.sin(yaw)*x+math.cos(yaw)*z
                        yy=math.cos(pitch)*y-math.sin(pitch)*zz
                        zdepth=math.sin(pitch)*y+math.cos(pitch)*zz
                        projected.append((xbase+300+xx*225,365-yy*225,zdepth))
                    # Lighting from actual triangle normal, stable across themes.
                    p0,p1,p2=[(pt[0]+PIVOTS[side],pt[1],pt[2]) for pt,_ in tri]
                    n=norm(cross(vecsub(p1,p0),vecsub(p2,p0)))
                    intensity=0.66+0.30*abs(n[0]*0.55+n[1]*0.7+n[2]*0.45)
                    color=tuple(min(255,int(c*intensity)) for c in matcolor)
                    triangles.append((sum(v[2] for v in projected)/3,
                                      [(int(v[0]),int(v[1])) for v in projected],
                                      color))
        for _,vertices,color in sorted(triangles,key=lambda t:t[0]):
            d.polygon(vertices,fill=color)
        d.text((xbase+24,634),"TWO SEPARATE PIVOTS / 0.26m PIN-TIP GAP",fill=text,font=font)
    buffer=io.BytesIO()
    out.save(buffer,format="PNG",compress_level=0,optimize=False)
    return buffer.getvalue()

def produce(output:Path):
    output.mkdir(parents=True,exist_ok=True)
    source=source_meshes()
    real_glb,gltf=create_glb(source)
    obj,mtl=create_obj(source)
    assets={NAME+".glb":real_glb,NAME+".obj":obj,NAME+".mtl":mtl}
    for filename,raw in assets.items():
        (output/filename).write_bytes(raw)
        ensure((output/filename).read_bytes()==raw,"physical asset written / byte parity")
    previews=[]
    for theme in ("dark","light"):
        data=preview(source,theme)
        name=NAME+"-"+theme+"-three-views.png"
        (output/name).write_bytes(data)
        with Image.open(io.BytesIO(data)) as im:
            im.load()
            ensure(im.format=="PNG" and im.mode=="RGB" and
                   im.size==(1800,700),"actual geometry preview PNG decode")
        previews.append({"name":name,"sha256":SHA(data),"bytes":len(data)})
    mesh_stats=[]
    for si,parts in enumerate(source):
        triangles=sum(len(x) for x in parts.values())
        mesh_stats.append({"side":SIDES[si],"pivot":[PIVOTS[si],0,0],
                           "primitive_material_groups":len(parts),
                           "triangles":triangles,
                           "vertices_triangle_soup":triangles*3})
    ensure(all(x["primitive_material_groups"]==6 for x in mesh_stats),
           "six original deterministic PBR material groups per side")
    total=sum(x["triangles"] for x in mesh_stats)
    receipt={
      "package_id":PACKAGE,"version":VERSION,"owner":"B01-31",
      "art_type":"original authored 3D two-root independently pivotable GLB2 and OBJ",
      "geometry_sources":"pure procedural closed low-poly triangle cylinders / no source external",
      "meters_per_unit":1.0,"Y_up":True,
      "actual_pivot_nodes":mesh_stats,"total_glb_triangles":total,
      "original_pbr_materials":[m[0] for m in MATERIALS],
      "left_pin_max_world_x":-0.13,"right_pin_min_world_x":0.13,
      "minimum_design_pin_tip_gap_m":0.26,
      "actual_preview_images":previews,
      "actual_binary_assets":[{"file":name,"sha256":SHA(raw),"bytes":len(raw)}
                              for name,raw in sorted(assets.items())],
      "runtime":{ "unity_G1":"NOT_RUN","physx":"NOT_RUN",
                   "gameplay_reattach":"NOT_IMPLEMENTED",
                   "game_save_load":"NOT_IMPLEMENTED",
                   "unity_import":"NOT_RUN",
                   "existing_game_modified":False,
                   "Codex_ACK":False,"G2":"HOLD"},
      "freeze":{"gameplay_pr":510,"gameplay_sha":"d94400d95d13db7f27e1533df05c9292d5380431",
                "save_pr":556,"save_sha":"33094f154c7b91119b36d58aab285e6e83c5de57"}
    }
    (output/"receipt.json").write_bytes((json.dumps(receipt,indent=2,sort_keys=True)+"\n").encode())
    # Additional 6-file deterministic physical distribution archive, not Unity installer.
    files=sorted(p for p in output.iterdir() if p.is_file())
    archive=output.parent/(output.name+"-physical.zip")
    with zipfile.ZipFile(archive,"w",zipfile.ZIP_STORED) as z:
        for p in files:
            info=zipfile.ZipInfo(p.name,(2020,1,1,0,0,0))
            info.create_system=3
            info.external_attr=0o100644<<16
            info.compress_type=zipfile.ZIP_STORED
            z.writestr(info,p.read_bytes())
    with zipfile.ZipFile(archive) as z:
        ensure(z.testzip() is None and len(z.namelist())==6,
               "physical distribution ZIP 6 real asset/receipt members")
    print(json.dumps({"status":"PASS","original_GLTF2_GLB":1,"original_OBJ_MTL":2,
                      "real_meshes":2,"independent_root_pivots":2,
                      "PBR_materials":len(MATERIALS),"actual_triangles":total,
                      "actual_pbr_primitive_groups":sum(x["primitive_material_groups"] for x in mesh_stats),
                      "real_review_png":len(previews),
                      "physical_zip_members":6,
                      "min_pin_tip_gap_m":0.26,
                      "glb_sha256":SHA(real_glb),
                      "unity_native":"NOT_RUN"}))
    return 0

if __name__=="__main__":
    a=argparse.ArgumentParser()
    a.add_argument("--output",required=True)
    args=a.parse_args()
    try:sys.exit(produce(Path(args.output)))
    except Exception as e:
        print("B0131_SPLIT_COUPLER_AUTHOR_ERROR "+type(e).__name__+": "+str(e),file=sys.stderr)
        raise
