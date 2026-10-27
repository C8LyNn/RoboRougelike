#!/usr/bin/env python3
"""B01-20 W66 independent CHL-003 native receipt operator, QA ONLY.
This supersedes the inaccessible, unhosted W30 script by public, reproducible
source-pinned validation; it is NOT a byte-identical modification of W30.
No Unity command is ever run by --selftest, --preflight-only or --verify-only.
"""
import argparse
import csv
import hashlib
import io
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

EXPECTED_SHA = 'c7b2a234a286089383503da41883d624fe650551'
EXPECTED_SOURCE_ZIP_SHA256 = '6fe99717b8a4ed3e9ee7b80ea0dabfad968a5768c55e7b0062e260186e5e27bb'
UNITY_VERSION = '6000.3.14f1'
ASSEMBLIES = {'EditMode': 'B01.CHL003.EditModeTests', 'PlayMode': 'B01.CHL003.PlayModeTests'}
CASE_CLASSES = {
    'EditMode': {
        'CHL003CoreEditModeTests': '''E01_OrderedGatesAndCompletionOnce E02_EarlyFinishNeverCompletes E03_OutOfOrderAndSkippedCheckpointRejected E04_MultiColliderDuplicateNeverAdvances E05_WrongActorAndWrongFinishRejected E06_ThreeRetriesNoStaleEventsOrElapsed E07_PauseAndResumeFreezeClockAndGates E08_FallFailsOnceAndRetryClearsFailure E09_TimeoutStopsAtExactDeadline E10_InvalidConfigurationAndNegativeTimeFailClosed'''.split(),
        'CHL003FinishAndTelemetryEditModeTests': '''E11_FinishContactBeforeFinalCheckpointCannotWinSameFixedStep E12_FinalCheckpointBeforeFinishAlsoDeferred E13_ExpiredContactPausedAndRetryNeverAutoWin E14_ActualContactFramesGenerateInspectableReproducibleCounters E15_RetryHeavyKeepsRigDifferenceClearsFallAndTime E16_ActualForwardFullHullCrossesGateWithClearance E17_ReverseAndWheelOnlyHitDoNotCreditGate E18_NonFiniteAndApertureVerticalOverflowFailClosed E19_UpstreamB0121HeavyYaw8OldGateFailsBridgeAlignedGatePasses E20_B0160NearGroundFalseSupportRejectsHoveringWheel'''.split(),
    },
    'PlayMode': {
        'CHL003PhysicsPlayModeTests': '''P01_RigidbodyCrossesOrderedActualPhysicsGates P02_RealSolidObstacleIsNotFakeTrigger P03_ActualRigidbodyFallThenRetryResets P04_PausedCourseDoesNotAdvanceTime P05_DifferentPhysicalRigMassesAndColliderFootprints P06_MultiColliderActorOtherActorAndThreeRetries'''.split(),
        'CHL003DemoAcceptancePlayModeTests': '''P07_SameNativeCourseLightHeavyThreeRunsEachWithRealContact P08_FinalCheckpointAndFinishCoLocatedNeverCompleteSameFixedStep P09_NativePhysicsPauseFallRetryAndNoStaleWin P10_AirborneMotorDoesNotAccelerateOrSteerWithoutRealTerrain P11_RealBackwardCrossAndWheelGrazeMustNotScore P12_DrivingForceOccursOnlyWhenRealWheelGroundRaySupports P13_ElapsedTimeTracksPhysicsTicksNotRenderedFrames P14_HeavyYaw8_RealGateMatchesBridgeRatherThanRejectingClearance P15_B0160NearGroundAirGapNeverAppliesThrustOrYaw'''.split(),
    },
}

class ReceiptError(Exception): pass

def require(condition, message):
    if not condition:
        raise ReceiptError(message)

def run_git(path, *args):
    proc = subprocess.run(['git', '-C', str(path), *args], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if proc.returncode:
        raise ReceiptError('GIT_FAILED:' + proc.stderr.decode(errors='replace').strip()[:250])
    return proc.stdout.decode('utf-8', errors='replace').strip()

def hash_file(path):
    h = hashlib.sha256()
    with open(path, 'rb') as f:
        for b in iter(lambda: f.read(1 << 18), b''):
            h.update(b)
    return h.hexdigest()

def safe_output(project, evidence):
    pr, ev = Path(project).resolve(strict=True), Path(evidence).resolve(strict=False)
    require(ev != pr and pr not in ev.parents, 'EVIDENCE_IN_PROJECT_NOT_RUN')
    require(pr != ev and ev not in pr.parents, 'EVIDENCE_ANCESTOR_OF_PROJECT_NOT_RUN')
    return pr, ev

def registered_detached(project, expected_sha):
    root = Path(project).resolve(strict=True)
    marker = root / '.git'
    require(marker.is_file(), 'NOT_REGISTERED_LINKED_WORKTREE_NOT_RUN')
    require(run_git(root, 'rev-parse', 'HEAD') == expected_sha, 'WRONG_SOURCE_SHA_NOT_RUN')
    require(run_git(root, 'rev-parse', '--is-inside-work-tree') == 'true', 'NO_GIT_WORKTREE_NOT_RUN')
    entries, current = [], None
    for line in run_git(root, 'worktree', 'list', '--porcelain').splitlines() + ['']:
        if line.startswith('worktree '):
            current = {'path': line[9:]}
        elif line == 'detached' and current is not None:
            current['detached'] = True
        elif line.startswith('HEAD ') and current is not None:
            current['sha'] = line[5:]
        elif not line and current is not None:
            entries.append(current)
            current = None
    require(any(Path(x['path']).resolve() == root and x.get('detached') and
                x.get('sha') == expected_sha for x in entries), 'NOT_DETACHED_LINKED_WORKTREE_NOT_RUN')
    require(run_git(root, 'status', '--porcelain', '--untracked-files=all') == '', 'DIRTY_CHECKOUT_NOT_RUN')
    ignored = subprocess.run(['git', '-C', str(root), 'ls-files', '--others', '--ignored', '--exclude-standard', '-z', '--',
                              'Assets', 'Packages', 'ProjectSettings'], stdout=subprocess.PIPE, check=True).stdout
    require(not ignored, 'IGNORED_OR_UNTRACKED_UNITY_SOURCE_NOT_RUN')
    return root

def validate_source(project, expected_sha=EXPECTED_SHA, check_manifest=True):
    root = registered_detached(project, expected_sha)
    v = (root / 'ProjectSettings/ProjectVersion.txt').read_text(encoding='utf-8')
    require(re.search(r'^m_EditorVersion:\s*' + re.escape(UNITY_VERSION) + r'\s*$', v, re.M), 'WRONG_UNITY_VERSION_NOT_RUN')
    require((root/'Assets/RoboRougelikeChallenges/CHL003/Demo/CHL003_Demo.unity').is_file(), 'CHL_DEMO_MISSING_NOT_RUN')
    if check_manifest:
        m = json.loads((root / 'Docs/B01/CHL003/manifest.json').read_text(encoding='utf-8'))
        require(m.get('package_id') == 'CHL-003' and m.get('revision') == 'R1', 'BAD_PACKAGE_ID_NOT_RUN')
        sha_map, byte_map = m['source_sha256_by_path'], m['source_bytes_by_path']
        require(len(sha_map) == 60 and set(sha_map) == set(byte_map), 'MANIFEST_FILE_SET_NOT_RUN')
        for name, expected in sha_map.items():
            file = root / name
            require(file.is_file() and not file.is_symlink(), 'SOURCE_FILE_MISSING_NOT_RUN:'+name)
            require(hash_file(file) == expected and file.stat().st_size == byte_map[name], 'SOURCE_MANIFEST_MISMATCH_NOT_RUN:'+name)
    return root

def ids(stage):
    return {f'B01.CHL003.Tests.{cls}.{name}' for cls, names in CASE_CLASSES[stage].items() for name in names}

def validate_xml(stage, xml_text):
    require(stage in ASSEMBLIES, 'UNKNOWN_STAGE')
    root = ET.fromstring(xml_text)
    require(root.tag == 'test-run' and root.get('result') == 'Passed', 'NUNIT_ROOT_NOT_PASSED')
    assemblies = root.findall(".//test-suite[@type='Assembly']")
    require(any(Path((s.get('name') or '')).name.replace('.dll', '') == ASSEMBLIES[stage] for s in assemblies),
            'WRONG_NUNIT_ASSEMBLY')
    nodes = root.findall('.//test-case')
    full, expected = set(), ids(stage)
    require(len(nodes) == len(expected), 'NUNIT_CASE_COUNT_MISMATCH')
    for case in nodes:
        fname = case.get('fullname') or ''
        cls = case.get('classname') or ''
        name = case.get('name') or ''
        require(fname and cls and name and fname == cls + '.' + name, 'NUNIT_CLASSNAME_FULLNAME_MISMATCH')
        require(fname in expected and case.get('result') == 'Passed' and case.get('label') not in ('Ignored','Skipped'),
                'NUNIT_CASE_UNEXPECTED_OR_NOT_PASSED')
        require(fname not in full, 'NUNIT_DUPLICATE_CASE')
        full.add(fname)
    require(full == expected, 'MISSING_NUNIT_CASES')
    return len(nodes)

def extract_trials(log_text):
    lines = log_text.splitlines()
    trials = []
    for index, line in enumerate(lines):
        key = 'CHL003_NATIVE_REAL_TRIAL '
        if key not in line: continue
        header = line.split(key, 1)[1].strip()
        require(header.startswith('profile,attempt,massKg,'), 'TRIAL_HEADER_INVALID')
        require(index + 1 < len(lines), 'TRIAL_ROW_MISSING')
        data = lines[index + 1].strip()
        cols = next(csv.reader([header])); cells = next(csv.reader([data]))
        require(len(cols) == len(cells), 'TRIAL_CSV_FIELD_COUNT')
        trial = dict(zip(cols, cells))
        trials.append(trial)
    return trials

def validate_trials(log_text):
    data = extract_trials(log_text)
    require(len(data) == 6, 'SIX_PHYSICAL_TRIALS_REQUIRED')
    expected = {(p, str(i)) for p in ('LIGHT','HEAVY') for i in (1,2,3)}
    found = {(x.get('profile'), x.get('attempt')) for x in data}
    require(found == expected, 'TRIAL_PROFILE_ATTEMPT_MISMATCH')
    widths = {}
    for d in data:
        def number(name):
            try: val = float(d[name])
            except (KeyError, ValueError, OverflowError): raise ReceiptError('INVALID_TRIAL_NUMBER:'+name)
            require(val == val and val not in (float('inf'), float('-inf')), 'NONFINITE_TRIAL_NUMBER:'+name)
            return val
        profile = d['profile']
        require(d.get('state') == 'Completed', 'TRIAL_NOT_COMPLETED')
        for name, expected_number in (('checkpoints',4),('completions',1),('falls',0)):
            require(number(name) == expected_number, 'TRIAL_BAD_'+name)
        for name, minimum in (('physicsSamples',4),('travelMeters',35),('collisionEntries',0),('contactPoints',0),('rampPoints',0),('bridgePoints',0)):
            require(number(name) > minimum, 'TRIAL_INSUFFICIENT_'+name)
        require(number('massKg') == {'LIGHT':270,'HEAVY':1150}[profile], 'TRIAL_WRONG_MASS')
        require(number('elapsedSec') >= 0, 'NEGATIVE_ELAPSED')
        widths[profile] = number('widthM')
    require(widths['HEAVY'] > widths['LIGHT'], 'RIG_FOOTPRINT_IDENTICAL_OR_INVERTED')
    return len(data)

def validate_receipt(evidence):
    root = Path(evidence)
    meta = json.loads((root / 'execution.json').read_text(encoding='utf-8'))
    require(meta.get('source_sha') == EXPECTED_SHA and meta.get('unity_version') == UNITY_VERSION,
            'WRONG_RECEIPT_SOURCE_OR_UNITY')
    for stage in ASSEMBLIES:
        value = meta['stages'][stage]['exit_code']
        require(type(value) is int and value == 0, 'NATIVE_EXIT_CODE_NOT_INT_ZERO_'+stage)
        xml = root / f'{stage}.xml'
        log = root / f'{stage}.log'
        require(xml.is_file() and log.is_file(), 'RECEIPT_FILES_MISSING_'+stage)
        require(validate_xml(stage, xml.read_text(encoding='utf-8-sig')) == len(ids(stage)), 'NUNIT_STAGE_BAD')
        for file in (xml, log):
            require(meta['hashes'].get(file.name) == hash_file(file), 'RECEIPT_HASH_MISMATCH:'+file.name)
    require(validate_trials((root / 'PlayMode.log').read_text(encoding='utf-8-sig')) == 6, 'TRIALS_BAD')
    return {'source_sha': EXPECTED_SHA,'author_test_inventory':{'EditMode':20,'PlayMode':15},
            'six_native_trial_rows':6,'status':'SOURCE_RECEIPT_VALIDATED_REQUIRES_INDEPENDENT_B01_60'}

def execute_native(project, unity, evidence, authorization):
    require(authorization.startswith('https://github.com/') and '/issues/71#issuecomment-' in authorization,
            'HQ_EXPLICIT_AUTHORIZATION_LINK_REQUIRED_NOT_RUN')
    pr, ev = safe_output(project, evidence)
    validate_source(pr)
    binary = Path(unity).resolve(strict=True)
    require(binary.is_file(), 'NO_UNITY_EXECUTABLE_NOT_RUN')
    require(not ev.exists(), 'EVIDENCE_OUTPUT_ALREADY_EXISTS_NOT_RUN')
    ev.mkdir(parents=True, exist_ok=False)
    payload = {'source_sha':EXPECTED_SHA,'unity_version':UNITY_VERSION,
               'hq_authorization_reference':authorization, 'stages':{},'hashes':{},'status':'NATIVE_UNVERIFIED'}
    for stage, assembly in ASSEMBLIES.items():
        args = [str(binary),'-batchmode','-runTests','-projectPath',str(pr),'-testPlatform',stage,
                '-assemblyNames',assembly,'-testResults',str(ev/f'{stage}.xml'),'-logFile',str(ev/f'{stage}.log')]
        proc = subprocess.run(args, check=False)
        payload['stages'][stage] = {'exit_code':int(proc.returncode),'assembly':assembly,'argv':args}
        for filename in (f'{stage}.xml', f'{stage}.log'):
            file = ev / filename
            if file.is_file():payload['hashes'][filename] = hash_file(file)
        # A failed edit stage cannot be promoted; fail closed before PlayMode.
        if proc.returncode:break
    (ev/'execution.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n',encoding='utf-8')
    return validate_receipt(ev)

def main():
    a=argparse.ArgumentParser(description='B01-20 CHL-003 exact source G1 operator (NOT Unity unless execute-native)')
    a.add_argument('--project')
    a.add_argument('--evidence')
    a.add_argument('--unity')
    a.add_argument('--hq-authorization-reference')
    group=a.add_mutually_exclusive_group(required=True)
    group.add_argument('--preflight-only',action='store_true')
    group.add_argument('--verify-only',action='store_true')
    group.add_argument('--execute-native',action='store_true')
    a.add_argument('--selftest',action='store_true',help='not a separate mode; use as --preflight-only --selftest')
    x=a.parse_args()
    try:
        if x.selftest:
            from test_chl003_w66_operator import run_tests
            run_tests()
            return 0
        if x.verify_only:
            require(x.evidence, 'EVIDENCE_REQUIRED')
            print(json.dumps(validate_receipt(x.evidence),sort_keys=True))
        elif x.preflight_only:
            require(x.project and x.evidence,'PROJECT_AND_EVIDENCE_REQUIRED')
            safe_output(x.project,x.evidence)
            validate_source(x.project)
            print(json.dumps({'source_sha':EXPECTED_SHA,'source_and_worktree_guard':'PASS',
                  'native_unity':'NOT_RUN','operator':'QA_ONLY'},sort_keys=True))
        else:
            require(x.project and x.evidence and x.unity and x.hq_authorization_reference,
                    'MISSING_REQUIRED_NATIVE_PARAMETERS_NOT_RUN')
            print(json.dumps(execute_native(x.project,x.unity,x.evidence,x.hq_authorization_reference),sort_keys=True))
        return 0
    except (ReceiptError, FileNotFoundError, OSError, ET.ParseError, KeyError, ValueError, subprocess.CalledProcessError) as err:
        print('CHL_W66_FAIL_CLOSED '+str(err)+' NATIVE_STATUS=NOT_RUN_OR_NOT_ACCEPTED',file=sys.stderr)
        return 2

if __name__ == '__main__':sys.exit(main())