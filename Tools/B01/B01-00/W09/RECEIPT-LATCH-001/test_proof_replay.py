"""W09 extra negative tests: anti-replay between independent agents and Codex."""
import unittest
from receipt_latch import audit
from test_receipt_latch import make_record, SHA, REPO

INTAKE = REPO + "issues/466#issuecomment-10001"
INDEPENDENT = REPO + "issues/466#issuecomment-10002"
OFFER = REPO + "issues/79#issuecomment-10003"
ACK = REPO + "issues/79#issuecomment-10004"


class ProofReplay(unittest.TestCase):
    def test_same_comment_cannot_masquerade_as_two_independent_reviews(self):
        x = make_record()
        x["qa"]["intake"]["url"] = INTAKE
        x["qa"]["independent"]["url"] = INTAKE
        result = audit(x)
        self.assertFalse(result["source_gates_consistent"])
        self.assertIn("independent_receipt_reused", result["blockers"])

    def test_distinct_independent_receipts_allow_source_candidate(self):
        x = make_record()
        x["qa"]["intake"]["url"] = INTAKE
        x["qa"]["independent"]["url"] = INDEPENDENT
        self.assertTrue(audit(x)["source_gates_consistent"])

    def test_offer_cannot_double_as_codex_ack(self):
        x = make_record()
        x["qa"]["intake"]["url"] = INTAKE
        x["qa"]["independent"]["url"] = INDEPENDENT
        x["offer"] = {"status": "OFFERED", "sha": SHA, "url": OFFER}
        x["ack"] = {"status": "YES", "actor": "CODEX", "sha": SHA, "url": OFFER}
        result = audit(x)
        self.assertFalse(result["ack_record_consistent"])
        self.assertIn("codex_ack_reused_offer_comment", result["blockers"])

    def test_distinct_codex_ack_receipt_remains_only_structurally_consistent(self):
        x = make_record()
        x["qa"]["intake"]["url"] = INTAKE
        x["qa"]["independent"]["url"] = INDEPENDENT
        x["offer"] = {"status": "OFFERED", "sha": SHA, "url": OFFER}
        x["ack"] = {"status": "YES", "actor": "CODEX", "sha": SHA, "url": ACK}
        result = audit(x)
        self.assertTrue(result["ack_record_consistent"])
        self.assertEqual(result["evidence_authentication"], "NOT_PERFORMED_OFFLINE")


if __name__ == "__main__":
    unittest.main()
