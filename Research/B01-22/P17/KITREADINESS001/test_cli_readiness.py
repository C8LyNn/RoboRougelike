"""Original read-only user-input C#9 CLI black-box tests, Windows + Ubuntu."""
import copy
import json
import pathlib
import subprocess
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[4]
SRC = ROOT / "Research/B01-22/P17/KITREADINESS001"
DLL = SRC / "bin/Release/net8.0/ReadinessCli.dll"
EXAMPLE = SRC / "scenarios/user-request-example.json"


class UserSuppliedReadinessCliTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if not DLL.is_file():
            raise AssertionError("ACTUAL_CSHARP_CLI_NOT_COMPILED")
        cls.sample_bytes = EXAMPLE.read_bytes()
        cls.sample = json.loads(cls.sample_bytes)

    def run_case(self, payload, *, existing=False, raw=None):
        with tempfile.TemporaryDirectory() as temp:
            folder = pathlib.Path(temp)
            input_path = folder / "requested.json"
            output_path = folder / "new-preview.json"
            src = (json.dumps(payload, sort_keys=True)+"\n").encode("utf-8") if raw is None else raw
            input_path.write_bytes(src)
            if existing:
                output_path.write_bytes(b"KEEP_EXISTING_FILE")
            cp = subprocess.run(
                ["dotnet", str(DLL), "--input", str(input_path),
                 "--out", str(output_path)], cwd=ROOT, capture_output=True, text=True)
            data = output_path.read_bytes() if output_path.is_file() else None
            unchanged = input_path.read_bytes() == src
            return cp.returncode, cp.stdout, cp.stderr, data, unchanged

    def assert_rejected(self, payload, raw=None):
        code, stdout, stderr, output, untouched = self.run_case(payload, raw=raw)
        self.assertEqual(code,2,stdout+stderr)
        self.assertIsNone(output)
        self.assertTrue(untouched)

    def test_01_real_user_json_is_actually_simulated(self):
        code, stdout, stderr, data, untouched = self.run_case(self.sample)
        self.assertEqual((code,untouched),(0,True),stderr)
        self.assertIn("KITREADINESS_USER_PREVIEW_PASS",stdout)
        obj = json.loads(data)
        self.assertEqual((obj["result"]["ReadyCount"],obj["result"]["BlockedCount"]),(1,1))
        self.assertEqual(obj["request_count"],2)
        self.assertEqual([r["Ready"] for r in obj["result"]["Requests"]],[True,False])
        self.assertEqual(obj["result"]["Requests"][1]["MissingPieces"],5)
        self.assertTrue(obj["reference_only"])
        self.assertFalse(obj["modifies_game"])
        self.assertNotIn(b"\r",data)

    def test_02_same_source_and_custom_stock_deterministic_bytes(self):
        first = self.run_case(self.sample)[3]
        second = self.run_case(self.sample)[3]
        self.assertEqual(first,second)

    def test_03_request_order_changes_virtual_reservations_only(self):
        payload = copy.deepcopy(self.sample)
        payload["requests"]=["EK08","EK01"]
        payload["owned_parts"] += [
            {"kind":"CrawlerTrack","count":2},
            {"kind":"Gun","weapon":"RocketLauncher","count":1},
            {"kind":"Gun","weapon":"Heavy","count":1},
            {"kind":"DirectionalShield","count":1}
        ]
        result = json.loads(self.run_case(payload)[3])
        self.assertEqual([x["Ready"] for x in result["result"]["Requests"]],[True,False])
        self.assertEqual(result["result"]["Requests"][1]["ReferenceMissingScrap"],30)

    def test_04_zero_requests_valid_no_stock_mutation(self):
        payload=copy.deepcopy(self.sample);payload["requests"]=[]
        code,_,_,data,_=self.run_case(payload)
        obj=json.loads(data)
        self.assertEqual(code,0)
        self.assertEqual((obj["result"]["ReadyCount"],obj["result"]["BlockedCount"]),(0,0))
        self.assertEqual(sum(x["Quantity"] for x in obj["result"]["RemainingVirtualStock"]),5)

    def test_05_forged_catalog_sha_rejected(self):
        payload=copy.deepcopy(self.sample);payload["source_sha"]="old"
        self.assert_rejected(payload)

    def test_06_false_reference_only_rejected(self):
        payload=copy.deepcopy(self.sample);payload["reference_only"]=False
        self.assert_rejected(payload)

    def test_07_changed_schema_rejected(self):
        payload=copy.deepcopy(self.sample);payload["schema"]="game-wallet-v1"
        self.assert_rejected(payload)

    def test_08_unknown_request_id_rejected(self):
        payload=copy.deepcopy(self.sample);payload["requests"]=["EK99"]
        self.assert_rejected(payload)

    def test_09_duplicate_owned_inventory_rejected(self):
        payload=copy.deepcopy(self.sample)
        payload["owned_parts"].append(copy.deepcopy(payload["owned_parts"][0]))
        self.assert_rejected(payload)

    def test_10_negative_stock_rejected(self):
        payload=copy.deepcopy(self.sample);payload["owned_parts"][0]["count"]=-1
        self.assert_rejected(payload)

    def test_11_non_gun_weapon_rejected(self):
        payload=copy.deepcopy(self.sample);payload["owned_parts"][0]["weapon"]="Railgun"
        self.assert_rejected(payload)

    def test_12_unknown_json_field_rejected(self):
        payload=copy.deepcopy(self.sample);payload["spend_wallet"]=True
        self.assert_rejected(payload)

    def test_13_duplicate_json_field_rejected(self):
        original=json.dumps(self.sample)
        repeated=original[:-1]+', "requests": ["EK08"]}'
        self.assert_rejected(self.sample,raw=repeated.encode("utf-8"))

    def test_14_existing_output_no_clobber_and_input_untouched(self):
        code,_,stderr,data,untouched=self.run_case(self.sample,existing=True)
        self.assertEqual(code,2,stderr)
        self.assertEqual(data,b"KEEP_EXISTING_FILE")
        self.assertTrue(untouched)

    def test_15_malformed_json_is_rejected_without_output(self):
        self.assert_rejected(self.sample,raw=b'{ "requests": ')

    def test_17_cannot_write_into_unity_asset_tree(self):
        protected=ROOT/"Assets/B01KitReadiness001/DO_NOT_WRITE_W10_TEST.json"
        self.assertFalse(protected.exists(),"Preexisting test path must not be touched")
        with tempfile.TemporaryDirectory() as tmp:
            input_path=pathlib.Path(tmp)/"request.json"
            input_path.write_bytes(self.sample_bytes)
            cp=subprocess.run(
                ["dotnet",str(DLL),"--input",str(input_path),"--out",str(protected)],
                cwd=ROOT,capture_output=True,text=True)
            self.assertEqual(cp.returncode,2,cp.stderr)
            self.assertFalse(protected.exists())

    def test_18_real_procurement_batch_shortfall_not_double_counted(self):
        code, _, stderr, data, _ = self.run_case(self.sample)
        self.assertEqual(code,0,stderr)
        obj=json.loads(data)
        f=obj["procurement"]
        self.assertEqual(f["TotalRequestedKits"],2)
        self.assertEqual((f["TotalRequiredPieces"],f["TotalCoveredPieces"],
                          f["TotalMissingPieces"]),(10,5,5))
        self.assertEqual(f["TotalReferenceMissingScrap"],110)
        self.assertFalse(f["AllRequestedKitsPossibleFromCurrentStock"])
        wheel=next(x for x in f["Lines"] if x["Kind"]=="Wheel")
        self.assertEqual((wheel["Required"],wheel["Covered"],wheel["Missing"]),(4,2,2))

    def test_19_three_totals_equal_sum_of_each_physical_deficit_line(self):
        forecast=json.loads(self.run_case(self.sample)[3])["procurement"]
        lines=forecast["Lines"]
        self.assertEqual(forecast["TotalRequiredPieces"],sum(x["Required"] for x in lines))
        self.assertEqual(forecast["TotalCoveredPieces"],sum(x["Covered"] for x in lines))
        self.assertEqual(forecast["TotalMissingPieces"],sum(x["Missing"] for x in lines))
        self.assertEqual(forecast["TotalReferenceMissingScrap"],
                         sum(x["Missing"]*x["UnitReferenceScrap"] for x in lines))

    def test_20_empty_stock_two_ek01_needs_10_pieces_reference_220(self):
        payload=copy.deepcopy(self.sample)
        payload["owned_parts"]=[]
        forecast=json.loads(self.run_case(payload)[3])["procurement"]
        self.assertEqual((forecast["TotalRequiredPieces"],forecast["TotalMissingPieces"],
                          forecast["TotalReferenceMissingScrap"]),(10,10,220))

    def test_21_zero_requests_is_empty_forecast(self):
        payload=copy.deepcopy(self.sample)
        payload["requests"]=[]
        forecast=json.loads(self.run_case(payload)[3])["procurement"]
        self.assertEqual(forecast["TotalRequestedKits"],0)
        self.assertEqual(forecast["Lines"],[])
        self.assertEqual(forecast["TotalReferenceMissingScrap"],0)
        self.assertTrue(forecast["AllRequestedKitsPossibleFromCurrentStock"])

    def test_22_full_stock_for_both_kits_is_no_missing_parts(self):
        payload=copy.deepcopy(self.sample)
        for line in payload["owned_parts"]:
            line["count"] *= 2
        full=json.loads(self.run_case(payload)[3])
        self.assertEqual(full["result"]["ReadyCount"],2)
        self.assertEqual(full["procurement"]["TotalMissingPieces"],0)
        self.assertEqual(full["procurement"]["TotalReferenceMissingScrap"],0)
        self.assertTrue(full["procurement"]["AllRequestedKitsPossibleFromCurrentStock"])

    def test_23_procurement_is_order_independent_while_virtual_reservations_are_ordered(self):
        payload=copy.deepcopy(self.sample)
        payload["requests"]=["EK01","EK08","EK02"]
        payload["owned_parts"] += [
            {"kind":"CrawlerTrack","count":2},
            {"kind":"Gun","weapon":"RocketLauncher","count":1},
            {"kind":"Gun","weapon":"Heavy","count":1},
            {"kind":"DirectionalShield","count":1}
        ]
        a=json.loads(self.run_case(payload)[3])
        payload["requests"]=list(reversed(payload["requests"]))
        b=json.loads(self.run_case(payload)[3])
        self.assertEqual(a["procurement"],b["procurement"])
        self.assertEqual(a["procurement"]["TotalMissingPieces"],1)
        self.assertEqual(a["procurement"]["TotalReferenceMissingScrap"],30)
        self.assertNotEqual([x["Id"] for x in a["result"]["Requests"]],
                            [x["Id"] for x in b["result"]["Requests"]])

    def test_16_too_many_requests_fail_closed(self):
        payload=copy.deepcopy(self.sample);payload["requests"]=["EK01"]*101
        self.assert_rejected(payload)


if __name__ == "__main__":
    unittest.main(verbosity=2)
