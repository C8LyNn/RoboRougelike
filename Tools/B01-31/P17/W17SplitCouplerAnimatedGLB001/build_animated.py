#!/usr/bin/env python3
"""B01-31 original TWO-NODE KINEMATIC ART for physically frozen W16 GLB.
Produces a real glTF2 animation channel asset and actual geometry-projected
7-pose preview PNGs. No gameplay hooks, physics, Codex or Save/Load.
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

PACKAGE="B01-31-SPLIT-COUPLER-KINEMATIC-GLB-001"
VERSION="P17-W17-R1"
W16_SOURCE_SHA="5df35807c61d3e238a04b3b3a493e095a589b001"
W16_GLB_SHA="eeae6a553ca6136b5eaa97a65d5dd995f5f28fcb8bc54eb2180dfe28bc3653d7"
W16_NAME="B0131SplitCouplerConcept"
OUT_NAME="B0131SplitCouplerConcept-SnapRecoil.glb"
TIMES=(0.,0.12,0.25,0.40,0.64,0.88,1.20)
LEFT=(-0.20,-0.28,-0.44,-0.49,-0.41,-0.36,-0.36)
RIGHT=tuple(-x for x in LEFT)
ANGLES=(0.,-5.,-15.,-19.,-11.,-3.,0.)
H=lambda b:hashlib.sha256(b).hexdigest()
def require(c,reason):
    if not c:raise ValueError("B0131_W17_KINEMATIC_ART_RED "+reason)

def parse_glb(data):
    require(len(data)>=28 and data[:4]==b"glTF","genuine GLB magic/size")
    version,size=struct.unpack_from("<II",data,4)
    require(version==2 and size==len(data),"glTF2 version and physical length")
    jlen,jtype=struct.unpack_from("<I4s",data,12)
    require(jtype==b"JSON" and jlen%4==0 and 28+jlen<=len(data),
            "JSON chunk alignment/bounds")
    document=json.loads(data[20:20+jlen].decode("utf8"))
    blen,btype=struct.unpack_from("<I4s",data,20+jlen)
    require(btype==b"BIN\0" and 28+jlen+blen==len(data),
            "genuine GLB2 binary chunk")
    return document,data[28+jlen:],data[20:20+jlen]

def pack_glb(doc,buf):
    json_data=json.dumps(doc,sort_keys=True,separators=(",",":")).encode()
    json_data+=b" "*((-len(json_data))%4)
    buf+=b"\0"*((-len(buf))%4)
    result=(struct.pack("<4sII",b"glTF",2,12+8+len(json_data)+8+len(buf))
            +struct.pack("<I4s",len(json_data),b"JSON")+json_data
            +struct.pack("<I4s",len(buf),b"BIN\0")+buf)
    require(len(result)==struct.unpack_from("<I",result,8)[0],
            "physical packed GLB has correct size")
    return result

def initial_source(source_dir):
    source_file=source_dir/(W16_NAME+".glb")
    receipt_file=source_dir/"receipt.json"
    require(source_file.is_file() and receipt_file.is_file(),
            "real immutable W16 original geometry+receipt unavailable")
    raw=source_file.read_bytes()
    require(H(raw)==W16_GLB_SHA,"frozen source W16 GLB sha256 mismatch")
    receipt=json.loads(receipt_file.read_bytes())
    require(receipt["package_id"]=="B01-31-SPLIT-COUPLER-CONCEPT3D-001"
            and receipt["version"]=="P17-W16-R1"
            and receipt["total_glb_triangles"]==1312
            and receipt["minimum_design_pin_tip_gap_m"]==0.26
            and any(x["file"]==source_file.name and x["sha256"]==W16_GLB_SHA
                    for x in receipt["actual_binary_assets"]),
            "physical source receipt/original geometry mismatch")
    doc,buf,_=parse_glb(raw)
    require(doc["asset"]["version"]=="2.0" and
            len(doc["nodes"])==2 and len(doc["meshes"])==2 and
            [n["name"] for n in doc["nodes"]]==
            ["LeftConnectorPivot","RightConnectorPivot"] and
            [n["translation"] for n in doc["nodes"]]==[[-.2,0,0],[.2,0,0]]
            and len(doc["materials"])==6 and
            len(doc["bufferViews"])==36 and len(doc["accessors"])==36,
            "actual original W16 two-pivot six-material mesh topology altered")
    declared=doc["buffers"][0]["byteLength"]
    require(declared<=len(buf) and len(buf)-declared<=3,
            "real original W16 BIN chunk padding invalid")
    return doc,buf[:declared],raw

def quat_x(angle_deg):
    a=math.radians(angle_deg)/2
    return [math.sin(a),0.,0.,math.cos(a)]

def build_animated(doc,source_data):
    require(len(TIMES)==7 and len(LEFT)==len(RIGHT)==len(ANGLES)==7,
            "one full 7 keyframe motion choreography required")
    require(all(TIMES[i+1]>TIMES[i] for i in range(6)) and TIMES[-1]==1.2,
            "strict 1.2s original timeline")
    require(all(right-left-0.14>=0.25999 for left,right in zip(LEFT,RIGHT)),
            "physical total-geometry X gap must remain >=.26m")
    # Deep copy JSON in pure form to ensure source GLB JSON untouched.
    new=json.loads(json.dumps(doc))
    new["asset"]["generator"]=PACKAGE+"/"+VERSION
    new["asset"]["extras"]={
      "original_static_sha256":W16_GLB_SHA,
      "provenance":"original B01-31 two-root GLB unchanged geometry, only additive animation",
      "art_only":"NOT Unity/PhysX/detach/SaveLoad",
      "preview_playback_recommendation":"PLAY_ONCE_AND_HOLD, not enforced by glTF"}
    base_count=len(new["bufferViews"])
    acc_count=len(new["accessors"])
    buf=bytearray(source_data)
    def append_accessor(values,comps):
        nonlocal buf
        require(comps in (1,3,4) and len(values)%comps==0,
                "physically valid animation accessor component")
        require(all(math.isfinite(x) for x in values),"nonfinite animation values")
        buf+=b"\0"*((-len(buf))%4)
        offset=len(buf)
        raw=struct.pack("<"+"f"*len(values),*values)
        buf+=raw
        vi=len(new["bufferViews"])
        new["bufferViews"].append({"buffer":0,"byteOffset":offset,
                                    "byteLength":len(raw)})
        ai=len(new["accessors"])
        accessor={"bufferView":vi,"componentType":5126,
                  "count":len(values)//comps,
                  "type":{1:"SCALAR",3:"VEC3",4:"VEC4"}[comps]}
        if comps==1:
            accessor["min"]=[min(values)]
            accessor["max"]=[max(values)]
        new["accessors"].append(accessor)
        return ai
    taccess=append_accessor(TIMES,1)
    animations={"name":"B0131_SnapRecoil_1p20s",
        "extras":{"owner":"B01-31",
            "mode":"ART_ONLY_DISCONNECTED_NO_ATTACH",
            "playback_recommendation":"ONE_SHOT_HOLD_NO_LOOP; host chooses, glTF does not encode loop modes",
            "min_static_coupler_gap_m":0.26},
        "channels":[],"samplers":[]}
    for side,translations,angles in [(0,LEFT,ANGLES),(1,RIGHT,tuple(-a for a in ANGLES))]:
        coords=[]
        rotations=[]
        for x,a in zip(translations,angles):
            coords.extend((x,0.,0.))
            rotations.extend(quat_x(a))
        pa=append_accessor(coords,3)
        qa=append_accessor(rotations,4)
        for path,acc in (("translation",pa),("rotation",qa)):
            sampler=len(animations["samplers"])
            animations["samplers"].append({
                "input":taccess,"interpolation":"LINEAR","output":acc})
            animations["channels"].append({
                "sampler":sampler,"target":{"node":side,"path":path}})
    new["animations"]=[animations]
    new["buffers"][0]["byteLength"]=len(buf)
    require(len(new["accessors"])==acc_count+5 and
            len(new["bufferViews"])==base_count+5 and
            len(animations["channels"])==4,"real 2-node animation PBR geometry untouched")
    return pack_glb(new,bytes(buf)),new

def read_source_triangle_views(doc,buf):
    def accessor(i):
        acc=doc["accessors"][i]
        v=doc["bufferViews"][acc["bufferView"]]
        require(acc["componentType"]==5126 and acc["type"]=="VEC3"
                and v["byteOffset"]+v["byteLength"]<=len(buf),
                "real original float32 geometry accessor")
        return struct.unpack_from("<"+"f"*(acc["count"]*3),buf,v["byteOffset"])
    faces=[]
    for side,mesh in enumerate(doc["meshes"]):
        for prim in mesh["primitives"]:
            coords=accessor(prim["attributes"]["POSITION"])
            require(len(coords)%9==0,"actual original triangle geometry")
            for i in range(0,len(coords),9):
                faces.append((side,prim["material"],
                    [coords[i+j:i+j+3] for j in (0,3,6)]))
    require(len(faces)==1312,"W16 real original 1312 triangles were not regenerated")
    return faces

def render_board(doc,buf,theme):
    faces=read_source_triangle_views(doc,buf)
    assert theme in ("dark","light")
    bg=(22,32,45) if theme=="dark" else (226,234,240)
    fg=(235,243,248) if theme=="dark" else (32,48,62)
    panel_width=300
    image=Image.new("RGB",(panel_width*7,520),bg)
    d=ImageDraw.Draw(image)
    font=ImageFont.load_default()
    colors=[tuple(round(x*255) for x in m["pbrMetallicRoughness"]["baseColorFactor"][:3])
            for m in doc["materials"]]
    yaw=.30
    pitch=.24
    for frame,time in enumerate(TIMES):
        ox=frame*panel_width
        d.text((ox+15,12),f"T={time:.2f} S / KINEMATIC ART ONLY",
               fill=fg,font=font)
        d.rectangle((ox+5,38,ox+294,484),outline=fg,width=1)
        triangles=[]
        for side,material,tri in faces:
            rot=math.radians((ANGLES[frame] if side==0 else -ANGLES[frame]))
            offset=LEFT[frame] if side==0 else RIGHT[frame]
            transformed=[]
            for x,y,z in tri:
                # Rotation about X affects YZ but cannot close the world X-gap.
                ry=math.cos(rot)*y-math.sin(rot)*z
                rz=math.sin(rot)*y+math.cos(rot)*z
                wx=x+offset
                xx=math.cos(yaw)*wx+math.sin(yaw)*rz
                zz=-math.sin(yaw)*wx+math.cos(yaw)*rz
                yy=math.cos(pitch)*ry-math.sin(pitch)*zz
                depth=math.sin(pitch)*ry+math.cos(pitch)*zz
                transformed.append((ox+150+xx*111,245-yy*170,depth))
            col=colors[material]
            triangles.append((sum(x[2] for x in transformed)/3,
                              [(round(x[0]),round(x[1])) for x in transformed],
                              tuple(int(v*.9) for v in col)))
        for _,poly,color in sorted(triangles,key=lambda a:a[0]):
            d.polygon(poly,fill=color)
        d.text((ox+13,498),f"L X {LEFT[frame]:+.2f} / R X {RIGHT[frame]:+.2f}",
               fill=fg,font=font)
    out=io.BytesIO()
    image.save(out,format="PNG",optimize=False,compress_level=0)
    with Image.open(io.BytesIO(out.getvalue())) as test:
        test.load()
        require(test.format=="PNG" and test.mode=="RGB" and
                test.size==(2100,520),"real 7-pose geometry preview decode")
    return out.getvalue()

def write_zip(output,files):
    archive=output.parent/(output.name+"-physical.zip")
    with zipfile.ZipFile(archive,"w",compression=zipfile.ZIP_STORED) as z:
        for name in sorted(files):
            zi=zipfile.ZipInfo(name,(2020,1,1,0,0,0))
            zi.create_system=3;zi.external_attr=0o100644<<16
            zi.compress_type=zipfile.ZIP_STORED
            z.writestr(zi,files[name])
    with zipfile.ZipFile(archive) as z:
        require(z.testzip() is None and len(z.namelist())==5
                and all(z.read(name)==raw for name,raw in files.items()),
                "physical original animation distribution archive readback")
    return archive

def produce(original,output):
    doc,buf,source_raw=initial_source(original)
    animated,new=build_animated(doc,buf)
    decoded,new_bin,_=parse_glb(animated)
    require(new_bin[:len(buf)]==buf,
            "animated GLB binary source geometry must remain byte-identical")
    output.mkdir(parents=True,exist_ok=True)
    out={OUT_NAME:animated}
    phases=[]
    for name in ("dark","light"):
        filename="B0131-SplitCoupler-SnapRecoil-"+name+"-7poses.png"
        out[filename]=render_board(doc,buf,name)
        phases.append({"file":filename,"sha256":H(out[filename]),
                       "bytes":len(out[filename])})
    timeline={
      "package_id":PACKAGE,"version":VERSION,
      "original_static_source_pr":599,
      "original_static_source_sha":W16_SOURCE_SHA,
      "original_static_GLB_sha256":W16_GLB_SHA,
      "format":"glTF 2.0 single animation, four transform channels, two independent nodes",
      "animation_name":"B0131_SnapRecoil_1p20s",
      "key_times_sec":TIMES,
      "left_node_translation_x":LEFT,
      "right_node_translation_x":RIGHT,
      "left_rotation_x_degrees":ANGLES,
      "right_rotation_x_degrees":[-a for a in ANGLES],
      "minimum_entire_mesh_static_source_world_x_gap_m":0.26,
      "guarantee":"Both root X translations only separate farther, rotations about X preserve X range; linear interpolated positions also cannot cross",
      "requested_playback":"ONE_SHOT_HOLD, glTF animation has no native loop flag",
      "not_guaranteed":"Unity importer, runtime animation, reattach authority, Save/Load or PhysX",
      "runtime_native_unity":"NOT_RUN",
      "gameplay":"NOT_IMPLEMENTED",
      "frozen_reattach_sha":"d94400d95d13db7f27e1533df05c9292d5380431",
      "frozen_save_sha":"33094f154c7b91119b36d58aab285e6e83c5de57"
    }
    out["timeline.json"]=(json.dumps(timeline,indent=2,sort_keys=True)+"\n").encode()
    receipt={
      "package_id":PACKAGE,"version":VERSION,"owner":"B01-31",
      "original_static_source_SHA":W16_SOURCE_SHA,
      "original_static_GLB_SHA256":W16_GLB_SHA,
      "actual_animated_GLB_SHA256":H(animated),
      "animated_GLB_bytes":len(animated),
      "original_geometry_binary_prefix_bytes":len(buf),
      "all_original_source_mesh_byte_prefix_unchanged":True,
      "original_triangle_count":1312,
      "original_materials":6,
      "two_root_nodes":2,
      "animation_samplers":4,
      "animation_channels":4,
      "animation_keyframes_per_channel":7,
      "frame_timing_seconds":TIMES,
      "smallest_source_X_gap_m":0.26,
      "actual_preview_PNGs":phases,
      "actual_files":[{"file":name,"sha256":H(raw),"bytes":len(raw)}
                      for name,raw in sorted(out.items())],
      "unity_G1":"NOT_RUN","physx":"NOT_RUN",
      "game_save_load":"NOT_RUN",
      "B01_40_formal_G0":"PENDING",
      "G2":"HOLD","codex_ack":False,"integrated":False
    }
    out["receipt.json"]=(json.dumps(receipt,indent=2,sort_keys=True)+"\n").encode()
    for name,raw in out.items():
        (output/name).write_bytes(raw)
        require((output/name).read_bytes()==raw,
                "real original binary file write/readback")
    archive=write_zip(output,out)
    print(json.dumps({"status":"PASS","actual_animated_GLTF2_GLB":1,
      "unchanged_source_mesh_triangles":1312,
      "original_materials":6,"root_transform_nodes":2,
      "animation_channels":4,"samples_per_channel":7,
      "real_projected_PNG_storyboards":2,
      "real_distribution_ZIP_members":5,
      "glb_sha256":H(animated),
      "actual_source_binary_byte_prefix_length":len(buf),
      "real_Unity":"NOT_RUN"}))
    return 0

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--original",required=True)
    p.add_argument("--output",required=True)
    a=p.parse_args()
    try:sys.exit(produce(Path(a.original),Path(a.output)))
    except Exception as exc:
        print("B0131_W17_ORIGINAL_ANIMATED_GLB_RED "+str(exc),file=sys.stderr)
        raise
