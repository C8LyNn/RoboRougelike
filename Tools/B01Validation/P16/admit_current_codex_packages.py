#!/usr/bin/env python3
"""B01-40 PASS16: immutable-original Git admission for current Codex + CHL + PARTSIZE.

This is a REAL cross-source collision and schema gate, not a Unity compile, import
or proof that B01-20/B01-23 packages can run in the canonical game.
Does not merge trees, modify Assets, or access a Unity user save directory.
"""
import argparse
from collections import Counter
import copy
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

CODEX="589b60a912096d1e9917cb0bbac73a53e7543b4f"
CHL="c7b2a234a286089383503da41883d624fe650551"
PART="be2be0e4802bc3f450179901f9ae91c6d84bcb5b"
ARENA_OWNER="691c8cb6b0be086d455e530f2531de7f3753a0ba"
PINS={
    "codex_map_draft": ("codex","Assets/MapEditor/MapDraft.cs","0331a54a1545ecd1ea18b616ef13fed113474b8c"),
    "codex_map_storage": ("codex","Assets/MapEditor/MapStorage.cs","790a9683a5d2f3daf81f41e5bfa2f31502d79578"),
    "arena_map_codex": ("codex","Docs/CX08/Fixtures/LD02_ARENA.json","e39e2f174984832bd97311c050e3c22fd6fb1175"),
    "cover_map_codex": ("codex","Docs/CX08/Fixtures/LD02_COVER_CHOICE.json","8586312b9606d94b088e4e7640c28105e2d6bac1"),
    "arena_owner": ("owner","Research/B01-25/P14/LD02_ARENA.json","e39e2f174984832bd97311c050e3c22fd6fb1175"),
    "cover_owner": ("owner","Research/B01-25/P15/LD02_COVER_CHOICE.json","8586312b9606d94b088e4e7640c28105e2d6bac1"),
}
SOURCES={"codex":CODEX,"chl":CHL,"part":PART,"owner":ARENA_OWNER}
PREFIXES={"chl":"Assets/RoboRougelikeChallenges","part":"Assets/PARTSIZE001"}
EXPECTED_META={"chl":26,"part":17}
EXPECTED_ASMDEF={"chl":4,"part":3}
CORNER_CELLS={(0,0),(0,9),(11,0),(11,9)}
GUID=re.compile(rb"(?m)^guid:\s*([0-9a-f]{32})\s*$")

class Reject(ValueError):
    pass

def require(ok,reason):
    if not ok:raise Reject(reason)

def git(repo,*args):
    p=subprocess.run(["git","-C",str(repo),*args],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    if p.returncode:raise Reject("GIT_OBJECT_UNAVAILABLE "+str(args)+": "+p.stderr.decode(errors="replace")[:160])
    return p.stdout

def tree(repo,commit):
    # -z: paths with tabs, whitespace or non-ASCII cannot corrupt line parsing.
    objects={}
    for row in git(repo,"ls-tree","-r","-z",commit).split(b"\0"):
        if not row:continue
        header,pathname=row.split(b"\t",1)
        tokens=header.split()
        require(len(tokens)==3 and tokens[1]==b"blob","NON_BLOB_GIT_TREE")
        name=pathname.decode("utf-8")
        require(name not in objects,"DUPLICATE_GIT_TREE_PATH")
        objects[name]=tokens[2].decode("ascii")
    return objects

def read_objects(repo,oids):
    wanted=sorted(set(oids))
    p=subprocess.run(["git","-C",str(repo),"cat-file","--batch"],
                     input=("".join(s+"\n" for s in wanted)).encode(),
                     stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    require(p.returncode==0,"GIT_BATCH_READ_FAILED")
    stream=p.stdout;pos=0;result={}
    for oid in wanted:
        end=stream.index(b"\n",pos)
        fields=stream[pos:end].split()
        require(fields[:2]==[oid.encode(),b"blob"] and len(fields)==3,"GIT_BATCH_OBJECT_HEADER_MISMATCH")
        length=int(fields[2])
        value=stream[end+1:end+1+length]
        require(len(value)==length and stream[end+1+length:end+2+length]==b"\n",
                "GIT_BATCH_OBJECT_TRUNCATED")
        require(hashlib.sha1(b"blob "+str(length).encode()+b"\0"+value).hexdigest()==oid,
                "GIT_BLOB_CRYPTOGRAPHIC_MISMATCH")
        result[oid]=value
        pos=end+2+length
    require(pos==len(stream),"GIT_BATCH_EXTRA_BYTES")
    return result

def source_snapshot(repo):
    trees={label:tree(repo,sha) for label,sha in SOURCES.items()}
    paths={}
    for label,listing in trees.items():
        if label in PREFIXES:
            p=PREFIXES[label]
            paths[label]={k:v for k,v in listing.items() if k==p+".meta" or k.startswith(p+"/")}
        elif label=="codex":
            paths[label]={k:v for k,v in listing.items() if k.startswith("Assets/")}
        else:
            paths[label]={k:v for k,v in listing.items() if k.startswith("Research/B01-25/")}
    read_oids=[]
    for label,listing in paths.items():
        read_oids += [oid for path,oid in listing.items() if path.endswith(".meta") or path.endswith(".asmdef")]
    for key,(label,path,expected) in PINS.items():
        require(trees[label].get(path)==expected,"ORIGINAL_PINNED_BLOB_CHANGED:"+key)
        read_oids.append(expected)
    return {"trees":trees,"paths":paths,"data":read_objects(repo,read_oids)}

def metadata(snapshot):
    meta={}
    for label in ("codex","chl","part"):
        output={}
        for path,oid in snapshot["paths"][label].items():
            if not path.endswith(".meta"):continue
            m=GUID.search(snapshot["data"][oid])
            require(m is not None,"UNITY_META_GUID_MISSING:"+label+":"+path)
            output[path]=m.group(1).decode()
        meta[label]=output
    return meta

def assemblies(snapshot):
    output={}
    for label in ("codex","chl","part"):
        names={}
        for path,oid in snapshot["paths"][label].items():
            if not path.endswith(".asmdef"):continue
            try:obj=json.loads(snapshot["data"][oid])
            except (ValueError,UnicodeDecodeError) as e:raise Reject("ASSEMBLY_JSON_INVALID:"+path) from e
            name=obj.get("name")
            require(isinstance(name,str) and len(name)>2 and name==name.strip(),"ASSEMBLY_NAME_INVALID:"+path)
            names[path]=name
        output[label]=names
    return output

def validate_map(snapshot,key,expected_items,expected_wall):
    label,path,oid=PINS[key]
    raw=snapshot["data"][snapshot["trees"][label][path]]
    require(len(raw)<=1024*1024,"MAP_EXCEEDS_ORIGINAL_STORAGE_SIZE")
    try:doc=json.loads(raw)
    except ValueError as e:raise Reject("MAP_JSON_INVALID:"+key) from e
    require(isinstance(doc,dict) and doc.get("version")==2 and doc.get("globalFloor") is False and
            isinstance(doc.get("items"),list) and len(doc["items"])==expected_items,
            "MAP_DOCUMENT_V2_DRIFT:"+key)
    counts=Counter()
    corners=set();floor=set();other=set()
    for item in doc["items"]:
        require(isinstance(item,dict) and
                set(item)=={"kind","x","z","rotation","scaleX","scaleY","scaleZ"} and
                all(type(v) is int for v in item.values()),"MAP_ITEM_CONTRACT_DRIFT")
        kind=item["kind"];x=item["x"];z=item["z"];rot=item["rotation"]
        require(kind in range(10) and rot in range(4) and abs(x)<=50 and abs(z)<=50 and
                all(1<=item[k]<=4 for k in ("scaleX","scaleY","scaleZ")),
                "MAP_ITEM_CSHARP_RANGE_DRIFT")
        occupied=set()
        for dx in range(item["scaleX"]):
            for dz in range(item["scaleZ"]):
                rx,rz=((dx,dz),(dz,-dx),(-dx,-dz),(-dz,dx))[rot]
                occupied.add((x+rx,z+rz))
        require(all(abs(px)<=50 and abs(pz)<=50 for px,pz in occupied),"MAP_FOOTPRINT_EXCEEDS_50")
        layer=floor if kind==0 else other
        require(not occupied.intersection(layer),"MAP_SAME_LAYER_OVERLAP")
        layer.update(occupied);counts[kind]+=1
        if (kind==6 and (x,z) in CORNER_CELLS and rot==0 and
                item["scaleX"]==item["scaleZ"]==1 and item["scaleY"]==4):
            corners.add((x,z))
    require(corners==CORNER_CELLS,"KNOWN_COVER_CORNER_GAP_REGRESSION")
    require(counts[0]==120 and counts[1]==expected_wall and counts[6]==4 and
            counts[4]==counts[5]==1 and counts[7]==4 and counts[8]==2,
            "MAP_EXPECTED_OBJECT_KINDS_DRIFT")
    require(len(floor)==120,"MAP_FLOOR_GRID_DRIFT")
    return {"items":expected_items,"walls":expected_wall,"repaired_corners":4,"floor_cells":len(floor),
            "sha256":hashlib.sha256(raw).hexdigest()}

def check(snapshot):
    paths=snapshot["paths"]
    codex=set(paths["codex"])
    chl=set(paths["chl"])
    part=set(paths["part"])
    require(len(chl)==45 and len(part)==28,"PACKAGE_NEW_UNRECEIVED_SOURCE_CHANGE")
    require(not codex.intersection(chl),"CHL_CANONICAL_ASSET_PATH_COLLISION")
    require(not codex.intersection(part),"PART_CANONICAL_ASSET_PATH_COLLISION")
    require(not chl.intersection(part),"TWO_PRODUCER_ASSET_PATH_COLLISION")
    # Unity/Windows can silently collapse case-distinct paths during checkout.
    allpaths=[(label,path) for label in ("codex","chl","part") for path in paths[label]]
    by_case={}
    for label,path in allpaths:
        folded=path.casefold()
        require(folded not in by_case,"CASEFOLD_UNITY_ASSET_PATH_COLLISION:"+path)
        by_case[folded]=(label,path)
    refs={label:set() for label in ("codex","chl","part")}
    meta=metadata(snapshot)
    for label,items in meta.items():
        if label in EXPECTED_META:
            require(len(items)==EXPECTED_META[label],"OWNER_META_SET_DRIFT:"+label)
        for path,guid in items.items():
            require(guid not in refs[label],"DUPLICATE_GUID_IN_SOURCE:"+label)
            refs[label].add(guid)
    require(not (refs["codex"]&refs["chl"]),"CHL_GUID_COLLISION_WITH_CODEX")
    require(not (refs["codex"]&refs["part"]),"PART_GUID_COLLISION_WITH_CODEX")
    require(not (refs["chl"]&refs["part"]),"TWO_PRODUCER_GUID_COLLISION")
    ass=assemblies(snapshot)
    for label,items in ass.items():
        if label in EXPECTED_ASMDEF:
            require(len(items)==EXPECTED_ASMDEF[label],"OWNER_ASMDEF_SET_DRIFT:"+label)
        require(len(set(items.values()))==len(items),"DUPLICATE_ASMDEF_IN_SOURCE:"+label)
    names=[v for items in ass.values() for v in items.values()]
    require(len(set(names))==len(names),"CROSS_SOURCE_ASMDEF_NAME_COLLISION")
    available=set(names)
    for label in ("chl","part"):
        for path,oid in paths[label].items():
            if not path.endswith(".asmdef"):continue
            obj=json.loads(snapshot["data"][oid])
            for ref in obj.get("references",[]):
                # Local B01 references must resolve from this exact manifest union.
                if isinstance(ref,str) and ref.startswith("B01."):
                    require(ref in available,"OWNER_ASMDEF_REFERENCE_MISSING:"+ref)
    draft=snapshot["data"][PINS["codex_map_draft"][2]].decode("utf-8")
    storage=snapshot["data"][PINS["codex_map_storage"][2]].decode("utf-8")
    require("MaxItems=2000" in draft and
            "items.Count(a=>Ground(a.kind)==Ground(kind))>=MaxItems" in draft and
            "doc.items.Count>MaxItems*2" in draft,"NEW_CODEX_TWO_LAYER_MAP_CAP_CONTRACT_CHANGED")
    require('Path.Combine(Application.persistentDataPath,"Maps")' in storage and
            "ValidPath(path" in storage and
            "bool overwrite=false" in storage and "OrdinalIgnoreCase" in storage,
            "NEW_CODEX_MAPSTORAGE_COLLISION_CONTRACT_CHANGED")
    first=validate_map(snapshot,"arena_map_codex",168,36)
    second=validate_map(snapshot,"cover_map_codex",173,41)
    for original,copykey in (("arena_owner","arena_map_codex"),("cover_owner","cover_map_codex")):
        require(PINS[original][2]==PINS[copykey][2] and
                snapshot["data"][PINS[original][2]]==snapshot["data"][PINS[copykey][2]],
                "CODEX_COPIED_MAP_DIFFERS_FROM_B01_25_OWNER:"+original)
    return {"gate":"PASS_SOURCE_ONLY","unity":"NOT_RUN","g1":"HOLD","g2":"HOLD",
            "codex_sha":CODEX,"chl_sha":CHL,"part_sha":PART,"b01_25_sha":ARENA_OWNER,
            "package_file_counts":{"chl":len(chl),"part":len(part)},
            "meta_guid_counts":{label:len(items) for label,items in meta.items()},
            "asmdef_counts":{label:len(items) for label,items in ass.items()},
            "guid_collisions":0,"asset_path_collisions":0,"asmdef_name_collisions":0,
            "map_fixtures":{"LD02_ARENA":first,"LD02_COVER_CHOICE":second},
            "remaining":"Unity import/scene/PhysX + independent G1, Codex package ACK and HQ G2 NOT_RUN"}

def main():
    p=argparse.ArgumentParser(description="Real immutable current-Codex / CHL / PART source admission")
    p.add_argument("--repo",default=".")
    p.add_argument("--out",help="Existing external path, write unique source-only report (no overwrite)")
    a=p.parse_args()
    try:
        root=Path(a.repo).resolve(strict=True)
        snapshot=source_snapshot(root)
        result=check(snapshot)
        payload=(json.dumps(result,sort_keys=True,indent=2)+"\n").encode()
        if a.out:
            target=Path(a.out).resolve(strict=False)
            require(target.parent.is_dir() and target.suffix==".json" and
                    not target.exists() and not target.is_symlink(),
                    "RECEIPT_OUTPUT_NOT_NEW_JSON")
            with open(target,"xb") as f:f.write(payload)
        print(payload.decode(),flush=True)
        return 0
    except (Reject,OSError,ValueError,KeyError,UnicodeDecodeError) as exc:
        print("B01_40_ADMISSION_REJECT "+str(exc)+" UNITY_NOT_RUN",file=sys.stderr)
        return 2

if __name__=="__main__":sys.exit(main())
