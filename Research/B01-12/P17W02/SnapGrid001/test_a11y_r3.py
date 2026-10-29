#!/usr/bin/env python3
"""Independent-scenario producer browser accessibility bugfix test. Not Unity."""
import hashlib,json,sys
from pathlib import Path
from playwright.sync_api import sync_playwright
ROOT=Path(__file__).resolve().parent
SRC=ROOT/'snapgrid.html'
MAN=ROOT/'manifest.json'
m=json.loads(MAN.read_text(encoding='utf-8'))
b=SRC.read_bytes()
h=hashlib.sha256(b).hexdigest()
if h!=m['html_sha256'] or len(b)!=m['source_size']:
 print('FAIL_CLOSED source mismatch',file=sys.stderr);sys.exit(2)
old="${pset.has(key)?valid?' — montaż możliwy':' — operacja zablokowana':''}"
new="${pset.has(key)?valid?(tool==='remove'?' — demontaż możliwy':' — montaż możliwy'):' — operacja zablokowana':''}"
assert b.decode().count(new)==1
old_source=b.decode().replace(new,old).replace('B01-12-SNAPGRID-001/R3 ·','B01-12-SNAPGRID-001/R1 ·')
old_bytes=old_source.encode('utf-8')
assert hashlib.sha1(b'blob '+str(len(old_bytes)).encode()+b'\0'+old_bytes).hexdigest()=='1ee408a1532d07c3a83437780e2d87174a6b2656', 'immutable R2 reconstruction failed'
checks=[];errors=[]
def check(case, cond):
 checks.append({'case':case,'pass':bool(cond)})
 if not cond: print('FAILED',case,file=sys.stderr)
def t(page,x,y): return page.locator(f'button.tile[data-x="{x}"][data-y="{y}"]')
def focus_tile(page,x,y):
 page.evaluate("([x,y])=>{const b=document.querySelector(`button.tile[data-x=\"${x}\"][data-y=\"${y}\"]`);b.focus();b.dispatchEvent(new Event(\"mouseenter\"))}",[x,y])
with sync_playwright() as p:
 browser=p.chromium.launch(headless=True,executable_path='/usr/bin/chromium',args=['--no-sandbox'])
 for w,hh in [(320,720),(768,1024),(1280,720),(1920,1080)]:
  tag=str(w)
  old_page=browser.new_page(viewport={'width':w,'height':hh})
  old_page.set_content(old_source)
  old_page.locator('#demo').click();old_page.locator('#tool').select_option('remove')
  old_page.mouse.move(1,1);old_page.wait_for_timeout(30)
  t(old_page,8,3).focus()
  check(tag+'/old_wrong_removal_aria_reproduced',t(old_page,8,3).get_attribute('aria-label').endswith('— montaż możliwy'))
  old_page.close()
  page=browser.new_page(viewport={'width':w,'height':hh})
  page.on('pageerror',lambda e:errors.append(str(e)))
  page.set_content(b.decode())
  page.locator('#demo').click();page.locator('#tool').select_option('remove')
  page.mouse.move(1,1);page.wait_for_timeout(30)
  t(page,8,3).focus()
  check(tag+'/remove_ready_aria',t(page,8,3).get_attribute('aria-label').endswith('— demontaż możliwy'))
  check(tag+'/remove_preview_green',t(page,8,3).get_attribute('data-preview')=='good')
  check(tag+'/accessible_snapshot', 'demontaż możliwy' in t(page,8,3).aria_snapshot())
  page.mouse.move(1,1);t(page,6,3).focus()
  check(tag+'/blocked_bridge_aria',t(page,6,3).get_attribute('aria-label').endswith('— operacja zablokowana'))
  check(tag+'/blocked_bridge_visual',t(page,6,3).get_attribute('data-preview')=='bad')
  count_before=page.locator('#partCount').inner_text();t(page,6,3).press('Enter')
  check(tag+'/blocked_bridge_no_mutation',page.locator('#partCount').inner_text()==count_before)
  page.mouse.move(1,1);t(page,8,3).focus();t(page,8,3).press('Enter')
  check(tag+'/remove_enter',page.locator('#partCount').inner_text()=='4' and t(page,8,3).get_attribute('data-kind')=='empty')
  page.locator('#tool').select_option('armor');page.mouse.move(1,1);t(page,4,4).focus()
  check(tag+'/install_aria',t(page,4,4).get_attribute('aria-label').endswith('— montaż możliwy'))
  check(tag+'/no_stale_remove_aria',not ('demontaż możliwy' in t(page,4,4).get_attribute('aria-label')))
  check(tag+'/no_horizontal_overflow',page.evaluate('document.documentElement.scrollWidth<=window.innerWidth'))
  page.locator('#export').click();j=json.loads(page.locator('#plan').input_value())
  check(tag+'/export_matches_live_layout',len(j['parts'])==4)
  page.locator('#reset').click()
  check(tag+'/r2_export_stale_protection',page.locator('#plan').input_value()=='' and 'wyeksportuj' in page.locator('#exportStatus').inner_text())
  if w==1280:page.screenshot(path=str(ROOT/'snapgrid-r3-1280x720.png'),full_page=True)
  page.close()
 browser.close()
result={'package':'B01-12-SNAPGRID-001/R3','sha256':h,'total':len(checks),'pass':sum(c['pass'] for c in checks),'fail':[c['case'] for c in checks if not c['pass']],'js_errors':errors,'native_unity':'NOT_RUN','checks':checks}
(ROOT/'a11y-results.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k!='checks'},indent=2))
sys.exit(0 if not result['fail'] and not errors else 1)
