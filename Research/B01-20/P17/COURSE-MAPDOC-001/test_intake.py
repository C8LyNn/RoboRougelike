#!/usr/bin/env python3
"""Independent regression tests over REAL original-MAPGEN-CSharp → Codex MapDraft C# output."""
import copy
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import tempfile
import unittest

HERE=Path(__file__).resolve().parent
ROOT=Path(os.environ["P17_MAPDOC_INPUT"]).resolve()
spec=importlib.util.spec_from_file_location("pack",HERE/"package_intake.py")
pack=importlib.util.module_from_spec(spec)
spec.loader.exec_module(pack)

class OriginalMapdocR2Tests(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  if not (ROOT/"original-codex-load-receipt.json").exists():
   raise ValueError("ACTUAL_ORIGINAL_CODEX_CSHARP_RECEIPT_REQUIRED")
  cls.source=ROOT
 def setUp(self):
  self.temp=tempfile.TemporaryDirectory(prefix="b01-20-mapdoc-r2-")
  self.root=Path(self.temp.name)/"accepted"
  shutil.copytree(self.source,self.root,ignore=shutil.ignore_patterns(
   "codex-import-only-18-mapdocs.zip","codex-import-only-receipt.json"))
 def tearDown(self):
  self.temp.cleanup()
 def read(self,name):
  return json.loads((self.root/name).read_text(encoding="utf-8"))
 def write(self,name,doc):
  (self.root/name).write_text(json.dumps(doc,ensure_ascii=False,separators=(",",":"))+"\n",
                              encoding="utf-8",newline="\n")
 def fail(self,code):
  with self.assertRaisesRegex(pack.IntakeRejected,code):
   pack.build(self.root)
  self.assertFalse((self.root/pack.ZIP_NAME).exists())
 def test_authentic_original_mapdraft_18_only_zip(self):
  rec=pack.build(self.root)
  self.assertEqual(rec["zipMembers"],18)
  self.assertFalse(rec["zipContainsNonNativeCheckpointSidecars"])
  with pack.zipfile.ZipFile(self.root/pack.ZIP_NAME) as z:
   self.assertEqual(z.namelist(),sorted(pack.ALLOW))
   for info in z.infolist():
    self.assertTrue(info.filename.endswith("-mapdocument-v2.json"))
    self.assertEqual(info.date_time,(1980,1,1,0,0,0))
    self.assertEqual(z.read(info), (self.root/info.filename).read_bytes())
  self.assertEqual(rec["zipSha256"],pack.digest((self.root/pack.ZIP_NAME).read_bytes()))
  self.assertEqual(len(rec["mapDocuments"]),18)
 def test_binary_zip_replay_byte_identical_after_restarting_from_same_original_source(self):
  a=pack.build(self.root)["zipSha256"]
  (self.root/pack.ZIP_NAME).unlink()
  b=pack.build(self.root)["zipSha256"]
  self.assertEqual(a,b)
 def test_reject_unknown_source_mapgen_sha(self):
  rec=self.read("receipt.json");rec["sourceMapgen"]="0"*40
  self.write("receipt.json",rec);self.fail("SOURCE_RECEIPT_MISMATCH")
 def test_reject_missing_or_moved_original_codex_source_confirmation(self):
  rec=self.read("original-codex-load-receipt.json")
  rec["originalMapDraftGitBlob"]="0"*40
  self.write("original-codex-load-receipt.json",rec)
  self.fail("ORIGINAL_CODEX_CSHARP_NOT_VERIFIED")
 def test_reject_changed_map_bytes(self):
  rec=self.read("receipt.json")
  name=rec["converted"][0]["map"]
  with (self.root/name).open("ab") as f:f.write(b" ")
  self.fail("SOURCE_MAP_BYTES_MISMATCH")
 def test_reject_traversal_name_in_original_receipt(self):
  rec=self.read("receipt.json")
  rec["converted"][0]["map"]="../Assets/MapEditor/MapDraft.cs"
  self.write("receipt.json",rec)
  self.fail("UNKNOWN_OR_DUPLICATE_DOCUMENT")
 def test_reject_unapproved_extra_map(self):
  (self.root/"P17-COURSE-SECRET-seed-99-mapdocument-v2.json").write_text("{}",encoding="utf-8")
  self.fail("UNRECEIPTED_SOURCE_MAP_FILES")
 def test_reject_modified_item_even_if_attacker_rehashes_receipt(self):
  rec=self.read("receipt.json");entry=rec["converted"][0];name=entry["map"]
  doc=self.read(name)
  doc["items"][0]["kind"]=777
  self.write(name,doc)
  entry["mapSha256"]=hashlib.sha256((self.root/name).read_bytes()).hexdigest()
  self.write("receipt.json",rec)
  self.fail("UNSUPPORTED_KIND")
 def test_reject_missing_non_native_sidecar(self):
  rec=self.read("receipt.json")
  (self.root/rec["converted"][0]["sidecar"]).unlink()
  self.fail("MISSING_OR_SYMLINK")
 def test_reject_swapped_checkpoint_sidecar(self):
  rec=self.read("receipt.json")
  e=rec["converted"][0]
  data=self.read(e["sidecar"])
  data["sourceMapgenSha"]="0"*40
  self.write(e["sidecar"],data)
  e["sidecarSha256"]=hashlib.sha256((self.root/e["sidecar"]).read_bytes()).hexdigest()
  self.write("receipt.json",rec)
  self.fail("SIDECAR_PROVENANCE_MISMATCH")
 def test_reject_duplicate_file_in_original_receipt(self):
  rec=self.read("receipt.json")
  rec["converted"][1]=copy.deepcopy(rec["converted"][0])
  self.write("receipt.json",rec)
  self.fail("UNKNOWN_OR_DUPLICATE_DOCUMENT")
 def test_never_overwrite_preexisting_import_archive(self):
  (self.root/pack.ZIP_NAME).write_bytes(b"original preexisting artifact")
  with self.assertRaisesRegex(pack.IntakeRejected,"OUTPUT_ZIP_ALREADY_EXISTS"):
   pack.build(self.root)
  self.assertEqual((self.root/pack.ZIP_NAME).read_bytes(),b"original preexisting artifact")

if __name__=="__main__":
 unittest.main(verbosity=2)
