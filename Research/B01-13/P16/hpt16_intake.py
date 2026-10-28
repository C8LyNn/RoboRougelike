#!/usr/bin/env python3
"""B01-13 GBC01 offline participant JSON intake; structural validation only.
Not human consent verification, not gameplay or Unity evidence.
"""
from __future__ import annotations
import argparse, json, os, re, sys, tempfile
from pathlib import Path

SCHEMA='B01-13/PASS16/GBC01/session-v1'
PROTOCOL='B01-13/PASS16/GBC01/protocol-v1'
TOMBSTONES='B01-13/PASS16/GBC01/withdrawn-v1'
VALID={'UNAIDED_SUCCESS','ASSISTED_SUCCESS','FAIL','PRODUCT_BLOCKED','BLOCKED_SETUP'}
PID=re.compile(r'P[0-9]{2,4}\Z');SID=re.compile(r'S[0-9]{2,4}\Z')
SHA40=re.compile(r'[a-fA-F0-9]{40}\Z'); SHA64=re.compile(r'[a-fA-F0-9]{64}\Z')
REF=re.compile(r'[a-zA-Z0-9._-]{5,100}\Z')

class BadReceipt(ValueError): pass

def need(ok,msg):
    if not ok:raise BadReceipt(msg)

def read_json(path,limit=130000):
    need(path.is_file() and not path.is_symlink(),f'Missing/unsafe path: {path.name}')
    need(path.stat().st_size<=limit,f'Oversized file: {path.name}')
    try:
        value=json.loads(path.read_text(encoding='utf-8'))
    except (UnicodeError,ValueError) as exc:raise BadReceipt(f'Invalid JSON: {path.name}: {exc}')
    need(isinstance(value,dict),f'Object required: {path.name}')
    return value

def owner_root(root):
    need(root.is_dir() and not root.is_symlink(),'Private root must be a real directory')
    return root

def load_tomb(root):
    path=root/'withdrawn.json'
    if not path.exists(): return {'schema':TOMBSTONES,'pids':[]}
    v=read_json(path,30000)
    need(v.get('schema')==TOMBSTONES and isinstance(v.get('pids'),list),'Invalid tombstone schema')
    ids=v['pids'];need(all(isinstance(x,str) and PID.fullmatch(x) for x in ids),'Invalid tombstone PID')
    need(len(set(ids))==len(ids),'Duplicate tombstone PID')
    return v

def original_protocol(path):
    p=read_json(path,50000)
    need(p.get('schema')==PROTOCOL,'Wrong protocol schema')
    tasks=p.get('scenario',{}).get('tasks',[])
    need([(x.get('id'),x.get('minutes')) for x in tasks]==[('G01',3),('B02',6),('C03',6)],'Wrong protocol stages')
    source=p.get('source',{})
    need(SHA40.fullmatch(str(source.get('codex_head',''))),'Invalid original source SHA')
    need(isinstance(source.get('source_files'),dict) and len(source['source_files'])>=3,'Missing anchored original code files')
    return p

def inspect(path,proto,tomb):
    x=read_json(path)
    need(x.get('schema')==SCHEMA,'Wrong session schema')
    need(x.get('protocol_schema')==proto['schema'],'Protocol version mismatch')
    need(x.get('source')==proto['source'],'Not pinned to exact original source')
    mode,pid,sid=x.get('mode'),x.get('pid'),x.get('sid')
    need(mode in ('SYNTHETIC','HUMAN_APPROVED'),'Invalid mode')
    need(isinstance(pid,str) and PID.fullmatch(pid) and isinstance(sid,str) and SID.fullmatch(sid),'Invalid pseudonym/session ID')
    need(pid not in tomb['pids'],'WITHDRAWN_PID_IN_LOCAL_EXPORT')
    need(path.name==f'GBC01_{mode}_{pid}_{sid}.json','Filename does not match claimed study identity')
    need(SHA40.fullmatch(str(x.get('buildsha',''))) and SHA64.fullmatch(str(x.get('zipsha',''))),'Missing full build/ZIP SHA')
    witness=x.get('consent_witness',{})
    need(isinstance(witness,dict) and witness.get('operator_attestation') is True,'Missing operator preflight attestation')
    if mode=='HUMAN_APPROVED':
        need(all(isinstance(witness.get(k),str) and REF.fullmatch(witness[k]) for k in ('gate','host','consent','facilitator')),'Missing human HQ/host/consent/facilitator witness refs')
    tasks=x.get('tasks',[])
    need(isinstance(tasks,list) and len(tasks)==3,'Wrong task count')
    for i,(task,spec) in enumerate(zip(tasks,proto['scenario']['tasks'])):
        need(isinstance(task,dict) and task.get('task_id')==spec['id'],'Out of order or mutated task ID')
        need(task.get('configured_minutes')==spec['minutes'],'Task limit does not match exact protocol')
        duration=task.get('elapsed_ms')
        need(type(duration)==int and 0<duration<=60*60*1000,'Invalid actual elapsed time')
        for key,upper in (('errors',99),('help_count',9)):
            value=task.get(key);need(type(value)==int and 0<=value<=upper,f'Invalid {key}')
        outcome=task.get('outcome')
        need(outcome in VALID,'Invalid outcome')
        need(isinstance(task.get('comment'),str) and len(task['comment'])<=500,'Comment invalid or oversized')
        if outcome=='UNAIDED_SUCCESS': need(task['help_count']==0,'Unaided success cannot contain hints')
        if outcome=='ASSISTED_SUCCESS': need(task['help_count']>=1,'Assisted success needs hints')
        if outcome=='PRODUCT_BLOCKED': need(len(task['comment'])>=5,'Product blocker lacks observation')
    return x

def verify(root,protocol):
    root=owner_root(root);proto=original_protocol(protocol);tomb=load_tomb(root)
    files=sorted(root.glob('GBC01_*.json'))
    need(bool(files) or bool(tomb['pids']),'No study exports or withdrawal proof')
    need(len(files)<=500,'Excessive export count')
    records=[]
    for file in files:records.append(inspect(file,proto,tomb))
    keys=[(x['pid'],x['sid']) for x in records]
    need(len(set(keys))==len(keys),'Duplicate participant/session records')
    modes={x['mode'] for x in records};need(len(modes)<=1,'Mixed SYNTHETIC/HUMAN_APPROVED exports')
    versions={(x['buildsha'],x['zipsha']) for x in records};need(len(versions)<=1,'Mixed game builds/ZIPs')
    return {'status':'STRUCTURAL_PASS_NOT_HUMAN_OR_UNITY_PROOF','sessions':len(records),'pids':len({x['pid'] for x in records}), 'mode':next(iter(modes),'WITHDRAWAL_ONLY'),'tasks':len(records)*3,'withdrawn_pids':len(tomb['pids']),'original_source_sha':proto['source']['codex_head'],'human_consent_proven':'NOT_VERIFIED'}

def atomic_write(path, data):
    need(not path.is_symlink(),'Refuse symlink output')
    fd,tmp=tempfile.mkstemp(prefix='.gbc01-',suffix='.tmp',dir=path.parent)
    try:
        with os.fdopen(fd,'w',encoding='utf-8') as f:
            f.write(data);f.flush();os.fsync(f.fileno())
        os.replace(tmp,path)
    finally:
        if os.path.exists(tmp):os.unlink(tmp)

def revoke(root,pid,confirm):
    need(confirm,'Explicit --confirm-local-delete required')
    need(isinstance(pid,str) and PID.fullmatch(pid),'Invalid pseudonym')
    root=owner_root(root)
    # Validate all targets before mutating any file. Only exact filename convention is eligible.
    targets=[]
    for path in root.glob('GBC01_*.json'):
        need(path.is_file() and not path.is_symlink(),'Unsafe target')
        v=read_json(path)
        if v.get('pid')==pid:targets.append(path)
    tomb=load_tomb(root)
    if pid not in tomb['pids']:
        tomb['pids'].append(pid)
        # Tombstone first: any interrupted deletion causes subsequent verify to FAIL_CLOSED.
        atomic_write(root/'withdrawn.json',json.dumps(tomb,ensure_ascii=False,indent=2)+'\n')
    for path in targets:path.unlink()
    return {'status':'LOCAL_EXPORTS_DELETED_NOT_BACKUPS','pid':pid,'deleted_files':len(targets),'remaining_purge':'ALL EXTERNAL COPIES / BACKUPS REQUIRE MANUAL POLICY'}

def main(argv=None):
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('command',choices=('verify','revoke'))
    p.add_argument('--root',type=Path,required=True)
    p.add_argument('--protocol',type=Path,help='Pinned GBC01-protocol.json')
    p.add_argument('--pid')
    p.add_argument('--confirm-local-delete',action='store_true')
    a=p.parse_args(argv)
    try:
        if a.command=='verify':
            need(a.protocol is not None,'--protocol required for verify')
            result=verify(a.root,a.protocol)
        else:result=revoke(a.root,a.pid,a.confirm_local_delete)
        print(json.dumps(result,ensure_ascii=False,sort_keys=True));return 0
    except (BadReceipt,ValueError,OSError) as e:
        print('FAIL_CLOSED: '+str(e),file=sys.stderr);return 2
if __name__=='__main__':raise SystemExit(main())
