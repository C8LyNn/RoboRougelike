#!/usr/bin/env python3
"""B01-13 AIM-VAULT-001: private screenshot evidence index + fail-closed local withdrawal.

Owns only files under an operator-created private vault, never deletes external backups.
All proof is structural; no actual player, gameplay physics, or human consent is established.
"""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import stat
import sys
import tempfile

SCHEMA = 'B01-13/P17-W03/AIM-VAULT-001/v1'
RECEIVER_SHA256 = '8e6cd46bc5f1229b54e565a1b74ea9387d27073d463f61268809f212a9e2ddf4'
SID = re.compile(r'S\d{2,4}\Z')
REPORT = re.compile(r'REPORT\d{2,4}\.html\Z')
HEX40 = re.compile(r'[0-9a-f]{40}\Z')
HEX64 = re.compile(r'[0-9a-f]{64}\Z')
SAFE_IMAGE = re.compile(r'[A-Za-z0-9_.-]{1,80}\.(png|jpg|jpeg|webp)\Z', re.I)

class VaultError(ValueError): pass

def need(condition, reason):
    if not condition: raise VaultError(reason)

def regular(path: Path, max_bytes: int):
    need(path.is_file() and not path.is_symlink(), f'UNSAFE_OR_MISSING_FILE: {path.name}')
    need(path.stat().st_size <= max_bytes, f'FILE_TOO_LARGE: {path.name}')
    return path.read_bytes()

def sha(data: bytes): return hashlib.sha256(data).hexdigest()

def private_root(root: Path):
    need(root.is_dir() and not root.is_symlink(), 'PRIVATE_ROOT_NOT_DIRECTORY')
    # Only local operator data, never a repository tree; do not create or remove from Git.
    need(not any((p/'.git').exists() for p in (root, *root.parents)), 'REFUSE_GIT_WORKTREE')
    if os.name != 'nt':
        need(not (root.stat().st_mode & 0o077), 'PRIVATE_ROOT_MUST_BE_MODE_0700')
    need((root/'sessions').is_dir() and not (root/'sessions').is_symlink(), 'MISSING_SESSIONS_DIR')
    need((root/'derived').is_dir() and not (root/'derived').is_symlink(), 'MISSING_DERIVED_DIR')
    return root

def get_index(root):
    index_path = root/'.aim-vault.json'
    data = regular(index_path, 100000)
    try: v=json.loads(data)
    except (UnicodeError,ValueError) as e: raise VaultError('INVALID_INDEX_JSON: '+str(e))
    need(type(v)==dict and v.get('schema')==SCHEMA, 'INVALID_INDEX_SCHEMA')
    need(type(v.get('sessions'))==dict and type(v.get('derived'))==dict
         and type(v.get('withdrawn'))==list, 'INVALID_INDEX_STRUCTURE')
    need(all(SID.fullmatch(x) for x in v['sessions']) and
         all(SID.fullmatch(x) for x in v['withdrawn']) and
         all(REPORT.fullmatch(x) for x in v['derived']), 'INVALID_INDEX_IDENTIFIERS')
    need(len(v['withdrawn'])==len(set(v['withdrawn'])), 'DUPLICATE_TOMBSTONE')
    for sid,record in v['sessions'].items():
        need(type(record)==dict and type(record.get('files'))==dict and len(record['files'])==5,
             'INVALID_SESSION_INDEX')
        files=record['files']
        need('receipt.json' in files and all(
             (name=='receipt.json' or bool(SAFE_IMAGE.fullmatch(name))) and
             type(h)==str and bool(HEX64.fullmatch(h)) for name,h in files.items()),
             'INVALID_INDEX_FILE_NAME_OR_SHA')
        need(type(record.get('game_sha'))==str and HEX40.fullmatch(record['game_sha']) and
             type(record.get('zip_sha256'))==str and HEX64.fullmatch(record['zip_sha256']) and
             record.get('mode') in ('SYNTHETIC','OPERATOR_SOLO','HUMAN_APPROVED'),
             'INVALID_INDEX_SOURCE_METADATA')
    for name,record in v['derived'].items():
        need(type(record)==dict and type(record.get('sha256'))==str and HEX64.fullmatch(record['sha256']) and
             type(record.get('sessions'))==list and len(record['sessions'])==2 and
             all(type(x)==str and SID.fullmatch(x) for x in record['sessions']) and
             len(set(record['sessions']))==2, 'INVALID_DERIVED_INDEX')
    return v

def write_index(root, index):
    target=root/'.aim-vault.json'
    need(not target.is_symlink(), 'INDEX_SYMLINK_REFUSED')
    data=(json.dumps(index,sort_keys=True,ensure_ascii=False,indent=2)+'\n').encode('utf-8')
    fd,tmp=tempfile.mkstemp(prefix='.aim-index-',dir=root)
    try:
        if os.name != 'nt': os.fchmod(fd,0o600)
        with os.fdopen(fd,'wb') as f:
            f.write(data);f.flush();os.fsync(f.fileno())
        os.replace(tmp,target)
    finally:
        if os.path.lexists(tmp): os.unlink(tmp)

def initialize(root):
    need(not root.exists() or (root.is_dir() and not any(root.iterdir())), 'ROOT_MUST_BE_NEW_OR_EMPTY')
    root.mkdir(parents=True,mode=0o700,exist_ok=True)
    if os.name != 'nt': os.chmod(root,0o700)
    (root/'sessions').mkdir(mode=0o700);(root/'derived').mkdir(mode=0o700)
    write_index(root,{'schema':SCHEMA,'sessions':{},'derived':{},'withdrawn':[]})
    return {'status':'PRIVATE_VAULT_CREATED','human_authorization':'NOT_PROVEN'}

def receiver(path: Path):
    raw=regular(path,20000)
    need(sha(raw)==RECEIVER_SHA256, 'ORIGINAL_RECEIVER_SHA_MISMATCH')
    spec=importlib.util.spec_from_file_location('aim_obs_original',path)
    need(spec and spec.loader, 'ORIGINAL_RECEIVER_UNAVAILABLE')
    module=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module

def require_dirs(root,sid):
    need(type(sid)==str and SID.fullmatch(sid), 'INVALID_SID')
    session=root/'sessions'/sid
    need(session.is_dir() and not session.is_symlink(),'SESSION_DIRECTORY_MISSING_OR_LINKED')
    return session

def session_entries(session):
    entries={p.name:p for p in session.iterdir()}
    need(len(entries)==5 and 'receipt.json' in entries,'EXPECTED_RECEIPT_AND_EXACT_FOUR_IMAGES')
    return entries

def seal(root,sid,receiver_path,game,zipsha,allow_human):
    private_root(root);index=get_index(root)
    need(sid not in index['withdrawn'] and sid not in index['sessions'],'SESSION_EXISTS_OR_WITHDRAWN')
    need(type(game)==str and HEX40.fullmatch(game) and type(zipsha)==str and HEX64.fullmatch(zipsha),'EXPECTED_FULL_BUILD_HASHES')
    session=require_dirs(root,sid);entries=session_entries(session)
    original=receiver(receiver_path)
    try: original.verify(session/'receipt.json',session,game,zipsha)
    except original.Invalid as e: raise VaultError('ORIGINAL_RECEIVER_REJECTED: '+str(e)) from e
    receipt=original.load_json(session/'receipt.json')
    need(receipt['sid']==sid,'SID_MISMATCH')
    need(receipt['mode'] in ('SYNTHETIC','OPERATOR_SOLO') or allow_human,'HUMAN_EVIDENCE_REQUIRES_EXPLICIT_FLAG')
    names={'receipt.json',*[f['image']['filename'] for f in receipt['frames']]}
    need(set(entries)==names,'EXTRA_OR_MISSING_SESSION_FILES')
    hashes={name:sha(regular(path,8*1024*1024)) for name,path in entries.items()}
    need(all(hashes[f['image']['filename']]==f['image']['sha256'] for f in receipt['frames']), 'IMAGE_CHANGED_SINCE_RECEIVER_CHECK')
    index['sessions'][sid]={'mode':receipt['mode'],'game_sha':game,'zip_sha256':zipsha,'files':hashes}
    write_index(root,index)
    return {'status':'SEALED_LOCAL_BYTES_ONLY','sid':sid,'file_count':len(hashes),'mode':receipt['mode'],'consent_authenticated':False}

def attach_derived(root,report_name,sids):
    private_root(root);index=get_index(root)
    need(type(report_name)==str and REPORT.fullmatch(report_name),'INVALID_REPORT_NAME')
    need(report_name not in index['derived'],'REPORT_ALREADY_REGISTERED')
    need(len(sids)==2 and len(set(sids))==2 and all(sid in index['sessions'] and sid not in index['withdrawn'] for sid in sids),'TWO_ACTIVE_SESSIONS_REQUIRED')
    path=root/'derived'/report_name
    digest=sha(regular(path,40*1024*1024))
    index['derived'][report_name]={'sha256':digest,'sessions':sorted(sids)}
    write_index(root,index)
    return {'status':'DERIVED_PRIVATE_REPORT_REGISTERED','report':report_name,'sessions':sorted(sids),'sha256':digest}

def verify(root,receiver_path):
    private_root(root);index=get_index(root)
    # Before reading content, enforce no orphan/extra/linked entries.
    sessions_dir=root/'sessions';derived_dir=root/'derived'
    need({p.name for p in sessions_dir.iterdir()}==set(index['sessions']),'ORPHAN_OR_MISSING_SESSION')
    need({p.name for p in derived_dir.iterdir()}==set(index['derived']),'ORPHAN_OR_MISSING_REPORT')
    original=receiver(receiver_path)
    total=0
    for sid,record in index['sessions'].items():
        need(sid not in index['withdrawn'],'TOMBSTONED_SESSION_STILL_PRESENT')
        folder=require_dirs(root,sid);entries=session_entries(folder)
        need(set(entries)==set(record['files']),'CHANGED_SESSION_FILE_SET')
        for name,expected in record['files'].items():
            need(sha(regular(entries[name],8*1024*1024))==expected,'SEALED_BYTES_CHANGED: '+sid+'/'+name)
            total+=1
        try: original.verify(folder/'receipt.json',folder,record['game_sha'],record['zip_sha256'])
        except original.Invalid as e: raise VaultError('ORIGINAL_RECEIVER_REJECTED: '+str(e)) from e
    for name,record in index['derived'].items():
        need(all(s in index['sessions'] and s not in index['withdrawn'] for s in record['sessions']),'DERIVED_USES_WITHDRAWN_SESSION')
        need(sha(regular(derived_dir/name,40*1024*1024))==record['sha256'],'DERIVED_REPORT_CHANGED: '+name)
        total+=1
    return {'status':'PRIVATE_LOCAL_BYTES_VERIFIED_NOT_GAMEPLAY_OR_CONSENT','sessions':len(index['sessions']),'reports':len(index['derived']),'files_checked':total,'withdrawn':len(index['withdrawn'])}

def withdraw(root,sid,confirm):
    need(confirm,'REQUIRES_CONFIRM_LOCAL_DELETE')
    private_root(root);index=get_index(root)
    need(type(sid)==str and SID.fullmatch(sid),'INVALID_SID')
    need(sid in index['sessions'] or sid in index['withdrawn'],'UNKNOWN_SESSION')
    if sid not in index['withdrawn']:
        index['withdrawn'].append(sid)
        write_index(root,index)  # Tombstone FIRST: interrupted deletion is rejected by verify.
    session=index['sessions'].get(sid)
    reports=[name for name,r in index['derived'].items() if sid in r['sessions']]
    # Refuse untracked files; never recursively delete.
    if session:
        folder=require_dirs(root,sid)
        need({p.name for p in folder.iterdir()}<=set(session['files']), 'FOREIGN_SESSION_FILES_REFUSE_DELETE')
        for name in session['files']:
            p=folder/name
            need(not p.is_symlink() and (not p.exists() or p.is_file()), 'UNSAFE_SESSION_FILE')
    for name in reports:
        p=root/'derived'/name
        need(not p.is_symlink() and (not p.exists() or p.is_file()),'UNSAFE_REPORT_FILE')
    for name in reports:
        p=root/'derived'/name
        if p.exists():p.unlink()
        index['derived'].pop(name)
    if session:
        for name in session['files']:
            p=root/'sessions'/sid/name
            if p.exists():p.unlink()
        (root/'sessions'/sid).rmdir()
        index['sessions'].pop(sid)
    write_index(root,index)
    return {'status':'LOCAL_REGISTERED_FILES_UNLINKED_NOT_SECURE_ERASE_OR_BACKUP_PURGE',
            'sid':sid,'linked_reports_removed':len(reports),'local_session_files_removed':len(session['files']) if session else 0,
            'backups_and_external_copies':'MANUAL_PURGE_REQUIRED'}

def main(argv=None):
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('command',choices=('init','seal','attach-report','verify','withdraw'))
    p.add_argument('--root',type=Path,required=True)
    p.add_argument('--receiver',type=Path)
    p.add_argument('--sid')
    p.add_argument('--game-sha')
    p.add_argument('--zip-sha256')
    p.add_argument('--allow-human-data',action='store_true')
    p.add_argument('--report')
    p.add_argument('--related-session',action='append',default=[])
    p.add_argument('--confirm-local-delete',action='store_true')
    args=p.parse_args(argv)
    try:
        if args.command=='init':v=initialize(args.root)
        elif args.command=='seal':
            need(args.receiver is not None,'RECEIVER_REQUIRED')
            v=seal(args.root,args.sid,args.receiver,args.game_sha,args.zip_sha256,args.allow_human_data)
        elif args.command=='attach-report':v=attach_derived(args.root,args.report,args.related_session)
        elif args.command=='verify':
            need(args.receiver is not None,'RECEIVER_REQUIRED')
            v=verify(args.root,args.receiver)
        else:v=withdraw(args.root,args.sid,args.confirm_local_delete)
        print(json.dumps(v,sort_keys=True,ensure_ascii=False));return 0
    except (VaultError,ValueError,OSError,KeyError,TypeError) as exc:
        print('FAIL_CLOSED: '+str(exc),file=sys.stderr);return 2
if __name__=='__main__':raise SystemExit(main())
