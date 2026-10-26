#!/usr/bin/env python3
"""Byte-pinned MAPGENWORLD 1.0.1 -> 1.1 disposable-only, journaled upgrade.
Never touches a normal checkout or active Codex project. Neither source nor this script authorizes G2.
"""
import argparse, hashlib, io, json, os, re, subprocess, tempfile, uuid, zipfile
from pathlib import Path

OLD_SHA='28b6422e7c358f379a47b49a0a671f631cee6d7f627daf3f6bf4807d7c3b8a41'
NEW_SHA='a27891c3b9420a3060190c613104121154ef1385c6d15354d2d68aa22f22802a'
PREFIX='Assets/B01Packages/MAPGENWORLD001'
MARKER='.b01-package-auto-sandbox.json'
JOURNAL='.b01-world-upgrade'

class Blocked(RuntimeError):pass

def sha(data):return hashlib.sha256(data).hexdigest()

def git(*args,cwd):
    p=subprocess.run(['git',*args],cwd=cwd,capture_output=True,text=True)
    if p.returncode:raise Blocked('GIT:'+p.stderr.strip())
    return p.stdout.strip()

def atomic_json(path,obj):
    path.parent.mkdir(parents=True,exist_ok=True)
    tmp=path.with_name(path.name+'.'+uuid.uuid4().hex+'.tmp')
    with tmp.open('x') as f:
        json.dump(obj,f,sort_keys=True,indent=2);f.write('\n');f.flush();os.fsync(f.fileno())
    os.replace(tmp,path)

def sandbox(target):
    target=Path(target).resolve()
    marker=target/MARKER
    if not marker.is_file():raise Blocked('NOT_DISPOSABLE_MARKED_WORKTREE')
    j=json.loads(marker.read_text());
    if j.get('sandbox_absolute_path')!=str(target) or not j.get('sandbox_id') or not re.fullmatch(r'[0-9a-f]{40}',j.get('expected_head','')):
        raise Blocked('INVALID_MARKER')
    if not (target/'.git').is_file():raise Blocked('NOT_GIT_WORKTREE')
    if Path(git('rev-parse','--show-toplevel',cwd=target)).resolve()!=target:raise Blocked('WRONG_GIT_ROOT')
    if git('rev-parse','HEAD',cwd=target)!=j['expected_head']:raise Blocked('WRONG_GIT_HEAD')
    return target,j

def source_zip(blob,expected):
    if sha(blob)!=expected:raise Blocked('ORIGINAL_ZIP_SHA_MISMATCH')
    try:
        with zipfile.ZipFile(io.BytesIO(blob)) as z:
            if z.testzip() is not None:raise Blocked('ZIP_CRC')
            data={i.filename:z.read(i.filename) for i in z.infolist() if i.filename.startswith(PREFIX)}
    except (OSError,zipfile.BadZipFile) as e:raise Blocked('BAD_SOURCE_ZIP') from e
    if not data or any(k!=PREFIX+'.meta' and not k.startswith(PREFIX+'/') for k in data):raise Blocked('SOURCE_SCOPE')
    return data

def plan(target,old_zip,new_zip):
    target,marker=sandbox(target)
    old=source_zip(Path(old_zip).read_bytes(),OLD_SHA);new=source_zip(Path(new_zip).read_bytes(),NEW_SHA)
    if set(old)-set(new):raise Blocked('REMOVAL_UNSUPPORTED')
    old_keys=set(old)
    # Verify *all old module bytes* before touching a single file, not just modified ones.
    for name,payload in old.items():
        dst=target/name
        if dst.is_symlink() or not dst.is_file() or sha(dst.read_bytes())!=sha(payload):raise Blocked('TARGET_BASE_MISMATCH:'+name)
    changed={p:payload for p,payload in new.items() if p not in old or payload!=old[p]}
    # Frozen precisely known 5 replacements and 2 additions; never generic arbitrary upgrade.
    change_names=[p for p in changed if p in old];new_names=[p for p in changed if p not in old]
    if (len(change_names),len(new_names))!=(5,2):raise Blocked('UNEXPECTED_CHANGE_SCOPE')
    for name in new_names:
        if (target/name).exists() or (target/name).is_symlink():raise Blocked('ADDITION_ALREADY_EXISTS:'+name)
    shared=target/'Assets/B01Packages.meta'
    if not shared.is_file() or not re.search(rb'(?m)^guid: [0-9a-fA-F]{32}\s*$',shared.read_bytes()):raise Blocked('ROOT_META_MISSING_OR_INVALID')
    # Worktree must be otherwise clean; only marker and our own transaction directory are exempt.
    entries=git('status','--porcelain','--untracked-files=all',cwd=target).splitlines()
    allowed=(MARKER,JOURNAL+'/')
    for line in entries:
        path=line[3:].strip('"')
        if path!=MARKER and not path.startswith(JOURNAL+'/'):raise Blocked('DIRTY_TARGET:'+path)
    actions=[{'path':p,'old_sha256':sha(old[p]) if p in old else None,'new_sha256':sha(d),'new_bytes':len(d)} for p,d in sorted(changed.items())]
    return target,marker,actions,changed,old,{'G0_TARGET':'SYNTHETIC_DISPOSABLE_PREFLIGHT_PASS_NOT_LIVE','replacements':len(change_names),'additions':len(new_names),'actions':actions,'G2':'HOLD'}

def rollback(target,journal):
    target,marker=sandbox(target)
    jp=Path(journal).resolve()
    if (target/JOURNAL).resolve() not in jp.parents:raise Blocked('JOURNAL_OUTSIDE_SANDBOX')
    j=json.loads(jp.read_text())
    if j.get('sandbox_id')!=marker['sandbox_id']:raise Blocked('WRONG_SANDBOX_ID')
    if j['state']=='ROLLED_BACK':return {'restored':0,'state':'ROLLED_BACK'}
    # Verify every touched path BEFORE restoring any. Files not in old/new state are human modifications.
    for action in j['intents']:
        dest=target/action['path'];new=action['new_sha256'];old=action['old_sha256']
        if dest.is_symlink():raise Blocked('ROLLBACK_MODIFIED:'+action['path'])
        if dest.exists():
            if not dest.is_file() or sha(dest.read_bytes()) not in ({old,new}-{None}):raise Blocked('ROLLBACK_MODIFIED:'+action['path'])
        elif old is not None:raise Blocked('ROLLBACK_MISSING_OLD:'+action['path'])
    restored=0
    for action in reversed(j['intents']):
        dest=target/action['path'];old=action['old_sha256']
        if old is not None:
            backup=jp.parent/'backups'/hashlib.sha256(action['path'].encode()).hexdigest()
            if not backup.is_file() or sha(backup.read_bytes())!=old:raise Blocked('BACKUP_MISSING_OR_TAMPERED')
            dest.parent.mkdir(parents=True,exist_ok=True)
            with tempfile.NamedTemporaryFile(dir=dest.parent,delete=False) as f:
                f.write(backup.read_bytes());f.flush();os.fsync(f.fileno());tmp=f.name
            os.replace(tmp,dest)
        elif dest.exists():dest.unlink()
        restored+=1
    j['state']='ROLLED_BACK';atomic_json(jp,j)
    return {'restored':restored,'state':'ROLLED_BACK','G2':'HOLD'}

def stage(target,old_zip,new_zip,fault_after=0):
    target,marker,actions,changed,old,summary=plan(target,old_zip,new_zip)
    jp=target/JOURNAL/(uuid.uuid4().hex+'.json');meta={'state':'INSTALLING','sandbox_id':marker['sandbox_id'],'intents':[]}
    atomic_json(jp,meta)
    try:
        for n,action in enumerate(actions,1):
            name=action['path'];dest=target/name
            # Original bytes persisted before destructive replacement, uniquely bound to path.
            if action['old_sha256']:
                backup=jp.parent/'backups'/hashlib.sha256(name.encode()).hexdigest()
                backup.parent.mkdir(parents=True,exist_ok=True)
                if backup.exists() and sha(backup.read_bytes())!=action['old_sha256']:raise Blocked('BACKUP_CONFLICT')
                if not backup.exists():
                    with backup.open('xb') as f:f.write(old[name]);f.flush();os.fsync(f.fileno())
            meta['intents'].append(action);atomic_json(jp,meta)
            dest.parent.mkdir(parents=True,exist_ok=True)
            with tempfile.NamedTemporaryFile(dir=dest.parent,delete=False) as f:
                f.write(changed[name]);f.flush();os.fsync(f.fileno());temp_name=f.name
            os.replace(temp_name,dest)
            if fault_after and n>=fault_after:raise Blocked('INJECTED_FAILURE')
        meta['state']='INSTALLED';atomic_json(jp,meta)
        return {'upgraded':len(actions),'journal':str(jp),'G2':'HOLD'}
    except BaseException:
        rollback(target,jp)
        raise

def cli():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['plan','stage','rollback']);p.add_argument('--target',required=True);p.add_argument('--old-zip');p.add_argument('--new-zip');p.add_argument('--journal');p.add_argument('--fault-after',type=int,default=0)
    a=p.parse_args()
    try:
        if a.mode=='rollback':r=rollback(a.target,a.journal)
        else:
            if not a.old_zip or not a.new_zip:raise Blocked('BOTH_PINNED_ZIPS_REQUIRED')
            if a.mode=='plan':r=plan(a.target,a.old_zip,a.new_zip)[-1]
            else:r=stage(a.target,a.old_zip,a.new_zip,a.fault_after)
        print(json.dumps(r,indent=2));return 0
    except Blocked as e:print(json.dumps({'status':'BLOCKED','reason':str(e)}));return 2
if __name__=='__main__':raise SystemExit(cli())
