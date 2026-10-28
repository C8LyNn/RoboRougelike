#!/usr/bin/env python3
"""B01-22 PASS8: run OLD security contracts on ORIGINAL Codex PR310 C# sources.

Independent QA only. The original PR302/303 offline repro remains byte-immutable.
Expected RED means DEFECT_STILL_OPEN, never product/Unity success.
"""
import hashlib
import json
import re
import shutil
import subprocess
import tempfile
from pathlib import Path
from urllib.request import urlopen

HERE = Path(__file__).resolve().parent
REPO = "C8LyNn/RoboRougelike"
OLD = "c89744d5ab43f4a795abe62527d94439be573693"
NEW = "231005c6d0351945a8ee6684f3856af118f0e94f"
INPUTS = {
    "Assets/R3/R3VehicleModel.cs": (
        "6d8b1c01449f531970f2e284154ddfce911151a8",
        "9f1bf190c7535182c6ce375cbfe67f5b2862e811"),
    "Assets/R4/R4WeaponDefinition.cs": (
        "f4044c053f5aebb055b8ba0df0026ea156246e44",
        "502f6d7f70d4e8078260de632ab181f244a4d730"),
}

def blob_sha(raw):
    return hashlib.sha1(b"blob " + str(len(raw)).encode() + b"\0" + raw).hexdigest()

def body(source, signature):
    found = re.search(signature, source)
    if not found:
        raise AssertionError("MISSING_OR_CHANGED_METHOD_SIGNATURE: " + signature)
    first = source.find("{", found.end())
    if first < 0:
        raise AssertionError("NO_METHOD_BODY: " + signature)
    depth = 0
    for pos in range(first, len(source)):
        if source[pos] == "{":
            depth += 1
        elif source[pos] == "}":
            depth -= 1
            if depth == 0:
                return source[first:pos + 1].replace("\r\n", "\n")
    raise AssertionError("UNBALANCED_BRACES: " + signature)

def run(args, cwd, allowed=(0,)):
    p = subprocess.run(args, cwd=cwd, text=True, capture_output=True,
                       check=False, timeout=180)
    print("RUN", " ".join(str(x) for x in args), "exit", p.returncode)
    print((p.stdout + p.stderr)[-2500:])
    if p.returncode not in allowed:
        raise RuntimeError("EXECUTABLE_FAILED: " + str(args) + " exit=" + str(p.returncode))
    return p.returncode

def main():
    evidence = Path("artifacts/chl-econ/codex310-r3-security-differential.json").resolve()
    evidence.parent.mkdir(parents=True, exist_ok=True)
    fetched = {}
    for path, (old_sha, new_sha) in INPUTS.items():
        original = (HERE / "_pinned" / path).read_bytes()
        if blob_sha(original) != old_sha:
            raise RuntimeError("ORIGINAL_QA_SOURCE_MISMATCH: " + path)
        url = "https://raw.githubusercontent.com/" + REPO + "/" + NEW + "/" + path
        with urlopen(url, timeout=40) as response:
            changed = response.read()
        if blob_sha(changed) != new_sha:
            raise RuntimeError("NEW_CODEX_SOURCE_MISMATCH: " + path)
        fetched[path] = (original, changed)
        print("PINNED_CODEX_ORIGINAL_SOURCE_VERIFIED", path, old_sha, new_sha)

    targets = {
        "Assets/R3/R3VehicleModel.cs": [
            r"\bpublic bool ReceiveSalvage\s*\(",
            r"\bpublic bool RepairInventory\s*\(",
            r"\bpublic bool Dismantle\s*\(",
            r"\bpublic int DismantleValue\s*\(",
        ],
        "Assets/R4/R4WeaponDefinition.cs": [
            r"\bpublic static R4WeaponDefinition Get\s*\(",
        ],
    }
    unchanged = []
    for path, signatures in targets.items():
        before, after = (raw.decode("utf-8") for raw in fetched[path])
        for signature in signatures:
            if "DismantleValue" in signature:
                # This method is an expression-bodied single-line method.
                expr = r"\bpublic int DismantleValue\s*\([^;\r\n]+;"
                old_match, new_match = re.search(expr, before), re.search(expr, after)
                if old_match is None or new_match is None or old_match.group() != new_match.group():
                    raise AssertionError("DismantleValue changed; require full independent review")
            elif body(before, signature) != body(after, signature):
                raise AssertionError("RELEVANT_METHOD_CHANGED: " + signature)
            unchanged.append(signature)
    print("5_OF_5_RELEVANT_FUNCTION_BODIES_UNCHANGED_AFTER_PR303")

    with tempfile.TemporaryDirectory(prefix="b01-22-pr310-diff-") as temp:
        stage = Path(temp) / "Weapon302"
        stage.mkdir()
        for name in ("OutsideUnityApi.cs", "Program.cs", "SecurityContract.cs",
                     "RealWeaponEconomyQA.csproj", "SecurityContract.csproj"):
            shutil.copy2(HERE / name, stage / name)
        for path, (_, changed) in fetched.items():
            target = stage / "_pinned" / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(changed)
        run(["dotnet", "build", "RealWeaponEconomyQA.csproj", "-c", "Release", "--nologo"], stage)
        positive = stage / "positive.json"
        run(["dotnet", "run", "--project", "RealWeaponEconomyQA.csproj",
             "-c", "Release", "--no-build", "--", str(positive)], stage)
        witness = json.loads(positive.read_text("utf-8"))
        if (witness.get("total") != 9 or witness.get("tests_fail") != 0 or
                witness.get("confirmed_defect_reproductions") != 3):
            raise AssertionError("ORIGINAL_SOURCE_WITNESS_NOT_9_OF_9_WITH_3_REPRODUCED")
        run(["dotnet", "build", "SecurityContract.csproj", "-c", "Release", "--nologo"], stage)
        strict_file = stage / "strict.json"
        code = run(["dotnet", "run", "--project", "SecurityContract.csproj",
                    "-c", "Release", "--no-build", "--", str(strict_file)], stage, (1,))
        strict = json.loads(strict_file.read_text("utf-8"))
        actual_cases = {x.get("id"):x.get("status") for x in strict.get("cases", [])}
        expected = {
            "E01_REJECT_INVALID_ENUM_BEFORE_SALVAGE_COMMIT":"FAIL",
            "E02_PREEXISTING_CORRUPT_REPAIR_IS_FAIL_CLOSED_ATOMIC":"FAIL",
            "E03_PREEXISTING_CORRUPT_DISMANTLE_NEVER_LOSES_ITEM":"FAIL",
        }
        if code != 1 or strict.get("security_contract_fail") != 3 or (
            strict.get("security_contract_pass") != 0 or actual_cases != expected
        ):
            raise AssertionError("ACTUAL_SECURITY_STATUS_CHANGED_REVIEW_MANUALLY: " + json.dumps(strict))
        receipt = {
            "task_id":"B01-22-W46-PR310-INVALID-R4WEAPONKIND-DELTA",
            "codex_input_pr":310,
            "codex_input_sha":NEW,
            "baseline_pr303_sha":OLD,
            "git_blobs":{p:{"old":v[0],"new":v[1]} for p,v in INPUTS.items()},
            "source_methods_exact_unchanged":len(unchanged),
            "original_source_witness_cases_pass":9,
            "defect_reproduced_cases":3,
            "security_contract_green":0,
            "security_contract_red":3,
            "strict_test_exit":code,
            "result":"DEFECT_STILL_OPEN",
            "historical_offline_bundle_modified":False,
            "new_reproduction_package_created":False,
            "unity_native":"NOT_RUN",
            "real_player_save_load":"NOT_RUN",
            "codex_original_source_modified":False,
        }
        evidence.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print("CODEX_PR310_DIFFERENTIAL_ACTUAL_9_OF_9_WITNESS_3_OF_3_RED_NO_FIX")
        print(json.dumps(receipt, sort_keys=True))

if __name__ == "__main__":
    main()
