#!/usr/bin/env python3
"""SOURCE-only regression witness for live R3 vs owner-approved ECON V7.

Never label this as native Unity compile, CLR execution or an economy fix.
"""
import hashlib
import json
import math
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[3]
SOURCE = ROOT / "Assets/R3/R3VehicleModel.cs"
TEST = ROOT / "Assets/Tests/Editor/B0110W31EconomyParityTests.cs"
META = pathlib.Path(str(TEST) + ".meta")
TEMPORAL_TEST = ROOT / "Assets/Tests/Editor/B0110W32EconomySettlementParityTests.cs"
TEMPORAL_META = pathlib.Path(str(TEMPORAL_TEST) + ".meta")
PARENT = "3bd9b159997924dcecb4e1cd1d41137896e5199e"
EXPECTED_GAME_BLOB = "1d0f378dc598e08b3d7c16975f419d84231511bb"
GUID = "04789746afe84cd7b86254ad3c412d21"
TEMPORAL_GUID = "e2848a3cf72e4ab29a51d2eec810fb32"
ALLOWED = {
    "Assets/Tests/Editor/B0110W31EconomyParityTests.cs",
    "Assets/Tests/Editor/B0110W31EconomyParityTests.cs.meta",
    "Assets/Tests/Editor/B0110W32EconomySettlementParityTests.cs",
    "Assets/Tests/Editor/B0110W32EconomySettlementParityTests.cs.meta",
    "Docs/B01Design/W31_ECON_LIVE_R3_PARITY_QA.md",
    "Docs/B01Design/W32_ECON_V7_DEFERRED_SETTLEMENT_RECEIVER.md",
    "Tools/B01-10/W31_ECON_PARITY/source_guard.py",
    ".github/workflows/b01-10-w31-econ-parity-source.yml",
}

def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT, text=True).strip()

def detect(source):
    body = source.split("public int RepairCost(R3PartState part)", 1)[1].split("public bool Repair(", 1)[0]
    rate = re.search(r"\(1\s*-\s*part\.Health\)\s*\*\s*purchase\s*\*\s*(0\.\d+)f", body)
    if not rate:
        raise AssertionError("RepairCost original source formula missing — review updated code")
    zero_case = "part.Health<=0&&purchase<=1)return 0;" in body
    repair = source.split("public bool Repair(Vector3Int cell)",1)[1].split("public int RepairAffordable()",1)[0]
    stock = source.split("public bool RepairInventory(string id)",1)[1].split("public int DismantleValue(",1)[0]
    return (float(rate.group(1)), zero_case, "price <= 0" in repair, "cost<=0" in stock,
            "Scrap -= price; part.Health = 1; return true;" in repair,
            "Scrap-=cost;part.Health=1;return true;" in stock)

def quote(p, rate, one_zero):
    if p <= 1 and one_zero:
        return 0
    return min(p-1, max(1, math.ceil(p*rate - 1e-10)))

def main():
    actual = SOURCE.read_text(encoding="utf-8")
    test = TEST.read_text(encoding="utf-8")
    meta = META.read_text(encoding="utf-8")
    temporal_test = TEMPORAL_TEST.read_text(encoding="utf-8")
    temporal_meta = TEMPORAL_META.read_text(encoding="utf-8")
    assert git("hash-object", str(SOURCE.relative_to(ROOT))) == EXPECTED_GAME_BLOB, "Original live game code SHA changed"
    paths = set(git("diff", "--name-only", PARENT, "HEAD").splitlines())
    assert paths == ALLOWED, f"Foreign or missing source paths: {sorted(paths ^ ALLOWED)}"
    assert meta.count(f"guid: {GUID}") == 1
    assert len(set(re.findall(r"guid:\s*([0-9a-f]{32})", meta))) == 1
    assert test.count("[TestCase(") == 4 and test.count("[Test]") == 1
    assert "Assert.That(owned.Health, Is.Zero" in test, "W31 fixture must not allow instant 0HP recovery"
    assert temporal_test.count("[Test]") == 2 and "part.Health, Is.Zero" in temporal_test
    assert "part.Health, Is.EqualTo(1f)" in temporal_test, "Above-zero partial HP test missing"
    assert temporal_meta.count(f"guid: {TEMPORAL_GUID}") == 1
    assert GUID != TEMPORAL_GUID, "Unity GUID collision"
    assert "model.RepairCost(" in test and "model.RepairInventory(" in test and "model.ReceiveSalvage(" in test
    rate, boundary, rejected_mounted, rejected_stock, instant_mounted, instant_inventory = detect(actual)
    assert rate == 0.35 and boundary and rejected_mounted and rejected_stock and instant_mounted and instant_inventory, "Current expected-source RED has changed; inspect genuine fix"

    rows = []
    for purchase in (1, 5, 15, 30, 100):
        current = quote(purchase, rate, boundary)
        v7 = math.floor(purchase*0.65)
        rows.append({"purchase":purchase, "r3_0hp_quote":current, "approved_v7_quote":v7,
                     "differs": current != v7, "paid_zero_rejected":purchase==1 and rejected_stock})
    assert [r["r3_0hp_quote"] for r in rows] == [0,2,6,11,35]
    assert [r["approved_v7_quote"] for r in rows] == [0,3,9,19,65]
    assert sum(r["differs"] for r in rows)==4

    # Negatives: source policy changed or ZERO-PAID refusal corrected must invalidate this RED witness.
    for mutant in (
        actual.replace("purchase*0.35f", "purchase*0.65f"),
        actual.replace("if(part.Health<=0&&purchase<=1)return 0;", "// boundary changed"),
        actual.replace("if(part==null||cost<=0||Scrap<cost)return false;", "if(part==null||Scrap<cost)return false;"),
        actual.replace("Scrap -= price; part.Health = 1; return true;", "Scrap -= price; return true;"),
        actual.replace("Scrap-=cost;part.Health=1;return true;", "Scrap-=cost;return true;"),
    ):
        assert detect(mutant) != (rate, boundary, rejected_mounted, rejected_stock, instant_mounted, instant_inventory)
    receipt = {"kind":"SOURCE_WITNESS_EXPECTED_GAME_RED_NOT_NATIVE_UNITY",
               "source_base":PARENT, "game_blob":EXPECTED_GAME_BLOB,
               "qa_commit":git("rev-parse","HEAD"),
               "tested_qa_paths":sorted(paths),
               "source_sha256":hashlib.sha256(actual.encode("utf-8")).hexdigest(),
               "rows":rows,
               "native_unity":"NOT_RUN", "windows_player":"NOT_RUN", "economic_fix":"NOT_IMPLEMENTED",
               "deferred_0hp_booking_source_gap":"DETECTED",
               "mutation_negatives_rejected":5}
    print(json.dumps(receipt,sort_keys=True,indent=2))
    print("B0110_W32_ORIGINAL_R3_V7_PRICE_AND_TEMPORAL_DIFF_EXPECTED_RED_SOURCE_WITNESS_PASS")
    return 0

if __name__=="__main__":
    try:
        sys.exit(main())
    except Exception as e:
        print("SOURCE_VERIFIER_FAIL:", type(e).__name__,str(e),file=sys.stderr)
        sys.exit(2)
