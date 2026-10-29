#!/usr/bin/env python3
"""Isolated negative tests against ACTUAL C# generated MapBlueprint, no handcrafted source layouts."""
import copy
import importlib.util
import json
from pathlib import Path
import sys
import unittest
ROOT=Path(__file__).parent
SOURCE_DIR=Path(sys.argv[-1]) if len(sys.argv)==2 else Path(".")
s=importlib.util.spec_from_file_location("converter",ROOT/"convert.py")
m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
class ConversionTests(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  source=SOURCE_DIR
  data=json.loads((source/"results.json").read_text(encoding="utf8"))
  cls.g=json.loads((source/data["generated"][0]["file"]).read_text(encoding="utf8"))
 def test_original_source_passes(self):
  doc,sc=m.convert(self.g)
  self.assertEqual(doc["version"],2)
  self.assertEqual(len(sc["checkpointMarkersNonNative"]),3)
  self.assertTrue(sc["nativeItemCounts"]["ground"]>100)
 def expect_bad(self,fn):
  raw=copy.deepcopy(self.g);fn(raw)
  with self.assertRaisesRegex(ValueError,"COURSE_MAPDOC_FAIL_CLOSED"):m.convert(raw)
 def test_reject_wrong_mapgen_sha(self):
  self.expect_bad(lambda x:x.__setitem__("sourceSha","wrong"))
 def test_reject_wrong_recipe(self):
  self.expect_bad(lambda x:x.__setitem__("recipe","CombatArena"))
 def test_reject_missing_checkpoint(self):
  self.expect_bad(lambda x:x.__setitem__("checkpoints",[]))
 def test_reject_off_floor_start(self):
  self.expect_bad(lambda x:x["start"].__setitem__("x",49))
 def test_reject_invalid_source_item_kind(self):
  self.expect_bad(lambda x:x["elements"][0].__setitem__("kind","Portal"))
 def test_reject_disconnected_source_floor(self):
  def erase(x):x["elements"]=[e for e in x["elements"] if e["kind"]!="Floor"]+[
    {"kind":"Floor","position":{"x":0,"z":1e3},"size":{"x":2,"z":2}}]
  self.expect_bad(erase)
if __name__=="__main__":
 sys.argv=[sys.argv[0]]
 unittest.main(verbosity=2)
