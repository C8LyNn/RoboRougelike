#!/usr/bin/env python3
"""Original-byte real Chromium UI acceptance. NOT Unity or human playtest."""
import hashlib,json,sys
from pathlib import Path
from playwright.sync_api import sync_playwright
ROOT=Path(__file__).resolve().parent
manifest=json.loads((ROOT/'manifest.json').read_text(encoding='utf-8'))
for name,pin in manifest['pinned'].items():
    source=(ROOT/name).read_bytes()
    if hashlib.sha256(source).hexdigest()!=pin['sha256'] or len(source)!=pin['bytes']:
        print('FAIL_CLOSED source mismatch: '+name,file=sys.stderr)
        sys.exit(2)
checks=[]
errors=[]
def check(name,good):
    checks.append({'case':name,'pass':bool(good)})
    if not good:print('FAILED '+name,file=sys.stderr)
with sync_playwright() as p:
    chromium_binary='/usr/bin/chromium' if Path('/usr/bin/chromium').is_file() else None
    launch_options={'headless':True,'args':['--no-sandbox']}
    if chromium_binary: launch_options['executable_path']=chromium_binary
    browser=p.chromium.launch(**launch_options)
    for w,h in [(320,720),(768,1024),(1280,720),(1920,1080)]:
        page=browser.new_page(viewport={'width':w,'height':h})
        page.on('pageerror',lambda e: errors.append(str(e)))
        # The operator blocks file:// navigation. Preserve original verified JS bytes, embed as classic script in the original verified HTML.
        html=(ROOT/'a11y.html').read_text(encoding='utf-8')
        engine=(ROOT/'a11y_engine.js').read_text(encoding='utf-8')
        assert html.count('<script src="a11y_engine.js"></script>')==1
        page.set_content(html.replace('<script src="a11y_engine.js"></script>','<script>'+engine+'</script>'))
        tag=str(w)
        check(tag+'/title',page.title()=='B01-12 · Dostępność — laboratorium UX')
        check(tag+'/initial_100',page.locator('#textScale').input_value()=='100')
        check(tag+'/initial_no_export',page.locator('#exportState').inner_text()=='Brak')
        check(tag+'/five_controls',page.locator('select').count()==5)
        check(tag+'/initial_warning_icon_text','⚠' in page.locator('#warning').inner_text() and 'brak napędu' in page.locator('#warning').inner_text())
        check(tag+'/no_horizontal_scroll_initial',page.evaluate('document.documentElement.scrollWidth<=window.innerWidth'))
        page.locator('#contrast').select_option('high')
        check(tag+'/contrast_change',page.evaluate("getComputedStyle(document.documentElement).getPropertyValue('--bg').trim()")=='#000000')
        check(tag+'/contrast_ratio',float(page.locator('#ratio').inner_text().split(':')[0])>=7)
        page.locator('#textScale').select_option('150')
        check(tag+'/150_text',page.evaluate("getComputedStyle(document.documentElement).getPropertyValue('--scale').trim()")=='1.5')
        page.locator('#motion').select_option('reduced')
        check(tag+'/zero_motion',page.locator('#duration').inner_text()=='0 ms')
        check(tag+'/css_duration_zero',page.evaluate("getComputedStyle(document.documentElement).getPropertyValue('--duration').trim()")=='0ms')
        page.locator('#cues').select_option('icon-text-pattern')
        check(tag+'/pattern',page.locator('#warning').get_attribute('data-pattern')=='true')
        page.locator('#captions').select_option('false')
        check(tag+'/captions_hide',page.locator('#subtitle').is_hidden())
        check(tag+'/critical_warning_retained',page.locator('#warning strong').is_visible())
        page.locator('#demo').click()
        check(tag+'/demo_status','Ostrzeżenie:' in page.locator('#status').inner_text())
        page.locator('#export').click()
        s=page.locator('#profile').input_value()
        doc=json.loads(s)
        check(tag+'/export_schema',doc['schema']=='b01-12.a11y-profile' and doc['version']==1)
        check(tag+'/export_values',doc['prefs']['textScale']==150 and doc['prefs']['motion']=='reduced')
        check(tag+'/export_current',page.locator('#exportState').inner_text()=='Aktualny')
        page.locator('#textScale').select_option('125')
        check(tag+'/stale_export_clear',page.locator('#profile').input_value()=='' and page.locator('#exportState').inner_text()=='Brak')
        page.locator('#undo').click()
        check(tag+'/undo_restores',page.locator('#textScale').input_value()=='150')
        page.locator('#redo').click()
        check(tag+'/redo_restores',page.locator('#textScale').input_value()=='125')
        page.locator('#reset').click()
        check(tag+'/reset_default',page.locator('#textScale').input_value()=='100' and page.locator('#contrast').input_value()=='standard')
        check(tag+'/reset_warning_still_visible',page.locator('#warning').is_visible())
        page.locator('#profile').fill(s)
        page.locator('#import').click()
        check(tag+'/valid_import',page.locator('#textScale').input_value()=='150' and page.locator('#captions').input_value()=='false')
        original_revision=page.locator('#revision').inner_text()
        page.locator('#profile').fill('{"schema":"invalid"}')
        page.locator('#import').click()
        check(tag+'/invalid_import_atomic',page.locator('#revision').inner_text()==original_revision and page.locator('#textScale').input_value()=='150')
        check(tag+'/invalid_import_feedback','Nieobsługiwany' in page.locator('#status').inner_text())
        page.locator('#profile').fill(s.replace('"textScale": 150','"textScale": "huge"'))
        page.locator('#import').click()
        check(tag+'/invalid_numeric_import',page.locator('#revision').inner_text()==original_revision)
        check(tag+'/keyboard_focus',page.locator('#contrast').evaluate('(e)=>{e.focus();return document.activeElement===e}'))
        page.locator('#contrast').press('Home')
        page.locator('#contrast').press('ArrowDown')
        check(tag+'/keyboard_select',page.locator('#contrast').input_value()=='high')
        check(tag+'/keyboard_state_css',page.evaluate("getComputedStyle(document.documentElement).getPropertyValue('--bg').trim()")=='#000000')
        check(tag+'/no_horizontal_scroll_150',page.evaluate('document.documentElement.scrollWidth<=window.innerWidth'))
        check(tag+'/no_js_errors',len(errors)==0)
        if w==1280: page.screenshot(path=str(ROOT/'evidence-1280x720.png'),full_page=True)
        page.close()
    browser.close()
result={'package':'B01-12-A11Y-PREFS-001/R1','environment':'Linux Chromium/Playwright; original SHA-pinned HTML with original SHA-pinned JS inlined due to operator file:// restriction','total':len(checks),'pass':sum(c['pass'] for c in checks),'fail':[c['case'] for c in checks if not c['pass']],'js_errors':errors,'native_unity':'NOT_RUN','cases':checks}
(ROOT/'browser-results.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k!='cases'},ensure_ascii=False,indent=2))
sys.exit(0 if not result['fail'] and not errors else 1)
