#!/usr/bin/env python3
"""Adversarial tests of the V2 evidence gate; NO Unity execution involved."""
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
import qa_gate_v2 as gate

S='a'*40
T='b'*40

def map_plan():
    cubes=[f'{r}_Seed{s}_{t}_CubePhysics' for r in ('Arena','Course') for s in (7,31,83) for t in ('Light','Heavy')]
    cases=[f'R6-{r}-{s}-{t}' for r in ('Arena','Course') for s in (7,31,83) for t in ('Light','Heavy')]
    return dict(schema='b01-60-native-plan-v1',profile='MAPGEN_R6_FULL',source_sha=S,target_sha=T,
                source_files={'Assets/B01Packages/Pinned.cs':'c'*64},test_filter='B01.MAPGEN001.Tests',
                required_stages=list(gate.STAGES),required_nunit={'EditMode':['EditSmoke'],'PlayMode':cubes},
                screenshots={'Player720':['Arena720.png'],'Player1080':['Arena1080.png']},
                required_functional={'Player720':cases[:4],'Player1080':cases[4:]})

def camera_plan():
    p=map_plan();p['profile']='CAMERA_R12_FULL'
    p['required_functional']={'Player720':['CAM-LIGHT'],'Player1080':['CAM-HEAVY']}
    p['required_nunit']['PlayMode']=['CAM-PITCH','CAM-WALL']
    return p

def good_camera():
    return dict(id='CAM-LIGHT',status='PASS',actualOwnRayHits=0,worldPenetrationCount=0,
                maxContinuousSpeedMps=12.5,maxAimDeltaDeg=.02)

def good_r6():
    return dict(id='R6-Arena-7-Light',status='PASS',wheelContactSamples=120,wheelQueryCount=121,
                zeroContactSamples=1,stallCount=0,fallCount=0,flipCount=0,
                reachedCheckpoints=4,distanceMeters=54.3,finishReached=True)

class QAPlanV2(unittest.TestCase):
    def test_valid_mapgen_all_cases(self):self.assertEqual(gate.validate_plan(map_plan()),[])
    def test_valid_camera(self):self.assertEqual(gate.validate_plan(camera_plan()),[])
    def test_empty_stages(self):
        p=map_plan();p['required_stages']=[];self.assertIn('INVALID_REQUIRED_STAGES',gate.validate_plan(p))
    def test_unknown_stage(self):
        p=map_plan();p['required_stages']=['Compile','Fake'];self.assertIn('INVALID_REQUIRED_STAGES',gate.validate_plan(p))
    def test_compile_stage_mandatory(self):
        p=map_plan();p['required_stages']=['EditMode'];self.assertIn('COMPILE_STAGE_MANDATORY',gate.validate_plan(p))
    def test_full_profile_cannot_omit_player(self):
        p=map_plan();p['required_stages']=['Compile','EditMode','PlayMode'];self.assertIn('FULL_PROFILE_REQUIRES_ALL_STAGES',gate.validate_plan(p))
    def test_missing_source_digest(self):
        p=map_plan();p['source_files']={};self.assertIn('MISSING_SOURCE_SHA256_PINS',gate.validate_plan(p))
    def test_bad_digest(self):
        p=map_plan();p['source_files']={'ok.cs':'notasha'};self.assertIn('INVALID_SOURCE_DIGEST',gate.validate_plan(p))
    def test_escaping_source_path(self):
        p=map_plan();p['source_files']={'../outside.cs':'c'*64};self.assertIn('INVALID_SOURCE_PATH',gate.validate_plan(p))
    def test_bad_sha_unchanged_length(self):
        p=map_plan();p['source_sha']='Z'*40;self.assertIn('INVALID_SOURCE_SHA',gate.validate_plan(p))
    def test_missing_editmode_pins(self):
        p=map_plan();p['required_nunit']['EditMode']=[];self.assertIn('MISSING_NUNIT_CASE_PINS:EditMode',gate.validate_plan(p))
    def test_duplicate_test_names(self):
        p=map_plan();p['required_nunit']['PlayMode'].append(p['required_nunit']['PlayMode'][0]);self.assertIn('MISSING_NUNIT_CASE_PINS:PlayMode',gate.validate_plan(p))
    def test_missing_one_cube(self):
        p=map_plan();p['required_nunit']['PlayMode'].pop();self.assertIn('MISSING_MAPGEN_12_CUBE_CASES',gate.validate_plan(p))
    def test_missing_one_r6(self):
        p=map_plan();p['required_functional']['Player1080'].pop();self.assertIn('MISSING_R6_12_CASES',gate.validate_plan(p))
    def test_missing_player_screenshot(self):
        p=map_plan();p['screenshots']['Player720']=[];self.assertIn('INVALID_SCREENSHOT_PINS:Player720',gate.validate_plan(p))
    def test_empty_profile_rejected(self):
        p=map_plan();p.pop('profile');self.assertIn('INVALID_QA_PROFILE',gate.validate_plan(p))
    def test_empty_nunit_filter(self):
        p=map_plan();p['test_filter']='';self.assertIn('MISSING_TEST_FILTER',gate.validate_plan(p))
    def test_bogus_stages_integer_fails_closed(self):
        p=map_plan();p['required_stages']=7;self.assertIn('INVALID_REQUIRED_STAGES',gate.validate_plan(p))

class QATelemetryV2(unittest.TestCase):
    def test_camera_valid(self):self.assertEqual(gate._functional(good_camera()),[])
    def test_camera_nan_speed_rejected(self):
        p=good_camera();p['maxContinuousSpeedMps']=float('nan');self.assertIn('CAM_SPEED_NONFINITE_OR_EXCESSIVE',gate._functional(p))
    def test_camera_negative_speed_rejected(self):
        p=good_camera();p['maxContinuousSpeedMps']=-1;self.assertIn('CAM_SPEED_NONFINITE_OR_EXCESSIVE',gate._functional(p))
    def test_camera_nan_aim_rejected(self):
        p=good_camera();p['maxAimDeltaDeg']=float('nan');self.assertIn('CAM_AIM_ERROR_NONFINITE_OR_EXCESSIVE',gate._functional(p))
    def test_camera_missing_aim_rejected(self):
        p=good_camera();p.pop('maxAimDeltaDeg');self.assertIn('CAM_AIM_ERROR_NONFINITE_OR_EXCESSIVE',gate._functional(p))
    def test_camera_bool_fake_count_rejected(self):
        p=good_camera();p['actualOwnRayHits']=False;self.assertIn('CAM_ZERO_REQUIRED:actualOwnRayHits',gate._functional(p))
    def test_camera_los_hit_rejected(self):
        p=good_camera();p['actualOwnRayHits']=1;self.assertIn('CAM_ZERO_REQUIRED:actualOwnRayHits',gate._functional(p))
    def test_r6_valid(self):self.assertEqual(gate._functional(good_r6()),[])
    def test_r6_no_distance_rejected(self):
        p=good_r6();p['distanceMeters']=0;self.assertIn('R6_MISSING_OR_INVALID_DISTANCE',gate._functional(p))
    def test_r6_no_grounding_rejected(self):
        p=good_r6();p['wheelContactSamples']=0;self.assertIn('R6_REQUIRED_POSITIVE:wheelContactSamples',gate._functional(p))
    def test_r6_no_checkpoint_rejected(self):
        p=good_r6();p['reachedCheckpoints']=0;self.assertIn('R6_REQUIRED_POSITIVE:reachedCheckpoints',gate._functional(p))
    def test_r6_stall_rejected(self):
        p=good_r6();p['stallCount']=1;self.assertIn('R6_BAD_EVENT:stallCount',gate._functional(p))
    def test_r6_contacts_larger_than_queries(self):
        p=good_r6();p['wheelContactSamples']=122;self.assertIn('R6_CONTACTS_EXCEED_QUERIES',gate._functional(p))
    def test_r6_unmeasured_zero_contacts_rejected(self):
        p=good_r6();p.pop('zeroContactSamples');self.assertIn('R6_REQUIRED_COUNTER:zeroContactSamples',gate._functional(p))

class QAReceiptV2(unittest.TestCase):
    def setUp(self):
        self.t=tempfile.TemporaryDirectory()
        self.root=Path(self.t.name)
        self.plan=camera_plan()
        self.plan['required_stages']=['Compile']
        self.plan['profile']='SCOPED_DIAGNOSTIC'
    def tearDown(self):self.t.cleanup()
    def _legacy(self,plan,root):
        return {'status':'PASS','stages':[dict(stage=st,status='PASS' if (self.root/st/'process.json').exists() else 'NOT_RUN',problems=[]) for st in gate.STAGES]}
    def receipt(self,stage='Compile',extra=None):
        d=self.root/stage;d.mkdir(exist_ok=True)
        p=dict(stage=stage,attribution='B01-60_CONTROLLED_RUNNER',started_utc='2026-10-26T00:00:00+00:00',
               ended_utc='2026-10-26T00:01:00+00:00',source_sha=S,target_sha=T,unity_version='6000.3.14f1',exit_code=0)
        p.update(extra or {});(d/'process.json').write_text(json.dumps(p))
        return d
    def result(self):
        with patch.object(gate.qa_native,'check',side_effect=self._legacy,create=True),patch.object(gate.qa_native,'js',side_effect=lambda p:json.loads(Path(p).read_text()),create=True):
            return gate.verify(self.plan,self.root)
    def test_missing_native_stage_not_run(self):self.assertEqual(self.result()['status'],'NOT_RUN')
    def test_valid_provenance_structural(self):self.receipt();self.assertEqual(self.result()['status'],'PASS')
    def test_wrong_attribution_rejected(self):
        self.receipt(extra={'attribution':'EXTERNAL_CODEX'});self.assertEqual(self.result()['status'],'FAIL')
    def test_missing_clock_rejected(self):
        self.receipt(extra={'ended_utc':None});self.assertEqual(self.result()['status'],'FAIL')
    def test_reversed_clock_rejected(self):
        self.receipt(extra={'ended_utc':'2026-10-25T23:59:00+00:00'});self.assertEqual(self.result()['status'],'FAIL')
    def test_missing_stage_label_rejected(self):
        self.receipt(extra={'stage':'PlayMode'});self.assertEqual(self.result()['status'],'FAIL')
    def test_invalid_plan_blocks_without_native(self):
        self.plan['required_stages']=[];self.assertEqual(self.result()['status'],'BLOCKED')
    def test_bad_player_telemetry_target_rejected(self):
        self.plan['required_stages']=['Compile','Player720'];self.receipt();d=self.receipt('Player720')
        (d/'telemetry.json').write_text(json.dumps({'target_sha':'c'*40,'cases':[good_camera()]}))
        self.assertEqual(self.result()['status'],'FAIL')
    def test_good_player_telemetry_structural(self):
        self.plan['required_stages']=['Compile','Player720'];self.receipt();d=self.receipt('Player720')
        (d/'telemetry.json').write_text(json.dumps({'target_sha':T,'cases':[good_camera()]}))
        self.assertEqual(self.result()['status'],'PASS')
    def test_nan_player_telemetry_rejected(self):
        self.plan['required_stages']=['Compile','Player720'];self.receipt();d=self.receipt('Player720')
        bad=good_camera();bad['maxContinuousSpeedMps']=float('nan')
        (d/'telemetry.json').write_text(json.dumps({'target_sha':T,'cases':[bad]}))
        self.assertEqual(self.result()['status'],'FAIL')

if __name__=='__main__':unittest.main()