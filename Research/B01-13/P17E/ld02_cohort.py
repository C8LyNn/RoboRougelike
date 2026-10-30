#!/usr/bin/env python3
"""B01-13 LD02-COHORT-001: strict, private technical-session funnel aggregation."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import statistics
import sys

BASE_SOURCE_SHA = '61f1e65deaaac3f670850a18e1a7011d7b8cb22a7640a383b5103fcfffa1a516'
PARENT_OBSERVER_HEAD = '5a2420b479f2e8afad3d1f6cf3fff059243ec39f'
SCHEMA = 'b01-ld02-technical-cohort-v1'
MIN_SESSIONS = 3
MAX_SESSIONS = 1000
MAX_DIR_ENTRIES = 1000


class IntakeError(ValueError):
    """Fail closed; no session metadata in exception strings."""


def parent_module():
    path = Path(__file__).resolve().parent.parent / 'P17D' / 'ld02_observer.py'
    if not path.is_file() or path.is_symlink():
        raise IntakeError('ORIGINAL_OBSERVER_NOT_FOUND')
    if hashlib.sha256(path.read_bytes()).hexdigest() != BASE_SOURCE_SHA:
        raise IntakeError('ORIGINAL_OBSERVER_SHA256_MISMATCH')
    spec = importlib.util.spec_from_file_location('ld02_cohort_exact_parent', path)
    if spec is None or spec.loader is None:
        raise IntakeError('OBSERVER_MODULE_NOT_LOADABLE')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def private_location(path, observer):
    try:
        return observer.private_root(path)
    except (ValueError, OSError) as exc:
        raise IntakeError('PRIVATE_LOCATION_REJECTED') from exc


def audit_sessions(folder, player, source_sha, observer):
    folder = private_location(folder, observer)
    if not folder.is_dir() or folder.is_symlink():
        raise IntakeError('INPUT_DIRECTORY_MISSING')
    player = Path(player)
    if not player.is_file() or player.is_symlink():
        raise IntakeError('PLAYER_FILE_NOT_REGULAR')
    if not re.fullmatch(r'[0-9a-f]{40}', source_sha):
        raise IntakeError('SOURCE_SHA_INVALID')
    real_player_hash = observer.file_sha(player)
    paths = sorted(folder.iterdir(), key=lambda p: p.name)
    if len(paths) > MAX_DIR_ENTRIES:
        raise IntakeError('INPUT_COUNT_LIMIT')
    if len(paths) < MIN_SESSIONS:
        raise IntakeError('COHORT_TOO_SMALL')
    if len(paths) > MAX_SESSIONS:
        raise IntakeError('COHORT_TOO_LARGE')
    seen = set()
    audited = []
    for path in paths:
        if path.suffix != '.jsonl' or path.is_symlink() or not path.is_file():
            raise IntakeError('UNEXPECTED_FILE_OR_SYMLINK_IN_COHORT')
        try:
            rows = observer.read_rows(path)
            stats = observer.validate_events(rows)
        except (ValueError, OSError, TypeError) as exc:
            raise IntakeError('INVALID_OR_TAMPERED_SOURCE_LOG') from exc
        header = rows[0]['payload']
        if not stats['ended'] or stats['active_task'] is not None:
            raise IntakeError('SESSION_NOT_FINISHED')
        if header['mode'] != 'technical':
            raise IntakeError('NONTECHNICAL_OR_HUMAN_SESSION_REFUSED')
        if header['source_sha'] != source_sha:
            raise IntakeError('SOURCE_BUILD_MISMATCH')
        if header['player_sha256'] != real_player_hash:
            raise IntakeError('PLAYER_BINARY_MISMATCH')
        sid = header['session']
        if path.name != sid + '.jsonl':
            raise IntakeError('LOG_FILENAME_SESSION_MISMATCH')
        if sid in seen:
            raise IntakeError('DUPLICATE_SESSION_ID')
        seen.add(sid)
        audited.append(stats)
    return audited, real_player_hash


def count_and_metrics(audited, observer):
    n = len(audited)
    tasks = {}
    all_hints = 0
    all_blocks = 0
    for name in observer.TASKS:
        entries = [stats['tasks'][name] for stats in audited]
        completed = [item['duration_ms'] for item in entries if item['status'] == 'COMPLETE']
        statuses = {s: sum(item['status'] == s for item in entries)
                    for s in ('COMPLETE', 'ABANDONED', 'INTERRUPTED', 'NOT_OBSERVED')}
        if sum(statuses.values()) != n:
            raise IntakeError('INVALID_TASK_STATE')
        hints = sum(item['hints'] for item in entries)
        blocks = sum(item['blocks'] for item in entries)
        all_hints += hints
        all_blocks += blocks
        tasks[name] = {
            'reached': n - statuses['NOT_OBSERVED'],
            'completed': statuses['COMPLETE'],
            'abandoned': statuses['ABANDONED'],
            'interrupted': statuses['INTERRUPTED'],
            'not_observed': statuses['NOT_OBSERVED'],
            'completed_rate_of_all_sessions': round(statuses['COMPLETE'] / n, 4),
            'hint_events_total': hints,
            'block_events_total': blocks,
            # Small groups do not reveal a single operator-marked duration.
            'completed_median_ms_if_at_least_3': statistics.median(completed) if len(completed) >= MIN_SESSIONS else None,
        }
    full = sum(stats['completed'] == len(observer.TASKS) for stats in audited)
    return {
        'schema': SCHEMA, 'technical_sessions': n, 'human_participants_verified': 0,
        'technical_observations_not_gameplay_telemetry': True,
        'actual_player_executed_by_this_tool': False,
        'completed_all_four_stages': full,
        'full_completion_rate_of_all_sessions': round(full / n, 4),
        'hint_events_total': all_hints, 'block_events_total': all_blocks,
        'stages': tasks,
    }


def build_report(folder, player, source_sha, observer=None):
    observer = observer if observer is not None else parent_module()
    audited, exe_sha = audit_sessions(folder, player, source_sha, observer)
    report = count_and_metrics(audited, observer)
    report['game_source_sha_claim'] = source_sha
    report['observed_executable_sha256'] = exe_sha
    report['original_observer_commit'] = PARENT_OBSERVER_HEAD
    report['original_observer_source_sha256'] = BASE_SOURCE_SHA
    report['identity_and_actual_play_not_independently_attested'] = True
    report['human_data_included'] = False
    return report


def publish(path, report, observer):
    dest = Path(os.path.abspath(os.path.expanduser(str(path))))
    if dest.suffix != '.json':
        raise IntakeError('OUTPUT_MUST_BE_JSON')
    root = private_location(dest.parent, observer)
    if not root.is_dir() or dest.is_symlink():
        raise IntakeError('OUTPUT_PARENT_OR_LINK_INVALID')
    if dest.exists():
        raise IntakeError('OUTPUT_EXISTS_NO_OVERWRITE')
    raw = (json.dumps(report, sort_keys=True, indent=2, ensure_ascii=True) + '\n').encode('ascii')
    fd = os.open(dest, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    try:
        with os.fdopen(fd, 'wb', closefd=True) as target:
            target.write(raw)
            target.flush()
            os.fsync(target.fileno())
    except BaseException:
        # Fail closed; leave partial path, never silently mark it as valid.
        raise
    return hashlib.sha256(raw).hexdigest()


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--sessions-dir', required=True, help='private non-Git directory: 3+ immutable W06 technical JSONL sessions only')
    parser.add_argument('--player-file', required=True, help='local physical executable bytes whose SHA must match all headers')
    parser.add_argument('--source-sha', required=True, help='40-hex expected source claim from each header')
    parser.add_argument('--output', required=True, help='new private .json outside input session directory; never overwrite')
    args = parser.parse_args(argv)
    try:
        observer = parent_module()
        inp = private_location(args.sessions_dir, observer)
        out = Path(os.path.abspath(os.path.expanduser(args.output)))
        if out.parent == inp or inp in out.parents:
            raise IntakeError('OUTPUT_INSIDE_INPUT_DIRECTORY')
        report = build_report(inp, args.player_file, args.source_sha, observer)
        digest = publish(out, report, observer)
        print('TECHNICAL_COHORT_COMPLETE output_sha256=' + digest + ' count=' + str(report['technical_sessions']))
        return 0
    except (IntakeError, ValueError, OSError) as exc:
        print('FAIL_CLOSED: ' + str(exc), file=sys.stderr)
        return 2


if __name__ == '__main__':
    raise SystemExit(main())
