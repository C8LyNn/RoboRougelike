#!/usr/bin/env python3
"""B01-40 P17 physical ZIP + genuine original Git-source independent negatives."""
import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
import zipfile
import receive_mapgen_arenas as m

REPO=Path(__file__).resolve().parents[3]
ARCHIVE="B01-40-P17-MAPGEN-ARENAS-CODEX.zip"

class SourceGeneratedArenaDelivery(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        import os
        cls.out=Path(os.environ["B01_40_P17_OUTPUT"])
        cls.zipfile=cls.out/ARCHIVE
        cls.raw=cls.zipfile.read_bytes()
        cls.receipt=json.loads((cls.out/"receiver-receipt.json").read_bytes())
        with zipfile.ZipFile(cls.zipfile) as a:
            cls.files={name:a.read(name) for name in a.namelist()}

    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(prefix="b0140_p17_verify_")
        self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name)
        self.work=self.root/"w"
        (self.work/"owner-output"/"previews").mkdir(parents=True)
        (self.work/"source-presets.json").write_bytes(self.files["presets.json"])
        for name,value in self.files.items():
            if name.startswith("blueprints/"):
                (self.work/"owner-output"/Path(name).name).write_bytes(value)
            elif name.startswith("previews/"):
                (self.work/"owner-output"/"previews"/Path(name).name).write_bytes(value)
            elif name in ("gallery.html","results.json"):
                (self.work/"owner-output"/name).write_bytes(value)

    def check(self):
        return m.check_generated(self.work)

    def test_01_actual_original_owner_and_base_git_blobs(self):
        manifest=m.verify_pinned_source(REPO)
        self.assertEqual(manifest["source_receipt"]["exported_blueprints"],18)
        self.assertEqual(manifest["original_mapgen_producer"]["immutable_sha"],m.MAPGEN_SHA)

    def test_02_actual_replay_18_original_csharp_blueprints_readback(self):
        files,r=self.check()
        self.assertEqual(len(files),len(self.files))
        self.assertEqual(files,self.files)
        self.assertEqual(r["geometry_blueprints"],18)
        self.assertEqual(r["original_csharp_plan_route_tests"],36)
        self.assertEqual(r["cover_means_by_variant"],{
            "P16-ARENA-A-OPEN":4.833,
            "P16-ARENA-B-COVER":9.333,
            "P16-ARENA-C-DENSE":14.5})

    def test_03_exact_real_portable_archive_bytes(self):
        self.assertEqual(self.raw,m.zip_payload(self.files))
        self.assertEqual(hashlib.sha256(self.raw).hexdigest(),
                         self.receipt["archive_sha256"])
        self.assertEqual(self.raw,m.zip_payload(self.files))

    def test_04_zip_has_fortyone_safe_unique_members(self):
        with zipfile.ZipFile(self.zipfile) as z:
            names=z.namelist()
            self.assertEqual(len(names),41)
            self.assertEqual(len(set(names)),41)
            self.assertTrue(all(".." not in n and not n.startswith("/") for n in names))
            self.assertEqual(len([n for n in names if n.startswith("blueprints/")]),18)
            self.assertEqual(len([n for n in names if n.startswith("previews/")]),18)
            for member in z.infolist():
                self.assertEqual(member.create_system,3)
                self.assertEqual(member.date_time,(1980,1,1,0,0,0))
                self.assertEqual(member.external_attr >> 16,0o100644)

    def test_05_source_geometry_hash_collision_negative_rejected(self):
        row=json.loads((self.work/"owner-output"/"results.json").read_bytes())
        row["generatedBlueprints"][1]["geometryHash"]=row["generatedBlueprints"][0]["geometryHash"]
        (self.work/"owner-output"/"results.json").write_bytes(m.canonical_json(row))
        with self.assertRaisesRegex(m.Reject,"BLUEPRINT_PROVENANCE_CHANGED|DUPLICATE_OR_INVALID_GEOMETRY_HASH"):
            self.check()

    def test_06_wrong_profile_negative_rejected(self):
        path=next((self.work/"owner-output").glob("P16-ARENA-*-seed-*.json"))
        data=json.loads(path.read_bytes())
        data["sourceValidation"][1]["profile"]="Light"
        path.write_bytes(m.canonical_json(data))
        with self.assertRaisesRegex(m.Reject,"ORIGINAL_ROUTE_AND_GEOMETRY_CONTRACT_CHANGED"):
            self.check()

    def test_07_missing_blueprint_negative_rejected(self):
        path=next((self.work/"owner-output").glob("P16-ARENA-*-seed-*.json"))
        path.unlink()
        with self.assertRaises((FileNotFoundError,m.Reject)):
            self.check()

    def test_08_missing_preview_negative_rejected(self):
        path=next((self.work/"owner-output"/"previews").glob("*.svg"))
        path.unlink()
        with self.assertRaisesRegex(m.Reject,"EXPECTED_18_SVG_PREVIEWS"):
            self.check()

    def test_09_incomplete_seed_set_negative_rejected(self):
        j=json.loads((self.work/"source-presets.json").read_bytes())
        j["variants"][0]["seeds"][0]=999
        (self.work/"source-presets.json").write_bytes(m.canonical_json(j))
        with self.assertRaisesRegex(m.Reject,"SOURCE_SEEDS_CHANGED"):
            self.check()

    def test_10_dangerous_zip_name_is_rejected(self):
        with self.assertRaisesRegex(m.Reject,"UNSAFE_ZIP_MEMBER"):
            m.zip_payload({"../external.txt":b"EVIL"})

    def test_11_external_outputs_cannot_be_overwritten(self):
        with self.assertRaisesRegex(m.Reject,"OUTPUT_MUST_BE_NEW_EXTERNAL_DIRECTORY"):
            m.receive(REPO,self.out)
        self.assertEqual(self.zipfile.read_bytes(),self.raw)

    def test_12_game_editor_mapdocument_must_not_be_falsely_claimed(self):
        doc=json.loads(self.files["codex-generate-calls.json"])
        self.assertEqual(doc["type"],"MAPGEN_ORIGINAL_CSHARP_DIAGNOSTIC_RECIPE_CALLS")
        self.assertEqual(len(doc["calls"]),18)
        self.assertIn("NOT MapDocument v2",doc["notes"])
        self.assertEqual(doc["unityNative"],"NOT_RUN")
        self.assertFalse(doc["integratedIntoGame"])
        self.assertEqual({x["recipe"] for x in doc["calls"]},{"CombatArena"})
        self.assertTrue(all(x["sourceMapgenSha"]==m.MAPGEN_SHA for x in doc["calls"]))

    def test_13_manifest_lists_only_canonical_payload_sha256(self):
        receipt=json.loads(self.files["receipt.json"])
        self.assertEqual(receipt["package_id"],m.PACKAGE)
        self.assertEqual(len(receipt["contents_sha256"]),40)
        for name,sha in receipt["contents_sha256"].items():
            self.assertEqual(sha,hashlib.sha256(self.files[name]).hexdigest())
        self.assertEqual(receipt["unity_native"],"NOT_RUN")
        self.assertEqual(receipt["G2"],"HOLD")

    def test_14_wrong_real_owner_git_blob_must_be_denied(self):
        from unittest.mock import patch
        path,correct=m.ORIGINAL[0]
        self.assertNotEqual(correct,"0"*40)
        with patch.object(m,"ORIGINAL",[(path,"0"*40)]+m.ORIGINAL[1:]):
            with self.assertRaisesRegex(m.Reject,"PINNED_SOURCE_DRIFT"):
                m.verify_pinned_source(REPO)

    def test_15_encoded_traversal_name_rejected_without_repacking(self):
        with self.assertRaisesRegex(m.Reject,"UNSAFE_ZIP_MEMBER"):
            m.zip_payload({"sub\\evil.json":b"NO"})

    def test_16_deterministic_canonical_json_true_not_integer(self):
        v=m.canonical_json({"a":True,"b":1.0,"z":"Łódź"})
        self.assertIn(b'true',v)
        self.assertEqual(m.canonical_json(json.loads(v)),v)

if __name__=="__main__":
    print("P17_REAL_B01_20_CSHARP_GENERATION_RECEIVER; UNITY_NOT_RUN",flush=True)
    unittest.main(verbosity=2)
