#!/usr/bin/env python3
"""B01-40 W16: read-only, fail-closed source package intake for PARTSIZE/CHL.

This tool never installs files, authenticates Codex, executes Unity or grants G2.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import re
import subprocess
import stat
import sys
import unicodedata
import zipfile
from pathlib import Path, PurePosixPath

SPEC = {
    'PARTSIZE-001': {'version':'R1','producer':'B01-23','roots':('Assets/B01Packages/PARTSIZE001/', 'Docs/B01/PARTSIZE001/'), 'asset':'Assets/B01Packages/PARTSIZE001/'},
    'CHL-003': {'version':'R1','producer':'B01-20','roots':('Assets/RoboRougelikeChallenges/CHL003/', 'Docs/B01/CHL003/'), 'asset':'Assets/RoboRougelikeChallenges/CHL003/'},
}
STATES = {'DESIGN','IN_PROGRESS','QA','OFFERED_FROZEN','RESERVED_FOR_CODEX_ACK_PENDING','CODEX_CLAIMED','INTEGRATED','RETURNED_FOR_REWORK','REJECTED'}
BAD_PARTS={'CON','PRN','AUX','NUL',*(f'COM{i}' for i in range(1,10)),*(f'LPT{i}' for i in range(1,10))}
SHA256=re.compile(r'^[a-f0-9]{64}$')
SHA1=re.compile(r'^[a-f0-9]{40}$')
GUID=re.compile(r'(?m)^guid:\s*([a-f0-9]{32})\s*$')
MAX_TOTAL=100*1024*1024
MAX_ITEM=30*1024*1024

class GateError(Exception):
    pass

def check_path(name: str) -> str:
    if not isinstance(name,str) or not name or '\\' in name or '\x00' in name or name.startswith('/'):
        raise GateError(f'unsafe_path:{name!r}')
    norm=unicodedata.normalize('NFC',name)
    if norm!=name or name.endswith('/') or any(ord(c)<32 for c in name):
        raise GateError(f'unsafe_path:{name!r}')
    parts=name.split('/')
    if any(x in ('','.','..') or x[-1] in '. ' or ':' in x or x.split('.')[0].upper() in BAD_PARTS for x in parts):
        raise GateError(f'unsafe_path:{name!r}')
    return norm.casefold()

def digest(x:bytes) -> str:
    return hashlib.sha256(x).hexdigest()

def blob_sha1(x:bytes) -> str:
    return hashlib.sha1(b'blob '+str(len(x)).encode()+b'\x00'+x).hexdigest()

def load_json(blob:bytes, label:str):
    try:
        obj=json.loads(blob)
    except (ValueError, UnicodeDecodeError) as exc:
        raise GateError(f'bad_json:{label}:{exc}') from exc
    if not isinstance(obj,dict):
        raise GateError(f'not_object:{label}')
    return obj

def git_tree_at_commit(repo: Path, commit: str) -> dict:
    if not SHA1.fullmatch(commit):
        raise GateError('source_sha_not_full_commit')
    def run(*args):
        proc=subprocess.run(['git','-C',str(repo),*args],capture_output=True,timeout=15,check=False)
        if proc.returncode:
            raise GateError(f'git_source_unavailable:{proc.stderr.decode(errors="replace")[:220]}')
        return proc.stdout
    actual=run('rev-parse','--verify',commit+'^{commit}').decode().strip()
    if actual!=commit:
        raise GateError('git_commit_identity_mismatch')
    rows={}
    for entry in run('ls-tree','-rz','--full-tree',commit).split(b'\0'):
        if not entry:continue
        try:
            header,path=entry.split(b'\t',1)
            mode,kind,sha=header.decode('ascii').split(' ')
            if kind=='blob':rows[path.decode('utf-8')]=sha
        except (UnicodeDecodeError,ValueError) as exc:
            raise GateError('bad_git_tree_encoding') from exc
    return rows

def verify(archive:Path, receipt:dict, target:dict|None=None, source_tree:dict|None=None) -> dict:
    package=receipt.get('package_id')
    if package not in SPEC:
        raise GateError('not_one_of_two_authorized_packages')
    spec=SPEC[package]
    if receipt.get('version')!=spec['version'] or receipt.get('producer')!=spec['producer']:
        raise GateError('wrong_version_or_producer')
    state=receipt.get('state')
    if state not in STATES or state in ('INTEGRATED','RETURNED_FOR_REWORK','REJECTED'):
        raise GateError('state_requires_human_handoff_disposition')
    source_sha=receipt.get('source_sha')
    if state not in ('DESIGN','IN_PROGRESS') and (not isinstance(source_sha,str) or not SHA1.fullmatch(source_sha)):
        raise GateError('missing_immutable_source_sha')
    if state=='CODEX_CLAIMED' and not receipt.get('codex_ack_link','').startswith('https://github.com/C8LyNn/RoboRougelike/'):
        raise GateError('codex_claim_requires_direct_github_ack_reference')
    # An ack URL is metadata, not proof that Codex actually acknowledged; require human review.
    if state=='OFFERED_FROZEN' and not receipt.get('producer_pr','').startswith('https://github.com/C8LyNn/RoboRougelike/pull/'):
        raise GateError('frozen_offer_missing_pr')
    if state in ('OFFERED_FROZEN','CODEX_CLAIMED') and source_tree is None:
        raise GateError('frozen_source_requires_local_git_commit_proof')
    if not archive.is_file():
        raise GateError('missing_physical_source_zip')
    whole=archive.read_bytes()
    if len(whole)>MAX_TOTAL:
        raise GateError('oversize_archive')
    if digest(whole)!=receipt.get('zip_sha256'):
        raise GateError('archive_sha256_mismatch')
    manifests=receipt.get('files')
    if not isinstance(manifests,list) or not manifests:
        raise GateError('missing_per_file_manifest')
    indexed={}
    for f in manifests:
        if not isinstance(f,dict):
            raise GateError('malformed_manifest_record')
        path=f.get('path')
        key=check_path(path)
        if key in indexed:
            raise GateError('manifest_duplicate_casefold_path')
        if not any(path.startswith(p) for p in spec['roots']):
            raise GateError(f'out_of_owner_scope:{path}')
        if not isinstance(f.get('sha256'),str) or not SHA256.fullmatch(f['sha256']) or type(f.get('bytes')) is not int or f['bytes']<0 or f['bytes']>MAX_ITEM:
            raise GateError(f'invalid_source_hash_or_size:{path}')
        indexed[key]=f
    try:
        with zipfile.ZipFile(archive,'r') as z:
            entries=z.infolist()
            if len(entries)!=len(manifests):
                raise GateError(f'zip_count_mismatch:{len(entries)}!={len(manifests)}')
            payload={}
            compressed_total=0
            expanded_total=0
            for entry in entries:
                if entry.is_dir() or (entry.external_attr>>16)&0o170000==stat.S_IFLNK:
                    raise GateError(f'zip_directory_or_symlink:{entry.filename}')
                key=check_path(entry.filename)
                if key in payload:
                    raise GateError(f'zip_casefold_collision:{entry.filename}')
                if key not in indexed:
                    raise GateError(f'unregistered_zip_file:{entry.filename}')
                if entry.filename!=indexed[key]['path']:
                    raise GateError('zip_path_case_mismatch')
                if entry.file_size>MAX_ITEM or expanded_total+entry.file_size>MAX_TOTAL:
                    raise GateError('zip_bomb_size_limit')
                content=z.read(entry)
                if len(content)!=indexed[key]['bytes'] or digest(content)!=indexed[key]['sha256']:
                    raise GateError(f'per_file_sha256_or_size_mismatch:{entry.filename}')
                expanded_total+=entry.file_size
                compressed_total+=entry.compress_size
                payload[entry.filename]=content
            if set(payload)!=set(f['path'] for f in manifests):
                raise GateError('source_file_manifest_set_mismatch')
    except (zipfile.BadZipFile, EOFError, RuntimeError) as exc:
        raise GateError(f'corrupt_zip:{exc}') from exc
    for f in manifests:
        if source_tree is not None:
            gitsha=source_tree.get(f['path'])
            if not isinstance(gitsha,str) or blob_sha1(payload[f['path']])!=gitsha:
                raise GateError(f'source_git_blob_mismatch:{f["path"]}')
    assets={p:v for p,v in payload.items() if p.startswith(spec['asset'])}
    guid_by_path={}
    guid_to_path={}
    for p,b in assets.items():
        if p.endswith('.meta'):
            try: txt=b.decode('utf-8-sig')
            except UnicodeDecodeError as exc: raise GateError(f'invalid_meta_encoding:{p}') from exc
            matches=GUID.findall(txt)
            if len(matches)!=1: raise GateError(f'meta_guid_invalid:{p}')
            guid=matches[0]
            if guid in guid_to_path and guid_to_path[guid]!=p:
                raise GateError(f'guid_collision:{guid}:{p}')
            guid_to_path[guid]=p
            guid_by_path[p]=guid
            original=p[:-5]
            if original not in assets and not any(k.startswith(original+'/') for k in assets):
                raise GateError(f'orphan_meta:{p}')
        elif p+'.meta' not in assets:
            raise GateError(f'missing_meta:{p}')
    asmdefs={}
    references=[]
    for p,b in assets.items():
        if p.endswith('.asmdef'):
            o=load_json(b,p)
            name=o.get('name')
            if not isinstance(name,str) or not name or name in asmdefs:
                raise GateError(f'bad_or_duplicate_assembly:{p}')
            asmdefs[name]=p
            if not isinstance(o.get('references',[]),list):
                raise GateError(f'invalid_asmdef_references:{p}')
            references+=o.get('references',[])
    available=set(asmdefs)|set(receipt.get('external_assemblies',[]))
    local_asmdef_guids={guid_by_path[p+'.meta'] for p in asmdefs.values() if p+'.meta' in guid_by_path}
    ext_guids=receipt.get('external_assembly_guids',[])
    if not isinstance(ext_guids,list) or any(not isinstance(g,str) or not re.fullmatch(r'[a-f0-9]{32}',g) for g in ext_guids):
        raise GateError('invalid_external_asmdef_guids')
    for ref in references:
        if not isinstance(ref,str):raise GateError('invalid_asmdef_reference')
        if ref.startswith('GUID:'):
            if ref[5:] not in local_asmdef_guids and ref[5:] not in ext_guids:
                raise GateError(f'unresolved_asmdef_reference:{ref}')
        elif ref not in available:
            raise GateError(f'unresolved_asmdef_reference:{ref}')
    if not any(p.endswith('.cs') and '/Runtime/' in p for p in assets):
        raise GateError('missing_runtime_implementation')
    if not any(p.endswith('.unity') for p in assets):
        raise GateError('missing_real_demo_scene')
    if not any('/Tests/EditMode/' in p and p.endswith('.cs') for p in assets):
        raise GateError('missing_editmode_test_source')
    if not any('/Tests/PlayMode/' in p and p.endswith('.cs') for p in assets):
        raise GateError('missing_playmode_test_source')
    if not any('/Tests/' in p and p.endswith('.cs') and ('Invalid' in p or 'Negative' in p) for p in assets):
        raise GateError('missing_negative_test_source')
    if package=='CHL-003' and len([p for p in assets if p.endswith('.prefab') and '/Rigs/' in p])<2:
        raise GateError('chl_requires_two_rigidbody_rig_prefabs')
    for name in ('README.md','INSTALL_FOR_CODEX.md','ROLLBACK.md','TEST_PLAN.md'):
        if not any(p.endswith('/'+name) for p in payload if p.startswith('Docs/')):
            raise GateError(f'missing_handoff_document:{name}')
    conflicts=[]
    unchanged=[]
    preserves=[]
    target=target or {}
    if not isinstance(target,dict): raise GateError('invalid_target_index')
    target_by_case={}
    for target_path in target:
        norm=check_path(target_path)
        if norm in target_by_case:
            raise GateError(f'target_casefold_duplicate:{target_path}')
        target_by_case[norm]=target_path
    for p,b in assets.items():
        old_path=target_by_case.get(check_path(p))
        if old_path is not None and old_path!=p:
            conflicts.append(f'target_case_conflict:{old_path}->{p}')
            continue
        row=target.get(p)
        if row is None: continue
        if not isinstance(row,dict) or not isinstance(row.get('sha256'),str) or not SHA256.fullmatch(row['sha256']):
            raise GateError(f'invalid_target_index_entry:{p}')
        if row['sha256']==digest(b):
            unchanged.append(p)
        elif p=='Assets/B01Packages.meta':
            preserves.append(p)
        else:
            conflicts.append(p)
    for existing_path, row in target.items():
        if existing_path.endswith('.meta') and isinstance(row,dict) and row.get('guid'):
            for p,g in guid_by_path.items():
                if g==row['guid'] and existing_path!=p:
                    conflicts.append(f'guid:{g}:{p}->{existing_path}')
    if conflicts:
        raise GateError('target_path_or_guid_conflicts:'+','.join(sorted(set(conflicts))))
    # Fail closed: validating a file/source is not native engine execution or Codex ownership.
    return {'package_id':package,'version':spec['version'],'source_sha':source_sha,
            'state_recorded':state,'source_entries':len(payload),'unity_assets':len(assets),
            'git_source_crosscheck':'EXACT_COMMIT_BLOBS_VERIFIED' if source_tree is not None else 'NOT_RUN',
            'unity_guids':len(guid_to_path),'assemblies':sorted(asmdefs),'sha256':digest(whole),
            'declared_external_assemblies':receipt.get('external_assemblies',[]),
            'declared_external_assembly_guids':ext_guids,
            'target_identical_files':len(unchanged),'target_meta_preserved':preserves,
            'g0_source':'PASS_SOURCE_BYTES','g0_target_live':'NOT_VERIFIED',
            'native_unity':'NOT_RUN_BY_RECEIVER','b01_60_independent_qa':'NOT_VERIFIED',
            'codex_ack':'NOT_VERIFIED','codex_ready':False,'g2':'HOLD',
            'manual_steps':['independent B01-60 native EditMode/PlayMode and physically distinct rigs/real contacts',
                            'authorized target SHA/dirty/path reservation ACK and fresh sandbox stage/rollback',
                            'direct Codex claim comment verification; HQ G2 decision separate']}

def main():
    pa=argparse.ArgumentParser(description=__doc__)
    pa.add_argument('--zip',required=True,type=Path)
    pa.add_argument('--receipt',required=True,type=Path)
    pa.add_argument('--target-index',type=Path)
    pa.add_argument('--git-repo',type=Path,help='read-only local full Git repo: verify blobs from exact source_sha commit')
    args=pa.parse_args()
    try:
        receipt=load_json(args.receipt.read_bytes(),'receipt')
        target=load_json(args.target_index.read_bytes(),'target') if args.target_index else None
        tree=git_tree_at_commit(args.git_repo,receipt['source_sha']) if args.git_repo else None
        report=verify(args.zip,receipt,target,tree)
        print(json.dumps(report,indent=2,sort_keys=True));return 0
    except (GateError,FileNotFoundError,OSError) as exc:
        print(json.dumps({'g0_source':'FAIL_CLOSED','g2':'HOLD','error':str(exc)},indent=2),file=sys.stderr)
        return 2
if __name__=='__main__':sys.exit(main())
