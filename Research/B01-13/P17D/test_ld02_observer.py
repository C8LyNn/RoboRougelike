#!/usr/bin/env python3
"""Executable red/green tests for operator-owned LD02 physical observation log."""
import io
import os
from pathlib import Path
import tempfile
import types
import unittest

import ld02_observer as o

SHA = 'a' * 40
PLAYER = b'GENUINE TEST BINARY BYTES (synthetic fixture, NOT UNITY PLAYER)'


def header(mode='dry-run'):
    return {'type': 'header', 'schema': o.SCHEMA, 'session': 't1', 'mode': mode,
            'source_sha': SHA, 'player_sha256': 'c' * 64,
            'approval_ref': 'HQ#GATE-D' if mode == 'human' else None,
            'consent_ref': 'LOCAL-CONSENT' if mode == 'human' else None}


def rows_for(*events, mode='dry-run'):
    rows = []
    o.add_row(rows, header(mode))
    for action, task, elapsed in events:
        o.add_row(rows, {'action': action, 'task': task, 'elapsed_ms': elapsed})
    return rows


class TestLifecycle(unittest.TestCase):
    def test_real_canonical_full_four_stage_timeline(self):
        events = [('start','FIGHT',0),('hint','FIGHT',100),('blocked','FIGHT',200),('complete','FIGHT',900),
                  ('start','GARAGE',1000),('complete','GARAGE',1200),
                  ('start','REPAIR',2000),('complete','REPAIR',4000),
                  ('start','CHECKPOINT',4300),('complete','CHECKPOINT',4500),('end',None,4800)]
        stats = o.validate_events(rows_for(*events))
        self.assertEqual(stats['completed'], 4)
        self.assertEqual(stats['tasks']['FIGHT']['duration_ms'], 900)
        self.assertEqual(stats['tasks']['FIGHT']['hints'], 1)
        self.assertEqual(stats['tasks']['FIGHT']['blocks'], 1)
        self.assertTrue(stats['ended'])

    def test_bail_out_still_reports_missing_stages_not_pass(self):
        stats = o.validate_events(rows_for(('start','FIGHT',5),('abandon','FIGHT',300),('end',None,400)))
        self.assertEqual(stats['completed'], 0)
        self.assertEqual(stats['tasks']['GARAGE']['status'], 'NOT_OBSERVED')
        self.assertEqual(stats['tasks']['FIGHT']['status'], 'ABANDONED')

    def test_interrupt_active_stage(self):
        stats = o.validate_events(rows_for(('start','FIGHT',0),('end',None,200)))
        self.assertEqual(stats['tasks']['FIGHT']['status'], 'INTERRUPTED')
        self.assertEqual(stats['tasks']['FIGHT']['duration_ms'], 200)

    def test_order_enforced(self):
        with self.assertRaisesRegex(o.Invalid, 'TASK_ORDER'):
            rows_for(('start','GARAGE',0))

    def test_finish_without_start_fails(self):
        with self.assertRaisesRegex(o.Invalid, 'TASK_NOT_ACTIVE'):
            rows_for(('complete','FIGHT',0))

    def test_no_parallel_start(self):
        with self.assertRaisesRegex(o.Invalid, 'TASK_ORDER'):
            rows_for(('start','FIGHT',0),('start','GARAGE',10))

    def test_no_repeated_task_start(self):
        with self.assertRaisesRegex(o.Invalid, 'TASK_ORDER'):
            rows_for(('start','FIGHT',0),('complete','FIGHT',9),('start','FIGHT',10))

    def test_no_event_after_end(self):
        with self.assertRaisesRegex(o.Invalid, 'EVENT_AFTER_END'):
            rows_for(('end',None,0),('start','FIGHT',1))

    def test_monotonic_elapsed_time_must_not_decrease(self):
        with self.assertRaisesRegex(o.Invalid, 'EVENT_TIME'):
            rows_for(('start','FIGHT',50),('complete','FIGHT',49))

    def test_bool_not_a_valid_millisecond(self):
        with self.assertRaisesRegex(o.Invalid, 'EVENT_TIME'):
            rows_for(('end',None,True))

    def test_bad_sha_rejected(self):
        rows = rows_for(('start','FIGHT',0),('end',None,3))
        rows[1]['sha256'] = 'a' * 64
        with self.assertRaisesRegex(o.Invalid, 'HASH_CHAIN'):
            o.validate_events(rows)

    def test_rehashed_out_of_order_sequence_rejected(self):
        rows = rows_for(('start','FIGHT',0))
        rows[1]['seq'] = 5
        with self.assertRaisesRegex(o.Invalid, 'SEQUENCE'):
            o.validate_events(rows)

    def test_extra_payload_key_fail_closed(self):
        rows = rows_for(('start','FIGHT',0))
        rows[1]['payload']['free_text_notes'] = 'personal information'
        rows[1]['sha256'] = o.sha(o.canonical({k:rows[1][k] for k in ('seq','prev','payload')}))
        with self.assertRaisesRegex(o.Invalid, 'EVENT_SCHEMA'):
            o.validate_events(rows)

    def test_human_gate_must_have_both_references(self):
        rows = rows_for(mode='human')
        self.assertEqual(o.validate_events(rows)['completed'], 0)
        rows[0]['payload']['consent_ref'] = None
        rows[0]['sha256'] = o.sha(o.canonical({k:rows[0][k] for k in ('seq','prev','payload')}))
        with self.assertRaisesRegex(o.Invalid, 'HUMAN_GATE'):
            o.validate_events(rows)

    def test_nonhuman_gate_values_rejected(self):
        rows = rows_for()
        rows[0]['payload']['approval_ref'] = 'HQ-FAKE'
        rows[0]['sha256'] = o.sha(o.canonical({k:rows[0][k] for k in ('seq','prev','payload')}))
        with self.assertRaisesRegex(o.Invalid, 'NON_HUMAN_GATE'):
            o.validate_events(rows)

    def test_unknown_task_rejected(self):
        with self.assertRaisesRegex(o.Invalid, 'UNKNOWN_TASK'):
            rows_for(('hint','WEAPON',0))


class TestPhysical(unittest.TestCase):
    def setUp(self):
        self.work = tempfile.TemporaryDirectory()
        self.addCleanup(self.work.cleanup)
        self.root = Path(self.work.name)
        self.binary = self.root/'player.exe'
        self.binary.write_bytes(PLAYER)
        self.private = self.root/'private'

    def args(self, **kw):
        args = dict(root=str(self.private), session='testing_01', player_file=str(self.binary),
                    source_sha=SHA, mode='dry-run', approval_ref=None, consent_ref=None)
        args.update(kw)
        return types.SimpleNamespace(**args)

    def test_physical_session_report_and_binary_sha(self):
        commands = 'start FIGHT\nhint FIGHT\ncomplete FIGHT\nstart GARAGE\nabandon GARAGE\nend\n'
        fake_ticks = iter([0,100_000_000,200_000_000,300_000_000,400_000_000,500_000_000,600_000_000])
        report = o.capture(self.args(), io.StringIO(commands), io.StringIO(), clock=lambda: next(fake_ticks))
        self.assertTrue(report['ended'])
        self.assertEqual(report['mode'], 'dry-run')
        self.assertEqual(report['player_sha256'], o.sha(PLAYER))
        self.assertEqual(report['completed'], 1)
        self.assertFalse(report['gameplay_automatically_verified'])
        self.assertEqual(report['tasks']['FIGHT']['duration_ms'], 200)
        self.assertEqual(report['tasks']['GARAGE']['status'], 'ABANDONED')
        self.assertGreater(self.private.joinpath('testing_01.jsonl').stat().st_size, 250)

    def test_persisted_log_rejects_one_byte_tamper(self):
        o.capture(self.args(), io.StringIO('end\n'), io.StringIO(), clock=iter([0,1000000]).__next__)
        log = self.private/'testing_01.jsonl'
        raw = log.read_bytes().replace(b'"dry-run"', b'"unknown"')
        log.write_bytes(raw)
        with self.assertRaisesRegex(o.Invalid, 'HASH_CHAIN'):
            o.inspect(log)

    def test_log_rejects_partial_final_line(self):
        o.capture(self.args(), io.StringIO('end\n'), io.StringIO(), clock=iter([0,1000000]).__next__)
        log = self.private/'testing_01.jsonl'
        log.write_bytes(log.read_bytes()[:-1])
        with self.assertRaisesRegex(o.Invalid, 'TRUNCATED_LOG'):
            o.read_rows(log)

    def test_log_rejects_oversized_file(self):
        log = self.private/'large.jsonl'
        self.private.mkdir()
        log.write_bytes(b'x'*(o.MAX_BYTES+1))
        with self.assertRaisesRegex(o.Invalid, 'LOG_PATH_OR_SIZE'):
            o.read_rows(log)

    def test_log_rejects_symlink(self):
        o.capture(self.args(), io.StringIO('end\n'), io.StringIO(), clock=iter([0,1000000]).__next__)
        target = self.private/'testing_01.jsonl'
        alias = self.private/'alias.jsonl'
        try:
            alias.symlink_to(target)
        except (OSError, NotImplementedError):
            self.skipTest('Filesystem symlink unavailable: not a security PASS')
        with self.assertRaisesRegex(o.Invalid, 'LOG_PATH_OR_SIZE'):
            o.read_rows(alias)

    def test_private_root_rejects_git_without_creating_output(self):
        repo = self.root/'repo';repo.mkdir();(repo/'.git').write_text('gitdir: elsewhere')
        with self.assertRaisesRegex(o.Invalid, 'GIT_WORKTREE_DENIED'):
            o.capture(self.args(root=str(repo/'unsafe')),io.StringIO('end\n'),io.StringIO())
        self.assertFalse((repo/'unsafe').exists())

    def test_private_root_rejects_symlink_before_writing(self):
        alias = self.root/'alias'
        try:
            alias.symlink_to(self.root, target_is_directory=True)
        except (OSError, NotImplementedError):
            self.skipTest('Filesystem symlink unavailable: not a security PASS')
        with self.assertRaisesRegex(o.Invalid, 'SYMLINK|PATH_REDIRECTION'):
            o.capture(self.args(root=str(alias/'data')),io.StringIO('end\n'),io.StringIO())
        self.assertFalse((self.root/'data').exists())

    def test_session_id_traversal_refused(self):
        with self.assertRaisesRegex(o.Invalid, 'INVALID_SESSION_ID'):
            o.capture(self.args(session='../escape'),io.StringIO('end\n'),io.StringIO())
        self.assertFalse(self.private.exists())

    def test_player_is_hashed_before_log_created(self):
        with self.assertRaisesRegex(o.Invalid, 'PLAYER_FILE'):
            o.capture(self.args(player_file=str(self.root/'missing.exe')),io.StringIO('end\n'),io.StringIO())
        self.assertFalse(self.private.exists())

    def test_exclusive_create_refuses_overwrite(self):
        o.capture(self.args(),io.StringIO('end\n'),io.StringIO(),clock=iter([0,1000000]).__next__)
        before = (self.private/'testing_01.jsonl').read_bytes()
        with self.assertRaises(FileExistsError):
            o.capture(self.args(),io.StringIO('end\n'),io.StringIO())
        self.assertEqual((self.private/'testing_01.jsonl').read_bytes(),before)

    def test_missing_end_keeps_partial_and_fails(self):
        with self.assertRaisesRegex(o.Invalid, 'MISSING_EXPLICIT_END'):
            o.capture(self.args(),io.StringIO('start FIGHT\n'),io.StringIO(),clock=iter([0,1000000]).__next__)
        self.assertFalse(o.inspect(self.private/'testing_01.jsonl')['ended'])

    def test_invalid_command_does_not_create_log_entry(self):
        buf=io.StringIO()
        res=o.capture(self.args(),io.StringIO('badgarbage\nstart REPAIR\nend\n'),buf,clock=iter([0,1000000,2000000]).__next__)
        self.assertIn('REJECT invalid command',buf.getvalue())
        self.assertIn('REJECT TASK_ORDER_INVALID',buf.getvalue())
        self.assertEqual(res['recorded_events'],1)

    def test_human_capture_without_gate_refs_rejected(self):
        with self.assertRaisesRegex(o.Invalid, 'HUMAN_REQUIRES'):
            o.capture(self.args(mode='human'),io.StringIO('end\n'),io.StringIO())
        self.assertFalse(self.private.exists())

    def test_binary_pin_detectably_differs_after_replacement(self):
        o.capture(self.args(),io.StringIO('end\n'),io.StringIO(),clock=iter([0,1000000]).__next__)
        pinned=o.inspect(self.private/'testing_01.jsonl')['player_sha256']
        self.binary.write_bytes(PLAYER+b'changed')
        self.assertNotEqual(pinned,o.file_sha(self.binary))

    @unittest.skipIf(os.name=='nt','Native NTFS ACL/junction coverage NOT_RUN on Windows')
    def test_posix_private_permissions(self):
        o.capture(self.args(),io.StringIO('end\n'),io.StringIO(),clock=iter([0,1000000]).__next__)
        self.assertEqual(self.private.stat().st_mode & 0o777,0o700)
        self.assertEqual((self.private/'testing_01.jsonl').stat().st_mode & 0o777,0o600)


if __name__ == '__main__':
    unittest.main(verbosity=2)
