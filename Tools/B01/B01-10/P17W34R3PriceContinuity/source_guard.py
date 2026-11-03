#!/usr/bin/env python3
"""Exact original #923 one-writer negative QA; SOURCE, not Unity / not a gameplay fix."""
import json
import pathlib
import re
import subprocess
ROOT=pathlib.Path(__file__).resolve().parents[4]
BASE="0d4904b735999f772e048be4bddc6bcc7bb9a9f3"
FILES={
    ".github/workflows/b01-10-w34-r3-price-continuity.yml",
    "Tools/B01/B01-10/P17W34R3PriceContinuity/Program.cs",
    "Tools/B01/B01-10/P17W34R3PriceContinuity/W34PriceContinuity.csproj",
    "Tools/B01/B01-10/P17W34R3PriceContinuity/source_guard.py",
    "Tools/B01/B01-10/P17W34R3PriceContinuity/README.md",
}
def git(*args):
    return subprocess.check_output(["git",*args],cwd=ROOT,text=True).strip()
def blob(path):
    return git("hash-object",path)
def main():
    files=set(filter(None,git("diff","--name-only",BASE,"HEAD").splitlines()))
    assert files==FILES,("scope",sorted(files^FILES))
    model=git("show","3bd9b159997924dcecb4e1cd1d41137896e5199e:Assets/R3/R3VehicleModel.cs")
    assert "ArmorCost = 5, WheelCost = 15, GunCost = 30" in model,"game default prices drifted"
    expected={
      "Tools/B01/B01-62/P17W33R3World/CanonicalR3WorldV1.cs":"029021b32538c309fc0e85eb48ca5202c96e239c",
      "Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs":"65e58e579becedb86f1162d9a511508cb77dfb7b",
      "Assets/R3/R3VehicleCatalog.cs":"f76de5bbdcc1dbdb7ddb9fe9859c36c232fd8584",
    }
    for name,sha in expected.items():assert blob(name)==sha,(name,"producer drifted")
    producer=(ROOT/"Tools/B01/B01-62/P17W33R3World/Program.cs").read_text()
    assert 'PurchaseScrap=100' in producer,"producer model assumption changed"
    store=(ROOT/"Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs").read_text()
    semantic=store.split("private bool Semantic(",1)[1].split("private static bool SameEconomicSection(",1)[0]
    assert "dynamicWorld.Parts.Count!=econ.Parts.Count" in semantic
    assert "economicIds.TryGetValue(p.Id,out float hp)||hp!=p.Hp" in semantic
    assert "PurchaseScrap" not in semantic and "Category" not in semantic,"source admission now price-aware: review witness expectation"
    test=(ROOT/"Tools/B01/B01-10/P17W34R3PriceContinuity/Program.cs").read_text()
    for needle in ("WholeGameEconomySlotV3.Create","store.Commit(","CanonicalColdReadV3.ReadClosedSlot","WrongPaidZeroArmorBookingPersists","ColdPersistPrice(100,65)","ColdPersistPrice(5,3)"):
        assert needle in test,needle
    report={
      "tier":"PINNED_ORIGINAL_SOURCE_PRECHECK_ONLY",
      "base923":BASE,"head":git("rev-parse","HEAD"),
      "source923_blobs":expected,"files":sorted(files),
      "original_game_default_armor_purchase":5,
      "producer_fixture_armor_purchase":100,
      "warning":"Source-only store admits canonical Economy purchase prices without real R3 price provenance; genuine host binder must supply price authority.",
      "native_unity":"NOT_RUN",
    }
    print(json.dumps(report,sort_keys=True,indent=2))
    print("B0110_W34_ORIGINAL_R3_PRICE_PROVENANCE_SCOPE_PASS")
if __name__=="__main__":main()
