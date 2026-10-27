"""B01-13 PASS11 — real Chromium UI/form and script-audit checks; synthetic fixtures only.
No humans, real game runtime, Unity, or performance data are used here.
Run: python test_hpt13.py
"""
from __future__ import annotations
import csv
import io
import json
from pathlib import Path
import shutil
from playwright.sync_api import sync_playwright

PATH = Path(__file__).with_name('HPT13-scoring.html')
CHECKS = []

def check(cond, description):
    CHECKS.append((description, bool(cond)))
    assert cond, description

def synthetic_record(page):
    return page.evaluate("""() => {
      const r=HPT13.gather(); r.pid='P01'; r.sid='S01';
      r.tasks[0].outcome='UNAIDED_SUCCESS'; r.tasks[0].duration_seconds='81';
      r.tasks[0].attempts='2'; r.tasks[0].errors='1';
      r.tasks[0].help_count='0'; r.tasks[0].comprehension_0_2='2';
      return r;
    }""")

with sync_playwright() as p:
    browser = p.chromium.launch(executable_path=(shutil.which('chromium') or shutil.which('chromium-browser') or '/usr/bin/chromium'), headless=True, args=['--no-sandbox'])
    page = browser.new_page(viewport={'width':1280,'height':720}, accept_downloads=True)
    errors = []
    requests=[]
    page.on('pageerror', lambda e: errors.append(str(e)))
    page.on('request', lambda r: requests.append(r.url))
    page.set_content(PATH.read_text(encoding='utf-8'), wait_until='load')
    check(page.title()=='B01-13 | Human Playtest Scoring — W58 PASS 11', 'A1 browser loads offline app')
    check(page.locator('article.task').count()==5, 'A2 exactly 5 task cards')
    check(page.evaluate('HPT13.saved.length')==0, 'A3 empty baseline, no invented participant data')
    check(page.locator('#mode').input_value()=='SYNTHETIC', 'A4 synthetic-only default')
    check(not errors, 'A5 no JavaScript runtime errors at load')
    check(not requests or all(x.startswith('file:') for x in requests), 'A6 no outgoing HTTP requests')
    record=synthetic_record(page)
    check(page.evaluate('(r) => HPT13.validateRecord(r)',record)==[], 'A7 valid synthetic row passes schema')
    bad=json.loads(json.dumps(record));bad['tasks'][0]['help_count']='1'
    check(any('0 podpowiedzi' in x for x in page.evaluate('(r) => HPT13.validateRecord(r)',bad)), 'A8 cannot mark prompted success as unaided')
    bad=json.loads(json.dumps(record));bad['tasks'][0]['outcome']='ASSISTED_SUCCESS'
    check(any('co najmniej 1' in x for x in page.evaluate('(r) => HPT13.validateRecord(r)',bad)), 'A9 assisted success must have help')
    bad=json.loads(json.dumps(record));bad['tasks'][0]['outcome']='PRODUCT_BLOCKED';bad['tasks'][0]['comprehension_0_2']='1';bad['tasks'][0]['incident']=''
    check(any('PRODUCT_BLOCKED' in x for x in page.evaluate('(r) => HPT13.validateRecord(r)',bad)), 'A10 known product blockers require documented cause')
    bad=json.loads(json.dumps(record));bad['tasks'][0]['duration_seconds']='241'
    check(any('limit' in x for x in page.evaluate('(r) => HPT13.validateRecord(r)',bad)), 'A11 task cap enforced')
    bad=json.loads(json.dumps(record));bad['buildsha']='not_sha'
    check(any('Build SHA' in x for x in page.evaluate('(r) => HPT13.validateRecord(r)',bad)), 'A12 SHA pin requires exact 40 hex')
    bad=json.loads(json.dumps(record));bad['mode']='HUMAN_APPROVED'
    check(any('zgody HQ' in x for x in page.evaluate('(r) => HPT13.validateRecord(r)',bad)), 'A13 human gate fails without HQ+consent+preflight')
    bad['hq']=True;bad['consent']=True;bad['preflight']=True
    check(page.evaluate('(r) => HPT13.validateRecord(r)',bad)==[], 'A14 gate logic accepts only fully acknowledged human form (synthetic logical test, no save)')
    bad=json.loads(json.dumps(record));bad['tasks'][0]['outcome']='WITHDRAWN';bad['tasks'][0]['comprehension_0_2']='';bad['tasks'][1]['outcome']='FAIL';bad['tasks'][1]['duration_seconds']='5';bad['tasks'][1]['attempts']='1';bad['tasks'][1]['errors']='1';bad['tasks'][1]['help_count']='0';bad['tasks'][1]['comprehension_0_2']='0'
    check(any('po wycofaniu' in x for x in page.evaluate('(r) => HPT13.validateRecord(r)',bad)), 'B1 withdrawal stops remaining tasks')
    bad=json.loads(json.dumps(record));bad['tasks'][1]['duration_seconds']='17'
    check(any('NOT_RUN' in x for x in page.evaluate('(r) => HPT13.validateRecord(r)',bad)), 'B2 NOT_RUN rejects misleading metrics')
    bad=json.loads(json.dumps(record));bad['tasks'][0]['fixture_id']='../../private'
    check(any('fixture' in x for x in page.evaluate('(r) => HPT13.validateRecord(r)',bad)), 'B3 fixture ID path traversal rejected')
    bad=json.loads(json.dumps(record));bad['tasks']=bad['tasks'][:4]
    check(any('pięciu' in x for x in page.evaluate('(r) => HPT13.validateRecord(r)',bad)), 'B4 truncated task list rejected')
    bad=json.loads(json.dumps(record));bad['pid']='Jan Kowalski'
    check(any('pseudonim' in x for x in page.evaluate('(r) => HPT13.validateRecord(r)',bad)), 'B5 pseudonym validator rejects real-name form')
    bad=json.loads(json.dumps(record));bad['tasks'][0]['note']='=HYPERLINK("https://phishing.test","run")'
    csv_text=page.evaluate('(r) => HPT13.csv([{mode:r.mode,pid:r.pid,sid:r.sid,buildsha:r.buildsha,zipsha:r.zipsha,resolution:r.resolution,...r.tasks[0]}])',bad)
    parsed=list(csv.DictReader(io.StringIO(csv_text)))
    check(parsed[0]['note'].startswith("'=HYPERLINK"), 'B6 CSV spreadsheet formula injection neutralized')
    check(parsed[0]['task_id']=='HPT-T01', 'B7 CSV quotes/newlines parse and columns align')
    prompts=page.evaluate('HPT13.TASKS.map(t=>t.prompt)')
    leading=['LPM','PPM','D03','D05','OFF','Assist','Full','brak broni','brak napędu','przycisk A','przycisk B']
    check(all(not any(token.lower() in s.lower() for token in leading) for s in prompts), 'B8 scripts do not reveal specific solutions or defect IDs')
    check(len(set(page.evaluate('HPT13.TASKS.map(t=>t.fixture)')))==5, 'B9 all five tasks have independent reproducible fixture IDs')
    check(all(len(v)>35 for v in prompts), 'B10 all tasks have non-empty full participant scripts')
    check('localStorage' not in PATH.read_text().replace('LocalStorage',''), 'B11 no localStorage persistence in executable JS')
    page.locator('#pid').fill('P01');page.locator('#sid').fill('S01')
    a=page.locator('#HPT-T01')
    a.locator('[data-field="outcome"]').select_option('UNAIDED_SUCCESS')
    a.locator('[data-field="duration_seconds"]').fill('81')
    a.locator('[data-field="attempts"]').fill('2')
    a.locator('[data-field="errors"]').fill('1')
    a.locator('[data-field="help_count"]').fill('0')
    a.locator('[data-field="comprehension_0_2"]').select_option('2')
    page.locator('#validate').click()
    check('FORM VALIDATION PASS' in page.locator('#feedback').inner_text(), 'A15 real button performs form validation')
    page.locator('#save').click()
    check(page.evaluate('HPT13.saved.length')==5, 'A16 real button appends one 5-task session')
    check(page.locator('#summary tbody tr').count()==5, 'A17 summary has all five task rows')
    check(page.locator('#summary tbody tr').first.locator('td').nth(2).inner_text()=='0', 'A18 synthetic rows never counted as human successes')
    page.locator('#save').click()
    check('duplikat' in page.locator('#feedback').inner_text(), 'A19 duplicate session blocked')
    with page.expect_download() as info: page.locator('#export').click()
    download=info.value
    export=Path(__file__).with_name('QA_SYNTHETIC_export.csv')
    download.save_as(export)
    rows=list(csv.DictReader(export.open(encoding='utf-8',newline='')))
    check(len(rows)==5 and all(x['mode']=='SYNTHETIC' for x in rows), 'A20 actual CSV export contains 5 explicitly synthetic rows')
    page.locator('#clear').click()
    check(page.evaluate('HPT13.saved.length')==0, 'A21 clear removes all in-memory rows')
    page.set_viewport_size({'width':390,'height':844})
    check(page.evaluate('document.documentElement.scrollWidth <= innerWidth + 1'), 'B12 mobile 390px has no page-level horizontal overflow')
    check(not errors, 'B13 no JS runtime errors during dynamic interactions')
    browser.close()

print('B01-13 PASS11 OFFLINE UI / SYNTHETIC VALIDATION RECEIPT')
for name, ok in CHECKS: print(('PASS ' if ok else 'FAIL ')+name)
print(f'RESULT: {sum(v for _,v in CHECKS)}/{len(CHECKS)} PASS')
print('HUMAN_PLAYTEST: NOT_RUN | UNITY: NOT_RUN | REAL_GUI_GAME: NOT_RUN')
