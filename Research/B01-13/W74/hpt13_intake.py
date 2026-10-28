#!/usr/bin/env python3
"""B01-13 P15. Offline HPT13 CSV intake and durable withdrawal tombstones.

This enforces provenance and *structural* consent witness references. It cannot
establish that an actual person consented, HQ approved, or a Unity build ran.
Do not publish private study data or pretend deletion from backups/cloud sync.
"""
from __future__ import annotations
import argparse
import csv
import hashlib
import io
import json
import os
from pathlib import Path
import re
import sys
import tempfile

PINNED_HTML_BLOB = "d9ebb3dfc54f8e3e7e2992949b1852275ffef2a0"
SCHEMA = "B01-13/PASS15/INTAKE/v1"
TOMBSTONES_SCHEMA = "B01-13/PASS15/WITHDRAWALS/v1"
FIELDS = ('mode','pid','sid','buildsha','zipsha','resolution','task_id','outcome','duration_seconds','attempts','errors','help_count','comprehension_0_2','fixture_id','incident','note')
TASKS = tuple(f'HPT-T0{i}' for i in range(1,6))
STATUSES = {'NOT_RUN','UNAIDED_SUCCESS','ASSISTED_SUCCESS','FAIL','PRODUCT_BLOCKED','BLOCKED_SETUP'}
PID_RE = re.compile(r'P\d{2,4}\Z')
SID_RE = re.compile(r'S\d{2,4}\Z')
SHA40 = re.compile(r'[0-9a-fA-F]{40}\Z')
SHA64 = re.compile(r'[0-9a-fA-F]{64}\Z')

class GateError(ValueError):
    pass

def require(condition, reason):
    if not condition: raise GateError(reason)

def sha256(data:bytes)->str: return hashlib.sha256(data).hexdigest()
def git_blob(data:bytes)->str:
    return hashlib.sha1(f'blob {len(data)}\0'.encode('ascii')+data).hexdigest()

def paths(root:Path, html:Path):
    root = root.absolute()
    require(root.is_dir() and not root.is_symlink(), 'Evidence root must be a real directory')
    for path in (html,root/'results.csv',root/'intake.json',root/'withdrawals.json'):
        require(not path.is_symlink(),f'Symlink forbidden: {path.name}')
    require(html.is_file(), 'Pinned original HTML file missing')
    return root

def get_json(path:Path):
    require(path.is_file() and path.stat().st_size<=100_000, f'Missing/oversized {path.name}')
    try: result=json.loads(path.read_text(encoding='utf-8'))
    except (ValueError,UnicodeError) as e: raise GateError(f'Invalid {path.name}: {e}')
    require(isinstance(result,dict),f'Invalid {path.name}: root must be object')
    return result

def get_csv(path:Path):
    require(path.is_file() and path.stat().st_size<=20_000_000, 'Missing/oversized results.csv')
    try:
        data=path.read_text(encoding='utf-8-sig')
        reader=csv.DictReader(io.StringIO(data,newline=''),strict=True)
        require(tuple(reader.fieldnames or [])==FIELDS,'CSV header mismatch')
        rows=list(reader)
    except (csv.Error,UnicodeError,ValueError) as e: raise GateError(f'Invalid CSV: {e}')
    require(all(None not in row and all(isinstance(v,str) for v in row.values()) for row in rows),'Malformed CSV cells')
    return rows

def check_source(html:Path, manifest:dict):
    source=html.read_bytes()
    got=git_blob(source)
    require(got==PINNED_HTML_BLOB, 'Original HTML blob does not match pinned B01-13 source')
    require(manifest.get('schema')==SCHEMA,'Incorrect intake schema')
    require(manifest.get('form_html_git_blob')==got,'Intake form blob does not match original HTML')
    require(manifest.get('mode') in ('SYNTHETIC','HUMAN_APPROVED'),'Unrecognized intake mode')
    require(isinstance(manifest.get('build_sha'),str) and SHA40.fullmatch(manifest['build_sha']),'Bad build SHA')
    require(isinstance(manifest.get('zip_sha256'),str) and SHA64.fullmatch(manifest['zip_sha256']),'Bad ZIP SHA256')
    return got

def withdrawals(root):
    path=root/'withdrawals.json'
    if not path.exists(): return {'schema':TOMBSTONES_SCHEMA,'pids':[]}
    value=get_json(path)
    require(value.get('schema')==TOMBSTONES_SCHEMA and isinstance(value.get('pids'),list),'Invalid withdrawals ledger')
    pids=value['pids'];require(all(isinstance(x,str) and PID_RE.fullmatch(x) for x in pids),'Invalid withdrawal pseudonym')
    require(len(pids)==len(set(pids)),'Duplicate withdrawal pseudonyms')
    return value

def check_sessions(rows,manifest,removed):
    require(bool(rows),'No result rows to validate (empty after withdrawal)')
    groups={}
    for i,r in enumerate(rows):
        require(PID_RE.fullmatch(r['pid']) and SID_RE.fullmatch(r['sid']), f'Row {i}: invalid pseudonym/session ID')
        require(r['pid'] not in removed,f'WITHDRAWN_PARTICIPANT_IN_CSV: {r["pid"]}')
        require(r['mode']==manifest['mode'],'Mixed or wrong research modes')
        require(r['buildsha'].lower()==manifest['build_sha'].lower() and r['zipsha'].lower()==manifest['zip_sha256'].lower(),'Game build SHA mismatch')
        require(r['outcome'] in STATUSES,f'Unknown or retained WITHDRAWN outcome at row {i}')
        require(bool(re.fullmatch(r'[A-Za-z0-9_-]{3,60}',r['fixture_id'])),f'Bad fixture at row {i}')
        require(len(r['note'])<=800 and len(r['incident'])<=200, f'Oversized observation at row {i}')
        if r['outcome']=='NOT_RUN':
            require(all(r[k]=='' for k in ('duration_seconds','attempts','errors','help_count','comprehension_0_2')),'NOT_RUN has metrics')
        else:
            for col,high in [('duration_seconds',3600),('attempts',99),('errors',99),('help_count',2)]:
                raw=r[col]; require(re.fullmatch(r'(0|[1-9][0-9]*)',raw) and int(raw)<=high, f'Bad {col} at row {i}')
            if r['outcome'] in ('UNAIDED_SUCCESS','ASSISTED_SUCCESS','FAIL','PRODUCT_BLOCKED'):
                require(r['comprehension_0_2'] in ('0','1','2'),f'Missing comprehension at row {i}')
            if r['outcome']=='UNAIDED_SUCCESS': require(r['help_count']=='0','Unaided success has help')
            if r['outcome']=='ASSISTED_SUCCESS': require(int(r['help_count'])>=1,'Assisted success without help')
            if r['outcome']=='PRODUCT_BLOCKED': require(len(r['incident'])>=3,'Missing product blocker')
        key=(r['pid'],r['sid'])
        groups.setdefault(key,[]).append(r['task_id'])
    for key,tasks in groups.items(): require(tuple(tasks)==TASKS, f'Incomplete/reordered/duplicate tasks for {key[0]} {key[1]}')
    require(any(r['outcome']!='NOT_RUN' for r in rows),'No completed or attempted task')
    declared=manifest.get('sessions')
    require(isinstance(declared,list),'Missing operator session ledger')
    declared_by={}
    for v in declared:
        require(isinstance(v,dict) and PID_RE.fullmatch(str(v.get('pid',''))) and SID_RE.fullmatch(str(v.get('sid',''))),'Bad session ledger row')
        key=v['pid'],v['sid'];require(key not in declared_by,'Duplicate declared session');declared_by[key]=v
    require(set(groups)==set(declared_by),'CSV sessions do not match operator intake ledger')
    if manifest['mode']=='HUMAN_APPROVED':
        require(re.fullmatch(r'[A-Za-z0-9._-]{5,100}',str(manifest.get('hq_gate_reference',''))),'Missing external HQ authorization reference')
        require(re.fullmatch(r'[A-Za-z0-9._-]{5,100}',str(manifest.get('host_preflight_reference',''))),'Missing actual Player host witness reference')
        require(re.fullmatch(r'[A-Za-z0-9._-]{3,60}',str(manifest.get('facilitator_id',''))),'Missing facilitator ID')
        for key,v in declared_by.items():
            require(v.get('consent_state')=='APPROVED' and re.fullmatch(r'[A-Za-z0-9._-]{5,100}',str(v.get('consent_reference',''))),f'Missing distinct external consent witness {key}')
    return len(groups)

def verify(root,html,allow_empty=False):
    root=paths(root,html); m=get_json(root/'intake.json'); blob=check_source(html,m)
    tomb=withdrawals(root); rows=get_csv(root/'results.csv')
    require(not set(r['pid'] for r in rows)&set(tomb['pids']),'WITHDRAWN_PARTICIPANT_IN_CSV')
    if not rows and tomb['pids'] and m.get('sessions')==[]: count=0
    elif not rows and allow_empty: count=0
    else: count=check_sessions(rows,m,set(tomb['pids']))
    return {'status':'STRUCTURAL_PASS_NOT_HUMAN_OR_GAMEPLAY_PROOF', 'source_html_blob':blob,'mode':m['mode'],'sessions':count,'rows':len(rows),'withdrawn_tombstones':len(tomb['pids']),'csv_sha256':sha256((root/'results.csv').read_bytes())}

def atomic_bytes(path:Path,data:bytes):
    # File is always a regular operator-owned local file; no symlink following.
    require(not path.is_symlink(),f'Symlink output forbidden: {path.name}')
    fd,tmp=tempfile.mkstemp(prefix='.hpt13-',suffix='.tmp',dir=path.parent)
    try:
        with os.fdopen(fd,'wb') as f:
            f.write(data);f.flush();os.fsync(f.fileno())
        os.replace(tmp,path)
    finally:
        if os.path.exists(tmp):os.unlink(tmp)

def revoke(root,html,pid,ack):
    require(ack,'Explicit --confirm-local-export-rewrite is required')
    require(PID_RE.fullmatch(pid),'Invalid withdrawal pseudonym')
    root=paths(root,html)
    # Right to withdraw must NOT be dependent on possessing a valid current HQ consent witness.
    rows=get_csv(root/'results.csv')
    tomb=withdrawals(root)
    original=(root/'results.csv').read_bytes()
    removed=sum(r['pid']==pid for r in rows)
    survivors=[r for r in rows if r['pid']!=pid]
    if pid not in tomb['pids']:
        tomb['pids'].append(pid)
        # Critical safety ordering: durable tombstone FIRST; if process crashes next,
        # verification fails closed until the CSV is cleaned up.
        atomic_bytes(root/'withdrawals.json',(json.dumps(tomb,ensure_ascii=False,indent=2)+'\n').encode('utf-8'))
    output=io.StringIO(newline=''); writer=csv.DictWriter(output,fieldnames=FIELDS,lineterminator='\r\n',extrasaction='raise');writer.writeheader();writer.writerows(survivors)
    atomic_bytes(root/'results.csv',output.getvalue().encode('utf-8'))
    # Remove withdrawn sessions from intake ledger too (after CSV, preserving fail-closed crash states).
    ledger=get_json(root/'intake.json')
    if isinstance(ledger.get('sessions'),list):
        ledger['sessions']=[v for v in ledger['sessions'] if v.get('pid')!=pid]
        atomic_bytes(root/'intake.json',(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n').encode('utf-8'))
    clean=get_csv(root/'results.csv')
    require(all(r['pid']!=pid for r in clean),'Withdrawal purge failed')
    return {'status':'LOCAL_CSV_PURGED_NOT_BACKUPS','removed_rows':removed,'remaining_rows':len(survivors),'old_csv_sha256':sha256(original),'new_csv_sha256':sha256((root/'results.csv').read_bytes()),'backup_and_cloud_copies':'REQUIRE_MANUAL_PURGE'}

def main(argv=None):
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('command',choices=('verify','revoke'))
    p.add_argument('--root',type=Path,required=True)
    p.add_argument('--source-html',type=Path,required=True)
    p.add_argument('--pid',help='Pseudonym to withdraw (P01 etc)')
    p.add_argument('--confirm-local-export-rewrite',action='store_true')
    args=p.parse_args(argv)
    try:
        if args.command=='verify': out=verify(args.root,args.source_html)
        else:out=revoke(args.root,args.source_html,args.pid or '',args.confirm_local_export_rewrite)
        print(json.dumps(out,ensure_ascii=False,sort_keys=True));return 0
    except (GateError,ValueError,OSError) as e:
        print('FAIL_CLOSED: '+str(e),file=sys.stderr);return 2
if __name__=='__main__':raise SystemExit(main())
