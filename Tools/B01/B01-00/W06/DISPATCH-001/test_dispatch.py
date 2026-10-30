import copy
import hashlib
import json
from pathlib import Path
import tempfile
import threading
import unittest

from dispatch import DispatchError, claim, claims_from_dir, clean_scope, decision, plan, release


def fixture():
    return {'schema_version': 'b01-dispatch-v1', 'stream_limit': 2,
            'active_streams': ['PARTSIZE', 'CHL'], 'approved_streams': ['PARTSIZE', 'CHL'],
            'codex_scope_confirmed': False,
            'codex_reserved': ['Assets/Scripts', 'Assets/Prefabs', 'Tools/B01/B01-40'],
            'tasks': [
                {'id': 'W06-DISPATCH', 'package_id': 'DISPATCH001', 'owner': 'B01-00', 'kind': 'support', 'state': 'NEW', 'scope': ['Tools/B01/B01-00/W06/DISPATCH-001'], 'dependencies': [], 'stream': None, 'priority': 95},
                {'id': 'CHL-WORK', 'package_id': 'CHL003', 'owner': 'B01-20', 'kind': 'production', 'state': 'NEW', 'scope': ['Research/B01-20/CHL'], 'dependencies': [], 'stream': 'CHL', 'priority': 80},
                {'id': 'CAMERA', 'package_id': 'CAMERA001', 'owner': 'B01-20', 'kind': 'production', 'state': 'NEW', 'scope': ['Assets/Scripts/Camera'], 'dependencies': [], 'stream': 'CAMERA', 'priority': 90},
                {'id': 'FIRE', 'package_id': 'FIRE001', 'owner': 'B01-24', 'kind': 'production', 'state': 'NEW', 'scope': ['Research/B01-24/FIRE'], 'dependencies': [], 'stream': 'FIRE', 'priority': 78},
                {'id': 'CHL-OVERLAP', 'package_id': 'CHL004', 'owner': 'B01-21', 'kind': 'support', 'state': 'NEW', 'scope': ['Research/B01-20'], 'dependencies': [], 'stream': None, 'priority': 50},
            ]}


class ScopeTests(unittest.TestCase):
    def test_reject_dotdot(self):
        for path in ['../X', 'a/../b', '/absolute', 'x//y', 'x/', 'x/*', 'x\\y', 'x/.']:
            with self.subTest(path=path), self.assertRaises(DispatchError):
                clean_scope(path)

    def test_segment_boundary(self):
        data = fixture(); data['codex_reserved'] = ['Assets/Scripts']
        data['tasks'][0]['scope'] = ['Assets/Scripts2']
        self.assertTrue(decision(data, [], 'W06-DISPATCH')['eligible'])

    def test_reject_codex_overwrite(self):
        self.assertIn('codex_reserved_scope', decision(fixture(), [], 'CAMERA')['blockers'])

    def test_no_new_unapproved_stream(self):
        d = decision(fixture(), [], 'FIRE')
        self.assertIn('stream_not_approved', d['blockers'])
        self.assertIn('active_stream_limit', d['blockers'])

    def test_approved_existing_stream_allowed(self):
        self.assertTrue(decision(fixture(), [], 'CHL-WORK')['eligible'])

    def test_deterministic_batch_and_overlap(self):
        a = plan(fixture(), [])
        self.assertEqual(a, plan(fixture(), []))
        self.assertEqual(a['selected'], ['W06-DISPATCH', 'CHL-WORK'])
        d = next(d for d in a['decisions'] if d['task_id'] == 'CHL-OVERLAP')
        self.assertIn('overlap_with_CHL-WORK', d['blockers'])

    def test_duplicate_package_fail(self):
        a = fixture(); a['tasks'][1]['package_id'] = 'DISPATCH001'
        with self.assertRaises(DispatchError): plan(a, [])

    def test_dependency_not_done(self):
        a = fixture(); a['tasks'][0]['dependencies'] = ['CHL-WORK']
        self.assertIn('dependency_CHL-WORK_not_done', decision(a, [], 'W06-DISPATCH')['blockers'])

    def test_cyclic_dependency_rejected(self):
        a = fixture(); a['tasks'][0]['dependencies'] = ['CHL-WORK']; a['tasks'][1]['dependencies'] = ['W06-DISPATCH']
        with self.assertRaises(DispatchError): plan(a, [])

    def test_unknown_codex_scope_fail_closed(self):
        a = fixture(); a['codex_reserved'] = []
        with self.assertRaises(DispatchError): plan(a, [])

    def test_duplicate_claim_identity(self):
        a = fixture(); d = {'id': 'CLAIM1', 'package_id': 'PKG01', 'owner': 'B01-99', 'scope': ['Elsewhere'], 'state': 'ACTIVE'}
        with self.assertRaises(DispatchError): plan(a, [d, copy.deepcopy(d)])

    def test_existing_claim_collision_blocks(self):
        a = fixture(); old = {'id': 'CLAIM1', 'package_id': 'PKG01', 'owner': 'B01-99', 'scope': ['Tools/B01/B01-00'], 'state': 'FROZEN'}
        self.assertIn('overlap_with_CLAIM1', decision(a, [old], 'W06-DISPATCH')['blockers'])

    def test_already_done_not_reclaimed(self):
        a = fixture(); a['tasks'][0]['state'] = 'DONE'
        self.assertIn('task_state_done', decision(a, [], 'W06-DISPATCH')['blockers'])

    def test_claimed_production_stream_counts_against_cap(self):
        a = fixture(); a['active_streams'] = ['CHL']; a['approved_streams'] = ['CHL', 'PARTSIZE', 'NEWSTREAM']
        a['tasks'][1]['stream'] = 'NEWSTREAM'
        old = {'id': 'PRIORWORK', 'package_id': 'PRIORPKG', 'owner': 'B01-22',
               'scope': ['Research/B01-22'], 'state': 'ACTIVE', 'stream': 'PARTSIZE'}
        self.assertIn('active_stream_limit', decision(a, [old], 'CHL-WORK')['blockers'])

    def test_planned_new_stream_consumes_single_slot(self):
        a = fixture(); a['active_streams'] = []; a['approved_streams'] = ['CHL', 'PARTSIZE']
        a['stream_limit'] = 1
        a['tasks'][0]['state'] = 'DONE'
        a['tasks'][2]['state'] = 'DONE'; a['tasks'][3]['state'] = 'DONE'
        a['tasks'][4]['kind'] = 'production'; a['tasks'][4]['stream'] = 'PARTSIZE'
        a['tasks'][4]['scope'] = ['Research/B01-21/PARTSIZE']
        decisions = plan(a, [])
        self.assertEqual(decisions['selected'], ['CHL-WORK'])
        blocked = next(d for d in decisions['decisions'] if d['task_id'] == 'CHL-OVERLAP')
        self.assertIn('active_stream_limit', blocked['blockers'])

    def test_active_stream_exceed_cap_blocks_everything(self):
        a = fixture(); a['active_streams'].append('FIRE'); a['approved_streams'].append('FIRE')
        with self.assertRaises(DispatchError): plan(a, [])


class ClaimTests(unittest.TestCase):
    def test_claim_and_hash_locked_release(self):
        with tempfile.TemporaryDirectory() as d:
            receipt = claim(fixture(), d, 'W06-DISPATCH')
            raw = Path(receipt['claim']).read_bytes()
            self.assertEqual(receipt['sha256'], hashlib.sha256(raw).hexdigest())
            self.assertEqual(len(claims_from_dir(d)), 1)
            with self.assertRaises(DispatchError): claim(fixture(), d, 'W06-DISPATCH')
            with self.assertRaises(DispatchError): release(d, 'W06-DISPATCH', '0' * 64)
            self.assertTrue(Path(receipt['claim']).exists())
            self.assertEqual(release(d, 'W06-DISPATCH', receipt['sha256']), {'released': 'W06-DISPATCH'})
            self.assertEqual(claims_from_dir(d), [])

    def test_inter_task_overlapping_reserve_denied(self):
        a = fixture()
        a['tasks'][4]['scope'] = ['Research/B01-20']
        with tempfile.TemporaryDirectory() as d:
            claim(a, d, 'CHL-WORK')
            with self.assertRaises(DispatchError): claim(a, d, 'CHL-OVERLAP')

    def test_atomic_guard_blocks_claim(self):
        with tempfile.TemporaryDirectory() as d:
            (Path(d) / '.dispatch_guard').mkdir()
            with self.assertRaises(DispatchError): claim(fixture(), d, 'W06-DISPATCH')

    def test_symlink_claim_dir_rejected(self):
        with tempfile.TemporaryDirectory() as d:
            src = Path(d) / 'real'; src.mkdir(); link = Path(d) / 'link'
            link.symlink_to(src, target_is_directory=True)
            with self.assertRaises(DispatchError): claim(fixture(), str(link), 'W06-DISPATCH')

    def test_malformed_existing_claim_fail_closed(self):
        with tempfile.TemporaryDirectory() as d:
            (Path(d) / 'bad.json').write_text('{nope}', encoding='utf-8')
            with self.assertRaises(json.JSONDecodeError): claim(fixture(), d, 'W06-DISPATCH')

    def test_concurrent_claim_only_one_wins(self):
        with tempfile.TemporaryDirectory() as d:
            outcomes = []
            def run():
                try: claim(fixture(), d, 'W06-DISPATCH'); outcomes.append('OK')
                except DispatchError: outcomes.append('BLOCKED')
            a = threading.Thread(target=run); b = threading.Thread(target=run)
            a.start(); b.start(); a.join(); b.join()
            self.assertEqual(sorted(outcomes), ['BLOCKED', 'OK'])
            self.assertEqual(len(claims_from_dir(d)), 1)


if __name__ == '__main__':
    unittest.main()
