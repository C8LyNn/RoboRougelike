#!/usr/bin/env python3
"""B01-13 W29: verified physical multi-file Windows Player + recovery evidence on W28 intake.
Never authenticates Unity gameplay or a human participant. No network, game writes or extraction.
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
import stat
import sys
import zipfile
from datetime import datetime
from pathlib import PurePosixPath

PACKAGE = 'B01-13-W31-PLAYTEST-EVIDENCE-INTAKE-001/R3'
W27_SOURCE_HEAD = '65c73fd4a3e6c9fcc61b2e388542b96dbfa1748c'
W27_SCHEMA = 'b01-13-observer-r1'
CASE_IDS = ('P01_BASIC_AUTOMATIC_HEAVY','P02_SHOTGUN_RAIL_ROCKET','P03_BEAM','P04_ROTARY_SAW',
            'P05_EMP_AREA','P06_DYNAMIC_OBSTRUCTIONS','P07_LPM_RPM_GROUPS',
            'P08_GARAGE_FIVE_ZONES','P09_GARAGE_ROTATE_055M','P10_GARAGE_FAILED_TRANSACTION',
            'P11_MECHANICAL_ENVELOPES','P12_LOANER_POST_WIN_CONTINUATION',
            'P13_WIN_DEFERRED_REBUILD','P14_PLAYER_TWO_REAL_FIGHTS',
            'P15_PLAYER_ZIP_BOOT','P16_SAVE_LOAD_COLD','P17_TURRET_V3_P1',
            'P18_SAW_DEFAULT_MOUNT_AND_OVERFLOW','P19_EMP_DETACH_DIRECT_GUARD',
            'P20_GARAGE_PHYSICAL_ZONE_COMMIT','P21_SAVE_ONE_OWNER_COLD_UNITY')
P0 = tuple(x for x in CASE_IDS if x != 'P17_TURRET_V3_P1')
STATUS = ('PASS','FAIL','BLOCKED','NOT_RUN')
REASONS = ('NONE','COLLISION','AMMO_COOLDOWN','HEAT_ENERGY','AREA_OR_CONTACT','DYNAMIC',
           'GARAGE_ZONE','INVALID_INSTALL_MUTATED','FOOTPRINT','SOFTLOCK','REWARD',
           'BUILD','SAVE','MODEL','INTEGRATION_MISSING','OTHER')
RECOVERY = ('NOT_EVALUATED','UNRESOLVED','CONTINUE_VERIFIED','RUN_END_VERIFIED')
SHA = re.compile(r'^[a-f0-9]{40}$')
ALLOWED_SUFFIXES = ('.png','.jpg','.jpeg','.log','.txt','.xml','.json','.mp4','.webm','.zip')
W27_IDS = tuple('PT%02d'%i for i in range(1,9))
W27_ISSUES = ('NONE','DOCK','CLEARANCE','LOCOMOTION','COLLIDER','UI','SAVE','LOANER','SOFTLOCK','OTHER')

class Refused(Exception): pass

def ensure(ok: bool, code: str):
    if not ok: raise Refused(code)

def regular(file: Path, code='NOT_REGULAR_FILE'):
    ensure(not file.is_symlink(), 'SYMLINK_'+code)
    try: mode=file.lstat().st_mode
    except FileNotFoundError: raise Refused(code)
    ensure(stat.S_ISREG(mode), code)
    return file

def file_hash(file: Path, cap=64*1024*1024):
    regular(file)
    ensure(0 < file.stat().st_size <= cap, 'FILE_SIZE')
    h=hashlib.sha256()
    with file.open('rb') as f:
        while chunk:=f.read(1024*1024): h.update(chunk)
    return {'sha256':h.hexdigest(),'bytes':file.stat().st_size}

def read_json(file: Path):
    file_hash(file, 1024*1024)
    try: return json.loads(file.read_text('utf8'))
    except (ValueError,UnicodeError): raise Refused('JSON_INVALID')

def strict_keys(data, keys, code):
    ensure(type(data) is dict and set(data)==set(keys), code)

def validate_w27(report, game_sha):
    ensure(type(report) is dict, 'W27_DOC')
    ensure(report.get('schema')==W27_SCHEMA,'W27_SCHEMA')
    ensure(report.get('buildSha')==game_sha,'W27_DIFFERENT_GAME_SHA')
    ensure(report.get('source')=='OPERATOR_SELF_REPORTED_NO_INDEPENDENT_AUTHENTICATION','W27_PROVENANCE')
    ensure(report.get('actualParticipantsCertifiedByTool') is False,'W27_FALSE_HUMAN_CLAIM')
    rows=report.get('submittedRows')
    ensure(type(rows) is list and len(rows)==8,'W27_ROWS')
    seen=set(); counts={x:0 for x in STATUS}; p0=0; soft=0; assists=0; timings=[]
    for row in rows:
        strict_keys(row, ('taskId','outcome','issue','severity','seconds','assists'),'W27_ROW_SCHEMA')
        tid=row['taskId']; ensure(tid in W27_IDS and tid not in seen,'W27_TASK');seen.add(tid)
        o=row['outcome'];ensure(o in STATUS,'W27_STATUS');counts[o]+=1
        issue=row['issue']; sev=row['severity'];ensure(issue in W27_ISSUES,'W27_ISSUE');ensure(sev in ('NONE','P2','P1','P0'),'W27_SEVERITY')
        sec=row['seconds']; hints=row['assists']
        ensure(type(sec) is int and 0<=sec<=3600 and type(hints) is int and 0<=hints<=20,'W27_METRICS')
        if issue=='NONE':ensure(sev=='NONE','W27_NONE_SEVERITY')
        else:ensure(sev!='NONE' and o!='NOT_RUN','W27_ISSUE_SEVERITY')
        if o=='FAIL':ensure(issue!='NONE','W27_FAIL_WITHOUT_ISSUE')
        if o=='PASS':ensure(sev!='P0','W27_PASS_WITH_P0')
        if o=='BLOCKED':ensure(sec==0,'W27_BLOCKED_TIMED')
        if o=='NOT_RUN':ensure(sec==0 and hints==0 and issue=='NONE' and sev=='NONE','W27_NOTRUN_MEASURED')
        if o in ('PASS','FAIL'): timings.append(sec); assists+=hints
        if sev=='P0':p0+=1
        if issue=='SOFTLOCK':soft+=1
    ensure(seen==set(W27_IDS),'W27_MISSING_TASK')
    summary=report.get('summary');ensure(type(summary) is dict,'W27_SUMMARY')
    ensure(summary.get('counts')==counts,'W27_SUMMARY_COUNT_MISMATCH')
    ensure(summary.get('p0')==p0,'W27_SUMMARY_P0_MISMATCH')
    ensure(summary.get('assists')==assists,'W27_SUMMARY_ASSISTS_MISMATCH')
    durations=sorted(timings)
    median=(durations[(len(durations)-1)//2] if len(durations)%2 else (durations[len(durations)//2-1]+durations[len(durations)//2])/2) if durations else None
    ensure(summary.get('medianSeconds')==median,'W27_SUMMARY_MEDIAN_MISMATCH')
    complete=counts['BLOCKED']==0 and counts['NOT_RUN']==0
    criteria={'C1':counts['PASS']>=6 if complete else None,'C2':p0==0 and soft==0 if complete else None,'C3':assists<=2 if complete else None,'C4':median is not None and median<=180 if complete else None}
    expected='INCOMPLETE' if not complete else ('CRITERIA_MET' if all(criteria.values()) else 'CRITERIA_NOT_MET')
    ensure(summary.get('gate')==expected and summary.get('criteria')==criteria,'W27_SUMMARY_GATE_MISMATCH')
    return {'counts':counts,'p0':p0,'softlock':soft,'pilot_gate':expected}

def template(game_sha):
    ensure(type(game_sha) is str and SHA.fullmatch(game_sha) is not None,'GAME_COMMIT_SHA')
    return {'schema':'b01-13-w29-acceptance-r2','gameCommit':game_sha,'realHumanParticipants':'UNVERIFIED',
            'softlockDisposition':'NOT_EVALUATED',
            'cases':[{'id':n,'status':'NOT_RUN','reason':'NONE','evidence':None} for n in CASE_IDS]}

def safe_file(root: Path, relative: str):
    ensure(type(relative) is str and len(relative)<180 and relative and '\\' not in relative,'EVIDENCE_PATH')
    parts=Path(relative).parts
    ensure(not Path(relative).is_absolute() and parts and all(p not in ('..','.','') for p in parts),'EVIDENCE_ESCAPE')
    ensure(not relative.startswith('.') and all(not p.startswith('.') for p in parts),'EVIDENCE_HIDDEN')
    ensure(Path(relative).suffix.lower() in ALLOWED_SUFFIXES,'EVIDENCE_FORMAT')
    ensure(root.exists() and root.is_dir() and not root.is_symlink(),'EVIDENCE_ROOT')
    cursor=root
    for part in parts:
        cursor=cursor/part
        ensure(not cursor.is_symlink(),'EVIDENCE_SYMLINK')
    resolved=root.joinpath(*parts)
    ensure(resolved.resolve().is_relative_to(root.resolve()),'EVIDENCE_ESCAPE_RESOLVE')
    return regular(resolved,'EVIDENCE_NOT_REGULAR')

def inspect_unity_zip(file: Path):
    try:
        with zipfile.ZipFile(file,'r') as z:
            members=z.infolist()
            ensure(len(members)<=20000 and len(members)>2,'ZIP_MEMBERS')
            ensure(all(not x.filename.startswith('/') and '\\' not in x.filename and '..' not in Path(x.filename).parts for x in members),'ZIP_TRAVERSAL')
            ensure(sum(x.file_size for x in members)<3*1024*1024*1024,'ZIP_BOMB_DECLARED')
            names=[x.filename.lower() for x in members]
            ensure(any(n.endswith('.exe') for n in names),'ZIP_PLAYER_EXE_MISSING')
            ensure(any(n.endswith('unityplayer.dll') for n in names),'ZIP_UNITY_DLL_MISSING')
            ensure(any('_data/' in n and (n.endswith('boot.config') or n.endswith('globalgamemanagers')) for n in names),'ZIP_UNITY_DATA_MISSING')
            ensure(z.testzip() is None,'ZIP_CORRUPT')
    except (zipfile.BadZipFile,EOFError,ValueError): raise Refused('ZIP_INVALID')
    return 'ARCHIVE_STRUCTURE_PRESENT_NOT_BOOT_PROOF'

def verify_w30_session(receipt: Path, archive: Path, player_log: Path, game_sha: str):
    """Cross-check REAL FILE BYTES with the existing W30 operator-runner receipt.

    This attests consistency, not that Unity ran or gameplay/humans passed.
    """
    doc=read_json(receipt)
    ensure(type(doc) is dict and doc.get('schema')=='b01-13-w30-windows-player-session-r1',
           'W31_SESSION_SCHEMA')
    ensure(doc.get('gameCommit')==game_sha and
           doc.get('gameCommitProvenance')=='OPERATOR_ASSERTED_NOT_EMBEDDED_IN_PLAYER',
           'W31_SESSION_GAME_SHA')
    ensure(type(doc.get('processExitCode')) is int and doc['processExitCode']==0 and
           doc.get('captureStatus')=='PROCESS_EXITED_LOG_PRESENT','W31_SESSION_PROCESS_EXIT')
    ensure(doc.get('unityNativeG1')=='NOT_VERIFIED' and
           doc.get('actualGameplayPassed')=='NOT_VERIFIED' and
           doc.get('humanSessionsIndependentlyVerified')==0 and
           doc.get('playerLaunchG1')=='NOT_INDEPENDENTLY_VERIFIED',
           'W31_SESSION_FALSE_QA_CLAIM')
    log=doc.get('playerLog')
    ensure(type(log) is dict and set(log)=={'sha256','bytes'},'W31_SESSION_LOG_SCHEMA')
    physical_log=file_hash(player_log)
    ensure(log==physical_log,'W31_PLAYER_LOG_MISMATCH')
    try:
        start=datetime.fromisoformat(doc['utcStart'])
        end=datetime.fromisoformat(doc['utcEnd'])
        ensure(start.utcoffset() is not None and end.utcoffset() is not None
               and 0 <= (end-start).total_seconds() <= 86400,'W31_SESSION_TIME')
    except (KeyError,TypeError,ValueError):raise Refused('W31_SESSION_TIME')
    exe_sha=doc.get('exeSha256');exe_size=doc.get('exeBytes')
    ensure(type(exe_sha) is str and len(exe_sha)==64 and
           re.fullmatch('[0-9a-f]{64}',exe_sha) and
           type(exe_size) is int and 0<exe_size<=512*1024*1024,
           'W31_SESSION_EXE_RECORD')
    found=0
    try:
        with zipfile.ZipFile(archive,'r') as z:
            names={x.filename.casefold() for x in z.infolist()}
            for info in z.infolist():
                if not info.filename.lower().endswith('.exe') or info.is_dir():continue
                if info.file_size!=exe_size:continue
                # Corresponding UnityPlayer.dll and Unity Data must be siblings of this EXE.
                part=PurePosixPath(info.filename)
                prefix=str(part.parent)
                prefix='' if prefix=='.' else prefix+'/'
                dll=(prefix+'UnityPlayer.dll').casefold()
                data=(prefix+part.stem+'_Data/').casefold()
                if dll not in names or not any(x in names for x in
                       (data+'boot.config',data+'globalgamemanagers')):continue
                h=hashlib.sha256()
                with z.open(info,'r') as binary:
                    while True:
                        chunk=binary.read(1024*1024)
                        if not chunk:break
                        h.update(chunk)
                if h.hexdigest()==exe_sha:found+=1
    except (zipfile.BadZipFile,EOFError,ValueError,RuntimeError,OSError):
        raise Refused('W31_SESSION_ZIP_READ')
    ensure(found==1,'W31_SESSION_ARCHIVE_EXE_MISMATCH')
    return 'W30_RECEIPT_EXE_AND_LOG_BYTES_MATCH_NOT_INDEPENDENT_UNITY_QA'

def evaluate_w28(body, game_sha, evidence_dir):
    strict_keys(body,('schema','gameCommit','realHumanParticipants','softlockDisposition','cases'),'W28_SCHEMA_KEYS')
    ensure(body['schema']=='b01-13-w29-acceptance-r2','W28_SCHEMA')
    ensure(body['gameCommit']==game_sha,'W28_GAME_MISMATCH')
    ensure(body['realHumanParticipants']=='UNVERIFIED','W28_NO_PARTICIPANT_AUTHORITY')
    ensure(body['softlockDisposition'] in RECOVERY,'W28_SOFTLOCK_DISPOSITION')
    rows=body['cases'];ensure(type(rows) is list and len(rows)==len(CASE_IDS),'W28_ROW_COUNT')
    seen=set();evidence=[];results={};seenfiles=set();w30_crosscheck='NOT_EVALUATED'
    def attach(case_id, role, name, allowed=None, is_zip=False):
        ensure(type(name) is str and name, 'W29_EVIDENCE_ROLE_MISSING')
        ensure(name not in seenfiles, 'W29_EVIDENCE_REUSED')
        f=safe_file(evidence_dir,name)
        if allowed is not None:ensure(f.suffix.lower() in allowed, 'W29_EVIDENCE_ROLE_FORMAT')
        seenfiles.add(name)
        proof=file_hash(f,cap=2*1024*1024*1024 if is_zip else 64*1024*1024)
        if is_zip:
            ensure(f.suffix.lower()=='.zip','W29_PLAYER_ZIP_REQUIRED')
            proof['zipStructure']=inspect_unity_zip(f)
        evidence.append({'case':case_id,'role':role,'file':name,**proof})
    for row in rows:
        strict_keys(row,('id','status','reason','evidence'),'W29_CASE_SCHEMA')
        id=row['id'];ensure(type(id) is str and id in CASE_IDS and id not in seen,'W29_CASE_ID');seen.add(id)
        status=row['status'];reason=row['reason'];fname=row['evidence']
        ensure(status in STATUS and reason in REASONS,'W29_ENUM')
        if status in ('PASS','FAIL'):
            ensure(reason=='NONE' if status=='PASS' else reason!='NONE','W29_REASON_MISMATCH')
            if status=='PASS' and id=='P15_PLAYER_ZIP_BOOT':
                strict_keys(fname,('playerZip','playerLog','sessionReceipt'),'W31_BOOT_SESSION_REQUIRED')
                attach(id,'player_zip',fname['playerZip'],is_zip=True)
                attach(id,'player_log',fname['playerLog'],allowed=('.log','.txt'))
                attach(id,'session_receipt',fname['sessionReceipt'],allowed=('.json',))
                w30_crosscheck=verify_w30_session(
                    safe_file(evidence_dir,fname['sessionReceipt']),
                    safe_file(evidence_dir,fname['playerZip']),
                    safe_file(evidence_dir,fname['playerLog']),game_sha)
                evidence[-1]['crossCheck']=w30_crosscheck
            elif status=='PASS' and id=='P12_LOANER_POST_WIN_CONTINUATION':
                strict_keys(fname,('before','after','playerLog'),'W29_RECOVERY_BUNDLE_REQUIRED')
                attach(id,'before',fname['before'],allowed=('.png','.jpg','.jpeg','.json'))
                attach(id,'after',fname['after'],allowed=('.png','.jpg','.jpeg','.json'))
                attach(id,'player_log',fname['playerLog'],allowed=('.log','.txt'))
                before=evidence[-3];after=evidence[-2]
                ensure(before['sha256']!=after['sha256'],'W29_RECOVERY_IDENTICAL_STATES')
            else:
                ensure(type(fname) is str and fname,'W29_RESULT_WITHOUT_EVIDENCE')
                if id=='P15_PLAYER_ZIP_BOOT' and fname.endswith('.zip'):
                    attach(id,'failed_boot_zip',fname,is_zip=True)
                elif id=='P14_PLAYER_TWO_REAL_FIGHTS':
                    attach(id,'observation',fname,allowed=('.log','.txt','.xml'))
                else:
                    attach(id,'observation',fname)
        else:
            ensure(fname is None and reason in ('NONE','INTEGRATION_MISSING','BUILD','OTHER'),'W29_UNOBSERVED_EVIDENCE')
        results[id]=status
    ensure(seen==set(CASE_IDS),'W28_MISSING_CASE')
    recovery=body['softlockDisposition']
    if results['P12_LOANER_POST_WIN_CONTINUATION']=='PASS':
        ensure(recovery in ('CONTINUE_VERIFIED','RUN_END_VERIFIED'),'W28_PHANTOM_SOFTLOCK_PASS')
    else:ensure(recovery in ('NOT_EVALUATED','UNRESOLVED'),'W28_RECOVERY_WITHOUT_PASS')
    if results['P12_LOANER_POST_WIN_CONTINUATION']=='FAIL':ensure(recovery=='UNRESOLVED','W28_FAILED_SOFTLOCK_MUST_BE_UNRESOLVED')
    mandatory_complete=all(results[x]=='PASS' for x in P0)
    missing=[x for x in P0 if results[x]!='PASS']
    return {'coverage':'ALL_P0_OPERATOR_PASS_WITH_REQUIRED_FILES' if mandatory_complete else 'P0_INCOMPLETE_OR_FAILED',
            'cases':results,'missingOrFailingP0':missing,'softlockDisposition':recovery,
            'evidence':evidence,'gameCommit':game_sha,
            'participantSessionsIndependentlyVerified':0,
            'nativeUnityG1':'NOT_VERIFIED_BY_THIS_TOOL',
            'windowsPlayerBoot':'NOT_INDEPENDENTLY_VERIFIED_BY_THIS_TOOL',
            'sourceEvidence':'PHYSICAL_BYTES_SHA256_ONLY_NOT_GAME_AUTHENTICITY',
            'w29RequiredBootEvidence':'ZIP_PLUS_PLAYER_LOG_PLUS_W30_RECEIPT_OPERATOR_SUBMITTED',
            'w31SessionCrossCheck':w30_crosscheck,
            'w29RecoveryEvidence':'BEFORE_AFTER_PLUS_PLAYER_LOG_OPERATOR_SUBMITTED',
            'productGate':'HOLD_INDEPENDENT_G0_G1_AND_REAL_HUMAN_ACCEPTANCE'}

def make_csv(receipt):
    f=io.StringIO(newline='');w=csv.writer(f,lineterminator='\n')
    w.writerow(['case_id','operator_status','evidence_role','evidence_filename','evidence_sha256','evidence_bytes','game_commit','independent_unity_g1'])
    proofs={id:[] for id in CASE_IDS}
    for item in receipt['evidence']:proofs[item['case']].append(item)
    for id in CASE_IDS:
        for p in proofs[id] or [{}]:
            w.writerow([id,receipt['cases'][id],p.get('role',''),p.get('file',''),p.get('sha256',''),p.get('bytes',''),receipt['gameCommit'],'NOT_VERIFIED'])
    return f.getvalue()

def safe_scratch(directory: Path):
    ensure(directory.is_dir() and not directory.is_symlink(), 'SCRATCH_DIR')
    cursor=directory.resolve()
    for parent in (cursor,*cursor.parents):
        ensure(not (parent/'.git').exists(),'REFUSE_GIT_WORKTREE')
        ensure(not parent.is_symlink(),'SCRATCH_SYMLINK_PARENT')

def write_exclusive(out: Path, content: bytes):
    safe_scratch(out.parent)
    ensure(not out.is_symlink() and not out.exists(),'REFUSE_OVERWRITE')
    fd=os.open(out,os.O_WRONLY|os.O_CREAT|os.O_EXCL|getattr(os,'O_NOFOLLOW',0),0o600)
    try:
        with os.fdopen(fd,'wb') as f:f.write(content);f.flush();os.fsync(f.fileno())
    except Exception:
        try:out.unlink()
        except OSError:pass
        raise

def run(argv=None):
    parser=argparse.ArgumentParser(description='B01-13 W29: actual boot and recovery bundles; still not independent Unity QA')
    parser.add_argument('--game-commit',required=True)
    parser.add_argument('--make-template',metavar='ABSOLUTE_JSON')
    parser.add_argument('--w27-report')
    parser.add_argument('--w28-responses')
    parser.add_argument('--evidence-dir')
    parser.add_argument('--output-dir')
    args=parser.parse_args(argv)
    sha=args.game_commit
    ensure(SHA.fullmatch(sha) is not None,'GAME_COMMIT_SHA')
    if args.make_template:
        dest=Path(args.make_template)
        ensure(dest.is_absolute() and dest.parent.is_dir(),'TEMPLATE_ABSOLUTE_DIR')
        safe_scratch(dest.parent)
        write_exclusive(dest,(json.dumps(template(sha),indent=2,ensure_ascii=False)+'\n').encode())
        print('W29_EMPTY_TEMPLATE_CREATED',dest)
        return
    ensure(all([args.w27_report,args.w28_responses,args.evidence_dir,args.output_dir]),'VERIFY_ARGS')
    inputs=[Path(args.w27_report),Path(args.w28_responses),Path(args.evidence_dir),Path(args.output_dir)]
    ensure(all(p.is_absolute() for p in inputs),'ABSOLUTE_PATH_REQUIRED')
    ensure(inputs[3].is_dir() and not inputs[3].is_symlink(),'OUT_DIR_REQUIRED')
    safe_scratch(inputs[3])
    ensure(not any((inputs[3]/p).exists() for p in ('W29_RECEIPT.json','W29_RECEIPT.csv')),'REFUSE_OVERWRITE')
    ensure(inputs[3].resolve()!=inputs[2].resolve(),'OUTSIDE_EVIDENCE_DIR')
    report=read_json(inputs[0]);w27=validate_w27(report,sha)
    result=evaluate_w28(read_json(inputs[1]),sha,inputs[2]);result['w27Observation']=w27
    result['packageId']=PACKAGE;result['w27SourceSha']=W27_SOURCE_HEAD
    result['w27ReportFileSha256']=file_hash(inputs[0])['sha256']
    result['w28ResponseFileSha256']=file_hash(inputs[1])['sha256']
    payload=(json.dumps(result,indent=2,sort_keys=True)+'\n').encode('utf8')
    csv_data=make_csv(result).encode('utf8')
    write_exclusive(inputs[3]/'W29_RECEIPT.json',payload)
    write_exclusive(inputs[3]/'W29_RECEIPT.csv',csv_data)
    print('W29_PHYSICAL_EVIDENCE_VERIFIED',len(result['evidence']),'P0',result['coverage'],'SOFTLOCK',result['softlockDisposition'], 'PRODUCT',result['productGate'])
    print('W29_RECEIPT_JSON_SHA256',hashlib.sha256(payload).hexdigest())
    print('W29_RECEIPT_CSV_SHA256',hashlib.sha256(csv_data).hexdigest())

if __name__=='__main__':
    try:run()
    except (Refused,OSError) as e:
        print('W29_FAIL_CLOSED',str(e),file=sys.stderr);sys.exit(2)
