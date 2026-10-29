#!/usr/bin/env python3
"""B01-40 W04 independent real original bytes/source QA, cannot waive owner SHA."""
import io
import json
import os
from pathlib import Path
import unittest
from unittest.mock import patch
import zipfile
import receive_kit_compat as r

REPO=Path(__file__).resolve().parents[3]
class OriginalKitReceivingTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.out=Path(os.environ["B0140_W04_OUTPUT"])
        cls.binary=(cls.out/r.ZIPNAME).read_bytes()
        cls.record=json.loads((cls.out/"compatibility-receipt.json").read_bytes())
        with zipfile.ZipFile(io.BytesIO(cls.binary)) as z:
            cls.files={n:z.read(n) for n in z.namelist()}
    def test_01_all_13_real_original_payloads_and_immutable_source_scope(self):
        s,cat,r3,r4=r.validate_source(REPO)
        self.assertTrue(s["13_payloads_sha256_verified"])
        self.assertEqual(len(s["original_scope_paths"]),15)
        self.assertEqual(len(cat["kits"]),10)
        self.assertGreater(len(r3),30000)  # Git bytes include UTF-8 and CRLF; exact +8 assertion below
    def test_02_real_archived_original_staged_files(self):
        self.assertEqual(set(self.files),{
            "staged/catalog.json","staged/offers.json","staged/receipt.json",
            "receiver-compatibility-receipt.json"})
        self.assertEqual(len(json.loads(self.files["staged/offers.json"])["offers"]),10)
        self.assertEqual(json.loads(self.files["staged/receipt.json"])["modified_host"],False)
    def test_03_sha_stable_four_member_zip(self):
        self.assertEqual(r.digest(self.binary),self.record["qa_only_zip_sha256"])
        self.assertEqual(r.newzip(self.files),self.binary)
        self.assertEqual(len(self.files),4)
    def test_04_unity_native_not_run_and_author_pin_gate_not_bypassed(self):
        self.assertEqual(self.record["native_unity"],"NOT_RUN")
        self.assertEqual(self.record["G2"],"HOLD")
        self.assertEqual(self.record["codex_ack"],"PENDING")
        self.assertFalse(self.record["direct_game_import_authorized"])
        self.assertEqual(self.record["author_pin_gate_current_exit"],2)
        self.assertEqual(self.record["producer_frozen_sha_gate"],"REMAINS_FAIL_CLOSED")
    def test_05_real_producer_tests_executed(self):
        self.assertEqual(self.record["author_original_node_pass"],26)
        self.assertEqual(self.record["author_original_python_pass"],18)
        self.assertEqual(self.record["author_source_selftest_negatives"],7)
        self.assertEqual(self.record["current_r3_r4_author_semantic_price_enum_check"],
                         "PASS_10_OF_10")
    def test_06_old_r3_and_new_r3_differ_exactly_partial(self):
        old,b_old=r.blob(REPO,r.PINNED,r.R3)
        new,b_new=r.blob(REPO,r.GAME,r.R3)
        self.assertEqual((b_old,b_new),(r.R3_OLD_BLOB,r.R3_NEW_BLOB))
        self.assertEqual(old.count(r.OLD),1)
        self.assertEqual(old.replace(r.OLD,r.NEW,1),new)
    def test_07_orig_r4_and_new_r4_byte_same(self):
        self.assertEqual(r.blob(REPO,r.PINNED,r.R4),r.blob(REPO,r.GAME,r.R4))
    def test_08_manifest_sha_mutation_rejects(self):
        with patch.object(r,"MANIFEST_BLOB","0"*40):
            with self.assertRaisesRegex(r.Reject,"ORIGINAL_MANIFEST_GIT_SHA_CHANGED"):
                r.validate_source(REPO)
    def test_09_codex_r3_unexpected_pin_mutation_rejects(self):
        with patch.object(r,"R3_NEW_BLOB","0"*40):
            with self.assertRaisesRegex(r.Reject,"EXPECTED_EXACT_CODEX_R3_SHA_DRIFT_CHANGED"):
                r.validate_source(REPO)
    def test_10_changed_class_source_location_rejects(self):
        with patch.object(r,"NEW",b"public sealed partial class R3FakeModel"):
            with self.assertRaisesRegex(r.Reject,"UNAPPROVED_R3_DIFFERENCE"):
                r.validate_source(REPO)
    def test_11_original_byte_hash_matches_all_receipted_payloads(self):
        for name,v in self.files.items():
            self.assertEqual(r.digest(v),self.record["payload_sha256"][name])
    def test_12_traversal_archive_path_rejects(self):
        with self.assertRaisesRegex(r.Reject,"UNSAFE_ZIP_MEMBER"):
            r.newzip({"../escape":b"invalid"})
    def test_13_no_overwrite_test_archive_output(self):
        with self.assertRaisesRegex(r.Reject,"OUTPUT_MUST_BE_NEW_OUTSIDE_GIT"):
            r.receive(REPO,self.out)
        self.assertEqual(r.digest((self.out/r.ZIPNAME).read_bytes()),
                         self.record["qa_only_zip_sha256"])
    def test_14_no_canonical_game_repo_output(self):
        with self.assertRaisesRegex(r.Reject,"OUTPUT_MUST_BE_NEW_OUTSIDE_GIT"):
            r.receive(REPO,REPO/"Assets"/"B0140_unsafe_out")
    def test_15_no_producer_overwrites(self):
        self.assertTrue(all(x.startswith(r.FOLDER+"/") or
                        x==".github/workflows/b01-22-p17-equipment-kits.yml"
                        for x in self.record["source_checks"]["original_scope_paths"]))
        self.assertNotIn("Assets/",str(self.record["source_checks"]["original_scope_paths"]))
    def test_16_new_codex_price_semantics_gate_was_executed(self):
        self.assertEqual(self.record["kits"],10)
        self.assertEqual(self.record["added_independent_current_price_negative"],"REJECTED")
        self.assertEqual(self.record["source_checks"]["r3_only_diff"],
                         "EXACT_ADD_8_BYTES_PARTIAL_KEYWORD")
    def test_17_original_gate_still_failed_intentionally(self):
        self.assertEqual(self.record["author_pin_gate_current_reason"],
                         "LATEST_CODEX_BLOB_DRIFT")
        self.assertIn("REPIN_IN_NEW_IMMUTABLE_VERSION",self.record["recommendation"])
if __name__=="__main__":
    unittest.main(verbosity=2)
