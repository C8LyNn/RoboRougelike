#!/usr/bin/env python3
"""Read-only original Codex C# R3/R4 enum/price provenance gate. NOT a Unity test."""
import argparse
import copy
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
R3 = "Assets/R3/R3VehicleModel.cs"
R4 = "Assets/R4/R4WeaponDefinition.cs"

def git(ref, path, mode):
    return subprocess.run(["git", mode, f"{ref}:{path}"], check=True, capture_output=True, text=True).stdout.strip()

def enum(src, name):
    m = re.search(r"public\s+enum\s+" + name + r"\s*\{([^}]+)\}", src)
    if not m:
        raise ValueError("ENUM_MISSING_" + name)
    return [s.strip() for s in m.group(1).split(",")]

def check(c, r3, r4):
    if c.get("schema") != "b01-equipment-kits-v1":
        raise ValueError("INVALID_CATALOG")
    kinds, weapons = enum(r3, "R3PartKind"), enum(r4, "R4WeaponKind")
    switch = re.search(r"public\s+int\s+Price\(R3PartKind\s+kind\)\s*=>\s*kind\s+switch\s*\{(.*?)\};", r3, re.S)
    if not switch:
        raise ValueError("MISSING_R3_PRICE")
    constants = {}
    for k in ("ArmorCost", "WheelCost", "GunCost"):
        m = re.search(r"\b" + k + r"\s*=\s*(\d+)", r3)
        if not m:
            raise ValueError("MISSING_" + k)
        constants[k] = int(m.group(1))
    price = {"Armor": constants["ArmorCost"]}
    for kind, raw in re.findall(r"R3PartKind\.(\w+)\s*=>\s*(\w+)", switch.group(1)):
        price[kind] = int(raw) if raw.isdecimal() else constants[raw]
    for k, v in c["part_prices"].items():
        if k not in kinds or price.get(k) != v:
            raise ValueError("HOST_R3_PRICE_OR_KIND_DRIFT_" + k)
    defs = re.findall(r'new\s+R4WeaponDefinition\(\s*"[^"]+"\s*,\s*(.*?)\s*,\s*new\s+Color\(', r4, re.S)
    if len(defs) != len(weapons):
        raise ValueError("WEAPON_DEFINITION_COUNT_DRIFT")
    actual = {}
    for kind, params in zip(weapons, defs):
        args = [v.strip() for v in params.split(",")]
        if len(args) != 8 or not args[7].isdecimal():
            raise ValueError("UNPARSABLE_R4_PRICE")
        actual[kind] = int(args[7])
    if actual != c["weapon_prices"]:
        raise ValueError("HOST_R4_PRICE_OR_ENUM_DRIFT")
    for kit in c["kits"]:
        for item in kit["parts"]:
            if item["kind"] not in kinds or item["kind"] == "Core" or (item["kind"] == "Gun" and item.get("weapon") not in weapons):
                raise ValueError("INVALID_KIT_KIND")
    return len(c["kits"])

def mutation(name, catalog, r3, r4):
    try:
        check(catalog, r3, r4)
    except (KeyError, TypeError, ValueError):
        return name
    raise AssertionError("NEGATIVE_MUTATION_ACCEPTED_" + name)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--current-ref", required=True)
    ap.add_argument("--selftest", action="store_true")
    args = ap.parse_args()
    c = json.loads((ROOT / "catalog.json").read_text(encoding="utf-8"))
    p = c["provenance"]
    for file, sha in ((R3, p["r3_blob"]), (R4, p["r4_blob"])):
        if git(p["codex_sha"], file, "rev-parse") != sha:
            raise ValueError("ORIGINAL_BLOB_MISMATCH_" + file)
        if git(args.current_ref, file, "rev-parse") != sha:
            raise ValueError("LATEST_CODEX_BLOB_DRIFT_" + file)
    r3, r4 = git(p["codex_sha"], R3, "show"), git(p["codex_sha"], R4, "show")
    n = check(c, r3, r4)
    results = []
    if args.selftest:
        x = copy.deepcopy(c)
        x["part_prices"]["Wheel"] += 1
        results.append(mutation("tamper-part-price", x, r3, r4))
        x = copy.deepcopy(c)
        x["weapon_prices"]["Railgun"] += 1
        results.append(mutation("tamper-weapon-price", x, r3, r4))
        x = copy.deepcopy(c)
        x["kits"][0]["parts"][0]["kind"] = "Unknown"
        results.append(mutation("unknown-part", x, r3, r4))
        x = copy.deepcopy(c)
        x["kits"][0]["parts"][0] = {"kind":"Gun","weapon":"Unknown","count":1}
        results.append(mutation("unknown-weapon", x, r3, r4))
        x = r3.replace("R3PartKind.Booster=>35", "R3PartKind.Booster=>39", 1)
        if x == r3: raise AssertionError("R3_MUTANT_MISSING")
        results.append(mutation("changed-original-R3", c, x, r4))
        x = r4.replace("Basic, Automatic", "Automatic, Basic", 1)
        if x == r4: raise AssertionError("R4_ENUM_MUTANT_MISSING")
        results.append(mutation("reordered-R4-enum", c, r3, x))
        x = r4.replace("1.8f,24,140,30,", "1.8f,24,140,31,", 1)
        if x == r4: raise AssertionError("R4_PRICE_MUTANT_MISSING")
        results.append(mutation("changed-original-R4", c, r3, x))
    print(json.dumps({"result":"PASS","kits":n,"original_sha":p["codex_sha"],"current_ref":args.current_ref,
        "original_blobs":[p["r3_blob"],p["r4_blob"]],"rejected_mutations":results}, indent=2))

if __name__ == "__main__":
    try: main()
    except (ValueError, KeyError, TypeError, AssertionError, subprocess.CalledProcessError) as ex:
        print("FAIL: " + str(ex), file=sys.stderr)
        sys.exit(2)
