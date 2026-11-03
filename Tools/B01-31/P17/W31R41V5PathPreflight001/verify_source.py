#!/usr/bin/env python3
"""W31 read-only independent receiver of exact Git bytes+physical ZIP, 8 RED mutants.
Does not import producer script and never asserts native Unity.
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
def need(x,msg):
 if not x:raise ValueError("B01_31_W31_ORIGINAL_RECEIVER_RED "+msg)
def inspect(files,receipt,repo):
 expected=receipt["source_paths"]
 wanted=[x["path"] for x in expected]
 need(len(wanted)==12 and len(wanted)==len(set(wanted)) and
      len(files)==12 and set(files)==set(wanted),
      "12 original source paths exactly, no unowned/foreign duplicates")
 for row in expected:
  name=row["path"];data=files[name]
  p=pathlib.PurePosixPath(name)
  need(".." not in p.parts and not p.is_absolute() and "\\" not in name and
       name.startswith((ROOT,TOOL,".github/workflows/b01-31-p17w31-")) and
       data==(repo/name).read_bytes() and len(data)==row["bytes"] and
       sha(data)==row["SHA256"] and blob(data)==row["Git_blob"],
       "true Git original path and byte identity "+name)
 editor=files[EDITOR]
 must=[b"R41V5AssetPaths.TryResolveFbxPath(",
       b"Application.dataPath",b"File.OpenRead(realFbxPath)",
       b"TryValidateSourceVisualHierarchy(",
       b"GetComponentsInChildren<MonoBehaviour>(true)",
       b"GetComponentsInChildren<Joint>(true)",
       b"FindUnique(originalVisual.transform, \"Muzzle_L\")",
       b"FindUnique(originalVisual.transform, \"Muzzle_R\")",
       b"if (!TryValidateSourceVisualHierarchy(originalFbx, out rejection))",
       b"copy = Object.Instantiate(originalFbx);"]
 need(all(v in editor for v in must),
       "original Editor real source FBX hash/project-root/pre-instantiate guards")
 need(editor.index(b"if (!TryValidateSourceVisualHierarchy(originalFbx, out rejection))") <
      editor.index(b"copy = Object.Instantiate(originalFbx);"),
      "original foreign scripts MUST be rejected before cloning")
 pathsource=files[SOURCE]
 need(b"ExactV5AssetPath" in pathsource and
      b"Path.GetFullPath(" in pathsource and
      b"FOREIGN_OR_UNAPPROVED_V5_ASSET_PATH" in pathsource and
      b"UNITY_ASSETS_ROOT_NAME_MISMATCH" in pathsource,
      "actual production asset-path source guards")
 test=files[NATIVE]
 need(test.count(b"[Test]")==4 and
      b"W31OriginalGameplayMonoBehaviourRefusedBeforeAnyClone" in test and
      b"W31ImportedJointRefusedBeforeCloneAndLeavesPhysicsUntouched" in test and
      b"W31DuplicateOrMissingMuzzleFailsClosedWithoutMutatingSource" in test,
      "actual four original native tests authored not run")
 metas=[n for n in wanted if n.startswith(ROOT) and n.endswith(".meta")]
 need(len(metas)==2,"two new GUID files")
 ids=[]
 for name in metas:
  vals=[v[6:].strip() for v in files[name].decode().splitlines()
        if v.startswith("guid: ")]
  need(len(vals)==1 and len(vals[0])==32 and
       all(c in "0123456789abcdef" for c in vals[0]),"real Unity GUID format")
  ids+=vals
 need(len(set(ids))==2 and sorted(ids)==receipt["new_unique_Unity_meta_GUIDs"],
      "distinct original two Unity meta GUIDs and receipt parity")
 return ids
def hostile(files,receipt,repo):
 def changed(path,data):
  x=dict(files);x[path]=data;return x
 cases=[
  changed(EDITOR,files[EDITOR]+b"\n// unreviewed runtime mutation"),
  changed(EDITOR,files[EDITOR].replace(
    b"File.OpenRead(realFbxPath)",b"File.OpenRead(path)")),
  changed(EDITOR,files[EDITOR].replace(
    b"if (!TryValidateSourceVisualHierarchy(originalFbx, out rejection))",
    b"if (false)")),
  changed(SOURCE,files[SOURCE].replace(
    b"ExactV5AssetPath",b"UnrestrictedV5AssetPath")),
  changed(NATIVE,files[NATIVE].replace(b"[Test]",b"[Ignore]")),
  changed(ROOT+"/R41V5AssetPaths.cs.meta",
          files[ROOT+"/Tests/EditMode/R41V5W31PrecloneEditModeTests.cs.meta"]),
 ]
 missing=dict(files);missing.pop(ROOT+"/R41V5AssetPaths.cs.meta");cases.append(missing)
 extra=dict(files);extra["Assets/R3/FOREIGN_AUTHORITY_OVERWRITE.cs"]=b"UNAUTHORIZED";cases.append(extra)
 rejected=0
 for variant in cases:
  try:inspect(variant,receipt,repo)
  except (ValueError,KeyError,UnicodeError):rejected+=1
  else:raise AssertionError("W31_R41_FALSE_GREEN_CORRUPT_CODE")
 need(rejected==8,"eight deliberate original source/GUID/foreign negative attempts RED")
 return rejected
def verify(repo,out):
 receipt=json.loads((out/"receipt.json").read_text(encoding="utf-8"))
 original=(out/"B0131W31-R41V5-PATH-PREFLIGHT-SOURCE-PATCH.zip").read_bytes()
 need(receipt["package_id"]==PKG and receipt["revision"]=="P17-W31-R1"
      and receipt["exact_W30_parent"]==PARENT and
      receipt["old_W30_Editor_Git_blob"]=="a138507c864a639d3ea6a8e219615412d7a7fc2c"
      and receipt["actual_zip_sha256"]==sha(original) and
      receipt["actual_zip_bytes"]==len(original) and
      receipt["file_count"]==12 and
      receipt["actual_external_FBX_bytes_in_archive"] is False and
      receipt["W31_native_Unity_EditMode_four"]=="AUTHORED_NOT_RUN" and
      receipt["W30_real_external_FBX_EditMode_four"]=="AUTHORED_NOT_RUN" and
      receipt["unity_G1"]=="NOT_RUN" and
      receipt["independent_formal_G0"]=="PENDING" and
      receipt["Codex_ACK"] is False and receipt["integrated"] is False
      and receipt["released"] is False,"original source SHA and honest gates")
 with zipfile.ZipFile(io.BytesIO(original)) as z:
  need(z.testzip() is None,"original physical ZIP CRC")
  names=z.namelist()
  need(len(names)==len(set(names)),"no duplicate path entries")
  files={name:z.read(name) for name in names}
 ids=inspect(files,receipt,repo)
 rejected=hostile(files,receipt,repo)
 print(json.dumps({"status":"PASS","original_source_files":len(files),
    "original_source_sha256":sha(original),"new_native_Unity_GUIDs":len(ids),
    "original_mutation_negative_cases_rejected":rejected,
    "external_R41_FBX":"REFERENCE_ONLY","native_Unity_G1":"NOT_RUN"}))
if __name__=="__main__":
 p=argparse.ArgumentParser();p.add_argument("--repo",required=True);p.add_argument("--out",required=True)
 a=p.parse_args()
 try:verify(pathlib.Path(a.repo),pathlib.Path(a.out))
 except Exception as e:print(str(e),file=sys.stderr);raise
