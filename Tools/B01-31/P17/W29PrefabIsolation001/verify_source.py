#!/usr/bin/env python3
"""W29 independent source ZIP receiver; never imports exporter or writes Unity assets.
Rejects exact-byte tamper, extra/alias/foreign, GUID collisions, deleted tests.
Native Unity has NOT RUN; this checks real source transport and original methods.
"""
import argparse,hashlib,io,json,pathlib,sys,zipfile
PKG="B01-31-P17W29-DETACHED-ARMOR-PREFAB-INTAKE-001"
DIR="Assets/B01_31/W29PrefabIsolation"
EDITOR="Assets/B01_31/W27ArmorLiveDemo/Editor/B0131W27CreateArmorLiveDemoScene.cs"
TEST=DIR+"/Tests/EditMode/B0131W29DetachedArmorPrefabTests.cs"
SHA=lambda d:hashlib.sha256(d).hexdigest()
BLOB=lambda d:hashlib.sha1(b"blob "+str(len(d)).encode()+b"\0"+d).hexdigest()
def need(x,message):
 if not x:raise ValueError("W29_INDEPENDENT_SOURCE_RECEIVER_RED "+message)
def inspect(files,repo,receipt):
 expected=receipt["original_source_files"]
 paths=[row["path"] for row in expected]
 need(len(expected)==16 and len(paths)==len(set(paths)) and
      len(files)==16 and set(files)==set(paths),
      "precisely 16 original source files, no foreign/unowned members")
 for row in expected:
  p=row["path"];data=files[p]
  need(".." not in pathlib.PurePosixPath(p).parts and
       not p.startswith("/") and not "\\" in p and
       p.startswith(("Assets/B01_31/", "Tools/B01-31/",".github/workflows/")) and
       data==(repo/p).read_bytes() and len(data)==row["bytes"] and
       SHA(data)==row["sha256"] and BLOB(data)==row["git_blob"],
       "actual original source bytes/Git SHA/path "+p)
 names=[p for p in paths if p.startswith(DIR) and p.endswith(".meta")]
 need(len(names)==5,"five exclusive original Unity .meta")
 guids=[]
 for name in names:
  keys=[line[6:].strip() for line in files[name].decode("utf8").splitlines()
        if line.startswith("guid: ")]
  need(len(keys)==1 and len(keys[0])==32 and
       all(ch in "0123456789abcdef" for ch in keys[0]),"strict GUID")
  guids.extend(keys)
 need(len(set(guids))==5 and sorted(guids)==receipt["5_new_real_Unity_GUIDs"],
      "own 5 GUIDs collision-free original author/source")
 editor=files[EDITOR]
 for token in (b"CreateDetachedVisualClone(",
               b"Object.Instantiate(originalPrefab)",
               b"PrefabUtility.UnpackPrefabInstance(",
               b"PrefabUnpackMode.Completely",
               b"InteractionMode.AutomatedAction",
               b"GetComponentsInChildren<MonoBehaviour>(true)",
               b"if (!StripVisualOnlyPhysics(visual))"):
  need(token in editor,"actual approved game editor method not malformed "+str(token))
 case=files[TEST]
 need(case.count(b"[Test]")==5 and
      b"ActualW24PrefabCopyIsUnpacked" in case and
      b"AttachedRigidbodyAndInactiveChildCollider" in case and
      b"OriginalThreeModelReferencesSurviveUnpacking" in case and
      b"UnexpectedImportedMonoBehaviourFailsClosed" in case and
      b"InvalidSceneOrUnmarkedForeignSource" in case,
      "five actual Unity native original W24 imported prefab tests source")
 return guids
def hostile(files,repo,receipt):
 p=EDITOR; meta=DIR+"/Tests/EditMode/B0131W29DetachedArmorPrefabTests.cs.meta"
 def patched(name,data):
  x=dict(files);x[name]=data;return x
 cases=[
  patched(p,files[p]+b"\n// silent unreviewed changed W29 code"),
  patched(p,files[p].replace(b"PrefabUnpackMode.Completely",b"PrefabUnpackMode.OutermostRoot")),
  patched(meta,files[meta].replace(b"guid: ",b"guid: deadbeef")),
  patched(meta,files[DIR+"/Tests.meta"]),
  patched(TEST,files[TEST].replace(b"[Test]",b"[Ignore]")),
 ]
 missing=dict(files);missing.pop(meta);cases.append(missing)
 extra=dict(files);extra["Assets/Pack/FOREIGN_RigOwner.cs"]=b"unsafe";cases.append(extra)
 alias=dict(files);alias[DIR+"/../W24ArmorStates/FOREIGN.cs"]=b"unsafe";cases.append(alias)
 count=0
 for x in cases:
  try:inspect(x,repo,receipt)
  except (ValueError,KeyError,UnicodeError):count+=1
  else:raise AssertionError("W29_INVALID_ORIGINAL_SOURCE_FALSE_GREEN")
 need(count==8,"8 corrupted original sources rejected")
 return count
def verify(repo,out):
 src=(out/"B0131W29-Original-W24-DetachedPrefab-SOURCE.zip").read_bytes()
 receipt=json.loads((out/"receipt.json").read_text(encoding="utf8"))
 need(receipt["package_id"]==PKG and receipt["revision"]=="P17-W29-R1" and
      receipt["exact_W28_parent"]=="2508f533d3ed8c31f581e491afb2494ab7fa774c" and
      receipt["original_W28_Editor_Git_blob"]=="3a4a82c28f419497a0511e64d930e5dec30f9826" and
      receipt["source_ZIP_sha256"]==SHA(src) and receipt["source_ZIP_bytes"]==len(src) and
      receipt["source_original_file_count"]==16 and
      receipt["native_W29_EditMode_5"]=="AUTHORED_NOT_RUN" and
      receipt["native_Unity_G1"]=="NOT_RUN" and
      receipt["B01_30_40_FORMAL_G0"]=="PENDING" and
      receipt["codeX_ACK"] is False and receipt["G2"]=="HOLD" and
      receipt["integration"] is False and receipt["release"] is False,
      "exact original source receipt and honest incomplete gates")
 with zipfile.ZipFile(io.BytesIO(src)) as z:
  need(z.testzip() is None,"original physical ZIP CRC")
  names=z.namelist()
  need(len(names)==len(set(names)),"no duplicate ZIP naming")
  files={name:z.read(name) for name in names}
 g=inspect(files,repo,receipt)
 negative=hostile(files,repo,receipt)
 print(json.dumps({"status":"PASS","physical_source_zip_sha256":SHA(src),
                   "exact_original_files":len(files),
                   "new_original_Unity_GUIDs":len(g),
                   "real_source_corruption_cases_rejected":negative,
                   "native_Unity_G1":"NOT_RUN"}))
if __name__=="__main__":
 a=argparse.ArgumentParser();a.add_argument("--repo",required=True);a.add_argument("--out",required=True)
 p=a.parse_args()
 try:verify(pathlib.Path(p.repo),pathlib.Path(p.out))
 except Exception as error:print(str(error),file=sys.stderr);raise
