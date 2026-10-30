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
    raise SystemExit(main())
