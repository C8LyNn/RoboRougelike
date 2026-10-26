"""W15: real two-GLB receipt-forgery mutations; no fabricated gameplay QA."""
import copy
import importlib.util
import json
import os
import shutil
import tempfile
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
PACKAGE=Path(os.environ.get("CI_PACKAGE_DIR",str(ROOT/"_not_generated_yet")))
spec=importlib.util.spec_from_file_location("b01_w15_provenance",ROOT/"tools/verify_sweep_provenance.py")
v=importlib.util.module_from_spec(spec)
spec.loader.exec_module(v)

class ActualSweepIntegrityTests(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  if not (PACKAGE/"models/M04-024_LEG-MOV-001_R1.glb").exists():
   raise unittest.SkipTest("actual two-GLB Codex package is not yet staged")
  cls.receipt=v.audit_package(PACKAGE)
  cls.claims={m:json.loads((PACKAGE/"qa"/(m+"_SWEEP.json")).read_text()) for m in v.FILES}
  sampler=v.load_sampler()
  cls.real={m:sampler.inspect(PACKAGE/"models"/fn,samples=121) for m,fn in v.FILES.items()}

 def forged(self,model,change):
  claimed=copy.deepcopy(self.claims[model])
  change(claimed)
  with self.assertRaisesRegex(ValueError,"SWEEP_RECEIPT_FORGED"):
   v.validate_receipt(self.real[model],claimed,model)

 def test_real_two_pinned_models_authentic(self):
  self.assertEqual(self.receipt["status"],"BOTH_SOURCE_REPORTS_AUTHENTICATED")
  self.assertEqual({x["modelId"] for x in self.receipt["models"]},set(v.FILES))
 def test_real_animation_clip_counts(self):
  self.assertEqual({m:len(c["animations"]) for m,c in self.claims.items()},
                   {"M01-018":1,"M04-024":5})
 def test_exact_original_evidence(self):
  for model in v.FILES:
   self.assertEqual(v.validate_receipt(self.real[model],self.claims[model],model)["status"],
                    "SOURCE_RECOMPUTED_EQUAL")
 def test_forged_extent_with_unchanged_real_glb_sha(self):
  m="M04-024";claimed=copy.deepcopy(self.claims[m])
  for clip in claimed["animations"]:
   clip["sampledSweptMin"][2]=-.5
   clip["sampledSweptMax"][2]=.5
   clip["sampledSweptSize"][2]=1.0
  self.assertEqual(claimed["source_sha256"],self.real[m]["source_sha256"])
  with self.assertRaisesRegex(ValueError,"SWEEP_RECEIPT_FORGED"):
   v.validate_receipt(self.real[m],claimed,m)
 def test_forged_min(self):
  self.forged("M04-024",lambda x:x["animations"][0]["sampledSweptMin"].__setitem__(2,-.5))
 def test_forged_max(self):
  self.forged("M01-018",lambda x:x["animations"][0]["sampledSweptMax"].__setitem__(2,1.0))
 def test_forged_size_only(self):
  self.forged("M04-024",lambda x:x["animations"][-1]["sampledSweptSize"].__setitem__(2,.1))
 def test_forged_middle_animation_frame(self):
  self.forged("M04-024",lambda x:x["animations"][-1]["snapshots"][2]["maximum"].__setitem__(0,.1))
 def test_forged_channel_count(self):
  self.forged("M04-024",lambda x:x["animations"][-1].__setitem__("channels",1))
 def test_forged_clip_name(self):
  self.forged("M04-024",lambda x:x["animations"][-1].__setitem__("clip","FAKE"))
 def test_forged_animation_start(self):
  self.forged("M01-018",lambda x:x["animations"][0].__setitem__("startSeconds",3.2))
 def test_forged_vertex_count(self):
  self.forged("M04-024",lambda x:x["animations"][-1].__setitem__("verticesPerSample",1))
 def test_forged_extra_clip(self):
  self.forged("M04-024",lambda x:x["animations"].append(copy.deepcopy(x["animations"][0])))
 def test_forged_missing_clip(self):
  self.forged("M04-024",lambda x:x["animations"].pop())
 def test_forged_mesh_count(self):
  self.forged("M01-018",lambda x:x.__setitem__("meshCount",99))
 def test_forged_samples(self):
  self.forged("M04-024",lambda x:x.__setitem__("samplesPerClip",120))
 def test_misleading_evidence_disclaimer(self):
  fake=copy.deepcopy(self.claims["M04-024"])
  fake["disclaimer"]="UNITY_PHYSX_PASS"
  with self.assertRaisesRegex(ValueError,"mismatched evidence disclaimer"):
   v.validate_receipt(self.real["M04-024"],fake,"M04-024")
 def test_provenance_does_not_approve_mount(self):
  self.assertFalse(self.receipt["mountApproved"])
  self.assertEqual(self.receipt["nativeUnity"],"NOT_RUN")
 def test_actual_package_forgery_fails_end_to_end(self):
  with tempfile.TemporaryDirectory() as tmp:
   candidate=Path(tmp)/"source"
   shutil.copytree(PACKAGE,candidate)
   p=candidate/"qa/M04-024_SWEEP.json"
   forged=json.loads(p.read_text())
   for clip in forged["animations"]:
    clip["sampledSweptMin"][2]=-.5
    clip["sampledSweptMax"][2]=.5
    clip["sampledSweptSize"][2]=1.
   p.write_text(json.dumps(forged))
   with self.assertRaisesRegex(ValueError,"SWEEP_RECEIPT_FORGED"):
    v.audit_package(candidate)

if __name__=="__main__":unittest.main(verbosity=2)
