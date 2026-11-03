#!/usr/bin/env python3
"""B01-50 W32 independent original-game source scope/fixture check, NOT native Unity."""
from __future__ import annotations
import pathlib
import re
import subprocess
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[4]
BASE = "3bd9b159997924dcecb4e1cd1d41137896e5199e"
NATIVE = "Assets/Tests/Editor/B0150W32RealGarageProfilerTests.cs"
META = NATIVE + ".meta"
README = "Tools/B01/B01-50/W32LiveGarageProfile001/README.md"
VERIFIER = "Tools/B01/B01-50/W32LiveGarageProfile001/verify.py"
WORKFLOW = ".github/workflows/b01-50-w32-live-garage-profile.yml"
MANIFEST = "Tools/B01/B01-50/W32LiveGarageProfile001/MANIFEST-GIT-BLOBS.tsv"
GUID = "d7a253c614c3419bac8ae3d68f4a1e32"
OWN = {NATIVE, META, README, VERIFIER, WORKFLOW, MANIFEST}
PINS = {
    "Assets/R3/R3BuildValidation.cs": "996a2de80039103538700020cca38bc0ede6076a",
    "Assets/R3/R3RestorationValidation.cs": "0d31d7201957d36b637a9be58574ca6c4c5d1dbf",
    "Assets/R3/R3VehicleCatalog.cs": "f76de5bbdcc1dbdb7ddb9fe9859c36c232fd8584",
    "Assets/R3/R3GarageView.cs": "1976a19516905be63259109ec1af0a92ef52d6f4",
}
REQUIRED = (
    "UnityTest", "EditorSceneManager.OpenScene", "EnterPlayMode",
    "ExitPlayMode", "R1RunController.Instance", "R3VehicleKind.Standard",
    "OpenSandboxGarage", "GarageRemove", "R4WeaponKind.RotarySaw",
    "OwnedPlacementError", "GC.GetAllocatedBytesForCurrentThread",
    "Stopwatch.GetTimestamp", "SafeSawCell", "Assert.That",
    "B0150_W32_REAL_R3_GARAGE_PROFILE", "beforeScrap",
    "beforeInventory", "beforeIds", "yield return null",
    "UNPINNED_NOT_ACCEPTABLE_FOR_G1",
    "R3VehicleKind.Light", "R3VehicleKind.Heavy",
    "ActualLight20PartR3GaragePlacementLatencyAndAllocations",
    "ActualHeavy139PartR3GaragePlacementLatencyAndAllocations",
    "B0150_W33_REAL_R3_GARAGE_SCALE",
    "baseline_parts=", "samples=32", "ProfileCatalogScale",
    "R3GarageView", "view.ChooseInventory(id)", "view.Select(cell)",
    "view.PreviewValid", "view.HasTarget",
    "B0150_W34_REAL_R3_HEAVY_GARAGE_VISUAL",
    "heavy139_hover_ui",
)

def git(*args):
    p = subprocess.run(["git", *args], cwd=ROOT, text=True, capture_output=True)
    if p.returncode:
        raise RuntimeError(f"git {' '.join(args)}: {p.stderr.strip()}")
    return p.stdout.strip()

def check_fixture(source):
    for marker in REQUIRED:
        if marker not in source:
            raise ValueError("Missing real native profiling invariant: " + marker)
    if source.count("[UnityTest]") != 4:
        raise ValueError("Expected exactly four genuine native fixtures (20/83/139 + Heavy139 UI)")
    if source.count("OwnedPlacementError(") < 4:
        raise ValueError("No repeated actual game validation paths")
    if "GarageBuy(" in source or "GarageMount(" in source or "File.Write" in source:
        raise ValueError("Read-only profile must never spend/write player state")
    if "GC.Collect(" in source or "Thread.Sleep(" in source:
        raise ValueError("Manual global garbage collection/timing interference")
    if "yield return null" not in source:
        raise ValueError("No frame retirement between temporary geometry probes")

def check_ci_workflow(workflow):
    if "on:\n  pull_request:" not in workflow.replace("\r\n", "\n"):
        raise ValueError("Duplicate or absent source-only PR trigger")
    if re.search(r"(?m)^  push:\s*$", workflow):
        raise ValueError("W34 must not duplicate push+pull_request runner jobs")
    for required in (
        "cancel-in-progress: true",
        "actions/checkout@11d5960a326750d5838078e36cf38b85af677262",
        "actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02",
        "matrix.os",
    ):
        if required not in workflow:
            raise ValueError("Missing pinned/original job efficiency requirement: " + required)
    if "actions/checkout@v4" in workflow or "actions/upload-artifact@v4" in workflow:
        raise ValueError("Mutable Action tag forbidden in B01-50 W34 CI")

def run():
    script=(ROOT/NATIVE).read_text(encoding="utf-8")
    check_ci_workflow((ROOT/WORKFLOW).read_text(encoding="utf-8"))
    check_fixture(script)
    meta=(ROOT/META).read_text(encoding="utf-8")
    if re.findall(r"(?m)^guid: ([0-9a-f]{32})\s*$", meta) != [GUID]:
        raise ValueError("Own Unity GUID malformed or changed")
    guid_count=0
    for f in (ROOT/"Assets").rglob("*.meta"):
        if f.is_symlink():
            raise ValueError("Unexpected symlink meta: "+str(f))
        if f.read_text(encoding="utf-8-sig", errors="replace").find("guid: "+GUID) >= 0:
            guid_count+=1
    if guid_count != 1:
        raise ValueError(f"Unity import GUID collision count={guid_count}")
    source=(ROOT/"Assets/R3/R3BuildValidation.cs").read_text(encoding="utf-8")
    if "Physics.OverlapBox(" not in source or "Physics.OverlapSphereNonAlloc(" not in source:
        raise ValueError("Current existing R3 source allocation/functional safety path not pinned")
    if "new HashSet<Collider>()" not in source or "Installed()" not in source:
        raise ValueError("Current original R3 allocation path changed; re-review measurement")
    for path,expected in PINS.items():
        actual=git("hash-object","--no-filters",path)
        if actual!=expected:
            raise ValueError(f"Current original game Git blob mismatch: {path} {actual}")
    rows=(ROOT/MANIFEST).read_text(encoding="utf-8").strip().splitlines()
    seen=set()
    for row in rows:
        sha,path=row.split("\t")
        if path in seen or path not in OWN-{MANIFEST}:
            raise ValueError("Duplicate/foreign manifest path: "+path)
        if git("hash-object","--no-filters",path)!=sha:
            raise ValueError("Own original Git blob mismatch: "+path)
        seen.add(path)
    if seen!=OWN-{MANIFEST}:
        raise ValueError("Manifest incomplete: "+repr(sorted((OWN-{MANIFEST})-seen)))
    statuses=git("diff","--name-status",BASE,"HEAD").splitlines()
    actual=set()
    for row in statuses:
        status,path=row.split("\t")
        if status!="A" or path not in OWN:
            raise ValueError("Foreign or non-additive change: "+row)
        actual.add(path)
    if actual!=OWN:
        raise ValueError("Not exactly six original new B01-50 paths: "+repr(sorted(actual)))
    print("B0150_W34_ORIGINAL_SOURCE_PASS six ADD-only, five exact own blobs, four R3 original game pins; four genuine authored Unity tests (20/83/139 plus Heavy139 real UI)")
    print("B0150_W32_TRUE_NATIVE_PROFILE_SOURCE_PRESENT; UNITY_EDITOR_NOT_RUN; WINDOWS_PLAYER_NOT_BUILT; GPU_FPS_NOT_MEASURED")

class NativeFixtureNegativeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source=(ROOT/NATIVE).read_text(encoding="utf-8")
        cls.workflow=(ROOT/WORKFLOW).read_text(encoding="utf-8")
    def check_rejects(self, a,b):
        self.assertIn(a,self.source)
        with self.assertRaises(ValueError):
            check_fixture(self.source.replace(a,b))
    def test_no_fake_real_scene(self):
        self.check_rejects("EditorSceneManager.OpenScene","EditorSceneManager.Disabled")
    def test_no_fake_real_garage(self):
        self.check_rejects("OpenSandboxGarage","FakeGarage")
    def test_no_fake_measurement(self):
        self.check_rejects("GC.GetAllocatedBytesForCurrentThread","GC.GetTotalMemory")
    def test_no_fake_r3_entrypoint(self):
        self.check_rejects("OwnedPlacementError","InventedPlacementError")
    def test_no_missing_provenance_warning(self):
        self.check_rejects("UNPINNED_NOT_ACCEPTABLE_FOR_G1","NATIVE_PASS")
    def test_no_fake_playmode(self):
        self.check_rejects("EnterPlayMode","UnknownMode")
    def test_no_fake_testframe(self):
        self.check_rejects("yield return null","yield break")
    def test_no_removed_original_light_scale(self):
        self.check_rejects("R3VehicleKind.Light", "R3VehicleKind.Standard")
    def test_no_removed_original_heavy_scale(self):
        self.check_rejects("R3VehicleKind.Heavy", "R3VehicleKind.Standard")
    def test_no_removed_actual_scale_metrics(self):
        self.check_rejects("B0150_W33_REAL_R3_GARAGE_SCALE", "SYNTHETIC_FAKE_SCALE")
    def test_no_fake_original_ui_receiver(self):
        self.check_rejects("R3GarageView", "FakeGarageView")
    def test_no_unmeasured_visual_preflight(self):
        self.check_rejects("view.PreviewValid", "fakePreviewValid")
    def test_no_missing_visual_measurements(self):
        self.check_rejects("B0150_W34_REAL_R3_HEAVY_GARAGE_VISUAL", "UNSUPPORTED_FAKE_METRIC")
    def test_no_duplicate_push_event(self):
        self.assertIn("  pull_request:", self.workflow)
        with self.assertRaises(ValueError):
            check_ci_workflow(self.workflow.replace("  pull_request:", "  push:"))
    def test_no_mutable_checkout_tag(self):
        with self.assertRaises(ValueError):
            check_ci_workflow(self.workflow.replace(
                "actions/checkout@11d5960a326750d5838078e36cf38b85af677262",
                "actions/checkout@v4"))
    def test_no_removed_stale_run_cancellation(self):
        with self.assertRaises(ValueError):
            check_ci_workflow(self.workflow.replace("cancel-in-progress: true", "cancel-in-progress: false"))
    def test_original_valid_source(self):
        check_fixture(self.source)
        check_ci_workflow(self.workflow)

if __name__ == "__main__":
    if "--selftest" in sys.argv:
        unittest.main(argv=[sys.argv[0]],verbosity=2)
    else:
        run()
