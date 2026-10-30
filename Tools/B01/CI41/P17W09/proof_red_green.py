#!/usr/bin/env python3
"""Read immutable R1 Git source and show three real unsafe classifications corrected by R2."""
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[4]
ORIGINAL = "432acd34ca12378f512fa5e2384afbe1ff041b7b"
SOURCE = "Tools/B01/CI41/P17W08/test_impact_plan.py"
PINNED_BLOB = "463edd27d74d7dc0b178a2d651ff57a95c0ce89e"
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from test_test_impact_plan import PhysicalGitImpactTests, A

def run():
    actual = subprocess.run(["git", "-C", str(ROOT), "rev-parse", "--verify", ORIGINAL+"^{commit}"],
                            capture_output=True, text=True)
    if actual.returncode or actual.stdout.strip() != ORIGINAL:
        raise RuntimeError("FROZEN_ORIGINAL_COMMIT_UNAVAILABLE")
    raw = subprocess.run(["git", "-C", str(ROOT), "show", ORIGINAL+":"+SOURCE],
                         capture_output=True, check=True).stdout
    blob = subprocess.run(["git", "-C", str(ROOT), "hash-object", "--stdin"],
                          input=raw, capture_output=True, check=True).stdout.decode().strip()
    if blob != PINNED_BLOB:
        raise RuntimeError("FROZEN_ORIGINAL_SOURCE_BLOB_MISMATCH:"+blob)
    with tempfile.TemporaryDirectory(prefix="ci41-frozen-source-") as d:
        file = Path(d)/"original_r1.py"
        file.write_bytes(raw)
        spec = importlib.util.spec_from_file_location("original_r1", file)
        old = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(old)
        prefix="Assets/RoboRougelikeMechanics/"+A+"/Runtime/"
        cases=[
            ("canonical_native_playmode_ci_gate", ".github/workflows/ci-03-playmode.yml",
             "name: bypass", "SOURCE_ONLY_NO_GAME_ASSETS_CHANGED"),
            ("unity_script_meta_guid", prefix+"Engine.cs.meta",
             "fileFormatVersion: 2\\nguid: "+"f"*32, "TARGETED_PRECHECK_PLUS_FULL_G1_LATER"),
            ("unity_shader_asset", prefix+"Renderer.shader",
             'Shader "Changed" {}', "TARGETED_PRECHECK_PLUS_FULL_G1_LATER"),
        ]
        evidence=[]
        for name,path,data,expected_old in cases:
            fixture=PhysicalGitImpactTests("test_01_original_physical_two_module_inventory")
            fixture.setUp()
            try:
                r2,_,head=fixture.report(change={path:data})
                inv_r1=old.inventory(fixture.repo,head)
                old_result=old.decide(inv_r1,old.changed_paths(fixture.repo,fixture.base,head),
                                      fixture.base,head)
                if old_result["mode"] != expected_old:
                    raise AssertionError(name+": unexpected original status "+old_result["mode"])
                if r2["mode"] != "FULL_UNITY_SUITE_REQUIRED" or not r2["full_suite_required_now"]:
                    raise AssertionError(name+": R2 did not escalate")
                evidence.append({"case":name,"original_mode":old_result["mode"],
                                 "revised_mode":r2["mode"],"original_git_blob":PINNED_BLOB,
                                 "native_unity":"NOT_RUN","result":"RED_TO_GREEN_CONFIRMED"})
            finally:
                fixture.tearDown()
        out=HERE/"red-green-receipt.json"
        out.write_text(json.dumps({"source_sha":ORIGINAL,"cases":evidence,
                          "execution":"real Git and Python; NOT Unity G1"},indent=2)+"\n",encoding="utf-8")
        print("IMMUTABLE_ORIGINAL_R1_THREE_REAL_RED_TO_R2_GREEN_CASES_PASS")
        return 0

if __name__=="__main__":
    try:
        sys.exit(run())
    except Exception as e:
        print("FAIL_CLOSED: "+repr(e),file=sys.stderr)
        sys.exit(2)
