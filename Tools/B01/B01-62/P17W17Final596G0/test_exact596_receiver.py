#!/usr/bin/env python3
"""Independent byte-precise G0 physical selected #596 receipt, not Unity native G1."""
import hashlib,importlib.util,io,json,os,subprocess,tempfile,unittest,zipfile
from pathlib import Path
SHA="efb2b06eafedab5469b57f163152d96aca3c5915"
ZIP="005ac910ca9476203bc3ff1748d9198d45835abad21d97e12ad6c1bfe50484e9"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
class Selected596ReceiverQA(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.root=Path(os.environ["B0162_ORIGINAL596_WORKTREE"]).resolve()
  assert subprocess.check_output(["git","-C",str(cls.root),"rev-parse","HEAD"],text=True).strip()==SHA
  script=cls.root/"Tools/B01/B01-23/P17W15UnitySaveBridge001/package.py"
  spec=importlib.util.spec_from_file_location("exact596_original_author_packager",script)
  cls.p=importlib.util.module_from_spec(spec);spec.loader.exec_module(cls.p)
  cls.raw=cls.p.build()
  cls.files=cls.p.verify(cls.raw)
  cls.manifest=json.loads(cls.files[cls.p.MANIFEST])
 def stage(self,root):
  d=Path(root)/"only-selected596"
  self.p.stage(self.raw,d)
  return d
 def test_01_exact_original_git_head(self):
  self.assertEqual(self.p.git("rev-parse","HEAD"),SHA)
 def test_02_inner_zip_actual_bytes_sha(self):
  self.assertEqual(len(self.raw),81190)
  self.assertEqual(hashlib.sha256(self.raw).hexdigest(),ZIP)
 def test_03_exact_27_physical_entries(self):
  self.assertEqual(len(self.files),23)
  self.assertEqual(len(self.p.scope()),22)
 def test_04_all_git_blobs_exactly_unmodified(self):
  for k,blob in self.p.PINS.items():
   self.assertEqual(self.p.git("rev-parse","HEAD:"+k),blob)
  self.assertEqual(self.manifest["own_head_sha"],SHA)
  for info in self.manifest["files"]:
   self.assertEqual(hashlib.sha256(self.files[info["path"]]).hexdigest(),info["sha256"])
   self.assertEqual(self.files[info["path"]],self.p.source(info["path"]))
 def test_05_only_frozen_dependencies(self):
  self.assertEqual(self.manifest["only_save_source_sha"],SAVE)
  self.assertEqual(self.manifest["gameplay_510_sha"],GAME)
  self.assertEqual(self.manifest["native_unity"],"NOT_RUN")
  self.assertEqual(self.manifest["canonical_510_live_import"],"HOLD")
  self.assertFalse(self.manifest["codex_ack"])
 def test_06_exact_11_guids_three_scoped_asmdefs(self):
  paths=self.p.scope()
  guids=[]
  for path in paths:
   if path.endswith(".meta"):
    a=[line.split(":",1)[1].strip() for line in self.files[path].decode().splitlines() if line.startswith("guid:")]
    self.assertEqual(len(a),1);guids+=a
  self.assertEqual(len(guids),10);self.assertEqual(len(set(guids)),10)
  defs=[json.loads(self.files[x])["name"] for x in paths if x.endswith(".asmdef")]
  self.assertEqual(sorted(defs),["B0123.UnitySaveBridge.Core","B0123.UnitySaveBridge.PlayModeTests","B0123.UnitySaveBridge.Unity"])
 def test_07_no_alt_host_mixture_or_legacy_w07(self):
  paths=list(self.files)
  self.assertFalse(any("B0123SecureV2HostBridge001" in x or "B0123DebrisRig" in x or "DebrisRigPlayground" in x for x in paths))
  self.assertFalse(any("B0123SecureReattachV2" in x or "B0123DetachedSaveContract001" in x for x in paths))
 def test_08_real_owned_stage_and_clean_rollback(self):
  with tempfile.TemporaryDirectory(prefix="b0162-w17-") as t:
   d=self.stage(t);self.assertEqual((d/self.p.MANIFEST).read_bytes(),self.files[self.p.MANIFEST])
   self.p.rollback(self.raw,d);self.assertFalse(d.exists())
 def test_09_foreign_empty_root_directory_never_deleted(self):
  with tempfile.TemporaryDirectory(prefix="b0162-w17-") as t:
   d=self.stage(t);alien=d/"NotOwned"/"empty"
   alien.mkdir(parents=True)
   with self.assertRaises(ValueError):self.p.rollback(self.raw,d)
   self.assertTrue(alien.is_dir());self.assertEqual((d/self.p.MANIFEST).read_bytes(),self.files[self.p.MANIFEST])
 def test_10_foreign_empty_nested_Unity_folder_never_deleted(self):
  with tempfile.TemporaryDirectory(prefix="b0162-w17-") as t:
   d=self.stage(t);alien=d/"Assets"/"B0123UnitySaveBridge001"/"new-unowned-empty"
   alien.mkdir()
   with self.assertRaises(ValueError):self.p.rollback(self.raw,d)
   self.assertTrue(alien.is_dir())
 def test_11_dirty_source_file_blocks_rollback(self):
  with tempfile.TemporaryDirectory(prefix="b0162-w17-") as t:
   d=self.stage(t);p=d/"Assets"/"B0123UnitySaveBridge001"/"Core"/"AtomicWorldSwap.cs"
   p.write_text("DO-NOT-DELETE")
   with self.assertRaises(ValueError):self.p.rollback(self.raw,d)
   self.assertEqual(p.read_text(),"DO-NOT-DELETE")
 def test_12_missing_owned_file_blocks_rollback(self):
  with tempfile.TemporaryDirectory(prefix="b0162-w17-") as t:
   d=self.stage(t);(d/self.p.MANIFEST).unlink()
   with self.assertRaises(ValueError):self.p.rollback(self.raw,d)
   self.assertTrue(d.exists())
 def test_13_foreign_file_blocks_rollback(self):
  with tempfile.TemporaryDirectory(prefix="b0162-w17-") as t:
   d=self.stage(t);p=d/"USER_FILES.txt";p.write_text("important")
   with self.assertRaises(ValueError):self.p.rollback(self.raw,d)
   self.assertEqual(p.read_text(),"important")
 def test_14_collision_refuses_second_install(self):
  with tempfile.TemporaryDirectory(prefix="b0162-w17-") as t:
   d=self.stage(t)
   with self.assertRaises(ValueError):self.p.stage(self.raw,d)
   self.assertTrue(d.exists())
 def test_15_symlink_refused_and_preserved(self):
  with tempfile.TemporaryDirectory(prefix="b0162-w17-") as t:
   d=self.stage(t);p=d/"FOREIGN_LINK"
   try:p.symlink_to(t,target_is_directory=True)
   except (OSError,NotImplementedError):self.skipTest("symlink creation prohibited")
   with self.assertRaises(ValueError):self.p.rollback(self.raw,d)
   self.assertTrue(p.is_symlink())
 def test_16_original_zip_manifest_forgery_rejected(self):
  with zipfile.ZipFile(io.BytesIO(self.raw)) as z: items={x:z.read(x) for x in z.namelist()}
  m=json.loads(items[self.p.MANIFEST]);m["native_unity"]="PASS";items[self.p.MANIFEST]=json.dumps(m).encode()
  o=io.BytesIO()
  with zipfile.ZipFile(o,"w") as z:
   for name in sorted(items):z.writestr(self.p.entry(name),items[name])
  with self.assertRaises(ValueError):self.p.verify(o.getvalue())
 def test_17_original_zip_extra_alt_path_refused(self):
  o=io.BytesIO()
  with zipfile.ZipFile(io.BytesIO(self.raw)) as inp,zipfile.ZipFile(o,"w") as z:
   for name in inp.namelist():z.writestr(self.p.entry(name),inp.read(name))
   z.writestr(self.p.entry("Assets/B0123SecureV2HostBridge001/Runtime/OTHER.cs"),b"foreign")
  with self.assertRaises(ValueError):self.p.verify(o.getvalue())
 def test_18_unauthorized_repo_destination_refused(self):
  with self.assertRaises(ValueError):self.p.external(self.root/"DO_NOT_TOUCH")
 def test_19_second_clean_stage_roundtrip(self):
  with tempfile.TemporaryDirectory(prefix="b0162-w17-") as t:
   d=self.stage(t);self.p.rollback(self.raw,d)
   e=self.stage(t);self.p.rollback(self.raw,e)
   self.assertFalse(e.exists())
 def test_20_source_readback_r2_p0_guards(self):
  core=self.p.source("Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs").decode()
  unity=self.p.source("Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs").decode()
  for token in ("candidate.Activate();","post-activation physics/ID mismatch","checkpoint.Commit(","return quarantined?WorldSwapVerdict.RollbackQuarantine:verdict;","snap.Generation<=currentCold.Snapshot.Generation","FileCheckpointStore","FileShare.None","newest+1"):
   self.assertIn(token,core)
  for token in ("new FileCheckpointStore(sidecar)","Application.persistentDataPath","part.SetActive(true);"):
   self.assertIn(token,unity)
  self.assertNotIn("Assets/B0123UnitySaveBridge001/Core/CheckpointFence.cs",self.files)
if __name__=="__main__":unittest.main(verbosity=2)
