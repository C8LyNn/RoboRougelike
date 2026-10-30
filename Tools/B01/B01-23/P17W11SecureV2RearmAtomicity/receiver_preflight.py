#!/usr/bin/env python3
"""Read-only migration gate for an isolated V2 receiver workspace; NO GAME WRITES.
Rejects installed W07/W08/W09 source, prefab/scene MonoScript GUID references,
host-source legacy C# types, and symlinks. Exact old GUIDs from frozen Git objects.
"""
from pathlib import Path
import re
import sys

BANNED_PREFIXES=(
    "Assets/B0123DebrisPlayground001/",
    "Assets/B0123ReattachClearance001/",
    "Assets/B0123DockHardening001/",
)
FROZEN_LEGACY_SCRIPT_GUIDS={
    "b017d070000000000000000000000008", # original #439 W07 public bypass
    "b018d080000000000000000000000008", # original #455 optional gate
    "b019d090000000000000000000000008", # original #471 GUI overlay
}
BANNED_CSHARP_TYPES=(
    "DebrisRigPlayground",
    "DebrisSafeReattachGate",
    "HardenedDockDemo",
)
V2_SOURCE="Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs"
def validate(root):
    root=Path(root)
    if not root.is_dir() or root.is_symlink():
        raise ValueError("RECEIVER_MISSING_OR_LINKED")
    required=root/V2_SOURCE
    if not required.is_file() or required.is_symlink():
        raise ValueError("RECEIVER_MISSING_EXCLUSIVE_V2_CONTROLLER")
    original=required.read_text(encoding="utf-8")
    for route in ("Reattach","TrySafeReattach","ReattachFromGui"):
        if "public bool "+route+"(int index) => DockGate()."+route+"(index);" not in original:
            raise ValueError("RECEIVER_PUBLIC_ROUTE_UNGUARDED_"+route)
    if "if(!rig.Emitter.RearmAfterReattach(rig.Spec.PartId))\n                return false;" not in original:
        raise ValueError("RECEIVER_TRANSACTION_VERSION_NOT_V1_1")
    checked=0
    for p in sorted(root.rglob("*")):
        if p.is_symlink():raise ValueError("RECEIVER_SYMLINK_REFUSED")
        if not p.is_file():continue
        rel=p.relative_to(root).as_posix()
        if any(rel.startswith(prefix) for prefix in BANNED_PREFIXES):
            raise ValueError("LEGACY_COMPONENT_OR_SOURCE_PATH "+rel)
        # Scan scene/prefab/asset YAML for real original legacy MonoScript GUIDs;
        # comments don't count as runtime references unless the GUID itself exists.
        if p.suffix.lower() in {".unity",".prefab",".asset"}:
            data=p.read_text(encoding="utf-8",errors="replace").lower()
            for guid in FROZEN_LEGACY_SCRIPT_GUIDS:
                if re.search(r"guid:\s*"+guid+r"\b",data):
                    raise ValueError("LEGACY_SCENE_PREFAB_SCRIPT_GUID "+rel)
        # Source that uses the old component remains unsafe even if not instantiated yet.
        if p.suffix.lower()==".cs" and not rel.startswith("Assets/B0123SecureReattachV2/"):
            data=p.read_text(encoding="utf-8",errors="replace")
            for name in BANNED_CSHARP_TYPES:
                if re.search(r"\b"+name+r"\b",data):
                    raise ValueError("LEGACY_HOST_CODE_REFERENCE "+rel)
        checked+=1
    return checked
if __name__=="__main__":
    try:
        count=validate(sys.argv[1])
        print("EXCLUSIVE_V2_V1_1_RECEIVER_PREFLIGHT_PASS",count,
              "OLD_W07_W08_W09_GUIDS_ABSENT=true")
    except (ValueError,IndexError) as error:
        print("EXCLUSIVE_V2_V1_1_RECEIVER_PREFLIGHT_RED "+str(error),file=sys.stderr)
        sys.exit(3)
