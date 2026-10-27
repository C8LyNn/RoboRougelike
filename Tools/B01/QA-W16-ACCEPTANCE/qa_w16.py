#!/usr/bin/env python3
"""B01-60 W16: fail-closed *evidence-data* checks, never Unity G1 certification.

This is a read-only verifier for telemetry produced by CHL-003 and PARTSIZE-001.
It cannot attest the process that generated its input; output labels reflect this.
"""
import argparse
import json
import math
import re
from pathlib import Path

SHA40 = re.compile(r'^[0-9a-f]{40}$')
SHA64 = re.compile(r'^[0-9a-f]{64}$')

class InvalidEvidence(ValueError):
    pass


def require(cond, code):
    if not cond:
        raise InvalidEvidence(code)


def number(v):
    return type(v) in (int, float) and math.isfinite(v)


def positive(v):
    return number(v) and v > 0


def nonnegative(v):
    return number(v) and v >= 0


def vector3(v, strictly_positive=True):
    return isinstance(v, list) and len(v) == 3 and all(positive(x) if strictly_positive else number(x) for x in v)


def common(payload, expected):
    require(isinstance(payload, dict), 'INVALID_ROOT')
    require(payload.get('schema') == 'b01-60-w16-qa-input-v1', 'INVALID_SCHEMA')
    require(payload.get('package_id') == expected, 'WRONG_PACKAGE')
    require(payload.get('version') == 'R1', 'WRONG_VERSION')
    require(isinstance(payload.get('source_sha'), str) and SHA40.fullmatch(payload['source_sha']), 'INVALID_SOURCE_SHA')
    require(payload.get('native_status') in ('NOT_RUN', 'EXTERNAL_UNVERIFIED'), 'FALSE_NATIVE_ASSERTION')
    require(payload.get('origin') in ('SYNTHETIC_TEST', 'EXTERNAL_PRODUCER'), 'UNDECLARED_ORIGIN')
    if payload['origin'] == 'SYNTHETIC_TEST':
        require(payload['native_status'] == 'NOT_RUN', 'SYNTHETIC_CLAIMS_NATIVE')
    else:
        require(payload['native_status'] == 'EXTERNAL_UNVERIFIED', 'EXTERNAL_ORIGIN_STATUS')
    # Even when external telemetry is structurally consistent, B01-60 native remains NOT_RUN.


def check_chl(payload):
    common(payload, 'CHL-003')
    course = payload.get('course')
    require(isinstance(course, dict), 'MISSING_COURSE')
    checkpoints = course.get('checkpoint_ids')
    require(isinstance(checkpoints, list) and 3 <= len(checkpoints) <= 5, 'CHECKPOINT_COUNT')
    require(all(isinstance(x, str) and x and x.strip() == x for x in checkpoints), 'CHECKPOINT_ID')
    require(len(set(checkpoints)) == len(checkpoints), 'DUPLICATE_CHECKPOINT')
    require(isinstance(course.get('seed'), int) and type(course['seed']) is int, 'INVALID_SEED')
    rig_list = payload.get('rigs')
    require(isinstance(rig_list, list) and len(rig_list) == 2, 'NEED_TWO_RIGS')
    rigs = {}
    for r in rig_list:
        require(isinstance(r, dict) and isinstance(r.get('id'), str) and r['id'], 'RIG_ID')
        require(r['id'] not in rigs, 'DUPLICATE_RIG_ID')
        require(positive(r.get('mass_kg')) and positive(r.get('width_m')), 'INVALID_RIG_GEOMETRY')
        require(r.get('rigidbody') is True, 'NOT_A_RIGIDBODY')
        rigs[r['id']] = r
    a, b = rig_list
    require(abs(a['width_m'] - b['width_m']) >= .1 or abs(a['mass_kg'] - b['mass_kg']) >= 1, 'RIGS_NOT_PHYSICALLY_DISTINCT')
    trials = payload.get('trials')
    require(isinstance(trials, list) and len(trials) >= 6, 'INSUFFICIENT_TRIALS')
    trial_keys, tallies = set(), {k: 0 for k in rigs}
    for trial in trials:
        require(isinstance(trial, dict), 'INVALID_TRIAL')
        rid = trial.get('rig_id')
        require(rid in rigs, 'UNKNOWN_RIG')
        replicate = trial.get('replicate')
        require(type(replicate) is int and replicate in (1, 2, 3), 'BAD_REPLICATE')
        require((rid, replicate) not in trial_keys, 'DUPLICATE_TRIAL')
        trial_keys.add((rid, replicate))
        tallies[rid] += 1
        require(trial.get('seed') == course['seed'], 'SEED_MISMATCH')
        require(trial.get('physics_engine') == 'Unity.PhysX', 'NOT_REAL_PHYSICS_DECLARATION')
        require(type(trial.get('fixed_steps')) is int and trial['fixed_steps'] > 0, 'MISSING_FIXED_STEPS')
        require(positive(trial.get('displacement_m')), 'MISSING_MOTION')
        require(type(trial.get('wheel_contact_samples')) is int and trial['wheel_contact_samples'] > 0, 'NO_CONTACT_SAMPLES')
        require(type(trial.get('control_samples')) is int and trial['control_samples'] > 0, 'NO_CONTROL_SAMPLES')
        require(isinstance(trial.get('telemetry_sha256'), str) and SHA64.fullmatch(trial['telemetry_sha256']), 'MISSING_TELEMETRY_HASH')
        events = trial.get('events')
        require(isinstance(events, list) and len(events) >= len(checkpoints) + 2, 'EVENTS_INCOMPLETE')
        require(events[0] == {'type': 'begin', 'actor': rid}, 'INVALID_START')
        index, terminal, completed_count = 0, False, 0
        for e in events[1:]:
            require(isinstance(e, dict) and e.get('actor') == rid, 'FOREIGN_ACTOR_COUNTED')
            t = e.get('type')
            require(not terminal, 'EVENT_AFTER_TERMINAL')
            if t == 'checkpoint':
                require(index < len(checkpoints) and e.get('id') == checkpoints[index], 'BAD_CHECKPOINT_ORDER_OR_DUPLICATE')
                index += 1
            elif t == 'finish':
                require(index == len(checkpoints), 'EARLY_FINISH')
                terminal = True
                completed_count += 1
            else:
                raise InvalidEvidence('ILLEGAL_COUNTED_EVENT')
        require(terminal and completed_count == 1, 'NO_SINGLE_COMPLETION')
    require(all(n >= 3 for n in tallies.values()), 'MISSING_RIG_REPEATS')
    return {'data_gate': 'CONSISTENT_UNVERIFIED_INPUT', 'package_id': 'CHL-003',
            'trials_checked': len(trials), 'checkpoints': len(checkpoints),
            'independent_native_unity': 'NOT_RUN', 'g2': 'HOLD'}


def check_partsize(payload):
    common(payload, 'PARTSIZE-001')
    require(number(payload.get('grid_m')) and abs(payload['grid_m'] - .55) < 1e-8, 'GRID_MISMATCH')
    parts = payload.get('parts')
    require(isinstance(parts, list) and len(parts) >= 1, 'NO_PARTS')
    observed = set()
    for part in parts:
        require(isinstance(part, dict), 'INVALID_PART')
        name = part.get('id')
        require(isinstance(name, str) and name and name not in observed, 'DUPLICATE_OR_MISSING_PART')
        observed.add(name)
        cells = part.get('cells')
        require(isinstance(cells, list) and len(cells) == 3 and all(type(x) is int and 1 <= x <= 24 for x in cells), 'INVALID_CELLS')
        require(isinstance(part.get('physical_owner_id'), str) and part['physical_owner_id'], 'OWNER_MISSING')
        require(isinstance(part.get('mesh_sha256'), str) and SHA64.fullmatch(part['mesh_sha256']), 'MESH_SOURCE_UNHASHED')
        samples = part.get('samples')
        require(isinstance(samples, list) and len(samples) == 4, 'NEED_FOUR_YAWS')
        yaws = [s.get('yaw_degrees') for s in samples if isinstance(s, dict)]
        require(set(yaws) == {0, 90, 180, 270} and len(yaws) == 4, 'INVALID_YAW_MATRIX')
        indexed = {s['yaw_degrees']: s for s in samples}
        for yaw, s in indexed.items():
            box = s.get('collider_aabb_m')
            require(vector3(box), 'INVALID_COLLIDER_AABB')
            require(s.get('physics_api') == 'Unity.Physics', 'NOT_PHYSX_SOURCE_DECLARATION')
            require(nonnegative(s.get('penetration_m')), 'INVALID_PENETRATION')
            require(s['penetration_m'] <= .005, 'OVERLAPPING_COLLIDERS')
            planned = [cells[0] * .55, cells[1] * .55, cells[2] * .55]
            if yaw in (90, 270): planned[0], planned[2] = planned[2], planned[0]
            # QA-only 0.025m preflight tolerance; not a production geometry decision.
            require(all(actual <= budget + .025 for actual, budget in zip(box, planned)), 'COLLIDER_EXCEEDS_LOGICAL_FOOTPRINT')
        x0, x90, x180, x270 = [indexed[y]['collider_aabb_m'] for y in (0,90,180,270)]
        near = lambda x,y: abs(x-y) <= .025
        require(all(near(x0[i], x180[i]) and near(x90[i], x270[i]) for i in (0,1,2)), 'OPPOSITE_YAW_DIMENSIONS_CHANGED')
        require(near(x0[0],x90[2]) and near(x0[2],x90[0]) and near(x0[1],x90[1]), 'RIGHT_ANGLE_AABB_NOT_SWAPPED')
    return {'data_gate': 'CONSISTENT_UNVERIFIED_INPUT', 'package_id': 'PARTSIZE-001',
            'parts_checked': len(parts), 'rotations_checked': 4*len(parts),
            'independent_native_unity': 'NOT_RUN', 'g2': 'HOLD'}


def verify(payload):
    pid = payload.get('package_id') if isinstance(payload, dict) else None
    if pid == 'CHL-003': return check_chl(payload)
    if pid == 'PARTSIZE-001': return check_partsize(payload)
    raise InvalidEvidence('UNKNOWN_PACKAGE')


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('json_receipt', type=Path)
    ns = ap.parse_args()
    try:
        out = verify(json.loads(ns.json_receipt.read_text(encoding='utf-8')))
        status=0
    except (ValueError, OSError, TypeError, KeyError) as e:
        out = {'data_gate': 'FAIL_CLOSED', 'problem': str(e), 'independent_native_unity': 'NOT_RUN','g2':'HOLD'}
        status=2
    print(json.dumps(out, indent=2, sort_keys=True))
    return status


if __name__ == '__main__': raise SystemExit(main())
