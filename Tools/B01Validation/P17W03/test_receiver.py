import copy
import io
import json
import os
from pathlib import Path
import unittest
from unittest.mock import patch
import zipfile
import receive_formations as r

REPO=Path(__file__).resolve().parents[3]
class RealIntakeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        root=Path(os.environ["B0140_W03_OUTPUT"])
        cls.root=root
        cls.binary=(root/r.ARCHIVE).read_bytes()
        cls.files=r.load_zip(cls.binary)
        cls.receipt=json.loads((root/"receiver-receipt.json").read_bytes())
        cls.queue=json.loads((root/"codex-queue.json").read_bytes())
        cls.forms=json.loads(cls.files["source/formations.json"])
        cls.calls=json.loads(cls.files["binding/optional-spawn-arguments.json"])["calls"]
    def test_01_original_git_bytes_and_runtime(self):
        raw,forms,checks,scope=r.owner_receipt(REPO)
        self.assertEqual(raw,self.files["source/formations.json"])
        self.assertEqual(len(checks),4)
        self.assertTrue(all(c["compatible"] for c in checks))
        self.assertEqual(len(scope),6)
    def test_02_actual_12_original_vector_calls(self):
        self.assertEqual(len(self.calls),12)
        self.assertEqual(sum(len(c["world_xz"]) for c in self.calls),30)
        for row in self.forms["formations"]:
            for n in range(1,5):
                c=next(x for x in self.calls if x["formation_id"]==row["id"] and x["spawn_count"]==n)
                self.assertEqual(c["world_xz"],row["options"][str(n)])
                self.assertEqual(c["vector3_argument"].count("new Vector3("),n)
    def test_03_original_binary_stable(self):
        self.assertEqual(r.sha(self.binary),self.receipt["archive_sha256"])
        self.assertEqual(r.payload_zip(self.files),self.binary)
    def test_04_six_exact_files_and_safe_metadata(self):
        self.assertEqual(len(self.files),6)
        self.assertEqual(len([n for n in self.files if n.endswith(".svg")]),3)
        with zipfile.ZipFile(io.BytesIO(self.binary)) as z:
            for item in z.infolist():
                self.assertEqual(item.date_time,(1980,1,1,0,0,0))
                self.assertEqual(item.external_attr>>16,0o100644)
    def test_05_three_original_producer_svg(self):
        for name,raw in self.files.items():
            if name.endswith(".svg"):
                self.assertIn(b"<svg",raw)
                self.assertIn(b"enemies",raw)
                self.assertGreater(raw.count(b"<circle"),4)
    def test_06_original_test_and_source_provenance(self):
        self.assertEqual(self.receipt["owner_node_tests"],{"pass":35,"fail":0})
        self.assertEqual(self.receipt["source_manifest_git_blob"],r.ORIG_MANIFEST)
        self.assertEqual(self.receipt["original_content_git_blob"],r.ORIG_DATA)
        self.assertEqual(len(self.receipt["receipt_members_sha256"]),5)
        for p,d in self.receipt["receipt_members_sha256"].items():
            self.assertEqual(r.sha(self.files[p]),d)
    def test_07_source_gate_is_not_unity_gate(self):
        self.assertEqual(self.receipt["G0"],"B01_40_SOURCE_G0_PASS")
        self.assertEqual(self.receipt["G1"],"NOT_RUN")
        self.assertEqual(self.receipt["G2"],"HOLD")
        self.assertEqual(self.receipt["codex_ack"],"PENDING")
        self.assertFalse(self.receipt["game_integrated"])
    def test_08_equipment_priority_hold_is_explicit(self):
        self.assertEqual([x["pr"] for x in self.queue["ordered"]],[355,333])
        self.assertEqual(self.queue["ordered"][1]["status"],"HOLD_CODEX_R3_BYTE_DRIFT")
        self.assertNotEqual(self.receipt["equipment_priority_two"]["pinned_original_blob"],
                            self.receipt["equipment_priority_two"]["current_codex_blob"])
    def test_09_manifest_sha_mutant_rejects(self):
        with patch.object(r,"ORIG_MANIFEST","0"*40):
            with self.assertRaisesRegex(r.Reject,"OWNER_MANIFEST_OR_CONTENT_DRIFT"):
                r.owner_receipt(REPO)
    def test_10_new_codex_contract_mutant_rejects(self):
        with patch.object(r,"LATEST",r.HOLD_EQUIP):
            with self.assertRaisesRegex(r.Reject,"LATEST_CODEX_RUNTIME_CONTRACT_CHANGED"):
                r.owner_receipt(REPO)
    def test_11_missing_count_rejects(self):
        c=copy.deepcopy(self.forms)
        c["formations"][0]["options"].pop("4")
        with self.assertRaisesRegex(r.Reject,"SOURCE_SPAWNCOUNT_MISSING"):
            r.check_content(c,r.canonical(c))
    def test_12_duplicate_arena_rejects(self):
        c=copy.deepcopy(self.forms)
        c["formations"][1]["arena"]=0
        with self.assertRaisesRegex(r.Reject,"DUPLICATE_ARENA"):
            r.check_content(c,r.canonical(c))
    def test_13_invalid_coordinate_rejects(self):
        c=copy.deepcopy(self.forms)
        c["formations"][0]["options"]["1"][0][0]=float("nan")
        with self.assertRaisesRegex(r.Reject,"ORIGINAL_POSITION_INVALID"):
            r.check_content(c,b"test fixture")
    def test_14_zip_traversal_rejects(self):
        with self.assertRaisesRegex(r.Reject,"ZIP_TRAVERSAL_PATH"):
            r.payload_zip({"../unsafe":b"bad"})
    def test_15_incomplete_original_zip_rejects(self):
        with self.assertRaisesRegex(r.Reject,"ZIP_ENTRY_SET_INVALID"):
            r.load_zip(r.payload_zip({"one":b"short"}))
    def test_16_existing_archive_no_clobber(self):
        with self.assertRaisesRegex(r.Reject,"NEW_EXTERNAL_OUTPUT_REQUIRED"):
            r.produce(REPO,self.root)
        self.assertEqual((self.root/r.ARCHIVE).read_bytes(),self.binary)
    def test_17_inside_repo_output_rejects(self):
        with self.assertRaisesRegex(r.Reject,"NEW_EXTERNAL_OUTPUT_REQUIRED"):
            r.produce(REPO,REPO/"Tools"/"UNSAFE_OUT")
    def test_18_accurate_latest_sha(self):
        self.assertEqual(self.receipt["latest_codex_gameplay_sha"],r.LATEST)
        self.assertEqual(self.receipt["latest_codex_report_sha"],r.REPORT)

if __name__=="__main__":
    unittest.main(verbosity=2)
