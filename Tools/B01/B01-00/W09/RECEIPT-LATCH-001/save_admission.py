#!/usr/bin/env python3
"""W14 declarative admission gate for ONE stable-ID Save/Load contract.

Offline audit only. This tool cannot authenticate GitHub actors, source hashes,
review content, tested binaries or a Codex ACK. Source-only offer is not a
Save/Load implementation and NEVER grants native G1/G2 or integration.
"""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path
import re
import sys

REPO = "https://github.com/C8LyNn/RoboRougelike/"
SHA = re.compile(r"^[0-9a-f]{40}$")
SCHEMA = "b01-w14-single-save-admission-v1"
REQUIRED_HOLDS = {
    "native_g1": "NOT_RUN",
    "canonical_save_load": "NOT_IMPLEMENTED",
    "canonical_migration": "NOT_DONE",
    "original_510_rollback": "FORBIDDEN",
    "g2": "HOLD",
    "codex_ack": "NONE",
    "integrated": False,
    "merge": False,
}
ALLOWED_RED = frozenset(("VALIDATED_SNAPSHOT_MUTABLE", "W06_LEDGER_PUBLIC_MUTATOR",
                         "OTHER_VERIFIED_SECURITY_RED"))


class AdmissionError(ValueError):
    pass


def doc(value, name):
    if type(value) is not dict:
        raise AdmissionError(name + " must be an object")
    return value


def valid_sha(x):
    return type(x) is str and bool(SHA.fullmatch(x))


def natural(x):
    return type(x) is int and x > 0


def pr_url(n):
    return REPO + "pull/" + str(n)


def audit(payload):
    source = doc(payload, "root")
    if source.get("schema") != SCHEMA:
        raise AdmissionError("unsupported schema, explicit version migration required")
    game = doc(source.get("frozen_gameplay"), "frozen_gameplay")
    if not natural(game.get("pr")) or not valid_sha(game.get("sha")):
        raise AdmissionError("invalid frozen gameplay identity")
    if game.get("frozen") is not True or game.get("url") != pr_url(game["pr"]):
        raise AdmissionError("gameplay must be frozen and canonical")
    # This W14 authority is ONLY the explicitly frozen and independently
    # source-G0-accepted #510; avoid parsing a generic other-game offer.
    if (game["pr"] != 510
            or game["sha"] != "d94400d95d13db7f27e1533df05c9292d5380431"):
        raise AdmissionError("wrong immutable selected gameplay source")

    proposals = source.get("proposals")
    if type(proposals) is not list or len(proposals) != 2:
        raise AdmissionError("exactly two comparison proposals required")
    candidates = {}
    for candidate in proposals:
        x = doc(candidate, "proposal")
        n = x.get("pr")
        h = x.get("source_sha")
        if not natural(n) or not valid_sha(h) or x.get("url") != pr_url(n):
            raise AdmissionError("proposal source identity malformed")
        if n in candidates:
            raise AdmissionError("duplicate proposal PR")
        if x.get("target_game_sha") != game["sha"]:
            raise AdmissionError("proposal targets wrong frozen gameplay SHA")
        if x.get("producer_ci") not in ("PASS_SOURCE_ONLY", "FAIL", "NOT_RUN"):
            raise AdmissionError("invalid producer evidence state")
        qa = doc(x.get("independent_qa"), "independent_qa")
        if qa.get("state") not in ("PASS_SOURCE_ONLY", "SECURITY_RED", "NOT_RUN"):
            raise AdmissionError("invalid independent QA state")
        if qa["state"] == "NOT_RUN":
            if any(qa.get(k) is not None for k in
                   ("review_pr", "review_sha", "target_sha", "tested_blob_sha")):
                raise AdmissionError("QA NOT_RUN cannot contain fabricated review")
        else:
            if (not natural(qa.get("review_pr")) or qa["review_pr"] == n
                    or not valid_sha(qa.get("review_sha"))
                    or not valid_sha(qa.get("target_sha"))):
                raise AdmissionError("independent review must pin tested source commit")
            if qa["state"] == "PASS_SOURCE_ONLY" and qa["target_sha"] != h:
                raise AdmissionError("older QA PASS cannot promote later producer HEAD")
            if qa["state"] == "SECURITY_RED" and qa["target_sha"] != h:
                # A RED from an older branch commit can remain an active
                # blocker ONLY for exactly the same original vulnerable blob.
                # Do not pretend the review tested the later branch HEAD.
                if (not valid_sha(x.get("source_blob_sha"))
                        or qa.get("tested_blob_sha") != x["source_blob_sha"]):
                    raise AdmissionError("older RED cannot transfer without identical source blob")
        codes = qa.get("failure_codes")
        if qa["state"] == "SECURITY_RED":
            if (type(codes) is not list or not codes
                    or len(codes) != len(set(codes))
                    or any(type(c) is not str or c not in ALLOWED_RED for c in codes)):
                raise AdmissionError("SECURITY_RED must preserve exact known defect codes")
        elif codes not in ([], None):
            raise AdmissionError("cannot attach SECURITY_RED codes to non-RED verdict")
        candidates[n] = x
    if set(candidates) != {544, 549}:
        raise AdmissionError("this wave compares #544 against #549 only")

    choice = doc(source.get("owner_choice"), "owner_choice")
    if choice.get("authority") != "B01-23":
        raise AdmissionError("only source contract owner B01-23 may choose")
    state = choice.get("state")
    if state not in ("UNDECIDED", "SELECTED_ONE"):
        raise AdmissionError("invalid selection state")
    if state == "UNDECIDED":
        if any(choice.get(k) is not None for k in ("pr", "source_sha", "decision_url")):
            raise AdmissionError("undecided cannot name selected contract")
    else:
        chosen = choice.get("pr")
        if chosen not in candidates or choice.get("source_sha") != candidates[chosen]["source_sha"]:
            raise AdmissionError("chosen proposal SHA must be exact current source")
        url = choice.get("decision_url")
        if (not isinstance(url, str)
                or not re.fullmatch(
                    r"https://github\.com/C8LyNn/RoboRougelike/"
                    r"(?:issues|pull)/[1-9][0-9]*#issuecomment-[1-9][0-9]*", url)):
            raise AdmissionError("owner choice needs a distinct canonical decision receipt")

    holds = doc(source.get("release_gates"), "release_gates")
    if set(holds) != set(REQUIRED_HOLDS):
        raise AdmissionError("release gate fields must be complete and exact")
    for key, value in REQUIRED_HOLDS.items():
        if holds[key] != value or type(holds[key]) is not type(value):
            raise AdmissionError("attempt to promote unverified release gate: " + key)

    statuses = {}
    for n, x in sorted(candidates.items()):
        q = x["independent_qa"]["state"]
        if q == "SECURITY_RED":
            status = "REJECT_EXACT_SOURCE_SECURITY_RED"
        elif x["producer_ci"] != "PASS_SOURCE_ONLY":
            status = "HOLD_PRODUCER_TESTS"
        elif q != "PASS_SOURCE_ONLY":
            status = "HOLD_INDEPENDENT_SOURCE_QA"
        elif state != "SELECTED_ONE" or choice["pr"] != n:
            status = "HOLD_NOT_EXCLUSIVE_OWNER_SELECTION"
        else:
            status = "MANUAL_G0_REVIEW_ONLY"
        statuses[str(n)] = status

    any_red = any(x["independent_qa"]["state"] == "SECURITY_RED"
                  for x in candidates.values())
    eligible = [n for n, x in sorted(candidates.items())
                if x["producer_ci"] == "PASS_SOURCE_ONLY"
                and x["independent_qa"]["state"] == "PASS_SOURCE_ONLY"
                and state == "SELECTED_ONE" and choice["pr"] == n]
    if any_red and state == "SELECTED_ONE" and (
            candidates[choice["pr"]]["independent_qa"]["state"] == "SECURITY_RED"):
        eligible.clear()
    chosen = eligible[0] if len(eligible) == 1 else None
    # Even if a later actual source-only QA PASS exists, this is an OFFLINE
    # statement, not an authenticated G0; intake still needs B01-40 review.
    return {
        "schema": SCHEMA,
        "frozen_gameplay_sha": game["sha"],
        "candidate_statuses": statuses,
        "only_source_candidate_for_manual_review": chosen,
        "single_contract_source_record_consistent": chosen is not None,
        "owner_choice_state": state,
        "independent_security_red_present": any_red,
        "source_only_candidate_proof_authenticated": False,
        "host_save_load": "HOLD",
        "safe_receiver_migration": "HOLD",
        "native_unity_g1": "NOT_RUN",
        "g2": "HOLD",
        "codex_ack": "UNCONFIRMED",
        "integrated": False,
        "release_allowed": False,
        "verdict": ("MANUAL_G0_REVIEW_REQUIRED" if chosen is not None
                    else "HOLD_NO_ACCEPTED_SINGLE_SAVE_CONTRACT"),
    }



# W15 distinct explicit source receipt; W14 remains historical evidence.
SELECTED_V2_SCHEMA = "b01-w15-selected-save-host-gate-v1"
FROZEN_GAME_SHA = "d94400d95d13db7f27e1533df05c9292d5380431"
FROZEN_SAVE_SHA = "33094f154c7b91119b36d58aab285e6e83c5de57"
HOLD_HOST = {
    "adapter": "NOT_IMPLEMENTED", "actual_unity_save_reload": "NOT_RUN",
    "native_physx_g1": "NOT_RUN", "migration": "NOT_DONE",
    "receiver_rollback": "SCRATCH_ONLY",
    "original_510_rollback": "FORBIDDEN", "codex_ack": "NONE",
    "integrated": False, "merged": False, "g2": "HOLD",
}


def audit_selected_v2(raw):
    """Offline receipt consistency, NEVER live Unity/QA actor authentication."""
    root = doc(raw, "W15 receipt")
    if root.get("schema") != SELECTED_V2_SCHEMA:
        raise AdmissionError("unsupported W15 versioned receipt schema")
    game = doc(root.get("gameplay"), "gameplay")
    save = doc(root.get("selected_save"), "selected_save")
    if (game.get("pr") != 510 or game.get("sha") != FROZEN_GAME_SHA
            or game.get("url") != pr_url(510) or game.get("frozen") is not True):
        raise AdmissionError("frozen gameplay #510 mismatch")
    if (save.get("pr") != 556 or save.get("sha") != FROZEN_SAVE_SHA
            or save.get("url") != pr_url(556) or save.get("frozen") is not True
            or save.get("target_game_sha") != FROZEN_GAME_SHA
            or save.get("version") != "2.0.0"):
        raise AdmissionError("only frozen #556 can be selected")
    owner = doc(root.get("owner_choice"), "owner_choice")
    if (owner.get("agent") != "B01-23" or owner.get("selection") != "SELECTED_ONE"
            or owner.get("pr") != 556 or owner.get("sha") != FROZEN_SAVE_SHA):
        raise AdmissionError("exclusive producer choice missing")
    held = root.get("alternatives_hold")
    if (type(held) is not list or sorted(held) != [494, 511, 544, 549, 554]):
        raise AdmissionError("old and alternative source HOLD missing")
    g0 = doc(root.get("physical_source_g0"), "physical_source_g0")
    expected_g0 = {
        "agent": "B01-40", "state": "ACCEPTED_SOURCE_ONLY", "pr": 560,
        "url": pr_url(560),
        "qa_sha": "1a218df9e287a8a254b7d84c228c962ab232b9b5",
        "target_sha": FROZEN_SAVE_SHA,
        "inner_zip_sha256":
            "20c3c17c252f084014e9766010ab4651770a41f73ce534fc2aa7fd2edc697060",
        "members": 17,
    }
    if g0 != expected_g0:
        raise AdmissionError("physical G0 source-only provenance mismatch")
    qas = root.get("independent_source_qa")
    if type(qas) is not list or len(qas) != 2:
        raise AdmissionError("two source QA receipts required")
    expected_qa = {
        563: ("42aa590a2f6b08e33886027ea452ef3392a2f2c0", "OPEN_DRAFT"),
        570: ("413bee4c232145be4cdf5745088fe1538a5f421b", "CLOSED_UNMERGED"),
    }
    seen = set()
    for entry in qas:
        qa = doc(entry, "independent QA")
        n = qa.get("pr")
        if n not in expected_qa or n in seen:
            raise AdmissionError("duplicate or alternative QA")
        seen.add(n)
        h, state = expected_qa[n]
        if (qa.get("agent") != "B01-61" or qa.get("target_sha") != FROZEN_SAVE_SHA
                or qa.get("result") != "PASS_SOURCE_ONLY"
                or qa.get("qa_sha") != h or qa.get("pr_state") != state
                or qa.get("url") != pr_url(n)
                or qa.get("merged") is not False):
            raise AdmissionError("QA SHA/state does not match selected #556")
    history = root.get("historical_rejections")
    if (type(history) is not list or len(history) != 2
            or {r.get("pr") for r in history if type(r) is dict} != {544, 549}
            or any(type(r) is not dict or
                   r.get("status") != "HISTORICAL_SECURITY_RED_NOT_SELECTED"
                   for r in history)):
        raise AdmissionError("historical security RED forgotten")
    if (root.get("legacy_security_qa") != 550
            or root.get("legacy_red_source_blob") !=
                "6c036985411164372bf3d21d467780ff3777544e"):
        raise AdmissionError("historical original #550 SHA mismatch")
    host = doc(root.get("host_runtime"), "host_runtime")
    if set(host) != set(HOLD_HOST):
        raise AdmissionError("unknown host gate field or missing blocker")
    for k, v in HOLD_HOST.items():
        if host[k] != v or type(host[k]) is not type(v):
            raise AdmissionError("unreviewed host gate promotion: " + k)
    if root.get("adapter_package_id") != "NOT_ASSIGNED":
        raise AdmissionError("no approved host bridge can be invented")
    return {
        "schema": SELECTED_V2_SCHEMA,
        "selected_gameplay_pr": 510, "selected_save_pr": 556,
        "selected_save_sha": FROZEN_SAVE_SHA,
        "source_g0_recorded": "ACCEPTED_SOURCE_ONLY",
        "independent_source_qa_records": [563, 570],
        "historical_rejected_sources": [544, 549, 554],
        "source_claims_authenticated_online": False,
        "unity_host_adapter": "NOT_IMPLEMENTED",
        "unity_cold_save_load": "NOT_RUN", "native_physx_g1": "NOT_RUN",
        "receiver_migration": "NOT_DONE", "rollback": "SCRATCH_ONLY",
        "codex_ack": "UNCONFIRMED", "g2": "HOLD",
        "integrated": False, "release_allowed": False,
        "verdict": "SOURCE_G0_RECORDED_HOST_GATE_HOLD",
    }


def selected_main(argv=None):
    parser = argparse.ArgumentParser(description="W15 selected frozen #556 host receipt")
    parser.add_argument("receipt", type=Path)
    parser.add_argument("--output", type=Path)
    a = parser.parse_args(argv)
    try:
        blob = a.receipt.read_bytes()
        result = audit_selected_v2(json.loads(blob.decode("utf-8")))
        result["input_sha256"] = hashlib.sha256(blob).hexdigest()
        rendered = json.dumps(result, sort_keys=True, indent=2) + "\n"
        if a.output is None:
            print(rendered, end="")
        else:
            if a.output.resolve() == a.receipt.resolve() or a.output.exists():
                raise AdmissionError("refuse to overwrite evidence")
            with a.output.open("x", encoding="utf-8") as stream:
                stream.write(rendered)
        return 0
    except (AdmissionError, OSError, ValueError, UnicodeError, TypeError) as err:
        print("SELECTED_SAVE_HOST_HOLD: " + str(err), file=sys.stderr)
        return 2

def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("receipt", type=Path)
    p.add_argument("--require-single-source-candidate", action="store_true")
    p.add_argument("--output", type=Path)
    args = p.parse_args(argv)
    try:
        raw = args.receipt.read_bytes()
        result = audit(json.loads(raw.decode("utf-8")))
        result["input_sha256"] = hashlib.sha256(raw).hexdigest()
        rendered = json.dumps(result, indent=2, sort_keys=True) + "\n"
        if args.output is None:
            print(rendered, end="")
        else:
            if args.output.resolve() == args.receipt.resolve() or args.output.exists():
                raise AdmissionError("refuse to overwrite evidence")
            with args.output.open("x", encoding="utf-8") as f:
                f.write(rendered)
        if args.require_single_source_candidate and not result["single_contract_source_record_consistent"]:
            return 2
        return 0
    except (AdmissionError, OSError, ValueError, TypeError, UnicodeError) as exc:
        print("SAVE_CONTRACT_HOLD: " + str(exc), file=sys.stderr)
        return 2


if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "--selected-v2":
        raise SystemExit(selected_main(sys.argv[2:]))
    raise SystemExit(main())
