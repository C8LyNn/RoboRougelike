#!/usr/bin/env python3
"""Run original pinned HTML/CSS/JS in genuine Chromium; never mock DOM or reducer."""
import json, os, hashlib, pathlib, sys, time
from playwright.sync_api import sync_playwright
BASE=pathlib.Path(__file__).resolve().parent
M=json.loads((BASE/'manifest.json').read_text())
for file, info in M['pins'].items():
    data=(BASE/file).read_bytes()
    if len(data)!=info['bytes'] or hashlib.sha256(data).hexdigest()!=info['sha256']:
        print('FAIL_CLOSED',file,file=sys.stderr)
        sys.exit(2)
html=(BASE/'demo.html').read_text()
for path in ['confirm_engine.js','ui.js']:
    token=f'<script src="{path}"></script>'
    assert html.count(token)==1, 'unexpected script reference'
    html=html.replace(token, '<script>\n'+(BASE/path).read_text()+'\n</script>')
results=[]
def check(name,fn):
    try:
        fn()
        results.append({'name':name,'pass':True})
    except Exception as err:
        results.append({'name':name,'pass':False,'error':str(err)})
def eq(a,b):
    assert a==b,f'{a!r} != {b!r}'
with sync_playwright() as p:
    executable=os.getenv('CHROMIUM_PATH') or None
    browser=p.chromium.launch(headless=True,executable_path=executable,args=['--no-sandbox'])
    for width,height in [(320,720),(768,1024),(1280,720),(1920,1080)]:
        page=browser.new_page(viewport={'width':width,'height':height},reduced_motion='reduce')
        errors=[]
        page.on('pageerror',lambda e: errors.append(str(e)))
        page.set_content(html,wait_until='load')
        pre=f'{width}x{height}: '
        def snapshot(): return page.evaluate('ConfirmGuardDemo.snapshot()')
        check(pre+'title',lambda:eq(page.title(),'B01-12 — Guard potwierdzenia'))
        check(pre+'initial idle',lambda:eq(snapshot()['phase'],'idle'))
        check(pre+'initial receipt null',lambda:eq(snapshot()['receipt'],None))
        check(pre+'initial commits 0',lambda:eq(snapshot()['committed'],0))
        check(pre+'has accessible status',lambda:eq(page.locator('[role=status]').count(),1))
        check(pre+'has progressbar',lambda:eq(page.locator('[role=progressbar]').count(),1))
        check(pre+'all selected actions',lambda:eq(page.locator('#action option').count(),3))
        check(pre+'two modalities',lambda:eq(page.locator('#mode option').count(),2))
        check(pre+'hold initially disabled',lambda:eq(page.locator('#hold').is_disabled(),True))
        check(pre+'confirm initially disabled',lambda:eq(page.locator('#confirm').is_disabled(),True))
        check(pre+'no document overflow',lambda: eq(page.evaluate('document.documentElement.scrollWidth<=innerWidth'),True))
        page.locator('#arm').click()
        check(pre+'armed',lambda:eq(snapshot()['phase'],'armed'))
        check(pre+'hold enabled',lambda:eq(page.locator('#hold').is_enabled(),True))
        check(pre+'selector disabled while armed',lambda:eq(page.locator('#action').is_disabled(),True))
        check(pre+'live status armed',lambda:eq('Przytrzymaj' in page.locator('#status').inner_text(),True))
        # Short pointer press must never commit.
        box=page.locator('#hold').bounding_box()
        if box is None: raise RuntimeError('hold button not visible')
        page.mouse.move(box['x']+box['width']/2,box['y']+box['height']/2)
        page.mouse.down(); page.wait_for_timeout(80);page.mouse.up()
        check(pre+'short press rejected',lambda:eq(snapshot()['committed'],0))
        check(pre+'short press still armed',lambda:eq(snapshot()['phase'],'armed'))
        # Hold >1.2 real wall-clock seconds
        page.mouse.down();page.wait_for_timeout(1290);page.mouse.up()
        check(pre+'long press accepted',lambda:eq(snapshot()['phase'],'committed'))
        check(pre+'receipt action',lambda:eq(snapshot()['receipt']['action'],'discard'))
        check(pre+'receipt source hold',lambda:eq(snapshot()['receipt']['mode'],'hold'))
        check(pre+'single commit',lambda:eq(snapshot()['committed'],1))
        check(pre+'repeat pointer impossible',lambda:eq(page.locator('#hold').is_disabled(),True))
        page.locator('#reset').click()
        check(pre+'reset returns idle',lambda:eq(snapshot()['phase'],'idle'))
        check(pre+'reset clears receipt',lambda:eq(snapshot()['receipt'],None))
        # Two-step keyboard-accessible alternative
        page.locator('#mode').select_option('two-step')
        page.locator('#action').select_option('abandon')
        check(pre+'mode updated',lambda:eq(snapshot()['mode'],'two-step'))
        check(pre+'action updated',lambda:eq(snapshot()['action'],'abandon'))
        page.locator('#arm').focus();page.keyboard.press('Enter')
        check(pre+'keyboard arm',lambda:eq(snapshot()['phase'],'armed'))
        check(pre+'hold disabled in two-step',lambda:eq(page.locator('#hold').is_disabled(),True))
        check(pre+'confirm enabled',lambda:eq(page.locator('#confirm').is_enabled(),True))
        page.wait_for_timeout(250)
        page.locator('#confirm').focus();page.keyboard.press('Enter')
        check(pre+'keyboard confirm commits',lambda:eq(snapshot()['phase'],'committed'))
        check(pre+'two-step correct receipt',lambda:eq(snapshot()['receipt']['mode'],'two-step'))
        check(pre+'two-step correct action',lambda:eq(snapshot()['receipt']['action'],'abandon'))
        page.locator('#arm').click()
        check(pre+'rearm serial 3',lambda:eq(snapshot()['serial'],3))
        check(pre+'rearm receipt null',lambda:eq(snapshot()['receipt'],None))
        page.keyboard.press('Escape')
        check(pre+'escape cancels',lambda:eq(snapshot()['phase'],'idle'))
        check(pre+'escape no receipt',lambda:eq(snapshot()['receipt'],None))
        page.locator('#action').select_option('overwrite')
        check(pre+'third action switch',lambda:eq(snapshot()['action'],'overwrite'))
        page.locator('#arm').click()
        page.locator('#cancel').click()
        check(pre+'manual cancel',lambda:eq(snapshot()['phase'],'idle'))
        check(pre+'manual cancel message',lambda:eq('Anulowano' in page.locator('#status').inner_text(),True))
        check(pre+'no overflow after changes',lambda:eq(page.evaluate('document.documentElement.scrollWidth<=innerWidth'),True))
        check(pre+'no JS exception',lambda:eq(errors,[]))
        if width==1280:
            page.screenshot(path=str(BASE/'evidence-1280x720.png'),full_page=True)
        page.close()
    browser.close()
passed=sum(x['pass'] for x in results)
(BASE/'browser-results.json').write_text(json.dumps({'total':len(results),'passed':passed,'failed':len(results)-passed,'cases':results},indent=2,ensure_ascii=False)+'\n')
print(f'CHROMIUM {passed}/{len(results)} '+('PASS' if passed==len(results) else 'FAIL'))
for x in results:
    if not x['pass']: print('FAIL',x['name'],x['error'])
if passed!=len(results): sys.exit(1)
