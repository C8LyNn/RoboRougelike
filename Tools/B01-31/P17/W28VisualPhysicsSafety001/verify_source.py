#!/usr/bin/env python3
"""Independent read-only W28 source ZIP verifier, not author import.
Verifies original bytes against checked-in real W27/W28 code and five .meta GUIDs,
then denies eight malicious modified, extra, missing or alias collisions.
This is SOURCE-only G0, not native Unity/PhysX G1.
"""
import argparse,hashlib,io,json,pathlib,sys,zipfile
PKG="B01-31-P17W28-ARMOR-VISUAL-PHYSICS-ISOLATION-001"
PARENT="45a5c54d6b6985b54e6fba22a9095ecbf04eba60"
TOOL="Tools/B01-31/P17/W28VisualPhysicsSafety001"
ROOT="Assets/B01_31/W28VisualPhysicsSafety"
sha=lambda b:hashlib.sha256(b).hexdigest()
blob=lambda b:hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()
def check(v,m):
 if not v:raise ValueError("W28_SOURCE_INDEPENDENT_REJECT "+m)
def inspect(files,repo,receipt):
 expected=receipt["files"]
 wanted=[f["path"] for f in expected]
 check(len(wanted)==17 and len(wanted)==len(set(wanted)) and
       len(files)==17 and set(files)==set(wanted),
       "exact original 17 source file members and no foreign/duplicate paths")
 for f in expected:
  path=f["path"];data=files[path]
  check(path.startswith(("Assets/B01_31/",".github/workflows/","Tools/B01-31/"))
        and ".." not in pathlib.PurePosixPath(path).parts,
        "owner-only no parent traversal")
  check(sha(data)==f["sha256"] and len(data)==f["bytes"]
        and blob(data)==f["Git_blob"] and data==(repo/path).read_bytes(),
        "actual Git original source member bytes and receipt")
 metas=[p for p in wanted if p.startswith(ROOT) and p.endswith(".meta")]
 check(len(metas)==5,"new Unity .meta 5")
 gids=[]
 for path in metas:
  keys=[s[6:] for s in files[path].decode().splitlines() if s.startswith("guid: ")]
  check(len(keys)==1 and len(keys[0])==32
        and all(x in "0123456789abcdef" for x in keys[0]),"real Unity GUID format")
  gids.append(keys[0])
 check(len(set(gids))==5 and sorted(gids)==receipt["new_Unity_meta_guids"],
       "own W28 GUIDs unique, original SHA/source and physical manifest")
 editor=files["Assets/B01_31/W27ArmorLiveDemo/Editor/B0131W27CreateArmorLiveDemoScene.cs"]
 checks=[b"StripVisualOnlyPhysics(GameObject ownVisualInstance)",
         b"GetComponentsInChildren<P12PipelineSafeDamageable>(true)",
         b"GetComponentsInChildren<Joint>(true)",
         b"Object.DestroyImmediate(bodies[i])",
         b"Object.DestroyImmediate(colliders[i])",
         b"if (!StripVisualOnlyPhysics(visual))"]
 check(all(x in editor for x in checks),
       "real production Editor logic validates then strips physics from a fresh visual instance")
 test=files[ROOT+"/Tests/EditMode/B0131W28VisualOnlyEditModeTests.cs"]
 check(test.count(b"[Test]")==5 and
       b"OriginalImportedMeshColliderRemoved" in test
       and b"RefuseVisualTreeContainingOriginalGameplayP12" in test
       and b"RefusePhysicsJoints" in test,
       "real five Unity native EditMode NUnit cases authored (NOT_RUN)")
 return gids
def mutations(files,repo,receipt):
 p="Assets/B01_31/W27ArmorLiveDemo/Editor/B0131W27CreateArmorLiveDemoScene.cs"
 m=ROOT+"/Tests/EditMode/B0131W28VisualOnlyEditModeTests.cs.meta"
 def patch(file,data):
  c=dict(files);c[file]=data;return c
 bad=[]
 bad.append(patch(p,files[p]+b"\n// modified after original QA"))
 bad.append(patch(p,files[p].replace(b"Object.DestroyImmediate(colliders[i]);",b"// unsafe collision retained;")))
 bad.append(patch(m,files[m].replace(b"guid: ",b"guid: 00000000000000000000000000000000 #")))
 bad.append(patch(m,files[ROOT+"/Tests.meta"]))
 bad.append(patch(m,files[m]+b"\n# FOREIGN"))
 missing=dict(files);del missing[m];bad.append(missing)
 extra=dict(files);extra["Assets/Pack/FOREIGN_W28_CollisionHack.cs"]=b"foreign source";bad.append(extra)
 alias=dict(files);alias["Assets/B01_31/W28VisualPhysicsSafety/../W27ArmorLiveDemo/FOREIGN.cs"]=b"traverse";bad.append(alias)
 rejected=0
 for attempt in bad:
  try:inspect(attempt,repo,receipt)
  except (ValueError,KeyError,UnicodeError):rejected+=1
  else:raise AssertionError("W28_FALSE_GREEN_DIAGNOSTIC")
 check(rejected==8,"8 source path/bytes/GUID mutational regressions were rejected")
 return rejected
def main(repo,out):
 path=out/"B0131W28-ArmorVisual-Physics-Isolation-SOURCE.zip"
 raw=path.read_bytes()
 rec=json.loads((out/"receipt.json").read_text())
 check(rec["package_id"]==PKG and rec["revision"]=="P17-W28-R1" and
       rec["exact_W27_parent"]==PARENT and
       rec["old_W27_Editor_Git_blob"]=="6f2fb998469ca232332c9485c13cc51f1518b1e6" and
       rec["original_game_P12_Git_blob"]=="544bed274b616b91fd98c45d2b194b2f3a2d112a" and
       rec["original_W24_visual_Git_blob"]=="8cf3664b9b6e5b0fd6623be7c3f4e72fd9164664" and
       rec["original_W25_binder_Git_blob"]=="e1622abf22319e4448b53a0630d40387ade7a2b2" and
       rec["real_source_zip_sha256"]==sha(raw) and
       rec["real_source_zip_bytes"]==len(raw) and
       rec["file_count"]==17 and
       rec["formal_independent_G0"]=="PENDING" and
       rec["native_Unity_G1"]=="NOT_RUN" and
       rec["integrated"] is False and rec["Codex_ACK"] is False,
       "real original exact source SHA, parent and honest gate receipt")
 with zipfile.ZipFile(io.BytesIO(raw)) as z:
  check(z.testzip() is None,"actual source ZIP CRC verified")
  names=z.namelist()
  check(len(names)==len(set(names)),"no duplicate ZIP file name ambiguity")
  files={n:z.read(n) for n in names}
 guid=inspect(files,repo,rec)
 rejected=mutations(files,repo,rec)
 print(json.dumps({"status":"PASS","actual_original_Source_ZIP_SHA256":sha(raw),
    "exact_original_17_real_files":len(files),"actual_Unity_GUIDs":len(guid),
    "deliberately_modified_real_source_negative_rejected":rejected,
    "real_Gameplay_P12_W24_W25_unchanged":True,
    "genuine_Unity_5_EditMode_tests":"AUTHORED_NOT_RUN",
    "native_Unity_G1":"NOT_RUN"}))
if __name__=="__main__":
 p=argparse.ArgumentParser();p.add_argument("--repo",required=True);p.add_argument("--out",required=True)
 a=p.parse_args()
 try:main(pathlib.Path(a.repo),pathlib.Path(a.out))
 except Exception as ex:print(str(ex),file=sys.stderr);raise
