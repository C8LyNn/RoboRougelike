#!/usr/bin/env python3
"""Validate ONLY authentic raw Unity benchmark CSV; no fabricated native results.

A clean report is a candidate for independent G1 inspection, never game acceptance.
Source-unit fixtures are explicitly synthetic and MUST NOT be reported as measured Unity.
"""
import csv
import json
import math
import statistics
import sys
from collections import defaultdict
from pathlib import Path

FIELDS = (
    "record_type", "unity_version", "platform", "r3_dependency_sha", "scenario",
    "parts", "order", "variant", "frame", "main_thread_ns", "physics_simulate_ns",
    "gc_alloc_bytes", "sleeping_count", "sleep_request_count",
    "active_control_sleep_requests", "static_stays", "body_stays", "safety"
)
SHA = "b38216d94fb0f87af0c1cbc47090c7cb4bf7177e"
SIZES = (10, 100, 500)
SCENARIOS = ("STATIC_REST", "DYNAMIC_CONTACT")
ABBA = ("UNITY_AUTO_BASELINE", "R3_CANDIDATE", "R3_CANDIDATE", "UNITY_AUTO_BASELINE")


class InvalidEvidence(ValueError):
    pass


def integer(raw, field):
    try:
        n = int(raw)
    except (TypeError, ValueError):
        raise InvalidEvidence(f"invalid {field}: {raw!r}")
    if n < 0:
        raise InvalidEvidence(f"negative {field}")
    return n


def check_rows(rows, min_frames=120):
    grouped = defaultdict(lambda: {"frames": [], "summaries": []})
    seen_context = set()
    for row in rows:
        if set(row) != set(FIELDS) or any(row[k] is None for k in FIELDS):
            raise InvalidEvidence("malformed CSV column structure")
        if row["r3_dependency_sha"] != SHA:
            raise InvalidEvidence("wrong R3 dependency SHA")
        if not row["unity_version"] or not row["platform"]:
            raise InvalidEvidence("missing native runtime identity")
        seen_context.add((row["unity_version"], row["platform"]))
        n = integer(row["parts"], "parts")
        order = integer(row["order"], "order")
        scene = row["scenario"]
        if n not in SIZES or scene not in SCENARIOS or order not in range(4):
            raise InvalidEvidence("unexpected native scenario size/order")
        if row["variant"] != ABBA[order]:
            raise InvalidEvidence("ABBA baseline/candidate order violated")
        key = (n, scene, order)
        if row["record_type"] == "FRAME":
            frame = integer(row["frame"], "frame")
            for metric in ("main_thread_ns", "physics_simulate_ns", "gc_alloc_bytes"):
                v = row[metric]
                if v != "NOT_MEASURED":
                    integer(v, metric)
            grouped[key]["frames"].append((frame, row))
        elif row["record_type"] == "SUMMARY":
            if row["frame"] != "NOT_APPLICABLE":
                raise InvalidEvidence("summary includes a fake frame")
            for field in ("sleeping_count", "sleep_request_count",
                          "active_control_sleep_requests", "static_stays", "body_stays"):
                integer(row[field], field)
            if row["safety"] not in ("PASS", "FAIL", "INCONCLUSIVE"):
                raise InvalidEvidence("safety is missing explicit disposition")
            grouped[key]["summaries"].append(row)
        else:
            raise InvalidEvidence("unknown record type")
    if len(seen_context) != 1:
        raise InvalidEvidence("mixed Unity versions/platforms in one evidence file")
    if len(grouped) != len(SIZES) * len(SCENARIOS) * 4:
        raise InvalidEvidence("missing or additional test sessions")

    per_session = {}
    for n in SIZES:
        for scenario in SCENARIOS:
            for order in range(4):
                key = (n, scenario, order)
                group = grouped.get(key)
                if not group:
                    raise InvalidEvidence(f"missing group {key}")
                frames = group["frames"]
                if len(frames) < min_frames or sorted(f for f, _ in frames) != list(range(len(frames))):
                    raise InvalidEvidence(f"missing/duplicate frame or too few frames {key}")
                if len(group["summaries"]) != 1:
                    raise InvalidEvidence(f"missing/duplicate summary {key}")
                summary = group["summaries"][0]
                if summary["safety"] != "PASS":
                    raise InvalidEvidence(f"FAIL or INCONCLUSIVE native safety at {key}")
                if integer(summary["active_control_sleep_requests"], "active requests") != 0:
                    raise InvalidEvidence(f"forced sleep of active sentinel at {key}")
                if integer(summary["sleeping_count"], "sleeping") > n:
                    raise InvalidEvidence(f"invalid sleeping-body tally at {key}")
                if scenario == "STATIC_REST" and integer(summary["static_stays"], "static stays") == 0:
                    raise InvalidEvidence(f"static contact not physically witnessed at {key}")
                if scenario == "DYNAMIC_CONTACT" and integer(summary["body_stays"], "body stays") == 0:
                    raise InvalidEvidence(f"dynamic contact not physically witnessed at {key}")
                if order in (0, 3) and integer(summary["sleep_request_count"], "requests") != 0:
                    raise InvalidEvidence(f"baseline must never have governor requests at {key}")
                metrics = {}
                for label in ("main_thread_ns", "physics_simulate_ns", "gc_alloc_bytes"):
                    values = [integer(row[label], label) for _, row in frames
                              if row[label] != "NOT_MEASURED"]
                    metrics[label] = (statistics.median(values) if len(values) >=
                                      math.ceil(.9 * len(frames)) else None)
                per_session[key] = {"metrics": metrics,
                                    "requests": integer(summary["sleep_request_count"], "requests"),
                                    "sleeping": integer(summary["sleeping_count"], "sleeping"),
                                    "static_stays": integer(summary["static_stays"], "static_stays"),
                                    "body_stays": integer(summary["body_stays"], "body_stays")}
    aggregate = []
    for n in SIZES:
        for scene in SCENARIOS:
            pair = [per_session[n, scene, k] for k in range(4)]
            metric_results = {}
            for metric in ("main_thread_ns", "physics_simulate_ns", "gc_alloc_bytes"):
                baseline = [pair[i]["metrics"][metric] for i in (0, 3)]
                candidate = [pair[i]["metrics"][metric] for i in (1, 2)]
                if any(v is None for v in baseline+candidate):
                    metric_results[metric] = {"baseline_median": None, "candidate_median": None,
                                              "delta_percent": None, "status": "NOT_MEASURED"}
                    continue
                b, c = statistics.median(baseline), statistics.median(candidate)
                delta = None if b == 0 else round((c-b)*100.0/b, 3)
                metric_results[metric] = {
                    "baseline_median": b, "candidate_median": c,
                    "delta_percent": delta, "status": "MEASURED" if delta is not None else "INDETERMINATE_ZERO_BASELINE"}
            aggregate.append({"parts": n, "scenario": scene, "metrics": metric_results,
                              "candidate_sleep_requests": sum(pair[i]["requests"] for i in (1, 2)),
                              "candidate_last_sleeping": [pair[i]["sleeping"] for i in (1, 2)]})
    return {"status": "NATIVE_CSV_STRUCTURALLY_VALID_NOT_G1_ACCEPTED",
            "data_origin": "CALLER_SUPPLIED_NEVER_INFERRED",
            "dependency_r3_sha": SHA, "unity_context": list(seen_context)[0],
            "frame_sessions": len(per_session), "pairs": aggregate,
            "warning": "Only original Editor.log + NUnit XML + physical scene QA + SHA + reviewer can grant G1. Missing markers remain NOT_MEASURED; a negative delta does not by itself prove safe production benefit."}


def main():
    if len(sys.argv) != 2:
        print("usage: python analyze_native_csv.py <actual_native_raw_csv>", file=sys.stderr)
        return 2
    try:
        path = Path(sys.argv[1])
        with path.open(newline="", encoding="utf-8-sig") as f:
            reader = csv.DictReader(f)
            if tuple(reader.fieldnames or ()) != FIELDS:
                raise InvalidEvidence("wrong CSV header")
            result = check_rows(reader)
        print(json.dumps(result, indent=2, ensure_ascii=False))
        return 0
    except (OSError, InvalidEvidence, csv.Error) as e:
        print("NATIVE_EVIDENCE_INVALID_OR_INCOMPLETE: " + str(e), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
