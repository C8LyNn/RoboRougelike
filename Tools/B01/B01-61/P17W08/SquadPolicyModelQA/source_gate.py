#!/usr/bin/env python3
"""B01-61 independent QA source/scope gate for immutable B01-24 #429."""
import hashlib
import json
import pathlib
import subprocess

ROOT = pathlib.Path(__file__).resolve().parents[5]
PRODUCER = "4d13f76e6c0c08033e305900daf61c0a7267a725"
PIN = {
    "Research/B01-24/P17/W06-SQUAD-PRESSURE-001/SquadPressure.cs":
        "3402fe61153e307ba164340956afd76aec6e8603",
    "Assets/R3/R3EnemyArchetype.cs":
        "66cd1ee734b129117b3753d1205469a0496cb673",
}
QA = "Tools/B01/B01-61/P17W08/SquadPolicyModelQA/"
ALLOWED = {
    QA + "Program.cs", QA + "ModelQA.csproj", QA + "source_gate.py",
    QA + "README.md", QA + "manifest.json",
    ".github/workflows/b01-61-p17w08-squad-policy-model-qa.yml",
}

def git(*args):
    return subprocess.check_output(["git", "-C", str(ROOT), *args], text=True).strip()

def main():
    manifest = json.loads((ROOT / QA / "manifest.json").read_text(encoding="utf-8"))
    assert manifest["original_pr"] == 429 and manifest["original_sha"] == PRODUCER
    assert manifest["source_pins"] == PIN and set(manifest["owned_paths"]) == ALLOWED
    for path, blob in PIN.items():
        assert git("rev-parse", PRODUCER + ":" + path) == blob, "producer ref drift: " + path
        assert git("hash-object", path) == blob, "working source drift: " + path
        assert git("rev-parse", "HEAD:" + path) == blob, "branch source drift: " + path
    files = set(git("diff", "--name-only", PRODUCER, "HEAD").splitlines())
    assert files == ALLOWED, "owner scope violation: " + repr(sorted(files ^ ALLOWED))
    assert len(files) == 6
    print("PASS original runtime and Codex C# blobs 2/2")
    print("PASS unchanged source from frozen #429 SHA " + PRODUCER)
    print("PASS exact six QA-only paths, no gameplay/Codex/producer overwrite")
    print("UNITY_NATIVE_NOT_RUN")

if __name__ == "__main__":
    main()
