#!/usr/bin/env python3
"""B01-60: fail-closed paired RC1/RC2 Windows GUI exit-crash recorder.

This is independent QA tooling, not Unity native execution or a game patch.
"""
import argparse
import datetime as dt
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import subprocess
import sys
import time
import zipfile

SCHEMA = "b01-60-rc1-shutdown-ab-v1"
AV_CODE = 0xC0000005
MAX_ARCHIVE_BYTES = 1_500_000_000
MAX_MEMBERS = 20000
MAX_ENTRY_BYTES = 600_000_000
HEX64 = re.compile(r"[0-9a-f]{64}\Z")
HEX40 = re.compile(r"[0-9a-f]{40}\Z")


def utc():
    return dt.datetime.now(dt.timezone.utc).isoformat()


def dump(path, obj):
    target = Path(path)
    target.parent.mkdir(parents=True, exist_ok=True)
    with target.open("x", encoding="utf-8") as file:  # never overwrite evidence
        json.dump(obj, file, sort_keys=True, indent=2)
        file.write("\n")


def load(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def digest(path):
    h = hashlib.sha256()
    with Path(path).open("rb") as file:
        for b in iter(lambda: file.read(1024 * 1024), b""):
            h.update(b)
    return h.hexdigest()


def normname(raw):
    if not isinstance(raw, str) or not raw or "\\" in raw or "\x00" in raw or ":" in raw:
        raise ValueError("UNSAFE_ZIP_PATH")
    if raw.startswith("/") or "//" in raw:
        raise ValueError("UNSAFE_ZIP_PATH")
    name = raw.rstrip("/")
    parts = name.split("/")
    if not parts or any(p in ("..", ".", "") for p in parts):
        raise ValueError("UNSAFE_ZIP_PATH")
    return name


def archive_entries(zfile):
    names = set()
    total = 0
    infos = zfile.infolist()
    if not infos or len(infos) > MAX_MEMBERS:
        raise ValueError("ZIP_MEMBER_LIMIT")
    for info in infos:
        name = normname(info.filename)
        # ZIP unix symlink bits: mode type 0o120000
        if (info.external_attr >> 16) & 0o170000 == 0o120000:
            raise ValueError("ZIP_SYMLINK")
        lower = name.casefold()
        if lower in names:
            raise ValueError("ZIP_CASE_DUPLICATE")
        names.add(lower)
        if info.file_size < 0 or info.file_size > MAX_ENTRY_BYTES:
            raise ValueError("ZIP_ENTRY_TOO_LARGE")
        total += info.file_size
        if total > MAX_ARCHIVE_BYTES:
            raise ValueError("ZIP_TOTAL_TOO_LARGE")
    return infos


def verify_plan(plan):
    if plan.get("schema") != SCHEMA or plan.get("unity_version") != "6000.3.14f1":
        raise ValueError("PLAN_SCHEMA_OR_UNITY")
    builds = plan.get("builds")
    order = plan.get("order")
    if not isinstance(builds, dict) or set(builds) != {"BASE", "EXP", "RC2"}:
        raise ValueError("REQUIRE_THREE_PINNED_BUILDS")
    if order != ["BASE", "EXP", "EXP", "BASE", "RC2", "RC2"]:
        raise ValueError("MUST_USE_BALANCED_ABBA_RC2_ORDER")
    unique_shas = set()
    for label, b in builds.items():
        if not isinstance(b, dict) or not HEX64.fullmatch(b.get("sha256", "")):
            raise ValueError("BAD_ARCHIVE_SHA:" + label)
        if not HEX40.fullmatch(b.get("source_sha", "")):
            raise ValueError("BAD_SOURCE_SHA:" + label)
        if b["sha256"] in unique_shas:
            raise ValueError("SAME_ARCHIVE_TWO_VARIANTS")
        unique_shas.add(b["sha256"])
        for key in ("archive", "exe"):
            if not isinstance(b.get(key), str) or not b[key].strip():
                raise ValueError("MISSING_BUILD_FIELD:" + key)
        if not b["exe"].casefold().endswith(".exe") or b["exe"] != normname(b["exe"]):
            raise ValueError("UNSAFE_EXE_PATH:" + label)
        if "unknown" in b["source_sha"].lower():
            raise ValueError("UNPINNED_SOURCE")
    if not isinstance(plan.get("manual_instructions"), str) or len(plan["manual_instructions"]) < 12:
        raise ValueError("MANUAL_INSTRUCTIONS_REQUIRED")
    return plan


def prepare(plan_file, evidence):
    plan = verify_plan(load(plan_file))
    evidence = Path(evidence).resolve()
    if evidence.exists():
        raise ValueError("EVIDENCE_FOLDER_ALREADY_EXISTS")
    # Validate archive bytes and ZIP before creating any execution state.
    zips = {}
    for label, build in plan["builds"].items():
        path = Path(build["archive"]).resolve()
        if not path.is_file() or digest(path) != build["sha256"]:
            raise ValueError("ARCHIVE_SHA_MISMATCH:" + label)
        with zipfile.ZipFile(path) as z:
            infos = archive_entries(z)
            if build["exe"].casefold() not in {normname(i.filename).casefold() for i in infos if not i.is_dir()}:
                raise ValueError("EXE_NOT_IN_ARCHIVE:" + label)
        zips[label] = (path, build)
    evidence.mkdir(parents=True)
    dump(evidence / "plan.json", plan)
    for label, (path, _) in zips.items():
        with zipfile.ZipFile(path) as z:
            infos = archive_entries(z)
            root = evidence / "extracted" / label
            root.mkdir(parents=True)
            for member in infos:
                sub = normname(member.filename)
                target = root.joinpath(*PurePosixPath(sub).parts)
                if member.is_dir():
                    target.mkdir(parents=True, exist_ok=True)
                    continue
                target.parent.mkdir(parents=True, exist_ok=True)
                with z.open(member) as src, target.open("xb") as dst:
                    while True:
                        block = src.read(1024 * 1024)  # ZIP checks CRC on completion
                        if not block:
                            break
                        dst.write(block)
        exe = root.joinpath(*PurePosixPath(plan["builds"][label]["exe"]).parts)
        if not exe.is_file():
            raise ValueError("EXE_MISSING_AFTER_EXTRACTION:" + label)
    dump(evidence / "prepared.json", {"schema": SCHEMA, "date_utc": utc(),
        "plan_sha256": digest(evidence / "plan.json"), "run_order": plan["order"],
        "exe_sha256": {key: digest(evidence / "extracted" / key / val["exe"]) for key, val in plan["builds"].items()},
        "status": "PREPARED_NOT_EXECUTED", "native_qa": "NOT_RUN"})
    return {"status": "PREPARED_NOT_EXECUTED", "evidence": str(evidence)}


def launch(evidence, index, timeout, operator_note):
    if sys.platform != "win32":
        raise RuntimeError("WINDOWS_GUI_ONLY")
    evidence = Path(evidence).resolve()
    plan = verify_plan(load(evidence / "plan.json"))
    prep = load(evidence / "prepared.json")
    if prep.get("plan_sha256") != digest(evidence / "plan.json"):
        raise ValueError("ALTERED_PLAN_AFTER_PREPARE")
    if not isinstance(index, int) or index < 0 or index >= len(plan["order"]):
        raise ValueError("BAD_TRIAL_INDEX")
    if not isinstance(operator_note, str) or len(operator_note.strip()) < 8:
        raise ValueError("OPERATOR_INTERACTION_DESCRIPTION_REQUIRED")
    if timeout < 15 or timeout > 1800:
        raise ValueError("TIMEOUT_OUT_OF_RANGE")
    label = plan["order"][index]
    binary = evidence / "extracted" / label / plan["builds"][label]["exe"]
    if digest(binary) != prep["exe_sha256"][label]:
        raise ValueError("EXE_CHANGED_AFTER_PREPARE")
    run_dir = evidence / "runs" / f"{index + 1:02d}-{label}"
    if run_dir.exists():
        raise ValueError("REFUSE_REUSE_PRIOR_TRIAL")
    run_dir.mkdir(parents=True)
    start = utc()
    log = run_dir / "Player.log"
    argv = [str(binary), "-screen-fullscreen", "0", "-logFile", str(log)]
    start_mono = time.monotonic()
    proc = subprocess.Popen(argv, cwd=str(binary.parent))  # visible GUI: human operates Quit
    did_timeout = False
    try:
        native_exit = proc.wait(timeout=timeout)
    except subprocess.TimeoutExpired:
        did_timeout = True
        proc.kill()
        proc.wait()
        native_exit = None  # killed timeout cannot be reported as native crash
    elapsed = round(time.monotonic() - start_mono, 3)
    code = None if native_exit is None else native_exit & 0xFFFFFFFF
    classification = ("TIMEOUT_KILLED" if did_timeout else
        "ACCESS_VIOLATION_C0000005" if code == AV_CODE else
        "CLEAN_EXIT_ZERO" if code == 0 else "NONZERO_OTHER")
    receipt = {"schema": SCHEMA, "index": index, "label": label,
        "start_utc": start, "finish_utc": utc(), "elapsed_seconds": elapsed,
        "exe_sha256": digest(binary), "plan_sha256": prep["plan_sha256"],
        "native_exit_signed": native_exit, "native_exit_unsigned": code,
        "native_exit_hex": None if code is None else f"0x{code:08X}",
        "classification": classification, "timed_out": did_timeout,
        "operator_note": operator_note, "operator_attribution": "EXTERNAL_OPERATOR_REPORTED",
        "player_log_sha256": digest(log) if log.is_file() and log.stat().st_size else None,
        "player_log_bytes": log.stat().st_size if log.is_file() else 0,
        "unity_native": "WINDOWS_PLAYER_EXECUTED_BY_EXTERNAL_OPERATOR"}
    dump(run_dir / "receipt.json", receipt)
    return receipt


def verify(evidence):
    evidence = Path(evidence).resolve()
    plan = verify_plan(load(evidence / "plan.json"))
    prep = load(evidence / "prepared.json")
    problems = []
    if prep.get("plan_sha256") != digest(evidence / "plan.json"):
        problems.append("ALTERED_PLAN_AFTER_PREPARE")
    results = []
    for i, label in enumerate(plan["order"]):
        folder = evidence / "runs" / f"{i + 1:02d}-{label}"
        rec_path = folder / "receipt.json"
        if not rec_path.is_file():
            results.append({"index": i, "label": label, "status": "NOT_RUN"})
            continue
        try:
            item = load(rec_path)
            errors = []
            if item.get("schema") != SCHEMA or item.get("index") != i or item.get("label") != label:
                errors.append("RECEIPT_MISMATCH")
            if item.get("plan_sha256") != prep.get("plan_sha256") or item.get("exe_sha256") != prep.get("exe_sha256", {}).get(label):
                errors.append("STALE_OR_CHANGED_BUILD")
            if item.get("native_exit_signed") is not None and type(item["native_exit_signed"]) is not int:
                errors.append("FORGED_NATIVE_EXIT")
            x = item.get("native_exit_signed")
            signed = None if x is None else x & 0xFFFFFFFF
            cls = "TIMEOUT_KILLED" if item.get("timed_out") is True else "ACCESS_VIOLATION_C0000005" if signed == AV_CODE else "CLEAN_EXIT_ZERO" if signed == 0 else "NONZERO_OTHER"
            if item.get("native_exit_unsigned") != signed or item.get("classification") != cls or (None if signed is None else f"0x{signed:08X}") != item.get("native_exit_hex"):
                errors.append("EXIT_CLASSIFICATION_MISMATCH")
            if item.get("timed_out") is not True and signed is None:
                errors.append("MISSING_NATIVE_EXIT")
            if item.get("timed_out") is True and signed is not None:
                errors.append("CONTRADICTORY_TIMEOUT_EXIT")
            log = folder / "Player.log"
            if not log.is_file() or not log.stat().st_size or digest(log) != item.get("player_log_sha256") or log.stat().st_size != item.get("player_log_bytes"):
                errors.append("MISSING_OR_TAMPERED_PLAYER_LOG")
            if len(str(item.get("operator_note", "")).strip()) < 8:
                errors.append("MISSING_OPERATOR_NOTE")
            status = "FAIL" if errors or signed == AV_CODE or (signed not in (0, None)) else ("BLOCKED" if signed is None else "PASS")
            results.append({"index": i, "label": label, "status": status,
                "classification": cls, "problems": errors, "exit_hex": item.get("native_exit_hex")})
        except (ValueError, KeyError, OSError, TypeError, json.JSONDecodeError):
            results.append({"index": i, "label": label, "status": "FAIL", "problems": ["UNREADABLE_RECEIPT"]})
    for runfolder in (evidence / "runs").glob("*") if (evidence / "runs").exists() else []:
        if runfolder.is_dir() and runfolder.name not in {f"{i+1:02d}-{k}" for i, k in enumerate(plan["order"])}:
            problems.append("UNEXPECTED_RUN_FOLDER")
    overall = ("FAIL" if problems or any(r["status"] == "FAIL" for r in results) else
        "BLOCKED" if any(r["status"] == "BLOCKED" for r in results) else
        "NOT_RUN" if any(r["status"] == "NOT_RUN" for r in results) else "PASS")
    baseline = [r for r in results if r["label"] in ("BASE", "EXP")]
    rc2 = [r for r in results if r["label"] == "RC2"]
    conclusion = "NOT_PROVEN"
    if all(r["status"] not in ("NOT_RUN", "BLOCKED") for r in results):
        crash_baseline = any(r.get("classification") == "ACCESS_VIOLATION_C0000005" for r in baseline)
        crash_rc2 = any(r.get("classification") == "ACCESS_VIOLATION_C0000005" for r in rc2)
        if crash_rc2:
            conclusion = "RC2_ACCESS_VIOLATION_OBSERVED"
        elif crash_baseline:
            conclusion = "RC2_NOT_OBSERVED_IN_SAMPLED_RUNS_NOT_A_FIX_PROOF"
        else:
            conclusion = "NO_CRASH_IN_SAMPLED_RUNS_NOT_A_FIX_PROOF"
    return {"schema": SCHEMA, "status": overall,
        "attribution": "EXTERNAL_OPERATOR_PROVIDED_NATIVE_EVIDENCE_NOT_B01_60_INDEPENDENT_RUN",
        "g2": "HOLD", "cases": results, "problems": problems,
        "conclusion": conclusion,
        "disclaimer": "Same-host matched manual GUI quit only; does not prove cause or full RC readiness."}


def main(argv=None):
    cli = argparse.ArgumentParser(description=__doc__)
    sub = cli.add_subparsers(dest="command", required=True)
    p = sub.add_parser("prepare")
    p.add_argument("--plan", required=True)
    p.add_argument("--evidence", required=True)
    p = sub.add_parser("run")
    p.add_argument("--evidence", required=True)
    p.add_argument("--index", required=True, type=int)
    p.add_argument("--timeout", type=int, default=240)
    p.add_argument("--operator-note", required=True)
    p = sub.add_parser("verify")
    p.add_argument("--evidence", required=True)
    p.add_argument("--out")
    args = cli.parse_args(argv)
    try:
        if args.command == "prepare":
            verdict = prepare(args.plan, args.evidence)
        elif args.command == "run":
            verdict = launch(args.evidence, args.index, args.timeout, args.operator_note)
        else:
            verdict = verify(args.evidence)
            if args.out:
                dump(args.out, verdict)
        print(json.dumps(verdict, sort_keys=True))
        if args.command == "verify":
            return 0 if verdict["status"] == "PASS" else 2 if verdict["status"] == "FAIL" else 3
        return 0
    except (ValueError, RuntimeError, OSError, KeyError, zipfile.BadZipFile) as exc:
        print("BLOCKED_RC1_QA: " + str(exc), file=sys.stderr)
        return 3


if __name__ == "__main__":
    raise SystemExit(main())
