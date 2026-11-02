#!/usr/bin/env python3
import copy
import hashlib
import json
from pathlib import Path
import sys
import tempfile
import unittest
import zipfile
sys.path.insert(0,str(Path(__file__).parent))
import playtest_intake as m

SHA='a'*40

def w27():
    rows=[{'taskId':x,'outcome':'NOT_RUN','issue':'NONE','severity':'NONE','seconds':0,'assists':0} for x in m.W27_IDS]
    return {'schema':'b01-13-observer-r1','buildSha':SHA,'source':'OPERATOR_SELF_REPORTED_NO_INDEPENDENT_AUTHENTICATION','actualParticipantsCertifiedByTool':False,'submittedRows':rows,
     'summary':{'gate':'INCOMPLETE','counts':{'PASS':0,'FAIL':0,'BLOCKED':0,'NOT_RUN':8},'p0':0,'assists':0,'medianSeconds':None,
     'criteria':{'C1':None,'C2':None,'C3':None,'C4':None}}}

class TestEvidence(unittest.TestCase):
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup)
  self.root=Path(self.tmp.name)
  self.evidence=self.root/'evidence';self.evidence.mkdir()
  self.out=self.root/'receipt';self.out.mkdir()
  self.report=self.root/'w27.json';self.responses=self.root/'w28.json'
  self.report.write_text(json.dumps(w27()))
  self.responses.write_text(json.dumps(m.template(SHA)))
 def save(self,body): self.responses.write_text(json.dumps(body))
 def check(self): return m.evaluate_w28(json.loads(self.responses.read_text()),SHA,self.evidence)
 def output(self):m.run(['--game-commit',SHA,'--w27-report',str(self.report),'--w28-responses',str(self.responses),'--evidence-dir',str(self.evidence),'--output-dir',str(self.out)])
 def assert_refuse(self,fn,code):
  with self.assertRaisesRegex(m.Refused,code):fn()
 def log_pass(self,case='P01_BASIC_AUTOMATIC_HEAVY',name='test.log'):
  (self.evidence/name).write_text('synthetic test data only\n')
  q=json.loads(self.responses.read_text());case_row=next(x for x in q['cases'] if x['id']==case);case_row.update(status='PASS',reason='NONE',evidence=name);self.save(q)

 def test_template_17_unique(self):
  t=m.template(SHA);self.assertEqual(len(t['cases']),17);self.assertEqual(len(set(x['id'] for x in t['cases'])),17)
 def test_blank_always_hold(self):
  r=self.check();self.assertEqual(r['productGate'],'HOLD_INDEPENDENT_G0_G1_AND_REAL_HUMAN_ACCEPTANCE');self.assertEqual(r['participantSessionsIndependentlyVerified'],0)
 def test_valid_8_w27_notrun(self):self.assertEqual(m.validate_w27(w27(),SHA)['pilot_gate'],'INCOMPLETE')
 def test_w27_wrong_sha(self):
  d=w27();d['buildSha']='b'*40;self.assert_refuse(lambda:m.validate_w27(d,SHA),'W27_DIFFERENT_GAME_SHA')
 def test_w27_forged_human(self):
  d=w27();d['actualParticipantsCertifiedByTool']=True;self.assert_refuse(lambda:m.validate_w27(d,SHA),'W27_FALSE_HUMAN')
 def test_w27_wrong_provenance(self):
  d=w27();d['source']='CERTIFIED';self.assert_refuse(lambda:m.validate_w27(d,SHA),'W27_PROVENANCE')
 def test_w27_duplicate_task(self):
  d=w27();d['submittedRows'][1]['taskId']='PT01';self.assert_refuse(lambda:m.validate_w27(d,SHA),'W27_TASK')
 def test_w27_fake_success_count(self):
  d=w27();d['summary']['counts']['PASS']=8;self.assert_refuse(lambda:m.validate_w27(d,SHA),'W27_SUMMARY_COUNT')
 def test_w27_bool_seconds_not_valid(self):
  d=w27();d['submittedRows'][0]['seconds']=False;self.assert_refuse(lambda:m.validate_w27(d,SHA),'W27_METRICS')
 def test_w27_unrun_cannot_have_time(self):
  d=w27();d['submittedRows'][0]['seconds']=3;self.assert_refuse(lambda:m.validate_w27(d,SHA),'W27_NOTRUN_MEASURED')
 def test_w27_fail_requires_issue(self):
  d=w27();d['submittedRows'][0]['outcome']='FAIL';self.assert_refuse(lambda:m.validate_w27(d,SHA),'W27_FAIL_WITHOUT_ISSUE')
 def test_w28_build_sha_rejection(self):
  d=m.template('b'*40);self.save(d);self.assert_refuse(self.check,'W28_GAME_MISMATCH')
 def test_w28_participant_forged(self):
  d=m.template(SHA);d['realHumanParticipants']=1;self.save(d);self.assert_refuse(self.check,'W28_NO_PARTICIPANT')
 def test_w28_duplicates_rejected(self):
  d=m.template(SHA);d['cases'][0]['id']=d['cases'][1]['id'];self.save(d);self.assert_refuse(self.check,'W28_CASE_ID')
 def test_w28_pass_requires_physical_evidence(self):
  d=m.template(SHA);d['cases'][0]['status']='PASS';self.save(d);self.assert_refuse(self.check,'W28_RESULT_WITHOUT_EVIDENCE')
 def test_w28_fail_requires_reason(self):
  d=m.template(SHA);d['cases'][0].update(status='FAIL',evidence='test.log');(self.evidence/'test.log').write_text('x');self.save(d)
  self.assert_refuse(self.check,'W28_REASON_MISMATCH')
 def test_w28_physical_sha(self):
  self.log_pass();r=self.check();e=r['evidence'][0];self.assertEqual(e['sha256'],hashlib.sha256(b'synthetic test data only\n').hexdigest());self.assertEqual(r['coverage'],'P0_INCOMPLETE_OR_FAILED')
 def test_w28_must_not_reuse_evidence(self):
  self.log_pass();d=json.loads(self.responses.read_text());d['cases'][1].update(status='PASS',reason='NONE',evidence='test.log');self.save(d);self.assert_refuse(self.check,'W28_EVIDENCE_REUSED')
 def test_path_parent_escape(self):
  self.log_pass();d=json.loads(self.responses.read_text());d['cases'][0]['evidence']='../other.log';self.save(d);self.assert_refuse(self.check,'EVIDENCE_ESCAPE')
 def test_path_absolute_refusal(self):
  self.log_pass();d=json.loads(self.responses.read_text());d['cases'][0]['evidence']=str(self.evidence/'test.log');self.save(d);self.assert_refuse(self.check,'EVIDENCE_ESCAPE')
 def test_symlink_evidence_refusal(self):
  x=self.root/'original.log';x.write_text('proof')
  try:(self.evidence/'aliased.log').symlink_to(x)
  except OSError:self.skipTest('symlink permissions restricted')
  d=m.template(SHA);d['cases'][0].update(status='PASS',evidence='aliased.log');self.save(d);self.assert_refuse(self.check,'EVIDENCE_SYMLINK')
 def test_missing_evidence_file_refusal(self):
  d=m.template(SHA);d['cases'][0].update(status='PASS',evidence='missing.log');self.save(d);self.assert_refuse(self.check,'EVIDENCE_NOT_REGULAR')
 def test_softlock_no_phantom_pass(self):
  self.log_pass('P12_LOANER_POST_WIN_CONTINUATION');self.assert_refuse(self.check,'W28_PHANTOM_SOFTLOCK_PASS')
 def test_softlock_verified_continuation_not_native_pass(self):
  self.log_pass('P12_LOANER_POST_WIN_CONTINUATION');d=json.loads(self.responses.read_text());d['softlockDisposition']='CONTINUE_VERIFIED';self.save(d)
  r=self.check();self.assertEqual(r['productGate'],'HOLD_INDEPENDENT_G0_G1_AND_REAL_HUMAN_ACCEPTANCE')
 def test_softlock_failed_must_be_unresolved(self):
  self.log_pass('P12_LOANER_POST_WIN_CONTINUATION');d=json.loads(self.responses.read_text());d['softlockDisposition']='CONTINUE_VERIFIED';d['cases'][11]['status']='FAIL';d['cases'][11]['reason']='SOFTLOCK';self.save(d);self.assert_refuse(self.check,'W28_RECOVERY_WITHOUT_PASS')
 def test_softlock_fail_unresolved_allowed(self):
  self.log_pass('P12_LOANER_POST_WIN_CONTINUATION');d=json.loads(self.responses.read_text());d['softlockDisposition']='UNRESOLVED';d['cases'][11]['status']='FAIL';d['cases'][11]['reason']='SOFTLOCK';self.save(d);self.assertEqual(self.check()['softlockDisposition'],'UNRESOLVED')
 def test_blocked_does_not_claim_success(self):
  d=m.template(SHA);d['cases'][0].update(status='BLOCKED',reason='INTEGRATION_MISSING');self.save(d);self.assertEqual(self.check()['cases']['P01_BASIC_AUTOMATIC_HEAVY'],'BLOCKED')
 def test_zip_requires_actual_zip_extension(self):
  self.log_pass('P15_PLAYER_ZIP_BOOT','game.log');self.assert_refuse(self.check,'W28_PLAYER_ZIP_REQUIRED')
 def test_zip_rejects_bad_magic(self):
  (self.evidence/'fake.zip').write_bytes(b'PK\x03\x04bogus');d=m.template(SHA);d['cases'][14].update(status='PASS',evidence='fake.zip');self.save(d);self.assert_refuse(self.check,'ZIP_INVALID')
 def test_zip_requires_player_structure(self):
  with zipfile.ZipFile(self.evidence/'b.zip','w') as z:z.writestr('notes.txt','not a game');z.writestr('x.csv','dummy');z.writestr('x2.csv','dummy')
  d=m.template(SHA);d['cases'][14].update(status='PASS',evidence='b.zip');self.save(d);self.assert_refuse(self.check,'ZIP_PLAYER_EXE_MISSING')
 def test_zip_valid_structure_still_no_boot_claim(self):
  with zipfile.ZipFile(self.evidence/'player.zip','w') as z:
   z.writestr('RoboRougelike.exe','synthetic exe');z.writestr('UnityPlayer.dll','synthetic dll');z.writestr('RoboRougelike_Data/boot.config','synthetic config')
  d=m.template(SHA);d['cases'][14].update(status='PASS',evidence='player.zip');self.save(d)
  result=self.check();self.assertEqual(result['evidence'][0]['zipStructure'],'ARCHIVE_STRUCTURE_PRESENT_NOT_BOOT_PROOF');self.assertIn('NOT_INDEPENDENTLY',result['windowsPlayerBoot'])
 def test_zip_traversal_rejected(self):
  with zipfile.ZipFile(self.evidence/'trav.zip','w') as z:
   z.writestr('../RoboRougelike.exe','synthetic');z.writestr('UnityPlayer.dll','synthetic');z.writestr('Game_Data/boot.config','synthetic')
  d=m.template(SHA);d['cases'][14].update(status='PASS',evidence='trav.zip');self.save(d);self.assert_refuse(self.check,'ZIP_TRAVERSAL')
 def test_2_fights_requires_log(self):
  (self.evidence/'shot.png').write_bytes(b'dummy');d=m.template(SHA);d['cases'][13].update(status='PASS',evidence='shot.png');self.save(d);self.assert_refuse(self.check,'W28_PLAYER_FIGHTS_LOG_REQUIRED')
 def test_cli_receipt_physically_emits_both_files(self):
  self.output();j=json.loads((self.out/'W28_RECEIPT.json').read_text());self.assertEqual(j['gameCommit'],SHA);self.assertEqual(j['participantSessionsIndependentlyVerified'],0)
  self.assertEqual(len((self.out/'W28_RECEIPT.csv').read_text().splitlines()),18)
 def test_cli_refuses_overwrite_preserves_original_bytes(self):
  (self.out/'W28_RECEIPT.json').write_text('FOREIGN');self.assert_refuse(self.output,'REFUSE_OVERWRITE');self.assertEqual((self.out/'W28_RECEIPT.json').read_text(),'FOREIGN')
 def test_cli_template_physical(self):
  f=self.root/'template.json';m.run(['--game-commit',SHA,'--make-template',str(f)])
  self.assertEqual(len(json.loads(f.read_text())['cases']),17)
  self.assert_refuse(lambda:m.run(['--game-commit',SHA,'--make-template',str(f)]),'REFUSE_OVERWRITE')
 def test_evidence_input_cannot_be_output_dir(self):
  self.assert_refuse(lambda:m.run(['--game-commit',SHA,'--w27-report',str(self.report),'--w28-responses',str(self.responses),'--evidence-dir',str(self.evidence),'--output-dir',str(self.evidence)]),'OUTSIDE_EVIDENCE_DIR')
 def test_template_prohibits_invalid_sha(self):self.assert_refuse(lambda:m.template('broken'),'GAME_COMMIT_SHA')
 def test_git_worktree_output_forbidden(self):
  (self.out/'.git').mkdir()
  self.assert_refuse(self.output,'REFUSE_GIT_WORKTREE')
 def test_no_pii_free_text_in_template(self):
  j=json.dumps(m.template(SHA));self.assertNotIn('email',j.lower());self.assertNotIn('participantName',j)

if __name__=='__main__':unittest.main(verbosity=2)
