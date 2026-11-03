#!/usr/bin/env python3
"""B01-13 W30 opt-in REAL Windows Player interactive session receipt.
Does not certify gameplay, participants, safe Save isolation or Unity G1.
"""
from __future__ import annotations
import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import stat
import subprocess
import sys

PACKAGE = 'B01-13-W30-INTERACTIVE-WINDOWS-PLAYER-SESSION-001/R1'
SHA40 = re.compile(r'^[a-f0-9]{40}$')
SHA64 = re.compile(r'^[a-f0-9]{64}$')
class Refused(Exception): pass

def require(condition, code):
    if not condition: raise Refused(code)

def regular_file(p: Path, code='NOT_REGULAR'):
    require(not p.is_symlink(), 'SYMLINK_'+code)
    try: mode=p.lstat().st_mode
    except OSError: raise Refused(code)
    require(stat.S_ISREG(mode),'NOT_REGULAR_'+code)
    return p

def hash_file(p: Path):
    regular_file(p)
    h=hashlib.sha256()
    with p.open('rb') as f:
        while True:
            chunk=f.read(1024*1024)
            if not chunk: break
            h.update(chunk)
    return {'sha256':h.hexdigest(),'bytes':p.stat().st_size}

def inside(a: Path,b: Path):
    return a==b or b in a.parents

def preflight(exe: Path, out: Path, game_commit: str, expected_exe_sha: str):
    require(exe.is_absolute() and out.is_absolute(),'ABSOLUTE_PATHS_REQUIRED')
    require(type(game_commit) is str and SHA40.fullmatch(game_commit) is not None,'GAME_COMMIT_FORMAT')
    require(type(expected_exe_sha) is str and SHA64.fullmatch(expected_exe_sha) is not None,'EXE_HASH_FORMAT')
    require(exe.suffix.lower()=='.exe','WINDOWS_EXE_REQUIRED')
    regular_file(exe,'PLAYER_EXE')
    dll=exe.parent/'UnityPlayer.dll'; regular_file(dll,'UNITYPLAYER_DLL')
    data=exe.parent/(exe.stem+'_Data')
    require(data.is_dir() and not data.is_symlink(),'PLAYER_DATA_DIR')
    config=data/'boot.config'; managers=data/'globalgamemanagers'
    require(any(x.is_file() and not x.is_symlink() for x in (config,managers)),'PLAYER_DATA_FILES')
    require(out.is_dir() and not out.is_symlink(),'OUTPUT_DIR')
    exe_parent=exe.parent.resolve(); dst=out.resolve()
    require(not inside(dst,exe_parent) and not inside(exe_parent,dst),'OUTPUT_MUST_BE_SEPARATE_FROM_GAME')
    for p in (dst,*dst.parents):
        require(not (p/'.git').exists(),'OUTPUT_IN_GIT_WORKTREE')
    for name in ('W30_Player.log','W30_SESSION_RECEIPT.json'):
        require(not (out/name).exists() and not (out/name).is_symlink(),'REFUSE_EXISTING_OUTPUT')
    h=hash_file(exe)
    require(h['sha256']==expected_exe_sha,'EXE_HASH_MISMATCH')
    return h

def capture(exe:Path, out:Path, game_commit:str, expected_exe_sha:str,
            *, interactive:bool, save_risk:bool, platform:str|None=None, runner=None):
    meta=preflight(exe,out,game_commit,expected_exe_sha)
    require(interactive and save_risk,'MANUAL_ACK_BOTH_REQUIRED')
    require((platform if platform is not None else os.name)=='nt','REAL_WINDOWS_REQUIRED')
    log=out/'W30_Player.log'; receipt=out/'W30_SESSION_RECEIPT.json'
    runner=runner or subprocess.run
    before=datetime.now(timezone.utc).isoformat()
    try:
        completed=runner([str(exe),'-logFile',str(log),'-screen-fullscreen','0'],cwd=str(exe.parent),check=False)
        code=int(completed.returncode)
    except OSError as e:
        raise Refused('PLAYER_PROCESS_START_FAILED:'+type(e).__name__)
    after=datetime.now(timezone.utc).isoformat()
    log_meta=None
    if log.exists():
        regular_file(log,'PLAYER_LOG')
        if log.stat().st_size: log_meta=hash_file(log)
    result={
        'schema':'b01-13-w30-windows-player-session-r1','package':PACKAGE,
        'gameCommit':game_commit,'gameCommitProvenance':'OPERATOR_ASSERTED_NOT_EMBEDDED_IN_PLAYER',
        'exeSha256':meta['sha256'],'exeBytes':meta['bytes'],
        'utcStart':before,'utcEnd':after,'processExitCode':code,
        'playerLog':log_meta,
        'captureStatus':'PROCESS_EXITED_LOG_PRESENT' if log_meta else 'PROCESS_EXITED_LOG_MISSING',
        'humanSessionsIndependentlyVerified':0,
        'sessionMethod':'INTERACTIVE_PLAYER_LAUNCH_NOT_OBSERVED_BY_INDEPENDENT_QA',
        'actualGameplayPassed':'NOT_VERIFIED',
        'unityNativeG1':'NOT_VERIFIED',
        'playerLaunchG1':'NOT_INDEPENDENTLY_VERIFIED',
        'saveIsolation':'NOT_GUARANTEED_USER_ACKNOWLEDGED_RISK',
        'manualPlaytest':'REQUIRES_SEPARATE_OPERATOR_OBSERVATIONS'
    }
    payload=(json.dumps(result,sort_keys=True,indent=2)+'\n').encode('utf8')
    fd=os.open(receipt,os.O_WRONLY|os.O_CREAT|os.O_EXCL|getattr(os,'O_NOFOLLOW',0),0o600)
    with os.fdopen(fd,'wb') as f:
        f.write(payload);f.flush();os.fsync(f.fileno())
    return result

def main(argv=None):
    pa=argparse.ArgumentParser(description='Opt-in real Windows Player launch; NOT human or Unity G1 certification.')
    pa.add_argument('--exe',required=True,type=Path)
    pa.add_argument('--output-dir',required=True,type=Path)
    pa.add_argument('--game-commit',required=True)
    pa.add_argument('--expected-exe-sha256',required=True)
    pa.add_argument('--check-only',action='store_true')
    pa.add_argument('--interactive',action='store_true')
    pa.add_argument('--ack-save-risk',action='store_true')
    a=pa.parse_args(argv)
    meta=preflight(a.exe,a.output_dir,a.game_commit,a.expected_exe_sha256)
    if a.check_only:
        print('W30_PREFLIGHT_SOURCE_STRUCTURE_PASS EXE_SHA256',meta['sha256'], 'REAL_PLAYER_NOT_RUN')
        return 0
    result=capture(a.exe,a.output_dir,a.game_commit,a.expected_exe_sha256,interactive=a.interactive,save_risk=a.ack_save_risk)
    print('W30_ACTUAL_WINDOWS_PROCESS_EXIT',result['processExitCode'],result['captureStatus'],
          'GAMEPLAY_NOT_VERIFIED UNITY_G1_NOT_VERIFIED HUMANS_NOT_VERIFIED')
    return 0 if result['processExitCode']==0 and result['playerLog'] else 2

if __name__=='__main__':
    try:sys.exit(main())
    except (Refused,OSError,ValueError) as e:
        print('W30_FAIL_CLOSED',str(e),file=sys.stderr);sys.exit(2)
