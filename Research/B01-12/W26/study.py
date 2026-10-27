#!/usr/bin/env python3
"""B01-12 UX.U5 anonymous field-study CSV generator/validator.

No network, external deps, human-data collection, or Unity assumptions.
To prevent fabricated conclusions: never report an experimental PASS without
120 complete, uniquely keyed actual participant observations.
"""
import argparse
import csv
import hashlib
import io
import json
import math
from pathlib import Path
import sys
import tempfile
import unittest

BLOB = "81f8913159f7a2273d73ab65f2b8d9abdab8d7f9"
SHA256 = "20daf5c0d63fa9aff22b65d0896fedac78d7dcf81030a4be9be2e1c521bb0124"
ORDERS = ["ABC", "ACB", "BAC", "BCA", "CAB", "CBA", "ABC", "CBA"]
CASES = ["UX01", "UX02", "UX03", "UX04", "UX05"]
FIELDS = [
    "participant_id", "source_blob", "variant_order", "variant", "case_id",
    "sequence", "fixture", "answer", "seconds_to_first_answer",
    "wrong_clicks", "physically_approved", "socket_approved", "correct_cause",
    "correct_next_action", "retry_seconds", "observer_note"
]
BOOL = {"0", "1"}
ANSWERS = {"UNKNOWN", "REJECTED", "APPROVED", "NA"}
FIXTURES = ["TYPED_SOCKET_UNVERIFIED", "INCOMPLETE_EXTERNAL_BROADPHASE",
            "PHYSICS_QUERY_UNVERIFIED", "UNMEASURED_PHYSICAL_ENVELOPE"]


def expected_rows():
    rows = []
    for idx, order in enumerate(ORDERS):
        pid = f"P{idx + 1:02d}"
        for seq, (variant, case) in enumerate(
            ((variant, case) for variant in order for case in CASES), 1
        ):
            fixture = {
                "UX01": "UNMEASURED_PHYSICAL_ENVELOPE",
                "UX02": FIXTURES[idx // 2],
                "UX03": "LOGICAL_OCCUPIED",
                "UX04": "CHL_WRONG_CP_AND_FALL",
                "UX05": "CHL_RETRY_AND_RIG_CONTEXT",
            }[case]
            rows.append({
                "participant_id": pid, "source_blob": BLOB, "variant_order": order,
                "variant": variant, "case_id": case, "sequence": str(seq),
                "fixture": fixture, "answer": "", "seconds_to_first_answer": "",
                "wrong_clicks": "", "physically_approved": "", "socket_approved": "",
                "correct_cause": "", "correct_next_action": "",
                "retry_seconds": "", "observer_note": ""
            })
    return rows


def git_blob_sha(data):
    return hashlib.sha1(b"blob " + str(len(data)).encode() + b"\x00" + data).hexdigest()


def verify_html(path):
    data = Path(path).read_bytes()
    return {"git_blob": git_blob_sha(data), "sha256": hashlib.sha256(data).hexdigest(),
            "match": git_blob_sha(data) == BLOB and hashlib.sha256(data).hexdigest() == SHA256}


def write_csv(path, rows):
    with Path(path).open("w", encoding="utf-8", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=FIELDS, lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)


def read_csv(path):
    with Path(path).open("r", encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f)
        if reader.fieldnames != FIELDS:
            raise ValueError("CSV_SCHEMA_MISMATCH: expected exact ordered columns")
        rows = list(reader)
    return rows


def validate(rows):
    """Return issues and signed completeness; never treat blank template as data."""
    errors = []
    reference = expected_rows()
    if len(rows) != len(reference):
        errors.append(f"ROW_COUNT: expected 120, found {len(rows)}")
    expected = {(r["participant_id"], r["variant"], r["case_id"]): r for r in reference}
    seen = set()
    completed = []
    for num, row in enumerate(rows, 2):
        key = (row.get("participant_id"), row.get("variant"), row.get("case_id"))
        if key in seen:
            errors.append(f"line {num}: DUPLICATE_KEY {key}")
        seen.add(key)
        template = expected.get(key)
        if template is None:
            errors.append(f"line {num}: UNKNOWN_OR_UNAUTHORIZED_PARTICIPANT_VARIANT_CASE {key}")
            continue
        for item in ("source_blob", "variant_order", "sequence", "fixture"):
            if row.get(item) != template[item]:
                errors.append(f"line {num}: {item.upper()}_MISMATCH")
        observed = [row.get(field, "") for field in
                    ("answer", "seconds_to_first_answer", "wrong_clicks",
                     "physically_approved", "socket_approved", "correct_cause", "correct_next_action")]
        if not any(observed) and not row.get("retry_seconds", ""):
            continue  # blank observations are NOT_RUN
        if not all(observed):
            errors.append(f"line {num}: PARTIAL_OBSERVATION (complete all mandatory columns)")
            continue
        try:
            if row["answer"] not in ANSWERS:
                raise ValueError("answer must be UNKNOWN/REJECTED/APPROVED/NA")
            secs = float(row["seconds_to_first_answer"])
            wrong = int(row["wrong_clicks"])
            if not math.isfinite(secs) or secs < 0 or wrong < 0 or str(wrong) != row["wrong_clicks"]:
                raise ValueError("negative/nonfinite/invalid time or clicks")
            for b in ("physically_approved", "socket_approved", "correct_cause", "correct_next_action"):
                if row[b] not in BOOL:
                    raise ValueError(f"{b} must be 0 or 1")
            if row["case_id"] == "UX05":
                r = float(row["retry_seconds"])
                if not math.isfinite(r) or r < 0:
                    raise ValueError("retry_seconds invalid")
            elif row["retry_seconds"] != "":
                raise ValueError("retry_seconds reserved for UX05")
            if row["case_id"] in ("UX01", "UX02") and row["answer"] == "NA":
                raise ValueError("classification required for UX01/UX02")
        except (ValueError, TypeError, OverflowError) as exc:
            errors.append(f"line {num}: INVALID_OBSERVATION {exc}")
            continue
        completed.append(row)
    if set(expected) != seen:
        errors.append(f"MISSING_KEYS: {len(set(expected)-seen)}")
    return errors, completed


def ratio(rows, test):
    return sum(1 for row in rows if test(row)) / len(rows) if rows else 0.0


def report(rows, html_check):
    errors, completed = validate(rows)
    total = len(expected_rows())
    response = {
        "artifact": "B01-12-PASS3-UX-U5-PILOT",
        "source_blob": BLOB,
        "source_sha256": SHA256,
        "html_integrity": "PASS" if html_check["match"] else "FAIL",
        "html_actual_git_blob": html_check["git_blob"],
        "participant_observations": len(completed),
        "expected_observations": total,
        "study_status": "NOT_RUN",
        "test_status": "NOT_RUN",
        "errors": errors,
        "hypotheses": {"H1": "NOT_RUN", "H2": "NOT_RUN", "H3": "NOT_RUN"},
        "limitations": "Exploratory n=8 with repeated treatments; no inference to Unity or real motor/PhysX."
    }
    if not html_check["match"]:
        response["errors"].append("HTML_SOURCE_CHANGED: must review and re-pin study")
        response["test_status"] = "FAIL"
        return response
    if errors:
        response["test_status"] = "FAIL"
        return response
    if len(completed) != total:
        # No verdict is possible on incomplete or uncollected human observations.
        return response
    # Only actual complete observations qualify for a descriptive, exploratory score.
    response["study_status"] = "OBSERVED_COMPLETE_UNVERIFIED_SELF_REPORT"
    h1 = [r for r in completed if r["case_id"] in ("UX01", "UX02")]
    h2a = [r for r in completed if r["case_id"] == "UX02" and r["variant"] == "A"]
    h2b = [r for r in completed if r["case_id"] == "UX02" and r["variant"] == "B"]
    h3a = [r for r in completed if r["case_id"] == "UX04"]
    h3b = [r for r in completed if r["case_id"] == "UX05"]
    h1rate = ratio(h1, lambda r: r["answer"] == "UNKNOWN")
    falses = sum(1 for r in h1 if r["physically_approved"] == "1" or r["socket_approved"] == "1")
    h2lift = ratio(h2b, lambda r: r["correct_cause"] == "1") - ratio(h2a, lambda r: r["correct_cause"] == "1")
    h3cause = ratio(h3a, lambda r: r["correct_cause"] == "1")
    h3next = ratio(h3b, lambda r: r["correct_next_action"] == "1")
    h3retry = ratio(h3b, lambda r: float(r["retry_seconds"]) <= 5)
    response["metrics"] = {
        "H1_unknown_correct": {"n": len(h1), "ratio": round(h1rate, 4)},
        "H1_false_authorization": {"count": falses},
        "H2_B_minus_A_cause_percentage_points": round(100*h2lift, 2),
        "H3_failure_reason_correct": {"n": len(h3a), "ratio": round(h3cause, 4)},
        "H3_retry_next_action_correct": {"n": len(h3b), "ratio": round(h3next, 4)},
        "H3_retry_located_within_5s": {"n": len(h3b), "ratio": round(h3retry, 4)},
    }
    response["hypotheses"] = {
        "H1": "PASS_EXPLORATORY" if h1rate >= .90 and falses == 0 else "FAIL_EXPLORATORY",
        "H2": "PASS_EXPLORATORY" if h2lift >= .25 else "FAIL_EXPLORATORY",
        "H3": "PASS_EXPLORATORY" if h3cause >= .8 and h3next >= .9 and h3retry >= .9 else "FAIL_EXPLORATORY"
    }
    response["test_status"] = "ANALYZED_EXPLORATORY_ONLY"
    return response


class Selftest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.verify = {"git_blob": BLOB, "match": True}

    def complete(self):
        rows = expected_rows()
        for r in rows:
            r.update(answer="UNKNOWN" if r["case_id"] in ("UX01", "UX02") else "REJECTED" if r["case_id"] == "UX03" else "NA",
                     seconds_to_first_answer="2.5", wrong_clicks="0", physically_approved="0",
                     socket_approved="0", correct_cause="1", correct_next_action="1",
                     retry_seconds="3" if r["case_id"] == "UX05" else "")
        return rows

    def test_01_template_is_empty_not_pass(self):
        self.assertEqual(report(expected_rows(), self.verify)["hypotheses"]["H1"], "NOT_RUN")
    def test_02_complete_synthetic_pass_is_explicitly_exploratory(self):
        z = report(self.complete(), self.verify)
        self.assertEqual(z["test_status"], "ANALYZED_EXPLORATORY_ONLY")
        self.assertEqual(z["hypotheses"]["H1"], "PASS_EXPLORATORY")
    def test_03_duplicate_keys_rejected(self):
        r=self.complete();r[1]["case_id"]=r[0]["case_id"]
        self.assertEqual(report(r,self.verify)["test_status"], "FAIL")
    def test_04_wrong_blob_rejected(self):
        r=self.complete();r[0]["source_blob"]="wrong"
        self.assertEqual(report(r,self.verify)["test_status"], "FAIL")
    def test_05_nonfinite_time_rejected(self):
        r=self.complete();r[0]["seconds_to_first_answer"]="NaN"
        self.assertEqual(report(r,self.verify)["test_status"], "FAIL")
    def test_06_partial_row_rejected(self):
        r=expected_rows();r[0]["answer"]="UNKNOWN"
        self.assertEqual(report(r,self.verify)["test_status"], "FAIL")
    def test_07_wrong_variant_sequence_rejected(self):
        r=self.complete();r[0]["sequence"]="16"
        self.assertEqual(report(r,self.verify)["test_status"], "FAIL")
    def test_08_incomplete_not_fail_and_not_pass(self):
        r=self.complete();r[-1]["answer"]="";r[-1]["seconds_to_first_answer"]="";r[-1]["wrong_clicks"]="";r[-1]["physically_approved"]="";r[-1]["socket_approved"]="";r[-1]["correct_cause"]="";r[-1]["correct_next_action"]="";r[-1]["retry_seconds"]=""
        z=report(r,self.verify)
        self.assertEqual(z["test_status"],"NOT_RUN")
    def test_09_false_mount_authorization_fails_H1(self):
        r=self.complete();r[0]["physically_approved"]="1"
        self.assertEqual(report(r,self.verify)["hypotheses"]["H1"],"FAIL_EXPLORATORY")
    def test_10_h2_not_automatically_pass(self):
        r=self.complete()
        for x in r:
            if x["case_id"]=="UX02" and x["variant"]=="B": x["correct_cause"]="0"
        self.assertEqual(report(r,self.verify)["hypotheses"]["H2"],"FAIL_EXPLORATORY")
    def test_11_h3_threshold_negative(self):
        r=self.complete()
        for x in r:
            if x["case_id"]=="UX05": x["retry_seconds"]="6.1"
        self.assertEqual(report(r,self.verify)["hypotheses"]["H3"],"FAIL_EXPLORATORY")
    def test_12_modified_html_is_blocked(self):
        z=report(self.complete(),{"git_blob":"x","match":False})
        self.assertEqual(z["test_status"],"FAIL")
    def test_13_case_footprints_count(self):
        r=expected_rows()
        self.assertEqual(len(r),120)
        self.assertEqual(sum(x["case_id"]=="UX02" for x in r),24)
    def test_14_mismatch_header_rejected(self):
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/"x.csv"
            p.write_text("participant_id,wrong_header\n",encoding="utf-8")
            with self.assertRaises(ValueError): read_csv(p)
    def test_15_csv_roundtrip(self):
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/"data.csv";write_csv(p,expected_rows())
            self.assertEqual(read_csv(p),expected_rows())
    def test_16_exact_git_blob_integrity(self):
        html=Path(__file__).with_name("index.html")
        if html.exists(): self.assertTrue(verify_html(html)["match"])
    def test_17_extra_result_not_applicable_to_other_cases(self):
        r=self.complete();r[0]["retry_seconds"]="1"
        self.assertEqual(report(r,self.verify)["test_status"],"FAIL")


    def test_18_ux03_matches_actual_html(self):
        html = Path(__file__).with_name("index.html").read_text(encoding="utf-8")
        self.assertEqual({r["fixture"] for r in expected_rows() if r["case_id"]=="UX03"}, {"LOGICAL_OCCUPIED"})
        self.assertIn("LOGICAL_OCCUPIED", html)
        self.assertNotIn("LOGICAL_OVERLAP", html)
    def test_19_old_stale_fixture_rejected(self):
        rows = expected_rows()
        for r in rows:
            if r["case_id"] == "UX03": r["fixture"] = "LOGICAL_OVERLAP"
        errors, completed = validate(rows)
        self.assertEqual(sum("FIXTURE_MISMATCH" in e for e in errors), 24)
        self.assertEqual(len(completed), 0)
    def test_20_new_blank_matrix_valid_but_not_run(self):
        rows = expected_rows()
        self.assertEqual(len(rows), 120)
        self.assertEqual(validate(rows), ([], []))
        self.assertEqual(report(rows, self.verify)["study_status"], "NOT_RUN")


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument("command", choices=["init","report","selftest"])
    p.add_argument("--csv", default="observations.csv")
    p.add_argument("--html", default=str(Path(__file__).with_name("index.html")))
    p.add_argument("--out", default="study_report.json")
    args=p.parse_args()
    if args.command=="selftest":
        suite=unittest.defaultTestLoader.loadTestsFromTestCase(Selftest)
        r=unittest.TextTestRunner(verbosity=2).run(suite)
        return 0 if r.wasSuccessful() else 1
    if args.command=="init":
        if Path(args.csv).exists():
            print("REFUSED: observations file already exists; never overwrite real data",file=sys.stderr)
            return 2
        write_csv(args.csv,expected_rows())
        print(f"CREATED {args.csv}: 120 empty observations, 8 participants, pinned {BLOB}")
        return 0
    try:
        result=report(read_csv(args.csv),verify_html(args.html))
    except (ValueError,OSError) as exc:
        print(f"INPUT_ERROR: {exc}",file=sys.stderr)
        return 2
    Path(args.out).write_text(json.dumps(result,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(json.dumps({k:result[k] for k in ("study_status","test_status","participant_observations","expected_observations","hypotheses","errors")},ensure_ascii=False))
    return 2 if result["test_status"]=="FAIL" else 0


if __name__=="__main__":
    raise SystemExit(main())
