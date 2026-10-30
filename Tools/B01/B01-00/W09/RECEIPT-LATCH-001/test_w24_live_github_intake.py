"""B01-00 W24 original source tests for *live GitHub* intake logic.

The actual workflow uses GitHub API GET against original PRs and Actions.
These deterministic regression tests mock responses; no false external G1.
"""
import copy
import io
import json
import tempfile
import unittest
from pathlib import Path
from contextlib import redirect_stderr,redirect_stdout
import w24_live_github_intake as gate

def fixture():
    prs={}
    for n,sha in gate.PR_PINS.items():
        prs[str(n)]={"number":n,"state":"open","draft":True,"merged":False,
          "head":{"sha":sha},"base":{"repo":{"full_name":gate.REPO}},
          "title":("ONE versioned owner" if n==749 else "C# owner"),
          "body":("Canonical old .lease" if n==753 else "ColdRestart generation" if n==750
           else "#749 and #732 ACCEPTED_SOURCE_ONLY" if n==755
           else "#749 SOURCE_RELEASE_GATE=GREEN_SCOPE_ONLY_FROZEN_PRIVATE_510_HOLD 24/24" if n==758
           else "#750 SOURCE 10/10, native NOT_RUN" if n==766
           else "#749 honest crash 16 PASS" if n==768 else "signed")}
    runs={str(run):{"id":run,"head_sha":gate.PR_PINS[pr],
                  "status":"completed","conclusion":"success"}
          for run,pr in gate.ACTION_RUN_PINS.items()}
    issue={"number":731,"body":"## OWNER DECISIONS — APPROVED\n"
       +"\n".join(gate.POLICY_LINES.values())
       +"\n#749 is the currently stated selected bridge"}
    return{"pulls":prs,"runs":runs,"hq":issue}

class W24LiveIntakeUnitTests(unittest.TestCase):
    def deny(self,callback):
        payload=fixture();callback(payload)
        with self.assertRaises(gate.IntakeRejected):
            gate.validate(payload)
    def test_selected_749_only(self):
        v=gate.validate(fixture())
        self.assertEqual(v["selected_save_bridge"],749)
        self.assertEqual(v["selected_save_sha"],gate.PR_PINS[749])
    def test_product_still_hold(self):
        v=gate.validate(fixture())
        self.assertEqual(v["product_save_load"],"HOLD")
        self.assertFalse(v["ready_for_install"])
    def test_exclusive_owner(self):
        v=gate.validate(fixture())
        self.assertEqual(v["same_physical_owner"],".owner")
        self.assertEqual(v["legacy_669_raw_store_coinstall"],"FORBIDDEN")
    def test_native_not_run(self):
        self.assertEqual(gate.validate(fixture())["real_native_unity_g1"],"NOT_RUN")
    def test_g0_transport_only(self):
        v=gate.validate(fixture())
        self.assertEqual(v["formal_749_transport_g0"],"ACCEPTED_SOURCE_ONLY")
        self.assertEqual(v["independent_749_SECURITY"],"SOURCE_GREEN_SCOPE_ONLY_B01_61_758")
        self.assertEqual(v["selected_749_crash_source"],"SOURCE_GREEN_SCOPE_ONLY_B01_62_768")
        self.assertEqual(v["independent_750_fixture"],"SOURCE_GREEN_SCOPE_ONLY_B01_61_766")
    def test_two_alt_drafts(self):
        v=gate.validate(fixture())
        self.assertEqual([x["pr"] for x in v["non_selected_verified"]],[753,741])
    def test_750_not_wrong_732_g0(self):
        v=gate.validate(fixture())
        self.assertEqual(v["host_fixture_750"],"SOURCE_PASS_ONLY_NOT_SELECTED_NATIVE_G0")
        self.assertEqual(v["host_fixture_732"],"ACCEPTED_IN_755_SOURCE_ONLY")
    def test_economy_owner_approved_not_game_pass(self):
        v=gate.validate(fixture())
        self.assertEqual(v["gameplay_policies"],"OWNER_APPROVED_IMPLEMENTATION_NOT_CERTIFIED")
        self.assertEqual(v["economy_atomic_journal"],"NOT_PROVEN_IN_749")
    def test_anti_tamper_correct_scope(self):
        self.assertEqual(gate.validate(fixture())["malicious_external_whole_root_tamper"],
                         "OWNER_APPROVED_OUT_OF_RELEASE_SCOPE")
    def test_no_codex_ack(self):
        v=gate.validate(fixture())
        self.assertIsNone(v["codex_ack"])
        self.assertIsNone(v["acceptance_latency"])
        self.assertEqual(v["integration_feedback"],"NOT_MEASURED")
    def test_no_merge(self):
        self.assertFalse(gate.validate(fixture())["merge"])
    def test_output_stable_no_network_data_secrets(self):
        a=gate.validate(fixture())
        self.assertNotIn("token",json.dumps(a))
        self.assertEqual(a,gate.validate(fixture()))
    def test_get_reads_exact_paths(self):
        expected={"/repos/"+gate.REPO+"/issues/731"}
        expected.update("/repos/"+gate.REPO+"/pulls/"+str(n) for n in gate.PR_PINS)
        expected.update("/repos/"+gate.REPO+"/actions/runs/"+str(n)
                        for n in gate.ACTION_RUN_PINS)
        payload=fixture()
        def f(path,token):
            self.assertEqual(token,"fake")
            self.assertIn(path,expected)
            if "/pulls/" in path:return payload["pulls"][path.rsplit("/",1)[-1]]
            if "/runs/" in path:return payload["runs"][path.rsplit("/",1)[-1]]
            return payload["hq"]
        self.assertEqual(gate.obtain(get=f,token="fake"),payload)
    def test_readonly_urllib_url_limited(self):
        with self.assertRaises(gate.IntakeRejected):
            gate.github_get("/user/delete")
    def test_reject_payload_extra(self):
        self.deny(lambda x: x.update(extra=True))
    def test_reject_payload_missing(self):
        self.deny(lambda x: x.pop('runs'))
    def test_reject_issue_not_731(self):
        self.deny(lambda x: x['hq'].update(number=713))
    def test_reject_issue_owner_removed(self):
        self.deny(lambda x: x['hq'].update(body='not authorized'))
    def test_reject_issue_select_753(self):
        self.deny(lambda x: x['hq'].update(body=x['hq']['body'].replace('#749 is the currently stated selected bridge','#753 is the currently stated selected bridge')))
    def test_reject_issue_owner_demoted(self):
        self.deny(lambda x: x['hq'].update(body=x['hq']['body'].replace('APPROVED','PENDING')))
    def test_reject_source_749_sha(self):
        self.deny(lambda x: x['pulls']['749']['head'].update(sha='0'*40))
    def test_reject_source_753_sha(self):
        self.deny(lambda x: x['pulls']['753']['head'].update(sha='1'*40))
    def test_reject_source_741_sha(self):
        self.deny(lambda x: x['pulls']['741']['head'].update(sha='2'*40))
    def test_reject_source_750_sha(self):
        self.deny(lambda x: x['pulls']['750']['head'].update(sha='3'*40))
    def test_reject_source_732_sha(self):
        self.deny(lambda x: x['pulls']['732']['head'].update(sha='4'*40))
    def test_reject_source_755_sha(self):
        self.deny(lambda x: x['pulls']['755']['head'].update(sha='5'*40))
    def test_reject_source_729_sha(self):
        self.deny(lambda x: x['pulls']['729']['head'].update(sha='6'*40))
    def test_reject_source_690_sha(self):
        self.deny(lambda x: x['pulls']['690']['head'].update(sha='7'*40))
    def test_reject_source_pr_749_number(self):
        self.deny(lambda x: x['pulls']['749'].update(number=753))
    def test_reject_source_alt_state_closed(self):
        self.deny(lambda x: x['pulls']['753'].update(state='closed'))
    def test_reject_source_selected_merged(self):
        self.deny(lambda x: x['pulls']['749'].update(merged=True))
    def test_reject_source_750_non_draft(self):
        self.deny(lambda x: x['pulls']['750'].update(draft=False))
    def test_reject_source_other_repo(self):
        self.deny(lambda x: x['pulls']['749']['base']['repo'].update(full_name='evil/FORK'))
    def test_reject_source_missing_749(self):
        self.deny(lambda x: x['pulls'].pop('749'))
    def test_reject_source_fake_extra(self):
        self.deny(lambda x: x['pulls'].update({'999':{}}))
    def test_reject_source_missing_run(self):
        self.deny(lambda x: x['runs'].pop('41133131736'))
    def test_reject_source_run_replaced(self):
        self.deny(lambda x: x['runs']['41133131736'].update(id=1))
    def test_reject_source_run_wrong_head(self):
        self.deny(lambda x: x['runs']['41133131736'].update(head_sha='8'*40))
    def test_reject_source_755_g0_run_wrong_head(self):
        self.deny(lambda x: x['runs']['41134559111'].update(head_sha='9'*40))
    def test_reject_source_750_run_wrong_head(self):
        self.deny(lambda x: x['runs']['41133166507'].update(head_sha='a'*40))
    def test_reject_source_732_run_wrong_head(self):
        self.deny(lambda x: x['runs']['41132037039'].update(head_sha='b'*40))
    def test_reject_source_run_queued(self):
        self.deny(lambda x: x['runs']['41133131736'].update(status='queued'))
    def test_reject_source_run_failed(self):
        self.deny(lambda x: x['runs']['41133131736'].update(conclusion='failure'))
    def test_reject_source_run_cancelled(self):
        self.deny(lambda x: x['runs']['41134559111'].update(conclusion='cancelled'))
    def test_reject_source_missing_755_provenance(self):
        self.deny(lambda x: x['pulls']['755'].update(body='not a G0 receipt'))
    def test_reject_source_missing_r5_fix(self):
        self.deny(lambda x: x['pulls']['750'].update(body='unrelated'))
    def test_reject_source_753_not_lease(self):
        self.deny(lambda x: x['pulls']['753'].update(body='new source text'))
    def test_reject_source_749_not_one(self):
        self.deny(lambda x: x['pulls']['749'].update(title='unapproved variant'))
    def test_reject_no_65_policy(self):
        self.deny(lambda x: x['hq'].update(body=x['hq']['body'].replace('rebuild price = 65%','rebuild price = 100%')))
    def test_reject_no_loaner_policy(self):
        self.deny(lambda x: x['hq'].update(body=x['hq']['body'].replace('One-time weaker emergency loaner robot','Loaner undecided')))
    def test_reject_no_real_win_policy(self):
        self.deny(lambda x: x['hq'].update(body=x['hq']['body'].replace('One free destroyed-part rebuild per verified distinct victory','No free rebuilds')))
    def test_reject_no_core_eligibility(self):
        self.deny(lambda x: x['hq'].update(body=x['hq']['body'].replace('living Core AND an authentic eligible next challenge','no limitation')))
    def test_reject_no_crash_safety(self):
        self.deny(lambda x: x['hq'].update(body=x['hq']['body'].replace('honest local crash, torn write, OS process interruption, corruption','malicious disk only')))
    def test_reject_no_tamper_scope(self):
        self.deny(lambda x: x['hq'].update(body=x['hq']['body'].replace('NOT a release requirement','IS a release requirement')))
    def test_reject_fake_function_gate(self):
        self.deny(lambda x: x['hq'].update(body=x['hq']['body'].replace('Block fight start ONLY for a genuinely non-functional robot','Block always')))


    def test_reject_source_758_sha(self):
        self.deny(lambda x: x['pulls']['758']['head'].update(sha='a'*40))
    def test_reject_source_766_sha(self):
        self.deny(lambda x: x['pulls']['766']['head'].update(sha='b'*40))
    def test_reject_source_768_sha(self):
        self.deny(lambda x: x['pulls']['768']['head'].update(sha='c'*40))
    def test_reject_source_758_run_wrong_head(self):
        self.deny(lambda x: x['runs']['41137466367'].update(head_sha='0'*40))
    def test_reject_source_766_run_failed(self):
        self.deny(lambda x: x['runs']['41137933943'].update(conclusion='failure'))
    def test_reject_source_768_run_wrong_head(self):
        self.deny(lambda x: x['runs']['41138019965'].update(head_sha='0'*40))
    def test_reject_source_missing_758_scoped_qa(self):
        self.deny(lambda x: x['pulls']['758'].update(body='no independent source'))
    def test_reject_source_missing_768_crash_qa(self):
        self.deny(lambda x: x['pulls']['768'].update(body='no crash test'))
    def test_reject_source_missing_766_cold_qa(self):
        self.deny(lambda x: x['pulls']['766'].update(body='no fixture test'))

class W24LiveCLI(unittest.TestCase):
    def test_cli_snapshot_returns_source_hold(self):
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/"source.json"
            p.write_text(json.dumps(fixture()))
            out=io.StringIO()
            with redirect_stdout(out):
                self.assertEqual(gate.main(["--snapshot",str(p)]),0)
            self.assertEqual(json.loads(out.getvalue())["real_native_unity_g1"],"NOT_RUN")
    def test_cli_strict_install_returns3(self):
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/"source.json"
            p.write_text(json.dumps(fixture()))
            with redirect_stdout(io.StringIO()):
                self.assertEqual(gate.main(["--snapshot",str(p),"--require-install-ready"]),3)
    def test_cli_invalid_returns2(self):
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/"bad"
            p.write_text("NOT_JSON")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(gate.main(["--snapshot",str(p)]),2)
    def test_cli_refuses_overwrite_foreign(self):
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/"source.json"
            p.write_text(json.dumps(fixture()))
            out=Path(td)/"USER"
            out.write_text("KEEP USER DATA")
            with redirect_stderr(io.StringIO()):
                self.assertEqual(gate.main(["--snapshot",str(p),"--output",str(out)]),2)
            self.assertEqual(out.read_text(),"KEEP USER DATA")
    def test_cli_refuses_overwrite_input(self):
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/"source.json"
            p.write_text(json.dumps(fixture()))
            with redirect_stderr(io.StringIO()):
                self.assertEqual(gate.main(["--snapshot",str(p),"--output",str(p)]),2)
    def test_cli_write_once_valid(self):
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/"source.json"
            p.write_text(json.dumps(fixture()))
            o=Path(td)/"receipt.json"
            with redirect_stderr(io.StringIO()):
                self.assertEqual(gate.main(["--snapshot",str(p),"--output",str(o)]),0)
                self.assertEqual(gate.main(["--snapshot",str(p),"--output",str(o)]),2)
            self.assertEqual(json.loads(o.read_text())["selected_save_bridge"],749)
    def test_does_not_claim_source_is_real_unity(self):
        result=gate.validate(fixture())
        self.assertEqual(result["real_native_unity_g1"],"NOT_RUN")
        self.assertFalse(result["release"])
    def test_every_required_run_checked(self):
        self.assertEqual(set(gate.ACTION_RUN_PINS.values()),{749,755,750,732,758,766,768})
if __name__=="__main__":
    unittest.main()
