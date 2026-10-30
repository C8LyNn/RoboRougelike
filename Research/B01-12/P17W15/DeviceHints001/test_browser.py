from pathlib import Path
import hashlib, json, sys, os
from playwright.sync_api import sync_playwright
ROOT=Path(__file__).resolve().parent
m=json.loads((ROOT/'manifest.json').read_text())
for name,expected in m['files'].items():
    src=(ROOT/name).read_bytes()
    if len(src)!=expected['bytes'] or hashlib.sha256(src).hexdigest()!=expected['sha256']:
        print('FAIL_CLOSED '+name)
        sys.exit(2)
html=(ROOT/'device_hints.html').read_text()
cases=[]
def check(name,ok):
    cases.append({'name':name,'ok':bool(ok)})
    if not ok: print('FAIL:',name)
with sync_playwright() as pw:
    browser=pw.chromium.launch(headless=True, executable_path=None if os.getenv('GITHUB_ACTIONS') else '/usr/bin/chromium',args=['--no-sandbox'])
    for width,height in [(320,720),(768,1024),(1280,720),(1920,1080)]:
        page=browser.new_page(viewport={'width':width,'height':height})
        errors=[]
        page.on('pageerror',lambda e:errors.append(str(e)))
        page.set_content(html,wait_until='load')
        prefix=str(width)
        read=lambda:json.loads(page.locator('#snapshot').inner_text())
        check(prefix+' default KM', read()['active']=='kbm')
        check(prefix+' five hints', page.locator('.prompt').count()==5)
        check(prefix+' first glyph WASD', page.locator('.glyph').first.inner_text()=='WASD')
        check(prefix+' default clock', read()['now']==0)
        page.locator('#drift').click()
        check(prefix+' pad drift ignored', read()['active']=='kbm' and page.locator('#status').inner_text()=='NOISE_IGNORED')
        page.locator('#pad').click()
        check(prefix+' armed after first', read()['pending']['device']=='pad')
        page.locator('#pad').click()
        check(prefix+' bounce ignored', read()['active']=='kbm' and page.locator('#status').inner_text()=='BOUNCE_IGNORED')
        page.locator('#tick100').click()
        page.locator('#pad').click()
        check(prefix+' switched to pad', read()['active']=='pad')
        check(prefix+' RT visible', page.locator('.glyph').nth(1).inner_text()=='RT')
        check(prefix+' revision 1', read()['revision']==1)
        page.locator('#keyboard').click()
        check(prefix+' cooldown', read()['active']=='pad' and page.locator('#status').inner_text()=='COOLDOWN')
        page.locator('#tick900').click()
        page.locator('#keyboard').click()
        check(prefix+' KB candidate', read()['pending']['device']=='kbm')
        page.locator('#tick100').click()
        page.locator('#keyboard').click()
        check(prefix+' switched back KM',read()['active']=='kbm')
        page.locator('#context').select_option('garage')
        check(prefix+' garage context', read()['context']=='garage')
        check(prefix+' garage visible', page.locator('.glyph').nth(1).inner_text()=='R')
        page.locator('#reset').click()
        check(prefix+' reset active', read()['active']=='kbm' and read()['now']==0 and read()['revision']==0)
        check(prefix+' reset context', read()['context']=='combat')
        check(prefix+' no overflow',page.evaluate('document.documentElement.scrollWidth <= window.innerWidth'))
        page.locator('#pad').focus()
        page.keyboard.press('Enter')
        check(prefix+' keyboard Enter button works', read()['pending'] is not None)
        page.locator('#context').select_option('garage')
        check(prefix+' context cancels armed', read()['pending']==None)
        check(prefix+' no JS errors', len(errors)==0)
        if width==1280:
            page.screenshot(path=str(ROOT/'evidence-1280x720.png'),full_page=True)
        page.close()
    browser.close()
summary={'total':len(cases),'pass':sum(x['ok'] for x in cases),'fail':sum(not x['ok'] for x in cases),'cases':cases}
(ROOT/'browser-results.json').write_text(json.dumps(summary,indent=2)+'\n')
print('CHROMIUM',summary['pass'],'/',summary['total'],'PASS')
sys.exit(1 if summary['fail'] else 0)
