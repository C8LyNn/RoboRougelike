#!/usr/bin/env python3
"""B01-62/W10: independent immutable-source API reattach route gate.

Source proof only: optional W09 GUI hardening is not a public method boundary.
Not a native Unity/PhysX execution or a gameplay fix.
"""
import argparse
import hashlib
import json
import re
import subprocess
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "P17W09ReattachRouteIntake"))
from intake import method, SourceDrift, git_blob  # noqa: E402

REVISIONS = {
    "w07": ("cdcaea750fda1a9d7db6018e34970f4b9f206427", "Assets/B0123DebrisPlayground001/Runtime/DebrisRigPlayground.cs", "e4e31c32cb5abda2fef987efa4b45fd61e828312"),
    "w08": ("99dfbd25d471f9983f9472b12d3725f4c002f7e9", "Assets/B0123ReattachClearance001/Runtime/DebrisSafeReattachGate.cs", "7c7e96a10d69149119309863a0c2a9bdab829072"),
    "w09": ("763748f1be0c61356fb4462144640bb359364226", "Assets/B0123DockHardening001/Runtime/HardenedDockDemo.cs", "d46c6341db199d010f48de00f98250becd2d7c87"),
    "policy": ("763748f1be0c61356fb4462144640bb359364226", "Assets/B0123DockHardening001/Runtime/HardenedDockPolicy.cs", "6c7498a35ac49d9d49638b3b2550852547c67339"),
    "installer": ("763748f1be0c61356fb4462144640bb359364226", "Assets/B0123DockHardening001/Editor/HardenedDockInstaller.cs", "5a503c93c64e4e016bfd4b0e97feb6716af62ad8"),
}

def original_sources(repo):
    repo = Path(repo)
    out = {}
    for key, (ref, path, expected) in REVISIONS.items():
        blob = subprocess.check_output(["git", "-C", str(repo), "show", ref + ":" + path])
        actual = git_blob(blob)
        confirmed = subprocess.check_output(["git", "-C", str(repo), "rev-parse", ref + ":" + path]).decode().strip()
        if actual != expected or confirmed != expected:
            raise SourceDrift("FAIL_CLOSED_ORIGINAL_SOURCE_DRIFT " + key)
        out[key] = blob.decode("utf-8-sig")
    return out

def require(condition, name):
    if not condition:
        raise SourceDrift("FAIL_CLOSED_CONTRACT_DRIFT " + name)

def has(pattern, data):
    return re.search(pattern, data, re.DOTALL) is not None

def inspect(s):
    _, raw = method(s["w07"], "Reattach", "bool")
    _, old_ui = method(s["w07"], "OnGUI", "void")
    _, w08_re = method(s["w08"], "TrySafeReattach", "bool")
    _, arm = method(s["w09"], "Arm", "bool")
    _, update = method(s["w09"], "Update", "void")
    _, new_re = method(s["w09"], "TryDock", "bool")
    _, new_inspect = method(s["w09"], "Inspect", "HardenedDockVerdict")
    _, audit = method(s["w09"], "AuditEveryLayer", "DockVerdict")
    _, new_ui = method(s["w09"], "OnGUI", "void")
    _, policy = method(s["policy"], "Evaluate", "HardenedDockVerdict")
    _, installer = method(s["installer"], "Install", "void")

    require(has(r"\bif\s*\(\s*Inspect\s*\(\s*demo\s*,\s*index\s*\)\s*!=\s*DockVerdict\.Allowed\s*\)\s*return\s+false", w08_re), "W08_POLICY_GUARD")
    require(has(r"\breturn\s+demo\.Reattach\s*\(\s*index\s*\)", w08_re), "W08_ROUTE")
    m = re.search(r"\bif\s*\(\s*Inspect\s*\(\s*index\s*\)\s*!=\s*HardenedDockVerdict\.Allowed\s*\)\s*return\s+false\s*;", new_re)
    call = re.search(r"\breturn\s+demo\.Reattach\s*\(\s*index\s*\)\s*;", new_re)
    require(m is not None and call is not None and call.start() > m.end(), "W09_DOUBLE_GATE_ROUTE")
    require(has(r"\bAuditEveryLayer\s*\(\s*index\s*\)", new_inspect)
            and has(r"\bpreviousGate\.Inspect\s*\(\s*demo\s*,\s*index\s*\)", new_inspect), "W09_TWO_AUDITS")
    require(all(has(p, audit) for p in (r"\bPhysics\.OverlapBoxNonAlloc\s*\(", r"\bPhysics\.ComputePenetration\s*\(", r"\bHardenedDockPolicy\.AllPhysicsLayers\b")), "W09_ALL_LAYER_AUDIT")
    require(has(r"\bqueryLayerMask\s*!=\s*AllPhysicsLayers", policy), "W09_MASK_POLICY")
    require(has(r"\bTryDock\s*\(\s*i\s*\)", new_ui), "W09_UI_ROUTE")
    require(has(r"\bdemo\.enabled\s*=\s*false\s*;", arm)
            and has(r"\bpreviousGate\.enabled\s*=\s*false\s*;", arm), "W09_ARM_UI_ONLY")
    require(has(r"\bdemo\.enabled\s*=\s*false\s*;", update)
            and has(r"\bpreviousGate\.enabled\s*=\s*false\s*;", update), "W09_UPDATE_UI_ONLY")
    require(has(r"\bUndo\.AddComponent\s*<\s*HardenedDockDemo\s*>\s*\(\s*host\s*\)", installer), "W09_OPT_IN_INSTALLER")

    original_public = has(r"\bpublic\s+bool\s+Reattach\s*\(\s*int\s+index\s*\)", s["w07"])
    old_ui_route = has(r"(?<!\.)\bReattach\s*\(\s*i\s*\)", old_ui)
    mutates_body_and_ledger = all(has(p, raw) for p in (
        r"\brig\.Body\.isKinematic\s*=\s*true",
        r"\brig\.Part\.transform\.SetParent\s*\(",
        r"\brig\.Detached\s*=\s*false",
        r"\brig\.Emitter\.RearmAfterReattach\s*\("))
    require(original_public and old_ui_route and mutates_body_and_ledger, "W07_PUBLIC_IDENTITY_MUTATOR")
    # .enabled=false only suppresses Unity behaviour callbacks, not public C# invocations.
    no_policy_in_raw = not has(r"\b(?:Inspect|TryDock|TrySafeReattach|AuditEveryLayer|ComputePenetration|OverlapBoxNonAlloc)\s*\(", raw)
    if not no_policy_in_raw:
        raise SourceDrift("W07_PUBLIC_METHOD_CHANGED_REVIEW_REQUIRED")

    return {
        "w07_public_reattach_mutates_body_and_identity": True,
        "w07_legacy_ui_still_calls_raw_reattach": True,
        "w08_optional_safe_route_exists": True,
        "w09_optional_safe_route_exists": True,
        "w09_all_layer_audit_called_only_by_optional_route": True,
        "w09_disables_old_behaviours_but_not_public_method": True,
        "public_method_has_its_own_guard": False,
        "all_call_routes_are_guarded": False,
        "verdict": "API_BYPASS_HOLD",
        "native_unity_physx": "NOT_RUN",
    }

def receipt(s):
    return {
        "schema": "b01-62-p17w10-public-api-route-v1",
        "pins": {k: {"commit": a, "path": b, "blob": c} for k, (a, b, c) in sorted(REVISIONS.items())},
        "checks": inspect(s),
        "gate": "G0_SOURCE_REPRODUCED__G1_UNITY_NOT_RUN__G2_INTEGRATION_HOLD",
        "codex_ack": False,
        "merge_performed": False,
    }

def render(s):
    return (json.dumps(receipt(s), sort_keys=True, indent=2, ensure_ascii=True) + "\n").encode()

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--repo", required=True)
    ap.add_argument("--output", required=True)
    args = ap.parse_args()
    path = Path(args.output)
    require(path.parent.is_dir() and not path.exists(), "OUTPUT_MUST_BE_NEW")
    output = render(original_sources(args.repo))
    path.write_bytes(output)
    print("B01_62_W10=API_BYPASS_HOLD")
    print("RECEIPT_SHA256=" + hashlib.sha256(output).hexdigest())
    print("UNITY_G1=NOT_RUN")

if __name__ == "__main__":
    main()
