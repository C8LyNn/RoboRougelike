#!/usr/bin/env python3
"""Real Chromium DOM QA for original local files. Manifest checked before browser launch."""
from __future__ import annotations
import hashlib, json, os, pathlib, sys
from playwright.sync_api import sync_playwright
BASE=pathlib.Path(__file__).resolve().parent
m=json.loads((BASE/'manifest.json').read_text(encoding='utf8'))
for f, expected in m['sources'].items():
    b=(BASE/f).read_bytes()
    if hashlib.sha256(b).hexdigest()!=expected['sha256'] or len(b)!=expected['bytes']:
        print('FAIL_CLOSED source mismatch: '+f, file=sys.stderr)
        sys.exit(2)
checks=[]
def require(name,ok):
    if not ok: raise AssertionError(name)
    checks.append(name)
viewports=[(320,720),(768,1024),(1280,720),(1920,1080)]
with sync_playwright() as p:
    browser=p.chromium.launch(headless=True,executable_path=os.getenv('CHROMIUM_PATH') or p.chromium.executable_path,args=['--no-sandbox','--disable-dev-shm-usage'])
    for width,height in viewports:
        page=browser.new_page(viewport={'width':width,'height':height},device_scale_factor=1)
        errors=[]
        page.on('pageerror',lambda e: errors.append(str(e)))
        html=(BASE/'demo.html').read_text(encoding='utf8')
        html=html.replace('<script src="guide_engine.js"></script>', '<script>'+ (BASE/'guide_engine.js').read_text(encoding='utf8') + '</script>')
        html=html.replace('<script src="ui.js"></script>', '<script>'+ (BASE/'ui.js').read_text(encoding='utf8') + '</script>')
        page.set_content(html)
        pre=str(width)+'x'+str(height)+' '
        require(pre+'title',page.title()=='B01-12 - Shortcut Guide')
        require(pre+'initial menu visible',page.locator('#panel').is_visible())
        require(pre+'initial context garage',page.evaluate("window.__guideDemo.getState().scope")=='garage')
        require(pre+'initial count 10',page.locator('#results button').count()==10)
        require(pre+'global help exists',page.locator('[data-id="help"]').count()==1)
        require(pre+'garage move exists',page.locator('[data-id="garage-move"]').count()==1)
        require(pre+'combat hidden in garage',page.locator('[data-id="combat-fire1"]').count()==0)
        page.locator('[data-id="garage-move"]').click()
        require(pre+'detail selection',page.locator('#details h2').inner_text()=='Move module')
        require(pre+'aria pressed',page.locator('[data-id="garage-move"]').get_attribute('aria-pressed')=='true')
        page.locator('#query').fill('ctrl')
        require(pre+'search filters',page.locator('#results button').count()==4)
        require(pre+'query clears detail',page.locator('#details h2').inner_text()=='Wybierz polecenie')
        require(pre+'search by key',page.locator('[data-id="garage-undo"]').count()==1)
        page.locator('#query').fill('definitelynotpresent')
        require(pre+'zero results',page.locator('#results button').count()==0)
        require(pre+'empty state',page.locator('.empty').is_visible())
        page.locator('[data-scope="combat"]').click()
        require(pre+'combat context',page.evaluate("window.__guideDemo.getState().scope")=='combat')
        require(pre+'query reset on context change',page.locator('#query').input_value()=='')
        require(pre+'combat count 9',page.locator('#results button').count()==9)
        require(pre+'combat fire visible',page.locator('[data-id="combat-fire1"]').count()==1)
        page.locator('[data-scope="map"]').click()
        require(pre+'map count 7',page.locator('#results button').count()==7)
        page.locator('[data-id="map-ping"]').focus()
        page.keyboard.press('Enter')
        require(pre+'keyboard activates map detail',page.locator('#details h2').inner_text()=='Place ping')
        page.keyboard.press('Escape')
        require(pre+'escape closes panel',page.locator('#panel').is_hidden())
        require(pre+'aria close state',page.locator('#toggle').get_attribute('aria-expanded')=='false')
        page.locator('#toggle').click()
        require(pre+'toggle reopens',page.locator('#panel').is_visible())
        page.evaluate("document.dispatchEvent(new KeyboardEvent('keydown',{key:'?',bubbles:true}))")
        require(pre+'question key closes',page.locator('#panel').is_hidden())
        page.evaluate("document.dispatchEvent(new KeyboardEvent('keydown',{key:'?',bubbles:true}))")
        require(pre+'question key reopens',page.locator('#panel').is_visible())
        # Ensure query content is inserted with textContent, never parsed as markup.
        page.locator('#query').fill('<img src=x onerror=alert(1)>')
        require(pre+'no injected image',page.locator('img').count()==0)
        require(pre+'no unsafe result on markup',page.locator('#results button').count()==0)
        require(pre+'no horizontal document overflow',page.evaluate('document.documentElement.scrollWidth <= window.innerWidth'))
        require(pre+'no JS exceptions',not errors)
        if width==1280:
            page.locator('[data-scope="garage"]').click()
            page.locator('[data-id="garage-undo"]').click()
            page.screenshot(path=str(BASE/'evidence-1280x720.png'),full_page=True)
        page.close()
    browser.close()
result={'suite':'chromium','total':len(checks),'passed':len(checks),'failed':0,'viewports':[f'{w}x{h}' for w,h in viewports],'names':checks,'page_errors':0}
(BASE/'browser-results.json').write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n',encoding='utf8')
print(f'CHROMIUM {len(checks)}/{len(checks)} PASS; JS exceptions 0')
