#!/usr/bin/env python3
"""P17 Unity TextAsset stable GUID and cross-asset collision gate (no Unity)."""
import pathlib
import re
import sys

OWN = {
 "Assets/PARTSIZE001/Blueprints.meta": "81f0a91a6b4b4d4693eb791663cc51e4",
 "Assets/PARTSIZE001/Blueprints/P17.meta": "81f0a91a6b4b4d4693eb791663cc51e5",
 "Assets/PARTSIZE001/Blueprints/P17/P17_Flanker_Wheels.json.meta": "81f0a91a6b4b4d4693eb791663cc51e6",
 "Assets/PARTSIZE001/Blueprints/P17/P17_Bulwark_Tracks.json.meta": "81f0a91a6b4b4d4693eb791663cc51e7",
 "Assets/PARTSIZE001/Blueprints/P17/P17_Skirmisher_Hover.json.meta": "81f0a91a6b4b4d4693eb791663cc51e8",
}
GUID = re.compile(r"^guid: ([0-9a-f]{32})$", re.MULTILINE)
def parse_guid(text):
    matches=GUID.findall(text)
    if len(matches)!=1:
        raise ValueError("BAD_GUID_DECLARATION")
    return matches[0]

def check(repo):
    checked=0
    for path,expected in OWN.items():
        file=repo/path
        if not file.is_file():
            raise ValueError("MISSING_P17_META:"+path)
        body=file.read_text(encoding="utf-8")
        if parse_guid(body)!=expected:
            raise ValueError("P17_META_GUID_CHANGED:"+path)
        if path.endswith(".json.meta"):
            if "TextScriptImporter:" not in body or not (repo/path[:-5]).is_file():
                raise ValueError("P17_TEXT_ASSET_MISSING:"+path)
        elif "folderAsset: yes" not in body or not (repo/path[:-5]).is_dir():
            raise ValueError("P17_FOLDER_META_INCORRECT:"+path)
        checked+=1
    # No P17 GUID may be reused by any other existing Unity meta file.
    seen=set()
    for file in (repo/"Assets").rglob("*.meta"):
        # Do not turn unrelated legacy .meta formatting into a new P17 blocker.
        # Still inspect every GUID token for collisions with our exact five.
        matches=GUID.findall(file.read_text(encoding="utf-8"))
        for guid in matches:
            if guid in OWN.values():
                path=file.relative_to(repo).as_posix()
                if path not in OWN or OWN[path]!=guid or guid in seen:
                    raise ValueError("GUID_COLLISION_WITH_EXISTING_ASSET:"+path)
                seen.add(guid)
    if seen!=set(OWN.values()):
        raise ValueError("P17_GUIDS_MISSING_FROM_ASSETS")
    return checked

def selftest():
    assert parse_guid("fileFormatVersion: 2\nguid: "+"a"*32+"\n")=="a"*32
    for mutated in ("guid: "+"a"*31+"\n","guid: INVALID\n",
                    "guid: "+"a"*32+"\nguid: "+"b"*32+"\n",
                    "fileFormatVersion: 2\n"):
        try:
            parse_guid(mutated)
        except ValueError:
            continue
        raise AssertionError("META_NEGATIVE_FALSE_GREEN")
    return 4

if __name__=="__main__":
    try:
        count=check(pathlib.Path("."))
        negatives=selftest()
        print(f"P17_UNITY_META_GUID_PASS {count}/5 original .meta; {negatives}/4 negatives; Unity NOT_RUN")
    except (AssertionError,ValueError,OSError) as e:
        print("P17_UNITY_META_GUID_FAIL:"+str(e),file=sys.stderr)
        raise SystemExit(1)
