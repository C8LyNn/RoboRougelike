#!/usr/bin/env python3
"""Read-only B01 source/QA/Codex receipt consistency gate.

Does not authenticate external evidence. SHA/URLs must be separately verified
against GitHub by B01-40/B01-60 before any formal acceptance.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import sys

SCHEMA = "b01-receipt-latch-v1"
HASH = re.compile(r"^[a-f0-9]{40}$")
ID = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._-]{1,99}$")
REPO = "https://github.com/C8LyNn/RoboRougelike/"
ACTORS = {"intake": "B01-40", "independent": "B01-60"}
STATUS = {"PASS", "FAIL", "PENDING", "NOT_RUN"}


class GateError(ValueError):
    pass


def mapping(value, name):
    if type(value) is not dict:
        raise GateError(name + " must be a JSON object")
    return value


def token(value, name):
    if not isinstance(value, str) or not ID.fullmatch(value):
        raise GateError("invalid " + name)
    return value


def sha(value, name):
    if not isinstance(value, str) or not HASH.fullmatch(value):
        raise GateError("invalid " + name + " (strict 40-lowercase-hex)")
    return value


def ref_url(value, kind):
    """Structural first-party GitHub URL; no external provenance authentication."""
    if not isinstance(value, str) or not value.startswith(REPO):
        return False
    tail = value[len(REPO):]
    if any(x in tail for x in ("?", "\\", "%", "//")):
        return False
    if kind == "run":
        return bool(re.fullmatch(r"actions/runs/[1-9][0-9]*", tail))
    if kind == "pr":
        return bool(re.fullmatch(r"pull/[1-9][0-9]*", tail))
    if kind == "comment":
        return bool(re.fullmatch(r"(?:issues|pull)/[1-9][0-9]*#issuecomment-[1-9][0-9]*", tail))
    return False

def positive_ci(entry, source, label, blockers):
    if type(entry) is not dict:
        blockers.append(label + "_missing")
        return
    if entry.get("sha") != source:
        blockers.append(label + "_sha_mismatch")
    if entry.get("status") != "PASS":
        blockers.append(label + "_not_pass")
    if type(entry.get("passed")) is not int or entry["passed"] < 1:
        blockers.append(label + "_no_executed_tests")
    if type(entry.get("failed")) is not int or entry["failed"] != 0:
        blockers.append(label + "_failures_or_invalid")
    if not ref_url(entry.get("url"), "run"):
        blockers.append(label + "_no_run_url")


def audit(record):
    """Return deterministic, fail-closed advisory derived from declared receipts."""
    doc = mapping(record, "document")
    if doc.get("schema") != SCHEMA:
        raise GateError("unsupported receipt schema")
    token(doc.get("package_id"), "package_id")
    token(doc.get("version"), "version")
    producer = mapping(doc.get("producer"), "producer")
    token(producer.get("agent"), "producer.agent")
    if producer["agent"] in ACTORS.values() or producer["agent"] == "CODEX":
        raise GateError("producer must be distinct from independent QA and Codex")
    source = sha(producer.get("sha"), "producer.sha")
    if type(producer.get("source_pr")) is not int or producer["source_pr"] < 1:
        raise GateError("producer.source_pr must be a positive integer")
    if not ref_url(producer.get("url"), "pr") or producer["url"] != REPO + "pull/" + str(producer["source_pr"]):
        raise GateError("producer source URL is inconsistent")
    if type(doc.get("needs_native")) is not bool:
        raise GateError("needs_native must be explicit bool")

    blockers = []
    positive_ci(producer.get("ci"), source, "producer_ci", blockers)

    qa = mapping(doc.get("qa"), "qa")
    for role, agent in ACTORS.items():
        receipt = qa.get(role)
        if type(receipt) is not dict:
            blockers.append(role + "_missing")
            continue
        if receipt.get("agent") != agent or receipt.get("agent") == producer["agent"]:
            blockers.append(role + "_not_independent")
        if receipt.get("status") not in STATUS or receipt["status"] != "PASS":
            blockers.append(role + "_not_pass")
        if receipt.get("sha") != source:
            blockers.append(role + "_sha_mismatch")
        if not ref_url(receipt.get("url"), "comment"):
            blockers.append(role + "_no_comment_url")

    native = mapping(doc.get("native"), "native")
    if doc["needs_native"]:
        for part in ("edit", "play"):
            positive_ci(native.get(part), source, "native_" + part, blockers)
    qa_urls = [r.get("url") for r in (qa.get("intake"), qa.get("independent"))
               if type(r) is dict and isinstance(r.get("url"), str)]
    if len(qa_urls) != len(set(qa_urls)):
        blockers.append("independent_receipt_reused")
    source_ready = not blockers

    offer = mapping(doc.get("offer"), "offer")
    offer_status = offer.get("status")
    if offer_status not in ("NOT_SENT", "OFFERED"):
        raise GateError("offer.status must be NOT_SENT or OFFERED")
    offer_reused = offer.get("url") in qa_urls if offer_status == "OFFERED" else False
    if offer_reused:
        blockers.append("offer_receipt_reused_qa_comment")
    offer_proven = (offer_status == "OFFERED" and offer.get("sha") == source
                    and ref_url(offer.get("url"), "comment") and not offer_reused)
    if offer_status == "OFFERED" and not offer_proven:
        blockers.append("offer_receipt_invalid")
    if offer_status == "OFFERED" and not source_ready:
        blockers.append("offer_claim_before_gates")

    ack = mapping(doc.get("ack"), "ack")
    if ack.get("status") not in ("NO", "YES"):
        raise GateError("ack.status must be NO or YES")
    ack_reused_offer = ack.get("status") == "YES" and ack.get("url") == offer.get("url")
    ack_reused_qa = ack.get("status") == "YES" and ack.get("url") in qa_urls
    if ack_reused_offer:
        blockers.append("codex_ack_reused_offer_comment")
    if ack_reused_qa:
        blockers.append("codex_ack_reused_qa_comment")
    ack_proven = (ack.get("status") == "YES" and ack.get("actor") == "CODEX"
                  and ack.get("sha") == source and ref_url(ack.get("url"), "comment")
                  and offer_proven and source_ready and not ack_reused_offer
                  and not ack_reused_qa)
    if ack.get("status") == "YES" and not ack_proven:
        blockers.append("codex_ack_unverified_or_out_of_order")

    integration = mapping(doc.get("integration"), "integration")
    if integration.get("status") not in ("NO", "YES"):
        raise GateError("integration.status must be NO or YES")
    integrated = (integration.get("status") == "YES" and ack_proven
                  and integration.get("sha") == source
                  and isinstance(integration.get("commit_sha"), str)
                  and bool(HASH.fullmatch(integration["commit_sha"]))
                  and ref_url(integration.get("url"), "pr"))
    if integration.get("status") == "YES" and not integrated:
        blockers.append("integration_unverified_or_out_of_order")

    # Never infer ACK from a successful source gate or from an OFFERED claim.
    return {
        "schema": SCHEMA,
        "package_id": doc["package_id"],
        "version": doc["version"],
        "source_sha": source,
        "evidence_authentication": "NOT_PERFORMED_OFFLINE",
        "source_gates_consistent": source_ready,
        "offer_record_consistent": offer_proven and source_ready,
        "ack_record_consistent": ack_proven,
        "integration_record_consistent": integrated,
        "blockers": sorted(set(blockers)),
        "recommendation": "ELIGIBLE_FOR_EXTERNAL_SOURCE_REVIEW" if source_ready else "HOLD",
    }


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("receipt", type=Path)
    p.add_argument("--output", type=Path, default=None, help="optional derived JSON receipt")
    p.add_argument("--require-source-ready", action="store_true",
                   help="exit 2 for invalid/incomplete SOURCE gates")
    a = p.parse_args(argv)
    try:
        raw = a.receipt.read_bytes()
        report = audit(json.loads(raw.decode("utf-8")))
        report["input_sha256"] = hashlib.sha256(raw).hexdigest()
        result = json.dumps(report, indent=2, sort_keys=True, ensure_ascii=False) + "\n"
        if a.output is not None:
            if a.output.resolve() == a.receipt.resolve():
                raise GateError("input cannot be overwritten")
            if a.output.exists():
                raise GateError("output already exists; immutable evidence only")
            with a.output.open("x", encoding="utf-8") as f:
                f.write(result)
        else:
            print(result, end="")
        if a.require_source_ready and not report["source_gates_consistent"]:
            return 2
        return 0
    except (GateError, ValueError, UnicodeError, OSError, json.JSONDecodeError) as ex:
        print("RECEIPT_LATCH_BLOCKED: " + str(ex), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
