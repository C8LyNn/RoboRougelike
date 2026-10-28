#!/usr/bin/env python3
"""Original Unity TextAsset metadata and P17 W02 bracing package integrity (source only)."""
import json
import pathlib
import re
import sys
ROOT=pathlib.Path(".")
BASE="Assets/PARTSIZE001/Bracing"
FILES={
 "Assets/PARTSIZE001/Bracing.meta":"8a712c3456d74890ac51f0e203104101",
 "Assets/PARTSIZE001/Bracing/P17.meta":"8a712c3456d74890ac51f0e203104102",
 "Assets/PARTSIZE001/Bracing/P17/P17_Flanker_Reinforced.json.meta":"8a712c3456d74890ac51f0e203104103",
 "Assets/PARTSIZE001/Bracing/P17/P17_Bulwark_Reinforced.json.meta":"8a712c3456d74890ac51f0e203104104",
 "Assets/PARTSIZE001/Bracing/P17/P17_Skirmisher_Reinforced.json.meta":"8a712c3456d74890ac51f0e203104105",
 "Assets/PARTSIZE001/Bracing/P17/P17_StructuralBracingKits.json.meta":"8a712c3456d74890ac51f0e203104106",
}
PATTERN=re.compile(r"^guid: ([0-9a-f]{32})$",re.MULTILINE)
def exact(text):
    found=PATTERN.findall(text)
    if len(found)!=1:
        raise ValueError("INVALID_OR_MULTIPLE_UNITY_GUIDS")
    return found[0]
def check():
    seen=set()
    for file,guid in FILES.items():
        meta=(ROOT/file).read_text(encoding="utf-8")
        if exact(meta)!=guid: raise ValueError("ALTERED_OWN_GUID:"+file)
        if file.endswith(".json.meta"):
            if "TextScriptImporter:" not in meta or not (ROOT/file[:-5]).is_file():
                raise ValueError("ORIGINAL_TEXTASSET_OR_IMPORTER_MISSING")
        elif "folderAsset: yes" not in meta or not (ROOT/file[:-5]).is_dir():
            raise ValueError("FOLDER_META_CORRUPTED")
    for p in (ROOT/"Assets").rglob("*.meta"):
        for guid in PATTERN.findall(p.read_text(encoding="utf-8")):
            if guid in FILES.values():
                rel=p.as_posix()
                if rel not in FILES or FILES[rel]!=guid or guid in seen:
                    raise ValueError("COLLIDING_UNITY_META_GUID:"+rel)
                seen.add(guid)
    if seen!=set(FILES.values()):
        raise ValueError("MISSING_OWN_GUID_IN_PROJECT")
    kit=json.loads((ROOT/BASE/"P17/P17_StructuralBracingKits.json").read_text(encoding="utf-8"))
    if kit.get("schema")!="B01-P17-W02-STRUCTURAL-BRACING-KITS-v1" or len(kit["kits"])!=3:
        raise ValueError("KIT_SCHEMA_UNEXPECTED")
    if kit["originalWeakpointCommit"]!="098ccdbe8f129253fa4ada52318436b7cc2b4dc4":
        raise ValueError("PREVIOUS_PACKAGE_SHA_CHANGED")
    if kit["nativePhysicalStatus"]!="NOT_RUN" or kit["gameIntegrationStatus"]!="NOT_INTEGRATED":
        raise ValueError("FALSE_NATIVE_OR_INTEGRATION_PASS")
    if sum(len(k["mandatoryParts"]) for k in kit["kits"])!=5:
        raise ValueError("KIT_ARMOR_COUNT_CHANGED")
    if sum(len(k["expectedMitigations"]) for k in kit["kits"])!=6:
        raise ValueError("KIT_TARGET_COUNT_CHANGED")
    return True
def negatives():
    for mutation in ["missing","guid: BAD","guid: "+"a"*32+"\nguid: "+"b"*32,"guid: "+"a"*31]:
        try:exact(mutation)
        except ValueError:continue
        raise ValueError("BAD_META_ACCEPTED")
    return 4
if __name__=="__main__":
    try:
        check();n=negatives()
        print(f"B01_23_W02_BRACING_UNITY_META_PASS 6/6 GUIDs, 3/3 kits, 5/5 new Armor, 6/6 targets, {n}/4 negatives; UNITY_NOT_RUN")
    except (ValueError,KeyError,OSError) as e:
        print("B01_23_W02_BRACING_UNITY_META_FAIL:"+str(e),file=sys.stderr)
        sys.exit(1)
