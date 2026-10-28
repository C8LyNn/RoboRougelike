#!/usr/bin/env python3
"""Executable PASS17 B01-12 repair planner Chromium smoke on exact unmodified HTML bytes. No Unity."""
from pathlib import Path
import json,hashlib,sys
from playwright.sync_api import sync_playwright
ROOT=Path(__file__).resolve().parent
SRC=ROOT/'garage-repair-triage.html'
MANIFEST=ROOT/'manifest.json'
expected=json.loads(MANIFEST.read_text(encoding='utf-8'))['sha256']['garage-repair-triage.html'] if MANIFEST.exists() else None
actual=hashlib.sha256(SRC.read_bytes()).hexdigest()
if expected is not None and expected!=actual:
 print(f'FAIL-CLOSED: HTML SHA256 expected {expected}, got {actual}', file=sys.stderr);sys.exit(2)
checks=[];errors=[]
def check(name,cond):
 ok=bool(cond);checks.append({'case':name,'pass':ok});
 if not ok:print('FAIL:',name)
 return ok

def labels(page):return page.locator('#parts input:checked').count()
def quote(page):return page.locator('#quote').inner_text()
def scrap(page):return page.locator('#scrap').inner_text()
def alert(page,kind):return page.locator('#'+kind+'Alert').get_attribute('data-blocked')
def selected(page):return page.locator('#parts input:checked').evaluate_all('(els)=>els.map(e=>e.dataset.part).sort()')
def fixture(page,k):page.locator('#fixture').select_option(k)
def click(page,k):page.locator('#'+k).click()

def suite(page,w,h):
 tag=f'{w}x{h}'
 page.set_viewport_size({'width':w,'height':h});fixture(page,'dual')
 check(tag+'/responsive_no_overflow',page.evaluate('document.documentElement.scrollWidth <= innerWidth'))
 check(tag+'/initial_two_critical',alert(page,'weapon')=='true' and alert(page,'drive')=='true')
 check(tag+'/two_critical_ax_visible','BRAK SPRAWNEJ BRONI' in page.locator('body').aria_snapshot() and 'NAPĘD NIESPRAWNY' in page.locator('body').aria_snapshot())
 check(tag+'/initial_wallet32',scrap(page)=='32 zł')
 click(page,'minimal');check(tag+'/minimal_three_parts',selected(page)==['gunA','wheelL','wheelR'])
 check(tag+'/minimal_quote23',quote(page)=='23 zł' and page.locator('#balance').inner_text()=='9 zł')
 check(tag+'/planned_ready',page.locator('#future').inner_text()=='GOTOWY' and alert(page,'drive')=='true')
 check(tag+'/no_premature_spend',scrap(page)=='32 zł')
 click(page,'commit');check(tag+'/atomic_commit_wallet9',scrap(page)=='9 zł' and quote(page)=='0 zł')
 check(tag+'/after_commit_ready',alert(page,'weapon')=='false' and alert(page,'drive')=='false')
 check(tag+'/after_commit_hp_repaired','HP 100/100' in page.locator('[data-part="gunA"]').first.inner_text())
 click(page,'undo');check(tag+'/undo_restores_wallet_hp',scrap(page)=='32 zł' and alert(page,'weapon')=='true' and selected(page)==['gunA','wheelL','wheelR'])
 click(page,'redo');check(tag+'/redo_restores_exact_state',scrap(page)=='9 zł' and alert(page,'drive')=='false')
 fixture(page,'budget');click(page,'minimal')
 check(tag+'/budget_still_quote23',quote(page)=='23 zł' and scrap(page)=='6 zł')
 check(tag+'/budget_reject_disabled',page.locator('#commit').is_disabled() and page.locator('#balance').inner_text()=='-17 zł')
 page.evaluate("document.querySelector('#commit').click()")
 check(tag+'/budget_no_partial_change',scrap(page)=='6 zł' and alert(page,'weapon')=='true' and alert(page,'drive')=='true')
 fixture(page,'tracks');click(page,'minimal')
 check(tag+'/tracks_one_side_critical',selected(page)==['trackR'] and quote(page)=='16 zł' and alert(page,'drive')=='true')
 click(page,'commit');check(tag+'/tracks_restored',alert(page,'drive')=='false' and scrap(page)=='16 zł')
 fixture(page,'hover');click(page,'minimal');check(tag+'/hover_one_repair',selected(page)==['hover'] and quote(page)=='13 zł')
 fixture(page,'healthy');click(page,'minimal');check(tag+'/already_ready_zero_minimal',labels(page)==0 and page.locator('#future').inner_text()=='GOTOWY')
 click(page,'all');check(tag+'/maintenance_three_parts',selected(page)==['core','wheelL','wheelR'])
 check(tag+'/maintenance_quote_valid',quote(page)=='6 zł')
 # strict portable plan: export followed by round-trip of selected parts, without changing economy
 click(page,'export');data=page.locator('#planJson').input_value()
 check(tag+'/export_schema',json.loads(data)=={'schema':'B01-12-REPAIR-PLAN','version':1,'drive':'wheels','selected':['core','wheelL','wheelR']})
 click(page,'clear');page.locator('#planJson').fill(data);click(page,'import')
 check(tag+'/import_restores_only_plan',selected(page)==['core','wheelL','wheelR'] and scrap(page)=='24 zł')
 for kind,bad in [('unknown','{"schema":"B01-12-REPAIR-PLAN","version":1,"drive":"wheels","selected":["evil"]}'),('duplicate','{"schema":"B01-12-REPAIR-PLAN","version":1,"drive":"wheels","selected":["core","core"]}'),('wrongdrive','{"schema":"B01-12-REPAIR-PLAN","version":1,"drive":"hover","selected":["core"]}'),('malformed','{'),('extra','{"schema":"B01-12-REPAIR-PLAN","version":1,"drive":"wheels","selected":["core"],"scrap":999}')]:
  before=(selected(page),scrap(page),quote(page))
  page.locator('#planJson').fill(bad);click(page,'import')
  check(tag+'/invalid_'+kind+'_atomic_reject',(selected(page),scrap(page),quote(page))==before and 'ODRZUCONO import' in page.locator('#eventLog').inner_text())
 # keyboard checkbox focus retains after reconstructed table
 fixture(page,'tracks');cb=page.locator('input[data-part="trackR"]');cb.focus();page.keyboard.press('Space')
 check(tag+'/keyboard_focus_retained',page.evaluate("document.activeElement?.dataset?.part")=='trackR' and selected(page)==['trackR'])
 check(tag+'/final_responsive_no_overflow',page.evaluate('document.documentElement.scrollWidth <= innerWidth'))
 return

with sync_playwright() as p:
 browser=p.chromium.launch(headless=True,executable_path='/usr/bin/chromium',args=['--no-sandbox'])
 for width,height in [(320,720),(768,1024),(1280,720),(1920,1080)]:
  page=browser.new_page(viewport={'width':width,'height':height});page.on('pageerror',lambda e:errors.append(str(e)));page.set_content(SRC.read_text(encoding="utf-8"),wait_until="load");suite(page,width,height)
  if width in (1280,1920):
   fixture(page,'dual');click(page,'minimal');click(page,'export')
   page.screenshot(path=str(ROOT/'evidence'/f'repair-plan-{width}x{height}.png'),full_page=True)
  page.close()
 browser.close()
result={'source_sha256':actual,'browser':'Chromium Playwright Linux /usr/bin/chromium','test_count':len(checks),'pass':sum(x['pass'] for x in checks),'failed':[x['case'] for x in checks if not x['pass']],'page_errors':errors,'checks':checks,'native_unity':'NOT_RUN'}
(ROOT/'qa-results.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k!='checks'},ensure_ascii=False,indent=2))
sys.exit(0 if result['pass']==len(checks) and not errors else 1)
