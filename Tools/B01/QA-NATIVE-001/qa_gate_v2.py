#!/usr/bin/env python3
"""B01-60 V2 fail-closed plan + receipt gate (wraps existing qa_native.py).

This is a verifier for evidence structure, not an attestation of native execution.
"""
import argparse
import datetime as dt
import json
import math
import re
from pathlib import Path, PurePosixPath
import qa_native

SHA = re.compile(r"^[0-9a-f]{40}$")
HASH = re.compile(r"^[0-9a-f]{64}$")
STAGES = tuple(qa_native.STAGES)

def _names(value):
    return (isinstance(value, list) and bool(value) and
            all(isinstance(x, str) and bool(x.strip()) and x == x.strip() for x in value) and
            len(set(value)) == len(value))

def validate_plan(p):
    errors = []
    if not isinstance(p, dict) or p.get('schema') != 'b01-60-native-plan-v1':
        return ['INVALID_PLAN_SCHEMA']
    for key in ('source_sha', 'target_sha'):
        if not SHA.fullmatch(str(p.get(key, ''))):
            errors.append('INVALID_' + key.upper())
    files = p.get('source_files')
    if not isinstance(files, dict) or not files:
        errors.append('MISSING_SOURCE_SHA256_PINS')
    else:
        for path, digest in files.items():
            if not isinstance(path, str) or not path or '\\' in path or path.startswith('/') or ':' in path:
                errors.append('INVALID_SOURCE_PATH'); continue
            components = PurePosixPath(path).parts
            if any(segment in ('', '.', '..') for segment in path.split('/')) or not components:
                errors.append('INVALID_SOURCE_PATH')
            if not HASH.fullmatch(str(digest)):
                errors.append('INVALID_SOURCE_DIGEST')
    stages = p.get('required_stages')
    if not _names(stages) or any(s not in STAGES for s in stages or []):
        errors.append('INVALID_REQUIRED_STAGES')
        stages = []
    if 'Compile' not in stages:
        errors.append('COMPILE_STAGE_MANDATORY')
    if any(s in stages for s in ('EditMode', 'PlayMode')):
        if not isinstance(p.get('test_filter'), str) or not p['test_filter'].strip():
            errors.append('MISSING_TEST_FILTER')
    defined = p.get('required_nunit', {})
    if not isinstance(defined, dict) or any(key not in ('EditMode', 'PlayMode') for key in defined):
        errors.append('INVALID_NUNIT_CONFIG')
        defined = {}
    for name in ('EditMode', 'PlayMode'):
        if name in stages and not _names(defined.get(name)):
            errors.append('MISSING_NUNIT_CASE_PINS:' + name)
    screenshots = p.get('screenshots', {})
    functional = p.get('required_functional', {})
    if not isinstance(screenshots, dict) or not isinstance(functional, dict):
        errors.append('INVALID_PLAYER_CONFIG')
        screenshots, functional = {}, {}
    for stage in ('Player720', 'Player1080'):
        if stage in stages:
            sn = screenshots.get(stage)
            fn = functional.get(stage)
            if not _names(sn) or any('/' in n or '\\' in n or '..' in n or not n.endswith('.png') for n in sn or []):
                errors.append('INVALID_SCREENSHOT_PINS:' + stage)
            if not _names(fn):
                errors.append('MISSING_FUNCTIONAL_CASE_PINS:' + stage)
    profile = p.get('profile')
    if profile not in ('MAPGEN_R6_FULL', 'CAMERA_R12_FULL', 'SCOPED_DIAGNOSTIC'):
        errors.append('INVALID_QA_PROFILE')
    if profile in ('MAPGEN_R6_FULL', 'CAMERA_R12_FULL') and set(stages) != set(STAGES):
        errors.append('FULL_PROFILE_REQUIRES_ALL_STAGES')
    if profile == 'MAPGEN_R6_FULL':
        required = {
            f'{recipe}_Seed{seed}_{rig}_CubePhysics'
            for recipe in ('Arena', 'Course') for seed in (7,31,83) for rig in ('Light','Heavy')}
        if not required.issubset(set(defined.get('PlayMode', []) or [])):
            errors.append('MISSING_MAPGEN_12_CUBE_CASES')
        seen = set(functional.get('Player720', []) or []) | set(functional.get('Player1080', []) or [])
        matrix = {f'R6-{recipe}-{seed}-{rig}' for recipe in ('Arena','Course') for seed in (7,31,83) for rig in ('Light','Heavy')}
        if not matrix.issubset(seen):
            errors.append('MISSING_R6_12_CASES')
    return sorted(set(errors))

def _time(value):
    try:
        if not isinstance(value, str) or not value:
            return None
        v = dt.datetime.fromisoformat(value.replace('Z', '+00:00'))
        return v if v.tzinfo else None
    except ValueError:
        return None

def _integer(value, minimum=0):
    return type(value) is int and value >= minimum

def _real(value, minimum=0, maximum=None):
    return (type(value) in (float, int) and math.isfinite(value) and value >= minimum
            and (maximum is None or value <= maximum))

def _functional(case):
    cid = case.get('id','')
    failures = []
    if cid.startswith('CAM-'):
        for field in ('actualOwnRayHits', 'worldPenetrationCount'):
            if not _integer(case.get(field)) or case[field] != 0:
                failures.append('CAM_ZERO_REQUIRED:' + field)
        if not _real(case.get('maxContinuousSpeedMps'), 0, 15):
            failures.append('CAM_SPEED_NONFINITE_OR_EXCESSIVE')
        if not _real(case.get('maxAimDeltaDeg'), 0, .05):
            failures.append('CAM_AIM_ERROR_NONFINITE_OR_EXCESSIVE')
    if cid.startswith('R6-'):
        for field in ('wheelContactSamples','wheelQueryCount','reachedCheckpoints'):
            if not _integer(case.get(field), 1):
                failures.append('R6_REQUIRED_POSITIVE:' + field)
        for field in ('zeroContactSamples','stallCount','fallCount','flipCount'):
            if not _integer(case.get(field)):
                failures.append('R6_REQUIRED_COUNTER:' + field)
        for field in ('stallCount','fallCount','flipCount'):
            if _integer(case.get(field)) and case[field] != 0:
                failures.append('R6_BAD_EVENT:' + field)
        if not _real(case.get('distanceMeters'), 0.01):
            failures.append('R6_MISSING_OR_INVALID_DISTANCE')
        if case.get('finishReached') is not True:
            failures.append('R6_NO_FINISH')
        if (_integer(case.get('wheelContactSamples'),1) and _integer(case.get('wheelQueryCount'),1)
                and case['wheelContactSamples'] > case['wheelQueryCount']):
            failures.append('R6_CONTACTS_EXCEED_QUERIES')
        if (_integer(case.get('zeroContactSamples')) and _integer(case.get('wheelQueryCount'),1)
                and case['zeroContactSamples'] > case['wheelQueryCount']):
            failures.append('R6_ZERO_CONTACTS_EXCEED_QUERIES')
    return failures

def verify(plan, root):
    bad = validate_plan(plan)
    if bad:
        return {'schema':'b01-60-qa-verdict-v2','attribution':'EVIDENCE_STRUCTURE_ONLY',
                'status':'BLOCKED','problems':bad,'stages':[],'g2':'HOLD', 'manual':'NOT_RUN'}
    out = qa_native.check(plan,root)
    out['schema'] = 'b01-60-qa-verdict-v2'
    out['attribution'] = 'EVIDENCE_STRUCTURE_ONLY_NOT_NATIVE_WITNESS'
    for row in out['stages']:
        name = row['stage']
        proc = Path(root)/name/'process.json'
        if not proc.is_file():
            continue
        errors = []
        try:
            p = qa_native.js(proc)
            if p.get('stage') != name or p.get('attribution') != 'B01-60_CONTROLLED_RUNNER':
                errors.append('INVALID_RUNNER_PROVENANCE')
            start, end = _time(p.get('started_utc')), _time(p.get('ended_utc'))
            if start is None or end is None or end < start:
                errors.append('INVALID_RUN_TIMESTAMPS')
            if name.startswith('Player'):
                tpath=Path(root)/name/'telemetry.json'
                if tpath.is_file():
                    telemetry=qa_native.js(tpath)
                    if telemetry.get('target_sha') != plan['target_sha']:
                        errors.append('TELEMETRY_TARGET_SHA_MISMATCH')
                    data=telemetry.get('cases')
                    if not isinstance(data,list):
                        errors.append('INVALID_TELEMETRY_CASES')
                    else:
                        for case_id in plan['required_functional'][name]:
                            match=next((x for x in data if isinstance(x,dict) and x.get('id')==case_id), None)
                            if match is not None:
                                errors.extend(_functional(match))
        except (OSError,ValueError,KeyError,TypeError) as exc:
            errors.append('V2_EVIDENCE_PARSE_ERROR:' + type(exc).__name__)
        if errors:
            row['problems'] = sorted(set(row.get('problems', []) + errors))
            row['status'] = 'FAIL'
    required = [x for x in out['stages'] if x['stage'] in plan['required_stages']]
    out['status'] = ('FAIL' if any(x['status']=='FAIL' for x in required) else
                     'BLOCKED' if any(x['status']=='BLOCKED' for x in required) else
                     'NOT_RUN' if any(x['status']=='NOT_RUN' for x in required) else 'PASS')
    return out

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument('--plan',required=True);ap.add_argument('--evidence',required=True);ap.add_argument('--out')
    args=ap.parse_args()
    try:
        plan=qa_native.js(args.plan)
        verdict=verify(plan,args.evidence)
    except (OSError,ValueError) as exc:
        verdict={'schema':'b01-60-qa-verdict-v2','status':'BLOCKED','problems':['UNREADABLE_PLAN:'+type(exc).__name__],
                 'attribution':'EVIDENCE_STRUCTURE_ONLY','g2':'HOLD','manual':'NOT_RUN','stages':[]}
    text=json.dumps(verdict,sort_keys=True,indent=2)+'\n'
    if args.out:Path(args.out).write_text(text,encoding='utf-8')
    print(text,end='')
    return 0 if verdict['status']=='PASS' else 2 if verdict['status']=='FAIL' else 3
if __name__=='__main__':raise SystemExit(main())