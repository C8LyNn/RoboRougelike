#!/usr/bin/env python3
"""B01-50 PERFRUN-001: fail-closed launcher for *real* B0150 live Player trace.

Does not instrument the game, infer a perf gain, or assert native test acceptance.
Player gameplay configuration is manual; no undocumented game commands are sent.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import time
import zipfile

RECORDER_VALIDATOR_GIT_BLOB = '7060aa3e364b2bc69de99501faa99bba57e84f82'
CAPTURE_CLASS = 'GAME_FRAME_TIME_REVIEW_REQUIRED_NOT_DISPLAY_PRESENTMON_OR_NATIVE_PERF_PASS'
RESULT_CLASS = 'ORIGINAL_PLAYER_TRACE_SCHEMA_VALIDATED_NATIVE_REVIEW_REQUIRED'
ERROR_CLASS = 'CAPTURE_FAILED_NOT_NATIVE_EVIDENCE'
SCENARIO_PATTERN = re.compile(r'^[a-z0-9][a-z0-9_-]{0,47}$')
HEX40 = re.compile(r'^[a-f0-9]{40}$')
WINDOWS_RESERVED = {'con', 'prn', 'aux', 'nul', *(f'com{i}' for i in range(1,10)), *(f'lpt{i}' for i in range(1,10))}


class IntakeError(ValueError):
    pass


def require(condition: bool, message: str) -> None:
    if not condition:
        raise IntakeError(message)


def file_sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda: f.read(1048576), b''):
            h.update(block)
    return h.hexdigest()


def git_blob_sha(path: Path) -> str:
    contents = path.read_bytes()
    return hashlib.sha1(b'blob ' + str(len(contents)).encode('ascii') + b'\0' + contents).hexdigest()


def verify_exe_zip_member(player: Path, build: Path) -> str:
    """Fail closed unless the exact Player executable bytes appear in original ZIP."""
    expected = file_sha256(player)
    found = []
    try:
        with zipfile.ZipFile(build) as z:
            for info in z.infolist():
                if info.is_dir() or Path(info.filename).name.casefold() != player.name.casefold():
                    continue
                h = hashlib.sha256()
                with z.open(info) as src:
                    for block in iter(lambda: src.read(1048576), b''):
                        h.update(block)
                if h.hexdigest() == expected:
                    found.append(info.filename)
    except (zipfile.BadZipFile, RuntimeError, EOFError) as exc:
        raise IntakeError(f'not a readable original build ZIP: {exc}') from exc
    require(len(found) == 1, 'Player.exe must match EXACTLY ONE binary in the supplied build ZIP')
    return found[0]


def validated_inputs(args) -> dict:
    player, build, validator = (Path(x).expanduser().resolve() for x in (args.player, args.build_zip, args.validator))
    require(player.is_file(), 'Player file missing')
    require(build.is_file(), 'build ZIP missing')
    require(validator.is_file(), 'pinned validator missing')
    require(git_blob_sha(validator) == RECORDER_VALIDATOR_GIT_BLOB,
            'validator differs from exact PERFSCALING-001 #328 Git blob')
    require(bool(HEX40.fullmatch(args.game_sha)), 'game SHA must be 40 lowercase hexadecimal characters')
    require(bool(SCENARIO_PATTERN.fullmatch(args.scenario)) and args.scenario not in WINDOWS_RESERVED,
            'unsafe or invalid scenario; use lowercase letters, digits, underscore, dash')
    require(155 <= args.run_seconds <= 600, 'runtime must allow 30s warmup + 120s capture')
    require(player != build and player != validator and build != validator, 'input paths must be distinct')
    player_zip_member = verify_exe_zip_member(player, build)
    parent = Path(args.out_parent).expanduser().resolve()
    require(parent.is_dir(), 'output parent must already exist; never create an arbitrary parent')
    run_dir = parent / args.scenario
    require(not run_dir.exists(), 'refuse overwrite of existing run folder')
    return {'player': player, 'build': build, 'validator': validator, 'parent': parent,
            'run_dir': run_dir, 'game_sha': args.game_sha, 'scenario': args.scenario,
            'run_seconds': args.run_seconds, 'build_sha256': file_sha256(build),
            'player_sha256': file_sha256(player), 'player_zip_member': player_zip_member,
            'validator_git_blob': RECORDER_VALIDATOR_GIT_BLOB}


def player_argv(c: dict) -> list[str]:
    return [str(c['player']), '-b0150-live-output', str(c['run_dir'] / 'capture'),
            '-b0150-live-game-sha', c['game_sha'], '-b0150-live-build-sha', c['build_sha256'],
            '-b0150-live-scenario', c['scenario'], '-logFile', str(c['run_dir'] / 'Player.log')]


def run_and_validate(c: dict, *, runner=subprocess.Popen, verifier=subprocess.run) -> dict:
    """Unit-test injectable subprocessors, no mock mode exposed through production CLI."""
    c['run_dir'].mkdir(parents=False, exist_ok=False)
    argv = player_argv(c)
    capture = c['run_dir'] / 'capture'
    recorded = {'classification': ERROR_CLASS, 'scenario': c['scenario'],
                'declared_game_sha': c['game_sha'], 'build_zip_sha256': c['build_sha256'],
                'player_exe_sha256': c['player_sha256'], 'player_zip_member': c['player_zip_member'],
                'validator_git_blob': c['validator_git_blob'],
                'run_seconds_requested': c['run_seconds'],
                'command': argv, 'unity_edit_play': 'NOT_RUN_BY_OPERATOR',
                'render_gpu_gc_fps': 'NOT_MEASURED', 'native_performance_acceptance': False,
                'stop_reason': None, 'player_returncode': None, 'validator_returncode': None,
                'files_sha256': {}, 'limitations': 'Schema validation only; original Windows Player and hardware, legal game SHA/build, input controls, G1 and Codex acceptance require independent review.'}
    start = time.monotonic()
    try:
        proc = runner(argv, cwd=str(c['player'].parent), stdin=subprocess.DEVNULL,
                      stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        try:
            recorded['player_returncode'] = proc.wait(timeout=c['run_seconds'])
            recorded['stop_reason'] = 'PLAYER_EXITED'
        except subprocess.TimeoutExpired:
            recorded['stop_reason'] = 'OPERATOR_TERMINATED_AFTER_TIMEOUT'
            proc.terminate()
            try:
                recorded['player_returncode'] = proc.wait(timeout=10)
            except subprocess.TimeoutExpired:
                proc.kill()
                recorded['player_returncode'] = proc.wait(timeout=10)
                recorded['stop_reason'] = 'OPERATOR_KILLED_UNRESPONSIVE_PLAYER'
        recorded['observed_seconds'] = round(time.monotonic() - start, 3)
        require(recorded['stop_reason'] != 'OPERATOR_KILLED_UNRESPONSIVE_PLAYER', 'unresponsive Player force-killed')
        if recorded['stop_reason'] == 'PLAYER_EXITED':
            require(recorded['player_returncode'] == 0, 'Player exited nonzero')
        receipt_file = capture / 'receipt.json'
        require(receipt_file.is_file(), 'live capture did not emit receipt.json: check B0150_LIVE_PROFILING build define')
        receipt = json.loads(receipt_file.read_text(encoding='utf-8-sig'))
        require(receipt.get('classification') == CAPTURE_CLASS, 'capture classification mismatch')
        require(receipt.get('game_sha') == c['game_sha'], 'capture game SHA mismatch')
        require(receipt.get('build_zip_sha256') == c['build_sha256'], 'capture ZIP identity mismatch')
        require(receipt.get('scenario_id') == c['scenario'], 'capture scenario mismatch')
        require(receipt.get('capture_complete') is True, 'capture incomplete')
        # Reuse the *exact original* standalone structural receiver, never a test substitute.
        verify_args = [sys.executable, str(c['validator']), '--folder', str(capture),
                       '--game-sha', c['game_sha'], '--build-sha', c['build_sha256'],
                       '--scenario', c['scenario'], '--out', str(c['run_dir'] / 'validation.json')]
        with (c['run_dir'] / 'validator_stdout.log').open('w', encoding='utf-8') as out:
            with (c['run_dir'] / 'validator_stderr.log').open('w', encoding='utf-8') as err:
                v = verifier(verify_args, cwd=str(c['run_dir']), stdout=out, stderr=err,
                             timeout=30, check=False)
        recorded['validator_returncode'] = v.returncode
        require(v.returncode == 0, 'pinned original raw-frame validator returned nonzero')
        validation = json.loads((c['run_dir'] / 'validation.json').read_text(encoding='utf-8'))
        require(validation.get('status') == 'LIVE_RAW_REVIEW_REQUIRED_NOT_PERFORMANCE_ACCEPTANCE',
                'validator status mismatch')
        for name in ('receipt.json', 'frames.csv', 'objects.csv'):
            recorded['files_sha256'][name] = file_sha256(capture / name)
        recorded['classification'] = RESULT_CLASS
    except (OSError, IntakeError, ValueError, json.JSONDecodeError, subprocess.SubprocessError) as exc:
        recorded['error'] = f'{type(exc).__name__}: {exc}'
    finally:
        recorded['observed_seconds'] = round(time.monotonic() - start, 3)
        if (c['run_dir'] / 'Player.log').is_file():
            recorded['player_log_sha256'] = file_sha256(c['run_dir'] / 'Player.log')
        (c['run_dir'] / 'operator_receipt.json').write_text(json.dumps(recorded, indent=2, sort_keys=True)+'\n', encoding='utf-8')
    return recorded


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description='B01-50 original Windows Player live trace operator, no synthetic acceptance')
    parser.add_argument('--player', required=True, help='Existing built Unity Player.exe')
    parser.add_argument('--build-zip', required=True, help='Original source-pinned Player distribution ZIP')
    parser.add_argument('--validator', required=True, help='Exact #328 validate_live.py Git blob')
    parser.add_argument('--game-sha', required=True, help='Actual source commit, lowercase 40 hex')
    parser.add_argument('--scenario', required=True, help='Distinct physically configured scenario ID')
    parser.add_argument('--out-parent', required=True, help='Existing operator-owned destination directory')
    parser.add_argument('--run-seconds', type=int, default=165)
    parser.add_argument('--dry-run', action='store_true', help='Only validate inputs and print command; no file creation/player process')
    args = parser.parse_args(argv)
    try:
        c = validated_inputs(args)
        if args.dry_run:
            print(json.dumps({'classification':'DRY_RUN_NO_PLAYER_EXECUTED', 'command':player_argv(c),
                'build_zip_sha256':c['build_sha256'],'player_exe_sha256':c['player_sha256'],
                'player_zip_member':c['player_zip_member']}, indent=2))
            return 0
        result = run_and_validate(c)
        print(json.dumps({'classification':result['classification'], 'run_dir':str(c['run_dir']),
                          'error':result.get('error')}, indent=2))
        return 0 if result['classification'] == RESULT_CLASS else 3
    except (IntakeError, OSError, ValueError) as exc:
        print(f'INTAKE_REJECTED: {exc}', file=sys.stderr)
        return 2

if __name__ == '__main__':
    raise SystemExit(main())
