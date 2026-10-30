#!/usr/bin/env python3
"""Actually execute SHA-pinned production HTML/JS in Chromium; reject source byte changes."""
import hashlib, json, os, pathlib, sys
from playwright.sync_api import sync_playwright
ROOT=pathlib.Path(__file__).parent
EXPECTED={'demo.html','ping_engine.js','ui.js','test_engine.cjs','test_browser.py'}
try:
    man=json.loads((ROOT/'manifest.json').read_text(encoding='utf-8'))
    if man.get('package')!='B01-12-TACTICAL-PING-001/R1' or set(man.get('source',{}))!=EXPECTED:raise ValueError('manifest')
    for name,pin in man['source'].items():
        b=(ROOT/name).read_bytes()
        if len(b)!=pin['bytes'] or hashlib.sha256(b).hexdigest()!=pin['sha256']:raise ValueError('source mismatch: '+name)
except Exception as exc:
    print('FAIL_CLOSED',exc,file=sys.stderr);sys.exit(2)
page_text=(ROOT/'demo.html').read_text(encoding='utf-8')
for name in ('ping_engine.js','ui.js'):
    assert '<script src="'+name+'"></script>' in page_text
    source=(ROOT/name).read_text(encoding='utf-8').replace('</script','<\\/script')
    page_text=page_text.replace('<script src="'+name+'"></script>','<script>'+source+'</script>')
checks=[];errors=[]
def check(name, condition):
    checks.append({'name':name,'result':'PASS' if condition else 'FAIL'})
    if not condition:print('FAIL:',name)
with sync_playwright() as p:
    path=os.getenv('CHROMIUM_PATH','/usr/bin/chromium')
    browser=p.chromium.launch(headless=True,executable_path=path if pathlib.Path(path).exists() else None,args=['--no-sandbox'])
    for w,h in ((320,720),(768,1024),(1280,720),(1920,1080)):
        page=browser.new_page(viewport={'width':w,'height':h})
        page.on('pageerror',lambda exc:errors.append(str(exc)))
        page.set_content(page_text,wait_until='load')
        t=str(w)
        check(t+' title',page.title()=='B01-12 · Tactical Ping 001')
        check(t+' initial 0/6',page.locator('#count').inner_text()=='0 / 6')
        check(t+' status ready','Gotowe' in page.locator('#status').inner_text())
        check(t+' map visible',page.locator('#arena').is_visible())
        check(t+' no overflow',page.evaluate('document.documentElement.scrollWidth <= innerWidth'))
        page.locator('#arena').focus();page.keyboard.press('Enter')
        check(t+' enter creates',page.locator('.marker').count()==1)
        check(t+' first selected',page.locator('.marker.chosen').count()==1)
        check(t+' first attack',page.locator('.marker.attack').count()==1)
        check(t+' count one',page.locator('#count').inner_text()=='1 / 6')
        check(t+' snapshot valid',page.evaluate('JSON.parse(document.querySelector("#snapshot").textContent).markers.length')==1)
        page.locator('[data-kind="repair"]').click()
        page.locator('#priority').select_option('3')
        page.locator('#arena').focus();page.keyboard.press('Enter')
        check(t+' 2 markers',page.locator('.marker').count()==2)
        check(t+' repair color',page.locator('.marker.repair').count()==1)
        check(t+' snapshot priority',page.evaluate('JSON.parse(document.querySelector("#snapshot").textContent).markers[1].priority')==3)
        page.locator('#tick').click()
        check(t+' tick 2s',page.locator('#clock').inner_text()=='T+2.0 s')
        page.locator('#tick').click();page.locator('#tick').click()
        check(t+' attack expired',page.locator('.marker.attack').count()==0)
        check(t+' repair retained',page.locator('.marker.repair').count()==1)
        page.locator('#remove').click()
        check(t+' remove selected',page.locator('.marker').count()==0)
        page.locator('#reset').click()
        check(t+' reset clock',page.locator('#clock').inner_text()=='T+0.0 s')
        check(t+' reset markers',page.locator('#count').inner_text()=='0 / 6')
        check(t+' no exceptions',not errors)
        check(t+' no overflow after interactions',page.evaluate('document.documentElement.scrollWidth <= innerWidth'))
        if w==1280:
            page.locator('#arena').focus();page.keyboard.press('Enter')
            page.screenshot(path=str(ROOT/'evidence-1280x720.png'),full_page=True)
        page.close()
    browser.close()
result={'package':'B01-12-TACTICAL-PING-001/R1','browser':'actual Chromium Playwright','cases':checks,'total':len(checks),'pass':sum(x['result']=='PASS' for x in checks),'fail':sum(x['result']=='FAIL' for x in checks),'js_exceptions':errors}
print(f'CHROMIUM {result["pass"]}/{result["total"]} {"PASS" if result["fail"]==0 else "FAIL"}; JS exceptions {len(errors)}')
if os.getenv('WRITE_RECEIPT')=='1':(ROOT/'browser-results.json').write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
sys.exit(1 if result['fail'] or errors else 0)
