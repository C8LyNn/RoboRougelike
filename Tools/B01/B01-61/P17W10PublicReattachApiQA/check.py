#!/usr/bin/env python3
"""B01-61 W10 exact Git-object negative contract for newly protected #471.
--evidence PASS means the regression is proven, NOT that the game is safe.
--release exits 3 while the release blocker exists.  Git+stdlib only.
"""
import argparse
import hashlib
import json
import re
import subprocess
import sys
import unittest

CHAIN = {
    "w07": ("cdcaea750fda1a9d7db6018e34970f4b9f206427",
            "Assets/B0123DebrisPlayground001/Runtime/DebrisRigPlayground.cs",
            "e4e31c32cb5abda2fef987efa4b45fd61e828312"),
    "w08": ("99dfbd25d471f9983f9472b12d3725f4c002f7e9",
            "Assets/B0123ReattachClearance001/Runtime/DebrisSafeReattachGate.cs",
            "7c7e96a10d69149119309863a0c2a9bdab829072"),
    "w09": ("763748f1be0c61356fb4462144640bb359364226",
            "Assets/B0123DockHardening001/Runtime/HardenedDockDemo.cs",
            "d46c6341db199d010f48de00f98250becd2d7c87"),
}

def command(*args):
    return subprocess.check_output(["git", *args], stderr=subprocess.PIPE)

def original_git_object(sha, path, expected):
    actual = command("rev-parse", sha + ":" + path).decode().strip()
    if actual != expected:
        raise ValueError("SOURCE_BLOB_DRIFT " + path + " " + actual)
    raw = command("show", sha + ":" + path)
    actual_bytes = hashlib.sha1(
        b"blob " + str(len(raw)).encode() + b"\\x00" + raw).hexdigest()
    if actual_bytes != expected:
        raise ValueError("SOURCE_BYTES_DRIFT " + path)
    return raw.decode("utf-8")

def method(source, signature):
    begin = source.find(signature)
    if begin < 0:
        raise ValueError("METHOD_NOT_FOUND: " + signature)
    opening = source.find("{", begin)
    if opening < 0:
        raise ValueError("METHOD_NO_BODY: " + signature)
    nesting = 1
    for pos in range(opening + 1, len(source)):
        if source[pos] == "{":
            nesting += 1
        elif source[pos] == "}":
            nesting -= 1
            if nesting == 0:
                return source[opening+1:pos]
    raise ValueError("METHOD_UNBALANCED " + signature)

def inspect(w07, w08, w09):
    raw = method(w07, "public bool Reattach(int index)")
    guarded_w08 = method(w08, "public bool TrySafeReattach(DebrisRigPlayground demo, int index)")
    guarded_w09 = method(w09, "public bool TryDock(int index)")
    arm = method(w09, "public bool Arm()")

    original_mutation = all(token in raw for token in (
        "rig.Body.isKinematic = true", "rig.Part.transform.SetParent(",
        "rig.Emitter.RearmAfterReattach("))
    direct_protection = any(token in raw for token in (
        "Inspect(", "TryDock(", "TrySafeReattach(", "GuardReattach(",
        "Clearance(", "DockPermit(", "ValidateDock("))
    old_gate = ("Inspect(demo, index) != DockVerdict.Allowed" in guarded_w08)
    w09_gate = ("Inspect(index) != HardenedDockVerdict.Allowed" in guarded_w09
                and "demo.Reattach(index)" in guarded_w09)
    gui_disabled = "demo.enabled = false" in arm and "previousGate.enabled = false" in arm
    return {
        "legacy_public_reattach_can_mutate_without_check": original_mutation and not direct_protection,
        "w08_optional_gate_present": old_gate,
        "w09_optional_gate_present": w09_gate,
        "w09_disables_gui_but_not_public_api": gui_disabled,
        "external_api_bypass": original_mutation and not direct_protection and old_gate and w09_gate and gui_disabled,
    }

class DetectionSelfTests(unittest.TestCase):
    def setUp(self):
        self.w07 = """public bool Reattach(int index) {
          rig.Body.isKinematic = true;
          rig.Part.transform.SetParent(rig.Root.transform, false);
          rig.Emitter.RearmAfterReattach(rig.Definition.PartId); return true;
        }"""
        self.w08 = """public bool TrySafeReattach(DebrisRigPlayground demo, int index) {
          if (Inspect(demo, index) != DockVerdict.Allowed) return false;
          return demo.Reattach(index);
        }"""
        self.w09 = """public bool Arm() { demo.enabled = false;
          previousGate.enabled = false; return true; }
        public bool TryDock(int index) {
          if (Inspect(index) != HardenedDockVerdict.Allowed) return false;
          return demo.Reattach(index);
        }"""

    def test_detect_original_public_api_escape(self):
        self.assertTrue(inspect(self.w07, self.w08, self.w09)["external_api_bypass"])

    def test_reject_if_public_api_itself_checks_clearance(self):
        fixed = self.w07.replace(
            "rig.Body.isKinematic = true",
            "if (Inspect(index) != DockVerdict.Allowed) return false; rig.Body.isKinematic = true")
        self.assertFalse(inspect(fixed, self.w08, self.w09)["external_api_bypass"])

    def test_missing_w09_gate_is_not_accidentally_accepted(self):
        changed = self.w09.replace("Inspect(index) != HardenedDockVerdict.Allowed", "false")
        self.assertFalse(inspect(self.w07, self.w08, changed)["external_api_bypass"])

    def test_only_disabling_gui_does_not_repair_public_api(self):
        self.assertTrue(inspect(self.w07, self.w08, self.w09)["legacy_public_reattach_can_mutate_without_check"])

    def test_missing_original_mutating_rearm_is_not_classified_as_same_defect(self):
        changed = self.w07.replace("rig.Emitter.RearmAfterReattach(", "rig.Emitter.NoRearm(")
        self.assertFalse(inspect(changed, self.w08, self.w09)["external_api_bypass"])

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--mode", choices=("unit", "evidence", "release"), required=True)
    args = parser.parse_args()
    if args.mode == "unit":
        suite = unittest.defaultTestLoader.loadTestsFromTestCase(DetectionSelfTests)
        return 0 if unittest.TextTestRunner(verbosity=2).run(suite).wasSuccessful() else 1
    try:
        sources = {name: original_git_object(*params) for name, params in CHAIN.items()}
        observed = inspect(sources["w07"], sources["w08"], sources["w09"])
        print("GIT_SOURCE_PINS=3/3 PASS", flush=True)
        print("G0_OBSERVATION=" + json.dumps(observed, sort_keys=True), flush=True)
        if observed["external_api_bypass"]:
            print("SOURCE_NEGATIVE_REPRO=CONFIRMED W09 protected GUI leaves direct W07 public API unsafe; UNITY=NOT_RUN", flush=True)
            return 3 if args.mode == "release" else 0
        print("SOURCE_NEGATIVE_REPRO=NOT_OBSERVED; check changed source / new owner SHA", flush=True)
        return 1 if args.mode == "evidence" else 0
    except (subprocess.CalledProcessError, OSError, UnicodeError, ValueError) as exc:
        print("SOURCE_PROVENANCE_BLOCKED " + str(exc), file=sys.stderr, flush=True)
        return 2

if __name__ == "__main__":
    sys.exit(main())
