#!/usr/bin/env python3
import json, pathlib, hashlib, sys, os
from playwright.sync_api import sync_playwright
ROOT=pathlib.Path(__file__).resolve().parent
m=json.loads((ROOT/'manifest.json').read_text())
for filename,p in m['files'].items():
    b=(ROOT/filename).read_bytes()
    if len(b)!=p['bytes'] or hashlib.sha256(b).hexdigest()!=p['sha256']:
        print('FAIL_CLOSED source mismatch: '+filename,file=sys.stderr);sys.exit(2)
checks=[]
def check(label,cond):
    checks.append({'case':label,'status':'PASS' if cond else 'FAIL'})
    if not cond: print('FAIL',label,file=sys.stderr)
with sync_playwright() as p:
    browser=p.chromium.launch(executable_path=os.environ.get('CHROMIUM_PATH') or None,headless=True,args=['--no-sandbox'])
    for w,h in [(320,720),(768,1024),(1280,720),(1920,1080)]:
        page=browser.new_page(viewport={'width':w,'height':h},device_scale_factor=1)
        errors=[];page.on('pageerror',lambda err:errors.append(str(err)))
        doc=(ROOT/'demo.html').read_text(encoding='utf8')
        for fn in ['pan_engine.js','ui.js']:
            doc=doc.replace('<script src="'+fn+'"></script>','<script>'+(ROOT/fn).read_text(encoding='utf8')+'</script>')
        page.set_content(doc,wait_until='load');page.wait_for_timeout(150)
        d=lambda:page.evaluate('window.__edgePanDebug()')
        check(f'{w}: rendering',page.locator('.module').count()==8)
        check(f'{w}: stage bounded',160<=d()['view']['width']<=1440)
        check(f'{w}: no document overflow',page.evaluate('document.documentElement.scrollWidth<=window.innerWidth+1'))
        check(f'{w}: initial scroll',d()['scroll']=={'x':0,'y':0})
        check(f'{w}: toggler checked',page.locator('#toggle').is_checked())
        stage=page.locator('#stage');box=stage.bounding_box();item=page.locator('.module').first
        ibox=item.bounding_box()
        # Some mobile views have the 180px first module visible.
        check(f'{w}: first module visible',ibox is not None and ibox['width']>0)
        page.mouse.move(ibox['x']+20,ibox['y']+20);page.mouse.down()
        page.mouse.move(box['x']+box['width']-4,box['y']+box['height']-4,steps=5)
        page.wait_for_timeout(280)
        s=d();check(f'{w}: dragging active',s['drag']['active'])
        check(f'{w}: horizontal pan',s['scroll']['x']>0)
        check(f'{w}: vertical pan',s['scroll']['y']>0)
        check(f'{w}: finite pan',all(0<=s['scroll'][k]<=(s['canvas'][{'x':'width','y':'height'}[k]]-s['view'][{'x':'width','y':'height'}[k]]) for k in ('x','y')))
        page.mouse.up();page.wait_for_timeout(40)
        check(f'{w}: end clears drag',not d()['drag']['active'])
        before=d()['scroll']['x'];page.wait_for_timeout(120)
        check(f'{w}: stop after release',abs(d()['scroll']['x']-before)<0.001)
        page.locator('#reset').click();check(f'{w}: reset zero',d()['scroll']=={'x':0,'y':0})
        page.locator('#right').click();check(f'{w}: right preset',d()['scroll']['x']>0)
        page.locator('#left').click();check(f'{w}: left preset',d()['scroll']['x']==0)
        page.locator('#toggle').uncheck();check(f'{w}: disable',d()['drag']['enabled'] is False)
        page.locator('#toggle').check();check(f'{w}: enable',d()['drag']['enabled'] is True)
        check(f'{w}: aria live',page.locator('#status').get_attribute('aria-live')=='polite')
        check(f'{w}: no js error',not errors)
        check(f'{w}: viewport after',page.evaluate('document.documentElement.scrollWidth<=window.innerWidth+1'))
        if w==1280: page.screenshot(path=str(ROOT/'evidence-1280x720.png'),full_page=True)
        page.close()
    browser.close()
pass_n=sum(c['status']=='PASS' for c in checks)
(ROOT/'browser-results.json').write_text(json.dumps({'tests':checks,'pass':pass_n,'fail':len(checks)-pass_n},indent=2)+'\n')
print(f'CHROMIUM {pass_n}/{len(checks)}', 'PASS' if pass_n==len(checks) else 'FAIL')
if pass_n!=len(checks):sys.exit(1)
