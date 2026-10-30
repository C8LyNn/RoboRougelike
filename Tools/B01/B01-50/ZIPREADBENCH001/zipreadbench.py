#!/usr/bin/env python3
"""Measure observed ZIP read+decompression latency using the immutable #540 validator."""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import platform
import statistics
import sys
import time
import zipfile

SOURCE_BLOB = 'f8acb5a17d1721aade5d78eb8a8ae33f7db8477d'
SOURCE_COMMIT = 'd7278f224faecb2aa694bcb93139ef338171e885'
SCHEMA = 'b01-50-zipreadbench-v1'
CHUNK = 1024 * 1024


def file_hash(path: Path, git: bool = False) -> str:
    h = hashlib.sha1() if git else hashlib.sha256()
    with path.open('rb') as f:
        if git:
            h.update(b'blob ' + str(path.stat().st_size).encode() + b'\0')
        for block in iter(lambda: f.read(CHUNK), b''):
            h.update(block)
    return h.hexdigest()


def median(values: list[int]) -> int:
    return int(statistics.median(values))


def percentile(values: list[int], fraction: float) -> int:
    ordered = sorted(values)
    return ordered[max(0, math.ceil(len(ordered) * fraction) - 1)]


def bench(archive: Path, original_source: Path, expected_sha: str, repeats: int) -> dict:
    if not 2 <= repeats <= 8:
        raise ValueError('repeats must be 2..8')
    if archive.is_symlink() or original_source.is_symlink() or not archive.is_file() or not original_source.is_file():
        raise ValueError('input must be regular non-symlink file')
    if file_hash(original_source, git=True) != SOURCE_BLOB:
        raise ValueError('frozen source blob mismatch')
    if len(expected_sha) != 64 or any(c not in '0123456789abcdef' for c in expected_sha):
        raise ValueError('expected SHA256 malformed')
    if file_hash(archive) != expected_sha:
        raise ValueError('archive SHA256 mismatch')
    spec = importlib.util.spec_from_file_location('b0150_frozen_validator', original_source)
    if spec is None or spec.loader is None:
        raise ValueError('original source unavailable')
    original = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(original)
    snapshot = original.inspect(archive)
    if snapshot['archive_sha256'] != expected_sha:
        raise ValueError('original SHA mismatch')
    expected = {e['path']: e for e in snapshot['entries']}
    samples = {path: [] for path in expected}
    rounds = []
    with zipfile.ZipFile(archive, 'r') as z:
        members = [i for i in z.infolist() if not i.is_dir()]
        prefix, _ = original.detect_root([x.filename for x in members], snapshot['build_name'])
        if len(members) != len(expected):
            raise ValueError('ZIP member count mismatch')
        for repeat in range(repeats):
            start = time.perf_counter_ns()
            total_bytes = 0
            for member in members:
                if not member.filename.startswith(prefix):
                    raise ValueError('unexpected ZIP root')
                name = member.filename[len(prefix):]
                if name not in expected:
                    raise ValueError('unexpected member')
                t0 = time.perf_counter_ns()
                digest = hashlib.sha256()
                n = 0
                with z.open(member) as stream:
                    for block in iter(lambda: stream.read(CHUNK), b''):
                        digest.update(block)
                        n += len(block)
                ns = time.perf_counter_ns() - t0
                if n != expected[name]['bytes'] or digest.hexdigest() != expected[name]['sha256']:
                    raise ValueError('member bytes changed during benchmark')
                samples[name].append(ns)
                total_bytes += n
            elapsed = time.perf_counter_ns() - start
            if total_bytes != snapshot['summary']['player_uncompressed_bytes']:
                raise ValueError('round byte count mismatch')
            rounds.append({'round': repeat + 1, 'elapsed_ns': elapsed, 'bytes_read': total_bytes})
    if file_hash(archive) != expected_sha:
        raise ValueError('archive mutated during benchmark')
    files = [{'path': name, 'bytes': expected[name]['bytes'],
              'median_read_ns': median(samples[name]), 'p95_read_ns': percentile(samples[name], .95)}
             for name in sorted(expected)]
    top = sorted(files, key=lambda v: (-v['median_read_ns'], v['path']))[:15]
    round_ns = [r['elapsed_ns'] for r in rounds]
    size = snapshot['summary']['player_uncompressed_bytes']
    return {'schema': SCHEMA, 'provenance': 'HISTORICAL_ZIP_READ_AND_DECOMPRESS_NOT_UNITY_RUNTIME',
            'archive_sha256': expected_sha, 'original_source_blob': SOURCE_BLOB,
            'original_source_commit': SOURCE_COMMIT, 'archive_bytes': snapshot['archive_bytes'],
            'files': len(files), 'uncompressed_bytes': size, 'repeats': repeats,
            'host_os': platform.system(), 'python_version': platform.python_version(),
            'rounds': rounds, 'median_round_ns': median(round_ns),
            'p95_round_ns': percentile(round_ns, .95), 'top_median_read': top,
            'per_file': files,
            'limitations': 'Wall time includes Python/hash/ZIP/IO and OS cache; not Unity loading time or FPS.'}


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--archive', required=True, type=Path)
    p.add_argument('--frozen-source', required=True, type=Path)
    p.add_argument('--expected-sha256', required=True)
    p.add_argument('--out', required=True, type=Path)
    p.add_argument('--repeats', type=int, default=3)
    a = p.parse_args(argv)
    try:
        if a.out.exists() or a.out.resolve() in (a.archive.resolve(), a.frozen_source.resolve()):
            raise ValueError('output path already exists or equals input')
        result = bench(a.archive, a.frozen_source, a.expected_sha256, a.repeats)
        a.out.parent.mkdir(parents=True, exist_ok=True)
        with a.out.open('x', encoding='utf-8', newline='\n') as f:
            json.dump(result, f, indent=2, sort_keys=True, ensure_ascii=False)
            f.write('\n')
        print('ZIP_READ_BENCH_OBSERVED', result['files'], 'files', 'median_round_ms', round(result['median_round_ns']/1e6, 3))
        return 0
    except (ValueError, OSError, RuntimeError, zipfile.BadZipFile, EOFError) as e:
        print('ZIP_READ_BENCH_REJECT', type(e).__name__, str(e), file=sys.stderr)
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
