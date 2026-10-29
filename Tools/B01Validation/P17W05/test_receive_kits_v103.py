#!/usr/bin/env python3
"""Independent immutable v1.0.3 B01-40 source and real 10-offer artifact acceptance."""
import io
import json
import os
from pathlib import Path
import unittest
from unittest.mock import patch
import zipfile
import receive_kits_v103 as r

REPO=Path(__file__).resolve().parents[3]
class V103OriginalAcceptance(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.out=Path(os.environ["B0140_W05_OUTPUT"])
        cls.raw=(cls.out/r.ARCHIVE).read_bytes()
        cls.rec=json.loads((cls.out/"qa-source-receipt.json").read_bytes())
        with zipfile.ZipFile(io.BytesIO(cls.raw)) as z:
            cls.files={name:z.read(name) for name in z.namelist()}
    def test_01_author_only_seven_exact_changed_paths(self):
        info,catalog=r.verify_source(REPO)
        self.assertEqual(set(info["author_changed_paths"]),r.EXPECTED_DIFF)
        self.assertEqual(len(info["author_changed_paths"]),7)
    def test_02_new_immutable_git_manifest_catalog_pins(self):
        info,catalog=r.verify_source(REPO)
        self.assertEqual(info["new_catalog_git_blob"],r.NEW_CATALOG_BLOB)
        self.assertEqual(info["new_manifest_git_blob"],r.NEW_MANIFEST_BLOB)
        self.assertEqual(info["new_codex_sha"],r.CODEX)
        self.assertEqual(info["source_13_sha256_and_sizes"],"PASS")
    def test_03_old_frozen_boms_prices_unchanged(self):
        old=json.loads(r.obj(REPO,r.OLD,r.DIR+"/catalog.json")[0])
        new=json.loads(r.obj(REPO,r.NEW,r.DIR+"/catalog.json")[0])
        self.assertEqual(old["kits"],new["kits"])
        self.assertEqual(len(new["kits"]),10)
        self.assertEqual(old["part_prices"],new["part_prices"])
        self.assertEqual(len(new["part_prices"]),17)
        self.assertEqual(old["weapon_prices"],new["weapon_prices"])
        self.assertEqual(len(new["weapon_prices"]),9)
    def test_04_full_raw_r3_diff_exact_one_partial(self):
        old,_=r.obj(REPO,r.PREVIOUS_PIN,r.R3)
        fresh,_=r.obj(REPO,r.CODEX,r.R3)
        self.assertEqual(old.replace(b"public sealed class R3VehicleModel",
            b"public sealed partial class R3VehicleModel",1),fresh)
        self.assertEqual(len(fresh),len(old)+8)
    def test_05_original_author_logic_unchanged(self):
        for filename in ("verify_host_source.py","kit_core.mjs","kit_core.test.mjs",
                         "kit_cli.mjs","test_stage_kit_payload.py"):
            path=r.DIR+"/"+filename
            self.assertEqual(r.obj(REPO,r.OLD,path),r.obj(REPO,r.NEW,path))
    def test_06_real_original_staged_catalog_bytes(self):
        self.assertEqual(self.files["staged-original/catalog.json"],
                         r.obj(REPO,r.NEW,r.DIR+"/catalog.json")[0])
    def test_07_replay_all_ten_actual_offer_prices_independently(self):
        c=json.loads(self.files["staged-original/catalog.json"])
        data=json.loads(self.files["staged-original/offers.json"])
        by={p["id"]:p for p in data["offers"]}
        self.assertEqual(len(by),10)
        for k in c["kits"]:
            kit=by[k["id"]]
            total=sum(p["count"]*(c["weapon_prices"][p["weapon"]] if
                      p["kind"]=="Gun" else c["part_prices"][p["kind"]])
                      for p in k["parts"])
            self.assertEqual(kit["reference_total_scrap"],total)
            self.assertEqual(kit["item_count"],sum(x["count"] for x in k["parts"]))
    def test_08_real_sha_receipt_and_archive_bytes(self):
        self.assertEqual(r.digest(self.raw),self.rec["inner_archive_sha256"])
        self.assertEqual(r.archive(self.files),self.raw)
        for name,b in self.files.items():
            self.assertEqual(r.digest(b),self.rec["archive_member_sha256"][name])
    def test_09_all_four_members_byte_original(self):
        self.assertEqual(set(self.files),{"staged-original/catalog.json",
            "staged-original/offers.json","staged-original/receipt.json",
            "source-receipt.json"})
        self.assertEqual(self.rec["actual_archive_members"],4)
        self.assertFalse(json.loads(self.files["staged-original/receipt.json"])["modified_host"])
    def test_10_original_tests_and_provenance_executed_not_assumed(self):
        self.assertEqual(self.rec["author_node_pass"],26)
        self.assertEqual(self.rec["author_python_pass"],18)
        self.assertEqual(self.rec["author_seven_negative_mutants"],7)
        self.assertEqual(self.rec["new_sha_gate_latest_codex"],"PASS")
        self.assertEqual(self.rec["new_sha_gate_old_codex"],"EXPECTED_REJECT_EXIT_2")
    def test_11_source_g0_not_native_game(self):
        self.assertEqual(self.rec["qa_G0"],"B01_40_SOURCE_ACCEPT_PASS")
        self.assertEqual(self.rec["native_G1"],"NOT_RUN")
        self.assertEqual(self.rec["HQ_G2"],"HOLD")
        self.assertEqual(self.rec["CODEX_ACK"],"PENDING")
        self.assertFalse(self.rec["CANONICAL_GAME_FILES_MUTATED"])
    def test_12_changed_immutable_manifest_pin_mutation_fails(self):
        with patch.object(r,"NEW_MANIFEST_BLOB","0"*40):
            with self.assertRaisesRegex(r.Reject,"FROZEN_NEW_CATALOG_OR_MANIFEST_CHANGED"):
                r.verify_source(REPO)
    def test_13_unapproved_catalog_mutation_fails(self):
        with patch.object(r,"NEW_CATALOG_BLOB","0"*40):
            with self.assertRaisesRegex(r.Reject,"FROZEN_NEW_CATALOG_OR_MANIFEST_CHANGED"):
                r.verify_source(REPO)
    def test_14_improper_parent_sha_fails_closed(self):
        with patch.object(r,"OLD","0"*40):
            with self.assertRaisesRegex(r.Reject,"NEW_REVISION_NOT_SINGLE_CHILD"):
                r.verify_source(REPO)
    def test_15_latest_game_sha_mutation_fails(self):
        with patch.object(r,"CODEX",r.PREVIOUS_PIN):
            with self.assertRaisesRegex(r.Reject,"NEW_MANIFEST_VERSION_LINEAGE_INVALID"):
                r.verify_source(REPO)
    def test_16_traversal_zip_path_rejected(self):
        with self.assertRaisesRegex(r.Reject,"UNSAFE_QA_ARCHIVE_FILENAME"):
            r.archive({"../no":b"data"})
    def test_17_no_outside_mutation_if_reused_target(self):
        with self.assertRaisesRegex(r.Reject,"EXTERNAL_NEW_OUTPUT_REQUIRED"):
            r.receive(REPO,self.out)
        self.assertEqual(r.digest((self.out/r.ARCHIVE).read_bytes()),
                         self.rec["inner_archive_sha256"])
    def test_18_refuse_internal_git_output(self):
        with self.assertRaisesRegex(r.Reject,"EXTERNAL_NEW_OUTPUT_REQUIRED"):
            r.receive(REPO,REPO/"Tools"/"WRONG-B0140-W05-OUTPUT")
    def test_19_source_receipt_inside_zip_matches_external(self):
        x=json.loads(self.files["source-receipt.json"])
        external=dict(self.rec)
        for p in ("inner_archive_sha256","archive_member_sha256","actual_archive_members"):
            del external[p]
        self.assertEqual(x,external)
    def test_20_all_three_real_json_docs_parse(self):
        self.assertEqual(len(json.loads(self.files["staged-original/offers.json"])["offers"]),10)
        self.assertTrue(json.loads(self.files["staged-original/receipt.json"])["file_sha256"])
        self.assertEqual(json.loads(self.files["source-receipt.json"])["source_checks"]["source_SHA"],r.NEW)

if __name__=="__main__":
    unittest.main(verbosity=2)
