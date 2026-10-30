#!/usr/bin/env python3
"""B01 W13 offline source-only Codex offer receipt gate.

Strictly validates a *declared* handoff record. Does NOT authenticate GitHub
comments, reviewer identities, Codex ACK, Unity native runs or safe migration.
No network or GitHub writes. Never grants game integration or a G2 decision.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import sys

SCHEMA = "b01-source-only-offer-v1"
REPO = "https://github.com/C8LyNn/RoboRougelike/"
HEX40 = re.compile(r"^[0-9a-f]{40}$")
HEX64 = re.compile(r"^[0-9a-f]{64}$")
PKG = re.compile(r"^[A-Z0-9][A-Z0-9-]{3,99}$")
VERSION = re.compile(r"^[1-9][0-9]*\.[0-9]+\.[0-9]+$")


class OfferError(ValueError):
    pass


def obj(x, name):
    if type(x) is not dict:
        raise OfferError(name + ": JSON object required")
    return x


def posint(x):
    return type(x) is int and 0 < x < 2**53


def hash_ok(x, pattern):
    return type(x) is str and bool(pattern.fullmatch(x))


def pr_url(pr):
    return REPO + "pull/" + str(pr)


def offer_url(comment_id):
    return REPO + "issues/79#issuecomment-" + str(comment_id)


def check_offer(record):
    """Return a deterministic advisory, never G0/G1/G2, ACK or integration."""
    doc = obj(record, "document")
    if doc.get("schema") != SCHEMA:
        raise OfferError("unrecognized schema; version migration must be explicit")
    package_id = doc.get("package_id")
    version = doc.get("version")
    if type(package_id) is not str or not PKG.fullmatch(package_id):
        raise OfferError("invalid package_id")
    if type(version) is not str or not VERSION.fullmatch(version):
        raise OfferError("invalid package version")
    source = obj(doc.get("source"), "source")
    for field in ("pr", "sha", "url", "frozen"):
        if field not in source:
            raise OfferError("missing source." + field)
    if not posint(source["pr"]) or not hash_ok(source["sha"], HEX40):
        raise OfferError("invalid source PR or exact hash")
    original = source["sha"]
    blockers = []
    if source["url"] != pr_url(source["pr"]):
        blockers.append("source_url_mismatch")
    if source["frozen"] is not True:
        blockers.append("producer_not_frozen")

    archive = obj(doc.get("physical_archive"), "physical_archive")
    for field in ("sha256", "bytes", "members", "guid_count", "asmdef_count"):
        if field not in archive:
            raise OfferError("missing physical_archive." + field)
    if not hash_ok(archive["sha256"], HEX64):
        blockers.append("archive_hash_invalid")
    if not posint(archive["bytes"]) or not posint(archive["members"]):
        blockers.append("archive_size_or_members_invalid")
    if (type(archive["guid_count"]) is not int or archive["guid_count"] < 1
            or type(archive["asmdef_count"]) is not int or archive["asmdef_count"] < 1
            or (posint(archive["members"]) and
                (archive["guid_count"] > archive["members"]
                 or archive["asmdef_count"] > archive["members"]))):
        blockers.append("archive_structure_invalid")

    g0 = obj(doc.get("g0"), "g0")
    if g0.get("agent") != "B01-40" or g0.get("status") != "G0_ACCEPTED_SOURCE_ONLY":
        blockers.append("independent_physical_g0_not_accepted")
    if g0.get("source_sha") != original:
        blockers.append("g0_source_sha_mismatch")
    if (not posint(g0.get("pr")) or g0.get("pr") == source["pr"]
            or g0.get("url") != pr_url(g0.get("pr"))):
        blockers.append("g0_pr_invalid")
    if not hash_ok(g0.get("qa_sha"), HEX40):
        blockers.append("g0_exact_qa_commit_missing")
    if g0.get("archive_sha256") != archive["sha256"]:
        blockers.append("g0_archive_mismatch")

    qa = obj(doc.get("source_qa"), "source_qa")
    if qa.get("agent") != "B01-61" or qa.get("status") != "PASS_SOURCE_ONLY":
        blockers.append("independent_transaction_source_qa_missing")
    if qa.get("source_sha") != original:
        blockers.append("source_qa_sha_mismatch")
    if (not posint(qa.get("pr")) or qa.get("pr") in (source["pr"], g0.get("pr"))
            or qa.get("url") != pr_url(qa.get("pr"))):
        blockers.append("source_qa_pr_invalid")
    if not hash_ok(qa.get("qa_sha"), HEX40):
        blockers.append("source_qa_exact_commit_missing")

    offer = obj(doc.get("offer"), "offer")
    if offer.get("status") != "SOURCE_ONLY_OFFERED":
        blockers.append("offer_must_be_source_only")
    if not posint(offer.get("comment_id")):
        blockers.append("offer_comment_id_invalid")
    elif offer.get("url") != offer_url(offer["comment_id"]):
        blockers.append("offer_not_in_canonical_codex_inbox")
    if offer.get("source_sha") != original or offer.get("package_id") != package_id:
        blockers.append("offer_wrong_source_or_package")

    exclusions = doc.get("alternatives_hold")
    if (type(exclusions) is not list or not exclusions
            or any(not posint(x) or x == source["pr"] for x in exclusions)
            or len(set(exclusions)) != len(exclusions)):
        blockers.append("alternatives_not_explicitly_excluded")

    states = obj(doc.get("release_constraints"), "release_constraints")
    # This v1 protocol describes an offered-but-not-integratable review packet.
    # If its pending gates change, a *new version and independently reviewed
    # receipt* is required. Never silently reinterpret this v1 as a release.
    required = {
        "native_g1": "NOT_RUN",
        "save_load": "HOLD",
        "rollback_receiver": "HOLD_RETEST_REQUIRED",
        "canonical_migration": "NOT_DONE",
        "g2": "HOLD",
        "codex_ack": "PENDING",
        "integrated": "NO",
        "merge": "NO",
    }
    for name, state in required.items():
        if states.get(name) != state:
            blockers.append("unsafe_or_missing_state_" + name)
    if len(states) != len(required):
        blockers.append("unknown_release_state_fields")

    safe = not blockers
    return {
        "schema": SCHEMA,
        "package_id": package_id,
        "version": version,
        "source_sha": original,
        "source_only_offer_record_consistent": safe,
        "code_offer_authenticated_online": False,
        "independent_g0_authenticated_online": False,
        "native_unity_g1": "NOT_RUN",
        "save_load": "HOLD",
        "rollback_receiver": "HOLD",
        "canonical_migration": "NOT_DONE",
        "codex_ack": "UNCONFIRMED",
        "g2": "HOLD",
        "integrated": False,
        "merge_authorized": False,
        "verdict": "SOURCE_ONLY_RECORD_FOR_MANUAL_VERIFICATION" if safe else "HOLD",
        "blockers": sorted(set(blockers)),
    }


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("receipt", type=Path)
    p.add_argument("--require-structural-offer", action="store_true")
    p.add_argument("--output", type=Path)
    a = p.parse_args(argv)
    try:
        raw = a.receipt.read_bytes()
        report = check_offer(json.loads(raw.decode("utf-8")))
        report["input_sha256"] = hashlib.sha256(raw).hexdigest()
        output = json.dumps(report, sort_keys=True, indent=2) + "\n"
        if a.output is None:
            print(output, end="")
        else:
            if a.output.resolve() == a.receipt.resolve() or a.output.exists():
                raise OfferError("refusing to overwrite evidence")
            with a.output.open("x", encoding="utf-8") as f:
                f.write(output)
        if a.require_structural_offer and not report["source_only_offer_record_consistent"]:
            return 2
        return 0
    except (OfferError, OSError, ValueError, UnicodeError, TypeError) as e:
        print("OFFER_PROTOCOL_HOLD: " + str(e), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
