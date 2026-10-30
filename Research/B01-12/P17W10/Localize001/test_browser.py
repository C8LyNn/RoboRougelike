#!/usr/bin/env python3
"""Run original sha-pinned browser demo in actual Chromium. Not Unity."""
import hashlib,json,sys
from pathlib import Path
from playwright.sync_api import sync_playwright
ROOT=Path(__file__).resolve().parent
manifest=json.loads((ROOT/'manifest.json').read_text())
for file, info in manifest['files'].items():
    data=(ROOT/file).read_bytes()
    if hashlib.sha256(data).hexdigest()!=info['sha256'] or len(data)!=info['bytes']:
        print('FAIL_CLOSED source mismatch',file,file=sys.stderr)
        raise SystemExit(2)
html=(ROOT/'demo.html').read_text()
for name in ('localize_engine.js','catalog.js','app.js'):
    html=html.replace('<script src="'+name+'"></script>', '<script>\n'+(ROOT/name).read_text()+'\n</script>')
checks=[];errors=[]
def ck(label,yes):
    checks.append(dict(label=label,pass_=bool(yes)))
    if not yes:print('FAILED',label,file=sys.stderr)
with sync_playwright() as p:
    browser=p.chromium.launch(headless=True,executable_path=('/usr/bin/chromium' if Path('/usr/bin/chromium').exists() else p.chromium.executable_path),args=['--no-sandbox'])
    for width,height in [(320,720),(768,1024),(1280,720),(1920,1080)]:
        page=browser.new_page(viewport={'width':width,'height':height})
        page.on('pageerror',lambda err: errors.append(str(err)))
        page.set_content(html)
        tag=str(width)
        ck(tag+'/initial-pl-title',page.locator('#title').inner_text()=='Laboratorium lokalizacji interfejsu')
        ck(tag+'/initial-part-pl',page.locator('#screen .stat').inner_text()=='2 części')
        ck(tag+'/lang-pl',page.locator('html').get_attribute('lang')=='pl')
        ck(tag+'/no-overflow-initial',page.evaluate('document.documentElement.scrollWidth<=innerWidth'))
        page.locator('#quantity').fill('1');page.locator('#quantity').dispatch_event('change')
        ck(tag+'/one-part-polish',page.locator('#screen .stat').inner_text()=='1 część')
        ck(tag+'/state-qty-updated',json.loads(page.locator('#snapshot').inner_text())['quantity']==1)
        page.locator('#locale').select_option('en')
        ck(tag+'/en-title',page.locator('#title').inner_text()=='Interface localization lab')
        ck(tag+'/one-part-en',page.locator('#screen .stat').inner_text()=='1 part')
        ck(tag+'/state-qty-persists',json.loads(page.locator('#snapshot').inner_text())['quantity']==1)
        ck(tag+'/language-status', 'Language changed' in page.locator('#notice').inner_text())
        ck(tag+'/locale-html-en',page.locator('html').get_attribute('lang')=='en')
        page.locator('#combatTab').click()
        ck(tag+'/combat-pressed',page.locator('#combatTab').get_attribute('aria-pressed')=='true')
        ck(tag+'/combat-title-en',page.locator('#screen h2').inner_text()=='Weapon control')
        ck(tag+'/combat-weapon-unchanged',page.locator('#screen').get_by_text('Active weapon: Railgun').count()==1)
        ck(tag+'/combat-ammo-one',page.locator('#screen .stat').inner_text()=='1 projectile')
        page.locator('#quantity').fill('12');page.locator('#quantity').dispatch_event('change')
        ck(tag+'/combat-ammo-many',page.locator('#screen .stat').inner_text()=='12 projectiles')
        page.locator('#locale').select_option('pl')
        ck(tag+'/combat-12-polish',page.locator('#screen .stat').inner_text()=='12 pocisków')
        ck(tag+'/combat-active-kept',page.locator('#combatTab').get_attribute('aria-pressed')=='true')
        page.locator('#locale').select_option('pseudo')
        ck(tag+'/pseudo-title',page.locator('#title').inner_text().startswith('⟦'))
        ck(tag+'/pseudo-count-kept', '12' in page.locator('#screen .stat').inner_text())
        ck(tag+'/pseudo-status',page.locator('#notice').inner_text().startswith('⟦'))
        ck(tag+'/pseudo-html-lang-en',page.locator('html').get_attribute('lang')=='en')
        ck(tag+'/pseudo-no-x-overflow',page.evaluate('document.documentElement.scrollWidth<=innerWidth'))
        page.locator('#quantity').fill('99');page.locator('#quantity').dispatch_event('change')
        ck(tag+'/qty-99-persists',json.loads(page.locator('#snapshot').inner_text())['quantity']==99)
        page.locator('#quantity').fill('200');page.locator('#quantity').dispatch_event('change')
        ck(tag+'/qty-invalid-reset',page.locator('#quantity').input_value()=='99')
        ck(tag+'/qty-state-not-corrupt',json.loads(page.locator('#snapshot').inner_text())['quantity']==99)
        page.locator('#garageTab').click()
        ck(tag+'/garage-pressed',page.locator('#garageTab').get_attribute('aria-pressed')=='true')
        page.locator('#locale').select_option('pl')
        ck(tag+'/polish-99-parts',page.locator('#screen .stat').inner_text()=='99 części')
        page.locator('#actionButton').click()
        ck(tag+'/event-button-real',json.loads(page.locator('#snapshot').inner_text())['renderCount']==1)
        ck(tag+'/scrap-unchanged',json.loads(page.locator('#snapshot').inner_text())['scrap']==40)
        ck(tag+'/localized-wheel-pl',page.locator('#actionButton').inner_text()=='Zamontuj Koło')
        ck(tag+'/screen-warning-no-loss', 'brakuje napędu' in page.locator('#screen').inner_text())
        ck(tag+'/no-x-overflow-final',page.evaluate('document.documentElement.scrollWidth<=innerWidth'))
        if width==1280:
            page.screenshot(path=str(ROOT/'evidence-1280x720.png'),full_page=True)
        page.close()
    browser.close()
receipt={'package':'B01-12-LOCALIZE-001/R1','total':len(checks),'pass':sum(x['pass_'] for x in checks),'fail':[x['label'] for x in checks if not x['pass_']],'js_errors':errors,'checks':checks,'native_unity':'NOT_RUN'}
(ROOT/'browser-results.json').write_text(json.dumps(receipt,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({k:v for k,v in receipt.items() if k!='checks'},ensure_ascii=False,indent=2))
sys.exit(0 if not receipt['fail'] and not errors else 1)
