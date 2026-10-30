#!/usr/bin/env python3
"""Read-only provenance-locked execution of frozen B01-50 PlayerFootprint on a real ZIP."""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import sys
import zipfile
import zlib

FROZEN_GIT_BLOB = 'f8acb5a17d1721aade5d78eb8a8ae33f7db8477d'
FROZEN_GIT_COMMIT = 'd7278f224faecb2aa694bcb93139ef338171e885'
FROZEN_SOURCE_PATH = 'Tools/B01/B01-50/PLAYERFOOTPRINT001/playerfootprint.py'
HISTORICAL_ARCHIVE_SHA = 'a4e1054e09066bb6cd27d3985994ac1a66bcc85caf7ff202af8f59e340bdf442'
SCHEMA = 'B01-50-W14-PINNED-PLAYER-RECEIPT-001'


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1 << 20), b''):
            h.update(chunk)
    return h.hexdigest()


def git_blob(path: Path) -> str:
    data = path.read_bytes()
    return hashlib.sha1(b'blob ' + str(len(data)).encode('ascii') + b'\0' + data).hexdigest()


def verify(source: Path, archive: Path, expected_sha: str, out: Path) -> dict:
    if not re.fullmatch('[0-9a-f]{64}', expected_sha):
        raise ValueError('invalid expected ZIP hash')
    if source.is_symlink() or not source.is_file():
        raise ValueError('frozen source unavailable or symlinked')
    if archive.is_symlink() or not archive.is_file():
        raise ValueError('archive unavailable or symlinked')
    if out.exists() or out.resolve() in (source.resolve(), archive.resolve()):
        raise ValueError('refuse overwrite of existing file or input')
    actual_blob = git_blob(source)
    if actual_blob != FROZEN_GIT_BLOB:
        raise ValueError(f'REJECT source drift original Git blob: {actual_blob}')
    actual_archive = sha256(archive)
    if actual_archive != expected_sha:
        raise ValueError(f'REJECT archive SHA mismatch: {actual_archive}')
    spec = importlib.util.spec_from_file_location('b0150_w13_original_player_inspector', source)
    if spec is None or spec.loader is None:
        raise ValueError('frozen Python source could not be loaded')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    original = module.inspect(archive)
    if original['archive_sha256'] != expected_sha or original['provenance'] != 'WINDOWS_PLAYER_ZIP_BYTES_NOT_NATIVE_EXECUTION':
        raise ValueError('original report has unexpected provenance or archive hash')
    original_text = json.dumps(original, sort_keys=True, ensure_ascii=False, indent=2) + '\n'
    receipt = {
        'schema': SCHEMA,
        'source_pr': 540,
        'source_commit': FROZEN_GIT_COMMIT,
        'source_git_blob': actual_blob,
        'archive_sha256': actual_archive,
        'archive_bytes': original['archive_bytes'],
        'zip_non_directory_entries': original['summary']['files'],
        'uncompressed_file_bytes': original['summary']['player_uncompressed_bytes'],
        'original_full_json_sha256': hashlib.sha256(original_text.encode('utf-8')).hexdigest(),
        'original_full_json_bytes': len(original_text.encode('utf-8')),
        'largest_files': original['summary']['largest_files'][:5],
        'native_unity_editor_executed': False,
        'player_process_executed': False,
        'runtime_fps_or_memory_measured': False,
        'archive_provenance': 'HISTORICAL_EXTERNAL_BUILD_OPERATOR_DECLARED',
    }
    out.parent.mkdir(parents=True, exist_ok=True)
    with out.open('x', encoding='utf-8', newline='\n') as f:
        json.dump(receipt, f, sort_keys=True, ensure_ascii=False, indent=2)
        f.write('\n')
    return receipt


def main(argv=None) -> int:
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--source', required=True, type=Path)
    p.add_argument('--archive', required=True, type=Path)
    p.add_argument('--expected-sha256', required=True)
    p.add_argument('--out', required=True, type=Path)
    args = p.parse_args(argv)
    try:
        receipt = verify(args.source, args.archive, args.expected_sha256, args.out)
        print('HISTORICAL_PLAYER_SOURCE_AUDIT_PASS', receipt['archive_sha256'],
              'files', receipt['zip_non_directory_entries'],
              'bytes', receipt['uncompressed_file_bytes'])
        return 0
    except (OSError, ValueError, RuntimeError, ImportError, EOFError, zipfile.BadZipFile, zlib.error) as exc:
        print('PLAYER_AUDIT_REJECT', type(exc).__name__, str(exc), file=sys.stderr)
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
