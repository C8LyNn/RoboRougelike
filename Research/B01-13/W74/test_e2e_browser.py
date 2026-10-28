#!/usr/bin/env python3
"""Original B01-13 HTML -> REAL headless Chromium CSV -> fail-closed local intake/revoke.

All session records and checkbox states are SYNTHETIC. Zero real humans.
"""
from pathlib import Path
import csv
import hashlib
import io
import json
import shutil
import subprocess
import sys
import tempfile
from playwright.sync_api import sync_playwright
import hpt13_intake as gate

HTML=Path(__file__).resolve().parent.parent/'B01-13_PASS14'/'HPT13-scoring.html'
if not HTML.is_file(): HTML=Path(__file__).resolve().parent.parent/'W58'/'HPT13-scoring.html'  # GitHub checkout
PUBLISH=Path(__file__).resolve().parent
BUILD='7299dcc5fd63ac042a0ffcd97b69d02fb8744649'
ZIP='ec3193bb3ac24398c496ea65ad12aa653321db160a4dc74e8f618e3cc7ad6d1d'
checks=[]
def ok(desc,truth):
    checks.append((desc,bool(truth)))
    if not truth: raise AssertionError(desc)

def init(root,pids=('P01','P02'),mode='SYNTHETIC',src=None):
    m={'schema':gate.SCHEMA,'form_html_git_blob':gate.PINNED_HTML_BLOB,'mode':mode,'build_sha':BUILD,'zip_sha256':ZIP,
       'hq_gate_reference':None,'host_preflight_reference':None,'facilitator_id':None,
       'sessions':[{'pid':p,'sid':'S01','consent_state':None,'consent_reference':None} for p in pids]}
    (root/'intake.json').write_text(json.dumps(m,ensure_ascii=False),encoding='utf-8')

def red(desc,action):
    try:action()
    except gate.GateError:return ok(desc,True)
    raise AssertionError(desc+': invalid evidence unexpectedly ACCEPTED')

def parse(path):
    with path.open(newline='',encoding='utf-8') as f:return list(csv.DictReader(f))

def main():
    ok('true published original HTML Git blob',gate.git_blob(HTML.read_bytes())==gate.PINNED_HTML_BLOB)
    with tempfile.TemporaryDirectory(prefix='hpt13-pass15-') as d:
        root=Path(d)
        init(root)
        with sync_playwright() as pw:
            browser=pw.chromium.launch(headless=True,executable_path=shutil.which('chromium') or '/usr/bin/chromium',args=['--no-sandbox'])
            page=browser.new_page(accept_downloads=True,viewport={'width':1280,'height':720})
            errors=[];requests=[]
            page.on('pageerror',lambda e:errors.append(str(e)))
            page.on('request',lambda q:requests.append(q.url))
            page.set_content(HTML.read_text(encoding='utf-8'))
            def one(pid):
                page.locator('#pid').fill(pid)
                page.locator('#sid').fill('S01')
                t=page.locator('#HPT-T01')
                t.locator('[data-field="outcome"]').select_option('UNAIDED_SUCCESS')
                for field,v in [('duration_seconds','45'),('attempts','1'),('errors','0'),('help_count','0')]:
                    t.locator(f'[data-field="{field}"]').fill(v)
                t.locator('[data-field="comprehension_0_2"]').select_option('2')
                page.locator('#save').click()
                ok(f'browser accepted SYNTHETIC {pid}',page.evaluate('HPT13.saved.length')==5 if pid=='P01' else page.evaluate('HPT13.saved.length')==10)
            one('P01');one('P02')
            with page.expect_download() as info:page.locator('#export').click()
            info.value.save_as(root/'results.csv')
            ok('actual original browser CSV has 10 rows',len(parse(root/'results.csv'))==10)
            ok('actual original browser still no remote requests',not requests and not errors)
            command=[sys.executable,str(PUBLISH/'hpt13_intake.py'),'verify','--root',str(root),'--source-html',str(HTML)]
            cli=subprocess.run(command,text=True,capture_output=True)
            ok('real CLI verify source-to-evidence succeeds',cli.returncode==0 and 'STRUCTURAL_PASS' in cli.stdout)
            record=gate.verify(root,HTML)
            ok('original browser CSV structurally accepted',record['rows']==10 and record['sessions']==2 and record['mode']=='SYNTHETIC')
            original_export=(root/'results.csv').read_bytes()
            page.locator('#pid').fill('P01')
            page.once('dialog',lambda dlg:dlg.accept())
            page.locator('#revoke').click()
            ok('browser memory revoked P01',page.evaluate('HPT13.saved.length')==5)
            ok('confirmed exported CSV not touched by browser (known gap)',(root/'results.csv').read_bytes()==original_export)
            ok('old exported CSV STILL contains P01 (RED baseline)',any(r['pid']=='P01' for r in parse(root/'results.csv')))
            browser.close()
        # Persistent revocation tombstone must make a stale exported CSV immediately invalid.
        (root/'withdrawals.json').write_text(json.dumps({'schema':gate.TOMBSTONES_SCHEMA,'pids':['P01']}),encoding='utf-8')
        red('stale export fails closed once withdrawal is recorded',lambda:gate.verify(root,HTML))
        cli=subprocess.run(command,text=True,capture_output=True)
        ok('real CLI verify rejects stale exported CSV',cli.returncode==2 and 'WITHDRAWN_PARTICIPANT' in cli.stderr)
        revoke_cmd=[sys.executable,str(PUBLISH/'hpt13_intake.py'),'revoke','--root',str(root),'--source-html',str(HTML),'--pid','P01','--confirm-local-export-rewrite']
        cli=subprocess.run(revoke_cmd,text=True,capture_output=True)
        ok('real CLI revoke performs local rewrite',cli.returncode==0 and json.loads(cli.stdout)['removed_rows']==5)
        x={'removed_rows':5,'remaining_rows':len(parse(root/'results.csv'))}
        ok('new intake deletes P01 rows from original CSV',x['removed_rows']==5 and x['remaining_rows']==5)
        ok('other participant P02 is preserved',all(r['pid']=='P02' for r in parse(root/'results.csv')))
        ok('withdrawn session removed from external consent ledger',len(json.loads((root/'intake.json').read_text())['sessions'])==1)
        clean=gate.verify(root,HTML)
        ok('post-rewrite verified with original SHA',clean['sessions']==1 and clean['rows']==5 and clean['withdrawn_tombstones']==1)
        ok('repeated withdrawal idempotent',gate.revoke(root,HTML,'P01',True)['removed_rows']==0)
        ok('rejected rewrite without explicit operator confirmation',not_gate(lambda:gate.revoke(root,HTML,'P02',False)))
        # Replay P01 into exported CSV while keeping active withdrawal tombstone: fail closed.
        contaminated=(root/'results.csv').read_bytes().decode('utf-8')
        with io.StringIO(newline='') as s:
            writer=csv.DictWriter(s,fieldnames=gate.FIELDS,lineterminator='\r\n')
            writer.writeheader();writer.writerows(parse(root/'results.csv'))
            rows=parse(root/'results.csv')
            writer.writerows([{**r,'pid':'P01'} for r in rows])
            contaminated=s.getvalue()
        (root/'results.csv').write_text(contaminated,encoding='utf-8')
        red('reattached withdrawn participant cannot pass',lambda:gate.verify(root,HTML))
        (root/'results.csv').write_bytes(original_export) # original browser export for special negative
        red('rewritten original browser export cannot pass tombstone',lambda:gate.verify(root,HTML))
        gate.revoke(root,HTML,'P01',True)
        ok('repeat cleanup restores valid structural receipt',gate.verify(root,HTML)['rows']==5)
        # Wrong source blob: fail before trusting any participant rows.
        tampered=root/'altered.html';tampered.write_bytes(HTML.read_bytes()+b' ')
        red('tampered actual source HTML fails blob pin',lambda:gate.verify(root,tampered))
        m=json.loads((root/'intake.json').read_text());m['build_sha']='f'*40
        (root/'intake.json').write_text(json.dumps(m),encoding='utf-8')
        red('unmatched game build SHA denied',lambda:gate.verify(root,HTML))
        m['build_sha']=BUILD;m['mode']='HUMAN_APPROVED';m['hq_gate_reference']=None
        (root/'intake.json').write_text(json.dumps(m),encoding='utf-8')
        red('mere browser consent checkbox does not prove external HQ approval',lambda:gate.verify(root,HTML))
        # Real CSV contains SYNTHETIC; relabeling to HUMAN with fake attestations must not pass.
        m['hq_gate_reference']='HQ-ticket-00001';m['host_preflight_reference']='QA-reference-00001';m['facilitator_id']='MOD01'
        for v in m['sessions']:v.update({'consent_state':'APPROVED','consent_reference':'CONSENT-reference-00001'})
        (root/'intake.json').write_text(json.dumps(m),encoding='utf-8')
        red('synthetic original CSV cannot masquerade as HUMAN_APPROVED',lambda:gate.verify(root,HTML))
        # Malformed CSV header must be denied without deleting any records.
        valid_p02=(root/'results.csv').read_bytes()
        (root/'results.csv').write_text('pid,sid\nP02,S01\n',encoding='utf-8')
        red('malformed CSV rejected',lambda:gate.verify(root,HTML))
        # Restore the intact CSV; withdrawal must work even when HQ manifest is invalid.
        (root/'results.csv').write_bytes(valid_p02)
        x=gate.revoke(root,HTML,'P02',True)
        ok('withdrawal independent of stale approval field',x['removed_rows']==5)
        ok('after removing every participant only original header remains',parse(root/'results.csv')==[])
        # no false human approval, no Unity environment.
    print(f'PASS15 B01-13: {len(checks)}/{len(checks)} checks PASS; ACTUAL Chromium + original CSV; SYNTHETIC only, 0 human')
    for i,(label,passed) in enumerate(checks,1):print(f'{i:02d} PASS {label}')

def not_gate(fn):
    try:fn()
    except gate.GateError:return True
    return False

if __name__=='__main__':main()
