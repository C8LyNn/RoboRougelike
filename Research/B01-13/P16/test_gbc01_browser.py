#!/usr/bin/env python3
"""B01-13 original Chromium / synthetic-only QA for HPT16-GBC01 standalone operator console.
No real participants, game execution, consent claims or Unity.
"""
from __future__ import annotations
import hashlib
import json
import re
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT=Path(__file__).resolve().parent
HTML=ROOT/'HPT16-GBC01.html'
PROTOCOL=ROOT/'GBC01-protocol.json'
PASS=[]

def check(name, condition):
    if not condition: raise AssertionError('RED '+name)
    PASS.append(name)

def reset(page):
    page.set_content(HTML.read_text(encoding='utf-8'),wait_until='load')

def fill_start(page,*,mode='SYNTHETIC',preflight=True):
    page.locator('#mode').select_option(mode)
    page.locator('#build').fill('a'*40)
    page.locator('#zip').fill('b'*64)
    if preflight: page.locator('#preflight').check()

def enter(page):page.locator('#begin').click()

def task(page, outcome='UNAIDED_SUCCESS',help_count=0,comment='',expect_next=True):
    page.locator('#startTask').click()
    page.wait_for_timeout(140)
    page.locator('#stopTask').click()
    page.locator('#outcome').select_option(outcome)
    page.locator('#help').fill(str(help_count))
    page.locator('#comment').fill(comment)
    page.locator('#next').click()
    if expect_next: check('advance '+str(len(PASS)),page.locator('#feedback').inner_text().startswith('Następne zadanie') or page.locator('#feedback').inner_text().startswith('Trzy wyniki'))

with sync_playwright() as p:
    browser=p.chromium.launch(headless=True,executable_path='/usr/bin/chromium',args=['--no-sandbox'])
    context=browser.new_context(accept_downloads=True,viewport={'width':1280,'height':720})
    page=context.new_page()
    errors=[];page.on('pageerror',lambda e:errors.append(str(e)))
    reset(page)
    proto=json.loads(PROTOCOL.read_text(encoding='utf-8'))
    embedded=page.evaluate('window.HPT16.protocol')
    check('protocol JSON equals original embedded single-player source',embedded==proto)
    check('exact three source-linked tasks',[(x['id'],x['minutes']) for x in embedded['scenario']['tasks']]==[('G01',3),('B02',6),('C03',6)])
    check('default synthetic, no session',page.locator('#mode').input_value()=='SYNTHETIC' and page.evaluate('window.HPT16.session===null'))
    enter(page);check('no valid SHA rejects start',page.evaluate('window.HPT16.session===null') and 'SHA' in page.locator('#feedback').inner_text())
    fill_start(page,preflight=False);enter(page);check('missing preflight rejects start',page.evaluate('window.HPT16.session===null') and 'preflight' in page.locator('#feedback').inner_text())
    page.locator('#preflight').check();page.locator('#pid').fill('PII-TEST');enter(page);check('invalid ID rejected',page.evaluate('window.HPT16.session===null'))
    page.locator('#pid').fill('P01');page.locator('#mode').select_option('HUMAN_APPROVED');enter(page);check('unapproved human refuses silently unverifiable references',page.evaluate('window.HPT16.session===null') and 'HUMAN_APPROVED' in page.locator('#feedback').inner_text())
    page.locator('#mode').select_option('SYNTHETIC');enter(page)
    check('synthetic session begins and locked source labels',page.evaluate('window.HPT16.session.mode')=='SYNTHETIC' and page.locator('#begin').is_disabled())
    check('garage prompt is neutral',('Znajdź' in page.locator('#prompt').inner_text()) and 'G01' in page.locator('#stepStatus').inner_text())
    check('cannot export before observation',page.locator('#export').is_disabled())
    page.locator('#participantToggle').click()
    check('participant mode hides moderator and consent controls',page.locator('#moderator').count()==0 if False else page.locator('#mode').is_hidden() and page.locator('#obs').is_hidden() and page.locator('#begin').is_hidden())
    check('participant mode displays neutral task',page.locator('#prompt').is_visible() and page.locator('#taskTitle').is_visible())
    check('participant region focused',page.evaluate('document.activeElement.id')=='participantRegion')
    page.screenshot(path=str(ROOT/'QA_SYNTHETIC_PARTICIPANT_1280.png'),full_page=True)
    page.keyboard.press('Escape');check('escape restores operator without data loss',page.locator('#mode').is_visible() and page.evaluate('window.HPT16.session.tasks.length')==0)
    page.locator('#startTask').click();check('running stopwatch forbids second start',page.locator('#startTask').is_disabled() and page.locator('#stopTask').is_enabled())
    check('operator may enter participant view after actual timer start',page.locator('#participantToggle').is_enabled())
    page.locator('#participantToggle').click(); check('timer continues while participant sees prompt',page.locator('#prompt').is_visible() and page.locator('#mode').is_hidden())
    page.keyboard.press('Escape')
    page.wait_for_timeout(170);page.locator('#stopTask').click()
    check('timer actually measured elapsed nonzero',page.locator('#timer').inner_text()!='00:00.0')
    page.locator('#next').click();check('no outcome rejects commit',page.evaluate('window.HPT16.session.tasks.length')==0)
    page.locator('#outcome').select_option('UNAIDED_SUCCESS');page.locator('#help').fill('1');page.locator('#next').click()
    check('unaided with nonzero hints rejected',page.evaluate('window.HPT16.session.tasks.length')==0 and 'Bez pomocy' in page.locator('#feedback').inner_text())
    page.locator('#help').fill('0');page.locator('#next').click()
    check('G01 persisted and B02 displayed',page.evaluate('window.HPT16.session.tasks.length')==1 and page.locator('#stepStatus').inner_text().startswith('B02'))
    check('new task resets timer and cannot accidentally continue',page.locator('#timer').inner_text()=='00:00.0' and page.locator('#next').is_disabled())
    page.locator('#startTask').click();page.wait_for_timeout(170);page.locator('#stopTask').click()
    page.locator('#outcome').select_option('ASSISTED_SUCCESS');page.locator('#next').click()
    check('assisted result without hint rejected',page.evaluate('window.HPT16.session.tasks.length')==1)
    page.locator('#help').fill('2');page.locator('#next').click()
    check('B02 saved and C03 visible',page.evaluate('window.HPT16.session.tasks.length')==2 and 'C03' in page.locator('#stepStatus').inner_text())
    page.locator('#startTask').click();page.wait_for_timeout(130);page.locator('#stopTask').click()
    page.locator('#outcome').select_option('PRODUCT_BLOCKED');page.locator('#next').click()
    check('blocker without evidence rejected',page.evaluate('window.HPT16.session.tasks.length')==2)
    page.locator('#comment').fill('Brak możliwego powrotu do garażu w izolowanej kopii testowej');page.locator('#next').click()
    check('all tasks in order complete, export enabled',page.evaluate('window.HPT16.session.tasks.map(x=>x.task_id)')==['G01','B02','C03'] and page.locator('#export').is_enabled())
    with page.expect_download() as d:page.locator('#export').click()
    download=d.value;path=ROOT/'QA_SYNTHETIC_GBC01.json';download.save_as(path)
    exported=json.loads(path.read_text(encoding='utf-8'))
    check('browser emits real exported JSON bytes',path.stat().st_size>1000 and download.suggested_filename=='GBC01_SYNTHETIC_P01_S01.json')
    check('schema and fixed exact 3 tasks',exported['schema']=='B01-13/PASS16/GBC01/session-v1' and len(exported['tasks'])==3)
    check('all measured times positive',all(x['elapsed_ms']>0 for x in exported['tasks']))
    check('observations not synthesized by program',exported['tasks'][0]['outcome']=='UNAIDED_SUCCESS' and exported['tasks'][1]['help_count']==2 and exported['tasks'][2]['outcome']=='PRODUCT_BLOCKED')
    check('source and build pin retained in export',exported['source']['codex_head']==proto['source']['codex_head'] and exported['buildsha']=='a'*40 and exported['zipsha']=='b'*64)
    page.once('dialog',lambda d:d.dismiss());page.locator('#withdraw').click()
    check('cancelled withdrawal preserves session',page.evaluate('window.HPT16.session!==null'))
    page.once('dialog',lambda d:d.accept());page.locator('#withdraw').click()
    check('confirmed withdrawal zeros session',page.evaluate('window.HPT16.session===null && window.HPT16.withdrawn===true'))
    check('withdrawn form removes export and historical tasks',page.locator('#controls').is_hidden() and page.locator('#taskTitle').inner_text()=='Dane sesji wycofane')
    check('original source uses no persistent web storage',not re.search(r'\b(?:localStorage|sessionStorage|indexedDB)\b',HTML.read_text(encoding='utf-8')))
    reset(page);check('reload does not persist participant metadata',page.evaluate('window.HPT16.session===null') and page.locator('#mode').input_value()=='SYNTHETIC')
    for w in (320,720,1080):
        page.set_viewport_size({'width':w,'height':720});page.wait_for_timeout(100)
        if w==720:page.screenshot(path=str(ROOT/'QA_SYNTHETIC_OPERATOR_720.png'),full_page=True)
        check(f'viewport {w}px no horizontal scroll',page.evaluate('document.documentElement.scrollWidth <= window.innerWidth'))
    check('no actual JavaScript exceptions',not errors)
    # Independent controlled partial withdrawal negative on FRESH second session
    reset(page);fill_start(page);enter(page);page.locator('#startTask').click();page.wait_for_timeout(110);page.locator('#stopTask').click()
    page.once('dialog',lambda d:d.accept());page.locator('#withdraw').click()
    check('withdraw during partial session removes uncommitted timer record',page.evaluate('window.HPT16.session===null') and page.locator('#controls').is_hidden())
    check('withdraw disables subsequent new session until clean reload',page.locator('#begin').is_disabled())
    browser.close()

report={'mode':'SYNTHETIC','subject':'Automated Chromium/browser, never human','source_html_sha256':hashlib.sha256(HTML.read_bytes()).hexdigest(),'protocol_sha256':hashlib.sha256(PROTOCOL.read_bytes()).hexdigest(),'checks_executed':len(PASS),'pass':len(PASS),'fail':0,'checks':PASS,'native_unity':'NOT_RUN','real_participants':0,'player':'NOT_RUN','consent_proof':'NOT_RUN'}
(ROOT/'QA_BROWSER_PASS16.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('REAL_BROWSER_ASSERTIONS='+str(len(PASS))+'/'+str(len(PASS))+' PASS')
