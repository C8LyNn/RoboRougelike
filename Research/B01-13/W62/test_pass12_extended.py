"""W62 PASS12 — browser test real original B01-13 HTML (human and Unity NOT_RUN)."""
import json, re,sys,shutil
from pathlib import Path
from playwright.sync_api import sync_playwright
TARGET=Path(sys.argv[1] if len(sys.argv)>1 else Path(__file__).with_name('HPT13-scoring.html'))
checks=[]
def check(label,assertion):
    checks.append((label,bool(assertion)))
    if not assertion: raise AssertionError(label)
fixture=json.loads(Path(__file__).with_name('HPT-T05-MAP-v2.json').read_text())
check('T05 import file: v2, global floor, one kind=Wall',fixture['version']==2 and fixture['globalFloor'] and len(fixture['items'])==1 and fixture['items'][0]['kind']==1)
check('T05 occupied cell (0,0), scale 1, yaw0', all(fixture['items'][0][k]==v for k,v in dict(x=0,z=0,rotation=0,scaleX=1,scaleY=1,scaleZ=1).items()))
with sync_playwright() as p:
  browser=p.chromium.launch(executable_path=(shutil.which('chromium') or shutil.which('chromium-browser') or '/usr/bin/chromium'),headless=True,args=['--no-sandbox'])
  page=browser.new_page(viewport={'width':1280,'height':720})
  errors=[];page.on('pageerror',lambda e: errors.append(str(e)))
  page.set_content(TARGET.read_text())
  task=page.evaluate('HPT13.TASKS')
  checks.extend([('T03 actual missing gun and drive functional fixture',('rdzeń' in task[2]['obs'] and 'napędu' in task[2]['obs'])),('T04 empty draft with original 3 required components',all(s in task[3]['obs'] for s in ['pusty MapDraft','Spawn','Finish','Ground','DeployError'])),('T04 resume via Esc pause', 'Esc' in task[3]['obs'] and 'Wróć do edytora map' in task[3]['obs']),('T05 explicit source-grounded same-category Wall rejection/recovery', 'CanPlace=false' in task[4]['obs'] and 'x=3,z=0' in task[4]['obs'])])
  page.locator('#pid').fill('P01');page.locator('#sid').fill('S01')
  t=page.locator('#HPT-T01')
  for sel,value in [('outcome','UNAIDED_SUCCESS'),('duration_seconds','80'),('attempts','1'),('errors','0'),('help_count','0'),('comprehension_0_2','2')]:
    t.locator('[data-field="'+sel+'"]').select_option(value) if sel in ('outcome','comprehension_0_2') else t.locator('[data-field="'+sel+'"]').fill(value)
  page.locator('#save').click()
  check('first synthetic session saves five rows',page.evaluate('HPT13.saved.length')==5)
  page.locator('#sid').fill('S02');page.locator('#buildsha').fill('f'*40)
  page.locator('#save').click()
  check('different source SHA rejected',page.evaluate('HPT13.saved.length')==5 and 'inny build SHA' in page.locator('#feedback').inner_text())
  page.locator('#buildsha').fill('7299dcc5fd63ac042a0ffcd97b69d02fb8744649');page.locator('#zipsha').fill('e'*64)
  page.locator('#save').click()
  check('different ZIP SHA rejected',page.evaluate('HPT13.saved.length')==5)
  page.locator('#zipsha').fill('ec3193bb3ac24398c496ea65ad12aa653321db160a4dc74e8f618e3cc7ad6d1d')
  page.locator('#save').click()
  check('matching build session 2 allowed',page.evaluate('HPT13.saved.length')==10)
  check('human summary excludes synthetic ten rows',page.locator('#summary tbody tr').first.locator('td').nth(2).inner_text()=='0')
  page.locator('#clear').click();page.locator('#sid').fill('S03');page.locator('#buildsha').fill('f'*40);page.locator('#zipsha').fill('e'*64)
  page.locator('#save').click()
  check('new build allowed after explicit clear',page.evaluate('HPT13.saved.length')==5)
  check('no JS exceptions',not errors)
  browser.close()
assert all(x[1] for x in checks)
for desc,val in checks: print('PASS' if val else 'FAIL',desc)
print(f'RESULT: {len(checks)}/{len(checks)} pass; browser validation, NOT game/native/human')
