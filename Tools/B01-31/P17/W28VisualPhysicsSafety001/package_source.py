#!/usr/bin/env python3
"""Reproducible physical source patch ZIP with exact source/GUID provenance.
A W28 patch is NOT a standalone Unitypackage: its W27 Editor file replaces a
prior owner file and MUST be approved on an isolated W27 tree, not auto-installed.
"""
import argparse,hashlib,json,pathlib,sys,zipfile
PKG="B01-31-P17W28-ARMOR-VISUAL-PHYSICS-ISOLATION-001"
PARENT="45a5c54d6b6985b54e6fba22a9095ecbf04eba60"
EDITOR="Assets/B01_31/W27ArmorLiveDemo/Editor/B0131W27CreateArmorLiveDemoScene.cs"
ROOT="Assets/B01_31/W28VisualPhysicsSafety"
TOOL="Tools/B01-31/P17/W28VisualPhysicsSafety001"
PATHS=(
 EDITOR,
 "Assets/B01_31/W28VisualPhysicsSafety.meta",
 ROOT+"/Tests.meta",ROOT+"/Tests/EditMode.meta",
 ROOT+"/Tests/EditMode/B0131W28VisualOnlyEditModeTests.cs",
 ROOT+"/Tests/EditMode/B0131W28VisualOnlyEditModeTests.cs.meta",
 ROOT+"/Tests/EditMode/RoboRougelike.B0131.W28.NativeEditModeTests.asmdef",
 ROOT+"/Tests/EditMode/RoboRougelike.B0131.W28.NativeEditModeTests.asmdef.meta",
 TOOL+"/Harness/W28RealEditorSourceHarness.csproj",
 TOOL+"/Harness/SyntheticUnityEngine.cs",
 TOOL+"/Harness/SyntheticUnityEditor.cs",
 TOOL+"/Harness/Program.cs",
 TOOL+"/package_source.py",TOOL+"/verify_source.py",
 TOOL+"/README.md",TOOL+"/manifest.json",
 ".github/workflows/b01-31-p17w28-real-armor-physics-safety.yml"
)
sha=lambda b:hashlib.sha256(b).hexdigest()
blob=lambda b:hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()
def check(v,m):
 if not v:raise ValueError("W28_PHYSICAL_AUTHOR_RED "+m)
def create(repo,out):
 manifest=json.loads((repo/(TOOL+"/manifest.json")).read_text())
 check(manifest["package_id"]==PKG and manifest["exact_W27_parent"]==PARENT
       and sorted(manifest["owner_source_paths"])==sorted(PATHS),
       "real source manifest exact path/parent")
 assets={}
 for name in PATHS:
  check(".." not in pathlib.PurePosixPath(name).parts and name.startswith(("Assets/B01_31/",".github/workflows/","Tools/B01-31/")),
        "owner-only no traversal")
  data=(repo/name).read_bytes()
  check(data,"no empty original source member")
  assets[name]=data
 code=assets[EDITOR]
 check(b"StripVisualOnlyPhysics(GameObject ownVisualInstance)" in code
       and b"Object.DestroyImmediate(bodies[i])" in code
       and b"Object.DestroyImmediate(colliders[i])" in code
       and b"GetComponentsInChildren<P12PipelineSafeDamageable>(true)" in code
       and b"GetComponentsInChildren<Joint>(true)" in code,
       "actual W27 changed Editor runtime physics fix")
 own_metas=[n for n in PATHS if n.startswith(ROOT) and n.endswith(".meta")]
 check(len(own_metas)==5,"5 entirely new Unity .meta files")
 guids=[]
 for path in own_metas:
  lines=[l[6:] for l in assets[path].decode().splitlines() if l.startswith("guid: ")]
  check(len(lines)==1 and len(lines[0])==32 and all(x in "0123456789abcdef" for x in lines[0]),
        "strict native Unity GUID on W28 new meta")
  guids.extend(lines)
 check(len(set(guids))==len(guids)==5,"new W28 GUIDs unique")
 out.mkdir(parents=True,exist_ok=True)
 archive=out/"B0131W28-ArmorVisual-Physics-Isolation-SOURCE.zip"
 with zipfile.ZipFile(archive,"w") as z:
  for name,content in sorted(assets.items()):
   zi=zipfile.ZipInfo(name,date_time=(1980,1,1,0,0,0))
   zi.compress_type=zipfile.ZIP_DEFLATED
   zi.create_system=3
   zi.external_attr=(0o100644<<16)
   z.writestr(zi,content,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
 raw=archive.read_bytes()
 receipt={"package_id":PKG,"revision":"P17-W28-R1",
          "exact_W27_parent":PARENT,"old_W27_Editor_Git_blob":"6f2fb998469ca232332c9485c13cc51f1518b1e6",
          "original_game_P12_Git_blob":"544bed274b616b91fd98c45d2b194b2f3a2d112a",
          "original_W24_visual_Git_blob":"8cf3664b9b6e5b0fd6623be7c3f4e72fd9164664",
          "original_W25_binder_Git_blob":"e1622abf22319e4448b53a0630d40387ade7a2b2",
          "real_source_zip_sha256":sha(raw),"real_source_zip_bytes":len(raw),
          "file_count":len(assets),
          "files":[{"path":n,"sha256":sha(c),"bytes":len(c),"Git_blob":blob(c)}
                   for n,c in sorted(assets.items())],
          "new_Unity_meta_guids":sorted(guids),
          "native_5_EditMode_tests":"AUTHORED_NOT_RUN",
          "native_5_prior_PlayMode_tests":"AUTHORED_NOT_RUN",
          "real_Editor_created_scene":"NOT_RUN",
          "formal_independent_G0":"PENDING","native_Unity_G1":"NOT_RUN",
          "Codex_ACK":False,"integrated":False,"merge":False}
 (out/"receipt.json").write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n",encoding="utf-8")
 print(json.dumps({"status":"PASS","actual_source_files":len(assets),"new_GUIDs":len(guids),
                   "original_ZIP_SHA256":sha(raw),"Unity_native":"NOT_RUN"}))
if __name__=="__main__":
 p=argparse.ArgumentParser();p.add_argument("--repo",required=True);p.add_argument("--out",required=True)
 a=p.parse_args()
 try:create(pathlib.Path(a.repo),pathlib.Path(a.out))
 except Exception as ex:print(str(ex),file=sys.stderr);raise
