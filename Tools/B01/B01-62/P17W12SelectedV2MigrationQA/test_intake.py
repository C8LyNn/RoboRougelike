"""B01-62 independent adversarial migration/save QA controls. No Unity/real saves."""
import copy,json,tempfile,unittest
from pathlib import Path
import intake as q

class MigrationTests(unittest.TestCase):
 def setUp(self):self.x=q.clean_fixture()
 def hold(self):
  with self.assertRaises(q.Hold):q.preflight(self.x)
 def test_01_clean(self):self.assertEqual(q.preflight(self.x),"QA_FIXTURE_ONLY")
 def test_02_light_detached(self):self.x["rigs"][0]["detached"]=True;self.hold()
 def test_03_heavy_detached(self):self.x["rigs"][1]["detached"]=True;self.hold()
 def test_04_light_consumed(self):self.x["rigs"][0]["consumed"]=True;self.hold()
 def test_05_heavy_consumed(self):self.x["rigs"][1]["consumed"]=True;self.hold()
 def test_06_w07_legacy(self):self.x["components"]=["B01.DebrisPlayground001.DebrisRigPlayground"];self.hold()
 def test_07_w09_overlay(self):self.x["components"]=[q.COMPONENT,"HardenedDockDemo"];self.hold()
 def test_08_w08_gate(self):self.x["components"]=[q.COMPONENT,"DebrisSafeReattachGate"];self.hold()
 def test_09_alternative_511_sha(self):self.x["source_sha"]="baf65f2664e38f3b9c38815c3f07285f8107c1b6";self.hold()
 def test_10_unknown_schema(self):self.x["schema"]="game-save";self.hold()
 def test_11_duplicate_part_id(self):self.x["rigs"][1]["part_id"]=q.IDS[0];self.hold()
 def test_12_reordered_ids(self):self.x["rigs"].reverse();self.hold()
 def test_13_boolean_id(self):self.x["rigs"][0]["part_id"]=True;self.hold()
 def test_14_numeric_detached(self):self.x["rigs"][0]["detached"]=0;self.hold()
 def test_15_unknown_field(self):self.x["currency"]=100;self.hold()
 def test_16_unknown_rig_field(self):self.x["rigs"][0]["hp"]=1;self.hold()
 def test_17_missing_rig(self):self.x["rigs"].pop();self.hold()
 def test_18_duplicate_component(self):self.x["components"].append(q.COMPONENT);self.hold()
 def test_19_deterministic_fixture(self):self.assertEqual(q.data_json(self.x),q.data_json(copy.deepcopy(self.x)))
 def test_20_clean_stage_and_rollback(self):
  with tempfile.TemporaryDirectory() as t:
   base=Path(t);(base/"foreign").write_text("mine")
   target=base/"stage";hash_=q.stage(self.x,target,base/"repo")
   self.assertEqual(len(hash_),64)
   q.rollback(target,base/"repo")
   self.assertFalse(target.exists());self.assertEqual((base/"foreign").read_text(),"mine")
 def test_21_preexisting_stage_refused(self):
  with tempfile.TemporaryDirectory() as t:
   base=Path(t);target=base/"stage";target.mkdir()
   with self.assertRaises(q.Hold):q.stage(self.x,target,base/"repo")
 def test_22_repo_stage_refused(self):
  with tempfile.TemporaryDirectory() as t:
   base=Path(t);(base/"repo").mkdir()
   with self.assertRaises(q.Hold):q.stage(self.x,base/"repo"/"stage",base/"repo")
 def test_23_changed_snapshot_rollback_refused(self):
  with tempfile.TemporaryDirectory() as t:
   base=Path(t);target=base/"stage";q.stage(self.x,target,base/"repo")
   (target/"snapshot.json").write_text("{}")
   with self.assertRaises(q.Hold):q.rollback(target,base/"repo")
   self.assertTrue(target.exists())
 def test_24_foreign_file_rollback_refused(self):
  with tempfile.TemporaryDirectory() as t:
   base=Path(t);target=base/"stage";q.stage(self.x,target,base/"repo")
   (target/"foreign").write_text("don't delete")
   with self.assertRaises(q.Hold):q.rollback(target,base/"repo")
 def test_25_missing_receipt_refused(self):
  with tempfile.TemporaryDirectory() as t:
   base=Path(t);target=base/"stage";q.stage(self.x,target,base/"repo")
   (target/"receipt.json").unlink()
   with self.assertRaises(q.Hold):q.rollback(target,base/"repo")
 def test_26_receipt_forgery_refused(self):
  with tempfile.TemporaryDirectory() as t:
   base=Path(t);target=base/"stage";q.stage(self.x,target,base/"repo")
   data=json.loads((target/"receipt.json").read_text());data["fixture_sha256"]="0"*64
   (target/"receipt.json").write_bytes(q.data_json(data))
   with self.assertRaises(q.Hold):q.rollback(target,base/"repo")
 def test_27_symlink_refused(self):
  with tempfile.TemporaryDirectory() as t:
   base=Path(t);target=base/"stage";q.stage(self.x,target,base/"repo")
   (target/"foreignlink").symlink_to(base/"foreign")
   with self.assertRaises(q.Hold):q.rollback(target,base/"repo")
 def test_28_git_blob_tamper(self):self.assertNotEqual(q.blob(b"tampered"),q.PINS["ledger"][1])
 def test_29_source_route_mutation(self):
  s=self.source();s["host"]=s["host"].replace("DockGate().Reattach(index)","Unsafe(index)")
  with self.assertRaises(q.Hold):q.source_gate(s)
 def test_30_source_compensation_removed(self):
  s=self.source();s["ledger"]=s["ledger"].replace("consumed.TryDetach(","bad(")
  with self.assertRaises(q.Hold):q.source_gate(s)
 def test_31_legacy_scene_mutation(self):
  s=self.source();s["scene"]+=" controller.AddComponent<DebrisRigPlayground>();"
  with self.assertRaises(q.Hold):q.source_gate(s)
 def test_32_original_source_no_save_contract(self):
  result=q.source_gate(self.source())
  self.assertEqual(result["save_load_acceptance"],"HOLD_NO_PERSISTENT_SAVE_CONTRACT")
  self.assertEqual(result["native_unity"],"NOT_RUN")
 def test_33_source_scene_builder_mutation(self):
  s=self.source();s["scene"]=s["scene"].replace("NewSceneSetup.EmptyScene","NewSceneSetup.DefaultGameObjects")
  with self.assertRaises(q.Hold):q.source_gate(s)
 def test_34_source_id_mutation(self):
  s=self.source();s["spec"]=s["spec"].replace("2310002","2340002")
  with self.assertRaises(q.Hold):q.source_gate(s)
 def source(self):
  return {
   "host":"public bool Reattach(int index) => DockGate().Reattach(index); public bool TrySafeReattach(int index) => DockGate().TrySafeReattach(index); public bool ReattachFromGui(int index) => DockGate().ReattachFromGui(index); rig.Ledger.TryReattach(rig.Spec.PartId,; Physics.OverlapBoxNonAlloc( ; Physics.ComputePenetration(",
   "ledger":"private readonly DebrisImpulseLedger consumed; consumed.RearmAfterReattach(id); applyPhysical(); rollbackPhysical(); consumed.TryDetach(",
   "policy":"DockResult.TransactionRejected",
   "scene":"NewScene(NewSceneSetup.EmptyScene; controller.AddComponent<SecureDebrisPlaygroundV2>();",
   "spec":"new SecureRigSpec(2310001, ...);new SecureRigSpec(2310002, ...);"
  }
if __name__=="__main__":unittest.main()
