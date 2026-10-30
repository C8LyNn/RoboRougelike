"""Fail-closed source regression controls for #494 AND #488.
Scanner test results do not mean transaction is fixed: expected HOLD is a passing QA test.
"""
import copy
import importlib.util
from pathlib import Path
import unittest
from unittest.mock import patch

spec=importlib.util.spec_from_file_location("gate",Path(__file__).with_name("verify.py"))
gate=importlib.util.module_from_spec(spec)
spec.loader.exec_module(gate)

def inputs():
    r2="""public bool Reattach(int index) {
     if (!ValidatePhysicalReattach(rig)) return false;
     rig.Body.linearVelocity = Vector3.zero;
     rig.Body.angularVelocity = Vector3.zero;
     rig.Body.isKinematic = true;
     rig.Part.transform.SetParent(rig.Root.transform, false);
     rig.Part.transform.localPosition = rig.LocalMountPosition;
     rig.Detached = false;
     if (!rig.Emitter.RearmAfterReattach(rig.Definition.PartId)) throw new InvalidOperationException();
     return true;
    }"""
    v2="""public bool Reattach(int index) => DockGate().Reattach(index);
    private bool CommitDock(int index) {
     rig.Body.linearVelocity=Vector3.zero;
     rig.Body.angularVelocity=Vector3.zero;
     rig.Body.isKinematic=true;
     rig.Part.transform.SetParent(rig.Root.transform,false);
     rig.Part.transform.localPosition=rig.LocalMount;
     rig.Detached=false;
     if(!rig.Emitter.RearmAfterReattach(rig.Spec.PartId)) throw new InvalidOperationException();
     return true;
    }"""
    ledger="""class DebrisImpulseLedger {
       public bool Contains(long id) => seen.Contains(id);
       public bool RearmAfterReattach(long id) => id>0 && seen.Remove(id);
       public void ClearForNewRun() => seen.Clear();
    }"""
    emitter="""public bool RearmAfterReattach(long id) => ledger.RearmAfterReattach(id);
      public void ClearForNewRun() => ledger.ClearForNewRun();"""
    return {"r2":r2,"v2":v2,"ledger":ledger,"emitter":emitter,
            "shim":"namespace B01.DebrisRepulse001","layout":"layout"}

class ScannerControls(unittest.TestCase):
    def test_01_expected_both_r2_and_v2_transaction_hold(self):
        self.assertEqual(gate.check(inputs())["data_integrity_verdict"],
                         "BOTH_CANDIDATES_TRANSACTION_HOLD")
    def test_02_six_mutations_before_rearm(self):
        self.assertEqual(gate.check(inputs())["r2_commit_order"]["mutations_before_ledger"],6)
    def test_03_v2_same_transaction_order(self):
        self.assertTrue(gate.check(inputs())["v2_commit_order"]["ledger_rearm_after_mutations"])
    def test_04_r2_missing_guard_is_schema_drift_not_safe(self):
        p=inputs();p["r2"]=p["r2"].replace("ValidatePhysicalReattach","IgnorePhysicalReattach")
        with self.assertRaises(gate.Drift):gate.check(p)
    def test_05_r2_missing_rearm_fails(self):
        p=inputs();p["r2"]=p["r2"].replace("RearmAfterReattach","ResetWithoutLedger")
        with self.assertRaises(gate.Drift):gate.check(p)
    def test_06_v2_missing_rearm_fails(self):
        p=inputs();p["v2"]=p["v2"].replace("RearmAfterReattach","ResetWithoutLedger")
        with self.assertRaises(gate.Drift):gate.check(p)
    def test_07_public_clear_removed_requires_new_review(self):
        p=inputs();p["emitter"]=p["emitter"].replace("ClearForNewRun","HiddenClear")
        with self.assertRaises(gate.Drift):gate.check(p)
    def test_08_ledger_semantics_changed_requires_review(self):
        p=inputs();p["ledger"]=p["ledger"].replace("seen.Remove(id)","false")
        with self.assertRaises(gate.Drift):gate.check(p)
    def test_09_new_atomic_precondition_no_longer_matches_old_hold(self):
        p=inputs();p["r2"]=p["r2"].replace("rig.Body.isKinematic = true;",
                              "if(!rig.Emitter.HasConsumedId(id)) return false; rig.Body.isKinematic = true;")
        # Shape scanner is NOT semantic approval: a new implementation requires manual QA.
        with self.assertRaises(gate.Drift):gate.check(p)
    def test_10_reordered_mutations_require_revalidation(self):
        p=inputs();p["r2"]=p["r2"].replace("rig.Body.angularVelocity = Vector3.zero;","")
        with self.assertRaises(gate.Drift):gate.check(p)
    def test_11_git_blob_id_changes_on_tampering(self):
        self.assertNotEqual(gate.git_blob(b"forged"),
                            gate.PIN["r2"][2])
    def test_12_git_show_failure_is_not_silent(self):
        with patch.object(gate.subprocess,"check_output",side_effect=RuntimeError("bad ref")):
            with self.assertRaises(RuntimeError):gate.pull_sources(".")
if __name__=="__main__":unittest.main()
