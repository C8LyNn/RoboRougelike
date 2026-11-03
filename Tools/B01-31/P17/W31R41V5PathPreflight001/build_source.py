#!/usr/bin/env python3
"""Actual deterministic 12-file B01-31 W31 original Git source PATCH ZIP.
Never copy/import the external R41 FBX. Parent W30 PR #893 remains required.
"""
import argparse,hashlib,io,json,pathlib,sys,zipfile
PKG="B01-31-P17W31-R41V5-ABSOLUTE-ASSET-AND-PRECLONE-GATE-001"
PARENT="bae40c25c48b26849c3291c84b79a66a9ba5c2a5"
TOOL="Tools/B01-31/P17/W31R41V5PathPreflight001"
ROOT="Assets/B01_31/W30R41V5Adapter"
EDITOR=ROOT+"/Editor/B0131W30CreateR41V5AdapterCandidate.cs"
SOURCE=ROOT+"/R41V5AssetPaths.cs"
NATIVE=ROOT+"/Tests/EditMode/R41V5W31PrecloneEditModeTests.cs"
def sha(b):return hashlib.sha256(b).hexdigest()
def blob(b):return hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()
def need(cond,msg):
 if not cond:raise ValueError("B01_31_W31_ORIGINAL_AUTHOR_RED "+msg)
def main(repo,out):
 m=json.loads((repo/TOOL/"manifest.json").read_text(encoding="utf-8"))
 paths=m["all_changed_owner_paths"]
 need(m["package_id"]==PKG and m["exact_W30_parent"]==PARENT and
      m["old_W30_Editor_Git_Blob"]=="a138507c864a639d3ea6a8e219615412d7a7fc2c" and
      paths==sorted(set(paths)) and len(paths)==12,
      "correct original W30 parent and 12 exact changed paths")
 files={}
 for name in paths:
  path=pathlib.PurePosixPath(name)
  need(".." not in path.parts and not path.is_absolute() and
       name.startswith((ROOT,TOOL,".github/workflows/b01-31-p17w31-")),
       "add/modify only reserved original B01-31 source")
  data=(repo/name).read_bytes()
  need(data,"real Git path must not be empty")
  files[name]=data
 editor=files[EDITOR]; pathsource=files[SOURCE]; native=files[NATIVE]
 must=[b"R41V5AssetPaths.TryResolveFbxPath(",b"Application.dataPath",
       b"File.OpenRead(realFbxPath)",b"TryValidateSourceVisualHierarchy(",
       b"GetComponentsInChildren<MonoBehaviour>(true)",
       b"GetComponentsInChildren<Joint>(true)",
       b"FindUnique(originalVisual.transform, \"Muzzle_L\")",
       b"FindUnique(originalVisual.transform, \"Muzzle_R\")",
       b"if (!TryValidateSourceVisualHierarchy(originalFbx, out rejection))",
       b"copy = Object.Instantiate(originalFbx);",
       b"PrefabUtility.UnpackPrefabInstance(",
       b"R41V5MuzzleGeometry.ValidateImportedNeutral(",
       b"KnownV5FbxSha256"]
 need(all(v in editor for v in must),
      "real previous source Editor + new original resolver/preclone protected")
 need(editor.index(b"if (!TryValidateSourceVisualHierarchy(originalFbx, out rejection))") <
      editor.index(b"copy = Object.Instantiate(originalFbx);"),
      "foreign script original hierarchy must be rejected BEFORE first Instantiate")
 need(b"public const string ExactV5AssetPath" in pathsource and
      b"Path.GetFullPath(" in pathsource and
      b"UNITY_ASSETS_DIRECTORY_NOT_ABSOLUTE" in pathsource and
      b"FOREIGN_OR_UNAPPROVED_V5_ASSET_PATH" in pathsource,
      "source project-root relative path risk fixed")
 need(native.count(b"[Test]")==4 and
      b"Application.dataPath" in native and
      b"FOREIGN_SCRIPT_BEFORE_CLONE" in native and
      b"FOREIGN_PHYSICAL_JOINT_BEFORE_CLONE" in native and
      b"DUPLICATE_FBX_TRANSFORM_Muzzle_L" in native,
      "four true native EditMode original source tests (not executed)")
 metas=[n for n in files if n.startswith(ROOT) and n.endswith(".meta")]
 need(len(metas)==2,"two new source native Unity metadata, existing W30 8 GUID untouched")
 guids=[]
 for name in metas:
  lines=[s[6:].strip() for s in files[name].decode().splitlines() if s.startswith("guid: ")]
  need(len(lines)==1 and len(lines[0])==32 and
       all(c in "0123456789abcdef" for c in lines[0]),"real Unity script GUID")
  guids.extend(lines)
 need(len(set(guids))==2,"new W31 GUIDs unique")
 need(not any(n.lower().endswith((".fbx",".glb",".blend",".unitypackage",".exe")) for n in files),
      "external producer FBX or game EXE NOT included")
 out.mkdir(parents=True,exist_ok=True)
 b=io.BytesIO()
 with zipfile.ZipFile(b,"w") as z:
  for name,data in sorted(files.items()):
   info=zipfile.ZipInfo(name,date_time=(1980,1,1,0,0,0))
   info.compress_type=zipfile.ZIP_DEFLATED
   info.create_system=3
   info.external_attr=0o100644<<16
   z.writestr(info,data,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
 raw=b.getvalue()
 (out/"B0131W31-R41V5-PATH-PREFLIGHT-SOURCE-PATCH.zip").write_bytes(raw)
 receipt={
  "package_id":PKG,"revision":"P17-W31-R1",
  "exact_W30_parent":PARENT,
  "old_W30_Editor_Git_blob":"a138507c864a639d3ea6a8e219615412d7a7fc2c",
  "original_v5_FBX_SHA256_REFERENCE_ONLY":"fc83d677744d7e60b5f5f5f3ad3d9e72c97f84b66bbacf142c14959becfa5533",
  "actual_external_FBX_bytes_in_archive":False,
  "actual_zip_sha256":sha(raw),"actual_zip_bytes":len(raw),
  "file_count":len(files),"new_unique_Unity_meta_GUIDs":sorted(guids),
  "source_paths":[{"path":n,"bytes":len(d),"SHA256":sha(d),"Git_blob":blob(d)}
                  for n,d in sorted(files.items())],
  "real_CSharp29":"PENDING_CI","original_W30_CSharp52":"PENDING_CI",
  "W31_native_Unity_EditMode_four":"AUTHORED_NOT_RUN",
  "W30_real_external_FBX_EditMode_four":"AUTHORED_NOT_RUN",
  "unity_G1":"NOT_RUN","independent_formal_G0":"PENDING",
  "Codex_ACK":False,"integrated":False,"released":False
 }
 (out/"receipt.json").write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n",encoding="utf-8")
 print(json.dumps({"status":"PASS","actual_file_count":len(files),
        "new_GUID_count":len(guids),"actual_source_zip_sha256":sha(raw),
        "external_FBX":"REFERENCE_ONLY","native_Unity_G1":"NOT_RUN"}))
if __name__=="__main__":
 p=argparse.ArgumentParser();p.add_argument("--repo",required=True);p.add_argument("--out",required=True)
 a=p.parse_args()
 try:main(pathlib.Path(a.repo),pathlib.Path(a.out))
 except Exception as e:print(str(e),file=sys.stderr);raise
