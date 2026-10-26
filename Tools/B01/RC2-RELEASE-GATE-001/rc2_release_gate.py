#!/usr/bin/env python3
"""B01-40 RC2 source and retail artifact gate. Read-only; never runs a binary.

No external packages. Does not touch active game checkout, only reads Git objects
and a user-supplied Windows ZIP plus the frozen producer's receipt.
"""
import argparse
import hashlib
import json
import re
import stat
import subprocess
import sys
import zipfile
from pathlib import Path, PurePosixPath

PINS = {
    'production': '46b42f62d591ad4aeb9dbfdeda9997044c6b5e1f',
    'editor_fix': 'f6184d2254c0ff4c59bcd1e9f21db6a134a54097',
    'report': '4f39b4487744f81b3e0075dabd15b67df2f07e25',
    'rc1': '86eb7bb77e6bfc7787f781ea1ccda547b8a4f643',
    'world': '5ce9d806d7fc7eb0ab2ef7b586626404c346174f',
    'keep3': '6bb6796ee3ad88a015fb6c8d033023c0ee3b5e94',
}
EXPECTED_ZIP_SHA = '7d35cd1bc823499a8855258e24ed0a0ff1d47e49bd694adb94c031ef8c946de6'
EXPECTED_ZIP_BYTES = 38_523_382
EXPECTED_ASSEMBLY_SHA = '9250338fa8ff71d153875cea35678f992d1c94461ef4a6c10bfedd978c465910'
EDITOR_FIX_PATHS = {'Assets/Tests/Editor/R1PlayIntegrationTests.cs', 'Assets/Tests/Editor/RRLW34ReviewRegressionTests.cs'}
CLEAN_RC1_PATHS = ('Assets/R3/', 'Assets/R6/', 'Packages/', 'ProjectSettings/')
PKG_PREFIXES = {'world': 'Assets/B01Packages/MAPGENWORLD001', 'keep3': 'Assets/B01Packages/REWARDKEEP3001'}
MAX_UNCOMPRESSED = 1_000_000_000

class GateError(Exception):
    pass

def ensure(cond, reason):
    if not cond:
        raise GateError(reason)

def sha256(data):
    return hashlib.sha256(data).hexdigest()

def file_sha256(path):
    h = hashlib.sha256()
    with open(path, 'rb') as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()

def valid_name(name):
    ensure(isinstance(name, str) and name and '\\' not in name and '\x00' not in name, 'UNSAFE_ZIP_NAME')
    p = PurePosixPath(name)
    ensure(not name.startswith('/') and not re.match(r'^[A-Za-z]:', name) and all(x not in ('', '.', '..') for x in name.rstrip('/').split('/')), 'UNSAFE_ZIP_TRAVERSAL')
    ensure(not any(x.endswith((' ', '.')) for x in name.rstrip('/').split('/')), 'UNSAFE_WINDOWS_PATH')
    ensure(all(not any(c in '<>:"|?*' for c in segment) for segment in name.rstrip('/').split('/')), 'UNSAFE_WINDOWS_NAME')
    return str(p).casefold().rstrip('/')

def inspect_release(zip_path, receipt):
    """Validate *actual ZIP bytes* against per-file digest receipt. Never extract."""
    path = Path(zip_path)
    ensure(path.is_file(), 'PENDING_BINARY_ACCESS')
    ensure(path.stat().st_size == EXPECTED_ZIP_BYTES, 'RC2_ZIP_BYTES_MISMATCH')
    ensure(file_sha256(path) == EXPECTED_ZIP_SHA, 'RC2_ZIP_SHA256_MISMATCH')
    ensure(receipt.get('source') == PINS['production'], 'RECEIPT_SOURCE_MISMATCH')
    ensure(receipt.get('zipBytes') == EXPECTED_ZIP_BYTES and receipt.get('zipSHA256', '').lower() == EXPECTED_ZIP_SHA, 'RECEIPT_PIN_MISMATCH')
    declared = {}
    for item in receipt.get('files', []):
        name = item.get('path', '')
        key = valid_name(name)
        ensure(not name.endswith('/') and key not in declared, 'DUPLICATE_RECEIPT_PATH')
        ensure(isinstance(item.get('bytes'), int) and item['bytes'] >= 0 and re.fullmatch(r'[0-9a-f]{64}', item.get('sha256', '')), 'INVALID_RECEIPT_FILE_HASH')
        declared[key] = item
    ensure(declared, 'EMPTY_RECEIPT')
    actual = set()
    total = 0
    try:
        with zipfile.ZipFile(path, 'r') as z:
            for info in z.infolist():
                key = valid_name(info.filename)
                ensure((info.external_attr >> 16 & 0o170000) != stat.S_IFLNK, 'ZIP_SYMLINK_FORBIDDEN')
                ensure(key not in actual, 'ZIP_DUPLICATE_WINDOWS_PATH')
                actual.add(key)
                if info.is_dir():
                    ensure(key not in declared, 'DIR_FILE_COLLISION')
                    continue
                ensure(key in declared, 'UNEXPECTED_ZIP_FILE:' + info.filename)
                item = declared[key]
                ensure(info.file_size == item['bytes'], 'ZIP_FILE_LENGTH_MISMATCH:' + info.filename)
                total += info.file_size
                ensure(total <= MAX_UNCOMPRESSED, 'ZIP_TOTAL_UNCOMPRESSED_LIMIT')
                digest = hashlib.sha256()
                with z.open(info, 'r') as f:
                    for chunk in iter(lambda: f.read(1024 * 1024), b''):
                        digest.update(chunk)
                ensure(digest.hexdigest() == item['sha256'], 'ZIP_FILE_SHA256_MISMATCH:' + info.filename)
    except (zipfile.BadZipFile, RuntimeError, OSError, EOFError, ValueError) as exc:
        raise GateError('ZIP_BAD_OR_INCOMPLETE:' + type(exc).__name__) from exc
    ensure(set(declared) == {a for a in actual if a in declared}, 'ZIP_MISSING_DECLARED_FILES')
    assembly = next((i for k, i in declared.items() if k == 'roborougelike_data/managed/assembly-csharp.dll'), None)
    ensure(assembly is not None and assembly['sha256'] == EXPECTED_ASSEMBLY_SHA, 'RC2_ASSEMBLY_PIN_MISMATCH')
    ensure('roborougelike.exe' in declared, 'RC2_EXE_MISSING')
    ensure(any(k.startswith('roborougelike_data/') for k in declared), 'RC2_DATA_MISSING')
    return {'status': 'PASS_INDEPENDENT_ACTUAL_ZIP_BYTES', 'files': len(declared), 'zip_sha256': EXPECTED_ZIP_SHA, 'uncompressed_bytes': total}

def git_tree(repo, ref):
    cmd = ['git', '-C', str(repo), 'ls-tree', '-r', '--full-tree', ref]
    p = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False)
    ensure(p.returncode == 0, 'PINNED_GIT_OBJECT_UNAVAILABLE:' + ref)
    d = {}
    for line in p.stdout.decode('utf-8', 'surrogateescape').splitlines():
        meta, sep, name = line.partition('\t')
        ensure(sep == '\t', 'BAD_GIT_TREE_FORMAT')
        mode, kind, sha = meta.split(' ')
        ensure(kind == 'blob', 'NON_BLOB_IN_TREE')
        d[name] = sha
    return d

def inspect_git_source(repo, pins=None):
    pins = pins or PINS
    trees = {k: git_tree(repo, v) for k, v in pins.items()}
    prod, editor, report, rc1 = (trees[n] for n in ('production', 'editor_fix', 'report', 'rc1'))
    results = {}
    for k, prefix in PKG_PREFIXES.items():
        src = trees[k]
        files = {p: h for p, h in src.items() if p == prefix + '.meta' or p.startswith(prefix + '/')}
        ensure(len(files) >= 10, 'EMPTY_PRODUCER_PACKAGE:' + k)
        ensure(all(prod.get(p) == h for p, h in files.items()), 'PRODUCER_PACKAGE_CHANGED:' + k)
        results[k] = len(files)
    protected = {}
    for prefix in CLEAN_RC1_PATHS:
        old = {p:h for p,h in rc1.items() if p.startswith(prefix)}
        new = {p:h for p,h in prod.items() if p.startswith(prefix)}
        ensure(old == new, 'PROTECTED_TREE_MODIFIED:' + prefix)
        protected[prefix] = len(old)
    for newer, label in ((editor, 'EDITOR'), (report, 'REPORT')):
        diffs = {p for p in set(prod)|set(newer) if p.startswith('Assets/') and prod.get(p) != newer.get(p)}
        ensure(diffs == EDITOR_FIX_PATHS, 'UNAUTHORIZED_'+label+'_ASSET_CHANGE:'+','.join(sorted(diffs ^ EDITOR_FIX_PATHS)))
    ensure(prod.get('Assets/B01Packages.meta') == editor.get('Assets/B01Packages.meta') == report.get('Assets/B01Packages.meta'), 'PARENT_META_CHANGED')
    # Protect no assertion on Assets/Pack: RC2 legitimately replaces enemy equipment
    # IDs with canonical physical GUIDs in P12 enemy turret (host producer-owned).
    known_pack_changed = {p for p in set(prod)|set(rc1) if p.startswith('Assets/Pack/') and prod.get(p) != rc1.get(p)}
    ensure(known_pack_changed == {'Assets/Pack/P12_PipelineSafeEnemyTurret.cs'}, 'UNREVIEWED_PACK_CHANGE')
    return {'status':'PASS_PINNED_GIT_SOURCE', 'producer_files':results,'protected_roots':protected, 'editor_only_fixes':sorted(EDITOR_FIX_PATHS), 'pack_change':sorted(known_pack_changed)}

def inspect_evidence(native_records, release_equivalence):
    ensure(isinstance(native_records, list) and native_records, 'NATIVE_RECEIPTS_MISSING')
    ensure(release_equivalence.get('productionBuildSHA') == PINS['production'], 'NATIVE_EQ_SOURCE_MISMATCH')
    ensure(release_equivalence.get('editorTestRevision') == PINS['editor_fix'], 'NATIVE_EQ_TEST_SHA_MISMATCH')
    ensure(not release_equivalence.get('productionChangesExcludingEditorTests'), 'PRODUCTION_CHANGED_AFTER_BUILD')
    ensure(release_equivalence.get('assemblySHA256') == EXPECTED_ASSEMBLY_SHA, 'BUILD_ASSEMBLY_MISMATCH')
    ensure(release_equivalence.get('archiveSHA256') == EXPECTED_ZIP_SHA, 'RELEASE_EQ_ZIP_SHA_MISMATCH')
    full = [x for x in native_records if x.get('nunit',{}).get('total') == 1068 and x.get('phase') == 'EditMode']
    ensure(full, 'FULL_EDIT_RUN_MISSING')
    ensure(all(x['nunit'].get('passed')==1066 and x['nunit'].get('failed')==2 and x.get('exitCode')!=0 for x in full), 'FULL_EDIT_RESULT_CONTRADICTS_PIN')
    reruns=[x for x in native_records if x.get('nunit',{}).get('total') == 2 and x.get('phase') == 'EditMode']
    ensure(reruns and any(x['nunit'].get('passed')==2 and x['nunit'].get('failed')==0 and x.get('exitCode')==0 and x.get('source')==PINS['editor_fix'] for x in reruns),'TARGETED_FIXED_EDIT_RETEST_NOT_PROVED')
    play=[x for x in native_records if x.get('nunit',{}).get('total')==424 and x.get('phase')=='PlayMode']
    ensure(play and any(x['nunit'].get('passed')==424 and x['nunit'].get('failed')==0 and x.get('exitCode')==0 for x in play),'FULL_PLAY_RECEIPT_NOT_PROVED')
    return {'status':'EXTERNAL_NATIVE_RECEIPTS_CONSISTENT_NOT_INDEPENDENT_G1', 'full_edit':'FAIL_1066_OF_1068_OLD_REVISION', 'corrected_2_cases':'PASS_2_OF_2_NEW_EDITOR_REVISION', 'full_edit_new_revision':'NOT_RUN','full_play':'PASS_EXTERNAL_424_OF_424','human_ui':'NOT_RUN','native_shutdown_crash':'NOT_CLEARED'}

def main(argv=None):
    ap=argparse.ArgumentParser(description='Read-only RC2 independent gate; no game execution or import')
    ap.add_argument('--repo',help='local git repository containing all pinned immutable commits')
    ap.add_argument('--zip',dest='zip_path',help='actual RC2 Windows ZIP to verify; without it binary gate is PENDING')
    ap.add_argument('--receipt',help='original RC2 release-receipt.json')
    ap.add_argument('--native',help='original RC2 Native/native-summary.json')
    ap.add_argument('--equivalence',help='original RC2 production-equivalence.json')
    ap.add_argument('--report',help='write JSON status')
    args=ap.parse_args(argv)
    result={'type':'B01-40_RC2_RECEIVER_ONLY','pins':PINS,'G0_TARGET_LIVE':'PENDING_OPERATOR_ACK','G1_B01_60':'NOT_RUN_BY_B01_40','G2':'HOLD'}
    failures=[]
    if args.repo:
        try:result['source']=inspect_git_source(args.repo)
        except (GateError,OSError) as e:failures.append('SOURCE:'+str(e))
    else:result['source']={'status':'NOT_RUN_MISSING_PINNED_GIT_REPOSITORY'}
    if args.zip_path and args.receipt:
        try:result['binary']=inspect_release(args.zip_path,json.loads(Path(args.receipt).read_text(encoding='utf-8')))
        except (GateError,OSError,ValueError) as e:failures.append('BINARY:'+str(e))
    else:result['binary']={'status':'PENDING_BINARY_ACCESS','reason':'Independent operator must provide RC2 ZIP and frozen release-receipt.json'}
    if args.native and args.equivalence:
        try:result['evidence']=inspect_evidence(json.loads(Path(args.native).read_text(encoding='utf-8')),json.loads(Path(args.equivalence).read_text(encoding='utf-8')))
        except (GateError,OSError,ValueError) as e:failures.append('NATIVE:'+str(e))
    else:result['evidence']={'status':'NOT_RUN_LOCAL_EVIDENCE_CHECK'}
    result['errors']=failures
    result['decision']='FAIL' if failures else 'HOLD_PENDING_BINARY_AND_FULL_EDIT_G1_G2'
    out=json.dumps(result,sort_keys=True,indent=2)
    if args.report:Path(args.report).write_text(out+'\n',encoding='utf-8')
    print(out)
    return 2 if failures else 0

if __name__=='__main__':
    sys.exit(main())
