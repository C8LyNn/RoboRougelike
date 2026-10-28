#!/usr/bin/env python3
"""GBC01 Python receiver tests, original JSON exported by real Chromium in test_gbc01_browser.py."""
from __future__ import annotations
import copy, json, tempfile
from pathlib import Path
from hpt16_intake import verify,revoke,BadReceipt, original_protocol

ROOT=Path(__file__).resolve().parent
proto=ROOT/'GBC01-protocol.json'
source=json.loads((ROOT/'QA_SYNTHETIC_GBC01.json').read_text(encoding='utf-8'))
N=0

def check(name,yes):
    global N
    if not yes:raise AssertionError(name)
    N+=1

def target(root,x):
    path=root/f"GBC01_{x['mode']}_{x['pid']}_{x['sid']}.json"
    path.write_text(json.dumps(x,ensure_ascii=False),encoding='utf-8')
    return path

def negative(name,edit):
    with tempfile.TemporaryDirectory() as d:
        root=Path(d);doc=copy.deepcopy(source);edit(doc);target(root,doc)
        try:verify(root,proto)
        except (BadReceipt,ValueError):check(name,True)
        else:raise AssertionError('Fail-open: '+name)

check('source schema matches',original_protocol(proto)['schema']=='B01-13/PASS16/GBC01/protocol-v1')
with tempfile.TemporaryDirectory() as d:
    root=Path(d);target(root,source)
    x=verify(root,proto)
    check('first synthetic export structurally verified',x['sessions']==1 and x['mode']=='SYNTHETIC')
    second=copy.deepcopy(source);second['pid']='P02';second['sid']='S02';target(root,second)
    check('two participants / six tasks valid',verify(root,proto)['tasks']==6)
    repeat=target(root,source);check('file replay has single identity by filename',verify(root,proto)['sessions']==2)
    try:revoke(root,'P01',False)
    except BadReceipt:check('unapproved withdrawal refuses deletion',True)
    else:raise AssertionError('invalid withdrawal did not fail')
    z=revoke(root,'P01',True)
    check('P01 was deleted locally',z['deleted_files']==1 and not repeat.exists())
    check('P02 retained',verify(root,proto)['sessions']==1 and (root/'withdrawn.json').exists())
    check('repeat withdrawal idempotent',revoke(root,'P01',True)['deleted_files']==0)
    target(root,source)
    try:verify(root,proto)
    except BadReceipt:check('replay of P01 old export fails closed after withdrawal',True)
    else:raise AssertionError('replay of withdrawn data accepted')
    target(root,second)
    revoke(root,'P01',True);revoke(root,'P02',True)
    check('all withdrawn yields no active sessions',verify(root,proto)['sessions']==0)

negative('wrong source pin',lambda x:x['source'].__setitem__('codex_head','a'*40))
negative('wrong protocol',lambda x:x.__setitem__('protocol_schema','bad'))
negative('wrong build sha',lambda x:x.__setitem__('buildsha','bad'))
negative('wrong ZIP sha',lambda x:x.__setitem__('zipsha','bad'))
negative('missing one task',lambda x:x['tasks'].pop())
negative('swapped task order',lambda x:x['tasks'].reverse())
negative('zero elapsed time',lambda x:x['tasks'][0].__setitem__('elapsed_ms',0))
negative('negative help count',lambda x:x['tasks'][1].__setitem__('help_count',-1))
negative('wrong unaided hint count',lambda x:x['tasks'][0].__setitem__('help_count',1))
negative('wrong assisted count',lambda x:x['tasks'][1].__setitem__('help_count',0))
negative('missing blocker note',lambda x:x['tasks'][2].__setitem__('comment',''))
negative('unapproved mode lacks witnesses',lambda x:x.__setitem__('mode','HUMAN_APPROVED'))
negative('task ID unexpected',lambda x:x['tasks'][0].__setitem__('task_id','B02'))
negative('oversized comment',lambda x:x['tasks'][0].__setitem__('comment','x'*501))
negative('invalid configured time',lambda x:x['tasks'][0].__setitem__('configured_minutes',999))
negative('false operator attestation',lambda x:x['consent_witness'].__setitem__('operator_attestation',False))
with tempfile.TemporaryDirectory() as d:
    root=Path(d);target(root,source)
    other=copy.deepcopy(source);other['pid']='P02';other['sid']='S02';other['buildsha']='f'*40;target(root,other)
    try:verify(root,proto)
    except BadReceipt:check('mixed exact build source rejected',True)
    else:raise AssertionError('mixed builds allowed')
with tempfile.TemporaryDirectory() as d:
    root=Path(d);target(root,source)
    other=copy.deepcopy(source);other['pid']='P02';other['sid']='S02';other['mode']='HUMAN_APPROVED';other['consent_witness'].update({'gate':'HQ_12345','host':'HOST12345','consent':'CONSENT123','facilitator':'FAC001'});target(root,other)
    try:verify(root,proto)
    except BadReceipt:check('mixed human/synthetic mode rejected',True)
    else:raise AssertionError('mixed modes allowed')
print(f'ORIGINAL_BROWSER_JSON_INTAKE={N}/{N} PASS')
