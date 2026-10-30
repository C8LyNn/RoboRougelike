#!/usr/bin/env python3
"""B01-31 W18 original standalone 3D 16-shard connector fracture burst.
Genuine glTF2 16 independently animated 3D triangular-prism objects,
32 translation/quaternion channels, 7 actual keyframes, 3 actual 16x80ms GIFs.
Pure procedural new geometry. NOT in-game physics/Unity/native gameplay.
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
from PIL import Image, ImageDraw, ImageFont

PKG="B01-31-CONNECTOR-SHARD-BURST3D-001"
VERSION="P17-W18-R1"
BASE="B0131-ConnectorShardBurst3D"
TIMES=(0.,0.08,0.20,0.36,0.60,0.88,1.20)
# Outward non-decreasing motion factors: impulse followed by damped ballistic spread.
SPREAD=(0.,0.10,0.32,0.58,0.82,0.94,1.0)
SPIN=(0.,0.2,0.55,0.95,1.45,1.85,2.10)
SHARDS=16
COLORS=[
 ("fractured_insulation_amber",(0.98,0.51,0.20),0.05,0.45),
 ("electrical_core_cyan",(0.21,0.72,0.90),0.20,0.37),
 ("graphite_fragment",(0.26,0.34,0.44),0.12,0.70),
 ("copper_contact_shard",(0.83,0.48,0.22),0.78,0.35),
]
H=lambda b:hashlib.sha256(b).hexdigest()
def require(ok,reason):
    if not ok: raise ValueError("B0131_W18_SHARD_BURST_RED "+reason)

def vsub(a,b):return tuple(a[i]-b[i] for i in range(3))
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],
                       a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def normalize(v):
    length=math.sqrt(sum(x*x for x in v))
    require(length>1e-9,"zero area original shard triangle")
    return tuple(x/length for x in v)
def triangles_for_shard(i):
    # Each shard has a distinct original lowpoly wedge with an actual
    # closed manifold 3D triangular prism (2 caps + 6 lateral triangles).
    a=0.022+0.002*(i%5)
    b=0.034+0.003*((i*7)%5)
    h=0.012+0.002*(i%4)
    skew=((i%3)-1)*0.009
    front=[(-a,-b/2,-h),(a,-b/2,-h),(skew,b/2,-h)]
    back=[(x,y,h) for x,y,_ in front]
    coords=[
      (front[0],front[2],front[1]), (back[0],back[1],back[2]),
      (front[0],front[1],back[1]), (front[0],back[1],back[0]),
      (front[1],front[2],back[2]), (front[1],back[2],back[1]),
      (front[2],front[0],back[0]), (front[2],back[0],back[2]),
    ]
    out=[]
    for tri in coords:
        normal=normalize(cross(vsub(tri[1],tri[0]),vsub(tri[2],tri[0])))
        out.append((tri,normal))
    return out

def placement(i,spread):
    # Non-zero cubic-spiral vertical motion ensures genuine 3D occupancy.
    theta=(i*2.399963229728653)%(2*math.pi)
    dz=-0.45+0.9*((i*7)%16)/15
    rad=math.sqrt(max(0.001,1-dz*dz))
    direction=(math.cos(theta)*rad,math.sin(theta)*rad,dz)
    initial=((i%4-1.5)*0.009,((i//4)%4-1.5)*0.009,(i%3-1)*0.011)
    extent=0.23+0.027*(i%6)
    return [initial[j]+direction[j]*extent*spread for j in range(3)]

def quaternion(i,spin):
    # Pure X axis quaternion, unity norm; color and each shard trajectory independent.
    angle=(i+1)*0.23*spin
    return [math.sin(angle/2),0.,0.,math.cos(angle/2)]

def build_glb():
    doc={"asset":{"version":"2.0","generator":PKG+"/"+VERSION,
         "extras":{"meaning":"original non-authoritative VFX, not Rigidbody or Save/Load",
         "loop_setting":"external host only; glTF defines no nonloop"}},
       "scene":0,"scenes":[{"name":"B0131-Cable-Fracture-Debris","nodes":list(range(SHARDS))}],
       "nodes":[],"meshes":[],"materials":[],"animations":[],
       "buffers":[],"bufferViews":[],"accessors":[]}
    for name,rgb,metal,rough in COLORS:
        doc["materials"].append({"name":name,"doubleSided":False,
             "pbrMetallicRoughness":{"baseColorFactor":[*rgb,1.],
                                     "metallicFactor":metal,"roughnessFactor":rough}})
    data=bytearray()
    def accessor(values,comps,kind="FLOAT",target=None):
        nonlocal data
        require(comps in (1,3,4),"source glTF accessor element dimension")
        data+=b"\0"*((-len(data))%4)
        offset=len(data)
        if kind=="FLOAT":
            raw=struct.pack("<"+"f"*len(values),*values)
            component=5126
        else:
            raw=struct.pack("<"+"H"*len(values),*values)
            component=5123
        data+=raw
        vw={"buffer":0,"byteOffset":offset,"byteLength":len(raw)}
        if target:vw["target"]=target
        vid=len(doc["bufferViews"]);doc["bufferViews"].append(vw)
        aid=len(doc["accessors"])
        ac={"bufferView":vid,"componentType":component,
            "count":len(values)//comps,"type":{1:"SCALAR",3:"VEC3",4:"VEC4"}[comps]}
        if kind=="FLOAT" and comps==3:
            ac["min"]=[min(values[j::3]) for j in range(3)]
            ac["max"]=[max(values[j::3]) for j in range(3)]
        if kind=="FLOAT" and comps==1:
            ac["min"]=[min(values)];ac["max"]=[max(values)]
        doc["accessors"].append(ac)
        return aid
    time_access=accessor(list(TIMES),1)
    anim={"name":"B0131_Shards_OutwardBurst_1p20s",
          "extras":{"requested_playback":"ONE_SHOT_HOLD_NOT_ENFORCED",
                    "art_only":True,"shard_count":SHARDS},
          "samplers":[],"channels":[]}
    for i in range(SHARDS):
        tris=triangles_for_shard(i)
        positions=[];normals=[]
        for tri,normal in tris:
            for vert in tri:
                positions.extend(vert)
                normals.extend(normal)
        count=len(positions)//3
        p=accessor(positions,3,target=34962)
        n=accessor(normals,3,target=34962)
        ind=accessor(list(range(count)),1,kind="UNSIGNED_SHORT",target=34963)
        doc["meshes"].append({"name":f"Shard_{i:02d}_PrismMesh",
              "primitives":[{"attributes":{"POSITION":p,"NORMAL":n},
                             "indices":ind,"material":i%4,"mode":4}]})
        doc["nodes"].append({"name":f"Shard_{i:02d}_IndependentPivot","mesh":i,
                             "translation":placement(i,0.),
                             "rotation":quaternion(i,0.),
                             "extras":{"owner":"B01-31","non_collision_vfx":True,
                                       "source_geometry":"original closed 8 triangle prism"}})
        trans=[];quats=[]
        for spread,spin in zip(SPREAD,SPIN):
            trans.extend(placement(i,spread))
            quats.extend(quaternion(i,spin))
        taccess=accessor(trans,3)
        qaccess=accessor(quats,4)
        for path,output in (("translation",taccess),("rotation",qaccess)):
            si=len(anim["samplers"])
            anim["samplers"].append({"input":time_access,"output":output,
                                     "interpolation":"LINEAR"})
            anim["channels"].append({"sampler":si,
                                     "target":{"node":i,"path":path}})
    doc["animations"]=[anim]
    doc["buffers"]=[{"byteLength":len(data)}]
    body=bytes(data)
    jsonbytes=json.dumps(doc,separators=(",",":"),sort_keys=True).encode()
    jsonbytes+=b" "*((-len(jsonbytes))%4)
    binbytes=body+b"\0"*((-len(body))%4)
    full=struct.pack("<4sII",b"glTF",2,12+8+len(jsonbytes)+8+len(binbytes))
    full+=struct.pack("<I4s",len(jsonbytes),b"JSON")+jsonbytes
    full+=struct.pack("<I4s",len(binbytes),b"BIN\0")+binbytes
    require(len(full)==struct.unpack_from("<I",full,8)[0],
            "real GLB2 physical byte framing")
    return full,doc

def lerp(a,b,t):
    return a+(b-a)*t

def sample_poses(t):
    if t>=TIMES[-1]:j=5;alpha=1.
    else:
        j=next(i for i in range(6) if TIMES[i]<=t<TIMES[i+1])
        alpha=(t-TIMES[j])/(TIMES[j+1]-TIMES[j])
    spread=lerp(SPREAD[j],SPREAD[j+1],alpha)
    spin=lerp(SPIN[j],SPIN[j+1],alpha)
    return [(placement(i,spread),quaternion(i,spin)) for i in range(SHARDS)]

def render(t,theme,view="iso",size=(700,430)):
    bg=(20,31,46) if theme=="dark" else (220,231,238)
    fg=(233,241,247) if theme=="dark" else (35,51,68)
    img=Image.new("RGB",size,bg)
    d=ImageDraw.Draw(img)
    font=ImageFont.load_default()
    width,height=size
    if view=="iso":yaw,pitch=(0.48,0.31)
    elif view=="side":yaw,pitch=(0.0,0.06)
    elif view=="top":yaw,pitch=(0.10,1.25)
    else:raise ValueError("unknown 3D camera view")
    mats=[tuple(round(c*255) for c in m[1]) for m in COLORS]
    poses=sample_poses(t)
    faces=[]
    for i in range(SHARDS):
        trans,quat=poses[i]
        a=2*math.atan2(quat[0],quat[3])
        for tri,normal in triangles_for_shard(i):
            projected=[]
            for x,y,z in tri:
                yy=y*math.cos(a)-z*math.sin(a)+trans[1]
                zz=y*math.sin(a)+z*math.cos(a)+trans[2]
                xx=x+trans[0]
                rx=math.cos(yaw)*xx+math.sin(yaw)*zz
                rz=-math.sin(yaw)*xx+math.cos(yaw)*zz
                ry=math.cos(pitch)*yy-math.sin(pitch)*rz
                rd=math.sin(pitch)*yy+math.cos(pitch)*rz
                projected.append((width/2+rx*width*.71,
                                  height/2-ry*height*1.22,rd))
            col=mats[i%4]
            intensity=0.74+0.24*abs(normal[0]*.3+normal[1]*.6+normal[2]*.6)
            color=tuple(min(255,int(c*intensity)) for c in col)
            faces.append((sum(p[2] for p in projected)/3,
                          [(int(v[0]),int(v[1])) for v in projected],color))
    for _,tri,col in sorted(faces,key=lambda item:item[0]):
        d.polygon(tri,fill=col)
    d.text((12,12),"SHARD BURST 3D - "+view.upper()+" / NONAUTHORITY ART",fill=fg,font=font)
    d.text((12,height-22),f"T={t:.2f}s / {SHARDS} ORIGINAL 3D SHARDS / NOT UNITY",fill=fg,font=font)
    return img

def png(image):
    b=io.BytesIO();image.save(b,format="PNG",compress_level=0,optimize=False)
    return b.getvalue()

def make_board(theme):
    canvas=Image.new("RGB",(2100,520),(22,33,46) if theme=="dark" else (225,235,241))
    d=ImageDraw.Draw(canvas)
    for i,t in enumerate(TIMES):
        piece=render(t,theme,"iso",(300,430))
        canvas.paste(piece,(i*300,50))
        d.text((i*300+6,15),f"Key pose {i+1} / {t:.2f}s",
               fill=(238,238,245) if theme=="dark" else (30,43,55))
    return png(canvas)

def create_gif(view):
    frames=[render(min(1.2,i*0.08),"dark",view,(640,400)) for i in range(16)]
    b=io.BytesIO()
    frames[0].save(b,format="GIF",save_all=True,append_images=frames[1:],
                   duration=[80]*len(frames),loop=0,disposal=2,optimize=False)
    data=b.getvalue()
    with Image.open(io.BytesIO(data)) as im:
        require(im.format=="GIF" and im.n_frames==16,"physical GIF 16 frames")
        hashes=[]
        for f in range(16):
            im.seek(f);require(im.info.get("duration")==80,"GIF actual 80ms frame")
            hashes.append(H(im.convert("RGB").tobytes()))
        require(len(set(hashes))==16,"all physical 16 gif frames distinctly rendered")
    return data

def packed_zip(files,path):
    with zipfile.ZipFile(path,"w",compression=zipfile.ZIP_STORED) as z:
        for name in sorted(files):
            zi=zipfile.ZipInfo(name,(2020,1,1,0,0,0))
            zi.create_system=3;zi.external_attr=0o100644<<16
            zi.compress_type=zipfile.ZIP_STORED
            z.writestr(zi,files[name])
    with zipfile.ZipFile(path) as z:
        require(z.testzip() is None and len(z.namelist())==len(files)
                and all(z.read(n)==files[n] for n in files),
                "original exact stored ZIP archive CRC and bytes")

def build(out):
    out.mkdir(parents=True,exist_ok=True)
    glb,doc=build_glb()
    files={BASE+".glb":glb}
    for theme in ("dark","light"):
        files[BASE+"-"+theme+"-7poses.png"]=make_board(theme)
    for view in ("iso","side","top"):
        files[BASE+"-"+view+"-16frames-80ms.gif"]=create_gif(view)
    timeline={
        "package":PKG,"version":VERSION,"original_3d_prism_shards":SHARDS,
        "original_faces_each_shard":8,
        "materials":[m[0] for m in COLORS],
        "key_times_seconds":TIMES,"radial_spread":SPREAD,"spin_multipliers":SPIN,
        "real_animation_tracks":SHARDS*2,
        "real_anim_keyframes":SHARDS*2*len(TIMES),
        "actual_80ms_gif_frames":16,
        "gif_duration_ms":16*80,
        "glTF_loop_spec":"NONE; host playback ONE_SHOT_HOLD requested only",
        "unity_G1":"NOT_RUN","gameplay_detach":"NOT_IMPLEMENTED",
        "physx":"NOT_RUN","save_load":"NOT_RUN",
        "selected_save_adapter_pr":596,
        "frozen_save_adapter_sha":"efb2b06eafedab5469b57f163152d96aca3c5915",
        "frozen_reattach_sha":"d94400d95d13db7f27e1533df05c9292d5380431",
        "frozen_save_contract_sha":"33094f154c7b91119b36d58aab285e6e83c5de57"
    }
    files["timeline.json"]=(json.dumps(timeline,indent=2,sort_keys=True)+"\n").encode()
    receipt={
        "package_id":PKG,"version":VERSION,"owner":"B01-31",
        "real_shards":16,"real_geometry_faces":128,
        "real_nodes":16,"real_meshes":16,"real_materials":4,
        "real_animation_channels":32,"animation_keyframes_total":224,
        "true_glb_sha256":H(glb),"actual_zip_entries":8,
        "actual_files":[{"path":n,"sha256":H(raw),"bytes":len(raw)}
                        for n,raw in sorted(files.items())],
        "actual_unity":"NOT_RUN","PhysX":"NOT_RUN",
        "SAVELOAD":"NOT_RUN","formal_G0":"PENDING","G2":"HOLD",
        "codex_ACK":False,"integrated":False
    }
    files["receipt.json"]=(json.dumps(receipt,indent=2,sort_keys=True)+"\n").encode()
    for name,content in files.items():
        (out/name).write_bytes(content)
        require((out/name).read_bytes()==content,"actual file write/readback")
    archive=out.parent/(out.name+"-physical.zip")
    packed_zip(files,archive)
    print(json.dumps({"status":"PASS","real_3d_shards":16,
                      "real_unique_models":16,"real_faces":128,
                      "real_materials":4,"real_animation_channels":32,
                      "physical_keyframes":224,"actual_16frame_gifs":3,
                      "actual_projected_3d_storyboards":2,
                      "physical_zip_members":8,"original_GLB_sha256":H(glb),
                      "actual_Unity":"NOT_RUN"}))
    return 0

if __name__=="__main__":
    parser=argparse.ArgumentParser();parser.add_argument("--output",required=True)
    args=parser.parse_args()
    try:
        raise SystemExit(build(Path(args.output)))
    except Exception as exc:
        print("W18_ORIGINAL_3D_SHARDS_ERROR "+type(exc).__name__+": "+str(exc),file=sys.stderr)
        raise
