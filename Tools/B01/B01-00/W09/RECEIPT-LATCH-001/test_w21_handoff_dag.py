"""W21 dependency graph original owned QA, source receipts != native Unity."""
import copy
import io
import json
from contextlib import redirect_stdout,redirect_stderr
from pathlib import Path
import tempfile
import unittest
from w21_handoff_dag import evaluate,main,Blocked

FIXTURE=Path(__file__).with_name("w21_handoff_sources.json")
def load():return json.loads(FIXTURE.read_text(encoding="utf-8"))
class W21CrossSubsystemDAG(unittest.TestCase):
    def invalid(self,fn):
        v=load();fn(v)
        with self.assertRaises(Blocked):evaluate(v)
    def test_stage_order_has_no_missing_dependency(self):
        x=evaluate(load())
        self.assertEqual(set(x["dependency_order"]),set(load()["packages"]))
        order=x["dependency_order"]
        for n in order:
            for dep in load()["packages"][n]["depends_on"]:
                self.assertLess(order.index(dep),order.index(n))
    def test_selected_owner_seams_frozen(self):
        x=evaluate(load())
        self.assertEqual(x["owner_selected"],{"r4":647,"api":681})
        self.assertEqual(x["alternatives_hold"],[652,682,644,604])
    def test_690_only_producer_not_independent_g0(self):
        x=evaluate(load())
        n=next(t for t in x["stages"] if t["id"]=="host_r5")
        self.assertEqual(n["owner_pr"],690)
        self.assertEqual(n["source_g0"],"PENDING")
        self.assertEqual(n["independent_status"],"PENDING")
    def test_691_stage_inherits_host_block(self):
        x=evaluate(load())
        n=next(t for t in x["stages"] if t["id"]=="api_v2")
        self.assertIn("INDEPENDENT_R5_COMPONENT_DESTROY_QA",n["inherited_blockers"])
    def test_669_storage_trust_not_self_checksum(self):
        x=evaluate(load())
        n=next(t for t in x["stages"] if t["id"]=="storage")
        self.assertIn("TRUSTED_MONOTONIC_WATERMARK_OR_SCOPED_THREAT_MODEL",n["immediate_blockers"])
    def test_native_runner_source_not_native_g1(self):
        x=evaluate(load())
        n=next(t for t in x["stages"] if t["id"]=="native_runner")
        self.assertEqual(n["native_g1"],"NOT_RUN")
    def test_real_private_game_adapter_not_implemented(self):
        x=evaluate(load())
        n=next(t for t in x["stages"] if t["id"]=="canonical_adapter")
        self.assertIsNone(n["owner_pr"])
        self.assertIn("REAL_FROZEN_510_PRIVATE_RIG_LEDGER_PORT",n["immediate_blockers"])
    def test_no_codex_ack_fabricated(self):
        x=evaluate(load())
        self.assertEqual(x["codex_ack"],"NONE")
        self.assertIsNone(x["acceptance_latency_seconds"])
    def test_integrator_inherits_both_unity_and_save(self):
        x=evaluate(load())
        self.assertIn("OWNER_EXECUTES_ACTIVATED_UNITY_6000_3_14F1",x["critical_integrator_blockers"])
        self.assertIn("REAL_FROZEN_510_PRIVATE_RIG_LEDGER_PORT",x["critical_integrator_blockers"])
    def test_deduplicate_inherited_requirements(self):
        x=evaluate(load())
        self.assertEqual(len(x["critical_integrator_blockers"]),
                         len(set(x["critical_integrator_blockers"])))
    def test_no_release_promoted_by_source_pass(self):
        x=evaluate(load())
        self.assertFalse(x["merged"])
        self.assertEqual(x["live_game_saveload"],"HOLD")
        self.assertFalse(any(n["ready_for_integrator"] for n in x["stages"]))
    def test_deny_old_r4_changed(self):
        self.invalid(lambda x:x["source_sha"].update(r4_frozen="0"*40))
    def test_deny_api_682_chosen(self):
        self.invalid(lambda x:x["authorities"]["api"].update(winner_pr=682))
    def test_deny_r4_652_accepted(self):
        self.invalid(lambda x:x["authorities"]["r4"].update(winner_pr=652))
    def test_deny_r5_new_sha_not_tested(self):
        self.invalid(lambda x:x["source_sha"].update(host_v5="1"*40))
    def test_deny_api_v2_sha_not_tested(self):
        self.invalid(lambda x:x["source_sha"].update(api_v2="1"*40))
    def test_deny_game510_source_changed(self):
        self.invalid(lambda x:x["source_sha"].update(game510="1"*40))
    def test_deny_save556_wire_changed(self):
        self.invalid(lambda x:x["source_sha"].update(save556="1"*40))
    def test_deny_storage_original_source_changed(self):
        self.invalid(lambda x:x["source_sha"].update(storage_v1="1"*40))
    def test_deny_operator_executed_unverified(self):
        self.invalid(lambda x:x["operator_access"].update(native_unity_executed=True))
    def test_deny_operator_pc_access_fabricated(self):
        self.invalid(lambda x:x["operator_access"].update(owner_pc_access=True))
    def test_deny_ssh_unsolicited(self):
        self.invalid(lambda x:x["operator_access"].update(ssh=True))
    def test_deny_fake_integrator_acceptance(self):
        self.invalid(lambda x:x["integrator_outcome"].update(status="ACCEPTED"))
    def test_deny_fake_zero_latency(self):
        self.invalid(lambda x:x["integrator_outcome"].update(elapsed_s=0))
    def test_deny_fake_zero_regressions(self):
        self.invalid(lambda x:x["integrator_outcome"].update(regressions=0))
    def test_deny_fake_codex_ack(self):
        self.invalid(lambda x:x["integrator_outcome"].update(ack="yes"))
    def test_deny_fake_merge(self):
        self.invalid(lambda x:x["authorities"].update(merge=True))
    def test_deny_fake_new_api_independent_g0(self):
        self.invalid(lambda x:x["stage_status"]["api_v2"].update(FORMAL_G0="PASS"))
    def test_deny_fake_r5_independent_review(self):
        self.invalid(lambda x:x["stage_status"]["host_r5"].update(INDEPENDENT="PASS"))
    def test_deny_claim_native_g1(self):
        self.invalid(lambda x:x["stage_status"]["native_runner"].update(NATIVE_G1="PASS"))
    def test_deny_hide_api_681_concurrent_qa(self):
        self.invalid(lambda x:x["security_evidence"].update(api_681_security_688="RELEASE_GREEN"))
    def test_deny_hide_prepared_world_685(self):
        self.invalid(lambda x:x["security_evidence"].update(api_682_security_685="RELEASE_GREEN"))
    def test_deny_hide_669_external_rollback(self):
        self.invalid(lambda x:x["security_evidence"].update(storage_669_680="RELEASE_GREEN"))
    def test_deny_change_host_dependency(self):
        self.invalid(lambda x:x["packages"]["api_v2"].update(depends_on=[]))
    def test_deny_promote_preview_to_live_save(self):
        self.invalid(lambda x:x["stage_status"]["canonical_adapter"].update(SOURCE_IMPL="PASS"))
    def test_deny_mix_alternative_source(self):
        self.invalid(lambda x:x["packages"]["api_v2"].update(pr=682))
    def test_deny_missing_stage(self):
        self.invalid(lambda x:x["packages"].pop("storage"))
    def test_deny_add_duplicate_fake_stage(self):
        self.invalid(lambda x:x["packages"].update(extra={"pr":999}))
    def test_deny_change_r5_ci_provenance(self):
        self.invalid(lambda x:x["packages"]["host_r5"].update(evidence_run=0))
    def test_deny_fake_release(self):
        self.invalid(lambda x:x["authorities"].update(releases=True))
    def test_cli_stdout_reports_real_hold_and_hash(self):
        buf=io.StringIO()
        with redirect_stdout(buf):self.assertEqual(main([str(FIXTURE)]),0)
        x=json.loads(buf.getvalue())
        self.assertEqual(x["verdict"],"HOLD_W21_INTEGRATION_DEPENDENCY_GATES")
        self.assertEqual(len(x["input_sha256"]),64)
    def test_cli_release_returns_3(self):
        with redirect_stdout(io.StringIO()):
            self.assertEqual(main([str(FIXTURE),"--require-release"]),3)
    def test_cli_never_overwrites_foreign(self):
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/"foreign"
            p.write_text("KEEP")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(FIXTURE),"--output",str(p)]),2)
            self.assertEqual(p.read_text(),"KEEP")
    def test_cli_atomic_write_once(self):
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/"own"
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(FIXTURE),"--output",str(p)]),0)
                self.assertEqual(main([str(FIXTURE),"--output",str(p)]),2)
            self.assertEqual(json.loads(p.read_text())["codex_ack"],"NONE")
    def test_cli_invalid_utf8_fails_closed(self):
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/"bad"
            p.write_bytes(b"\xff")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(p)]),2)
if __name__=="__main__":unittest.main()
