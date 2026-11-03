#!/usr/bin/env python3
"""W30 exact source guard. This does NOT compile or run Unity or PhysX."""
import hashlib
import json
import subprocess
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
BASE="d537b7310979b91df27a08b14d0f1c74d0889089"
RUNTIME="Assets/R3/R3BuildValidation.cs"
TESTS="Assets/Tests/Editor/GarageEditFreedomTests.cs"
ALLOWED={RUNTIME,TESTS,"Docs/W29/GARAGE_FUNCTIONAL_SWEEP_GATE.md",
         "Docs/W30/GARAGE_BOUNDED_PHYSICS_RECEIPT.md",
         "Tools/B01/Codex/W30/verify_garage.py",
         ".github/workflows/b01-w30-garage-source.yml"}

def checks(src,tests):
    return {
        "one_original_install_gate":"canonical.SetInstallationGate(candidate=>gate!=null?gate.Validate(candidate)" in src,
        "same_preview_buy_path":"return FunctionalWeaponPlacementIssue(candidate,probe);" in src,
        "original_BUILD001_before_sweep":src.find("BuildPlacementRules.CheckForInstallation") < src.find("return FunctionalWeaponPlacementIssue(candidate,probe);"),
        "physx_noalloc_buffers":"const int FunctionalQueryCapacity=256;" in src and src.count("Physics.OverlapSphereNonAlloc(")==2,
        "physx_bounded_cast":"Physics.SphereCastNonAlloc(" in src and "readonly RaycastHit[] functionalCast=" in src,
        "saw_original_cut_disc":"RotarySawContactMath.IsOnCuttingDisc(" in src and "muzzle.right,config.bladeRadius" in src,
        "refuse_truncated_overlaps":src.count(">=functionalOverlap.Length")==2 and ">=functionalCast.Length" in src,
        "three_fail_closed_reasons":src.count("przepełnienie bufora")==3 and src.count("(montaż zablokowany)")==3,
        "no_allocating_old_queries":"Physics.OverlapSphere(" not in src and "Physics.SphereCastAll(" not in src,
        "limited_aim_segment":"functionalCast,delta.magnitude" in src,
        "area_EMP_preserved":"candidate.Weapon==R4WeaponKind.EmpEmitter)return null;" in src,
        "original_physical_gate_preserved":"new BuildPhysicsEvidence(candidateColliders,neighbors.ToArray(),true)" in src,
        "test_real_game_scene":'EditorSceneManager.OpenScene("Assets/scene 1.unity"' in tests,
        "test_actual_scrap":"Assert.That(run.VehicleModel.Scrap,Is.EqualTo(beforeWallet));" in tests,
        "test_actual_part_id":"Assert.That(run.VehicleModel.Inventory.Single(p=>p.Id==id).Id,Is.EqualTo(id));" in tests,
        "test_260_physical_colliders":"new GameObject[260]" in tests and 'Does.Contain("przepełnienie")' in tests,
        "test_original_saw_collision":'Does.Contain("tarcza piły")' in tests,
        "test_recovery_actual_mount":"Assert.That(run.GarageMount(id,exterior,default),Is.True);" in tests
    }

src=(ROOT/RUNTIME).read_text(encoding="utf-8-sig")
tests=(ROOT/TESTS).read_text(encoding="utf-8-sig")
original=checks(src,tests)
assert all(original.values()),repr({k:v for k,v in original.items() if not v})
mutants={
    "disable_saturation_guard":(src.replace(">=functionalOverlap.Length",">functionalOverlap.Length"),tests),
    "remove_saw_disc":(src.replace("RotarySawContactMath.IsOnCuttingDisc(","NO_SAW_DISC("),tests),
    "revert_nonalloc_cast":(src.replace("Physics.SphereCastNonAlloc(","Physics.SphereCastAll("),tests),
    "remove_negative_test":(src,tests.replace('Does.Contain("przepełnienie")','Is.Null'))
}
for label,(bad_src,bad_test) in mutants.items():
    assert not all(checks(bad_src,bad_test).values()),"Guard accepted mutant "+label
paths=subprocess.check_output(["git","diff","--name-only",BASE,"HEAD"],cwd=ROOT,text=True).splitlines()
assert not (set(paths)-ALLOWED), "foreign files "+repr(sorted(set(paths)-ALLOWED))
assert {RUNTIME,TESTS}.issubset(set(paths))
receipt={"kind":"SOURCE_ONLY_NOT_UNITY","base":BASE,
         "head":subprocess.check_output(["git","rev-parse","HEAD"],cwd=ROOT,text=True).strip(),
         "checks":len(original),"passed":sum(original.values()),"mutants_rejected":len(mutants),
         "file_sha256":{p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in paths},
         "files_changed":paths}
print(json.dumps(receipt,sort_keys=True,ensure_ascii=False))
