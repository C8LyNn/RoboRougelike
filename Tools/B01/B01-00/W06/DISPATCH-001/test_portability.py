"""W08 RED->GREEN: fail-closed portable ownership names on case-insensitive filesystems."""
import copy
from pathlib import Path
import tempfile
import unittest

from dispatch import (DispatchError, check_scopes, claim, clean_scope,
                      decision, parse_snapshot, plan, scopes_overlap, validate_claims)
from test_dispatch import fixture


class WindowsPortableScopeTests(unittest.TestCase):
    def test_reserved_con_device(self):
        with self.assertRaises(DispatchError): clean_scope('Tools/CON')

    def test_reserved_nul_device_with_extension(self):
        with self.assertRaises(DispatchError): clean_scope('Tools/NUL.txt')

    def test_reserved_com_device(self):
        with self.assertRaises(DispatchError): clean_scope('Tools/COM1')

    def test_reserved_lpt_device_with_extension(self):
        with self.assertRaises(DispatchError): clean_scope('Tools/lPt9.LOG')

    def test_trailing_dot_rejected(self):
        with self.assertRaises(DispatchError): clean_scope('Assets/Part.')

    def test_trailing_space_rejected(self):
        with self.assertRaises(DispatchError): clean_scope('Assets/Part ')

    def test_colon_rejected(self):
        with self.assertRaises(DispatchError): clean_scope('Assets/Scripts:evil')

    def test_angle_brackets_rejected(self):
        with self.assertRaises(DispatchError): clean_scope('Assets/<Bad>')

    def test_pipe_rejected(self):
        with self.assertRaises(DispatchError): clean_scope('Assets/File|alt')

    def test_quote_rejected(self):
        with self.assertRaises(DispatchError): clean_scope('Assets/"File"')

    def test_control_character_rejected(self):
        with self.assertRaises(DispatchError): clean_scope('Assets/bad\x1f')

    def test_unicode_nfkc_ambiguity_rejected(self):
        with self.assertRaises(DispatchError): clean_scope('Assets/\u212aelvin')

    def test_repeated_casefold_scope_rejected(self):
        with self.assertRaises(DispatchError): check_scopes(['Assets/Parts', 'assets/parts'])

    def test_case_insensitive_nested_overlap(self):
        self.assertTrue(scopes_overlap(['Assets/SCRIPTS'], ['assets/scripts/camera']))

    def test_case_insensitive_boundary_not_false_overlap(self):
        self.assertFalse(scopes_overlap(['Assets/Scripts'], ['assets/scripts2']))

    def test_lowercase_codex_reservation_cannot_bypass(self):
        data = fixture()
        data['tasks'][2]['scope'] = ['assets/scripts/camera']
        self.assertIn('codex_reserved_scope', decision(data, [], 'CAMERA')['blockers'])

    def test_mixed_case_claim_scope_blocks(self):
        data = fixture()
        claimed = {'id':'LOCKED','package_id':'OtherPkg','owner':'B01-99',
                   'scope':['tools/b01/b01-00/W06'], 'state':'FROZEN','stream':None}
        self.assertIn('overlap_with_LOCKED', decision(data, [claimed], 'W06-DISPATCH')['blockers'])

    def test_mixed_case_scope_plan_never_coassigns(self):
        data=fixture()
        data['tasks'][4]['scope']=['research/b01-20/chl/deep']
        rows=plan(data,[])['decisions']
        candidate=next(x for x in rows if x['task_id']=='CHL-OVERLAP')
        self.assertIn('overlap_with_CHL-WORK',candidate['blockers'])

    def test_mixed_case_scope_actual_claim_cannot_double_reserve(self):
        data=fixture()
        data['tasks'][4]['scope']=['research/b01-20/chl/deep']
        with tempfile.TemporaryDirectory() as d:
            claim(data,d,'CHL-WORK')
            with self.assertRaises(DispatchError): claim(data,d,'CHL-OVERLAP')


class PortableIdAndStreamTests(unittest.TestCase):
    def test_casefold_duplicate_task_id_rejected(self):
        data=fixture(); copy_task=copy.deepcopy(data['tasks'][0])
        copy_task['id']='w06-dispatch'; copy_task['package_id']='NEWPKG'
        data['tasks'].append(copy_task)
        with self.assertRaises(DispatchError): parse_snapshot(data)

    def test_casefold_duplicate_package_id_rejected(self):
        data=fixture(); data['tasks'][1]['package_id']='dispatch001'
        with self.assertRaises(DispatchError): parse_snapshot(data)

    def test_casefold_duplicate_active_stream_rejected(self):
        data=fixture(); data['active_streams'].append('chl')
        with self.assertRaises(DispatchError): parse_snapshot(data)

    def test_casefold_duplicate_approved_stream_rejected(self):
        data=fixture(); data['approved_streams'].append('partsize')
        with self.assertRaises(DispatchError): parse_snapshot(data)

    def test_casefold_active_stream_matches_approved(self):
        data=fixture(); data['active_streams']=['partsize','chl']
        self.assertTrue(decision(data,[],'CHL-WORK')['eligible'])

    def test_casefold_new_stream_cannot_fake_new_capacity(self):
        data=fixture(); data['tasks'][1]['stream']='chl'
        self.assertTrue(decision(data,[],'CHL-WORK')['eligible'])

    def test_casefold_duplicate_claim_id_rejected(self):
        data=fixture()
        claim_a={'id':'LockA','package_id':'PackageA','owner':'B01-99',
                 'scope':['External/One'],'state':'ACTIVE','stream':None}
        claim_b={'id':'locka','package_id':'PackageB','owner':'B01-98',
                 'scope':['External/Two'],'state':'ACTIVE','stream':None}
        with self.assertRaises(DispatchError): validate_claims([claim_a,claim_b])

    def test_casefold_duplicate_claim_package_rejected(self):
        claim_a={'id':'LockA','package_id':'PackageA','owner':'B01-99',
                 'scope':['External/One'],'state':'ACTIVE','stream':None}
        claim_b={'id':'LockB','package_id':'packagea','owner':'B01-98',
                 'scope':['External/Two'],'state':'ACTIVE','stream':None}
        with self.assertRaises(DispatchError): validate_claims([claim_a,claim_b])

    def test_casefold_same_id_already_claimed(self):
        data=fixture()
        claimed={'id':'w06-dispatch','package_id':'OTHER_PKG','owner':'B01-99',
                 'scope':['External/Noncolliding'],'state':'ACTIVE','stream':None}
        self.assertIn('already_claimed', decision(data,[claimed],'W06-DISPATCH')['blockers'])

    def test_casefold_same_package_already_claimed(self):
        data=fixture()
        claimed={'id':'OTHER_LOCK','package_id':'dispatch001','owner':'B01-99',
                 'scope':['External/Noncolliding'],'state':'ACTIVE','stream':None}
        self.assertIn('already_claimed', decision(data,[claimed],'W06-DISPATCH')['blockers'])


if __name__ == '__main__':
    unittest.main()
