#!/usr/bin/env python3
"""Independent, read-only author-unimported R41V5 actual source ZIP verifier.
Explicitly not Unity/native/FBX acceptance; original FBX isn't in this repo.
"""
import argparse,hashlib,io,json,pathlib,sys,zipfile
PKG="B01-31-P17W30-R41-V5-AXIS-LOCKED-ADAPTER-001"
TOOL="Tools/B01-31/P17/W30TurretV5Adapter001"
ROOT="Assets/B01_31/W30R41V5Adapter"
VERIFIED="fc83d677744d7e60b5f5f5f3ad3d9e72c97f84b66bbacf142c14959becfa5533"
sha=lambda b:hashlib.sha256(b).hexdigest()
blob=lambda b:hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()
def check(x,m):
 if not x:raise ValueError("W30_R41V5_INDEPENDENT_RECEIVER_RED "+m)
def inspect(files,receipt,repo):
 spec=receipt["source_members"]
 names=[e["path"] for e in spec]
 check(len(names)==19 and len(set(names))==19 and set(files)==set(names)
       and len(files)==19,"exactly 19 original B01-31 new-only source paths")
 for row in spec:
  path=row["path"];content=files[path];p=pathlib.PurePosixPath(path)
  check(".." not in p.parts and not p.is_absolute()
        and not "\\" in path and path.startswith((ROOT,TOOL,".github/workflows/b01-31-"))
        and content==(repo/path).read_bytes()
        and sha(content)==row["sha256"] and blob(content)==row["git_blob"]
        and len(content)==row["bytes"],
        "source Git+SHA256 byte equality and owner-only no foreign path "+path)
 check(not any(path.lower().endswith((".fbx",".glb",".blend",".exe")) for path in names),
       "never ship any private original FBX binary as pseudo model proof")
 metas=[n for n in names if n.startswith(ROOT) and n.endswith(".meta")]
 check(len(metas)==8,"exact original 8 owned native Unity metadata")
 gids=[]
 for name in metas:
  lines=[s[6:].strip() for s in files[name].decode().splitlines()
         if s.startswith("guid: ")]
  check(len(lines)==1 and len(lines[0])==32
        and all(c in "0123456789abcdef" for c in lines[0]),
        "strict original Unity GUID bytes")
  gids.extend(lines)
 check(len(set(gids))==8 and sorted(gids)==receipt["new_unity_guid_values"],
       "8 unique native Unity GUID and exact physical metadata")
 code=files[ROOT+"/Editor/B0131W30CreateR41V5AdapterCandidate.cs"]
 assert_features=[
  b"KnownV5FbxSha256", VERIFIED.encode(), b"SHA256.Create()",b"bakeAxisConversion",
  b"Object.Instantiate(originalFbx)", b"PrefabUtility.UnpackPrefabInstance(",
  b"PrefabUnpackMode.Completely",b"InteractionMode.AutomatedAction",
  b"R41V5MuzzleGeometry.ValidateImportedNeutral(",
  b"R41V5MuzzleGeometry.TryPose(",b"R41V5MuzzleGeometry.ValidatePosePair(",
  b"GetComponentsInChildren<MonoBehaviour>(true)",
  b"PrefabUtility.SaveAsPrefabAsset(candidate,CandidatePath)",
  b"File.Exists(CandidatePath)",b"EditorUtility.DisplayDialog("
 ]
 check(all(v in code for v in assert_features),
       "real Unity source logic has exact version hash, physics and fallback guards")
 math=files[ROOT+"/R41V5MuzzleGeometry.cs"]
 check(b"GridMeters = 0.55" in math and
       b"BarrelLengthMeters = 0.700" in math and
       b"TryPose(" in math and b"ValidateImportedNeutral(" in math,
       "original pure C# 0.55m/0.70m kinematic source")
 test=files[ROOT+"/Tests/EditMode/R41V5ActualFbxImportEditModeTests.cs"]
 check(test.count(b"[Test]")==4 and
       b"Assert.Inconclusive(" in test and b"AssetDatabase.GetAssetDependencyHash" in test,
       "real Unity FBX NUnit test needs original native model; no mock PASS")
 return sorted(gids)
def negative(files,receipt,repo):
 editor=ROOT+"/Editor/B0131W30CreateR41V5AdapterCandidate.cs"
 math=ROOT+"/R41V5MuzzleGeometry.cs"
 test=ROOT+"/Tests/EditMode/R41V5ActualFbxImportEditModeTests.cs"
 meta=ROOT+"/Editor/B0131W30CreateR41V5AdapterCandidate.cs.meta"
 def change(p,content):
  o=dict(files);o[p]=content;return o
 cases=[
  change(editor,files[editor]+b"\n// mutated unreviewed importer"),
  change(editor,files[editor].replace(VERIFIED.encode(),b"0"*64)),
  change(editor,files[editor].replace(b"PrefabUnpackMode.Completely",b"PrefabUnpackMode.OutermostRoot")),
  change(math,files[math].replace(b"GridMeters = 0.55",b"GridMeters = 0.75")),
  change(test,files[test].replace(b"Assert.Inconclusive(",b"Assert.Pass(")),
  change(meta,files[ROOT+"/Tests.meta"]),
 ]
 missing=dict(files);missing.pop(meta);cases.append(missing)
 extra=dict(files);extra["Assets/R3/COMPETING_AUTOMATICGUN.cs"]=b"UNAUTHORIZED";cases.append(extra)
 rejected=0
 for pack in cases:
  try:inspect(pack,receipt,repo)
  except (ValueError,KeyError,UnicodeError):rejected+=1
  else:raise AssertionError("W30_R41V5_FALSE_PASS_MODIFIED_GAME_OR_IMPORTER")
 check(rejected==8,"8 altered real source/GUID/fake-native-PASS packets rejected")
 return rejected
def verify(repo,out):
 rec=json.loads((out/"receipt.json").read_text(encoding="utf-8"))
 file=(out/"B0131W30-R41V5-AXIS-GATED-SOURCE.zip").read_bytes()
 check(rec["package_id"]==PKG and rec["revision"]=="P17-W30-R1" and
       rec["exact_W29_parent"]=="ced677d7b3f9fa81e6b0e5b51a028a518770e096" and
       rec["original_external_v5_FBX_SHA256_reference_only"]==VERIFIED and
       rec["external_FBX_physically_included"] is False and
       rec["actual_source_zip_sha256"]==sha(file) and
       rec["actual_source_zip_bytes"]==len(file) and rec["actual_source_files"]==19 and
       rec["native_Unity_EditMode_4"]=="AUTHORED_NOT_RUN" and
       rec["native_Unity_G1"]=="NOT_RUN" and
       rec["original_FBX_native_import_W30"]=="NOT_RUN" and
       rec["B01_30_40_independent_formal_G0"]=="PENDING" and
       rec["Codex_ACK"] is False and
       rec["integrated"] is False and rec["released"] is False,
       "original source exact parent and honest model/native gates")
 with zipfile.ZipFile(io.BytesIO(file)) as z:
  check(z.testzip() is None,"actual ZIP CRC")
  names=z.namelist()
  check(len(set(names))==len(names),"no source ZIP duplicate or shadows")
  files={n:z.read(n) for n in names}
 guids=inspect(files,rec,repo)
 rejected=negative(files,rec,repo)
 print(json.dumps({"status":"PASS","actual_original_sources":len(files),
       "actual_new_Unity_GUIDs":len(guids),"original_SHA256":sha(file),
       "intentionally_corrupted_original_sources_REJECTED":rejected,
       "external_FBX":"REFERENCE_ONLY_NOT_PRESENT",
       "native_Unity_G1":"NOT_RUN"}))
if __name__=="__main__":
 a=argparse.ArgumentParser();a.add_argument("--repo",required=True);a.add_argument("--out",required=True)
 arg=a.parse_args()
 try:verify(pathlib.Path(arg.repo),pathlib.Path(arg.out))
 except Exception as e:print(str(e),file=sys.stderr);raise
