#!/usr/bin/env python3
"""Strict immutable B01-22 #333 -> Codex #383 R3 source drift gate; never modifies game."""
import argparse
import copy
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path

KITS = "0d212a706c1e633b773d2e03b7b40c28bb63521b"
OLD = "aa7a917f177a9b1f07203868fb7d1d871eb3ee46"
CURRENT = "203a0805251ed28293888e43f2d49f0df63767c6"
R3 = "Assets/R3/R3VehicleModel.cs"
R4 = "Assets/R4/R4WeaponDefinition.cs"
PREFIX = "Research/B01-22/P17/EQUIPMENT-KITS-001/"
OLD_R3 = "8cc68faf89793c3a803e5807594c9faf0df10091"
NEW_R3 = "1d0f378dc598e08b3d7c16975f419d84231511bb"
R4_BLOB = "502f6d7f70d4e8078260de632ab181f244a4d730"
DECL_OLD = "public sealed class R3VehicleModel"
DECL_NEW = "public sealed partial class R3VehicleModel"

class GateError(ValueError): pass

def git(repo, verb, ref, path):
    proc = subprocess.run(["git", verb, ref + ":" + path], cwd=repo, check=True, capture_output=True)
    return proc.stdout

def blob(repo, ref, path):
    return git(repo, "rev-parse", ref, path).decode("ascii").strip()

def content(repo, ref, path):
    return git(repo, "show", ref, path)

def load_source(repo):
    raw = content(repo, KITS, PREFIX + "manifest.json")
    m = json.loads(raw)
    if m.get("revision") != "1.0.2" or m.get("package_id") != "B01-22-EQUIPMENT-KITS-001":
        raise GateError("BAD_FROZEN_MANIFEST")
    entries = m.get("files")
    if not isinstance(entries, list) or len(entries) != 13:
        raise GateError("FROZEN_MANIFEST_COUNT_DRIFT")
    seen = set()
    for row in entries:
        path = row["path"]
        if not path.startswith(PREFIX) or path in seen or ".." in Path(path).parts:
            raise GateError("UNSAFE_FROZEN_PATH")
        seen.add(path)
        original = content(repo, KITS, path)
        if len(original) != row["bytes"] or hashlib.sha256(original).hexdigest() != row["sha256"]:
            raise GateError("FROZEN_SOURCE_HASH_MISMATCH:" + path)
    catalog = json.loads(content(repo, KITS, PREFIX + "catalog.json"))
    if catalog["provenance"] != {"codex_pr":310,"codex_sha":OLD,"r3_blob":OLD_R3,"r4_blob":R4_BLOB}:
        raise GateError("CATALOG_OLD_PROVENANCE_CHANGED")
    return catalog, hashlib.sha256(raw).hexdigest()

def parse_enum(source, name):
    match = re.search(r"public\s+enum\s+" + name + r"\s*\{([^}]+)\}", source)
    if not match: raise GateError("MISSING_ENUM:" + name)
    rows = [x.strip() for x in match.group(1).split(",")]
    if len(rows) != len(set(rows)) or any(not re.fullmatch("[A-Za-z][A-Za-z0-9_]*", x) for x in rows):
        raise GateError("BAD_ENUM:" + name)
    return rows

def parse_prices(r3, r4):
    kinds, weapons = parse_enum(r3, "R3PartKind"), parse_enum(r4, "R4WeaponKind")
    constants = {}
    for name in ("ArmorCost","WheelCost","GunCost"):
        found = re.search(r"\b" + name + r"\s*=\s*(\d+)\b", r3)
        if not found: raise GateError("PRICE_CONSTANT_MISSING:" + name)
        constants[name] = int(found.group(1))
    switch = re.search(r"public\s+int\s+Price\(R3PartKind\s+kind\)\s*=>\s*kind\s+switch\s*\{(.*?)\};",r3,re.S)
    if not switch: raise GateError("R3_PRICE_SWITCH_MISSING")
    parts = {"Armor":constants["ArmorCost"]}
    for kind, raw in re.findall(r"R3PartKind\.(\w+)\s*=>\s*(\w+)",switch.group(1)):
        if kind not in kinds or kind in parts: raise GateError("BAD_R3_PRICE_ENTRY")
        parts[kind] = int(raw) if raw.isdecimal() else constants[raw]
    parts.pop("Gun", None)
    raw_defs = re.findall(r'new\s+R4WeaponDefinition\(\s*"[^"]+"\s*,\s*(.*?)\s*,\s*new\s+Color\(',r4,re.S)
    if len(raw_defs) != len(weapons):raise GateError("R4_DEFINITION_COUNT")
    guns = {}
    for kind, raw in zip(weapons,raw_defs):
        values = [x.strip() for x in raw.split(",")]
        if len(values) != 8 or not values[7].isdecimal(): raise GateError("R4_PRICE_PARSE")
        guns[kind] = int(values[7])
    return parts,guns

def check_catalog(catalog, r3, r4):
    prices, guns = parse_prices(r3,r4)
    if catalog["part_prices"] != prices or catalog["weapon_prices"] != guns:
        raise GateError("HOST_CATALOG_PRICE_OR_ENUM_DRIFT")
    kits = catalog["kits"]
    if len(kits) != 10: raise GateError("BAD_KIT_COUNT")
    output = []
    for i,kit in enumerate(kits,1):
        if kit["id"] != "EK%02d"%i:raise GateError("BAD_KIT_ID_OR_ORDER")
        sum_scrap,items,gun_types = 0,0,set()
        rows=set()
        for part in kit["parts"]:
            kind, count, weapon = part["kind"],part["count"],part.get("weapon")
            if type(count) is not int or not 1<=count<=8 or kind=="Core":
                raise GateError("INVALID_KIT_QUANTITY")
            if kind=="Gun":
                if weapon not in guns:raise GateError("INVALID_WEAPON_KIND")
                gun_types.add(weapon); unit=guns[weapon]
            else:
                if kind not in prices or weapon is not None:raise GateError("INVALID_PART_KIND")
                unit=prices[kind]
            if (kind,weapon) in rows:raise GateError("DUPLICATE_PART")
            rows.add((kind,weapon));sum_scrap+=count*unit;items+=count
        if items>12 or len(gun_types)>2 or not rows:raise GateError("KIT_LIMIT")
        output.append({"id":kit["id"],"reference_scrap":sum_scrap,"physical_items":items})
    return output

def check_versions(catalog, a,b,c,d):
    if a.count(DECL_OLD)!=1 or b.count(DECL_NEW)!=1 or a.replace(DECL_OLD,DECL_NEW,1)!=b:
        raise GateError("R3_DRIFT_NOT_EXACTLY_PARTIAL_DECLARATION")
    if c!=d:raise GateError("R4_BYTES_CHANGED")
    old=check_catalog(catalog,a,c)
    new=check_catalog(catalog,b,d)
    if old!=new:raise GateError("KIT_OFFERS_DRIFT")
    return new

def verify(repo):
    for ref,path,expected in ((OLD,R3,OLD_R3),(OLD,R4,R4_BLOB),(CURRENT,R3,NEW_R3),(CURRENT,R4,R4_BLOB)):
        if blob(repo,ref,path)!=expected:raise GateError("SOURCE_BLOB_NOT_APPROVED:"+ref+":"+path)
    catalog,manifest_hash=load_source(repo)
    a,b,c,d=(content(repo,ref,path).decode("utf-8") for ref,path in
        ((OLD,R3),(CURRENT,R3),(OLD,R4),(CURRENT,R4)))
    kits=check_versions(catalog,a,b,c,d)
    return {"schema":"b01-ek-r3-compat-v1","status":"EXACT_SOURCE_COMPATIBLE_NOT_INTEGRATED",
        "frozen_kits_sha":KITS,"frozen_manifest_sha256":manifest_hash,
        "frozen_files_verified":13,"old_codex_sha":OLD,"current_codex_sha":CURRENT,
        "old_r3_blob":OLD_R3,"current_r3_blob":NEW_R3,"r4_blob":R4_BLOB,
        "only_r3_change":"sealed class -> sealed partial class",
        "offers":kits,"native_unity":"NOT_RUN","independent_qa":"PENDING",
        "codex_ack":"PENDING","codex_integration":"NOT_PERFORMED"}

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--repo",default=".")
    parser.add_argument("--receipt")
    args=parser.parse_args()
    result=verify(Path(args.repo).resolve())
    output=json.dumps(result,ensure_ascii=False,sort_keys=True,indent=2)+"\n"
    if args.receipt:
        path=Path(args.receipt)
        if any(x.is_symlink() for x in (path,*path.parents)):
            raise GateError("RECEIPT_SYMLINK")
        with path.open("x",encoding="utf-8",newline="\n") as f:f.write(output)
    print(output)

if __name__=="__main__":
    try:main()
    except (GateError,OSError,KeyError,TypeError,ValueError,subprocess.CalledProcessError) as exc:
        print("FAIL_CLOSED: "+str(exc),file=sys.stderr)
        sys.exit(2)
