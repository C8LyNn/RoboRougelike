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
  (self.evidence/name).write_bytes(b'synthetic test data only\n')
  q=json.loads(self.responses.read_text());case_row=next(x for x in q['cases'] if x['id']==case);case_row.update(status='PASS',reason='NONE',evidence=name)
  if case=='P12_LOANER_POST_WIN_CONTINUATION':
   (self.evidence/'before.json').write_text('{"driveHp":0}')
   (self.evidence/'after.json').write_text('{"driveHp":100}')
   (self.evidence/'loaner-player.log').write_text('operator-submitted test-only player log')
   case_row['evidence']={'before':'before.json','after':'after.json','playerLog':'loaner-player.log'}
  self.save(q)

 def test_template_17_unique(self):
  t=m.template(SHA);self.assertEqual(len(t['cases']),21);self.assertEqual(len(set(x['id'] for x in t['cases'])),21)
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
  d=m.template(SHA);d['cases'][0]['id']=d['cases'][1]['id'];self.save(d);self.assert_refuse(self.check,'W29_CASE_ID')
 def test_w28_pass_requires_physical_evidence(self):
  d=m.template(SHA);d['cases'][0]['status']='PASS';self.save(d);self.assert_refuse(self.check,'W29_RESULT_WITHOUT_EVIDENCE')
 def test_w28_fail_requires_reason(self):
  d=m.template(SHA);d['cases'][0].update(status='FAIL',evidence='test.log');(self.evidence/'test.log').write_text('x');self.save(d)
  self.assert_refuse(self.check,'W29_REASON_MISMATCH')
 def test_w28_physical_sha(self):
  self.log_pass();r=self.check();e=r['evidence'][0];self.assertEqual(e['sha256'],hashlib.sha256(b'synthetic test data only\n').hexdigest());self.assertEqual(r['coverage'],'P0_INCOMPLETE_OR_FAILED')
 def test_w28_must_not_reuse_evidence(self):
  self.log_pass();d=json.loads(self.responses.read_text());d['cases'][1].update(status='PASS',reason='NONE',evidence='test.log');self.save(d);self.assert_refuse(self.check,'W29_EVIDENCE_REUSED')
 def test_path_parent_escape(self):
  self.log_pass();d=json.loads(self.responses.read_text());d['cases'][0]['evidence']='../other.log';self.save(d);self.assert_refuse(self.check,'EVIDENCE_ESCAPE')
 def test_path_absolute_refusal(self):
  self.log_pass();d=json.loads(self.responses.read_text());d['cases'][0]['evidence']=str(self.evidence/'test.log');self.save(d);self.assert_refuse(self.check,'EVIDENCE_(ESCAPE|PATH)')
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
  self.log_pass('P12_LOANER_POST_WIN_CONTINUATION');d=json.loads(self.responses.read_text());d['softlockDisposition']='CONTINUE_VERIFIED';d['cases'][11]['status']='FAIL';d['cases'][11]['reason']='SOFTLOCK';d['cases'][11]['evidence']='loaner-player.log';self.save(d);self.assert_refuse(self.check,'W28_RECOVERY_WITHOUT_PASS')
 def test_softlock_fail_unresolved_allowed(self):
  self.log_pass('P12_LOANER_POST_WIN_CONTINUATION');d=json.loads(self.responses.read_text());d['softlockDisposition']='UNRESOLVED';d['cases'][11]['status']='FAIL';d['cases'][11]['reason']='SOFTLOCK';d['cases'][11]['evidence']='loaner-player.log';self.save(d);self.assertEqual(self.check()['softlockDisposition'],'UNRESOLVED')
 def test_blocked_does_not_claim_success(self):
  d=m.template(SHA);d['cases'][0].update(status='BLOCKED',reason='INTEGRATION_MISSING');self.save(d);self.assertEqual(self.check()['cases']['P01_BASIC_AUTOMATIC_HEAVY'],'BLOCKED')
 def test_zip_requires_actual_zip_extension(self):
  self.log_pass('P15_PLAYER_ZIP_BOOT','game.log');self.assert_refuse(self.check,'W31_BOOT_SESSION_REQUIRED')
 def test_zip_rejects_bad_magic(self):
  (self.evidence/'fake.zip').write_bytes(b'PK\x03\x04bogus');d=m.template(SHA);d['cases'][14].update(status='PASS',evidence={'playerZip':'fake.zip','playerLog':'player.log','sessionReceipt':'session.json'});(self.evidence/'player.log').write_text('test only');self.save(d);self.assert_refuse(self.check,'ZIP_INVALID')
 def test_zip_requires_player_structure(self):
  with zipfile.ZipFile(self.evidence/'b.zip','w') as z:z.writestr('notes.txt','not a game');z.writestr('x.csv','dummy');z.writestr('x2.csv','dummy')
  d=m.template(SHA);d['cases'][14].update(status='PASS',evidence={'playerZip':'b.zip','playerLog':'player.log','sessionReceipt':'session.json'});(self.evidence/'player.log').write_text('test only');self.save(d);self.assert_refuse(self.check,'ZIP_PLAYER_EXE_MISSING')
 def test_zip_valid_structure_still_no_boot_claim(self):
  with zipfile.ZipFile(self.evidence/'player.zip','w') as z:
   z.writestr('RoboRougelike.exe','synthetic exe');z.writestr('UnityPlayer.dll','synthetic dll');z.writestr('RoboRougelike_Data/boot.config','synthetic config')
  d=m.template(SHA);d['cases'][14].update(status='PASS',evidence={'playerZip':'player.zip','playerLog':'player.log','sessionReceipt':'session.json'});(self.evidence/'player.log').write_text('test only');self.save(d)
  self.make_session_receipt('player.zip','player.log')
  result=self.check();self.assertEqual(result['evidence'][0]['zipStructure'],'ARCHIVE_STRUCTURE_PRESENT_NOT_BOOT_PROOF');self.assertIn('NOT_INDEPENDENTLY',result['windowsPlayerBoot'])
 def test_zip_traversal_rejected(self):
  with zipfile.ZipFile(self.evidence/'trav.zip','w') as z:
   z.writestr('../RoboRougelike.exe','synthetic');z.writestr('UnityPlayer.dll','synthetic');z.writestr('Game_Data/boot.config','synthetic')
  d=m.template(SHA);d['cases'][14].update(status='PASS',evidence={'playerZip':'trav.zip','playerLog':'player.log','sessionReceipt':'session.json'});(self.evidence/'player.log').write_text('test only');self.save(d);self.assert_refuse(self.check,'ZIP_TRAVERSAL')
 def test_2_fights_requires_log(self):
  (self.evidence/'shot.png').write_bytes(b'dummy');d=m.template(SHA);d['cases'][13].update(status='PASS',evidence='shot.png');self.save(d);self.assert_refuse(self.check,'W29_EVIDENCE_ROLE_FORMAT')
 def test_cli_receipt_physically_emits_both_files(self):
  self.output();j=json.loads((self.out/'W29_RECEIPT.json').read_text());self.assertEqual(j['gameCommit'],SHA);self.assertEqual(j['participantSessionsIndependentlyVerified'],0)
  self.assertEqual(len((self.out/'W29_RECEIPT.csv').read_text().splitlines()),22)
 def test_cli_refuses_overwrite_preserves_original_bytes(self):
  (self.out/'W29_RECEIPT.json').write_text('FOREIGN');self.assert_refuse(self.output,'REFUSE_OVERWRITE');self.assertEqual((self.out/'W29_RECEIPT.json').read_text(),'FOREIGN')
 def test_cli_template_physical(self):
  f=self.root/'template.json';m.run(['--game-commit',SHA,'--make-template',str(f)])
  self.assertEqual(len(json.loads(f.read_text())['cases']),21)
  self.assert_refuse(lambda:m.run(['--game-commit',SHA,'--make-template',str(f)]),'REFUSE_OVERWRITE')
 def test_evidence_input_cannot_be_output_dir(self):
  self.assert_refuse(lambda:m.run(['--game-commit',SHA,'--w27-report',str(self.report),'--w28-responses',str(self.responses),'--evidence-dir',str(self.evidence),'--output-dir',str(self.evidence)]),'OUTSIDE_EVIDENCE_DIR')
 def test_template_prohibits_invalid_sha(self):self.assert_refuse(lambda:m.template('broken'),'GAME_COMMIT_SHA')
 def test_git_worktree_output_forbidden(self):
  (self.out/'.git').mkdir()
  self.assert_refuse(self.output,'REFUSE_GIT_WORKTREE')
 def test_no_pii_free_text_in_template(self):
  j=json.dumps(m.template(SHA));self.assertNotIn('email',j.lower());self.assertNotIn('participantName',j)


 def make_session_receipt(self,archive='game.zip',player_log='player.log',**overrides):
  exe=b''
  with zipfile.ZipFile(self.evidence/archive) as z:
   exe=z.read('RoboRougelike.exe')
  lb=(self.evidence/player_log).read_bytes()
  doc={'schema':'b01-13-w30-windows-player-session-r1',
       'gameCommit':SHA,'gameCommitProvenance':'OPERATOR_ASSERTED_NOT_EMBEDDED_IN_PLAYER',
       'processExitCode':0,'captureStatus':'PROCESS_EXITED_LOG_PRESENT',
       'exeSha256':hashlib.sha256(exe).hexdigest(),'exeBytes':len(exe),
       'playerLog':{'sha256':hashlib.sha256(lb).hexdigest(),'bytes':len(lb)},
       'utcStart':'2026-11-03T12:00:00+00:00','utcEnd':'2026-11-03T12:02:00+00:00',
       'unityNativeG1':'NOT_VERIFIED','actualGameplayPassed':'NOT_VERIFIED',
       'humanSessionsIndependentlyVerified':0,'playerLaunchG1':'NOT_INDEPENDENTLY_VERIFIED'}
  doc.update(overrides)
  (self.evidence/'session.json').write_text(json.dumps(doc))
  return doc
 def set_boot(self,archive='game.zip'):
  self.update_row('P15_PLAYER_ZIP_BOOT',status='PASS',evidence={'playerZip':archive,'playerLog':'player.log','sessionReceipt':'session.json'})
 def make_player_zip(self,name='game.zip'):
  with zipfile.ZipFile(self.evidence/name,'w') as z:
   z.writestr('RoboRougelike.exe','test-only fake bytes')
   z.writestr('UnityPlayer.dll','test-only fake dll')
   z.writestr('RoboRougelike_Data/boot.config','test-only fake config')
  return name
 def update_row(self,case,**changes):
  d=json.loads(self.responses.read_text());next(x for x in d['cases'] if x['id']==case).update(changes);self.save(d)

 def test_w29_boot_rejects_legacy_single_zip_even_if_structurally_valid(self):
  name=self.make_player_zip();self.update_row('P15_PLAYER_ZIP_BOOT',status='PASS',evidence=name)
  self.assert_refuse(self.check,'W31_BOOT_SESSION_REQUIRED')
 def test_w29_boot_rejects_missing_log(self):
  name=self.make_player_zip();self.update_row('P15_PLAYER_ZIP_BOOT',status='PASS',evidence={'playerZip':name,'playerLog':'missing.log','sessionReceipt':'session.json'})
  self.assert_refuse(self.check,'EVIDENCE_NOT_REGULAR')
 def test_w29_boot_accepts_physical_zip_and_log_but_no_independent_boot_claim(self):
  name=self.make_player_zip();(self.evidence/'player.log').write_text('test-only bytes')
  self.update_row('P15_PLAYER_ZIP_BOOT',status='PASS',evidence={'playerZip':name,'playerLog':'player.log','sessionReceipt':'session.json'})
  self.make_session_receipt(name,'player.log')
  r=self.check();self.assertEqual([p['role'] for p in r['evidence']],['player_zip','player_log','session_receipt'])
  self.assertEqual(r['cases']['P15_PLAYER_ZIP_BOOT'],'PASS')
  self.assertEqual(r['windowsPlayerBoot'],'NOT_INDEPENDENTLY_VERIFIED_BY_THIS_TOOL')
  self.assertEqual(r['productGate'],'HOLD_INDEPENDENT_G0_G1_AND_REAL_HUMAN_ACCEPTANCE')
  lines=m.make_csv(r).splitlines();self.assertEqual(len(lines),24)
 def test_w29_boot_rejects_nonlog_second_file(self):
  name=self.make_player_zip();(self.evidence/'result.json').write_text('test-only')
  self.update_row('P15_PLAYER_ZIP_BOOT',status='PASS',evidence={'playerZip':name,'playerLog':'result.json','sessionReceipt':'session.json'})
  self.assert_refuse(self.check,'W29_EVIDENCE_ROLE_FORMAT')
 def test_w29_boot_rejects_unrecognized_metadata_fields(self):
  self.make_player_zip();(self.evidence/'player.log').write_text('test-only')
  self.update_row('P15_PLAYER_ZIP_BOOT',status='PASS',evidence={'playerZip':'game.zip','playerLog':'player.log','sessionReceipt':'session.json','humanCertified':True})
  self.assert_refuse(self.check,'W31_BOOT_SESSION_REQUIRED')
 def test_w29_recovery_rejects_old_one_log_pass(self):
  (self.evidence/'single.log').write_text('test-only')
  self.update_row('P12_LOANER_POST_WIN_CONTINUATION',status='PASS',evidence='single.log')
  self.assert_refuse(self.check,'W29_RECOVERY_BUNDLE_REQUIRED')
 def test_w29_recovery_rejects_identical_before_and_after(self):
  for name,bytes_ in [('before.json',b'{"hp":0}'),('after.json',b'{"hp":0}'),('player.log',b'test-only')]:
   (self.evidence/name).write_bytes(bytes_)
  self.update_row('P12_LOANER_POST_WIN_CONTINUATION',status='PASS',evidence={'before':'before.json','after':'after.json','playerLog':'player.log'})
  self.assert_refuse(self.check,'W29_RECOVERY_IDENTICAL_STATES')
 def test_w29_recovery_rejects_missing_after(self):
  (self.evidence/'before.json').write_text('{"hp":0}');(self.evidence/'player.log').write_text('test-only')
  self.update_row('P12_LOANER_POST_WIN_CONTINUATION',status='PASS',evidence={'before':'before.json','after':'missing.json','playerLog':'player.log'})
  self.assert_refuse(self.check,'EVIDENCE_NOT_REGULAR')
 def test_w29_recovery_actual_three_file_receipts_still_not_real_qa(self):
  self.log_pass('P12_LOANER_POST_WIN_CONTINUATION')
  d=json.loads(self.responses.read_text());d['softlockDisposition']='CONTINUE_VERIFIED';self.save(d)
  r=self.check();self.assertEqual(len(r['evidence']),3)
  self.assertEqual([x['role'] for x in r['evidence']],['before','after','player_log'])
  self.assertEqual(r['productGate'],'HOLD_INDEPENDENT_G0_G1_AND_REAL_HUMAN_ACCEPTANCE')
 def test_w29_all_four_extra_missing_are_p0(self):
  r=self.check()
  for case in ('P18_SAW_DEFAULT_MOUNT_AND_OVERFLOW','P19_EMP_DETACH_DIRECT_GUARD','P20_GARAGE_PHYSICAL_ZONE_COMMIT','P21_SAVE_ONE_OWNER_COLD_UNITY'):
   self.assertIn(case,r['missingOrFailingP0'])
 def test_w29_legacy_w28_schema_must_not_mix_silent_version(self):
  d=m.template(SHA);d['schema']='b01-13-w28-acceptance-r1';self.save(d);self.assert_refuse(self.check,'W28_SCHEMA')


 def test_w31_reject_zip_and_log_without_session_receipt(self):
  name=self.make_player_zip();(self.evidence/'player.log').write_text('synthetic log')
  self.set_boot(name)
  self.assert_refuse(self.check,'EVIDENCE_NOT_REGULAR')
 def test_w31_reject_receipt_game_sha_mismatch(self):
  name=self.make_player_zip();(self.evidence/'player.log').write_text('synthetic log')
  self.set_boot(name);self.make_session_receipt(gameCommit='b'*40)
  self.assert_refuse(self.check,'W31_SESSION_GAME_SHA')
 def test_w31_reject_receipt_nonzero_process_exit(self):
  self.make_player_zip();(self.evidence/'player.log').write_text('synthetic log')
  self.set_boot();self.make_session_receipt(processExitCode=3)
  self.assert_refuse(self.check,'W31_SESSION_PROCESS_EXIT')
 def test_w31_reject_receipt_missing_log(self):
  self.make_player_zip();(self.evidence/'player.log').write_text('synthetic log')
  self.set_boot();self.make_session_receipt(playerLog=None)
  self.assert_refuse(self.check,'W31_SESSION_LOG_SCHEMA')
 def test_w31_reject_receipt_player_log_tamper(self):
  self.make_player_zip();(self.evidence/'player.log').write_text('synthetic log')
  self.set_boot();self.make_session_receipt()
  (self.evidence/'player.log').write_text('TAMPERED')
  self.assert_refuse(self.check,'W31_PLAYER_LOG_MISMATCH')
 def test_w31_reject_receipt_exe_not_matching_zip(self):
  self.make_player_zip();(self.evidence/'player.log').write_text('synthetic log')
  self.set_boot();self.make_session_receipt(exeSha256='b'*64)
  self.assert_refuse(self.check,'W31_SESSION_ARCHIVE_EXE_MISMATCH')
 def test_w31_reject_receipt_invalid_unity_claim(self):
  self.make_player_zip();(self.evidence/'player.log').write_text('synthetic log')
  self.set_boot();self.make_session_receipt(unityNativeG1='PASS')
  self.assert_refuse(self.check,'W31_SESSION_FALSE_QA_CLAIM')
 def test_w31_reject_receipt_false_time(self):
  self.make_player_zip();(self.evidence/'player.log').write_text('synthetic log')
  self.set_boot();self.make_session_receipt(utcEnd='2026-11-03T11:00:00+00:00')
  self.assert_refuse(self.check,'W31_SESSION_TIME')
 def test_w31_reject_receipt_fake_schema(self):
  self.make_player_zip();(self.evidence/'player.log').write_text('synthetic log')
  self.set_boot();self.make_session_receipt(schema='not-our-tool')
  self.assert_refuse(self.check,'W31_SESSION_SCHEMA')
 def test_w31_reject_wrong_sibling_of_exe(self):
  with zipfile.ZipFile(self.evidence/'game.zip','w') as z:
   z.writestr('RoboRougelike.exe',b'test-only fake bytes')
   z.writestr('other/UnityPlayer.dll',b'test-only fake dll')
   z.writestr('RoboRougelike_Data/boot.config',b'test-only fake config')
  (self.evidence/'player.log').write_text('synthetic log')
  self.set_boot();self.make_session_receipt()
  self.assert_refuse(self.check,'W31_SESSION_ARCHIVE_EXE_MISMATCH')
 def test_w31_large_real_file_zip_over_64_mib_streaming_allowed(self):
  # Full original R2 rejected this 66 MiB physically real file with FILE_SIZE.
  with zipfile.ZipFile(self.evidence/'game.zip','w',compression=zipfile.ZIP_STORED,allowZip64=True) as z:
   with z.open('RoboRougelike.exe','w',force_zip64=True) as fd:
    for _ in range(66):fd.write(b'x'*1024*1024)
   z.writestr('UnityPlayer.dll',b'synthetic dll')
   z.writestr('RoboRougelike_Data/boot.config',b'synthetic config')
  self.assertGreater((self.evidence/'game.zip').stat().st_size,64*1024*1024)
  (self.evidence/'player.log').write_text('synthetic log')
  self.set_boot();self.make_session_receipt()
  r=self.check()
  self.assertEqual(r['w31SessionCrossCheck'],'W30_RECEIPT_EXE_AND_LOG_BYTES_MATCH_NOT_INDEPENDENT_UNITY_QA')
  self.assertEqual(len(r['evidence']),3)
  self.assertEqual(r['windowsPlayerBoot'],'NOT_INDEPENDENTLY_VERIFIED_BY_THIS_TOOL')
 def test_w31_normal_zip_receipt_cross_checked_but_not_g1(self):
  self.make_player_zip();(self.evidence/'player.log').write_text('synthetic log')
  self.set_boot();self.make_session_receipt()
  r=self.check()
  self.assertEqual(r['evidence'][2]['crossCheck'],r['w31SessionCrossCheck'])
  self.assertEqual(r['productGate'],'HOLD_INDEPENDENT_G0_G1_AND_REAL_HUMAN_ACCEPTANCE')

if __name__=='__main__':unittest.main(verbosity=2)
