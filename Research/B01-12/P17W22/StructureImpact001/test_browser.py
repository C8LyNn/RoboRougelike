#!/usr/bin/env python3
"""Actual Playwright Chromium DOM/pointer/keyboard run, manifest-gated."""
import hashlib,json,os,pathlib,sys
from playwright.sync_api import sync_playwright
HERE=pathlib.Path(__file__).resolve().parent
ALLOWED={'impact_engine.js','fixture.js','ui.js','demo.html','test_engine.cjs','test_browser.py'}
def gate():
 try:
  m=json.loads((HERE/'manifest.json').read_text(encoding='utf-8'))
  if m.get('schema')!=1 or set(m['files'])!=ALLOWED: raise ValueError('manifest schema')
  for name,spec in m['files'].items():
   data=(HERE/name).read_bytes()
   if len(data)!=spec['bytes'] or hashlib.sha256(data).hexdigest()!=spec['sha256']: raise ValueError('source mismatch: '+name)
 except Exception as e:
  print('FAIL_CLOSED '+str(e),file=sys.stderr);sys.exit(2)
gate()
records=[];errors=[]
def case(name,callback):
 try:callback();records.append({'case':name,'result':'PASS'})
 except Exception as e:records.append({'case':name,'result':'FAIL','reason':str(e)});print('FAIL',name,str(e))
def assert_text(page,selector,expected):
 actual=page.locator(selector).inner_text().strip()
 if actual!=expected:raise AssertionError(f'{selector}: {actual!r} != {expected!r}')
def assert_contains(page,selector,expected):
 actual=page.locator(selector).inner_text()
 if expected not in actual:raise AssertionError(f'{selector}: {actual!r} missing {expected!r}')
with sync_playwright() as p:
 browser=p.chromium.launch(headless=True,executable_path=os.environ.get('CHROMIUM_PATH') or None,args=['--no-sandbox'])
 for w,h in [(320,720),(768,1024),(1280,720),(1920,1080)]:
  page=browser.new_page(viewport={'width':w,'height':h});page.on('pageerror',lambda e:errors.append(str(e)))
  doc=(HERE/'demo.html').read_text(encoding='utf-8')
  for fn in ['impact_engine.js','fixture.js','ui.js']:
   doc=doc.replace('<script src="'+fn+'"></script>','<script>'+ (HERE/fn).read_text(encoding='utf-8')+'</script>')
  page.set_content(doc,wait_until='load');page.wait_for_selector('.module')
  prefix=f'{w}x{h}'
  case(prefix+' shows all 15 modules',lambda: (lambda c: (_ for _ in ()).throw(AssertionError(c)) if c!=15 else None)(page.locator('.module').count()))
  case(prefix+' shows 17 edges',lambda: (lambda c: (_ for _ in ()).throw(AssertionError(c)) if c!=17 else None)(page.locator('#edges line').count()))
  case(prefix+' initial baseline 14',lambda:assert_text(page,'#remainCount','14'))
  case(prefix+' baseline zero lost',lambda:assert_text(page,'#lostCount','0'))
  case(prefix+' core protected',lambda: (page.locator('[data-id="core"]').click(), assert_contains(page,'#status','Rdzeń chroniony')))
  case(prefix+' click crossbar',lambda: (page.locator('[data-id="crossbar"]').click(),assert_text(page,'#lostCount','1')))
  case(prefix+' target CSS selected',lambda: (lambda x: (_ for _ in ()).throw(AssertionError(x)) if not x else None)(page.locator('[data-id="crossbar"]').get_attribute('class').find('target')>=0))
  case(prefix+' gun is detached',lambda: (lambda x: (_ for _ in ()).throw(AssertionError(x)) if x!='gun' else None)(json.loads(page.locator('#json').inner_text())['detached'][0]))
  case(prefix+' gun marked danger',lambda: (lambda x: (_ for _ in ()).throw(AssertionError(x)) if not x else None)('lost' in page.locator('[data-id="gun"]').get_attribute('class')))
  case(prefix+' preview does not remove nodes',lambda: (lambda x: (_ for _ in ()).throw(AssertionError(x)) if x!=15 else None)(page.locator('.module').count()))
  case(prefix+' wheel is leaf',lambda: (page.locator('[data-id="wheel_fl"]').click(), assert_text(page,'#lostCount','0')))
  case(prefix+' keyboard Enter on rear L',lambda: (page.locator('[data-id="rear_l"]').focus(),page.keyboard.press('Enter'),assert_text(page,'#lostCount','1')))
  case(prefix+' worst button chooses top ranked',lambda: (page.locator('#worst').click(),assert_text(page,'#lostCount','1')))
  case(prefix+' JSON schema and stable selected',lambda: (lambda x: (_ for _ in ()).throw(AssertionError(x)) if x['schema']!=1 or x['selected']!='crossbar' else None)(json.loads(page.locator('#json').inner_text())))
  case(prefix+' reset restores empty state',lambda: (page.locator('#reset').click(),assert_text(page,'#lostCount','0'),assert_contains(page,'#headline','Wybierz moduł')))
  case(prefix+' export without selection status',lambda: (page.locator('#export').click(),assert_contains(page,'#status','Najpierw wybierz')))
  case(prefix+' export after selection valid',lambda: (page.locator('[data-id="front_l"]').click(),page.locator('#export').click(),assert_contains(page,'#json','wheel_fl')))
  case(prefix+' ARIA status exists',lambda: (lambda x: (_ for _ in ()).throw(AssertionError(x)) if x!='polite' else None)(page.locator('#status').get_attribute('aria-live')))
  case(prefix+' no document horizontal overflow',lambda: (lambda o: (_ for _ in ()).throw(AssertionError(o)) if o>3 else None)(page.evaluate('document.documentElement.scrollWidth - window.innerWidth')))
  case(prefix+' no unexpected JS errors',lambda: (lambda n: (_ for _ in ()).throw(AssertionError(str(errors))) if n else None)(len(errors)))
  case(prefix+' SVG does not capture pointer',lambda: (lambda v: (_ for _ in ()).throw(AssertionError(v)) if v!='none' else None)(page.locator('#edges').evaluate('(el)=>getComputedStyle(el).pointerEvents')))
  case(prefix+' scanning ranking not blank',lambda: (lambda n: (_ for _ in ()).throw(AssertionError(n)) if n!=6 else None)(page.locator('#ranking li').count()))
  page.close()
 browser.close()
failed=sum(r['result']=='FAIL' for r in records)
output={'suite':'B01-12-STRUCTURE-IMPACT-001','runner':'chromium','passed':len(records)-failed,'failed':failed,'js_errors':errors,'cases':records}
(HERE/'browser-results.json').write_text(json.dumps(output,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(f'CHROMIUM {output["passed"]}/{len(records)} {"FAIL" if failed else "PASS"}, JS exceptions {len(errors)}')
sys.exit(1 if failed else 0)
