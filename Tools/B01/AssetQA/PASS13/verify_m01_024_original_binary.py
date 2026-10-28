#!/usr/bin/env python3
"""B01-30 PASS13: read only original M01-024 OBJ/GLB/FBX Git blobs. NOT Unity."""
import itertools
import json
import math
import struct
import subprocess
import sys
import zlib

COMMIT="b27d210bb598e738c6dd979686706b5fc29b73ed"
PREFIX="Tools/ModelFactory/M01/M01-024/generated/M01-024_mechanical_anchor_harpoon."
BLOBS={"obj":"f1c3cc2274833cc522d422e563306f3f343d518c",
       "glb":"2d98f05eb05ac8e5c4c475114a3767a1b7981f8b",
       "fbx":"29344b9acb5a3fb4896178a06ec756a22f4ad633"}

def git(*args,input=None):
    return subprocess.run(["git",*args],input=input,check=True,capture_output=True).stdout

def load_original():
    out={}
    for suffix,sha in BLOBS.items():
        actual=git("rev-parse",f"{COMMIT}:{PREFIX}{suffix}").decode().strip()
        assert actual==sha,(suffix,"unexpected source Git blob",actual)
        raw=git("cat-file","blob",sha)
        assert git("hash-object","--stdin",input=raw).decode().strip()==sha
        out[suffix]=raw
    return out

def obj_vertices(raw):
    data={};name=None
    for row in raw.decode().splitlines():
        if row.startswith("o "):
            name=row[2:]; assert name not in data;data[name]=[]
        elif row.startswith("v "):
            assert name
            xyz=tuple(map(float,row.split()[1:]))
            assert len(xyz)==3 and all(map(math.isfinite,xyz))
            data[name].append(xyz)
    assert len(data)==51 and sum(map(len,data.values()))==3322
    return data

def glb_bounds(raw):
    assert raw[:4]==b"glTF"
    assert struct.unpack_from("<II",raw,4)==(2,len(raw))
    size,typ=struct.unpack_from("<II",raw,12)
    assert typ==0x4e4f534a
    g=json.loads(raw[20:20+size])
    out={}
    for node in g["nodes"]:
        if "mesh" not in node:continue
        assert node["name"].startswith("M01_024_")
        acc=g["accessors"][g["meshes"][node["mesh"]]["primitives"][0]["attributes"]["POSITION"]]
        out[node["name"][8:]]=(acc["min"],acc["max"])
    assert len(out)==51
    return out


def obj_triangles(raw):
    """Actual OBJ indexed triangles in authored -> GLB mesh coordinate frame."""
    positions=[];out={};name=None
    for row in raw.decode().splitlines():
        if row.startswith("o "):
            name=row[2:]
            assert name not in out
            out[name]=[]
        elif row.startswith("v "):
            positions.append(tuple(map(float,row.split()[1:])))
        elif row.startswith("f "):
            assert name is not None
            ids=[int(entry.split("/")[0])-1 for entry in row.split()[1:]]
            assert len(ids)==3 and all(0<=j<len(positions) for j in ids)
            triangle=[]
            for j in ids:
                x,y,z=positions[j];triangle.append((x,z,-y))
            out[name].append(triangle)
    assert len(out)==51
    assert sum(map(len,out.values()))==6508
    return out

def glb_actual_triangles(raw):
    """Decode actual GLB float POSITION payload and uint index buffer, not metadata AABB."""
    assert raw[:4]==b"glTF" and struct.unpack_from("<II",raw,4)==(2,len(raw))
    jlen,jtype=struct.unpack_from("<II",raw,12)
    assert jtype==0x4e4f534a
    gltf=json.loads(raw[20:20+jlen])
    blob_offset=20+jlen
    blen,btype=struct.unpack_from("<II",raw,blob_offset)
    assert btype==0x004e4942 and blob_offset+8+blen==len(raw)
    blob_offset+=8
    def accessor(i):
        a=gltf["accessors"][i]
        assert "sparse" not in a and "bufferView" in a
        view=gltf["bufferViews"][a["bufferView"]]
        assert view.get("buffer",0)==0
        typ=a["type"];comp=a["componentType"]
        fmt={5126:"f",5123:"H",5125:"I",5121:"B"}[comp]
        width={"SCALAR":1,"VEC3":3}[typ]
        stride=view.get("byteStride",struct.calcsize("<"+fmt)*width)
        off=blob_offset+view.get("byteOffset",0)+a.get("byteOffset",0)
        values=[]
        for idx in range(a["count"]):
            values.append(struct.unpack_from("<"+fmt*width,raw,off+idx*stride))
        return values,a
    mesh_triangles={};actual_counts=0;vertices=0
    for node in gltf["nodes"]:
        if "mesh" not in node:continue
        assert node["name"].startswith("M01_024_")
        name=node["name"][8:]
        assert name not in mesh_triangles
        primitives=gltf["meshes"][node["mesh"]]["primitives"]
        assert len(primitives)==1
        primitive=primitives[0]
        assert primitive.get("mode",4)==4
        positions,meta=accessor(primitive["attributes"]["POSITION"])
        assert meta["type"]=="VEC3" and meta["componentType"]==5126
        ids,imeta=accessor(primitive["indices"])
        indices=[v[0] for v in ids]
        assert len(indices)%3==0
        assert all(0<=i<len(positions) for i in indices)
        # The original accessor min/max can be forged: verify them against raw floats.
        posbox=bbox(positions)
        assert err(posbox,(meta["min"],meta["max"]))<0.000002,(name,"BAD_ACCESSOR_AABB")
        mesh_triangles[name]=[[positions[indices[i]],positions[indices[i+1]],
                                positions[indices[i+2]]]
                               for i in range(0,len(indices),3)]
        actual_counts+=len(indices)//3
        vertices+=len(positions)
    assert len(mesh_triangles)==51
    assert actual_counts==6508
    return mesh_triangles,vertices

def triangle_signature(triangle):
    """Order-independent 0.01mm quantized triangle; counts preserve duplicates."""
    return tuple(sorted(tuple(round(c,5) for c in pos) for pos in triangle))

def parse_fbx(raw):
    assert raw[:23]==b"Kaydara FBX Binary  \x00\x1a\x00"
    assert struct.unpack_from("<I",raw,23)[0]==7400
    geometry={};models={};settings={};total=[0];compressed=[0]
    scalar={b"I":("<i",4),b"L":("<q",8),b"D":("<d",8),b"F":("<f",4),
            b"Y":("<h",2),b"C":("<B",1)}
    def prop(off):
        typ=raw[off:off+1];off+=1
        if typ in scalar:
            fmt,n=scalar[typ];return struct.unpack_from(fmt,raw,off)[0],off+n
        if typ in (b"S",b"R"):
            n=struct.unpack_from("<I",raw,off)[0];off+=4
            val=raw[off:off+n]
            return (val.decode("utf-8") if typ==b"S" else val),off+n
        if typ in (b"d",b"f",b"i",b"l",b"b"):
            count,encoding,n=struct.unpack_from("<III",raw,off);off+=12
            payload=raw[off:off+n];off+=n
            assert encoding in (0,1)
            if encoding==1:payload=zlib.decompress(payload)
            code={b"d":"d",b"f":"f",b"i":"i",b"l":"q",b"b":"B"}[typ]
            assert len(payload)==count*struct.calcsize("<"+code)
            return ("array",typ,count,encoding,payload),off
        raise ValueError((typ,off))
    def walk(off,end,owner="",kind=""):
        while off+13<=end:
            finish,np,nbytes,nlen=struct.unpack_from("<IIIB",raw,off)
            if finish==0:return
            assert off<finish<=len(raw)
            tag=raw[off+13:off+13+nlen].decode()
            start=off+13+nlen;pos=start;args=[]
            for _ in range(np):
                value,pos=prop(pos);args.append(value)
            assert pos-start==nbytes
            total[0]+=1
            curr=owner;role=kind
            if tag in ("Geometry","Model"):
                curr=args[1].split("\0")[0];role=tag
            if tag=="Vertices" and kind=="Geometry":
                assert len(args)==1 and args[0][:2]==("array",b"d")
                _,_,cnt,encoding,content=args[0]
                assert cnt%3==0
                key=owner.removeprefix("M01_024_")
                assert key not in geometry
                geometry[key]=list(struct.iter_unpack("<ddd",content))
                compressed[0]+=encoding==1
            if tag=="P" and len(args)>=5:
                if kind=="Model":models.setdefault(owner,{})[args[0]]=args[4:]
                elif not owner:settings[args[0]]=args[4:]
            if pos<finish-13:walk(pos,finish,curr,role)
            off=finish
    walk(27,len(raw))
    assert len(geometry)==51
    return geometry,models,settings,total[0],compressed[0]

def bbox(points):
    return [min(v[i] for v in points) for i in range(3)],[max(v[i] for v in points) for i in range(3)]

def err(a,b):
    return max(abs(x-y) for aa,bb in zip(a,b) for x,y in zip(aa,bb))

def main():
    raw=load_original()
    objs=obj_vertices(raw["obj"]); glb=glb_bounds(raw["glb"])
    obj_faces=obj_triangles(raw["obj"])
    glb_faces,glb_payload_vertices=glb_actual_triangles(raw["glb"])
    assert set(obj_faces)==set(glb_faces)==set(objs)
    actual_triangle_total=0
    for name,expected_faces in obj_faces.items():
        actual_faces=glb_faces[name]
        assert len(expected_faces)==len(actual_faces),(name,"TRIANGLE_COUNT_MISMATCH")
        expected_signatures=sorted(triangle_signature(t) for t in expected_faces)
        actual_signatures=sorted(triangle_signature(t) for t in actual_faces)
        assert expected_signatures==actual_signatures,(name,"TRIANGLE_POSITION_MISMATCH")
        actual_triangle_total+=len(actual_faces)
    # SYNTHETIC negative: old GLB accessor-minmax-only QA would PASS even
    # if an internal triangle corner was displaced in the actual binary.
    witness_name=sorted(glb_faces)[0]
    forged_triangles=[list(t) for t in glb_faces[witness_name]]
    forged_triangles[0]=list(forged_triangles[0])
    tampered=list(forged_triangles[0][0]);tampered[0]+=.002
    forged_triangles[0][0]=tuple(tampered)
    assert sorted(triangle_signature(t) for t in forged_triangles)!=sorted(
        triangle_signature(t) for t in obj_faces[witness_name])
    fbx,models,settings,nodes,compressed=parse_fbx(raw["fbx"])
    assert set(objs)==set(glb)==set(fbx)
    glb_max=max(err(bbox([(x,z,-y) for x,y,z in obj]),glb[k]) for k,obj in objs.items())
    assert glb_max<0.000002,glb_max
    # The FBX exporter can place axis/unit conversion in meshes OR on a parent.
    # Diagnose all 48 signed axis permutations and 3 unit scales, no assumed winner.
    options=[]
    for order in itertools.permutations(range(3)):
        for signs in itertools.product((-1,1),repeat=3):
            for scale in (0.01,1.0,100.0):
                worst=max(err(bbox([tuple(scale*signs[i]*p[order[i]] for i in range(3))
                                        for p in obj]),bbox(fbx[k]))
                          for k,obj in objs.items())
                options.append((worst,order,signs,scale))
    options.sort()
    candidate=options[0]
    assert candidate[0]<0.00001, ("FAIL CLOSED: original FBX mesh basis cannot be reconciled",candidate)
    # PASS14: close a real QA false-negative: identical mesh AABBs do NOT imply
    # matching internal vertices. Check every original per-mesh vertex directly.
    order,signs,scale=candidate[1],candidate[2],candidate[3]
    vertex_deltas={}
    for key,source_vertices in objs.items():
        transformed=[tuple(scale*signs[i]*p[order[i]] for i in range(3))
                     for p in source_vertices]
        actual=fbx[key]
        assert len(transformed)==len(actual),(key,"VERTEX_COUNT_MISMATCH")
        # The Blender OBJ and FBX writers may reorder vertex indices; compare
        # whole point clouds independently of order (duplicates preserved).
        transformed=sorted(transformed)
        actual=sorted(actual)
        max_difference=max(abs(a-b) for expected,found in zip(transformed,actual)
                           for a,b in zip(expected,found))
        vertex_deltas[key]=max_difference
        assert max_difference<0.000002,(key,"PER_VERTEX_MESH_MISMATCH",max_difference)
    vertex_max=max(vertex_deltas.values())
    # PASS14 SYNTHETIC negative: move a real interior vertex without modifying
    # any AABB extrema; this mutation used to pass the old bounding-box-only QA.
    hidden_mutation=None
    for key,points in sorted(fbx.items()):
        lo,hi=bbox(points)
        for index,p in enumerate(points):
            for axis in range(3):
                if p[axis]>lo[axis]+0.005 and p[axis]<hi[axis]-0.005:
                    forged_vertices=list(points)
                    moved=list(p);moved[axis]+=0.002
                    forged_vertices[index]=tuple(moved)
                    if bbox(forged_vertices)!=bbox(points):
                        continue
                    old_box_test_passes=True  # extrema are byte-identical
                    new_vertex_test_rejects=(
                        max(abs(a-b) for expected,found in zip(sorted(points),sorted(forged_vertices))
                            for a,b in zip(expected,found)) > 0.000002
                    )
                    assert old_box_test_passes and new_vertex_test_rejects
                    hidden_mutation={"mesh":key,"vertex_index":index,
                                     "axis":axis,"delta_m":0.002,
                                     "old_AABB_detector":"FALSE_GREEN",
                                     "new_vertex_detector":"EXPECTED_REJECTION"}
                    break
            if hidden_mutation:break
        if hidden_mutation:break
    assert hidden_mutation is not None,"CANNOT_RUN_INTERIOR_VERTEX_NEGATIVE"
    # Clearly labelled SYNTHETIC falsifier: shift one original FBX submesh 20mm
    # and ensure the source-mesh reconciliation would reject the corruption.
    witness=sorted(objs)[0]
    original=objs[witness]
    order,signs,scale=candidate[1],candidate[2],candidate[3]
    expected=bbox([tuple(scale*signs[i]*p[order[i]] for i in range(3))
                   for p in original])
    forged=bbox([(p[0]+.020,p[1],p[2]) for p in fbx[witness]])
    mutant_rejected=err(expected,forged)>.005
    assert mutant_rejected, "FAIL CLOSED: +20mm mesh payload mutation not detected"
    report={
       "original_commit":COMMIT,"original_blobs":BLOBS,
       "actual_binary_bytes":{k:len(v) for k,v in raw.items()},
       "original_fbx_v7400_nodes":nodes,
       "original_fbx_meshes_decoded":len(fbx),
       "original_fbx_vertices_decoded":sum(map(len,fbx.values())),
       "original_fbx_zlib_arrays":compressed,
       "fbx_root_rotation":models.get("MOUNT_FRONT_SOCKET",{}).get("Lcl Rotation"),
       "fbx_root_scale":models.get("MOUNT_FRONT_SOCKET",{}).get("Lcl Scaling"),
       "fbx_axes":{k:settings.get(k) for k in ("UpAxis","FrontAxis","CoordAxis","UnitScaleFactor")},
       "fbx_raw_mesh_best_global_axis_basis":{
           "max_aabb_delta_m":candidate[0],"permutation":candidate[1],
           "signs":candidate[2],"unit_scale":candidate[3]},
       "fbx_raw_mesh_global_match_10um":candidate[0]<0.00001,
       "fbx_original_meshes_matching_each_vertex":len(vertex_deltas),
       "fbx_max_per_vertex_difference_m":vertex_max,
       "fbx_per_vertex_threshold_m":0.000002,
       "synthetic_hidden_vertex_mutant":hidden_mutation,
       "synthetic_fbx_20mm_negative_control":"EXPECTED_REJECTION",
       "glb_actual_binary_triangles_verified":actual_triangle_total,
       "glb_actual_position_entries_decoded":glb_payload_vertices,
       "glb_actual_payload_triangle_soup_matches_OBJ":True,
       "synthetic_glb_corner_2mm_negative":"EXPECTED_REJECTION",
       "glb_original_meshes_matching_obj_basis":51,
       "glb_max_aabb_delta_m":glb_max,
       "actual_test_kind":"GIT_ORIGINAL_BINARY_DEFLATE+OBJ_GLB_FBX_SOURCE_QA",
       "Unity_EditMode":"NOT_RUN","Unity_PlayMode":"NOT_RUN",
       "ModelImporter_runtime":"NOT_RUN","Physics":"NOT_RUN",
    }
    assert compressed==51
    assert all(k in models["MOUNT_FRONT_SOCKET"] for k in ("Lcl Rotation","Lcl Scaling"))
    print(json.dumps(report,indent=2))
    if candidate[0]>=0.00001:
        print("CAUTION: original FBX raw meshes not equivalent through one tested basis/scale. Root/mesh-local transforms may account for this; do NOT claim Unity import FAIL.",file=sys.stderr)

if __name__=="__main__":
    main()
