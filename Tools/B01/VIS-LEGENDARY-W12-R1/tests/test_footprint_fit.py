"""Real two-GLB footprint gate and negative mutation suite. Native Unity NOT_RUN."""
import copy
import importlib.util
import json
import os
import tempfile
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
PACKAGE=Path(os.environ.get("CI_PACKAGE_DIR",str(ROOT/"_not_generated_yet")))
spec=importlib.util.spec_from_file_location("footprint_fit_qa",ROOT/"tools/footprint_fit_qa.py")
f=importlib.util.module_from_spec(spec)
spec.loader.exec_module(f)

class FitTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if not (PACKAGE/"qa/M04-024_SWEEP.json").exists():
            raise unittest.SkipTest("complete two-asset ZIP staging does not exist yet")
        contract=json.loads((PACKAGE/"binding_spec.json").read_text())
        cls.models={}
        for m in contract["mounts"]:
            name=m["id"]
            fname="M01-018_LEG-WPN-001_R1.glb" if name=="M01-018" else "M04-024_LEG-MOV-001_R1.glb"
            binary=(PACKAGE/"models"/fname).read_bytes()
            data=json.loads((PACKAGE/"qa"/(name+"_SWEEP.json")).read_text())
            cls.models[name]=(m,data,binary)
    def test_real_complete_package_blocks(self):
        with tempfile.TemporaryDirectory() as tmp:
            result=f.assess_package(PACKAGE,Path(tmp)/"fit.json")
            self.assertEqual(result["status"],"BLOCKED_FOR_PHYSICAL_MOUNT")
            self.assertEqual({a["modelId"] for a in result["reports"]},set(f.MODELS))
    def test_real_m01_length(self):
        r=f.evaluate(*self.models["M01-018"])
        self.assertAlmostEqual(r["extentRemainderM"][2],-2.572,places=5)
        self.assertFalse(r["approvedToMount"])
    def test_real_m04_depth(self):
        r=f.evaluate(*self.models["M04-024"])
        self.assertAlmostEqual(r["extentRemainderM"][2],-0.47756,places=5)
        self.assertFalse(r["approvedToMount"])
    def test_unapproved_permuted_footprint_is_not_enough(self):
        m,s,b=self.models["M04-024"];m=copy.deepcopy(m);m["footprintNominal"]=[3,2,3]
        r=f.evaluate(m,s,b)
        self.assertTrue(r["sizeFitsZeroMargin"])
        self.assertFalse(r["sizeFitsMargin"])
        self.assertFalse(r["approvedToMount"])
    def test_larger_nominal_cannot_authorize_socket(self):
        m,s,b=self.models["M01-018"];m=copy.deepcopy(m);m["footprintNominal"]=[8,8,8]
        r=f.evaluate(m,s,b)
        self.assertTrue(r["sizeFitsMargin"])
        self.assertFalse(r["approvedToMount"])
    def test_modified_glb_fails_hash(self):
        m,s,b=self.models["M04-024"]
        with self.assertRaisesRegex(ValueError,"stale or tampered"):
            f.evaluate(m,s,b+b"mutated")
    def test_nonfinite_aabb_is_rejected(self):
        m,s,b=self.models["M04-024"];s=copy.deepcopy(s)
        s["animations"][0]["sampledSweptMin"][2]=float("nan")
        with self.assertRaisesRegex(ValueError,"invalid sampled"):f.evaluate(m,s,b)
    def test_missing_combined_gimbal_is_rejected(self):
        m,s,b=self.models["M04-024"];s=copy.deepcopy(s)
        s["animations"]=s["animations"][:-1]
        with self.assertRaisesRegex(ValueError,"combined clip"):f.evaluate(m,s,b)
    def test_one_channel_combined_clip_is_rejected(self):
        m,s,b=self.models["M04-024"];s=copy.deepcopy(s)
        s["animations"][-1]["channels"]=1
        with self.assertRaisesRegex(ValueError,"combined clip"):f.evaluate(m,s,b)
    def test_too_few_samples_rejected(self):
        m,s,b=self.models["M04-024"];s=copy.deepcopy(s)
        s["samplesPerClip"]=120
        with self.assertRaisesRegex(ValueError,"insufficient"):f.evaluate(m,s,b)
    def test_mass_calibration_not_silently_authorized(self):
        m,s,b=self.models["M01-018"];m=copy.deepcopy(m);m["massKg"]=650
        with self.assertRaisesRegex(ValueError,"unapproved mass"):f.evaluate(m,s,b)
    def test_decorative_node_cannot_be_build_socket(self):
        m,s,b=self.models["M04-024"];m=copy.deepcopy(m);m["visualNode"]="BuildSocketAuthority"
        with self.assertRaisesRegex(ValueError,"visual-only"):f.evaluate(m,s,b)
    def test_negative_margin_rejected(self):
        with self.assertRaisesRegex(ValueError,"invalid nonnegative"):f.evaluate(*self.models["M01-018"],margin_m=-1)
    def test_empty_animation_list_rejected(self):
        m,s,b=self.models["M01-018"];s=copy.deepcopy(s);s["animations"]=[]
        with self.assertRaisesRegex(ValueError,"no actual"):f.evaluate(m,s,b)
    def test_mismatched_legend_id_rejected(self):
        m,s,b=self.models["M01-018"];m=copy.deepcopy(m);m["legendaryId"]="LEG-MOV-001"
        with self.assertRaisesRegex(ValueError,"mismatched"):f.evaluate(m,s,b)

if __name__=="__main__":
    unittest.main(verbosity=2)
