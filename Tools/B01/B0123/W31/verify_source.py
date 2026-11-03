#!/usr/bin/env python3
"""Exact-source W31 preview GC regression; source-only, NOT a Unity test."""
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE = "56c874aeac19d7724d7cddeaabc7a6a4f82c08d4"
RUNTIME = "Assets/R3/R3BuildValidation.cs"
TESTS = "Assets/Tests/Editor/GarageEditFreedomTests.cs"
OWN = {RUNTIME, TESTS, "Tools/B01/B0123/W31/verify_source.py",
       ".github/workflows/b01-23-w31-garage-preview-source.yml"}

def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT, text=True, encoding="utf-8")

src = (ROOT / RUNTIME).read_text(encoding="utf-8")
test = (ROOT / TESTS).read_text(encoding="utf-8")
parent = git("show", BASE + ":" + RUNTIME)
old_tests = git("show", BASE + ":" + TESTS)
head = git("rev-parse", "HEAD").strip()

def invariant(s, t):
    return {
        "same_canonical_gate": "canonical.SetInstallationGate(candidate=>gate!=null?gate.Validate(candidate)" in s,
        "static_BUILD001": "new BuildPhysicsEvidence(candidateColliders,neighbors.ToArray(),true)" in s,
        "original_mount_reverse": "ExistingMountedWeaponPlacementIssue(probe,candidateColliders)" in s,
        "restoration_owner_ids": "ignoredOwnerIds!=null&&ignoredOwnerIds.Contains(gun.Id)" in s,
        "fixed_256": "const int FunctionalQueryCapacity=256;" in s,
        "bounded_physx": s.count("Physics.OverlapSphereNonAlloc(") == 2 and "Physics.OverlapCapsuleNonAlloc(" in s and "Physics.SphereCastNonAlloc(" in s,
        "fail_closed": "if(count>=functionalOverlap.Length)" in s and "if(count>=FunctionalQueryCapacity)" in s,
        "original_legacy_fixture": "ExistingDeadLegacySawCannotRepairOrEmergencyRepair" in t and "new GameObject[260]" in t,
        "original_reverse_fixture": "NewArmorCannotBlockExistingSawAfterGaragePurchase" in t,
        "cached_same_defaults": "readonly RotarySawConfiguration defaultSawClearance=new RotarySawConfiguration();" in s and s.count("var config=defaultSawClearance;") == 2,
        "no_hotpath_default_ctor": "var config=new RotarySawConfiguration();" not in s,
        "nonalloc_culling": "bool candidateNearBarrel=false;" in s and "if(!candidateNearBarrel)continue;" in s and "proposed.Any(c=>" not in s,
        "new_actual_scene_test": "RepeatedSawPreviewMaintainsPhysicalRefusalAndWallet" in t and "frame<32;frame++" in t,
        "scrap_and_ids_guard": "Is.EqualTo(initialScrap)" in t and "Is.EqualTo(initialIds)" in t,
    }

checks = invariant(src, test)
assert all(checks.values()), {k:v for k,v in checks.items() if not v}
old_field = "        readonly RaycastHit[] functionalCast=new RaycastHit[FunctionalQueryCapacity];"
new_field = old_field + """
        // R3 currently mounts WPN005 with its standard shared disc dimensions.
        // Avoid re-allocating this read-only-used configuration for every ghost hover.
        readonly RotarySawConfiguration defaultSawClearance=new RotarySawConfiguration();"""
old_loop = """                if(!proposed.Any(c=>c!=null&&c.enabled&&
                    c.bounds.Intersects(segmentBounds)))continue;"""
new_loop = """                // Repeated garage ghost refreshes must not allocate a LINQ
                // closure per previously-mounted directional gun.
                bool candidateNearBarrel=false;
                for(int candidateIndex=0;candidateIndex<proposed.Length;candidateIndex++)
                {
                    var collider=proposed[candidateIndex];
                    if(collider!=null&&collider.enabled&&
                       collider.gameObject.activeInHierarchy&&
                       collider.bounds.Intersects(segmentBounds))
                    {
                        candidateNearBarrel=true;
                        break;
                    }
                }
                if(!candidateNearBarrel)continue;"""
assert src.count(new_field) == 1 and src.count(new_loop) == 1
restored = src.replace(new_field, old_field).replace(
    "var config=defaultSawClearance;", "var config=new RotarySawConfiguration();"
).replace(new_loop, old_loop)
assert restored == parent, "W31 changed unexpected original gameplay code"
start = test.index("    // W31: the actual live garage repeatedly evaluates")
end = test.index("    [UnityTest] public IEnumerator ExistingDeadLegacySawCannotRepairOrEmergencyRepair()",start)
assert test[:start] + test[end:] == old_tests, "W31 altered existing NUnit source"

mutants = [
    src.replace("var config=defaultSawClearance;", "var config=new RotarySawConfiguration();"),
    src.replace("if(!candidateNearBarrel)continue;", "// unsafe accepted candidate"),
    src.replace("if(count>=FunctionalQueryCapacity)", "if(count>FunctionalQueryCapacity)"),
]
assert all(not all(invariant(s, test).values()) for s in mutants), "Source mutation escaped source gate"
changed = git("diff", "--name-only", BASE, "HEAD").splitlines()
assert set(changed) == OWN, "Foreign paths or missing W31 files: " + repr(changed)
print(json.dumps({"level":"SOURCE_ONLY_NOT_UNITY","head":head,"base":BASE,
                  "checks":len(checks),"passed":sum(checks.values()),
                  "mutants_rejected":len(mutants),"paths":changed}, sort_keys=True))
