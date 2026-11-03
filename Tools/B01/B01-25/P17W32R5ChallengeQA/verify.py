#!/usr/bin/env python3
"""W32 canonical R5 source-only owner/scope guard. NOT a Unity gameplay test."""
import pathlib
import math
import re
import subprocess
import sys

BASE = "3bd9b159997924dcecb4e1cd1d41137896e5199e"
ROOT = pathlib.Path(__file__).resolve().parents[4]
TEST = "Assets/Tests/Editor/B0125W32RealArenaChallengeTests.cs"
META = TEST + ".meta"
DOC = "Docs/W32/B01-25_R5_ARENA_CHALLENGE_QA.md"
CHECK = "Tools/B01/B01-25/P17W32R5ChallengeQA/verify.py"
WORKFLOW = ".github/workflows/b01-25-w32-r5-challenge-source.yml"
PRODUCTION = (
    "Assets/R5/R5Arena.cs",
    "Assets/B01Packages/MapJumpPad001/Runtime/MapJumpPad.cs",
    "Assets/Tests/Editor/W29LiveArenaJumpPadTests.cs",
    "Assets/R3/R3VehicleCatalog.cs",
    "Assets/R3/R3VehicleAssembly.cs",
    "Assets/PackageIntegration/RRLHazardAdapter.cs",
)
EXPECTED = {TEST, META, DOC, CHECK, WORKFLOW}
NAMES = (
    "AllThreeExistingR5ArenasRetainWideCentralSpawnToCombatCorridor",
    "RealSalvageYardJumpPadHasWideIngressAndPredictedClearTouchdown",
)

def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT).decode().strip()

def valid_suite(s):
    return s.count("[UnityTest]") == 2 and all(n in s for n in NAMES) and (
        "arena.Build(kind)" in s and
        "R3VehicleCatalog.Create(kind)" in s and
        "foreach (R3VehicleKind vehicle" in s and
        "const float originalGridMeters = .55f;" in s and
        "const float horizontalMarginMeters = .30f;" in s and
        "design.Parts.Count, Is.EqualTo(expected)" in s and
        "new Vector3(3.4f" not in s and
        "arena.Build(R5ArenaKind.SalvageYard)" in s and
        "collider.bounds.Intersects(envelope)" in s and
        "Physics.gravity.y" in s and
        "TelegraphedHazardZone" in s and
        "hazard.Radius" in s and
        "hazards.Length, Is.EqualTo(2)" in s and
        "dx * dx + dz * dz" in s and
        "new EnterPlayMode()" in s
    )

# W34 deterministic XZ hazard-clearance oracle sourced from the REAL
# production R5 arena positions and live RRLHazardAdapter radius. This runs
# on Win/Ubuntu CI without pretending to execute Unity/PhysX.
def live_hazard_geometry_clear(arena, adapter, catalog, assembly):
    # Fail closed if this original game contract changes: no silent stale oracle.
    required = (
        "int width = light ? 1 : heavy ? 3 : 2;",
        "int length = light ? 2 : heavy ? 4 : 3;",
        "side * (width + 1)",
        "light ? i * 2 - 1 : i - 3",
    )
    if any(token not in catalog for token in required):
        return False
    if "item.transform.localPosition=(Vector3)state.Cell*0.55f;" not in assembly:
        return False

    def parse_vector(expr):
        try:
            fields = expr.split(",", 2)
            if len(fields) != 3:
                return None
            return tuple(float(v.strip().rstrip("f")) for v in fields)
        except ValueError:
            return None

    def get_vector(prefix, source):
        index = source.find(prefix)
        if index < 0:
            return None
        after = source[index + len(prefix):].split(")", 1)[0]
        return parse_vector(after)

    radius_text = adapter.split("zone.Configure(", 1)
    if len(radius_text) != 2:
        return False
    try:
        radius = float(radius_text[1].split(",", 1)[0].strip().rstrip("f")) + .05
    except ValueError:
        return False
    fragments = arena.split("Hazard(new Vector3(")[1:]
    if len(fragments) != 2:
        return False
    hazards = [parse_vector(part.split(")", 1)[0]) for part in fragments]
    if None in hazards:
        return False
    hazard_xz = [(v[0], v[2]) for v in hazards]
    spawn_line = arena.split("PlayerSpawn =>", 1)
    spawn = get_vector("new Vector3(", spawn_line[1].split(";", 1)[0]) if len(spawn_line) == 2 else None
    pad = get_vector("JumpPad(new Vector3(", arena)
    if spawn is None or pad is None:
        return False
    if "launch.Configure(8.5f,2f,1.25f)" not in arena:
        return False

    # Nominal XZ grid envelopes derived from original R3 factory for all three
    # factory types; real mesh extrema can be larger and demand native QA.
    for width, length in ((1, 2), (2, 3), (3, 4)):
        half_x = ((2 * (width + 1) + 1) * .55 + .30) / 2
        half_z = ((2 * length + 1) * .55 + .30) / 2
        samples = [(0.0, float(z)) for z in range(-22, 19, 2)]
        # Existing SalvageYard real spawn -> JumpPad and nominal landing.
        # Gravity 9.81 is a source-level estimate, NOT a Unity trajectory run.
        landing_z = pad[2] + 2.0 * (2.0 * 8.5 / 9.81)
        for step in range(13):
            t = step / 12.0
            samples.append((spawn[0] + (pad[0] - spawn[0]) * t,
                            spawn[2] + (pad[2] - spawn[2]) * t))
            samples.append((pad[0], pad[2] + (landing_z - pad[2]) * t))
        for x, z in samples:
            for hx, hz in hazard_xz:
                dx = max(0.0, abs(hx - x) - half_x)
                dz = max(0.0, abs(hz - z) - half_z)
                if dx * dx + dz * dz <= radius * radius:
                    return False
    return True


def main():
    changed = set(git("diff", "--name-only", BASE, "HEAD").splitlines())
    assert changed == EXPECTED, "Foreign source or incomplete QA diff: " + repr(changed ^ EXPECTED)
    for path in PRODUCTION:
        assert (ROOT / path).read_bytes() == subprocess.check_output(
            ["git", "show", BASE + ":" + path], cwd=ROOT
        ), "Canonical production path unexpectedly changed: " + path
    source = (ROOT / TEST).read_text(encoding="utf-8")
    assert valid_suite(source), "Missing real R5 NUnit acceptance source"
    assert not valid_suite(source.replace("[UnityTest]", "", 1)), "Mutation guard: removed NUnit not rejected"
    assert not valid_suite(source.replace("Physics.gravity.y", "1f")), "Mutation guard: fake gravity not rejected"
    assert not valid_suite(source.replace("R3VehicleCatalog.Create(kind)", "FakeCatalog.Create(kind)")), "Mutation guard: fake factory not rejected"
    assert not valid_suite(source.replace("const float originalGridMeters = .55f;", "const float originalGridMeters = 0.1f;")), "Mutation guard: shrunken grid not rejected"
    meta = (ROOT / META).read_text(encoding="utf-8")
    m = re.search(r"^guid:\s*([0-9a-f]{32})\s*$", meta, re.M)
    assert m, "Missing legal Unity meta GUID"
    count = 0
    for p in (ROOT / "Assets").rglob("*.meta"):
        if re.search(r"^guid:\s*" + m.group(1) + r"\s*$", p.read_text(errors="replace"), re.M):
            count += 1
    assert count == 1, "GUID collision: " + str(count)
    arena = (ROOT / PRODUCTION[0]).read_text(encoding="utf-8")
    adapter = (ROOT / "Assets/PackageIntegration/RRLHazardAdapter.cs").read_text(encoding="utf-8")
    catalog = (ROOT / "Assets/R3/R3VehicleCatalog.cs").read_text(encoding="utf-8")
    assembly = (ROOT / "Assets/R3/R3VehicleAssembly.cs").read_text(encoding="utf-8")
    assert live_hazard_geometry_clear(arena, adapter, catalog, assembly), (
        "Real R5 hazard zones intersect factory vehicle travel envelope"
    )
    assert not live_hazard_geometry_clear(
        arena.replace("Hazard(new Vector3(-6,0,-6));",
                      "Hazard(new Vector3(0,0,-6));"), adapter, catalog, assembly
    ), "Mutation guard: hazard moved into central lane not rejected"
    assert not live_hazard_geometry_clear(
        arena, adapter.replace("zone.Configure(1.5f", "zone.Configure(5f"),
        catalog, assembly
    ), "Mutation guard: hazard radius expansion not rejected"
    assert not live_hazard_geometry_clear(
        arena.replace("Hazard(new Vector3(6,0,8));", ""), adapter,
        catalog, assembly
    ), "Mutation guard: missing second hazard not rejected"
    assert "JumpPad(new Vector3(7f,0f,-16f))" in arena
    assert "PlayerSpawn => new Vector3(0,.7f,-22)" in arena
    assert "if(kind==R5ArenaKind.SalvageYard)" in arena
    assert "UNITY" in (ROOT / DOC).read_text(encoding="utf-8").upper()
    print("B01_25_W32_SOURCE_SCOPE_PASS: 5 own files, 2 authored native cases, "
          "6 immutable production source blobs, one unique GUID, 7 negative mutations and real R5 hazard geometry")
    print("UNITY_EDITMODE=NOT_RUN PLAYMODE=NOT_RUN WINDOWS_PLAYER=NOT_BUILT")

if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print("B01_25_W32_SOURCE_SCOPE_RED:", exc, file=sys.stderr)
        raise
