#!/usr/bin/env python3
"""Original physical-file and failure-mode tests for LD02-COHORT-001."""
import contextlib
import hashlib
import io
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

import ld02_cohort as c

SHA = 'a' * 40
EXE = b'EXPLICITLY SYNTHETIC TEST PLAYER BYTES; NOT UNITY'
FULL = [('start', 'FIGHT', 0), ('complete', 'FIGHT', 100),
        ('start', 'GARAGE', 200), ('complete', 'GARAGE', 400),
        ('start', 'REPAIR', 500), ('complete', 'REPAIR', 800),
        ('start', 'CHECKPOINT', 900), ('complete', 'CHECKPOINT', 1100),
        ('end', None, 1200)]


class OriginalCohortTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.o = c.parent_module()

    def setUp(self):
        scratch = tempfile.TemporaryDirectory()
        self.addCleanup(scratch.cleanup)
        self.root = Path(scratch.name).resolve()
        self.logs = self.root / 'sessions'
        self.logs.mkdir()
        self.player = self.root / 'Player.exe'
        self.player.write_bytes(EXE)
        self.output = self.root / 'cohort.json'
        self.scratch = scratch
        for i in range(3):
            self.record('s' + str(i), FULL)

    def header(self, session, mode='technical', source=SHA, exe_sha=None):
        return {'type': 'header', 'schema': self.o.SCHEMA, 'session': session, 'mode': mode,
                'source_sha': source, 'player_sha256': exe_sha or hashlib.sha256(EXE).hexdigest(),
                'approval_ref': 'HQ-REF' if mode == 'human' else None,
                'consent_ref': 'CONSENT-REF' if mode == 'human' else None}

    def record(self, name, events, mode='technical', source=SHA, exe_sha=None):
        rows = []
        self.o.add_row(rows, self.header(name, mode, source, exe_sha))
        for action, task, ms in events:
            self.o.add_row(rows, {'action': action, 'task': task, 'elapsed_ms': ms})
        target = self.logs / (name + '.jsonl')
        target.write_bytes(b''.join(self.o.canonical(r) + b'\n' for r in rows))
        return target

    def rewrite(self, name, events, **kwargs):
        return self.record(name, events, **kwargs)

    def report(self):
        return c.build_report(self.logs, self.player, SHA, self.o)

    def test_real_three_technical_files_and_exact_source(self):
        r = self.report()
        self.assertEqual(r['technical_sessions'], 3)
        self.assertEqual(r['completed_all_four_stages'], 3)
        self.assertEqual(r['stages']['CHECKPOINT']['completed'], 3)
        self.assertEqual(r['stages']['FIGHT']['completed_median_ms_if_at_least_3'], 100)
        self.assertEqual(r['observed_executable_sha256'], self.o.file_sha(self.player))
        self.assertEqual(r['original_observer_source_sha256'], c.BASE_SOURCE_SHA)
        self.assertFalse(r['actual_player_executed_by_this_tool'])
        self.assertFalse(r['human_data_included'])
        self.assertNotIn('s0', json.dumps(r))
        self.assertNotIn(str(self.logs), json.dumps(r))

    def test_abandoned_task_is_not_fake_completion(self):
        self.rewrite('s2', [('start','FIGHT',0),('complete','FIGHT',100),
                            ('start','GARAGE',200),('abandon','GARAGE',450),('end',None,500)])
        r = self.report()
        self.assertEqual(r['completed_all_four_stages'], 2)
        self.assertEqual(r['stages']['GARAGE']['abandoned'], 1)
        self.assertEqual(r['stages']['REPAIR']['not_observed'], 1)
        self.assertIsNone(r['stages']['CHECKPOINT']['completed_median_ms_if_at_least_3'])

    def test_interruption_distinct_from_abandonment(self):
        self.rewrite('s0',[('start','FIGHT',0),('end',None,50)])
        r=self.report()
        self.assertEqual(r['stages']['FIGHT']['interrupted'],1)
        self.assertEqual(r['stages']['FIGHT']['abandoned'],0)
        self.assertEqual(r['stages']['CHECKPOINT']['not_observed'],1)

    def test_hint_and_block_events_are_group_aggregated(self):
        full=[('start','FIGHT',0),('hint','FIGHT',10),('blocked','FIGHT',20),
              ('complete','FIGHT',100),('end',None,105)]
        self.rewrite('s0',full)
        self.rewrite('s1',full)
        r=self.report()
        self.assertEqual(r['hint_events_total'],2)
        self.assertEqual(r['block_events_total'],2)
        self.assertEqual(r['stages']['FIGHT']['completed'],3)

    def test_median_only_for_three_or_more_completions(self):
        self.rewrite('s1',[('start','FIGHT',0),('abandon','FIGHT',120),('end',None,130)])
        r=self.report()
        self.assertEqual(r['stages']['FIGHT']['completed'],2)
        self.assertIsNone(r['stages']['FIGHT']['completed_median_ms_if_at_least_3'])

    def test_three_samples_median_not_biased_by_abandoned_duration(self):
        self.record('s3',[('start','FIGHT',0),('abandon','FIGHT',99999),('end',None,100000)])
        r=self.report()
        self.assertEqual(r['stages']['FIGHT']['completed_median_ms_if_at_least_3'],100)
        self.assertEqual(r['stages']['FIGHT']['abandoned'],1)

    def test_cohort_too_small_fail_closed(self):
        (self.logs/'s2.jsonl').unlink()
        with self.assertRaisesRegex(c.IntakeError,'COHORT_TOO_SMALL'):
            self.report()

    def test_unexpected_extra_file_fail_closed(self):
        (self.logs/'readme.txt').write_text('not a session')
        with self.assertRaisesRegex(c.IntakeError,'UNEXPECTED_FILE'):
            self.report()

    def test_nested_directory_fail_closed(self):
        (self.logs/'nested').mkdir()
        with self.assertRaisesRegex(c.IntakeError,'UNEXPECTED_FILE'):
            self.report()

    def test_renamed_input_filename_fail_closed(self):
        (self.logs/'s2.jsonl').rename(self.logs/'renamed.jsonl')
        with self.assertRaisesRegex(c.IntakeError,'LOG_FILENAME_SESSION_MISMATCH'):
            self.report()

    def test_byte_tamper_rejected_by_original_parent(self):
        p=self.logs/'s1.jsonl'
        p.write_bytes(p.read_bytes().replace(b'"technical"',b'"malicious"'))
        with self.assertRaisesRegex(c.IntakeError,'TAMPERED_SOURCE_LOG'):
            self.report()

    def test_truncated_input_rejected(self):
        p=self.logs/'s1.jsonl'
        p.write_bytes(p.read_bytes()[:-1])
        with self.assertRaisesRegex(c.IntakeError,'TAMPERED_SOURCE_LOG'):
            self.report()

    def test_unfinished_input_rejected(self):
        self.rewrite('s1',[('start','FIGHT',0),('complete','FIGHT',50)])
        with self.assertRaisesRegex(c.IntakeError,'SESSION_NOT_FINISHED'):
            self.report()

    def test_cannot_mix_source_sha_claims(self):
        self.rewrite('s1',FULL,source='b'*40)
        with self.assertRaisesRegex(c.IntakeError,'SOURCE_BUILD_MISMATCH'):
            self.report()

    def test_cannot_mix_binary_hash_claims(self):
        self.rewrite('s2',FULL,exe_sha='b'*64)
        with self.assertRaisesRegex(c.IntakeError,'PLAYER_BINARY_MISMATCH'):
            self.report()

    def test_changing_actual_binary_after_capture_is_detected(self):
        self.player.write_bytes(EXE+b'CHANGED')
        with self.assertRaisesRegex(c.IntakeError,'PLAYER_BINARY_MISMATCH'):
            self.report()

    def test_dry_run_excluded_even_with_valid_chain(self):
        self.rewrite('s1',FULL,mode='dry-run')
        with self.assertRaisesRegex(c.IntakeError,'NONTECHNICAL_OR_HUMAN'):
            self.report()

    def test_human_record_explicitly_refused_despite_mock_references(self):
        self.rewrite('s1',FULL,mode='human')
        with self.assertRaisesRegex(c.IntakeError,'NONTECHNICAL_OR_HUMAN'):
            self.report()

    def test_input_symlink_rejected_when_supported(self):
        target=self.logs/'s0.jsonl'; alias=self.logs/'alias.jsonl'
        try:
            alias.symlink_to(target)
        except (OSError, NotImplementedError):
            self.skipTest('native symlink permissions unavailable; not security PASS')
        with self.assertRaisesRegex(c.IntakeError,'UNEXPECTED_FILE'):
            self.report()

    def test_git_worktree_input_rejected_before_aggregation(self):
        (self.root/'.git').write_text('gitdir: elsewhere')
        with self.assertRaisesRegex(c.IntakeError,'PRIVATE_LOCATION_REJECTED'):
            self.report()
        self.assertFalse(self.output.exists())

    def test_single_valid_source_parent_hash_pin(self):
        self.assertEqual(self.o.file_sha(Path(self.o.__file__)),c.BASE_SOURCE_SHA)
        old=c.BASE_SOURCE_SHA
        try:
            c.BASE_SOURCE_SHA='0'*64
            with self.assertRaisesRegex(c.IntakeError,'ORIGINAL_OBSERVER_SHA256_MISMATCH'):
                c.parent_module()
        finally:
            c.BASE_SOURCE_SHA=old

    def test_real_physical_json_publishing_and_file_digest(self):
        report=self.report()
        digest=c.publish(self.output,report,self.o)
        actual=self.output.read_bytes()
        self.assertEqual(digest,hashlib.sha256(actual).hexdigest())
        self.assertEqual(json.loads(actual)['technical_sessions'],3)
        self.assertNotIn(b'"session"',actual)
        self.assertNotIn(b'HQ-REF',actual)

    def test_exclusive_publish_refuses_overwrite(self):
        report=self.report()
        c.publish(self.output,report,self.o)
        original=self.output.read_bytes()
        with self.assertRaisesRegex(c.IntakeError,'OUTPUT_EXISTS_NO_OVERWRITE'):
            c.publish(self.output,report,self.o)
        self.assertEqual(self.output.read_bytes(),original)

    def test_refuse_output_symlink(self):
        try:
            self.output.symlink_to(self.player)
        except (OSError,NotImplementedError):
            self.skipTest('native symlink unavailable')
        with self.assertRaisesRegex(c.IntakeError,'OUTPUT_PARENT_OR_LINK_INVALID'):
            c.publish(self.output,self.report(),self.o)
        self.assertEqual(self.player.read_bytes(),EXE)

    def test_cli_refuses_output_inside_source_directory(self):
        captured=io.StringIO()
        args=['--sessions-dir',str(self.logs),'--player-file',str(self.player),
              '--source-sha',SHA,'--output',str(self.logs/'report.json')]
        with contextlib.redirect_stderr(captured):
            status=c.main(args)
        self.assertEqual(status,2)
        self.assertIn('OUTPUT_INSIDE_INPUT_DIRECTORY',captured.getvalue())
        self.assertFalse((self.logs/'report.json').exists())

    def test_cli_actual_subprocess_end_to_end_real_disk(self):
        args=[sys.executable,str(Path(c.__file__).resolve()),
              '--sessions-dir',str(self.logs),'--player-file',str(self.player),
              '--source-sha',SHA,'--output',str(self.output)]
        result=subprocess.run(args,capture_output=True,text=True,timeout=15)
        self.assertEqual(result.returncode,0,result.stderr)
        self.assertIn('TECHNICAL_COHORT_COMPLETE',result.stdout)
        self.assertEqual(json.loads(self.output.read_text(encoding='utf-8'))['technical_sessions'],3)

    def test_posix_output_0600_when_supported(self):
        if os.name=='nt':
            self.skipTest('POSIX mode bits are not NTFS ACL; not a Windows PASS')
        c.publish(self.output,self.report(),self.o)
        self.assertEqual(self.output.stat().st_mode & 0o777,0o600)

    def test_invalid_source_sha_argument(self):
        with self.assertRaisesRegex(c.IntakeError,'SOURCE_SHA_INVALID'):
            c.build_report(self.logs,self.player,'not_a_sha',self.o)

    def test_missing_real_binary(self):
        self.player.unlink()
        with self.assertRaisesRegex(c.IntakeError,'PLAYER_FILE_NOT_REGULAR'):
            self.report()

    def test_output_must_be_json(self):
        with self.assertRaisesRegex(c.IntakeError,'OUTPUT_MUST_BE_JSON'):
            c.publish(self.root/'result.csv',self.report(),self.o)


if __name__=='__main__':
    unittest.main(verbosity=2)
