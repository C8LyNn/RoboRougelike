#!/usr/bin/env python3
"""Portable real Chromium replay of original B01-12/P17 HTML. Not Unity QA."""
import hashlib, json
from pathlib import Path
from playwright.sync_api import sync_playwright
HERE = Path(__file__).resolve().parent
SRC = HERE/'garage-blueprint-lab.html'
EXPECTED = 'd1732bca3fbc6b42558b742ae841b5372de69183b9f24e1bd60f2c426e28100e'
checks=[];errors=[]
def check(k,ok):
    checks.append((k,bool(ok)))
    if not ok: raise AssertionError(k)
def slot(page,key,action):
    page.locator(f'[data-slot="{key}"]').click()
    page.locator(f'[data-action="{action}"]').click()
def issue(page,code): return page.locator(f'#working [data-issue="{code}"]').count()>0
source=SRC.read_bytes()
check('sha256 source identity',hashlib.sha256(source).hexdigest()==EXPECTED)
with sync_playwright() as p:
    browser=p.chromium.launch(headless=True,executable_path='/usr/bin/chromium',args=['--no-sandbox'])
    for w,h in [(320,720),(768,1024),(1280,720),(1920,1080)]:
        page=browser.new_page(viewport={'width':w,'height':h})
        page.on('pageerror',lambda e: errors.append(str(e)))
        page.set_content(source.decode(),wait_until='load')
        k=f'{w}x{h}'
        check(k+' baseline ready','GOTOWY' in page.locator('#baseline').inner_text())
        check(k+' working ready','GOTOWY' in page.locator('#working').inner_text())
        slot(page,'gunA','damage');slot(page,'wheelR','damage')
        check(k+' dual independent critical',issue(page,'WEAPON') and issue(page,'MOBILITY'))
        page.locator('#undo').click();check(k+' undo mobility',not issue(page,'MOBILITY'))
        page.locator('#redo').click();check(k+' redo mobility',issue(page,'MOBILITY'))
        page.locator('#drive').select_option('tracks')
        page.locator('#filter').select_option('mobility')
        check(k+' mobility filter',page.locator('[data-slot="gunA"]').count()==0)
        slot(page,'trackL','toggle');slot(page,'trackR','toggle')
        check(k+' tracks repair selected mobility',not issue(page,'MOBILITY'))
        check(k+' quoted purchase',page.locator('#quote').inner_text().startswith('90 '))
        page.locator('#export').click();saved=page.locator('#payload').input_value()
        check(k+' export schema','B01-12/garage-blueprint-v1' in saved)
        page.locator('#reset').click();page.locator('#import').click()
        check(k+' import true',page.locator('#drive').input_value()=='tracks' and not issue(page,'MOBILITY'))
        before=page.locator('#working').inner_text()+page.locator('#quote').inner_text()
        for label,payload in [('invalid syntax','not-json'),('unsupported schema',saved.replace('garage-blueprint-v1','unknown')),('tampered hp',saved.replace('"trackL": 100','"trackL": 999'))]:
            page.locator('#payload').fill(payload);page.locator('#import').click()
            check(k+' rejected '+label,'Import odrzucony' in page.locator('#message').inner_text())
            check(k+' atomic '+label,before==page.locator('#working').inner_text()+page.locator('#quote').inner_text())
        page.locator('#commit').click()
        check(k+' committing zero quote',page.locator('#quote').inner_text().startswith('0 '))
        check(k+' responsive no overflow',page.evaluate('document.documentElement.scrollWidth<=innerWidth+1'))
        page.close()
    browser.close()
check('no JS exceptions',len(errors)==0)
result={'source_sha256':EXPECTED,'browser':'real Chromium/Playwright','checks':len(checks),'pass':sum(ok for _,ok in checks),'fail':[k for k,ok in checks if not ok],'pageerrors':errors}
print(json.dumps(result,ensure_ascii=False,indent=2))
assert not result['fail']
