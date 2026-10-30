#!/usr/bin/env python3
"""Original B01-31 P17/W22: three genuine original glTF2 armour geometry states.
No image swaps, no shared geometry meshes, no host damage/repair or Unity call.
Actual separate 1 x 2 x 2 grid-UNIT modular panel physical mesh variants:
OK complete plate, DAMAGED split angular metal, DESTROYED broken open shell.
"""
from __future__ import annotations
import argparse
import hashlib
import io
import json
import math
from pathlib import Path
import struct
import sys
import zipfile
from PIL import Image,ImageDraw,ImageFont

PKG="B01-31-MODULAR-ARMOR-3GEOMETRY-STATES-001"
VERSION="P17-W22-R1"
BASE="B0131ArmorModule1x2x2"
STATES=("OK","DAMAGED","DESTROYED")
COLORS=[
 ("FrameGraphite",(0.14,0.21,0.27,1),0.48,0.67),
 ("ArmorTitanium",(0.52,0.62,0.72,1),0.72,0.38),
 ("SafetyAmber",(0.92,0.54,0.15,1),0.19,0.48),
 ("CopperInternals",(0.77,0.39,0.21,1),0.84,0.31),
 ("SealRubber",(0.12,0.16,0.19,1),0.05,0.88),
 ("StatusCyan",(0.13,0.73,0.91,1),0.20,0.29)
]
SHA=lambda x:hashlib.sha256(x).hexdigest()
def require(a,b):
    if not a:raise ValueError("B0131_W22_SOURCE_RED "+b)
def polygon_ccw(poly):
    return sum(poly[i][0]*poly[(i+1)%len(poly)][1]-poly[(i+1)%len(poly)][0]*poly[i][1] for i in range(len(poly)))>0

def original_prism(x0,x1,poly,material,tag):
    """Real closed 3D extruded convex YZ polygon; outward winding and faces."""
    require(x1>x0 and 3<=len(poly)<=9 and polygon_ccw(poly),
            "closed convex original prism must have positive face orientation")
    for k in range(len(poly)):
        a,b,c=poly[(k-1)%len(poly)],poly[k],poly[(k+1)%len(poly)]
        cr=(b[0]-a[0])*(c[1]-b[1])-(b[1]-a[1])*(c[0]-b[0])
        require(cr>1e-8,"all original torn 3D armor polygons MUST remain convex")
    near=[(x0,y,z) for y,z in poly]
    far=[(x1,y,z) for y,z in poly]
    tri=[]
    for i in range(1,len(poly)-1):
        tri.append((near[0],near[i+1],near[i]))
        tri.append((far[0],far[i],far[i+1]))
    for i in range(len(poly)):
        j=(i+1)%len(poly)
        tri.append((near[i],near[j],far[j]))
        tri.append((near[i],far[j],far[i]))
    return [(material,tag,t) for t in tri]

def rectangular(x0,x1,y0,y1,z0,z1,mat,tag):
    require(y1>y0 and z1>z0,"3D slab nonzero extent")
    return original_prism(x0,x1,[(y0,z0),(y1,z0),(y1,z1),(y0,z1)],mat,tag)

def original_parts(state):
    require(state in STATES,"explicit three material state names")
    faces=[]
    def box(a,b,c,d,e,f,m,label):
        faces.extend(rectangular(a,b,c,d,e,f,m,label))
    def prism(a,b,p,m,label):
        faces.extend(original_prism(a,b,p,m,label))
    # Original common physical structural foundation and mounting ring.
    box(-.19,.22,-.84,.84,-.86,.86,0,"hull_core")
    box(.22,.31,-.89,.89,-.90,.90,1,"rear_backing")
    for y0,y1,z0,z1,name in (
        (-.92,-.78,-.91,.91,"bottom_rail"),
        (.78,.92,-.91,.91,"top_rail"),
        (-.77,.77,-.91,-.75,"left_bezel"),
        (-.77,.77,.75,.91,"right_bezel")):
        box(-.43,-.20,y0,y1,z0,z1,0,name)
    for y in (-.835,.835):
        for z in (-.835,.835):
            box(-.485,-.424,y-.032,y+.032,z-.032,z+.032,3,"corner_fastener")
    box(-.31,-.205,-.12,.12,-.12,.12,3,"internal_energized_relay")
    box(-.46,-.425,-.86,-.78,-.44,.44,2,"lower_warning_strip")
    # The real state difference is actual geometry, not a color/normal swap.
    if state=="OK":
        box(-.515,-.355,-.735,.735,-.735,.735,1,"intact_monocoque_armor")
        box(-.529,-.507,-.60,-.55,-.61,.61,5,"status_horizon_1")
        box(-.529,-.507,.55,.60,-.61,.61,5,"status_horizon_2")
        for z in (-.57,.57):
            box(-.541,-.513,-.36,.36,z-.024,z+.024,4,"flexible_rubber_edge")
    elif state=="DAMAGED":
        # Six physically separate irregular convex 3D armor wedges leave
        # a real transparent split and expose some inner structural geometry.
        for i,poly in enumerate([
            [(-.72,-.72),(-.19,-.72),(-.26,-.18),(-.72,-.12)],
            [(-.72,.13),(-.26,.22),(-.17,.73),(-.72,.73)],
            [(.18,-.72),(.72,-.72),(.72,-.14),(.27,-.24)],
            [(.26,.19),(.72,.09),(.72,.72),(.18,.72)],
            [(-.13,-.73),(.11,-.73),(.16,-.39),(-.09,-.43)],
            [(-.11,.40),(.14,.36),(.13,.72),(-.15,.72)]
        ]):
            prism(-.525 if i%2 else -.485,-.335 if i%2 else -.34,
                  poly,1,"damaged_torn_original_plate_%02d"%i)
        for z in (-.40,.40):
            box(-.535,-.44,-.72,-.62,z-.046,z+.046,2,"amber_frayed_edge")
        box(-.455,-.35,-.18,.18,-.12,.12,3,"exposed_copper_cracked_layer")
        box(-.47,-.40,.29,.36,-.38,.34,4,"torn_seal_strap")
    else:
        # Authentic open/broken shell; most armor plate removed. Independent
        # 3D splinters and exposed frame; no collider authority implied.
        for i,poly in enumerate([
            [(-.73,-.73),(-.46,-.73),(-.54,-.40),(-.72,-.36)],
            [(-.72,.44),(-.47,.37),(-.38,.72),(-.73,.72)],
            [(.41,-.72),(.72,-.72),(.71,-.45),(.46,-.35)],
            [(.46,.35),(.73,.42),(.73,.72),(.39,.72)],
            [(-.16,-.22),(-.03,-.29),(.08,-.03),(-.11,.07)],
            [(.08,.22),(.27,.13),(.34,.36),(.13,.43)]
        ]):
            prism(-.55+0.018*(i%3),-.37+0.01*(i%2),
                  poly,1 if i<4 else 2,"destroyed_jagged_debris_%02d"%i)
        for z in (-.53,-.33,.33,.53):
            box(-.455,-.39,-.36,.36,z-.022,z+.022,3,"visible_copper_internal")
        box(-.475,-.375,-.05,.05,-.42,.42,4,"ruptured_insulation")
        box(-.465,-.385,.13,.18,-.20,.22,5,"remaining_status_rod")
    return faces

def normal(tri):
    a,b,c=tri
    u=(b[0]-a[0],b[1]-a[1],b[2]-a[2])
    v=(c[0]-a[0],c[1]-a[1],c[2]-a[2])
    n=(u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0])
    l=math.sqrt(sum(x*x for x in n))
    require(l>1e-9,"original physical triangle not degenerate")
    return tuple(x/l for x in n)

def make_glb(state,faces):
    doc={"asset":{"version":"2.0","generator":PKG+"/"+VERSION,
          "extras":{"damage_visual_state":state,"units":"grid concept units",
                    "part_size_grid_xyz":[1,2,2],
                    "no_collision":"no native Unity/PhysX gameplay authority"}},
         "scene":0,"scenes":[{"name":"B0131_Armor1x2x2_"+state,"nodes":[0]}],
         "nodes":[{"name":"ArmorModule1x2x2_Pivot_"+state,"mesh":0,
                   "translation":[0,0,0],"rotation":[0,0,0,1]}],
         "materials":[],"meshes":[],"bufferViews":[],"accessors":[],"buffers":[]}
    for name,color,m,r in COLORS:
        doc["materials"].append({"name":name,
             "pbrMetallicRoughness":{"baseColorFactor":color,
                                    "metallicFactor":m,"roughnessFactor":r}})
    b=bytearray()
    def accessor(vals,n,kind=5126,target=None):
        b.extend(b"\0"*((-len(b))%4))
        off=len(b);fmt="f" if kind==5126 else "H"
        body=struct.pack("<"+fmt*len(vals),*vals);b.extend(body)
        vw={"buffer":0,"byteOffset":off,"byteLength":len(body)}
        if target:vw["target"]=target
        vid=len(doc["bufferViews"]);doc["bufferViews"].append(vw)
        ac={"bufferView":vid,"componentType":kind,"count":len(vals)//n,
            "type":{1:"SCALAR",3:"VEC3"}[n]}
        if kind==5126 and n==3:
            ac["min"]=[min(vals[i::3]) for i in range(3)]
            ac["max"]=[max(vals[i::3]) for i in range(3)]
        ai=len(doc["accessors"]);doc["accessors"].append(ac)
        return ai
    prisms=[]
    for m in range(6):
        relevant=[(tag,tri) for material,tag,tri in faces if material==m]
        if not relevant:continue
        pos=[];nor=[];idx=[]
        for tag,triangle in relevant:
            n=normal(triangle)
            for v in triangle:
                pos.extend(v);nor.extend(n);idx.append(len(idx))
        p=accessor(pos,3,5126,34962)
        n=accessor(nor,3,5126,34962)
        i=accessor(idx,1,5123,34963)
        prisms.append({"attributes":{"POSITION":p,"NORMAL":n},
                      "indices":i,"material":m,"mode":4})
    doc["meshes"]=[{"name":"B0131_Armor_"+state+"_ActualGeometry",
                    "primitives":prisms}]
    doc["buffers"]=[{"byteLength":len(b)}]
    jsonbytes=json.dumps(doc,sort_keys=True,separators=(",",":")).encode()
    jsonbytes+=b" "*((-len(jsonbytes))%4)
    bins=bytes(b)+b"\0"*((-len(b))%4)
    glb=(struct.pack("<4sII",b"glTF",2,12+8+len(jsonbytes)+8+len(bins))
         +struct.pack("<I4s",len(jsonbytes),b"JSON")+jsonbytes
         +struct.pack("<I4s",len(bins),b"BIN\0")+bins)
    require(len(glb)==struct.unpack_from("<I",glb,8)[0],"actual GLB2 physical size")
    return glb,doc

def render(faces,state,view="iso",theme="dark",size=(600,560)):
    yaw,pitch={"iso":(.64,.48),"face":(1.56,.0),"profile":(0.,.11)}[view]
    bg=(19,29,43) if theme=="dark" else (226,235,242)
    ink=(232,240,247) if theme=="dark" else (30,45,59)
    img=Image.new("RGB",size,bg);d=ImageDraw.Draw(img)
    w,h=size
    projected=[]
    for mat,tag,tri in faces:
        pts=[]
        for x,y,z in tri:
            rx=math.cos(yaw)*x+math.sin(yaw)*z
            rz=-math.sin(yaw)*x+math.cos(yaw)*z
            ry=math.cos(pitch)*y-math.sin(pitch)*rz
            depth=math.sin(pitch)*y+math.cos(pitch)*rz
            pts.append((w/2+rx*w*.22,h/2-ry*h*.30,depth))
        col=COLORS[mat][1]
        shaded=.76 if theme=="dark" else .91
        rgb=tuple(round(min(255,255*col[i]*shaded)) for i in range(3))
        projected.append((sum(q[2] for q in pts)/3,
                          [(round(q[0]),round(q[1])) for q in pts],rgb))
    for _,points,rgb in sorted(projected,key=lambda x:x[0]):
        d.polygon(points,fill=rgb)
    d.text((12,12),f"{state} / {view.upper()} / REAL GEOMETRY",
           fill=ink,font=ImageFont.load_default())
    d.text((12,h-25),"1x2x2 GRID CONCEPT / NOT UNITY COLLIDER",fill=ink)
    return img

def png(img):
    out=io.BytesIO()
    img.save(out,format="PNG",compress_level=0,optimize=False)
    return out.getvalue()

def review(faces,state,theme):
    canvas=Image.new("RGB",(1800,640),(19,29,43) if theme=="dark" else (226,235,242))
    d=ImageDraw.Draw(canvas)
    ink=(239,243,248) if theme=="dark" else (32,48,62)
    for i,view in enumerate(("iso","face","profile")):
        canvas.paste(render(faces,state,view,theme,(600,560)),(i*600,45))
        d.text((i*600+16,17),state+" / "+view.upper(),fill=ink)
    return png(canvas)

def comparison(by_state):
    canvas=Image.new("RGB",(1800,650),(20,29,43))
    d=ImageDraw.Draw(canvas)
    for i,state in enumerate(STATES):
        canvas.paste(render(by_state[state],state,"iso","dark",(600,560)),(i*600,55))
        d.text((i*600+15,15),f"{state} / ACTUAL ORIGIN PIVOT",fill=(238,244,249))
    return png(canvas)

def archive(files,where):
    with zipfile.ZipFile(where,"w",compression=zipfile.ZIP_STORED) as z:
        for name in sorted(files):
            zi=zipfile.ZipInfo(name,(2020,1,1,0,0,0))
            zi.create_system=3;zi.external_attr=0o100644<<16;zi.compress_type=zipfile.ZIP_STORED
            z.writestr(zi,files[name])
    with zipfile.ZipFile(where) as z:
        require(z.testzip() is None and all(z.read(n)==v for n,v in files.items()),
                "original physically stored ZIP CRC and member byte readback")

def main(out):
    by_state={state:original_parts(state) for state in STATES}
    files={}
    states={}
    for state,faces in by_state.items():
        model,doc=make_glb(state,faces)
        files[f"{BASE}-{state}.glb"]=model
        files[f"{BASE}-{state}-three-views.png"]=review(faces,state,"dark")
        states[state]={"triangle_count":len(faces),
             "geometry_sha256":SHA(model),
             "source_primitives":len(doc["meshes"][0]["primitives"]),
             "original_closed_prism_faces":len(faces),
             "original_3d_piece_tags":sorted(set(tag for _,tag,_ in faces))}
    files[f"{BASE}-three-states-comparison.png"]=comparison(by_state)
    detail={
        "package_id":PKG,"version":VERSION,
        "grid_module_envelope_xyz":[1,2,2],
        "three_visual_states":states,
        "semantics":{"OK":"complete primary armor cover",
                     "DAMAGED":"actual split/partial plate with visible torn wedges",
                     "DESTROYED":"real broken open armor with exposed core and debris"},
        "collision":"ART_ONLY_NO_COLLIDERS",
        "save_load":"NOT_IMPLEMENTED",
        "repair_economy":"NOT_IMPLEMENTED",
        "unity_native":"NOT_RUN",
        "mount_pivot_xyz":[0,0,0],
        "all_source_shaders":"glTF2 standard baseColor + metal/rough factors"}
    files["states.json"]=(json.dumps(detail,sort_keys=True,indent=2)+"\n").encode()
    receipt={
        "package_id":PKG,"version":VERSION,"owner":"B01-31",
        "state_counts":{k:v["triangle_count"] for k,v in states.items()},
        "actual_standalone_GLBs":3,"actual_original_PNG_boards":4,
        "real_ZIP_members":9,
        "actual_files":[{"name":name,"sha256":SHA(content),"bytes":len(content)}
                        for name,content in sorted(files.items())],
        "unity_G1":"NOT_RUN","PhysX":"NOT_RUN","gameplay":"NOT_RUN",
        "formal_G0":"PENDING","G2":"HOLD","codex_ACK":False,"integrated":False,
        "frozen_510_sha":"d94400d95d13db7f27e1533df05c9292d5380431",
        "frozen_556_sha":"33094f154c7b91119b36d58aab285e6e83c5de57",
        "frozen_596_sha":"efb2b06eafedab5469b57f163152d96aca3c5915"}
    files["receipt.json"]=(json.dumps(receipt,sort_keys=True,indent=2)+"\n").encode()
    out.mkdir(parents=True,exist_ok=True)
    for name,raw in files.items():
        (out/name).write_bytes(raw)
        require((out/name).read_bytes()==raw,"actual original disk roundtrip")
    archive(files,out.parent/(out.name+"-physical.zip"))
    print(json.dumps({"status":"PASS","three_real_original_GLTF2_models":3,
        "genuine_actual_triangles":{x:len(y) for x,y in by_state.items()},
        "original_independently_different_state_geometry":True,
        "actual_original_3D_image_boards":4,"physical_ZIP_members":9,
        "actual_physical_SHA256s":{s:states[s]["geometry_sha256"] for s in STATES},
        "Unity_G1":"NOT_RUN"}))
if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--output",required=True)
    args=parser.parse_args()
    try:main(Path(args.output))
    except Exception as exc:
        print("B01_31_W22_AUTHOR_RED "+type(exc).__name__+": "+str(exc),file=sys.stderr)
        raise
