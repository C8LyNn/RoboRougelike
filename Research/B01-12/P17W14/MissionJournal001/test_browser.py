#!/usr/bin/env python3
"""Exact SHA256 source guards, real Chromium browser (not mocked), negative import and interaction tests."""
import hashlib,json,os,pathlib,sys
from playwright.sync_api import sync_playwright
ROOT=pathlib.Path(__file__).resolve().parent
M=json.loads((ROOT/'manifest.json').read_text())
for f,meta in M['pins'].items():
    data=(ROOT/f).read_bytes()
    if len(data)!=meta['bytes'] or hashlib.sha256(data).hexdigest()!=meta['sha256']:
        print('FAIL_CLOSED',f,file=sys.stderr);sys.exit(2)
html=(ROOT/'demo.html').read_text()
for f in ('journal_engine.js','ui.js'):
    token=f'<script src="{f}"></script>'
    assert html.count(token)==1, 'missing original JS tag'
    html=html.replace(token,'<script>\n'+(ROOT/f).read_text()+'\n</script>')
results=[]
def check(name,fn):
    try: fn();results.append({'name':name,'pass':True})
    except Exception as e: results.append({'name':name,'pass':False,'error':str(e)})
def eq(a,b): assert a==b,f'{a!r} != {b!r}'
with sync_playwright() as p:
    browser=p.chromium.launch(headless=True,executable_path=os.environ.get('CHROMIUM_PATH') or None,args=['--no-sandbox'])
    for width,height in ((320,720),(768,1024),(1280,720),(1920,1080)):
        prefix=f'{width}x{height}: '
        page=browser.new_page(viewport={'width':width,'height':height},reduced_motion='reduce')
        errors=[]
        page.on('pageerror',lambda e:errors.append(str(e)))
        page.set_content(html,wait_until='load')
        snap=lambda:page.evaluate('MissionJournalDemo.snapshot()')
        check(prefix+'title',lambda:eq(page.title(),'B01-12 — Dziennik celów'))
        check(prefix+'five objectives',lambda:eq(page.locator('.item').count(),5))
        check(prefix+'initial revision',lambda:eq(snap()['revision'],0))
        check(prefix+'initial completions',lambda:eq(snap()['done'],[]))
        check(prefix+'initial available count',lambda:eq(page.locator('button:has-text("Ukończ"):not([disabled])').count(),1))
        check(prefix+'progress 0',lambda:eq(page.locator('#progress').get_attribute('value'),'0'))
        check(prefix+'no overflow initial',lambda:eq(page.evaluate('document.documentElement.scrollWidth <= innerWidth'),True))
        page.locator('.item').nth(0).get_by_role('button',name='Szczegóły: Dotrzyj do bramy').click()
        check(prefix+'selected',lambda:eq('Dotrzyj do bramy' in page.locator('#selected').inner_text(),True))
        page.locator('.item').nth(0).get_by_role('button',name='Przypnij').click()
        check(prefix+'pinned id',lambda:eq(snap()['pin'],'reach_gate'))
        check(prefix+'aria pressed true',lambda:eq(page.locator('.item').first.get_by_role('button',name='Odepnij').get_attribute('aria-pressed'),'true'))
        page.locator('.item').first.get_by_role('button',name='Odepnij').click()
        check(prefix+'unpinned',lambda:eq(snap()['pin'],None))
        page.locator('.item').first.get_by_role('button',name='Ukończ').focus()
        page.keyboard.press('Enter')
        check(prefix+'keyboard complete',lambda:eq(snap()['done'],['reach_gate']))
        check(prefix+'next goal available',lambda:eq(page.locator('button:has-text("Ukończ"):not([disabled])').count(),1))
        check(prefix+'progress one',lambda:eq(page.locator('#progress').get_attribute('value'),'1'))
        check(prefix+'receipt generated',lambda:eq(snap()['receipts'],['ui_1']))
        page.locator('#export').click()
        exported=page.locator('#json').input_value()
        check(prefix+'export valid JSON',lambda:eq(json.loads(exported)['done'],['reach_gate']))
        bad=json.dumps({'version':1,'done':['exit_arena'],'receipts':['forged'],'pin':None,'revision':1})
        page.locator('#json').fill(bad)
        page.locator('#import').click()
        check(prefix+'invalid import atomic',lambda:eq(snap()['done'],['reach_gate']))
        check(prefix+'invalid import status',lambda:eq('odrzucony' in page.locator('#status').inner_text(),True))
        page.locator('#json').fill(exported)
        page.locator('#reset').click()
        check(prefix+'reset all',lambda:eq(snap()['done'],[]))
        page.locator('#import').click()
        check(prefix+'round-trip restore',lambda:eq(snap()['done'],['reach_gate']))
        check(prefix+'restored revision',lambda:eq(snap()['revision'],3))
        page.locator('#filter').select_option('done')
        check(prefix+'done filter',lambda:eq(page.locator('.item').count(),1))
        page.locator('#filter').select_option('locked')
        check(prefix+'locked filter',lambda:eq(page.locator('.item').count(),3))
        page.locator('#filter').select_option('available')
        check(prefix+'available filter',lambda:eq(page.locator('.item').count(),1))
        page.locator('#filter').select_option('all')
        check(prefix+'all filter',lambda:eq(page.locator('.item').count(),5))
        check(prefix+'no overflow after controls',lambda:eq(page.evaluate('document.documentElement.scrollWidth <= innerWidth'),True))
        check(prefix+'no JS errors',lambda:eq(errors,[]))
        if width==1280:page.screenshot(path=str(ROOT/'evidence-1280x720.png'),full_page=True)
        page.close()
    browser.close()
passed=sum(c['pass'] for c in results)
(ROOT/'browser-results.json').write_text(json.dumps({'total':len(results),'passed':passed,'failed':len(results)-passed,'cases':results},indent=2,ensure_ascii=False)+'\n')
print(f'CHROMIUM {passed}/{len(results)} '+('PASS' if passed==len(results) else 'FAIL'))
for c in results:
    if not c['pass']:print('FAIL',c['name'],c['error'])
if passed!=len(results):sys.exit(1)
