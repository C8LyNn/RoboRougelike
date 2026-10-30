#!/usr/bin/env python3
"""B01-00 W18: source-backed *unmeasured* standard/polished integrator batch.

Only real integrator events, authenticated independently outside this offline
tool, may populate acceptance/timing metrics. Never invent a Codex ACK.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import sys

SCHEMA = "b01-w18-paired-integrator-baseline-v1"
SOURCES = {
    596: ("B01-23-UNITY-SAVE-HOST-BRIDGE-001", "B01-23",
          "efb2b06eafedab5469b57f163152d96aca3c5915"),
    607: ("B01-61-EXACT-596-TRANSACTION-QA", "B01-61",
          "86f3292c928e52609db6c7afdb2010971d6946f1"),
}
ARMS = {
    "standard": ["pr", "sha", "purpose"],
    "polished": ["pr", "sha", "purpose", "evidence", "dependencies",
                 "rollback", "known_blockers", "receiver"],
}
SIX = ("IMPLEMENTED", "TESTED", "QA_ACCEPTED", "READY_FOR_INTEGRATOR",
       "INTEGRATED", "RELEASED")
EXPECTED_FREEZE = {
    "game_pr": 510, "game_sha": "d94400d95d13db7f27e1533df05c9292d5380431",
    "save_pr": 556, "save_sha": "33094f154c7b91119b36d58aab285e6e83c5de57",
    "host_pr": 596, "host_sha": SOURCES[596][2],
    "alt_pr": 604, "alt_status": "ALT_HOLD", "immutable": True,
}
REVIEW_HOLDS = {
    "selected_host_g0": "PENDING",
    "selected_host_g1": "NOT_RUN",
    "unity_physx": "NOT_RUN",
    "canonical_game_save_load": "HOLD",
    "host_security_629": "UNVERIFIED_SOURCE_RISK",
    "fixture_631": "UNVERIFIED_SOURCE_RISK",
    "checkpoint_lock": "NOT_NATIVE_PROVEN",
    "codex_ack": "NONE",
    "codex_offer": "BLOCKED",
    "g2": "HOLD",
    "merged": False,
}
GH = "https://github.com/C8LyNn/RoboRougelike/"
SHA = re.compile(r"[a-f0-9]{40}\Z")


class BatchError(ValueError):
    pass


def require(condition, msg):
    if not condition:
        raise BatchError(msg)


def obj(v, label):
    require(type(v) is dict, label + " must be object")
    return v


def source_sha(s):
    return type(s) is str and SHA.fullmatch(s) is not None


def audit(data):
    root = obj(data, "batch")
    require(root.get("schema") == SCHEMA, "wrong experiment schema")
    require(root.get("freeze") == EXPECTED_FREEZE, "frozen selected source drift")
    require(root.get("guards") == REVIEW_HOLDS, "unjustified G0, G1, ACK or release")
    require(root.get("assignment") == "PAIRED_SAME_SOURCE_METADATA_ONLY",
            "nonpaired/confounded arm assignment")
    require(root.get("delivery") == "NOT_SENT", "cannot self-assert handoff to Codex")
    require(root.get("measured_feedback_source") is None,
            "integrator feedback must be separately authenticated")

    packages = root.get("packages")
    require(type(packages) is list and len(packages) == len(SOURCES),
            "expected exact two original package refs")
    seen = set()
    report = []
    for p in packages:
        x = obj(p, "package")
        n = x.get("pr")
        require(type(n) is int and n in SOURCES and n not in seen,
                "duplicate/unrecognized package")
        seen.add(n)
        pid, owner, sha = SOURCES[n]
        require(x.get("package_id") == pid and x.get("owner") == owner
                and x.get("sha") == sha and source_sha(x.get("sha"))
                and x.get("pr_url") == GH + "pull/" + str(n),
                "package ID, owner, PR or original SHA mismatch")
        require(type(x.get("purpose")) is str and len(x["purpose"]) > 15,
                "missing actual package purpose")
        require(x.get("producer_source") == "TESTED_SOURCE_ONLY"
                and x.get("independent_qa") == "NOT_FINAL_RELEASE",
                "source test cannot auto-pass independent physical G0")

        evidence = x.get("evidence")
        require(type(evidence) is list and len(evidence) >= 1
                and len(evidence) == len(set(evidence))
                and all(type(y) is str and y.startswith(GH + "actions/runs/")
                        and y[len(GH + "actions/runs/"):].isdigit()
                        for y in evidence),
                "missing or forged shape of original Actions references")
        dependencies = x.get("dependencies")
        require(type(dependencies) is list
                and dependencies == ["#510", "#556", "#596"]
                and len(set(dependencies)) == len(dependencies),
                "required frozen dependency identity")
        require(type(x.get("rollback")) is str
                and x["rollback"] == "DISCARD_OWN_UNMERGED_FILES_ONLY",
                "foreign shared path rollback forbidden")
        blockers = x.get("known_blockers")
        require(type(blockers) is list and
                blockers == ["G0_PENDING", "UNITY_NOT_RUN", "LIVE_GAME_SAVE_HOLD",
                             "INTEGRATOR_NOT_MEASURED"], "blocker suppression")
        require(x.get("receiver") == "B01-40/B01-60/B01-00",
                "wrong physical/native/offer receiver")

        stages = obj(x.get("stages"), "lifecycle stages")
        require(list(stages) == list(SIX)
                and all(type(stages[k]) is bool for k in SIX)
                and [stages[k] for k in SIX] == [True, True, False, False, False, False],
                "six stages cannot auto-promote")
        arms = x.get("arms")
        require(type(arms) is list and len(arms) == 2, "exactly two paired arms")
        found = set()
        for arm in arms:
            a = obj(arm, "handoff arm")
            name = a.get("name")
            require(name in ARMS and name not in found, "missing or duplicated arm")
            found.add(name)
            require(a.get("presentation_fields") == ARMS[name],
                    "standard/polished treatment drift")
            feedback = obj(a.get("integrator_feedback"), "integrator_feedback")
            require(feedback == {
                "status": "NOT_MEASURED", "real_integrator_evidence": None,
                "elapsed_seconds": None, "retries": None,
                "conflicts": None, "regressions": None,
                "reviewed_at": None,
            }, "fabricated integrator outcome, acceptance time or retries")
        require(found == set(ARMS), "both treatment arms are required")
        report.append({"package_id": pid, "pr": n, "sha": sha,
                       "source_tested": True, "qa_accepted": False,
                       "ready_for_integrator": False, "integrated": False,
                       "released": False, "standard": "NOT_MEASURED",
                       "polished": "NOT_MEASURED", "integration_seconds": None,
                       "conflicts": None, "regressions": None})
    require(seen == set(SOURCES), "missing one source package")

    return {
        "schema": SCHEMA,
        "experiment": "STANDARD_VS_POLISHED_SOURCE_HANDOFF_PAIRED_BASELINE",
        "matched_packages": len(report),
        "presentation_arms": len(report)*2,
        "completed_integrator_reviews": 0,
        "accepted_packages": None,
        "acceptance_yield": None,
        "median_integration_seconds": None,
        "retry_rate": None,
        "regression_rate": None,
        "cause_of_null_metrics": "NO_AUTHENTICATED_INTEGRATOR_EVENTS",
        "G0": "PENDING",
        "G1": "NOT_RUN",
        "host_live_save_load": "HOLD",
        "codex_offer": "BLOCKED",
        "codex_ack": "NONE",
        "G2": "HOLD",
        "state": "NOT_MEASURED_NO_CLAIM_OF_INTEGRATION",
        "packages": report,
    }


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("batch", type=Path)
    p.add_argument("--output", type=Path)
    p.add_argument("--require-actual-feedback", action="store_true")
    args = p.parse_args(argv)
    try:
        original = args.batch.read_bytes()
        report = audit(json.loads(original.decode("utf-8")))
        report["input_sha256"] = hashlib.sha256(original).hexdigest()
        rendered = json.dumps(report, indent=2, sort_keys=True) + "\n"
        if args.output is None:
            print(rendered, end="")
        else:
            require(args.output.resolve() != args.batch.resolve()
                    and not args.output.exists(), "refusing to overwrite evidence")
            with args.output.open("x", encoding="utf-8") as target:
                target.write(rendered)
        return 3 if args.require_actual_feedback else 0
    except (BatchError, OSError, TypeError, ValueError, UnicodeError) as e:
        print("W18_BASELINE_HOLD: " + str(e), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
