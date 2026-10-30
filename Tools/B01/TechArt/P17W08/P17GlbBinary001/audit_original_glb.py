#!/usr/bin/env python3
"""B01-30 P17/W08: real binary GLB 2.0 receiver for unchanged #423 3D assets.

Pure stdlib, independent of Blender and original exporter; directly decodes
GLB JSON/BIN chunks, node world TRS, vertex/index binary accessors, material
names, exact source cells/brace IDs and triangle areas. A true asset intake
test, NOT independent B01-40/60 approval, Unity collider or PhysX test.
"""
from __future__ import annotations
import argparse
import copy
import hashlib
import json
import math
import struct
from collections import Counter
from pathlib import Path

PREV="60a453e4ecd915725bc2fac5433ba7ad43a628cb"
OWNER="5666a39eabd4cea91b2b605a9eb99ea2f1e226e8"
SOURCE_BLOBS={
 "P17_Flanker_Reinforced.json":"7259e3454b2f7dd72ca66de83673c143075a807f",
 "P17_Bulwark_Reinforced.json":"8b00e5da047a22bc23644639e3292c447dd6b335",
 "P17_Skirmisher_Reinforced.json":"edf332a66645c5ea4374b1957d2cc1a7d356e4d2",
 "P17_StructuralBracingKits.json":"9b3d2403e7f6e32ea6e20f9f5bb0a83b859bc6e1",
}
NAMES={"Flanker":(15,1),"Bulwark":(16,2),"Skirmisher":(12,2)}
MATERIALS={
 "Core":(0.10,0.56,0.88,1.0),
 "Armor":(0.46,0.49,0.56,1.0),
 "Gun":(0.78,0.24,0.52,1.0),
 "Wheel":(0.13,0.15,0.19,1.0),
 "CrawlerTrack":(0.26,0.29,0.36,1.0),
 "HoverEngine":(0.10,0.67,0.53,1.0),
 "BRACE_SOURCE":(0.99,0.71,0.12,1.0)
}
GRID=0.55
TOL_M=0.0002
def fail(label):raise ValueError("B01-30-W08-GLB-BINARY-REJECT:"+str(label))
def digest(raw):return hashlib.sha256(raw).hexdigest()
def gitblob(raw):return hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\x00"+raw).hexdigest()
def finite(seq):
    return all(isinstance(x,(int,float)) and math.isfinite(float(x)) for x in seq)

def load_original(source:Path):
    originals={}
    for name,sha in SOURCE_BLOBS.items():
        p=source/name
        if not p.is_file():fail("SOURCE_FILE_MISSING:"+name)
        raw=p.read_bytes()
        if gitblob(raw)!=sha:fail("SOURCE_GIT_BLOB_DRIFT:"+name)
        originals[name]=json.loads(raw)
    kits=originals["P17_StructuralBracingKits.json"]
    if kits.get("originalPartSizeCommit")!="be2be0e4802bc3f450179901f9ae91c6d84bcb5b" or kits.get("originalWeakpointCommit")!="098ccdbe8f129253fa4ada52318436b7cc2b4dc4" or kits.get("nativePhysicalStatus")!="NOT_RUN":fail("SOURCE_OWNER_METADATA")
    result={}
    for name,(count,brace_count) in NAMES.items():
        rig=originals["P17_"+name+"_Reinforced.json"]
        if len(rig["parts"])!=count:fail("SOURCE_PART_COUNT:"+name)
        related=[k for k in kits["kits"] if k["variantId"]==rig["blueprintId"]]
        if len(related)!=1:fail("SOURCE_KIT_LINK:"+name)
        br={p["localId"] for p in related[0]["mandatoryParts"]}
        if len(br)!=brace_count:fail("SOURCE_BRACE_COUNT:"+name)
        ids={p["localId"]:p for p in rig["parts"]}
        if len(ids)!=count or not br.issubset(ids):fail("SOURCE_PART_IDS:"+name)
        for id in br:
            if ids[id]["kind"]!="Armor":fail("BRACE_NOT_ARMOR:"+id)
        result[name]=(rig,br,ids)
    return result

def unpack_glb(raw:bytes):
    if len(raw)<28 or len(raw)>15_000_000 or raw[:4]!=b"glTF":fail("GLB_HEADER")
    magic,version,total=struct.unpack_from("<4sII",raw)
    if version!=2 or total!=len(raw):fail("GLB2_LENGTH_OR_VERSION")
    offset=12; chunks=[]
    while offset<len(raw):
        if offset+8>len(raw):fail("TRUNCATED_CHUNK_HEADER")
        size,ctype=struct.unpack_from("<II",raw,offset)
        offset+=8
        if size%4 or size<4 or offset+size>len(raw):fail("CHUNK_ALIGNMENT_OR_BOUNDS")
        chunks.append((ctype,raw[offset:offset+size]))
        offset+=size
    if len(chunks)!=2 or chunks[0][0]!=0x4E4F534A or chunks[1][0]!=0x004E4942:fail("GLB_JSON_BIN_LAYOUT")
    try: doc=json.loads(chunks[0][1].rstrip(b" \x00"))
    except Exception as e:fail("INVALID_JSON:"+str(e))
    binary=chunks[1][1]
    if doc.get("asset",{}).get("version")!="2.0":fail("ASSET_VERSION")
    if len(doc.get("buffers",[]))!=1 or "uri" in doc["buffers"][0]:fail("EMBEDDED_BUFFER_REQUIRED")
    expected=doc["buffers"][0].get("byteLength")
    if type(expected)!=int or expected>len(binary) or expected<len(binary)-3:fail("BIN_BUFFER_BYTE_LENGTH")
    if doc.get("extensionsRequired"):fail("UNSUPPORTED_REQUIRED_EXTENSIONS")
    if doc.get("animations"):fail("UNEXPECTED_IMPORTED_ANIMATION")
    if not doc.get("meshes") or not doc.get("nodes") or not doc.get("materials"):fail("EMPTY_REAL_GEOMETRY")
    return doc,binary

def mat_mul(a,b):
    return [[sum(a[row][k]*b[k][col] for k in range(4)) for col in range(4)] for row in range(4)]
def identity():return [[float(i==j) for j in range(4)] for i in range(4)]

def node_transform(node):
    if "matrix" in node:
        m=node["matrix"]
        if len(m)!=16 or not finite(m):fail("NODE_BAD_MATRIX")
        return [[float(m[4*c+r]) for c in range(4)] for r in range(4)]
    t=node.get("translation",[0,0,0]); s=node.get("scale",[1,1,1]); q=node.get("rotation",[0,0,0,1])
    if len(t)!=3 or len(s)!=3 or len(q)!=4 or not finite(t+s+q):fail("NODE_BAD_TRS")
    if min(abs(x) for x in s)<1e-9:fail("ZERO_SCALE_NODE")
    x,y,z,w=[float(a) for a in q]
    norm=x*x+y*y+z*z+w*w
    if abs(norm-1)>0.02:fail("QUATERNION_NOT_NORMALIZED")
    r=[[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],
       [2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],
       [2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]]
    return [[r[i][j]*float(s[j]) for j in range(3)]+[float(t[i])] for i in range(3)]+[[0.,0.,0.,1.]]

def scene_mesh_nodes(doc):
    nodes=doc["nodes"];seen=set();active=set();world={}
    scenes=doc.get("scenes",[])
    if len(scenes)!=1 or doc.get("scene",0)!=0:fail("SCENE_COUNT")
    def visit(idx,parent):
        if type(idx)!=int or idx<0 or idx>=len(nodes):fail("NODE_INDEX_OUT_OF_RANGE")
        if idx in active or idx in seen:fail("NODE_MULTIPLE_PARENTS_OR_CYCLE")
        active.add(idx);seen.add(idx)
        node=nodes[idx]
        mat=mat_mul(parent,node_transform(node))
        world[idx]=mat
        for child in node.get("children",[]):visit(child,mat)
        active.remove(idx)
    for idx in scenes[0].get("nodes",[]):visit(idx,identity())
    if len(seen)!=len(nodes):fail("ORPHAN_NODES")
    return [(idx,nodes[idx],world[idx]) for idx in sorted(world) if "mesh" in nodes[idx]]

def decode_accessor(doc,binary,idx):
    arr=doc["accessors"];views=doc["bufferViews"]
    if type(idx)!=int or not 0<=idx<len(arr):fail("ACCESSOR_INDEX")
    ac=arr[idx]
    if "sparse" in ac:fail("UNEXPECTED_SPARSE_ACCESSOR")
    vi=ac.get("bufferView")
    if type(vi)!=int or not 0<=vi<len(views):fail("BUFFER_VIEW_INDEX")
    view=views[vi]
    if view.get("buffer")!=0:fail("NON_EMBEDDED_BUFFER")
    types={"SCALAR":1,"VEC2":2,"VEC3":3,"VEC4":4}
    num=types.get(ac.get("type"))
    typesize={5121:("B",1),5123:("H",2),5125:("I",4),5126:("f",4)}
    fmt=typesize.get(ac.get("componentType"))
    if not num or not fmt:fail("ACCESSOR_COMPONENT_OR_TYPE")
    count=ac.get("count")
    if type(count)!=int or not 1<=count<=200000:fail("ACCESSOR_COUNT")
    f,size=fmt;length=num*size
    stride=view.get("byteStride",length)
    begin=view.get("byteOffset",0)+ac.get("byteOffset",0)
    limit=view.get("byteOffset",0)+view.get("byteLength",0)
    if not all(type(x)==int for x in (stride,begin,limit)) or stride<length or begin<0 or begin+(count-1)*stride+length>limit or limit>len(binary):fail("ACCESSOR_BINARY_BOUNDS")
    values=[struct.unpack_from("<"+f*num,binary,begin+i*stride) for i in range(count)]
    if any(not finite(row) for row in values):fail("NONFINITE_REAL_VERTEX_DATA")
    return values

def triangles_for(doc,binary,mesh):
    if len(mesh.get("primitives",[]))!=1:fail("EXPECTED_ONE_REAL_MESH_PRIMITIVE")
    prim=mesh["primitives"][0]
    if prim.get("mode",4)!=4 or "indices" not in prim:fail("NON_TRIANGLE_OR_UNINDEXED_MESH")
    attrs=prim.get("attributes",{})
    if "POSITION" not in attrs:fail("MISSING_POSITION_ACCESSOR")
    xyz=decode_accessor(doc,binary,attrs["POSITION"])
    idx=decode_accessor(doc,binary,prim["indices"])
    if not 8<=len(xyz)<=10000 or len(idx)<18 or len(idx)%3:fail("EMPTY_OR_INVALID_TRIANGLE_DATA")
    if any(len(v)!=3 for v in xyz) or any(len(v)!=1 for v in idx):fail("POSITION_OR_INDEX_BAD_COMPONENTS")
    inds=[v[0] for v in idx]
    if len(set(inds))<8 or min(inds)<0 or max(inds)>=len(xyz):fail("BAD_INDEX_OR_DEGENERATE_MESH")
    areas=[]
    for i in range(0,len(inds),3):
        a,b,c=(xyz[inds[i+j]] for j in range(3))
        u=[b[j]-a[j] for j in range(3)]
        v=[c[j]-a[j] for j in range(3)]
        cross=(u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0])
        area=0.5*math.sqrt(sum(x*x for x in cross))
        if area<1e-11:fail("DEGENERATE_REAL_TRIANGLE")
        areas.append(area)
    if not areas:fail("ZERO_REAL_TRIANGLES")
    return len(xyz),len(areas),prim

def inspect_doc(doc,binary,name,original,entry):
    if doc.get("extensionsRequired"):fail("UNSUPPORTED_REQUIRED_EXTENSIONS")
    rig,braces,ids=original
    if len(doc.get("materials",[]))<3:fail("MATERIAL_PALETTE_TOO_SMALL")
    found=set();total_tri=0;stats=Counter();colors=Counter()
    for ni,node,world in scene_mesh_nodes(doc):
        if not isinstance(node.get("name"),str) or not node["name"].startswith("P17PART__"):fail("UNEXPECTED_MESH_NODE")
        ident=node["name"].removeprefix("P17PART__")
        if ident in found or ident not in ids:fail("DUPLICATE_OR_FOREIGN_SOURCE_PART:"+ident)
        found.add(ident)
        part=ids[ident]
        extras=node.get("extras",{})
        if extras.get("p17_source_localId")!=ident:fail("ORIGINAL_PART_ID_MISMATCH:"+ident)
        if extras.get("p17_source_kind")!=part["kind"]:fail("PART_KIND_MISMATCH:"+ident)
        if extras.get("p17_visual_only_no_collider") is not True:fail("MISLEADING_PHYSICAL_COLLIDER_FLAG:"+ident)
        if extras.get("p17_source_brace") is not (ident in braces):fail("BRACE_FLAG_MISMATCH:"+ident)
        cell=[part["cell"][axis] for axis in ("x","y","z")]
        if extras.get("p17_source_cell")!=str(cell):fail("ORIGINAL_SOURCE_CELL_METADATA:"+ident)
        expected=(cell[0]*GRID,cell[1]*GRID,-cell[2]*GRID)
        actual=tuple(world[k][3] for k in range(3))
        if max(abs(actual[k]-expected[k]) for k in range(3))>TOL_M:fail("WORLD_POSITION_CONVERSION:"+ident+":"+str(actual)+"/"+str(expected))
        mi=node["mesh"]
        meshes=doc["meshes"]
        if type(mi)!=int or not 0<=mi<len(meshes):fail("NODE_MESH_INDEX")
        vert,tri,prim=triangles_for(doc,binary,meshes[mi])
        material_idx=prim.get("material")
        materials=doc["materials"]
        if type(material_idx)!=int or not 0<=material_idx<len(materials):fail("MATERIAL_INDEX_MISSING")
        material=materials[material_idx]
        kind="BRACE_SOURCE" if ident in braces else part["kind"]
        if material.get("name")!=kind+"_VIS_ONLY":fail("MATERIAL_CATEGORY_MISMATCH:"+ident+":"+str(material.get("name")))
        base=material.get("pbrMetallicRoughness",{}).get("baseColorFactor")
        if not isinstance(base,list) or len(base)!=4 or not finite(base) or max(abs(base[k]-MATERIALS[kind][k]) for k in range(4))>0.035:
            fail("MATERIAL_COLOR_MISMATCH:"+ident+":"+str(base))
        stats[part["kind"]]+=1
        colors[kind]+=1
        total_tri+=tri
    if len(found)!=NAMES[name][0] or colors["BRACE_SOURCE"]!=NAMES[name][1] or not all(p in found for p in ids):
        fail("ORIGINAL_RIG_PART_OR_BRACE_COUNT:"+name)
    if total_tri<100:fail("IMPLAUSIBLE_TOTAL_GEOMETRY")
    return {"rig":name,"real_binary_mesh_nodes":len(found),"original_brace_material_nodes":colors["BRACE_SOURCE"],
      "real_triangles":total_tri,"kind_counts":dict(sorted(stats.items())),
      "material_categories":dict(sorted(colors.items())),"original_cell_coordinates_y_up":"(x, y, -z) * 0.55m"}

def validate_one(raw,rig,original,entry,check_hash=True):
    if check_hash and (len(raw)!=entry["glb_bytes"] or digest(raw)!=entry["glb_sha256"]):
        fail("ORIGINAL_GLTF_BINARY_HASH_DRIFT:"+rig)
    doc,binary=unpack_glb(raw)
    return inspect_doc(doc,binary,rig,original,entry)

def adversarial(original,rig,entry,raw):
    doc,binary=unpack_glb(raw)
    nodes=[x for x in doc["nodes"] if x.get("name","").startswith("P17PART__")]
    if len(nodes)!=NAMES[rig][0]:fail("ADVERSARIAL_SETUP_NO_ORIGINAL_NODES")
    brace=next(n for n in nodes if n.get("extras",{}).get("p17_source_brace") is True)
    nonbrace=next(n for n in nodes if n.get("extras",{}).get("p17_source_brace") is False)
    tests=[
       ("brace_flag_forgery",lambda d,b: next(n for n in d["nodes"] if n["name"]==brace["name"])["extras"].update(p17_source_brace=False)),
       ("source_id_reassignment",lambda d,b: next(n for n in d["nodes"] if n["name"]==nonbrace["name"])["extras"].update(p17_source_localId="P17-FORGED")),
       ("wrong_physical_collision_claim",lambda d,b: next(n for n in d["nodes"] if n["name"]==nonbrace["name"])["extras"].update(p17_visual_only_no_collider=False)),
       ("wrong_source_grid_cell",lambda d,b: next(n for n in d["nodes"] if n["name"]==nonbrace["name"])["extras"].update(p17_source_cell="[9, 9, 9]")),
       ("wrong_material_association",lambda d,b: d["meshes"][next(n for n in d["nodes"] if n["name"]==brace["name"])["mesh"]]["primitives"][0].update(material=next(i for i,m in enumerate(d["materials"]) if m["name"]!="BRACE_SOURCE_VIS_ONLY"))),
       ("forged_world_position",lambda d,b: (next(n for n in d["nodes"] if n["name"]==nonbrace["name"])["matrix"].__setitem__(12,12.0) if "matrix" in next(n for n in d["nodes"] if n["name"]==nonbrace["name"]) else next(n for n in d["nodes"] if n["name"]==nonbrace["name"]).update(translation=[12.0,2.0,3.0]))),
       ("missing_triangles",lambda d,b: d["meshes"][0]["primitives"][0].update(mode=1)),
       ("required_unknown_extension",lambda d,b: d.update(extensionsRequired=["KHR_unknown_fake_ext"])),
    ]
    results=[]
    for label,mutate in tests:
        d=copy.deepcopy(doc);blob=bytearray(binary)
        mutate(d,blob)
        try:inspect_doc(d,blob,rig,original,entry)
        except (ValueError,KeyError,IndexError,TypeError) as e:
            results.append({"mutation":label,"result":"EXPECTED_REJECT","reason":str(e)[:180]})
        else:raise AssertionError("FALSE_GREEN_ADVERSARIAL:"+label)
    # Unknown required extension is rejected by GLB reader, not semantic object checker.
    # The negative test above is validated by this reader separately.
    try:
        d=copy.deepcopy(doc);d["extensionsRequired"]=["KHR_unknown_fake_ext"]
        rebuilt=json.dumps(d,separators=(",",":")).encode()
        rebuilt+=b" "*((-len(rebuilt))%4)
        payload=struct.pack("<I4s",len(rebuilt),b"JSON")+rebuilt+struct.pack("<I4s",len(binary),b"BIN\x00")+binary
        encoded=struct.pack("<4sII",b"glTF",2,12+len(payload))+payload
        unpack_glb(encoded)
    except ValueError:
        results.append({"mutation":"required_unknown_extension_binary","result":"EXPECTED_REJECT"})
    else:raise AssertionError("FALSE_GREEN_EXTENSION_BINARY")
    # real binary vertex data mutation: change first accessor float to QNaN.
    d=copy.deepcopy(doc)
    mesh=d["meshes"][next(n for n in nodes if n.get("mesh") is not None)["mesh"]]
    ac=d["accessors"][mesh["primitives"][0]["attributes"]["POSITION"]]
    view=d["bufferViews"][ac["bufferView"]]
    offset=view.get("byteOffset",0)+ac.get("byteOffset",0)
    payload=bytearray(binary)
    struct.pack_into("<f",payload,offset,float("nan"))
    try:inspect_doc(d,bytes(payload),rig,original,entry)
    except ValueError as e:results.append({"mutation":"real_geometry_nan_float32","result":"EXPECTED_REJECT","reason":str(e)[:180]})
    else:raise AssertionError("FALSE_GREEN_BINARY_VERTEX_NAN")
    # Bad original SHA is a separate real-byte test; metadata mutations above
    # deliberately bypass only the OUTER hash to prove semantic gates are real.
    tampered=bytearray(raw);tampered[-13]^=0x21
    try:validate_one(bytes(tampered),rig,original,entry,check_hash=True)
    except ValueError as e:results.append({"mutation":"real_glb_byte_sha256_corrupt","result":"EXPECTED_REJECT","reason":str(e)[:180]})
    else:raise AssertionError("FALSE_GREEN_SHA256_CORRUPT")
    return results

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--binary",type=Path,required=True)
    parser.add_argument("--source",type=Path,required=True)
    parser.add_argument("--out",type=Path,required=True)
    args=parser.parse_args()
    args.out.mkdir(parents=True,exist_ok=True)
    actual=load_original(args.source)
    receipts=list(args.binary.rglob("source-export-receipt.json"))
    if len(receipts)!=1:fail("PREVIOUS_PRODUCER_RECEIPT_MISSING_OR_DUPLICATE")
    previous=json.loads(receipts[0].read_text())
    if previous.get("package_id")!="B01-30-P17-BRACE-VIS-001/R1" or previous.get("original_source_sha")!=OWNER or previous.get("original_source_pr")!=375:
        fail("PREVIOUS_PRODUCER_RECEIPT_INCORRECT")
    if previous.get("original_source_git_blobs")!=dict(sorted(SOURCE_BLOBS.items())):fail("PREVIOUS_SOURCE_GIT_BLOBS_DRIFT")
    entries=previous.get("exports")
    if not isinstance(entries,list) or len(entries)!=3:fail("PRODUCER_EXPORT_COUNT")
    original_name_set=set();reports=[];mutations=[]
    for item in entries:
        name=item.get("rig")
        if name not in NAMES or name in original_name_set:fail("UNEXPECTED_OR_DUPLICATE_RIG")
        original_name_set.add(name)
        expected=name+"-P17-BRACED-VISUAL-ONLY.glb"
        if item.get("glb")!=expected:fail("UNEXPECTED_GLB_FILENAME")
        matches=list(args.binary.rglob(expected))
        if len(matches)!=1:fail("GLB_BINARY_MISSING_OR_DUPLICATE:"+expected)
        file=matches[0];raw=file.read_bytes()
        report=validate_one(raw,name,actual[name],item)
        report["real_original_GLB_bytes"]=len(raw)
        report["sha256"]=digest(raw)
        reports.append(report)
        mutations.extend([{"rig":name,**test} for test in adversarial(actual[name],name,item,raw)])
    if original_name_set!=set(NAMES):fail("NOT_ALL_RIGS_PROCESSED")
    if sum(r["real_binary_mesh_nodes"] for r in reports)!=43 or sum(r["original_brace_material_nodes"] for r in reports)!=5:fail("GLOBAL_PART_BRACE_TOTAL")
    output={"package_id":"B01-30-P17-ORIGINAL-GLB-BINARY-QA-001/R1",
        "upstream_producer":423,"upstream_commit_sha":PREV,
        "original_owner":375,"original_owner_sha":OWNER,
        "source_git_blobs":dict(sorted(SOURCE_BLOBS.items())),
        "real_GLTF_version":2,"binary_GLB_count":3,
        "actual_mesh_nodes_total":43,"original_brace_material_nodes_total":5,
        "real_triangles_total":sum(r["real_triangles"] for r in reports),
        "rigs":reports,"adversarial_negative_cases":mutations,
        "adversarial_rejections":len(mutations),
        "outcome":"ORIGINAL_3_BINARY_GLB2_SOURCE_AND_GEOMETRY_GATE_PASS",
        "limitations":["source read-only, previous #423 unchanged","proxy-only geometry, no approved physical collider/socket",
        "native Unity/URP/PhysX NOT_RUN","producer-side test, B01-40/60 independent G0 pending",
        "no Codex ACK/integration; no source game Assets changed"]}
    path=args.out/"p17-original-glb-binary-qa.json"
    with path.open("w",encoding="utf-8",newline="\n") as out:
        out.write(json.dumps(output,indent=2,sort_keys=True)+"\n")
    print("B01_30_W08_ACTUAL_3_GLB2_43_MESH_5_BRACE_BINARY_QA_PASS",
        json.dumps({"triangles":output["real_triangles_total"],
          "binary_originals":3,"mesh_parts":43,"brace_visuals":5,
          "negative_mutations":len(mutations)}))
if __name__=="__main__":main()
