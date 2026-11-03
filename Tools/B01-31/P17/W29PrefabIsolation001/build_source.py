#!/usr/bin/env python3
"""W29 original strict source PATCH export. No write to Unity Assets or user PC."""
import argparse, hashlib, io, json, pathlib, sys, zipfile
PKG="B01-31-P17W29-DETACHED-ARMOR-PREFAB-INTAKE-001"
TOOL="Tools/B01-31/P17/W29PrefabIsolation001"
SHA=lambda b:hashlib.sha256(b).hexdigest()
BLOB=lambda b:hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()
def need(ok,msg):
 if not ok: raise ValueError("W29_SOURCE_EXPORT_RED "+msg)
def export(repo,out):
 m=json.loads((repo/TOOL/"manifest.json").read_text())
 paths=m["all_package_paths"]
 need(m["package_id"]==PKG and
      m["exact_W28_parent"]=="2508f533d3ed8c31f581e491afb2494ab7fa774c" and
      len(paths)==16 and paths==sorted(set(paths)) and
      sorted(m["modified_owner_paths"]+m["added_owner_paths"])==paths,
      "author owner manifest and 16 exact files")
 members={}
 for path in paths:
  pure=pathlib.PurePosixPath(path)
  need(".." not in pure.parts and not pure.is_absolute() and
       path.startswith(("Assets/B01_31/", "Tools/B01-31/",".github/workflows/")),
       "no foreign paths or escaping")
  content=(repo/path).read_bytes()
  need(len(content)>0,"empty real Git source")
  members[path]=content
 editor=members["Assets/B01_31/W27ArmorLiveDemo/Editor/B0131W27CreateArmorLiveDemoScene.cs"]
 for sig in (b"CreateDetachedVisualClone(",b"Object.Instantiate(originalPrefab)",
             b"PrefabUtility.UnpackPrefabInstance(",b"PrefabUnpackMode.Completely",
             b"InteractionMode.AutomatedAction",b"GetComponentsInChildren<MonoBehaviour>(true)",
             b"if (!StripVisualOnlyPhysics(visual))"):
  need(sig in editor,"original Unity source runtime API "+str(sig))
 metas=[p for p in members if p.startswith("Assets/B01_31/W29PrefabIsolation")
        and p.endswith(".meta")]
 need(len(metas)==5,"5 new-only owned .meta GUIDs")
 guid=[]
 for name in metas:
  rows=[line[6:].strip() for line in members[name].decode("utf8").splitlines()
        if line.startswith("guid: ")]
  need(len(rows)==1 and len(rows[0])==32 and
       all(ch in "0123456789abcdef" for ch in rows[0]),
       "valid native Unity GUID")
  guid.extend(rows)
 need(len(set(guid))==5,"no duplicate GUID")
 test=members["Assets/B01_31/W29PrefabIsolation/Tests/EditMode/B0131W29DetachedArmorPrefabTests.cs"]
 need(test.count(b"[Test]")==5 and b"PrefabUtility.IsPartOfPrefabInstance" in test and
      b"AssetDatabase.GetAssetDependencyHash" in test and
      b"OriginalThreeModelReferencesSurviveUnpacking" in test,
      "five true native NUnit original W24 prefab tests authored")
 out.mkdir(parents=True,exist_ok=True)
 data=io.BytesIO()
 with zipfile.ZipFile(data,"w") as z:
  for name,content in sorted(members.items()):
   info=zipfile.ZipInfo(name,date_time=(1980,1,1,0,0,0))
   info.compress_type=zipfile.ZIP_DEFLATED
   info.create_system=3
   info.external_attr=0o100644<<16
   z.writestr(info,content,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
 raw=data.getvalue()
 name="B0131W29-Original-W24-DetachedPrefab-SOURCE.zip"
 (out/name).write_bytes(raw)
 receipt={
  "package_id":PKG,"revision":"P17-W29-R1",
  "exact_W28_parent":m["exact_W28_parent"],
  "original_W28_Editor_Git_blob":"3a4a82c28f419497a0511e64d930e5dec30f9826",
  "source_ZIP_bytes":len(raw),"source_ZIP_sha256":SHA(raw),
  "source_original_file_count":len(paths),
  "5_new_real_Unity_GUIDs":sorted(guid),
  "original_source_files":[{"path":p,"sha256":SHA(members[p]),
                            "git_blob":BLOB(members[p]),"bytes":len(members[p])}
                           for p in sorted(members)],
  "native_W29_EditMode_5":"AUTHORED_NOT_RUN",
  "native_Unity_G1":"NOT_RUN",
  "B01_30_40_FORMAL_G0":"PENDING",
  "codeX_ACK":False,"G2":"HOLD","integration":False,"release":False}
 (out/"receipt.json").write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n",encoding="utf8")
 print(json.dumps({"status":"PASS","files":len(paths),"new_GUIDs":5,
                   "original_physical_SHA256":SHA(raw),"native_Unity_G1":"NOT_RUN"}))
if __name__=="__main__":
 a=argparse.ArgumentParser();a.add_argument("--repo",required=True);a.add_argument("--out",required=True)
 arg=a.parse_args()
 try:export(pathlib.Path(arg.repo),pathlib.Path(arg.out))
 except Exception as error: print(str(error),file=sys.stderr);raise
