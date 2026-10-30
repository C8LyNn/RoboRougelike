#!/usr/bin/env python3
"""Independent *physical* receiver for B01-31 new 3D two-node glTF animation.
Reads ORIGINAL W16 GLB bytes from a separate immutable GitHub Actions run;
checks new animated glTF against original source byte-for-byte. No producer import.
No simulated Unity/PhysX/SaveLoad claim.
"""
from __future__ import annotations
import argparse
import copy
import hashlib
import io
import json
import math
from pathlib import Path
import struct
import sys
import zipfile
from PIL import Image, ImageChops

PREFIX="B0131SplitCouplerConcept"
ANIM=PREFIX+"-SnapRecoil.glb"
PKG="B01-31-SPLIT-COUPLER-KINEMATIC-GLB-001"
ORIG_SHA="eeae6a553ca6136b5eaa97a65d5dd995f5f28fcb8bc54eb2180dfe28bc3653d7"
ORIG_SRC_SHA="5df35807c61d3e238a04b3b3a493e095a589b001"
T=(0.0,0.12,0.25,0.40,0.64,0.88,1.20)
LEFT=(-0.20,-0.28,-0.44,-0.49,-0.41,-0.36,-0.36)
RIGHT=tuple(-x for x in LEFT)
ANGLES=(0.,-5.,-15.,-19.,-11.,-3.,0.)
H=lambda b:hashlib.sha256(b).hexdigest()

def check(c,reason):
    if not c:raise ValueError("B0131_W17_INDEPENDENT_REAL_GLB_REJECT "+reason)

def parse_glb(blob):
    check(len(blob)>28 and blob[:4]==b"glTF","physical glTF original 4-byte magic")
    v,n=struct.unpack_from("<II",blob,4)
    check(v==2 and n==len(blob),"real GLB2 version/size")
    length,typ=struct.unpack_from("<I4s",blob,12)
    check(typ==b"JSON" and length%4==0 and length+28<=len(blob),
          "real original JSON view bounds")
    doc=json.loads(blob[20:20+length].decode("utf8"))
    bsize,btype=struct.unpack_from("<I4s",blob,20+length)
    check(btype==b"BIN\0" and 28+length+bsize==len(blob),
          "real GLB2 BIN view bounds")
    bin_data=blob[28+length:]
    return doc,bin_data

def read_accessor(doc,buf,index):
    ac=doc["accessors"][index]
    vw=doc["bufferViews"][ac["bufferView"]]
    comps={"SCALAR":1,"VEC3":3,"VEC4":4}
    check(ac["componentType"]==5126 and ac["type"] in comps,
          "real animation keyframe accessors must use FLOAT32")
    n=ac["count"]*comps[ac["type"]]
    check(vw["buffer"]==0 and vw["byteOffset"]%4==0
          and vw["byteLength"]==n*4
          and vw["byteOffset"]+vw["byteLength"]<=len(buf)
          and "target" not in vw,
          "real animation binary view stride/target/offset/length")
    a=struct.unpack_from("<"+"f"*n,buf,vw["byteOffset"])
    check(all(math.isfinite(x) for x in a),
          "nan/inf physical actual glTF animation coordinates")
    return a

def source_geometry(doc,buf):
    check(doc["asset"]["version"]=="2.0"
          and len(doc["meshes"])==2 and len(doc["nodes"])==2
          and len(doc["materials"])==6
          and len(doc["accessors"])==36 and len(doc["bufferViews"])==36,
          "original actual W16 static geometry/mats/count")
    sides=[]
    for s,mesh in enumerate(doc["meshes"]):
        positions=[]
        check(len(mesh["primitives"])==6,"source two meshes each six real material groups")
        for prim in mesh["primitives"]:
            ac=doc["accessors"][prim["attributes"]["POSITION"]]
            vw=doc["bufferViews"][ac["bufferView"]]
            check(ac["componentType"]==5126 and ac["type"]=="VEC3"
                  and vw["byteOffset"]+vw["byteLength"]<=len(buf),
                  "physical original actual position vertices")
            raw=struct.unpack_from("<"+"f"*(ac["count"]*3),buf,vw["byteOffset"])
            positions.extend(raw[::3])
        sides.append((min(positions),max(positions)))
    # Actual W16 maxx/left occurs at original LOCAL x=0.07 with pivot -.2;
    # actual right local x=-.07, translation .2. Derive entire mesh interval
    # directly from actual binary rather than hard-code claimed sizes.
    return sides

def validate_anim_document(original,doc,buf,source_sides):
    check(doc["asset"]["version"]=="2.0"
          and doc["asset"]["extras"]["original_static_sha256"]==ORIG_SHA,
          "animated original source asset provenance")
    for key in ("scene","scenes","nodes","meshes","materials"):
        check(doc[key]==original[key],
              "original W16 "+key+" must remain unmodified")
    for key in ("accessors","bufferViews"):
        check(doc[key][:len(original[key])]==original[key],
              "original W16 "+key+" source bytes descriptors must remain unchanged")
    check(len(doc["accessors"])==41 and len(doc["bufferViews"])==41,
          "exact five new physical binary keyframe accessors")
    check(doc["animations"] and len(doc["animations"])==1,
          "one single authored seven-keyframe recoil glTF 2 animation only")
    anim=doc["animations"][0]
    check(anim["name"]=="B0131_SnapRecoil_1p20s" and
          len(anim["samplers"])==4 and len(anim["channels"])==4,
          "four physical glTF motion channels in one clip")
    check(anim["extras"]["mode"]=="ART_ONLY_DISCONNECTED_NO_ATTACH"
          and "not enforced by glTF" in doc["asset"]["extras"]["preview_playback_recommendation"],
          "must not claim engine-enforced one-shot loop or real attach permissions")
    expect_targets=[(0,"translation"),(0,"rotation"),
                    (1,"translation"),(1,"rotation")]
    actual_targets=[(c["target"]["node"],c["target"]["path"])
                    for c in anim["channels"]]
    check(actual_targets==expect_targets,"both independent original pivot transform channel targets")
    info={}
    for i,channel in enumerate(anim["channels"]):
        sampler=anim["samplers"][channel["sampler"]]
        check(channel["sampler"]==i and sampler["interpolation"]=="LINEAR",
              "actual separate float interpolation source sampler")
        times=read_accessor(doc,buf,sampler["input"])
        check(len(times)==7 and
              all(abs(times[j]-T[j])<2e-7 for j in range(7)) and
              all(times[j+1]>times[j] for j in range(6)),
              "actual physical original 1.20s seven ordered sample times")
        values=read_accessor(doc,buf,sampler["output"])
        side,path=actual_targets[i]
        comps=3 if path=="translation" else 4
        check(len(values)==7*comps,"glTF animation physical output count")
        vec=[values[j*comps:(j+1)*comps] for j in range(7)]
        info[(side,path)]=vec
        for j,row in enumerate(vec):
            if path=="translation":
                expected=(LEFT if side==0 else RIGHT)[j]
                check(abs(row[0]-expected)<2e-7 and
                      abs(row[1])<1e-7 and abs(row[2])<1e-7,
                      "real translation keyframe deviates from authored safe separation")
            else:
                expected=math.radians(ANGLES[j]*(1 if side==0 else -1))/2
                check(abs(row[0]-math.sin(expected))<2e-7
                      and abs(row[1])<1e-7 and abs(row[2])<1e-7
                      and abs(row[3]-math.cos(expected))<2e-7
                      and abs(sum(x*x for x in row)-1)<1e-5,
                      "actual float32 quaternion invalid or wrong own X rotation")
    # Test interpolation in every 5ms interval; body X ranges derived from
    # immutable W16 original *geometry* binary, not a receipt assumption.
    points=[]
    for n in range(241):
        t=n/200
        j=min(next((i for i in range(6) if T[i]<=t<=T[i+1]),5),5)
        frac=(t-T[j])/(T[j+1]-T[j])
        l=LEFT[j]*(1-frac)+LEFT[j+1]*frac
        r=RIGHT[j]*(1-frac)+RIGHT[j+1]*frac
        gap=(source_sides[1][0]+r)-(source_sides[0][1]+l)
        points.append(gap)
    check(min(points)>0.25999 and max(points)>0.8,
          "actual original triangle geometry world-X clearance during all linear interpolated poses")
    check(abs(info[(0,"translation")][0][0]-original["nodes"][0]["translation"][0])<1e-7
          and abs(info[(1,"translation")][0][0]-original["nodes"][1]["translation"][0])<1e-7,
          "first animated pose diverges from original static source")
    return min(points),max(points),len(points)

def independent_mutations(orig,doc,buf,side_bounds):
    n=0
    attempts=[]
    a=copy.deepcopy(doc);a["animations"][0]["channels"][0]["target"]["node"]=1;attempts.append(a)
    a=copy.deepcopy(doc);a["animations"][0]["channels"].pop();attempts.append(a)
    a=copy.deepcopy(doc);a["materials"][0]["name"]="tampered_game";attempts.append(a)
    a=copy.deepcopy(doc);a["animations"][0]["samplers"][2]["interpolation"]="STEP";attempts.append(a)
    a=copy.deepcopy(doc);a["asset"]["extras"]["original_static_sha256"]="0"*64;attempts.append(a)
    for mutated in attempts:
        try:validate_anim_document(orig,mutated,buf,side_bounds)
        except (ValueError,KeyError,IndexError):n+=1
        else:raise AssertionError("FALSE GREEN bad animated GLB semantic mutation")
    check(n==5,"five independent test negative controls must be rejected")
    return n

def verify(source_dir,out):
    src=source_dir/(PREFIX+".glb")
    check(src.is_file(),"missing genuine original W16 physical GLB")
    input_blob=src.read_bytes()
    check(H(input_blob)==ORIG_SHA,"original frozen W16 source digest mismatch")
    original,orig_bin=parse_glb(input_blob)
    original_bin=orig_bin[:original["buffers"][0]["byteLength"]]
    source_sides=source_geometry(original,original_bin)
    rec=json.loads((out/"receipt.json").read_bytes())
    tl_blob=(out/"timeline.json").read_bytes()
    timeline=json.loads(tl_blob)
    check(rec["package_id"]==PKG and rec["version"]=="P17-W17-R1"
          and rec["original_static_source_SHA"]==ORIG_SRC_SHA
          and rec["original_static_GLB_SHA256"]==ORIG_SHA
          and rec["original_triangle_count"]==1312
          and timeline["package_id"]==PKG
          and timeline["original_static_source_sha"]==ORIG_SRC_SHA
          and timeline["original_static_GLB_sha256"]==ORIG_SHA,
          "real source immutable original provenance and version mismatch")
    check(rec["unity_G1"]=="NOT_RUN" and rec["physx"]=="NOT_RUN"
          and rec["game_save_load"]=="NOT_RUN"
          and timeline["frozen_reattach_sha"]=="d94400d95d13db7f27e1533df05c9292d5380431"
          and timeline["frozen_save_sha"]=="33094f154c7b91119b36d58aab285e6e83c5de57",
          "native game/Save false green or source SHA change")
    blob=(out/ANIM).read_bytes()
    check(H(blob)==rec["actual_animated_GLB_SHA256"]
          and len(blob)==rec["animated_GLB_bytes"],
          "real animated physical GLB binary/receipt SHA")
    doc,buf=parse_glb(blob)
    check(buf[:len(original_bin)]==original_bin
          and doc["buffers"][0]["byteLength"]>=len(original_bin),
          "every original GLB actual vertex/index/normal byte changed")
    clearance,largest,samples=validate_anim_document(original,doc,buf,source_sides)
    check(rec["animation_samplers"]==4 and rec["animation_channels"]==4
          and rec["animation_keyframes_per_channel"]==7
          and rec["two_root_nodes"]==2
          and rec["original_geometry_binary_prefix_bytes"]==len(original_bin)
          and rec["all_original_source_mesh_byte_prefix_unchanged"] is True,
          "original physical clip receipt claims mismatch")
    expected_files={ANIM,"timeline.json","receipt.json",
      "B0131-SplitCoupler-SnapRecoil-dark-7poses.png",
      "B0131-SplitCoupler-SnapRecoil-light-7poses.png"}
    check(len(rec["actual_files"])==4 and
          set(x["file"] for x in rec["actual_files"])==expected_files-{"receipt.json"},
          "actual source original 4 source generated files plus receipt")
    check(len(rec["actual_preview_PNGs"])==2,"real animation pose storyboards missing")
    for x in rec["actual_files"]:
        payload=(out/x["file"]).read_bytes()
        check(H(payload)==x["sha256"] and len(payload)==x["bytes"],
              "actual physical source model/JSON/board SHA/byte mismatch")
    for item in rec["actual_preview_PNGs"]:
        png=(out/item["file"]).read_bytes()
        check(H(png)==item["sha256"] and len(png)==item["bytes"],
              "actual PNG original SHA/original receipt")
        with Image.open(io.BytesIO(png)) as im:
            im.load()
            check(im.format=="PNG" and im.mode=="RGB" and im.size==(2100,520),
                  "physical 7-pose original PNG raster dimensions")
            # Each panel is an independently projected real 3D state. Count
            # visually distinct panels, not just a single image header.
            fingerprints=[]
            for i in range(7):
                crop=im.crop((i*300+10,70,i*300+290,450))
                fingerprints.append(H(crop.tobytes()))
            check(len(set(fingerprints))==7,
                  "all seven actual authored world-pose renderings must be physically visually different")
    archive=out.parent/(out.name+"-physical.zip")
    with zipfile.ZipFile(archive) as z:
        check(z.testzip() is None and set(z.namelist())==expected_files
              and len(z.namelist())==5,
              "actual original five-file physically produced distribution ZIP CRC")
        check(all(z.read(n)==(out/n).read_bytes() for n in expected_files),
              "actual distribution archive binary vs loose file mismatch")
    count=independent_mutations(original,doc,buf,source_sides)
    for broken in (
        b"GL0T"+blob[4:],
        blob[:8]+struct.pack("<I",len(blob)+4)+blob[12:],
    ):
        try:parse_glb(broken)
        except (ValueError,struct.error):count+=1
        else:raise AssertionError("false green corrupt GLB2 binary header")
    check(count==7,"seven independent invalid-source negative controls")
    print(json.dumps({"status":"PASS","real_animated_glTF2_binary":True,
      "original_W16_geometry_unchanged_bytes":len(original_bin),
      "real_materials":6,"original_triangle_faces":1312,
      "real_node_transform_tracks":4,"actual_keyframes_per_track":7,
      "independent_linear_pose_samples":samples,
      "minimum_animated_geometry_x_gap_m":round(clearance,5),
      "largest_animated_geometry_x_gap_m":round(largest,5),
      "real_genuine_pose_PNG":2,
      "independent_negative_mutants_rejected":count,
      "real_distribution_zip_members":5,"animated_GLTF2_sha256":H(blob),
      "real_Unity":"NOT_RUN","true_SaveLoad":"NOT_RUN"}))
    return 0

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--original",required=True)
    p.add_argument("--output",required=True)
    a=p.parse_args()
    try:sys.exit(verify(Path(a.original),Path(a.output)))
    except Exception as e:
        print("B0131_W17_INDEPENDENT_RECEIVER_FAIL "+type(e).__name__+": "+str(e),file=sys.stderr)
        raise
