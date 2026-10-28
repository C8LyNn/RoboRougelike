#!/usr/bin/env python3
"""Actual original HTML Chromium DOM/keyboard/placement tests for B01-12 SNAPGRID-001.
No Unity, no game artifacts, test source uses sha256 pinned in manifest.json.
"""
from pathlib import Path
from playwright.sync_api import sync_playwright
import json,hashlib,sys
ROOT=Path(__file__).resolve().parent
html=ROOT/'snapgrid.html'
manifest=ROOT/'manifest.json'
expected=json.loads(manifest.read_text(encoding='utf-8'))['html_sha256'] if manifest.exists() else None
actual=hashlib.sha256(html.read_bytes()).hexdigest()
if expected and actual!=expected:
 print('FAIL CLOSED original HTML SHA256 mismatch',file=sys.stderr);sys.exit(2)
results=[];errors=[]
def assert_case(name,ok):
 ok=bool(ok);results.append({'case':name,'pass':ok})
 if not ok:print('FAIL',name)
 return ok

def tile(page,x,y):return page.locator(f'.tile[data-x="{x}"][data-y="{y}"]')
def count(page):return int(page.locator('#partCount').inner_text())
def status(page):return page.locator('#feedback').inner_text()
def select(page,k):page.locator('#tool').select_option(k)
def click(page,x,y):tile(page,x,y).click()
def run(page,w,h):
 tag=f'{w}x{h}'
 page.set_viewport_size({'width':w,'height':h})
 page.locator('#reset').click()
 assert_case(tag+'/no_horizontal_scroll',page.evaluate('document.documentElement.scrollWidth <= innerWidth'))
 assert_case(tag+'/initial_core',count(page)==1 and tile(page,5,3).get_attribute('data-kind')=='core')
 assert_case(tag+'/initial_critical',page.locator('#gunAlert').get_attribute('data-critical')=='true' and page.locator('#wheelAlert').get_attribute('data-critical')=='true')
 assert_case(tag+'/grid77',page.locator('.tile').count()==77)
 # real placement checks
 select(page,'armor');click(page,0,0)
 assert_case(tag+'/isolated_rejected',count(page)==1 and 'Brak styku' in status(page))
 click(page,5,3)
 assert_case(tag+'/overlap_rejected',count(page)==1 and 'Kolizja' in status(page))
 click(page,10,6)
 assert_case(tag+'/boundary_rejected',count(page)==1 and 'Poza obszarem' in status(page))
 click(page,6,3)
 assert_case(tag+'/armor_placed',count(page)==2 and tile(page,7,3).get_attribute('data-kind')=='armor')
 select(page,'gun');click(page,8,3)
 assert_case(tag+'/gun_bridge_connected',count(page)==3 and page.locator('#gunAlert').get_attribute('data-critical')=='false')
 select(page,'remove');click(page,6,3)
 assert_case(tag+'/bridge_removal_rejected',count(page)==3 and 'odłączon' in status(page))
 click(page,5,3)
 assert_case(tag+'/core_immutable',count(page)==3 and 'Rdzeń jest stały' in status(page))
 click(page,8,3)
 assert_case(tag+'/gun_removed',count(page)==2 and page.locator('#gunAlert').get_attribute('data-critical')=='true')
 click(page,7,3)
 assert_case(tag+'/armor_removed',count(page)==1)
 page.locator('#undo').click()
 assert_case(tag+'/undo_restores_armor',count(page)==2 and tile(page,7,3).get_attribute('data-kind')=='armor')
 page.locator('#redo').click()
 assert_case(tag+'/redo_removes_armor',count(page)==1)
 # keyboard navigation and rotation
 page.locator('#reset').click();select(page,'gun');tile(page,5,2).focus()
 page.keyboard.press('r')
 assert_case(tag+'/keyboard_rotate',page.locator('#angleLabel').inner_text()=='90°' and page.locator('#footprint').inner_text()=='2×1')
 page.keyboard.press('ArrowRight')
 assert_case(tag+'/keyboard_arrows',page.evaluate("document.activeElement?.dataset?.x")=='6')
 page.keyboard.press('ArrowLeft')
 page.keyboard.press('Enter')
 assert_case(tag+'/keyboard_place',count(page)==2 and tile(page,5,2).get_attribute('data-kind')=='gun')
 page.keyboard.press('Delete')
 assert_case(tag+'/keyboard_remove',count(page)==1)
 # demo, dual warnings and export
 page.locator('#demo').click()
 assert_case(tag+'/demo_parts',count(page)==5 and int(page.locator('#occupiedCount').inner_text())==7)
 assert_case(tag+'/demo_dual_ready',page.locator('#gunAlert').get_attribute('data-critical')=='false' and page.locator('#wheelAlert').get_attribute('data-critical')=='false')
 select(page,'remove');click(page,6,3)
 assert_case(tag+'/demo_branch_protected',count(page)==5 and 'odłączon' in status(page))
 page.locator('#export').click()
 obj=json.loads(page.locator('#plan').input_value())
 assert_case(tag+'/export_schema',obj['schema']=='B01-12-SNAPGRID-PLAN' and obj['version']==1 and obj['grid']=={'columns':11,'rows':7} and len(obj['parts'])==5)
 assert_case(tag+'/export_dimensions',obj['parts'][1]['footprint']=={'w':2,'h':1} and obj['parts'][2]['footprint']=={'w':1,'h':2})
 assert_case(tag+'/end_no_overflow',page.evaluate('document.documentElement.scrollWidth <= innerWidth'))
 if w in [1280,1920]:
  page.screenshot(path=str(ROOT/'evidence'/f'snapgrid-{w}x{h}.png'),full_page=True)

with sync_playwright() as p:
 browser=p.chromium.launch(executable_path='/usr/bin/chromium',headless=True,args=['--no-sandbox'])
 for w,h in [(320,720),(768,1024),(1280,720),(1920,1080)]:
  page=browser.new_page(viewport={'width':w,'height':h});page.on('pageerror',lambda e:errors.append(str(e)))
  page.set_content(html.read_text(encoding='utf-8'),wait_until='load')
  run(page,w,h);page.close()
 browser.close()
report={'sha256':actual,'total':len(results),'passed':sum(v['pass'] for v in results),'failed':[v['case'] for v in results if not v['pass']],'js_errors':errors,'environment':'Linux Chromium Playwright /usr/bin/chromium','native_unity':'NOT_RUN','checks':results}
(ROOT/'results.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k!='checks'},ensure_ascii=False,indent=2))
sys.exit(0 if report['passed']==report['total'] and not errors else 1)
