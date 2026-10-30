#!/usr/bin/env python3
"""Exact-original W06->W10 V2 data-integrity source mutation controls."""
import copy
import json
import os
import unittest
from gate import GateError
from v2_transaction_gate import PINS, source, examine

class V2TransactionQA(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.files=source(os.environ.get("B01_REPO","."))
 def change(self,key,old,new):
  out=dict(self.files)
  self.assertEqual(out[key].count(old),1,"mutation must match unique original producer string")
  out[key]=out[key].replace(old,new)
  return out
 def rejects(self,key,old,new):
  with self.assertRaises(GateError):examine(self.change(key,old,new))
 def test_01_exact_five_original_gits(self):
  self.assertEqual(len(PINS),5)
 def test_02_v2_all_public_aliases_reach_one_inspector(self):
  self.assertEqual(examine(self.files)["source_gate"],"V2_ALL_PUBLIC_ALIASES_INSPECT_FIRST")
 def test_03_missing_ledger_id_throws_after_six_state_writes(self):
  r=examine(self.files)
  self.assertEqual(r["physical_mutations_before_missing_ledger_detection"],6)
  self.assertEqual(r["data_integrity_gate"],"HOLD_COMMIT_STATE_MUTATED_BEFORE_LEDGER_REARM_PRECONDITION")
 def test_04_source_receipt_is_stable_and_native_not_claimed(self):
  a=examine(self.files);b=examine(self.files)
  self.assertEqual(json.dumps(a,sort_keys=True),json.dumps(b,sort_keys=True))
  self.assertEqual(a["native_physx"],"NOT_RUN")
  self.assertEqual(a["real_save_reload"],"NOT_RUN")
 def test_05_physical_parent_mutation_missing_rejected(self):
  self.rejects("controller","rig.Part.transform.SetParent(rig.Root.transform,false);",
          "rig.Part.transform.SetParent(null,true);")
 def test_06_id_rearm_before_body_mutation_needs_new_review(self):
  self.rejects("controller",
   "rig.Detached=false;\n            if(!rig.Emitter.RearmAfterReattach(rig.Spec.PartId))",
   "if(!rig.Emitter.RearmAfterReattach(rig.Spec.PartId))\n            rig.Detached=false;")
 def test_07_deleting_old_public_alias_detected(self):
  self.rejects("controller","public bool Reattach(int index) => DockGate().Reattach(index);",
                         "public bool Reattach(int index) => CommitDock(index);")
 def test_08_route_directly_to_commit_detected(self):
  self.rejects("policy","public bool ReattachFromGui(int index) => Execute(index);",
                      "public bool ReattachFromGui(int index) => commit(index);")
 def test_09_reverse_inspect_decision_detected(self):
  self.rejects("policy","return LastResult == DockResult.Allowed && commit(index);",
                      "return LastResult != DockResult.Allowed && commit(index);")
 def test_10_missing_w06_id_removal_detected(self):
  self.rejects("ledger","public bool RearmAfterReattach(long id) => id > 0 && seen.Remove(id);",
                      "public bool RearmAfterReattach(long id) => id > 0;")
 def test_11_original_public_clear_for_new_run_preserved(self):
  self.assertTrue(examine(self.files)["w06_public_ledger_reset_available"])
 def test_12_w10_cannot_rewrite_frozen_w06_ledger_without_pins(self):
  before=copy.deepcopy(self.files)
  examine(self.files)
  self.assertEqual(self.files,before)
 def test_13_changed_scene_controller_detected(self):
  self.rejects("scene","controller.AddComponent<SecureDebrisPlaygroundV2>();",
                       "controller.AddComponent<DebrisRigPlayground>();")
 def test_14_missing_original_clear_in_emitter_detected(self):
  self.rejects("emitter","public void ClearForNewRun() => ledger.ClearForNewRun();",
                         "public void ClearForNewRun() => throw new System.Exception();")
if __name__=="__main__":unittest.main(verbosity=2)
