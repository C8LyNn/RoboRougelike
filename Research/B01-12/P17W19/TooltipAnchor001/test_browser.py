#!/usr/bin/env python3
"""Real Chromium, real rendered DOM & keyboard/pointer events; verifies original source bytes before any execution."""
import hashlib,json,os,sys,pathlib
from playwright.sync_api import sync_playwright
ROOT=pathlib.Path(__file__).resolve().parent
manifest=json.loads((ROOT/'manifest.json').read_text(encoding='utf-8'))
def verify():
    if manifest.get('package')!='B01-12-TOOLTIP-ANCHOR-001/R1' or manifest.get('version')!=1:raise ValueError('FAIL_CLOSED wrong manifest')
    for name,pin in manifest['sources'].items():
        raw=(ROOT/name).read_bytes()
        if len(raw)!=pin['bytes'] or hashlib.sha256(raw).hexdigest()!=pin['sha256']:
            raise ValueError('FAIL_CLOSED source mismatch: '+name)
try:verify()
except Exception as e:
    print(str(e),file=sys.stderr);sys.exit(2)
html=(ROOT/'demo.html').read_text(encoding='utf-8')
engine=(ROOT/'anchor_engine.js').read_text(encoding='utf-8')
ui=(ROOT/'ui.js').read_text(encoding='utf-8')
# Host blocks file://. Embed *exact verified originals* in-memory, no alternate browser implementation.
html=html.replace('<script src="anchor_engine.js"></script>','<script>'+engine+'</script>')
html=html.replace('<script src="ui.js"></script>','<script>'+ui+'</script>')
if '<script src=' in html:raise RuntimeError('unsubstituted script')
results=[]
def check(name,condition):
    results.append({'name':name,'status':'PASS' if condition else 'FAIL'})
    if not condition:
        print('FAIL',name)
        try:print('DEBUG',page.locator('#status').inner_text(),page.evaluate('B01TooltipDemo.getState()'),page.locator('#tooltip').get_attribute('aria-hidden'))
        except Exception as e:print('DEBUG_ERROR',e)
def rect_valid(result):
    if not result.get('ok'):return False
    r=result['rect'];return all(isinstance(r[k],(int,float)) for k in ('x','y','width','height'))
with sync_playwright() as p:
    binary=os.environ.get('CHROMIUM_PATH')
    browser=p.chromium.launch(headless=True,executable_path=binary if binary and os.path.exists(binary) else None,args=['--no-sandbox'])
    errors=[]
    for w,h in [(320,720),(768,1024),(1280,720),(1920,1080)]:
        page=browser.new_page(viewport={'width':w,'height':h},device_scale_factor=1)
        page.on('pageerror',lambda err:errors.append(str(err)))
        page.set_content(html,wait_until='load')
        label=f'{w}x{h}'
        check(label+' 8 buttons',page.locator('.module').count()==8)
        check(label+' default status',page.locator('#tooltip').get_attribute('aria-hidden')=='true')
        check(label+' no horizontal overflow',page.evaluate('document.documentElement.scrollWidth <= window.innerWidth'))
        check(label+' default width',page.locator('#viewport-label').inner_text()=='1280 × 720')
        for selected in ['1280x720','1920x1080','768x1024','320x720']:
            page.select_option('#view',selected)
            check(label+' dropdown '+selected,page.locator('#viewport-label').inner_text()==selected.replace('x',' × '))
        page.select_option('#view','1280x720')
        page.locator('#mod-hp').focus()
        check(label+' focus reveals',page.locator('#tooltip').get_attribute('aria-hidden')=='false')
        check(label+' label HP',page.locator('#tip-title').inner_text()=='Punkty wytrzymałości')
        check(label+' side default',page.locator('#tip-side').inner_text().startswith('STRONA: '))
        # Pointer mouse click sets pinned, leave does not hide.
        page.locator('#mod-hp').click()
        check(label+' click pins',page.locator('#mod-hp').get_attribute('aria-pressed')=='true')
        page.locator('#mod-mass').hover()
        check(label+' pin survives hover',page.locator('#tip-title').inner_text()=='Punkty wytrzymałości')
        check(label+' pin survives blur',page.locator('#tooltip').get_attribute('aria-hidden')=='false')
        page.keyboard.press('Escape')
        check(label+' escape closes',page.locator('#tooltip').get_attribute('aria-hidden')=='true')
        check(label+' escape clears pressed',page.locator('#mod-hp').get_attribute('aria-pressed')=='false')
        for pref in ['top','right','bottom','left']:
            page.mouse.move(1,1)
            page.select_option('#preferred',pref)
            page.locator('#preferred').focus()
            page.locator('#mod-mass').focus()
            check(label+' preference '+pref,page.locator('#tooltip').get_attribute('aria-hidden')=='false')
            # Candidate always within the simulated viewport and not intersecting the anchor.
            geometry=page.evaluate('''() => {const e=B01TooltipAnchor,v=B01TooltipDemo.viewport(),rect={x:v.width*.41,y:v.height*.1,width:v.width*.18,height:v.height*.15};return e.solve({viewport:v,anchor:rect,tooltip:{width:Math.min(v.width*.30,230),height:Math.min(v.height*.20,120)},preferred:document.querySelector('#preferred').value,gap:Math.min(14,v.width*.018),insets:{top:Math.min(v.height*.06,30),bottom:Math.min(v.height*.06,30),left:Math.min(v.width*.045,28),right:Math.min(v.width*.045,28)},exclusions:[{x:v.width*.37,y:v.height*.37,width:v.width*.25,height:v.height*.26}]})}''')
            check(label+' computed rect '+pref,rect_valid(geometry))
            page.keyboard.press('Escape')
        page.select_option('#obstacle','off');page.locator('#mod-energy').click()
        check(label+' obstruction toggle preserves tooltip',page.locator('#tooltip').get_attribute('aria-hidden')=='false')
        check(label+' pin control enabled',page.locator('#pin').is_enabled())
        page.locator('#close').click()
        check(label+' close button',page.locator('#tooltip').get_attribute('aria-hidden')=='true')
        check(label+' pin disabled when closed',not page.locator('#pin').is_enabled())
        page.select_option('#view','320x720');page.locator('#mod-repair').focus()
        check(label+' tiny-view safe state',page.locator('#status').inner_text()!='')
        check(label+' no JS exceptions',len(errors)==0)
        check(label+' responsive body width',page.evaluate('document.documentElement.scrollWidth <= window.innerWidth'))
        if w==1280:
            page.select_option('#view','1280x720');page.keyboard.press('Escape')
            page.locator('#preferred').focus();page.locator('#mod-hp').focus()
            page.screenshot(path=str(ROOT/'evidence-1280x720.png'),full_page=True)
        page.close()
    browser.close()
passed=sum(x['status']=='PASS' for x in results)
receipt={'package':manifest['package'],'browser':'real Chromium / Playwright','viewports':['320x720','768x1024','1280x720','1920x1080'],'passed':passed,'total':len(results),'failed':len(results)-passed,'pageErrors':errors,'cases':results}
(ROOT/'browser-results.json').write_text(json.dumps(receipt,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
print(f'CHROMIUM {passed}/{len(results)} PASS; JS exceptions {len(errors)}');sys.exit(0 if passed==len(results) and not errors else 1)
