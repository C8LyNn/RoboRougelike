#!/usr/bin/env python3
"""Original-source browser test, real Playwright Chromium; no network or Unity."""
import hashlib,json,os,pathlib,sys
from playwright.sync_api import sync_playwright
ROOT=pathlib.Path(__file__).resolve().parent
manifest=json.loads((ROOT/'manifest.json').read_text(encoding='utf-8'))
for name,pin in manifest['pins'].items():
    raw=(ROOT/name).read_bytes()
    if len(raw)!=pin['bytes'] or hashlib.sha256(raw).hexdigest()!=pin['sha256']:
        print('FAIL_CLOSED source mismatch: '+name,file=sys.stderr);sys.exit(2)
html=(ROOT/'demo.html').read_text(encoding='utf-8')
for name in ['box_engine.js','ui.js']:
    html=html.replace(f'<script src="{name}"></script>',f'<script>\n{(ROOT/name).read_text(encoding="utf-8")}\n</script>')
cases=[];errors=[]
def record(name,fn):
    try: fn();cases.append({'name':name,'ok':True})
    except Exception as exc: cases.append({'name':name,'ok':False,'error':str(exc)})
def check(assertion,why):
    if not assertion: raise AssertionError(why)
with sync_playwright() as pw:
    binary=os.environ.get('CHROMIUM_PATH') or '/usr/bin/chromium'
    kwargs={'headless':True,'args':['--no-sandbox','--disable-gpu']}
    if pathlib.Path(binary).exists(): kwargs['executable_path']=binary
    browser=pw.chromium.launch(**kwargs)
    for width,height in [(320,720),(768,1024),(1280,720),(1920,1080)]:
        page=browser.new_page(viewport={'width':width,'height':height},device_scale_factor=1)
        page.on('pageerror',lambda error:errors.append(str(error)))
        page.set_content(html,wait_until='load')
        prefix=f'{width}x{height}'
        def run(name,fn):record(prefix+'/'+name,fn)
        run('title',lambda:check(page.locator('h1').inner_text().startswith('Garage'),'title'))
        run('parts-11',lambda:check(page.locator('.part').count()==11,'part count'))
        run('disabled-core',lambda:check(page.locator('.part[data-id="core"]').is_disabled(),'locked core'))
        run('initial-rev',lambda:check(page.locator('#version').inner_text()=='REV 0','revision'))
        run('no-document-x-overflow',lambda:check(page.evaluate('document.documentElement.scrollWidth<=innerWidth'),'overflow'))
        run('board-scrollable',lambda:check(page.locator('.board-frame').evaluate('(e)=>e.scrollWidth>=e.clientWidth'),'board scroll'))
        page.locator('.part[data-id="gun-a"]').click()
        run('click-select',lambda:check(page.locator('.part[data-id="gun-a"]').get_attribute('aria-pressed')=='true','selection'))
        run('selected-list',lambda:check('Railgun A' in page.locator('#selected').inner_text(),'list'))
        run('undo-enabled',lambda:check(page.locator('#undo').is_enabled(),'undo enabled'))
        page.locator('#down').click()
        run('move-down',lambda:check(page.locator('.part[data-id="gun-a"]').evaluate('(e)=>parseInt(e.style.top)')==84,'movement'))
        page.locator('#undo').click()
        run('undo-move',lambda:check(page.locator('.part[data-id="gun-a"]').evaluate('(e)=>parseInt(e.style.top)')==64,'undo move'))
        page.locator('#redo').click()
        run('redo-move',lambda:check(page.locator('.part[data-id="gun-a"]').evaluate('(e)=>parseInt(e.style.top)')==84,'redo move'))
        page.locator('#clear').click()
        run('clear-selection',lambda:check(page.locator('.part[aria-pressed="true"]').count()==0,'clear'))
        page.locator('#select-mobility').click()
        run('select-all-wheels',lambda:check(page.locator('.part[aria-pressed="true"]').count()==4,'four wheels'))
        run('locked-core-not-selected',lambda:check(page.locator('.part[data-id="core"]').get_attribute('aria-pressed')=='false','core remains locked'))
        page.locator('#mode').select_option('subtract');page.locator('.part[data-id="wheel-fl"]').click()
        run('subtract',lambda:check(page.locator('.part[aria-pressed="true"]').count()==3,'subtract'))
        page.locator('#mode').select_option('add');page.locator('.part[data-id="wheel-fl"]').click()
        run('add',lambda:check(page.locator('.part[aria-pressed="true"]').count()==4,'add'))
        page.locator('#mode').select_option('toggle');page.locator('.part[data-id="wheel-fl"]').click()
        run('toggle',lambda:check(page.locator('.part[aria-pressed="true"]').count()==3,'toggle'))
        page.locator('#export').click()
        run('export-json',lambda:check('"version":1' in page.locator('#status').inner_text(),'export'))
        page.locator('#reset').click()
        run('reset-no-selection',lambda:check(page.locator('.part[aria-pressed="true"]').count()==0,'reset'))
        run('reset-position',lambda:check(page.locator('.part[data-id="gun-a"]').evaluate('(e)=>parseInt(e.style.top)')==64,'reset position'))
        page.locator('.part[data-id="gun-b"]').focus();page.keyboard.press('Enter')
        run('keyboard-activation',lambda:check(page.locator('.part[data-id="gun-b"]').get_attribute('aria-pressed')=='true','keyboard select'))
        page.keyboard.press('Alt+ArrowDown')
        run('keyboard-move',lambda:check(page.locator('.part[data-id="gun-b"]').evaluate('(e)=>parseInt(e.style.top)')==84,'keyboard movement'))
        page.keyboard.press('Control+z')
        run('keyboard-undo',lambda:check(page.locator('.part[data-id="gun-b"]').evaluate('(e)=>parseInt(e.style.top)')==64,'keyboard undo'))
        page.keyboard.press('Control+y')
        run('keyboard-redo',lambda:check(page.locator('.part[data-id="gun-b"]').evaluate('(e)=>parseInt(e.style.top)')==84,'keyboard redo'))
        page.locator('#reset').click()
        run('no-x-overflow-after-ui',lambda:check(page.evaluate('document.documentElement.scrollWidth<=innerWidth'),'overflow after'))
        if width==1280:
            # True Chromium pointer drag across the logical canvas (the panel can scroll on mobile).
            box=page.locator('#board').bounding_box()
            x=box['x']+160;y=box['y']+101
            page.mouse.move(x,y);page.mouse.down();page.mouse.move(x+100,y+80,steps=8);page.mouse.up()
            run('mouse-box-rect',lambda:check(page.locator('.part[data-id="wheel-fl"]').get_attribute('aria-pressed')=='true','drag select'))
            run('mouse-drag-gone',lambda:check(page.locator('#drag').is_hidden(),'drag indicator'))
            page.locator('#clear').click();page.locator('#contain').select_option('inside')
            page.mouse.move(x+20,y+20);page.mouse.down();page.mouse.move(x+30,y+30);page.mouse.up()
            run('rect-inside-doesnt-hit',lambda:check(page.locator('.part[aria-pressed="true"]').count()==0,'inside'))
            # Collision on an actual button: gun-a moving into gun-b must fail atomically.
            page.locator('.part[data-id="gun-a"]').click()
            old=page.locator('.part[data-id="gun-a"]').evaluate('(e)=>e.style.left')
            page.keyboard.press('Alt+ArrowRight')
            new=page.locator('.part[data-id="gun-a"]').evaluate('(e)=>e.style.left')
            run('right-20-success',lambda:check(old!=new,'move successful'))
            for _ in range(3):page.keyboard.press('Alt+ArrowRight')
            run('collision-rejected-ui',lambda:check('Odrzucono' in page.locator('#status').inner_text(),'collision fail closed'))
            page.locator('#reset').click()
            page.screenshot(path=str(ROOT/'evidence-1280x720.png'),full_page=True)
        page.close()
    browser.close()
report={'package':'B01-12-BOXSELECT-001/R1','total':len(cases),'pass':sum(1 for c in cases if c['ok']),'fail':sum(1 for c in cases if not c['ok']),'js_errors':errors,'native_unity':'NOT_RUN','tests':cases}
(ROOT/'browser-results.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('CHROMIUM',f"{report['pass']}/{report['total']}", 'PASS' if not report['fail'] and not errors else 'FAIL','js_errors',len(errors))
if report['fail'] or errors:
    print(json.dumps([x for x in cases if not x['ok']],indent=2));sys.exit(1)
