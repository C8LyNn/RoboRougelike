#!/usr/bin/env python3
"""B01-13 LD02-OBS001: private, operator-marked task timeline. Not game telemetry."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import sys
import time

SCHEMA = 'b01-ld02-observer-v1'
TASKS = ('FIGHT', 'GARAGE', 'REPAIR', 'CHECKPOINT')
ACTIONS = ('start', 'hint', 'blocked', 'complete', 'abandon', 'end')
ZERO = '0' * 64
HEX40 = re.compile(r'^[a-f0-9]{40}$')
SESSION = re.compile(r'^[a-zA-Z0-9_-]{1,48}$')
MAX_BYTES = 1_000_000


class Invalid(ValueError):
    pass


def canonical(data):
    return json.dumps(data, sort_keys=True, separators=(',', ':'), ensure_ascii=True).encode('ascii')


def sha(data):
    return hashlib.sha256(data).hexdigest()


def file_sha(path):
    h = hashlib.sha256()
    with open(path, 'rb') as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()


def private_root(root):
    raw = Path(os.path.abspath(os.path.expanduser(str(root))))
    for p in (raw, *raw.parents):
        if p.is_symlink():
            raise Invalid('SYMLINK_PATH_DENIED')
        if (p / '.git').exists():
            raise Invalid('GIT_WORKTREE_DENIED')
    # For symbolic links that are not the final component (including junctions),
    # resolve and recheck. Native Windows ACL/junction protection is NOT certified.
    resolved = raw.resolve(strict=False)
    if resolved != raw:
        raise Invalid('PATH_REDIRECTION_DENIED')
    return raw


def validate_events(rows):
    if not rows:
        raise Invalid('EMPTY_LOG')
    prev = ZERO
    active = None
    ended = False
    next_task = 0
    outcomes = {name: {'status': 'NOT_OBSERVED', 'duration_ms': None, 'hints': 0, 'blocks': 0} for name in TASKS}
    started_at = None
    last_ms = -1
    for index, row in enumerate(rows):
        if not isinstance(row, dict) or set(row) != {'seq', 'prev', 'payload', 'sha256'}:
            raise Invalid('ROW_SCHEMA_INVALID')
        if type(row['seq']) is not int or row['seq'] != index or row['prev'] != prev:
            raise Invalid('SEQUENCE_OR_CHAIN_INVALID')
        core = {k: row[k] for k in ('seq', 'prev', 'payload')}
        expected = sha(canonical(core))
        if row['sha256'] != expected:
            raise Invalid('HASH_CHAIN_INVALID')
        prev = expected
        payload = row['payload']
        if not isinstance(payload, dict):
            raise Invalid('PAYLOAD_INVALID')
        if index == 0:
            if set(payload) != {'type','schema','session','mode','source_sha','player_sha256','approval_ref','consent_ref'}:
                raise Invalid('HEADER_INVALID')
            if payload['type'] != 'header' or payload['schema'] != SCHEMA or not SESSION.fullmatch(str(payload['session'])):
                raise Invalid('HEADER_INVALID')
            if payload['mode'] not in ('dry-run', 'technical', 'human') or not HEX40.fullmatch(str(payload['source_sha'])):
                raise Invalid('SOURCE_OR_MODE_INVALID')
            if not re.fullmatch(r'[a-f0-9]{64}', str(payload['player_sha256'])):
                raise Invalid('PLAYER_PIN_INVALID')
            if payload['mode'] == 'human':
                if not payload['approval_ref'] or not payload['consent_ref']:
                    raise Invalid('HUMAN_GATE_MISSING')
            elif payload['approval_ref'] is not None or payload['consent_ref'] is not None:
                raise Invalid('NON_HUMAN_GATE_VALUES_INVALID')
            continue
        if ended:
            raise Invalid('EVENT_AFTER_END')
        if set(payload) != {'action', 'task', 'elapsed_ms'}:
            raise Invalid('EVENT_SCHEMA_INVALID')
        action, task, ms = payload['action'], payload['task'], payload['elapsed_ms']
        if action not in ACTIONS or type(ms) is not int or ms < last_ms or ms < 0:
            raise Invalid('EVENT_TIME_OR_ACTION_INVALID')
        last_ms = ms
        if action == 'end':
            if task is not None:
                raise Invalid('END_TASK_INVALID')
            ended = True
            if active is not None:
                outcomes[active]['status'] = 'INTERRUPTED'
                outcomes[active]['duration_ms'] = ms - started_at
                active = None
            continue
        if task not in TASKS:
            raise Invalid('UNKNOWN_TASK')
        if action == 'start':
            if active is not None or next_task >= len(TASKS) or task != TASKS[next_task]:
                raise Invalid('TASK_ORDER_INVALID')
            active = task
            started_at = ms
            outcomes[task]['status'] = 'IN_PROGRESS'
            next_task += 1
        elif active != task:
            raise Invalid('TASK_NOT_ACTIVE')
        elif action == 'hint':
            outcomes[task]['hints'] += 1
        elif action == 'blocked':
            outcomes[task]['blocks'] += 1
        elif action in ('complete', 'abandon'):
            outcomes[task]['status'] = 'COMPLETE' if action == 'complete' else 'ABANDONED'
            outcomes[task]['duration_ms'] = ms - started_at
            active = None
            started_at = None
    return {'ended': ended, 'active_task': active, 'tasks': outcomes,
            'completed': sum(t['status'] == 'COMPLETE' for t in outcomes.values()),
            'recorded_events': len(rows) - 1, 'last_elapsed_ms': last_ms}


def make_row(rows, payload):
    prev = rows[-1]['sha256'] if rows else ZERO
    core = {'seq': len(rows), 'prev': prev, 'payload': payload}
    return {**core, 'sha256': sha(canonical(core))}


def add_row(rows, payload):
    row = make_row(rows, payload)
    validate_events(rows + [row])
    rows.append(row)
    return row


def read_rows(path):
    p = Path(path)
    if p.is_symlink() or not p.is_file() or p.stat().st_size > MAX_BYTES:
        raise Invalid('LOG_PATH_OR_SIZE_INVALID')
    raw = p.read_bytes()
    if not raw or not raw.endswith(b'\n'):
        raise Invalid('TRUNCATED_LOG')
    try:
        rows = [json.loads(line) for line in raw.splitlines()]
    except (UnicodeError, json.JSONDecodeError) as exc:
        raise Invalid('INVALID_JSON') from exc
    validate_events(rows)
    return rows


def inspect(path):
    rows = read_rows(path)
    stats = validate_events(rows)
    return {'schema': SCHEMA, 'session': rows[0]['payload']['session'],
            'mode': rows[0]['payload']['mode'],
            'source_sha': rows[0]['payload']['source_sha'],
            'player_sha256': rows[0]['payload']['player_sha256'],
            'observation_only': True, 'gameplay_automatically_verified': False,
            'consent_independently_verified': False,
            'log_hash': file_sha(path), **stats}


def write_row(fd, row):
    raw = canonical(row) + b'\n'
    if len(raw) > 8192:
        raise Invalid('EVENT_TOO_LARGE')
    if os.write(fd, raw) != len(raw):
        raise OSError('PARTIAL_WRITE')
    os.fsync(fd)


def capture(args, input_stream=sys.stdin, output_stream=sys.stdout, clock=time.monotonic_ns):
    root = private_root(args.root)
    if not SESSION.fullmatch(args.session):
        raise Invalid('INVALID_SESSION_ID')
    if not HEX40.fullmatch(args.source_sha):
        raise Invalid('SOURCE_SHA_MUST_BE_40_HEX')
    if args.mode == 'human' and (not args.approval_ref or not args.consent_ref):
        raise Invalid('HUMAN_REQUIRES_BOTH_APPROVAL_AND_CONSENT_REFERENCES')
    if args.mode != 'human' and (args.approval_ref or args.consent_ref):
        raise Invalid('ONLY_HUMAN_MODE_TAKES_APPROVAL_REFERENCES')
    if args.approval_ref and not re.fullmatch('[A-Za-z0-9_:/#.-]{1,80}', args.approval_ref):
        raise Invalid('APPROVAL_REF_INVALID')
    if args.consent_ref and not re.fullmatch('[A-Za-z0-9_:/#.-]{1,80}', args.consent_ref):
        raise Invalid('CONSENT_REF_INVALID')
    player = Path(args.player_file)
    if not player.is_file() or player.is_symlink():
        raise Invalid('PLAYER_FILE_MISSING_OR_SYMLINK')
    player_hash = file_sha(player)  # actual physical binary bytes, not a complete game build
    root.mkdir(mode=0o700, parents=True, exist_ok=True)
    if os.name != 'nt':
        os.chmod(root, 0o700)
    target = root / (args.session + '.jsonl')
    flags = os.O_CREAT | os.O_EXCL | os.O_WRONLY
    fd = os.open(target, flags, 0o600)
    rows = []
    start_ns = clock()
    header = {'type': 'header', 'schema': SCHEMA, 'session': args.session,
              'mode': args.mode, 'source_sha': args.source_sha,
              'player_sha256': player_hash, 'approval_ref': args.approval_ref,
              'consent_ref': args.consent_ref}
    try:
        write_row(fd, add_row(rows, header))
        output_stream.write('Commands: start TASK | hint TASK | blocked TASK | complete TASK | abandon TASK | end\n')
        output_stream.write('TASKS: ' + ', '.join(TASKS) + '\n')
        for line in input_stream:
            words = line.strip().split()
            if not words:
                continue
            if words[0] == 'help':
                output_stream.write('start/hint/blocked/complete/abandon TASK or end\n')
                continue
            if len(words) == 1 and words[0] == 'end':
                action, task = 'end', None
            elif len(words) == 2 and words[0] in ACTIONS[:-1] and words[1] in TASKS:
                action, task = words
            else:
                output_stream.write('REJECT invalid command\n')
                continue
            elapsed_ms = max(0, (clock() - start_ns) // 1_000_000)
            payload = {'action': action, 'task': task, 'elapsed_ms': int(elapsed_ms)}
            try:
                row = add_row(rows, payload)
            except Invalid as exc:
                output_stream.write('REJECT ' + str(exc) + '\n')
                continue
            write_row(fd, row)
            output_stream.write('OK ' + action + (' ' + task if task else '') + '\n')
            if action == 'end':
                break
        if not validate_events(rows)['ended']:
            raise Invalid('MISSING_EXPLICIT_END; PARTIAL_LOG_SAVED')
    finally:
        os.close(fd)
    return inspect(target)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='command', required=True)
    rec = sub.add_parser('capture', help='operator marks timed tasks; no Player control or automated gameplay proof')
    rec.add_argument('--root', required=True)
    rec.add_argument('--session', required=True)
    rec.add_argument('--player-file', required=True, help='real local Player binary to hash')
    rec.add_argument('--source-sha', required=True, help='operator-supplied 40 hex game source SHA')
    rec.add_argument('--mode', choices=['dry-run', 'technical', 'human'], default='dry-run')
    rec.add_argument('--approval-ref')
    rec.add_argument('--consent-ref')
    val = sub.add_parser('validate', help='strict replay of actual on-disk hash-chain and task semantics')
    val.add_argument('--log', required=True)
    args = parser.parse_args(argv)
    try:
        if args.command == 'capture':
            result = capture(args)
        else:
            result = inspect(args.log)
            if not result['ended']:
                raise Invalid('SESSION_NOT_ENDED')
        print(json.dumps(result, sort_keys=True, indent=2))
        return 0
    except (Invalid, OSError, ValueError) as exc:
        print('FAIL_CLOSED: ' + str(exc), file=sys.stderr)
        return 2


if __name__ == '__main__':
    raise SystemExit(main())
