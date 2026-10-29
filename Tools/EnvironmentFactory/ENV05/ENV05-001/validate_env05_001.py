#!/usr/bin/env python3
"""ENV05-001 independent binary verification; Python stdlib only. CC0-1.0."""
import argparse,hashlib,json,struct
from pathlib import Path

def sha(path):
 h=hashlib.sha256()
 with path.open("rb") as f:
  for chunk in iter(lambda:f.read(1048576),b""): h.update(chunk)
 return h.hexdigest()

def main():
 p=argparse.ArgumentParser(); p.add_argument("output_dir",type=Path)
 d=p.parse_args().output_dir
 report=json.loads((d/"ENV05-001_v002_qa.json").read_text())
 assert report["status"]=="BLENDER_QA_PASS" and report["self_test"]
 assert report["revision"]=="v002" and report["metrics"]["revision"]=="v002"
 assert report["metrics"]["beacon_emission_strength"]>=3.0
 assert abs(report["metrics"]["aabb_min_m"][0]+.15)<.001
 assert .895<report["metrics"]["aabb_max_m"][2]<.902
 assert 200<=report["metrics"]["triangles"]<=1500
 assert len(report["metrics"]["materials"])==3
 assert report["metrics"]["animations"]=="NOT_APPLICABLE"
 for name,info in report["outputs"].items():
  f=d/name
  assert f.is_file() and f.stat().st_size==info["bytes"] and sha(f)==info["sha256"],name
 assert (d/"ENV05-001_v002_light_bollard.blend").read_bytes()[:7]==b"BLENDER"
 for name in ("ENV05-001_v002_front_three_quarter.png","ENV05-001_v002_rear_three_quarter.png"):
  with (d/name).open("rb") as f: assert f.read(8)==b"\x89PNG\r\n\x1a\n",name
 blob=(d/"ENV05-001_v002_light_bollard.glb").read_bytes()
 magic,version,length=struct.unpack_from("<4sII",blob,0)
 assert magic==b"glTF" and version==2 and length==len(blob)
 size,kind=struct.unpack_from("<I4s",blob,12)
 assert kind==b"JSON"
 gltf=json.loads(blob[20:20+size].decode("utf-8"))
 assert len(gltf.get("materials",[]))==3
 assert not gltf.get("animations",[])
 assert any(n.get("name")=="ENV05_001_ROOT_PIVOT_GROUND" for n in gltf["nodes"])
 emissive=[m for m in gltf["materials"] if "AmberBeacon" in m.get("name","")]
 assert len(emissive)==1,("amber_material_missing",emissive)
 amber=emissive[0]
 assert amber.get("emissiveFactor",[0,0,0])[0]>.9,amber
 assert amber.get("emissiveFactor",[0,0,0])[1]>.2,amber
 assert amber.get("extensions",{}).get("KHR_materials_emissive_strength",{}).get("emissiveStrength",0)>=3.0,amber
 assert gltf.get("meshes") and gltf.get("nodes") and gltf.get("scenes")
 triangles=0
 for mesh in gltf["meshes"]:
  for prim in mesh["primitives"]:
   assert prim.get("mode",4)==4
   assert "NORMAL" in prim.get("attributes",{}),("missing_vertex_normals",mesh)
   idx=prim.get("indices")
   triangles+=gltf["accessors"][idx]["count"]//3 if idx is not None else gltf["accessors"][prim["attributes"]["POSITION"]]["count"]//3
 assert 200<=triangles<=1500,triangles
 result={"id":"ENV05-001","revision":"v002","status":"INDEPENDENT_EXPORT_QA_PASS",
         "glb_amber_emission_strength":amber["extensions"]["KHR_materials_emissive_strength"]["emissiveStrength"],
         "glb_triangles":triangles,"glb_meshes":len(gltf["meshes"]),
         "glb_materials":len(gltf["materials"]),"glb_animations":0,
         "files":{f.name:{"bytes":f.stat().st_size,"sha256":sha(f)}
                  for f in sorted(d.iterdir()) if f.is_file()}}
 (d/"ENV05-001_v002_export_validation.json").write_text(json.dumps(result,indent=2)+"\n")
 print(json.dumps(result,indent=2))
if __name__=="__main__": main()
