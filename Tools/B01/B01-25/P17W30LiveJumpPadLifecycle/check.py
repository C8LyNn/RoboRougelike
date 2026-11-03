#!/usr/bin/env python3
"""W30: actual Git objects, exact original game source, real ZIP and scratch QA."""
import argparse,hashlib,json,pathlib,re,subprocess,tempfile,unittest,zipfile
ROOT=pathlib.Path(__file__).resolve().parents[4]
BASE="20273c3580079dfb299c30c85a67e76f8d893e7f"
R="Research/B01-25/P17/W30JumpPadLifecycle"
T="Tools/B01/B01-25/P17W30LiveJumpPadLifecycle"
W=".github/workflows/b01-25-p17w30-live-jumppad-lifecycle.yml"
M="Assets/B01Packages/MapJumpPad001/Runtime/MapJumpPad.cs"
N="Assets/Tests/Editor/W30LiveJumpPadDebrisLifecycleTests.cs"
MOD={M}
ADD={N,N+".meta",R+"/UnityPhysicsContractShim.cs",R+"/Program.cs",
 R+"/OriginalGameSource.csproj",R+"/.gitignore",T+"/check.py",T+"/README.md",
 T+"/.gitignore",T+"/manifest.json",W}
REFS={"Assets/R5/R5Arena.cs","Assets/Tests/Editor/W29LiveArenaJumpPadTests.cs",
 M+".meta","Assets/B01Packages/MapJumpPad001/Runtime/B01.MapJumpPad001.Runtime.asmdef"}
SRC={M,N,N+".meta"}
def git(*args):return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE)
def raw(p):return (ROOT/p).read_bytes()
def sha(b):return hashlib.sha1(("blob %d\0"%len(b)).encode()+b).hexdigest()
def doc(p):return json.loads(raw(p))
def pins():return doc(T+"/manifest.json")["gitBlobs"]
def confined(name):
 p=pathlib.PurePosixPath(name)
 if (name not in SRC and name!="PACKAGE_MANIFEST.json") or p.is_absolute() or ".." in p.parts or "\\" in name:raise ValueError(name)
 return p
def zip_sources(out):
 f={p:raw(p) for p in SRC}
 for p,b in f.items():
  if sha(b)!=pins()[p]:raise ValueError("invalid source: "+p)
 f["PACKAGE_MANIFEST.json"]=(json.dumps({"package":"B01-25-W30-LIVE-JUMPPAD-LIFECYCLE",
    "sourceOnly":True,"nativeUnity":"NOT_RUN","parent":BASE,
    "files":{p:sha(f[p]) for p in sorted(SRC)}},
    sort_keys=True,separators=(",",":"))+"\n").encode()
 with zipfile.ZipFile(out,"w") as z:
  for name in sorted(f):
   confined(name)
   info=zipfile.ZipInfo(name,(1980,1,1,0,0,0))
   info.create_system=3;info.compress_type=zipfile.ZIP_STORED
   info.external_attr=0o100644<<16
   z.writestr(info,f[name])
 return hashlib.sha256(out.read_bytes()).hexdigest()
def stage(zipname,root):
 with zipfile.ZipFile(zipname) as z:
  todo=[]
  for name in z.namelist():
   if name=="PACKAGE_MANIFEST.json":continue
   path=confined(name);dest=root.joinpath(*path.parts)
   ancestor=root
   for part in path.parts[:-1]:
    ancestor=ancestor/part
    if ancestor.is_symlink():raise ValueError("symlink")
   if dest.exists() or dest.is_symlink():raise FileExistsError(dest)
   todo.append((name,dest))
  added=[]
  try:
   for name,dest in todo:
    dest.parent.mkdir(parents=True,exist_ok=True)
    dest.write_bytes(z.read(name));added.append(dest)
   return added
  except Exception:
   for p in reversed(added):p.unlink()
   raise
class OriginalGameQA(unittest.TestCase):
 def test_01_exact_one_modified_original_plus_11_new_own_paths(self):
  self.assertEqual(MOD|ADD,set(git("diff","--name-only",BASE,"HEAD").decode().splitlines()))
  self.assertEqual(12,len(MOD|ADD))
 def test_02_exact_15_original_git_blobs_pinned(self):
  self.assertEqual(15,len(pins()))
  for path,blob in pins().items():
   self.assertEqual(raw(path),git("show","HEAD:"+path),path)
   self.assertEqual(sha(raw(path)),blob,path)
 def test_03_actual_R5Arena_and_old_native_jump_tests_unchanged(self):
  for path in ("Assets/R5/R5Arena.cs","Assets/Tests/Editor/W29LiveArenaJumpPadTests.cs"):
   self.assertEqual(raw(path),git("show",BASE+":"+path))
  self.assertIn(b"JumpPad(new Vector3(7f,0f,-16f))",raw("Assets/R5/R5Arena.cs"))
 def test_04_exact_original_jumppad_gameplay_not_duplicated(self):
  s=raw(M).decode()
  for term in ("sealed class MapJumpPad","private void OnTriggerEnter(Collider other)",
      "body.AddForce(velocityChange, ForceMode.VelocityChange)",
      "Time.fixedTimeAsDouble - last < cooldownSeconds",
      "bool first = colliders.Count == 0"):
   self.assertIn(term,s)
 def test_05_living_cooldown_preserved_but_destroyed_body_removed(self):
  s=raw(M).decode()
  for term in ("if (!entry.Key) staleBodies.Add(entry.Key);",
    "lastLaunch.Remove(body)","collider.attachedRigidbody != entry.Key",
    "!collider.enabled","!collider.gameObject.activeInHierarchy"):
   self.assertIn(term,s)
  section=s[s.index("private void FixedUpdate()"):s.index("private void OnDisable()")]
  self.assertNotIn("lastLaunch.Clear()",section)
 def test_06_authored_original_live_r5_nunit_two_not_native_run(self):
  s=raw(N).decode()
  self.assertEqual(2,s.count("[UnityTest]"))
  for v in ("R5ArenaKind.SalvageYard","WaitForFixedUpdate",
            "DestroyedLiveArenaBodyDoesNotLeakLaunchHistory",
            "DisabledMidTriggerCompoundColliderDoesNotStickInContactLedger"):
   self.assertIn(v,s)
 def test_07_original_meta_guid_new_unique_and_runtime_original_unchanged(self):
  self.assertEqual(raw(M+".meta"),git("show",BASE+":"+M+".meta"))
  guids=[]
  for p in (M+".meta",N+".meta"):
   m=re.search(rb"^guid: ([0-9a-f]{32})$",raw(p),re.M)
   self.assertIsNotNone(m,p);guids.append(m.group(1))
  self.assertEqual(2,len(set(guids)))
 def test_08_original_game_Csharp9_direct_link_and_no_fake_copy(self):
  s=raw(R+"/OriginalGameSource.csproj").decode()
  self.assertIn("MapJumpPad.cs",s)
  self.assertIn("<LangVersion>9.0</LangVersion>",s)
  self.assertIn("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>",s)
  self.assertFalse((ROOT/R/"MapJumpPad.cs").exists())
 def test_09_10_managed_tests_execute_direct_original_methods(self):
  s=raw(R+"/Program.cs").decode()
  self.assertEqual(10,s.count('Test("W30_'))
  self.assertIn("ORIGINAL_RUNTIME_WITH_MANAGED_UNITY_SHIM",s)
  self.assertIn('nativeUnity="NOT_RUN"',s)
 def test_10_zip_four_original_source_bytes_deterministic(self):
  with tempfile.TemporaryDirectory() as d:
   a=pathlib.Path(d)/"a.zip";b=pathlib.Path(d)/"b.zip"
   self.assertEqual(zip_sources(a),zip_sources(b))
   self.assertEqual(a.read_bytes(),b.read_bytes())
   with zipfile.ZipFile(a) as z:
    self.assertEqual(SRC|{"PACKAGE_MANIFEST.json"},set(z.namelist()))
 def test_11_scratch_stage_rollback_preserves_foreign_empty_occupied(self):
  with tempfile.TemporaryDirectory() as d:
   r=pathlib.Path(d);z=r/"source.zip";zip_sources(z)
   empty=r/"FOREIGN_EMPTY";empty.mkdir()
   keep=r/"FOREIGN"/"keep.txt";keep.parent.mkdir()
   keep.write_text("KEEP")
   new=stage(z,r)
   self.assertEqual(3,len(new))
   for p in reversed(new):p.unlink()
   self.assertEqual("KEEP",keep.read_text())
   self.assertTrue(empty.is_dir());self.assertEqual([],list(empty.iterdir()))
 def test_12_no_overwrite_existing_original_unity_code(self):
  with tempfile.TemporaryDirectory() as d:
   r=pathlib.Path(d);z=r/"source.zip";zip_sources(z)
   current=r/M;current.parent.mkdir(parents=True);current.write_text("FROZEN")
   with self.assertRaises(FileExistsError):stage(z,r)
   self.assertEqual("FROZEN",current.read_text())
 def test_13_symlink_and_foreign_archive_paths_denied(self):
  for n in ("../escape","/tmp/evil","Assets/R3/R3GarageView.cs","X\\evil"):
   with self.assertRaises(ValueError):confined(n)
  with tempfile.TemporaryDirectory() as d:
   r=pathlib.Path(d);z=r/"source.zip";zip_sources(z)
   foreign=r/"external";foreign.mkdir()
   (r/"Assets").symlink_to(foreign,target_is_directory=True)
   with self.assertRaises(ValueError):stage(z,r)
   self.assertEqual([],list(foreign.iterdir()))
 def test_14_original_diff_only_existing_game_jumppad_owned_test_doc_ci(self):
  paths=set(git("diff","--name-only",BASE,"HEAD").decode().splitlines())
  self.assertTrue(all(p==M or p in (N,N+".meta",W) or
    p.startswith(R+"/") or p.startswith(T+"/") for p in paths))
 def test_15_manifest_declares_source_only_native_hold_and_exact_parent(self):
  m=doc(T+"/manifest.json")
  self.assertEqual(BASE,m["baseCandidateSha"])
  self.assertEqual("SOURCE_ONLY",m["scope"])
  self.assertFalse(m["integrated"]);self.assertFalse(m["released"])
  self.assertEqual("NOT_RUN",m["nativeUnity"])
  self.assertTrue(m["requiresIndependentG0"])
 def test_16_receiver_docs_include_no_false_player_acceptance(self):
  s=raw(T+"/README.md").decode()
  self.assertGreater(len(s),1000)
  for term in ("NOT_RUN","G0","G1","R5Arena","MapJumpPad","R3Debris","Windows"):
   self.assertIn(term,s)
def main():
 ap=argparse.ArgumentParser();ap.add_argument("--out",required=True)
 a=ap.parse_args();out=ROOT/a.out;out.parent.mkdir(parents=True,exist_ok=True)
 r=unittest.TextTestRunner(verbosity=2).run(
  unittest.defaultTestLoader.loadTestsFromTestCase(OriginalGameQA))
 with tempfile.TemporaryDirectory() as d:
  z=pathlib.Path(d)/"original.zip";digest=zip_sources(z)
  out.with_suffix(".zip").write_bytes(z.read_bytes())
 good=r.testsRun-len(r.failures)-len(r.errors)
 out.write_text(json.dumps({"package":"B01-25-W30-JUMPPAD-LIVE-DEBRIS",
   "originalGitQA":r.testsRun,"passed":good,"sourceZipSHA256":digest,
   "nativeUnity":"NOT_RUN","WindowsPlayer":"NOT_RUN",
   "formalG0":"PENDING"},sort_keys=True,indent=2)+"\n")
 print("B0125_W30_REAL_GAME_SOURCE_GIT_QA",good,"/",r.testsRun,
       "SOURCE_ZIP_SHA256",digest,"UNITY_G1_NOT_RUN")
 raise SystemExit(not r.wasSuccessful())
if __name__=="__main__":main()
