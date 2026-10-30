"""W22 B01-00 independently executable governance regression source tests."""
import copy
import io
import json
from contextlib import redirect_stdout,redirect_stderr
from pathlib import Path
import tempfile
import unittest
from w22_one_save_authority import main,validate,Unsafe
FIXTURE=Path(__file__).with_name("w22_one_save_sources.json")
def original():
    return json.loads(FIXTURE.read_text(encoding="utf-8"))
class W22UniqueAuthority(unittest.TestCase):
    def deny(self,mutation):
        o=original()
        mutation(o)
        with self.assertRaises(Unsafe):
            validate(o)
    def test_expected_hold_not_released(self):
        v=validate(original())
        self.assertEqual(v["verdict"],"HOLD_W22_ONE_SAVE_AUTHORITY_NOT_RELEASED")
        self.assertEqual(v["game_save"],"HOLD")
        self.assertEqual(v["g2"],"HOLD")
    def test_selected_only_host_690_api_691(self):
        v=validate(original())
        self.assertEqual(v["selected"],{"host":690,"api":691,"session":712})
        self.assertIn(693,v["alternative_prs"])
        self.assertIn(695,v["alternative_prs"])
    def test_independent_705_source_green_only(self):
        v=validate(original())
        self.assertEqual(v["source_g0_host"],"ACCEPTED_SOURCE_ONLY")
        n=next(z for z in v["stages"] if z["stage"]=="r5_690")
        self.assertEqual(n["native_g1"],"NOT_RUN")
    def test_api_710_p0_red_even_when_physical_g0_source_green(self):
        v=validate(original())
        self.assertEqual(v["api_691_release"],"P0_RED_MULTI_OWNER")
        self.assertEqual(v["source_g0_api"],"ACCEPTED_SOURCE_ONLY")
    def test_session_712_no_inherited_707_green(self):
        v=validate(original())
        self.assertEqual(v["session_712_independent"],"NOT_RUN")
    def test_native_711_source_not_g1(self):
        self.assertEqual(validate(original())["native_unity_g1"],"NOT_RUN")
    def test_no_integrator_zero_metrics(self):
        v=validate(original())
        self.assertEqual(v["integrator_metrics"],"NOT_MEASURED")
        self.assertIsNone(v["elapsed_s"])
        self.assertIsNone(v["acceptance_ratio"])
    def test_source_dependency_order(self):
        v=validate(original())
        d=original()["dependencies"]
        for stage,parents in d.items():
            for parent in parents:
                self.assertLess(v["dependency_order"].index(parent),
                                v["dependency_order"].index(stage))
    def test_critical_inherits_710_red(self):
        v=validate(original())
        self.assertIn("NEW_EXCLUSIVE_SESSION_FENCE_710",v["critical_blockers"])
    def test_critical_inherits_707_independent_replay(self):
        v=validate(original())
        self.assertIn("RETEST_707_ON_EXACT_712_SHA",v["critical_blockers"])
    def test_critical_inherits_680_valid_file(self):
        v=validate(original())
        self.assertIn("EXTERNAL_BACKUP_VALID_ROLLBACK_POLICY_680",v["critical_blockers"])
    def test_critical_inherits_native_user_consent(self):
        v=validate(original())
        self.assertIn("OWNER_OPERATOR_OPT_IN_ACTIVATED_UNITY",v["critical_blockers"])
    def test_critical_inherits_private_510(self):
        v=validate(original())
        self.assertIn("NEW_VERSIONED_REAL_510_PRIVATE_RIG_LEDGER_WORLD_PORT",v["critical_blockers"])
    def test_blocker_count_unique(self):
        v=validate(original())
        self.assertEqual(len(v["critical_blockers"]),v["critical_blockers_count"])
    def test_lease_owner_requires_full_world_retire(self):
        v=validate(original())
        self.assertIn("PHYSICAL_PARTIAL_OWNER_RETIREMENT_CONFIRM",v["critical_blockers"])
    def test_runner_still_operator_only(self):
        n=next(z for z in validate(original())["stages"] if z["stage"]=="windows_711")
        self.assertEqual(n["physical_g0"],"DISPOSABLE_SCRATCH_ONLY")
        self.assertFalse(n["ready"])
    def test_deny_tamper_game_sha(self):
        self.deny(lambda x:x["git_sha"].update(game510="a"*40))
    def test_deny_tamper_save_sha(self):
        self.deny(lambda x:x["git_sha"].update(save556="b"*40))
    def test_deny_tamper_r5_sha(self):
        self.deny(lambda x:x["git_sha"].update(r5_selected_690="c"*40))
    def test_deny_tamper_api_sha(self):
        self.deny(lambda x:x["git_sha"].update(api_v2_selected_691="d"*40))
    def test_deny_tamper_session_sha(self):
        self.deny(lambda x:x["git_sha"].update(session_new_712="e"*40))
    def test_deny_tamper_runner_sha(self):
        self.deny(lambda x:x["git_sha"].update(windows_711="e"*40))
    def test_deny_tamper_alt_693(self):
        self.deny(lambda x:x["git_sha"].update(r5_alt_693="a"*40))
    def test_deny_tamper_storage_sha(self):
        self.deny(lambda x:x["git_sha"].update(storage_669="a"*40))
    def test_deny_tamper_external_anchor_sha(self):
        self.deny(lambda x:x["git_sha"].update(external_anchor_709="a"*40))
    def test_deny_select_693_alt(self):
        self.deny(lambda x:x["selected_owners"].update(r5=693))
    def test_deny_select_695_alt(self):
        self.deny(lambda x:x["selected_owners"].update(api=695))
    def test_deny_select_682_alt(self):
        self.deny(lambda x:x["selected_owners"].update(api=682))
    def test_deny_erase_alt_693(self):
        self.deny(lambda x:x["alternatives_hold"]["r5"].clear())
    def test_deny_erase_alt_695(self):
        self.deny(lambda x:x["alternatives_hold"]["api"].remove(695))
    def test_deny_fake_r5_native(self):
        self.deny(lambda x:x["source_evidence"]["r5_690"].update(native="PASS"))
    def test_deny_fake_api_source_release(self):
        self.deny(lambda x:x["source_evidence"]["api_691"].update(independent="RELEASE_GREEN"))
    def test_deny_fake_712_independent_release(self):
        self.deny(lambda x:x["source_evidence"]["session_712"].update(independent="PASS"))
    def test_deny_promote_712_physical(self):
        self.deny(lambda x:x["source_evidence"]["session_712"].update(physical="SOURCE_G0_ACCEPTED"))
    def test_deny_fake_709_trust_certification(self):
        self.deny(lambda x:x.update(external_anchor_709="CRYPTOGRAPHICALLY_TRUSTED"))
    def test_deny_erase_680_trust_warning(self):
        self.deny(lambda x:x["source_evidence"]["storage_669"].update(full_g0="PASS"))
    def test_deny_tamper_r5_inner_zip(self):
        self.deny(lambda x:x["source_evidence"]["r5_690"].update(zip_sha256="f"*64))
    def test_deny_tamper_r5_zip_entries(self):
        self.deny(lambda x:x["source_evidence"]["r5_690"].update(zip_members=26))
    def test_deny_tamper_691_inner_zip(self):
        self.deny(lambda x:x["source_evidence"]["api_691"].update(zip_bytes=61737))
    def test_deny_fake_705_original_qa(self):
        self.deny(lambda x:x["source_evidence"]["r5_690"].update(independent_sha="a"*40))
    def test_deny_fake_710_qa_sha(self):
        self.deny(lambda x:x["source_evidence"]["api_691"].update(independent_sha="b"*40))
    def test_deny_fake_707_release_run(self):
        self.deny(lambda x:x["source_evidence"]["session_712"].update(independent_run=41127040969))
    def test_deny_fake_unity_g1(self):
        self.deny(lambda x:x["gates"].update(native_unity="PASS"))
    def test_deny_fake_game_live(self):
        self.deny(lambda x:x["gates"].update(live_game_save="PASS"))
    def test_deny_fake_codex_ack(self):
        self.deny(lambda x:x["gates"].update(codex_ack="ACK"))
    def test_deny_fake_codex_acceptance(self):
        self.deny(lambda x:x["gates"].update(codex_outcome="ACCEPTED"))
    def test_deny_fake_zero_integrator_retries(self):
        self.deny(lambda x:x["gates"].update(attempts=0))
    def test_deny_fake_zero_regressions(self):
        self.deny(lambda x:x["gates"].update(regressions=0))
    def test_deny_fake_merge(self):
        self.deny(lambda x:x["gates"].update(merged=True))
    def test_deny_fake_operator_pc_access(self):
        self.deny(lambda x:x["gates"].update(operator_pc_access=True))
    def test_deny_fake_license_access(self):
        self.deny(lambda x:x["gates"].update(credential_access=True))
    def test_deny_fake_six_stage_qa(self):
        self.deny(lambda x:x["lifecycle"].update(QA_ACCEPTED=True))
    def test_deny_fake_six_stage_release(self):
        self.deny(lambda x:x["lifecycle"].update(RELEASED=True))
    def test_deny_fake_owner_permission(self):
        self.deny(lambda x:x.update(owner_intake_permission="MERGE_ALLOWED"))
    def test_deny_fake_human_econ(self):
        self.deny(lambda x:x.update(economy="HUMAN_BALANCE_PASS"))
    def test_deny_disable_710_dependency(self):
        self.deny(lambda x:x["requirements"]["api_691"].clear())
    def test_deny_disable_707_dependency(self):
        self.deny(lambda x:x["requirements"]["session_712"].clear())
    def test_deny_omit_private_game_adapter(self):
        self.deny(lambda x:x["dependencies"].pop("real_game_adapter"))
    def test_deny_source_dag_cycle(self):
        self.deny(lambda x:x["dependencies"]["storage_669"].append("codex_integrator"))
    def test_deny_add_unknown_stage(self):
        self.deny(lambda x:x["dependencies"].update({"fake_stage":[]}))
    def test_deny_erase_g1_dependency(self):
        self.deny(lambda x:x["dependencies"]["codex_integrator"].remove("windows_711"))
    def test_cli_valid_hold_receipt(self):
        out=io.StringIO()
        with redirect_stdout(out):
            self.assertEqual(main([str(FIXTURE)]),0)
        r=json.loads(out.getvalue())
        self.assertEqual(r["verdict"],"HOLD_W22_ONE_SAVE_AUTHORITY_NOT_RELEASED")
        self.assertEqual(len(r["input_sha256"]),64)
    def test_cli_release_gate_exit3(self):
        with redirect_stdout(io.StringIO()):
            self.assertEqual(main([str(FIXTURE),"--strict-release"]),3)
    def test_cli_never_overwrites_foreign(self):
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/"KEEP"
            p.write_text("ORIGINAL USER FILE")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(FIXTURE),"--output",str(p)]),2)
            self.assertEqual(p.read_text(),"ORIGINAL USER FILE")
    def test_cli_never_overwrites_input(self):
        with redirect_stderr(io.StringIO()):
            self.assertEqual(main([str(FIXTURE),"--output",str(FIXTURE)]),2)
    def test_cli_write_once(self):
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/"receipt"
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(FIXTURE),"--output",str(p)]),0)
                self.assertEqual(main([str(FIXTURE),"--output",str(p)]),2)
            self.assertEqual(json.loads(p.read_text())["native_unity_g1"],"NOT_RUN")
    def test_cli_bad_utf8_rejected(self):
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/"tamper"
            p.write_bytes(b"\xff")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(main([str(p)]),2)
if __name__=="__main__":
    unittest.main()
