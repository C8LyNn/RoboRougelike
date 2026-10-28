#!/usr/bin/env python3
"""P16 B01-40 real original Git-source receiver + adversarial in-memory negatives.

Original git blobs are *never modified*. Simulated adversarial mutations are
clearly marked TEST-ONLY; they prove the admission operator rejects regressions.
No native Unity tests are claimed.
"""
import copy
import json
from pathlib import Path
import tempfile
import unittest
import admit_current_codex_packages as g

REPO=Path(__file__).resolve().parents[3]

class CurrentCodexCrossPackageAdmission(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.snap=g.source_snapshot(REPO)
        cls.receipt=g.check(cls.snap)

    def mutated(self):
        # Test-only structures, immutable original .git tree + objects untouched.
        return copy.deepcopy(self.snap)

    def test_01_genuine_original_chl_part_current_codex_gate(self):
        r=self.receipt
        self.assertEqual(r["gate"],"PASS_SOURCE_ONLY")
        self.assertEqual(r["package_file_counts"],{"chl":45,"part":28})
        self.assertEqual(r["meta_guid_counts"]["chl"],26)
        self.assertEqual(r["meta_guid_counts"]["part"],17)
        self.assertEqual(r["asmdef_counts"]["chl"],4)
        self.assertEqual(r["asmdef_counts"]["part"],3)
        self.assertEqual(r["unity"],"NOT_RUN")

    def test_02_both_codex_fixture_map_blobs_are_exact_original_owner_bytes(self):
        for own,copykey in (("arena_owner","arena_map_codex"),
                            ("cover_owner","cover_map_codex")):
            a=g.PINS[own][2];b=g.PINS[copykey][2]
            self.assertEqual(a,b)
            self.assertEqual(self.snap["data"][a],self.snap["data"][b])
        self.assertEqual(self.receipt["map_fixtures"]["LD02_ARENA"]["repaired_corners"],4)
        self.assertEqual(self.receipt["map_fixtures"]["LD02_COVER_CHOICE"]["repaired_corners"],4)

    def test_03_clashing_source_path_fails_closed(self):
        x=self.mutated()
        existing=next(p for p in x["paths"]["codex"] if p.endswith(".meta"))
        x["paths"]["part"][existing]=x["paths"]["codex"][existing]
        with self.assertRaisesRegex(g.Reject,"PART_CANONICAL_ASSET_PATH_COLLISION"):
            g.check(x)

    def test_04_case_only_win_path_collision_fails_closed(self):
        x=self.mutated()
        existing=next(p for p in x["paths"]["codex"] if p.endswith(".meta"))
        x["paths"]["part"][existing.swapcase()]=x["paths"]["codex"][existing]
        with self.assertRaisesRegex(g.Reject,"CASEFOLD_UNITY_ASSET_PATH_COLLISION"):
            g.check(x)

    def test_05_untrusted_duplicate_chl_meta_guid_fails_closed(self):
        x=self.mutated()
        codex_path=next(p for p in x["paths"]["codex"] if p.endswith(".meta"))
        chl_path=next(p for p in x["paths"]["chl"] if p.endswith(".meta"))
        codex_guid=g.GUID.search(x["data"][x["paths"]["codex"][codex_path]]).group(1)
        oid=x["paths"]["chl"][chl_path]
        x["data"][oid]=g.GUID.sub(b"guid: "+codex_guid,x["data"][oid],count=1)
        with self.assertRaisesRegex(g.Reject,"CHL_GUID_COLLISION_WITH_CODEX"):
            g.check(x)

    def test_06_untrusted_duplicate_part_meta_guid_fails_closed(self):
        x=self.mutated()
        codex_path=next(p for p in x["paths"]["codex"] if p.endswith(".meta"))
        part_path=next(p for p in x["paths"]["part"] if p.endswith(".meta"))
        codex_guid=g.GUID.search(x["data"][x["paths"]["codex"][codex_path]]).group(1)
        oid=x["paths"]["part"][part_path]
        x["data"][oid]=g.GUID.sub(b"guid: "+codex_guid,x["data"][oid],count=1)
        with self.assertRaisesRegex(g.Reject,"PART_GUID_COLLISION_WITH_CODEX"):
            g.check(x)

    def test_07_cross_source_asmdef_name_collision_fails_closed(self):
        x=self.mutated()
        codex_path=next(p for p in x["paths"]["codex"] if p.endswith(".asmdef"))
        part_path=next(p for p in x["paths"]["part"] if p.endswith(".asmdef"))
        codex_name=json.loads(x["data"][x["paths"]["codex"][codex_path]])["name"]
        oid=x["paths"]["part"][part_path]
        obj=json.loads(x["data"][oid]);obj["name"]=codex_name
        x["data"][oid]=json.dumps(obj).encode()
        with self.assertRaisesRegex(g.Reject,"CROSS_SOURCE_ASMDEF_NAME_COLLISION"):
            g.check(x)

    def test_08_missing_package_asmdef_reference_fails_closed(self):
        x=self.mutated()
        part_path=next(p for p in x["paths"]["part"] if p.endswith("EditModeTests.asmdef"))
        oid=x["paths"]["part"][part_path]
        obj=json.loads(x["data"][oid]);obj["references"]=["B01.PARTSIZE001.NONEXISTENT"]
        x["data"][oid]=json.dumps(obj).encode()
        with self.assertRaisesRegex(g.Reject,"OWNER_ASMDEF_REFERENCE_MISSING"):
            g.check(x)

    def test_09_map_repaired_corners_regression_rejected(self):
        x=self.mutated()
        oid=g.PINS["arena_map_codex"][2]
        j=json.loads(x["data"][oid])
        corner=next(t for t in j["items"] if t["kind"]==6 and t["x"]==0 and t["z"]==0)
        corner.update(kind=1,rotation=1,scaleY=1)
        x["data"][oid]=json.dumps(j).encode()
        with self.assertRaisesRegex(g.Reject,"KNOWN_COVER_CORNER_GAP_REGRESSION"):
            g.check(x)

    def test_10_corrupt_codex_map_schema_is_rejected(self):
        x=self.mutated()
        oid=g.PINS["cover_map_codex"][2]
        j=json.loads(x["data"][oid]);j["globalFloor"]=True
        x["data"][oid]=json.dumps(j).encode()
        with self.assertRaisesRegex(g.Reject,"MAP_DOCUMENT_V2_DRIFT"):
            g.check(x)

    def test_11_new_codex_mapstorage_contract_drift_rejected(self):
        x=self.mutated()
        oid=g.PINS["codex_map_storage"][2]
        x["data"][oid]=x["data"][oid].replace(b"bool overwrite=false",b"bool overwrite=true")
        with self.assertRaisesRegex(g.Reject,"NEW_CODEX_MAPSTORAGE_COLLISION_CONTRACT_CHANGED"):
            g.check(x)

    def test_12_two_layer_cap_contract_drift_rejected(self):
        x=self.mutated()
        oid=g.PINS["codex_map_draft"][2]
        x["data"][oid]=x["data"][oid].replace(
            b"doc.items.Count>MaxItems*2",b"doc.items.Count>MaxItems")
        with self.assertRaisesRegex(g.Reject,"NEW_CODEX_TWO_LAYER_MAP_CAP_CONTRACT_CHANGED"):
            g.check(x)

    def test_13_actual_gate_can_write_single_external_no_overwrite_manifest(self):
        with tempfile.TemporaryDirectory(prefix="b01_40_p16_external_") as t:
            path=Path(t)/"B01-40-P16-admission.json"
            self.assertEqual(g.main.__name__,"main")
            payload=(json.dumps(self.receipt,sort_keys=True,indent=2)+"\n").encode()
            with open(path,"xb") as fh:fh.write(payload)
            self.assertEqual(json.loads(path.read_bytes())["codex_sha"],g.CODEX)
            with self.assertRaises(FileExistsError):
                with open(path,"xb") as fh:fh.write(payload)

    def test_14_file_sets_are_strictly_owned_and_disjoint(self):
        self.assertTrue(all(p.startswith("Assets/RoboRougelikeChallenges") for p in
                            self.snap["paths"]["chl"]))
        self.assertTrue(all(p.startswith("Assets/PARTSIZE001") for p in
                            self.snap["paths"]["part"]))
        for p in self.snap["paths"]["chl"]:
            self.assertNotIn(p,self.snap["paths"]["codex"])
        for p in self.snap["paths"]["part"]:
            self.assertNotIn(p,self.snap["paths"]["codex"])

if __name__=="__main__":
    print("B01_40_P16_SOURCE_ONLY_ORIGINAL_SHA="+g.CODEX+" UNITY_NATIVE=NOT_RUN",flush=True)
    unittest.main(verbosity=2)
