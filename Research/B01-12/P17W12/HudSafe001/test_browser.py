#!/usr/bin/env python3
"""Real original HTML+JS in Chromium; SHA pins verified BEFORE browser startup."""
import hashlib,json,pathlib,sys,os
P=pathlib.Path(__file__).resolve().parent
manifest=json.loads((P/'manifest.json').read_text())
for name,pin in manifest['pins'].items():
    data=(P/name).read_bytes()
    if len(data)!=pin['bytes'] or hashlib.sha256(data).hexdigest()!=pin['sha256']:
        print('FAIL_CLOSED: '+name,file=sys.stderr);sys.exit(2)
from playwright.sync_api import sync_playwright
HTML=(P/'demo.html').read_text()
for name in ['layout_engine.js','ui.js']:
    HTML=HTML.replace('<script src="'+name+'"></script>','<script>'+ (P/name).read_text()+'</script>')
assert 'src="layout_engine.js"' not in HTML
results=[]; failures=[];errors=[]
def check(view,case,label,expr):
    ok=bool(expr)
    results.append({'viewport':view,'case':case,'test':label,'pass':ok})
    if not ok: failures.append(f'{view}/{case}/{label}')
with sync_playwright() as p:
    browser=p.chromium.launch(headless=True,executable_path=os.getenv('CHROMIUM_PATH') or None,args=['--no-sandbox'])
    for w,h,preset in [(320,720,'mobile'),(768,1024,'tablet'),(1280,720,'desktop'),(1920,1080,'ultra')]:
        page=browser.new_page(viewport={'width':w,'height':h})
        page.on('pageerror',lambda error:errors.append(str(error)))
        page.set_content(HTML,wait_until='load')
        page.select_option('#preset',preset)
        for cut in ['none','notch','center']:
            for density in ['normal','crowded','impossible']:
                page.select_option('#cutout',cut)
                page.select_option('#density',density)
                v=page.evaluate('window.__hudResult')
                case=cut+'/'+density
                check(w,case,'status',v['result']['ok'] == (density!='impossible'))
                check(w,case,'no_overlaps',not v['badOverlap'])
                check(w,case,'atomic_fail',v['result']['placements']==[] if density=='impossible' else len(v['result']['placements'])==len(v['config']['widgets']))
                check(w,case,'no_document_horizontal_overflow',page.evaluate('document.documentElement.scrollWidth <= window.innerWidth'))
                check(w,case,'screen_matches_preset',page.evaluate('document.querySelector("#screen").style.width')==str(w)+'px')
                check(w,case,'live_status',bool(page.locator('[role="status"]').inner_text()))
        page.select_option('#density','normal');page.select_option('#cutout','none')
        page.select_option('#insets','large')
        check(w,'large_insets','safe_area',page.evaluate('window.__hudResult.result.safeArea.x===40'))
        page.keyboard.press('Tab')
        check(w,'keyboard','focus_visible',page.evaluate('document.activeElement !== document.body'))
        if w==1280:
            page.select_option('#insets','normal')
            page.screenshot(path=str(P/'evidence-1280x720.png'),full_page=True)
        page.close()
    browser.close()
output={'package':'B01-12-HUDSAFE-001/R1','kind':'chromium','passed':len(results)-len(failures),'failed':len(failures),'js_errors':errors,'failures':failures,'results':results,'exit':0 if not failures and not errors else 1}
(P/'browser-results.json').write_text(json.dumps(output,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({k:v for k,v in output.items() if k!='results'},ensure_ascii=False))
sys.exit(output['exit'])
