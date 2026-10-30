#!/usr/bin/env python3
"""Read-only independent integrity & scope validation of B01-60 W20 local evidence.

Integrity is SHA-256 self-consistency, NOT authenticity/attestation or proof
that a Unity process genuinely executed. Never touches user game data.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import sys
import xml.etree.ElementTree as ET

W20_SHA = 'c75cb6e52a0de11f27b570c34e08d32082af421f'
GAME_SHA = 'd94400d95d13db7f27e1533df05c9292d5380431'
CASE_PREFIX='B01.SecureReattachV2.NativeQA.'
EDIT={CASE_PREFIX+'SecureV2IndependentNativeEditMode.'+name for name in (
 'ExactOwnerRuntimeAndOriginalW06AreLinkableByUnity',
 'RealOriginalW06LedgerClearFailsClosedOnMissingIdBeforeCallbacks')}
PLAY={CASE_PREFIX+'SecureV2IndependentNativePlayMode.'+name+'('+str(i)+','+str(route)+')'
      for name in ('ColliderOnLayer31RefusesWithoutAnyBodyOrLedgerMutation',
                   'MissingConsumedIdRefusesWithoutRealRigidbodyPartialCommit')
      for i in (0,1) for route in (0,1,2)} | {
 CASE_PREFIX+'SecureV2IndependentNativePlayMode.ForcedMidCommitExceptionRestoresRealRigidbodyAndConsumedId',
 CASE_PREFIX+'SecureV2IndependentNativePlayMode.RollbackFailureIsQuarantinedRatherThanSilentlyAccepted'}

class Reject(ValueError):
    pass

def require(cond, msg):
    if not cond: raise Reject(msg)

def read_json(path):
    try: return json.loads(path.read_text('utf-8-sig'))
    except (ValueError, UnicodeError, OSError) as e: raise Reject('Invalid JSON: '+str(path.name)) from e

def no_links_tree(e):
    require(e.is_dir() and not e.is_symlink(), 'Evidence directory missing or linked')
    for parent in (e, *e.parents):
        require(not parent.is_symlink(), 'Linked evidence ancestor: '+str(parent))
    found=set()
    for root, dirs, files in os.walk(e, topdown=True, followlinks=False):
        for name in dirs+files:
            q=Path(root)/name
            require(not q.is_symlink(), 'Symlink inside evidence: '+name)
        for name in files:
            q=Path(root)/name
            require(q.is_file(), 'Non-regular evidence member')
            found.add(q.relative_to(e).as_posix())
    return found

def verify_xml(path, expected):
    try: root=ET.parse(path).getroot()
    except (ET.ParseError, OSError) as exc:raise Reject('Malformed or missing NUnit XML') from exc
    require(root.tag=='test-run' and root.get('result')=='Passed', 'NUnit root not Passed')
    try: passed=int(root.get('passed','-1')); failed=int(root.get('failed','-1'))
    except ValueError as exc:raise Reject('Non-numeric NUnit case counts') from exc
    require(passed>=len(expected) and failed==0,'NUnit suite counts wrong')
    got={}
    for case in root.iter('test-case'):
        name=case.get('fullname','').replace(' ','')
        if name in expected:
            got[name]=got.get(name,0)+1
            require(case.get('result')=='Passed','Required NUnit case not passed')
    require(set(got)==expected and all(x==1 for x in got.values()),'Missing or duplicate NUnit case identities')

def audit(evidence):
    e=Path(evidence).absolute()
    found=no_links_tree(e)
    manifest=e/'sha256-manifest.json'
    digest=e/'sha256-manifest.json.sha256'
    require({'operator-report.json','sha256-manifest.json','sha256-manifest.json.sha256'}<=found,
            'Missing operator report/hash inventory')
    expected=digest.read_text('ascii').strip().lower()
    require(len(expected)==64 and all(c in '0123456789abcdef' for c in expected),'Manifest digest invalid')
    actual=hashlib.sha256(manifest.read_bytes()).hexdigest()
    require(actual==expected,'Manifest digest mismatch')
    data=read_json(manifest)
    require(data.get('source_sha')==W20_SHA and data.get('unsigned') is True,'Wrong W20 source or claimed signature')
    entries=data.get('files')
    require(isinstance(entries,list) and entries,'Empty/invalid manifest')
    accounted=set()
    for ent in entries:
        require(isinstance(ent,dict),'Invalid member')
        name=ent.get('path')
        require(isinstance(name,str) and name and '\\' not in name and '\x00' not in name,'Bad member path')
        p=PurePosixPath(name)
        require(not p.is_absolute() and p.as_posix()==name and all(x not in ('','.','..') for x in name.split('/')),
                'Traversal/noncanonical evidence path')
        require(name not in accounted and name not in ('sha256-manifest.json','sha256-manifest.json.sha256'),
                'Duplicate/self-hashing member')
        accounted.add(name)
        q=e.joinpath(*p.parts)
        require(q.is_file() and not q.is_symlink(),'Missing/nonregular evidence file')
        digest=ent.get('sha256')
        require(isinstance(digest,str) and len(digest)==64 and all(c in '0123456789abcdef' for c in digest),
                'Invalid per-file SHA')
        require(type(ent.get('bytes')) is int and ent['bytes']>=0 and q.stat().st_size==ent['bytes'],
                'File-size mismatch')
        require(hashlib.sha256(q.read_bytes()).hexdigest()==digest,'Evidence member digest mismatch')
    require(accounted==found-{'sha256-manifest.json','sha256-manifest.json.sha256'},
            'Uninventoried evidence file or missing manifest member')
    report=read_json(e/'operator-report.json')
    require(report.get('source_sha')==W20_SHA and report.get('merged') is False
            and report.get('uploaded') is False,'Operator source/merge/upload status inconsistent')
    require(report.get('canonical_SaveLoad')=='HOLD' and report.get('selected_R4')==
            'SELECTED_647_SECURITY_P0_G0_HOLD','Unsafe source gate promotion')
    require('v2-source-preflight/receipt.json' in accounted,'Original V2 preflight receipt missing')
    require(report.get('source_preflight')=='PASS_16_AUTHORED_CASES_NOT_EXECUTED',
            'Original V2 source-only preflight not accepted')
    consent=report.get('operator_consent')
    require(type(consent) is bool,'Operator consent field invalid')
    claimed=report.get('native_g1')
    native_names={'original-V2-native-only/'+n for n in (
        'EditMode.xml','PlayMode.xml','EditMode-Editor.log','PlayMode-Editor.log','native-verdict.json')}
    if claimed=='NOT_RUN':
        require(report.get('mode')=='PREFLIGHT' and report.get('result')=='PREFLIGHT_PASS_NATIVE_NOT_RUN'
                and consent is False,'False source-only success')
        require(not (native_names&accounted), 'Native artifacts present despite NOT_RUN')
        return 'SOURCE_ONLY_PASS_NATIVE_NOT_RUN'
    require(claimed=='PASS_V2_ONLY_R4_STILL_NOT_RUN', 'Unknown native receipt scope')
    require(consent is True and report.get('mode')=='OPERATOR_NATIVE_V2_ONLY'
            and report.get('native_process_exit')==0 and
            report.get('result')=='REAL_V2_ONLY_PASS_NOT_CANONICAL_SAVE',
            'Native opt-in/exit/mode inconsistent')
    require(native_names<=accounted,'Missing original V2 NUnit and Editor.log evidence')
    v=read_json(e/'original-V2-native-only/native-verdict.json')
    require(v.get('G1')=='PASS' and v.get('native_unity')=='EXECUTED' and
            v.get('owner_sha')==GAME_SHA,'Original native V2 verdict/source invalid')
    require(set(v.get('results',{}))=={'EditMode','PlayMode'},'Original native suite set differs')
    for mode,names in (('EditMode',EDIT),('PlayMode',PLAY)):
        verify_xml(e/'original-V2-native-only'/f'{mode}.xml',names)
        log=(e/'original-V2-native-only'/f'{mode}-Editor.log').read_text('utf-8',errors='replace')
        require('6000.3.14f1' in log,'Unity Editor.log version absent')
    return 'STRUCTURALLY_CONSISTENT_V2_ONLY_NOT_NATIVE_ATTESTATION'

def main():
    a=argparse.ArgumentParser()
    a.add_argument('--evidence',required=True)
    args=a.parse_args()
    try:
        value=audit(args.evidence)
        print('B0160_W21_EVIDENCE='+value)
        print('B0160_W21_RELEASE=HOLD R5_690_AND_API_691_NOT_EXECUTED SOURCE_G0_SEPARATE')
        return 0
    except (Reject,OSError,UnicodeError) as exc:
        print('B0160_W21_EVIDENCE_REJECTED='+str(exc),file=sys.stderr)
        return 2
if __name__=='__main__':sys.exit(main())
