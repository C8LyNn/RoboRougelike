#!/usr/bin/env python3
"""B01-00 DISPATCH-001: fail-closed, offline ownership and stream scheduler.

Uses only the Python standard library and never calls GitHub or changes game code.
"""
from __future__ import annotations

import argparse
from contextlib import contextmanager
import hashlib
import json
import os
from pathlib import Path
import re
import sys
import unicodedata

SCHEMA = 'b01-dispatch-v1'
TASK_STATES = {'NEW', 'BLOCKED', 'IN_PROGRESS', 'DONE'}
LOCK_STATES = {'ACTIVE', 'FROZEN', 'CODEX_CLAIMED'}
KINDS = {'production', 'support'}
IDENT = re.compile(r'^[A-Za-z0-9][A-Za-z0-9_-]{1,79}$')
_WINDOWS_DEVICES = re.compile(r'^(?:con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\..*)?$', re.IGNORECASE)
_WINDOWS_FORBIDDEN = frozenset('<>:"|?*')


def folded(value):
    """Conservative cross-platform identity; reject aliases before writing files."""
    return unicodedata.normalize('NFKC', value).casefold()


class DispatchError(ValueError):
    pass


def must_id(value, label):
    if not isinstance(value, str) or not IDENT.fullmatch(value):
        raise DispatchError(f'invalid {label}: {value!r}')
    return value


def clean_scope(value):
    if not isinstance(value, str) or not value or '\\' in value or '\0' in value:
        raise DispatchError(f'invalid scope: {value!r}')
    if (value.startswith('/') or value.endswith('/') or '//' in value
            or any(part in ('.', '..') for part in value.split('/'))):
        raise DispatchError(f'unsafe scope: {value!r}')
    if unicodedata.normalize('NFKC', value) != value:
        raise DispatchError(f'nonportable Unicode path alias: {value!r}')
    for part in value.split('/'):
        if (part != part.strip() or part.endswith('.') or
                _WINDOWS_DEVICES.fullmatch(part) is not None or
                any((not ch.isprintable()) or ch in _WINDOWS_FORBIDDEN for ch in part)):
            raise DispatchError(f'Windows-incompatible scope: {value!r}')
    return value


def overlaps(a, b):
    # Windows/NTFS and macOS defaults may alias case or Unicode forms of same path.
    x, y = folded(a), folded(b)
    return x == y or x.startswith(y + '/') or y.startswith(x + '/')


def scopes_overlap(a, b):
    return any(overlaps(x, y) for x in a for y in b)


def check_scopes(scopes):
    if not isinstance(scopes, list) or not scopes or any(not isinstance(x, str) for x in scopes):
        raise DispatchError('scope must be nonempty string array')
    result = [clean_scope(x) for x in scopes]
    if len(result) != len({folded(x) for x in result}):
        raise DispatchError('duplicate scope')
    return result


def parse_snapshot(src):
    if not isinstance(src, dict) or src.get('schema_version') != SCHEMA:
        raise DispatchError(f'snapshot requires schema_version={SCHEMA}')
    limit = src.get('stream_limit')
    if type(limit) is not int or limit < 1:
        raise DispatchError('stream_limit must be positive integer')
    active = src.get('active_streams')
    approved = src.get('approved_streams')
    if not isinstance(active, list) or not isinstance(approved, list):
        raise DispatchError('active_streams and approved_streams must be arrays')
    for s in active + approved:
        must_id(s, 'stream')
    if (len({folded(x) for x in active}) != len(active)
            or len({folded(x) for x in approved}) != len(approved)
            or not {folded(x) for x in active} <= {folded(x) for x in approved}):
        raise DispatchError('invalid active/approved stream sets')
    if len(active) > limit:
        raise DispatchError('current active streams exceed cap; stop rather than grant new work')
    reservations = src.get('codex_reserved')
    if not isinstance(reservations, list):
        raise DispatchError('codex_reserved must be explicit array; unknown is not empty')
    if src.get('codex_scope_confirmed') is not False and src.get('codex_scope_confirmed') is not True:
        raise DispatchError('codex_scope_confirmed must be boolean')
    reserved = [clean_scope(s) for s in reservations]
    if not src['codex_scope_confirmed'] and not reserved:
        raise DispatchError('unknown Codex scope requires conservative reserved paths')
    if 'tasks' not in src or not isinstance(src['tasks'], list):
        raise DispatchError('tasks missing')
    tasks = {}
    seen_task_ids = set()
    packages = set()
    for row in src['tasks']:
        if not isinstance(row, dict):
            raise DispatchError('invalid task')
        tid = must_id(row.get('id'), 'task id')
        if folded(tid) in seen_task_ids:
            raise DispatchError('duplicate task id')
        seen_task_ids.add(folded(tid))
        pkg = must_id(row.get('package_id'), 'package id')
        if folded(pkg) in packages:
            raise DispatchError('duplicate package id')
        packages.add(folded(pkg))
        must_id(row.get('owner'), 'owner')
        if row.get('kind') not in KINDS or row.get('state') not in TASK_STATES:
            raise DispatchError(f'invalid kind/state for {tid}')
        priority = row.get('priority')
        if type(priority) is not int or not 0 <= priority <= 100:
            raise DispatchError('priority must be integer 0..100')
        check_scopes(row.get('scope'))
        deps = row.get('dependencies')
        if not isinstance(deps, list) or any(not isinstance(x, str) for x in deps) or len(deps) != len(set(deps)):
            raise DispatchError('invalid dependencies')
        stream = row.get('stream')
        if row['kind'] == 'production':
            must_id(stream, 'production stream')
        elif stream is not None:
            raise DispatchError('support task cannot allocate a production stream')
        tasks[tid] = row
    for tid, row in tasks.items():
        for dep in row['dependencies']:
            if dep not in tasks or dep == tid:
                raise DispatchError(f'missing or self dependency for {tid}: {dep}')
    # Acyclic dependency graph required, including already DONE tasks.
    visited, visiting = set(), set()
    def visit(tid):
        if tid in visiting:
            raise DispatchError('dependency cycle')
        if tid in visited:
            return
        visiting.add(tid)
        for dep in tasks[tid]['dependencies']:
            visit(dep)
        visiting.remove(tid)
        visited.add(tid)
    for tid in tasks:
        visit(tid)
    return tasks, active, approved, reserved


def validate_claims(claims):
    if not isinstance(claims, list):
        raise DispatchError('claims must be array')
    seen_ids, seen_packages = set(), set()
    for c in claims:
        if not isinstance(c, dict):
            raise DispatchError('invalid claim record')
        tid = must_id(c.get('id'), 'claim id')
        pkg = must_id(c.get('package_id'), 'claim package')
        must_id(c.get('owner'), 'claim owner')
        check_scopes(c.get('scope'))
        stream = c.get('stream')
        if stream is not None:
            must_id(stream, 'claim stream')
        if c.get('state') not in LOCK_STATES or folded(tid) in seen_ids or folded(pkg) in seen_packages:
            raise DispatchError('invalid/duplicate active claim')
        seen_ids.add(folded(tid)); seen_packages.add(folded(pkg))
    for i, a in enumerate(claims):
        for b in claims[i + 1:]:
            if scopes_overlap(a['scope'], b['scope']):
                raise DispatchError(f'existing claim collision {a["id"]} vs {b["id"]}')
    return claims


def decision(snapshot, claims, task_id):
    tasks, active, approved, codex_reserved = parse_snapshot(snapshot)
    validate_claims(claims)
    if task_id not in tasks:
        raise DispatchError(f'unknown task {task_id}')
    task = tasks[task_id]
    reasons = []
    if task['state'] != 'NEW':
        reasons.append(f'task_state_{task["state"].lower()}')
    for dep in task['dependencies']:
        if tasks[dep]['state'] != 'DONE':
            reasons.append(f'dependency_{dep}_not_done')
    if task['kind'] == 'production':
        stream = task['stream']
        active_now = {folded(x) for x in active} | {folded(c['stream']) for c in claims if c.get('stream') is not None}
        if folded(stream) not in {folded(x) for x in approved}:
            reasons.append('stream_not_approved')
        if folded(stream) not in active_now and len(active_now) >= snapshot['stream_limit']:
            reasons.append('active_stream_limit')
    if scopes_overlap(task['scope'], codex_reserved):
        reasons.append('codex_reserved_scope')
    for claim in claims:
        if folded(task['id']) == folded(claim['id']) or folded(task['package_id']) == folded(claim['package_id']):
            reasons.append('already_claimed')
        elif scopes_overlap(task['scope'], claim['scope']):
            reasons.append(f'overlap_with_{claim["id"]}')
    return {'task_id': task_id, 'owner': task['owner'], 'package_id': task['package_id'],
            'priority': task['priority'], 'eligible': not bool(reasons), 'blockers': sorted(set(reasons))}


def plan(snapshot, claims):
    tasks, *_ = parse_snapshot(snapshot)
    validate_claims(claims)
    ordered = sorted(tasks.values(), key=lambda t: (-t['priority'], t['id']))
    # Reserve eligible scopes in memory for an actual collision-free batch, but without writing.
    shadow = list(claims)
    result = []
    for row in ordered:
        d = decision(snapshot, shadow, row['id'])
        result.append(d)
        if d['eligible']:
            shadow.append({'id': row['id'], 'package_id': row['package_id'], 'owner': row['owner'],
                           'scope': row['scope'], 'stream': row['stream'], 'state': 'ACTIVE'})
    return {'schema_version': SCHEMA, 'decisions': result, 'selected': [r['task_id'] for r in result if r['eligible']]}


def load_json(path):
    with open(path, 'r', encoding='utf-8') as f:
        return json.load(f)


def claims_from_dir(path):
    p = Path(path)
    if p.is_symlink():
        raise DispatchError('claims directory symlink refused')
    if not p.exists():
        return []
    if not p.is_dir():
        raise DispatchError('claims path is not a directory')
    records = []
    for f in sorted(p.glob('*.json')):
        if f.is_symlink() or not f.is_file():
            raise DispatchError('claim symlink/nonfile refused')
        records.append(load_json(f))
    return validate_claims(records)


@contextmanager
def locked(path):
    p = Path(path)
    if p.is_symlink():
        raise DispatchError('claims directory symlink refused')
    p.mkdir(parents=True, exist_ok=True)
    guard = p / '.dispatch_guard'
    try:
        guard.mkdir()  # atomic cross-process guard; fail closed, no automatic stale removal
    except FileExistsError as e:
        raise DispatchError('claims directory busy: .dispatch_guard exists') from e
    try:
        yield p
    finally:
        guard.rmdir()


def claim(snapshot, claims_dir, task_id):
    with locked(claims_dir) as p:
        existing = claims_from_dir(p)
        d = decision(snapshot, existing, task_id)
        if not d['eligible']:
            raise DispatchError(f'claim rejected: {",".join(d["blockers"])}')
        task = next(t for t in snapshot['tasks'] if t['id'] == task_id)
        data = {'id': task_id, 'package_id': task['package_id'], 'owner': task['owner'],
                'scope': task['scope'], 'stream': task['stream'], 'state': 'ACTIVE'}
        raw = (json.dumps(data, sort_keys=True, separators=(',', ':')) + '\n').encode()
        target = p / f'{task_id}.json'
        flags = os.O_CREAT | os.O_EXCL | os.O_WRONLY
        fd = os.open(target, flags, 0o600)
        try:
            with os.fdopen(fd, 'wb') as f:
                f.write(raw); f.flush(); os.fsync(f.fileno())
        except BaseException:
            target.unlink(missing_ok=True)
            raise
        return {'claim': str(target), 'sha256': hashlib.sha256(raw).hexdigest(), 'task_id': task_id}


def _read_exact_owned_claim(path, task_id, expected_sha):
    """Read a file under the guard; never trust a matching hash alone as permission."""
    must_id(task_id, 'task id')
    if not isinstance(expected_sha, str) or not re.fullmatch(r'[0-9a-f]{64}', expected_sha):
        raise DispatchError('transition requires exact sha256')
    if path.is_symlink() or not path.is_file():
        raise DispatchError('claim not found or symlink')
    raw = path.read_bytes()
    if hashlib.sha256(raw).hexdigest() != expected_sha:
        raise DispatchError('claim changed: transition refused')
    try:
        data = json.loads(raw)
    except (json.JSONDecodeError, UnicodeDecodeError) as exc:
        raise DispatchError('invalid claim JSON') from exc
    validate_claims([data])
    if data['id'] != task_id:
        raise DispatchError('mismatched claim identity')
    return data


def freeze(claims_dir, task_id, expected_sha):
    """Transition ACTIVE to immutable FROZEN with an atomic replace and new hash.

    This is NOT Codex ACK. There is intentionally no local unfreeze or claim transfer.
    """
    must_id(task_id, 'task id')
    with locked(claims_dir) as p:
        target = p / f'{task_id}.json'
        data = _read_exact_owned_claim(target, task_id, expected_sha)
        if data['state'] != 'ACTIVE':
            raise DispatchError('only ACTIVE claim may be frozen; no ownership bypass')
        data = dict(data)
        data['state'] = 'FROZEN'
        raw = (json.dumps(data, sort_keys=True, separators=(',', ':')) + '\n').encode('utf-8')
        temp = p / f'.dispatch_freeze_{task_id}.tmp'
        fd = os.open(temp, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
        try:
            with os.fdopen(fd, 'wb') as handle:
                handle.write(raw)
                handle.flush()
                os.fsync(handle.fileno())
            os.replace(temp, target)
        finally:
            temp.unlink(missing_ok=True)
        return {'frozen': task_id, 'claim': str(target),
                'sha256': hashlib.sha256(raw).hexdigest(), 'previous_sha256': expected_sha}


def release(claims_dir, task_id, expected_sha):
    must_id(task_id, 'task id')
    with locked(claims_dir) as p:
        f = p / f'{task_id}.json'
        data = _read_exact_owned_claim(f, task_id, expected_sha)
        if data['state'] != 'ACTIVE':
            raise DispatchError('release forbidden for FROZEN or CODEX_CLAIMED ownership')
        f.unlink()
    return {'released': task_id}


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--snapshot', required=True)
    p.add_argument('--claims-dir', required=True)
    sub = p.add_subparsers(dest='action', required=True)
    sub.add_parser('plan')
    c = sub.add_parser('claim'); c.add_argument('task_id')
    fr = sub.add_parser('freeze'); fr.add_argument('task_id'); fr.add_argument('--expected-sha', required=True)
    r = sub.add_parser('release'); r.add_argument('task_id'); r.add_argument('--expected-sha', required=True)
    args = p.parse_args(argv)
    try:
        if args.action == 'release':
            result = release(args.claims_dir, args.task_id, args.expected_sha)
        elif args.action == 'freeze':
            result = freeze(args.claims_dir, args.task_id, args.expected_sha)
        else:
            snapshot = load_json(args.snapshot)
            result = plan(snapshot, claims_from_dir(args.claims_dir)) if args.action == 'plan' else claim(snapshot, args.claims_dir, args.task_id)
        print(json.dumps(result, indent=2, sort_keys=True))
        return 0
    except (DispatchError, OSError, json.JSONDecodeError, KeyError, TypeError) as exc:
        print(f'DISPATCH_BLOCKED: {exc}', file=sys.stderr)
        return 2


if __name__ == '__main__':
    raise SystemExit(main())
