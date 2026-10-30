#!/usr/bin/env python3
"""B01-60 W09: execute frozen W07 independent QA plus new 384-boundary matrix against exact source Git bytes.

BASELINE (no candidate): validate reproducible historical RED, never label a fix PASS.
CANDIDATE (40-hex SHA): validate only a B01-25-owned descendant whose gameplay source changed.
No worktree patching and no Codex/producer file writes. Only generated test-source copies.
"""
from __future__ import annotations
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[5]
HERE = Path(__file__).resolve().parent
EVIDENCE = ROOT / "qa-w09-hazard-evidence"
OLD = "6095577ece27320b1a861a738cc23ba42d6793a6"
W07 = "5dc838f3342083ebc78bbf8704053b996dac4a8a"
W07_BLOB = "e65ca6134e0730c1a91a7f98cc1da286f6ff08f6"
OLD_BLOB = "a7a2c69c80d43bf027f2b00896df72f892fa1a17"
CORE = "Research/B01-25/P17/HAZARDPULSE001/PulseCore.cs"
W07_RUNNER = "Tools/B01/B01-60/P17W07/HazardPulseBoundaryQA/Program.cs"
CI = ".github/workflows/b01-60-p17w09-hazard-phase-rework.yml"
QA = "Tools/B01/B01-60/P17W09/HazardPhaseReworkGate/"
OWN = {QA + f for f in (
    "BoundaryMatrix.cs", "Matrix.csproj", "FrozenW07.csproj",
    "retarget.py", "README.md", "manifest.json")} | {CI}
ALLOWED_OWNER = ("Research/B01-25/P17/HAZARDPULSE001/",)
ALLOWED_WORKFLOW = ".github/workflows/b01-25-w06-hazardpulse.yml"

def git(*params: str) -> bytes:
    return subprocess.check_output(["git", "-C", str(ROOT), *params],
                                   stderr=subprocess.STDOUT)

def oid(commit: str, path: str) -> str:
    return git("rev-parse", commit + ":" + path).decode().strip()

def fail_unless(condition: bool, reason: str):
    if not condition:
        raise AssertionError(reason)

def save(name: str, data: str):
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    (EVIDENCE / name).write_text(data, encoding="utf-8")

def execute(*argv: str, label: str) -> tuple[int,str]:
    p = subprocess.run(argv, cwd=ROOT, encoding="utf-8",
                       stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    save(label + ".txt", "EXIT_CODE=" + str(p.returncode) + "\n" + p.stdout)
    print(label + " EXIT_CODE=" + str(p.returncode))
    for line in p.stdout.splitlines():
        if any(s in line for s in ("W09_", "INDEPENDENT QA:", "FAIL FRAMERATE_",
                                    "Build succeeded.", "Build FAILED", "error CS")):
            print("  " + line[-330:])
    return p.returncode, p.stdout

def main():
    cand = os.environ.get("CANDIDATE_SOURCE_SHA", "").strip()
    candidate = bool(cand)
    if candidate:
        fail_unless(re.fullmatch(r"[a-f0-9]{40}", cand) is not None,
                    "candidate must be a literal immutable 40-char Git commit SHA")
        fail_unless(cand != OLD, "candidate SHA is frozen original known RED")
    changed = git("diff", "--name-only", OLD, "HEAD").decode().splitlines()
    fail_unless(set(changed) == OWN, "W09_QA_SCOPE_COLLISION " + repr(changed))
    # Confirm old immutable original truly remains unmodified on this QA branch:
    old_core = git("show", OLD + ":" + CORE)
    fail_unless(oid(OLD,CORE) == OLD_BLOB, "ORIGINAL_CORE_BLOB_MISMATCH")
    fail_unless(oid("HEAD",CORE) == OLD_BLOB, "QA_MUTATED_ORIGINAL_PRODUCER")
    w07_bytes = git("show", W07 + ":" + W07_RUNNER)
    fail_unless(oid(W07,W07_RUNNER) == W07_BLOB, "FROZEN_W07_REPRODUCER_CHANGED")
    producer_original = git("ls-tree", "-r", "--name-only", OLD,
                            "Research/B01-25/P17/HAZARDPULSE001/").decode().splitlines()
    fail_unless(len(producer_original) == 9, "ORIGINAL_9_FILES_NOT_FOUND")
    for name in producer_original:
        fail_unless(oid(OLD,name)==oid("HEAD",name), "QA_MUTATED_ORIGINAL "+name)

    source_sha = cand if candidate else OLD
    if candidate:
        subprocess.run(["git", "-C", str(ROOT), "merge-base", "--is-ancestor", OLD, cand],
                       check=True)
        diff = git("diff", "--name-only", OLD, cand).decode().splitlines()
        fail_unless(bool(diff) and all(
            f.startswith(ALLOWED_OWNER) or f==ALLOWED_WORKFLOW for f in diff),
            "UNTRUSTED_CANDIDATE_SCOPE: "+repr(diff))
        fail_unless(oid(cand,CORE)!=OLD_BLOB, "CANDIDATE_HAS_OLD_BUGGY_SOURCE_BLOB")
    source = git("show", source_sha + ":" + CORE)
    # Generated files are deliberately ephemeral, solely in the B01-60 test directory.
    # No permanent source copy of B01-25 or W07 test is committed.
    (HERE/"GeneratedPulseCore.cs").write_bytes(source)
    (HERE/"FrozenW07Program.generated.cs").write_bytes(w07_bytes)
    info = {"qa_role":"B01-60","wave":"PASS17-W09",
            "mode":"CANDIDATE" if candidate else "KNOWN_RED_BASELINE",
            "source_sha":source_sha,
            "original_sha":OLD,
            "original_blob":OLD_BLOB,
            "source_blob":oid(source_sha,CORE),
            "original_producer_git_files":len(producer_original),
            "frozen_w07_test_sha":W07,
            "frozen_w07_program_blob":W07_BLOB,
            "qa_owned_paths":len(changed),
            "native_unity":"NOT_RUN",
            "CODEX_ACK":"PENDING"}
    save("provenance.json",json.dumps(info,sort_keys=True,indent=2)+"\n")
    try:
        for project in ("FrozenW07.csproj", "Matrix.csproj"):
            code,_=execute("dotnet","build",str(HERE/project),"-c","Release",
                           "--warnaserror",label="build-"+project[:-7])
            fail_unless(code==0,"REAL_CSHARP9_COMPILATION_FAILED: "+project)
        env = dict(os.environ, B01_60_MODE="CANDIDATE" if candidate else "BASELINE")
        def run_project(proj: str,label: str):
            p = subprocess.run(["dotnet","run","--no-build","-c","Release",
                                "--project",str(HERE/proj)],
                               cwd=ROOT,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,
                               encoding="utf-8",env=env)
            save(label+".txt","EXIT_CODE="+str(p.returncode)+"\n"+p.stdout)
            print(label+" EXIT_CODE="+str(p.returncode))
            for line in p.stdout.splitlines():
                if "W09_" in line or "INDEPENDENT QA:" in line or line.startswith("FAIL"):
                    print("  "+line[-300:])
            return p.returncode,p.stdout
        result_old,logs_old=run_project("FrozenW07.csproj","frozen-W07-original-10-tests")
        result_matrix,logs_matrix=run_project("Matrix.csproj","W09-384-boundary-matrix")
        if candidate:
            fail_unless(result_old==0 and
                        "B01-60 INDEPENDENT QA: 10 PASS / 0 FAIL" in logs_old,
                        "CANDIDATE_W07_10_OF_10_NOT_GREEN")
            fail_unless(result_matrix==0 and
                        "B01_60_W09_CANDIDATE_MATRIX_ACCEPTED" in logs_matrix,
                        "CANDIDATE_ADDITIONAL_MATRIX_RED")
            state="CANDIDATE_SOURCE_GREEN"
        else:
            fail_unless(result_old!=0 and
                        "B01-60 INDEPENDENT QA: 9 PASS / 1 FAIL" in logs_old and
                        "FAIL FRAMERATE_BOUNDARY_0_1S_v_0_25S" in logs_old,
                        "FROZEN_W07_EXPECTED_RED_NOT_REPRODUCED")
            fail_unless(result_matrix==0 and
                        "B01_60_W09_EXPECTED_ORIGINAL_RED_CONFIRMED" in logs_matrix,
                        "W09_EXPECTED_ORIGINAL_RED_NOT_REPRODUCED")
            state="ORIGINAL_SOURCE_RED_CONFIRMED_NOT_FIXED"
        print("B01_60_W09_RESULT="+state+" original="+OLD+
              " test_source="+source_sha+
              " NATIVE_UNITY_G1=NOT_RUN INTEGRATED=NO")
        save("verdict.json",json.dumps({**info,"source_verdict":state,
               "original_W07_exit_code":result_old,
               "new_W09_matrix_exit_code":result_matrix},sort_keys=True,indent=2)+"\n")
    finally:
        # Ephemeral copies never become a producer patch or part of the Draft's diff.
        (HERE/"GeneratedPulseCore.cs").unlink(missing_ok=True)
        (HERE/"FrozenW07Program.generated.cs").unlink(missing_ok=True)
    return 0

if __name__=="__main__":
    try:
        raise SystemExit(main())
    except Exception as error:
        print("B01_60_W09_QA_GATE_FAILED "+repr(error),file=sys.stderr)
        raise SystemExit(2)
