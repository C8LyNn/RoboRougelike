#!/usr/bin/env python3
"""B01-41: Git-object-only fail-closed contract for real native Unity CI wiring.

This program DOES NOT execute Unity and never inspects GitHub secrets.
It prevents a CI configuration accidentally reporting complete tests when a
runner/check/activation/evidence stage has been removed or short-circuited.
Only the canonical, committed CI-02/03/04 Git objects are read.
"""
from __future__ import annotations
import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

COMMIT = re.compile(r"[0-9a-f]{40}\Z")
ACTIVATION = ("UNITY_EMAIL", "UNITY_PASSWORD", "UNITY_LICENSE", "UNITY_SERIAL")
FILES = {
    "editmode": ".github/workflows/ci-02-editmode.yml",
    "playmode": ".github/workflows/ci-03-playmode.yml",
    "windows-build": ".github/workflows/ci-04-build.yml",
}
CHECKOUT = "actions/checkout@11d5960a326750d5838078e36cf38b85af677262"
TEST_RUNNER = "game-ci/unity-test-runner@32e57712352b500e17974b245a6dce9e11a73213"
BUILDER = "game-ci/unity-builder@4423ee75828dff56037d1d1cfdac6b68e3a6ba00"
UPLOAD = "actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02"
VERSION = "6000.3.14f1"
STEP = re.compile(r"(?m)^      - (?=name:|uses:)")
MAX_FILE = 96_000

class UnsafeContract(Exception):
    pass

def command(repo: Path, *arguments: str) -> bytes:
    try:
        result = subprocess.run(["git", "-C", str(repo), *arguments],
                                capture_output=True, check=False)
    except OSError as exc:
        raise UnsafeContract("GIT_UNAVAILABLE") from exc
    if result.returncode:
        raise UnsafeContract("GIT_OBJECT_NOT_AVAILABLE")
    return result.stdout

def require_commit(repo: Path, sha: str) -> None:
    if not COMMIT.fullmatch(sha):
        raise UnsafeContract("EXACT_COMMIT_SHA_REQUIRED")
    actual = command(repo, "rev-parse", "--verify", sha+"^{commit}").decode("ascii").strip()
    if actual != sha:
        raise UnsafeContract("COMMIT_SHA_MISMATCH")

def object_text(repo: Path, sha: str, path: str) -> str:
    blob = command(repo, "show", sha+":"+path)
    if not blob or len(blob) > MAX_FILE or b"\0" in blob:
        raise UnsafeContract("MISSING_OR_TOO_LARGE_WORKFLOW:"+path)
    try:
        return blob.decode("utf-8")
    except UnicodeDecodeError as exc:
        raise UnsafeContract("NON_UTF8_WORKFLOW:"+path) from exc

def check(value: bool, code: str) -> None:
    if not value:
        raise UnsafeContract(code)

def must(section: str, pattern: str, reason: str) -> None:
    check(re.search(pattern, section, re.MULTILINE) is not None, reason)

def validate(kind: str, body: str) -> dict:
    path = FILES[kind]
    # Deliberately reject surprising layouts rather than guessing YAML semantics.
    must(body, r"^on:\s*$", "MISSING_GITHUB_EVENT_GATE:"+kind)
    must(body, r"^  push:\s*$", "MISSING_PUSH_TRIGGER:"+kind)
    must(body, r"^  workflow_dispatch:\s*$", "MISSING_MANUAL_TRIGGER:"+kind)
    if kind != "windows-build":
        must(body, r"^  pull_request:\s*$", "MISSING_PR_TRIGGER:"+kind)
    must(body, r"^    branches: \[main, develop\]\s*$",
         "NATIVE_BRANCH_SCOPE_DRIFT:"+kind)
    must(body, r"^permissions:\s*\n  contents: read\s*$",
         "UNSAFE_WORKFLOW_PERMISSIONS:"+kind)
    check("continue-on-error: true" not in body, "CONTINUE_ON_ERROR_BYPASS:"+kind)
    check(not re.search(r"(?m)^  (?:workflow_call|schedule):",body),
          "UNREVIEWED_TRIGGER:"+kind)

    parts = STEP.split(body)
    check(len(parts) == 7, "UNEXPECTED_NATIVE_STEP_COUNT:"+kind)
    steps = [body[match.start():end.start() if end else len(body)]
             for match,end in zip(list(STEP.finditer(body)),
                                  list(STEP.finditer(body))[1:]+[None])]
    check(len(steps)==6, "MISSING_NATIVE_STEPS:"+kind)
    for pos,step in enumerate(steps[:-1]):
        check(not re.search(r"(?m)^        if:", step),
              "CONDITIONAL_NATIVE_STEP_BYPASS:"+kind+":"+str(pos))
    checkout, preflight, activation, native, outcome, evidence = steps
    must(checkout, r"^      - uses: "+re.escape(CHECKOUT)+r"\b", "UNPINNED_CHECKOUT:"+kind)
    must(checkout, r"^          persist-credentials: false\s*$",
         "CHECKOUT_CREDENTIAL_LEAK:"+kind)
    must(preflight, r"^        run: python3 Tools/CI/validate_repository\.py\s*$",
         "MISSING_REPOSITORY_VALIDATOR:"+kind)
    must(activation, r"^      - name: Require valid Unity activation credentials\s*$",
         "MISSING_ACTIVATION_GATE:"+kind)
    must(activation, r"^        run: \|\s*$", "ACTIVATION_SCRIPT_NOT_SHELL:"+kind)
    for secret in ACTIVATION:
        must(activation, r"^          "+secret+r": \$\{\{ secrets\."+secret+r" \}\}\s*$",
             "ACTIVATION_SECRET_BINDING_MISSING:"+kind+":"+secret)
    must(activation, r"(?m)^            exit 1\s*$", "ACTIVATION_NOT_FAIL_CLOSED:"+kind)
    must(activation, r"BLOCKED: Unity NOT RUN\.", "ACTIVATION_BLOCKER_EVIDENCE_MISSING:"+kind)
    must(activation, r"\[ -z \"\$UNITY_EMAIL\" \]", "ACTIVATION_EMAIL_CHECK_MISSING:"+kind)
    must(activation, r"\[ -z \"\$UNITY_PASSWORD\" \]", "ACTIVATION_PASSWORD_CHECK_MISSING:"+kind)
    must(activation, r"\[ -z \"\$UNITY_LICENSE\" \]", "ACTIVATION_LICENSE_CHECK_MISSING:"+kind)
    must(activation, r"\[ -z \"\$UNITY_SERIAL\" \]", "ACTIVATION_SERIAL_CHECK_MISSING:"+kind)
    check(not re.search(r"(?m)^\s*echo.*\$UNITY_(?:PASSWORD|LICENSE|SERIAL)",activation),
          "ACTIVATION_SECRET_ECHO:"+kind)
    check("|| true" not in activation and "exit 0" not in activation,
          "ACTIVATION_SHELL_BYPASS:"+kind)

    if kind in ("editmode","playmode"):
        must(native, r"^        uses: "+re.escape(TEST_RUNNER)+r"\b", "MISSING_PINNED_UNITY_TEST_RUNNER:"+kind)
        must(native, r"^          unityVersion: "+re.escape(VERSION)+r"\s*$", "UNITY_VERSION_DRIFT:"+kind)
        must(native, r"^          testMode: "+kind+r"\s*$", "NATIVE_TEST_MODE_DRIFT:"+kind)
        must(native, r"^          artifactsPath: Artifacts/CI/"+("EditMode" if kind=="editmode" else "PlayMode")+r"\s*$",
             "NATIVE_TEST_ARTIFACT_PATH_DRIFT:"+kind)
        must(outcome, r"^        run: python3 Tools/CI/check_results\.py Artifacts/CI/"+
             ("EditMode/editmode-results" if kind=="editmode" else "PlayMode/playmode-results")+
             r"\.xml\s*$", "NATIVE_TEST_RESULT_CHECK_REMOVED:"+kind)
    else:
        must(native, r"^        uses: "+re.escape(BUILDER)+r"\b", "MISSING_PINNED_UNITY_BUILDER")
        must(native, r"^          targetPlatform: StandaloneWindows64\s*$", "WRONG_NATIVE_BUILD_TARGET")
        must(native, r"^          unityVersion: "+re.escape(VERSION)+r"\s*$", "UNITY_VERSION_DRIFT:build")
        must(outcome, r"^        run: test -s build/StandaloneWindows64/RoboRougelike\.exe\s*$",
             "NATIVE_PLAYER_OUTPUT_CHECK_REMOVED")
    must(native, r"^          projectPath: \.\s*$", "WRONG_NATIVE_UNITY_PROJECT:"+kind)
    for secret in ACTIVATION:
        must(native, r"^          "+secret+r": \$\{\{ secrets\."+secret+r" \}\}\s*$",
             "NATIVE_RUNNER_SECRET_BINDING_MISSING:"+kind+":"+secret)
    must(evidence, r"^        if: always\(\)\s*$", "LOSS_OF_FAILURE_EVIDENCE:"+kind)
    must(evidence, r"^        uses: "+re.escape(UPLOAD)+r"\b", "UNPINNED_EVIDENCE_UPLOAD:"+kind)
    must(evidence, r"^          if-no-files-found: warn\s*$", "MISSING_MISSING_ARTIFACT_WARNING:"+kind)
    must(evidence, r"^          path: (?:\||Artifacts/CI/)\s*$", "MISSING_LOG_OUTPUT_MAPPING:"+kind)
    check("Artifacts/CI/" in evidence, "MISSING_NATIVE_LOGS:"+kind)
    return {"file":path,"gate":"SOURCE_CONTRACT_PASS","native_unity":"NOT_RUN",
            "step_count":len(steps),"kind":kind}

def run(repo: Path, sha: str) -> dict:
    require_commit(repo,sha)
    results=[]
    for kind,path in FILES.items():
        results.append(validate(kind,object_text(repo,sha,path)))
    return {"package":"CI41-NATIVE-CONTRACT-001","version":"1.0.0",
            "source_sha":sha,"result":"SOURCE_CONTRACT_PASS","workflows":results,
            "native_unity":"NOT_RUN","game_g1":"NOT_ASSESSED",
            "codex_ack":"NOT_ASSESSED","release_decision":"NO_AUTHORIZATION"}

def main() -> int:
    parser=argparse.ArgumentParser()
    parser.add_argument("--repo",default=".")
    parser.add_argument("--head-sha",required=True)
    parser.add_argument("--out")
    args=parser.parse_args()
    try:
        result=run(Path(args.repo).resolve(),args.head_sha)
    except (UnsafeContract, ValueError, OSError) as exc:
        result={"package":"CI41-NATIVE-CONTRACT-001","result":"FAIL_CLOSED",
                "blocker":str(exc),"native_unity":"NOT_RUN","game_g1":"NOT_ASSESSED"}
    output=json.dumps(result,indent=2,sort_keys=True)+"\n"
    if args.out:
        Path(args.out).write_text(output,encoding="utf-8")
    print(output,end="")
    return 2 if result["result"]=="FAIL_CLOSED" else 0

if __name__=="__main__":
    sys.exit(main())
