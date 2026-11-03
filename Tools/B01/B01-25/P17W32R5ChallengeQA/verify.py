#!/usr/bin/env python3
"""W32 canonical R5 source-only owner/scope guard. NOT a Unity gameplay test."""
import pathlib
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
        "arena.Build(R5ArenaKind.SalvageYard)" in s and
        "collider.bounds.Intersects(envelope)" in s and
        "Physics.gravity.y" in s and
        "new EnterPlayMode()" in s
    )

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
    meta = (ROOT / META).read_text(encoding="utf-8")
    m = re.search(r"^guid:\s*([0-9a-f]{32})\s*$", meta, re.M)
    assert m, "Missing legal Unity meta GUID"
    count = 0
    for p in (ROOT / "Assets").rglob("*.meta"):
        if re.search(r"^guid:\s*" + m.group(1) + r"\s*$", p.read_text(errors="replace"), re.M):
            count += 1
    assert count == 1, "GUID collision: " + str(count)
    arena = (ROOT / PRODUCTION[0]).read_text(encoding="utf-8")
    assert "JumpPad(new Vector3(7f,0f,-16f))" in arena
    assert "PlayerSpawn => new Vector3(0,.7f,-22)" in arena
    assert "if(kind==R5ArenaKind.SalvageYard)" in arena
    assert "UNITY" in (ROOT / DOC).read_text(encoding="utf-8").upper()
    print("B01_25_W32_SOURCE_SCOPE_PASS: 5 own files, 2 authored native cases, "
          "3 immutable production source blobs, one unique GUID, 2 negative mutations")
    print("UNITY_EDITMODE=NOT_RUN PLAYMODE=NOT_RUN WINDOWS_PLAYER=NOT_BUILT")

if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print("B01_25_W32_SOURCE_SCOPE_RED:", exc, file=sys.stderr)
        raise
