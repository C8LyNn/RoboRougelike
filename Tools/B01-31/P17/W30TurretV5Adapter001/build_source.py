#!/usr/bin/env python3
"""Original 19-path add-only V5 production SOURCE ZIP. Never ships the private
producer's actual FBX or imports the candidate into any live Unity project.
"""
import argparse, hashlib, io, json, pathlib, sys, zipfile
PKG="B01-31-P17W30-R41-V5-AXIS-LOCKED-ADAPTER-001"
TOOL="Tools/B01-31/P17/W30TurretV5Adapter001"
ROOT="Assets/B01_31/W30R41V5Adapter"
EXPECTED_SHA="fc83d677744d7e60b5f5f5f3ad3d9e72c97f84b66bbacf142c14959becfa5533"
sha=lambda b:hashlib.sha256(b).hexdigest()
blob=lambda b:hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()
def need(x,m):
 if not x:raise ValueError("W30_R41V5_SOURCE_AUTHOR_RED: "+m)
def build(repo,out):
 manifest=json.loads((repo/TOOL/"manifest.json").read_text())
 paths=manifest["all_new_owner_source_paths"]
 need(manifest["package_id"]==PKG and
      manifest["exact_W29_owner_parent"]=="ced677d7b3f9fa81e6b0e5b51a028a518770e096"
      and manifest["external_verified_source"]["sha256"]==EXPECTED_SHA
      and manifest["external_verified_source"]["source_in_this_patch"] is False
      and len(paths)==19 and len(set(paths))==19 and sorted(paths)==paths,
      "exact original 19 source paths / FBX reference only")
 need(not (repo/ROOT/"Incoming/R41_Turret_v5.fbx").exists(),
      "must NOT accidentally ship local original producer FBX in Git source patch")
 files={}
 for path in paths:
  need(".." not in pathlib.PurePosixPath(path).parts and
       not pathlib.PurePosixPath(path).is_absolute() and
       path.startswith((ROOT,TOOL,".github/workflows/b01-31-")),
       "add-only own source")
  data=(repo/path).read_bytes()
  need(data,"empty actual original file")
  files[path]=data
 editor=files[ROOT+"/Editor/B0131W30CreateR41V5AdapterCandidate.cs"]
 math=files[ROOT+"/R41V5MuzzleGeometry.cs"]
 cases=files[ROOT+"/Tests/EditMode/R41V5ActualFbxImportEditModeTests.cs"]
 for feature in (b"Object.Instantiate(originalFbx)",b"PrefabUtility.UnpackPrefabInstance",
                 b"KnownV5FbxSha256",EXPECTED_SHA.encode(),
                 b"SHA256.Create()",b"bakeAxisConversion",
                 b"R41V5MuzzleGeometry.ValidateImportedNeutral",
                 b"PrefabUtility.SaveAsPrefabAsset(candidate,CandidatePath)",
                 b"File.Exists(CandidatePath)"):
  need(feature in editor,"actual original Unity Editor import gate not complete")
 for feature in (b"TryPose(",b"ValidatePosePair(",b"ValidateImportedNeutral(",
                 b"GridMeters = 0.55",b"BarrelLengthMeters = 0.700"):
  need(feature in math,"production original muzzle math absent")
 need(cases.count(b"[Test]")==4 and
      b"Assert.Inconclusive(" in cases and
      b"AssetDatabase.GetAssetDependencyHash" in cases,
      "four real Unity original FBX NUnit sources, missing source INCONCLUSIVE")
 metas=[name for name in files if name.startswith(ROOT) and name.endswith(".meta")]
 need(len(metas)==8,"8 exact original NEW Unity GUID .meta")
 guids=[]
 for m in metas:
  keys=[l[6:].strip() for l in files[m].decode().splitlines() if l.startswith("guid: ")]
  need(len(keys)==1 and len(keys[0])==32 and
       all(c in "0123456789abcdef" for c in keys[0]),"strict original Unity GUID")
  guids.extend(keys)
 need(len(set(guids))==8,"8 distinct original Unity GUIDs")
 out.mkdir(parents=True,exist_ok=True)
 archive=out/"B0131W30-R41V5-AXIS-GATED-SOURCE.zip"
 buff=io.BytesIO()
 with zipfile.ZipFile(buff,"w") as z:
  for name,content in sorted(files.items()):
   zi=zipfile.ZipInfo(name,date_time=(1980,1,1,0,0,0))
   zi.compress_type=zipfile.ZIP_DEFLATED
   zi.create_system=3
   zi.external_attr=0o100644<<16
   z.writestr(zi,content,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
 raw=buff.getvalue();archive.write_bytes(raw)
 receipt={"package_id":PKG,"revision":"P17-W30-R1",
          "exact_W29_parent":manifest["exact_W29_owner_parent"],
          "original_external_v5_FBX_SHA256_reference_only":EXPECTED_SHA,
          "external_FBX_physically_included":False,
          "actual_source_zip_sha256":sha(raw),"actual_source_zip_bytes":len(raw),
          "actual_source_files":len(files),"new_unity_guid_values":sorted(guids),
          "source_members":[{"path":n,"bytes":len(d),"sha256":sha(d),"git_blob":blob(d)}
                            for n,d in sorted(files.items())],
          "source_code_Net8_crossOS":"PENDING_CI",
          "native_Unity_EditMode_4":"AUTHORED_NOT_RUN",
          "native_Unity_G1":"NOT_RUN",
          "original_FBX_native_import_W30":"NOT_RUN",
          "B01_30_40_independent_formal_G0":"PENDING",
          "Codex_ACK":False,"integrated":False,"released":False}
 (out/"receipt.json").write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n",encoding="utf-8")
 print(json.dumps({"status":"PASS","actual_original_source_files":len(files),
                   "source_package_sha256":sha(raw),"new_Unity_GUIDs":8,
                   "real_imported_v5_fbx":"NOT_RUN","native_G1":"NOT_RUN"}))
if __name__=="__main__":
 p=argparse.ArgumentParser();p.add_argument("--repo",required=True);p.add_argument("--out",required=True)
 a=p.parse_args()
 try:build(pathlib.Path(a.repo),pathlib.Path(a.out))
 except Exception as e:print(str(e),file=sys.stderr);raise
