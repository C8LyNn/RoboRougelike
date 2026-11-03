import unittest
from unittest.mock import patch
import audit

class W33AcceptanceTests(unittest.TestCase):
 def test_01_pins(self):
  self.assertEqual(len(audit.PINS),7)
  for sha,path,git_blob in audit.PINS.values():
   self.assertEqual(len(sha),40)
   self.assertEqual(len(git_blob),40)
   self.assertEqual(path,audit.safe(path))
 def test_02_git_blob_bytes(self):
  self.assertEqual(audit.blob(b"hello\n"),"ce013625030ba8dba906f756967f9e9ca394464a")
 def test_03_unsafe_paths(self):
  for p in ("../x","/tmp/x","a\\b","bad\0x","a/../../z",""):
   with self.subTest(p=p),self.assertRaises(ValueError):audit.safe(p)
 def test_04_nested_method(self):
  x="public bool Repair(Vector3Int cell){if(true){return true;}return false;}"
  self.assertEqual(audit.method(x,"public bool Repair(Vector3Int cell)"),x)
 def test_05_no_original_method(self):
  with self.assertRaises(AssertionError):audit.method("no real method","public bool EmergencyRepair()")
 def test_06_trusted_next_win_is_required(self):
  x=";".join(("scrap=(int)((long)purchase*65L/100L)","TryReserveDestroyed(",
      "AfterWins=x.Wins","TryStageTrustedVictory(","t.AfterWins>=x.Wins+1",
      "p.Hp=1;p.ReservationId=null"))
  self.assertTrue(audit.policy_check(x))
  with self.assertRaises(AssertionError):audit.policy_check(x.replace("AfterWins=x.Wins","MISSING"))
 def test_07_original_frozen_schema_two_not_full_game(self):
  w="public const int Slots=2;s.Parts.Length!=FrozenPartCatalog.Slots;if(!SnapshotRules.Valid(snap))throw new InvalidDataException"
  c="int wheels = light ? 2 : 6;for (int y = 0; y < layers; y++)"
  a="ColdRestore.TryStage(world,out ColdRestore? staged)"
  self.assertEqual(audit.schema_check(w,c,a),{"Light":20,"Standard":83,"Heavy":139})
  with self.assertRaises(AssertionError):audit.schema_check(w.replace("Slots=2","Slots=200"),c,a)
 def test_08_bad_temporal_oracle_should_not_pass(self):
  s=("public int RepairCost(R3PartState part){purchase*65L;100L;"
     "Mathf.CeilToInt((1-part.Health)*purchase*0.35f);}"
     "public bool Repair(Vector3Int cell){part.Health=1;}"
     "public bool RepairInventory(string id){part.Health=1;}"
     "public bool EmergencyRepair(){part.Health=1;}")
  self.assertEqual(len(audit.inspect(s)),4)
  with self.assertRaises(AssertionError):audit.inspect(s.replace("part.Health=1;","pending();"))
 def test_09_paid_zero_denial_must_be_reported(self):
  s=("public int RepairCost(R3PartState part){purchase*65L;100L;"
     "Mathf.CeilToInt((1-part.Health)*purchase*0.35f);}"
     "public bool Repair(Vector3Int cell){part.Health=1;price <= 0;}"
     "public bool RepairInventory(string id){part.Health=1;}"
     "public bool EmergencyRepair(){part.Health=1;}")
  with self.assertRaises(AssertionError):audit.inspect(s)
 def test_10_real_git_bytes_cannot_change(self):
  with patch("audit.git",return_value=b"tamper"):
   with self.assertRaises(AssertionError):audit.read("r3_915")
if __name__=="__main__":unittest.main()
