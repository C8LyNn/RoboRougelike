#!/usr/bin/env python3
"""Actual Chromium F3 0HP temporary-alert dismissal regression for B01-12 HUD3."""
from pathlib import Path
import hashlib,json,sys
from playwright.sync_api import sync_playwright
src=Path(__file__).resolve().parent/'HUD3-overflow.html'
checks=[]
def chk(label,val):
 checks.append((label,bool(val)))
 if not val:raise AssertionError(label)
def main():
 b=src.read_bytes();problems=[]
 with sync_playwright() as p:
  browser=p.chromium.launch(headless=True,executable_path='/usr/bin/chromium',args=['--no-sandbox'])
  for w in (320,720,1280,1920):
   page=browser.new_page(viewport={'width':w,'height':720})
   page.on('pageerror',lambda e:problems.append(str(e)))
   page.set_content(b.decode())
   page.locator('#fixture').select_option('F3')
   chk(f'{w} F3 baseline destroyed',page.locator('[data-status="DESTROYED"]').count()==1)
   page.locator('#resolveA').click()
   chk(f'{w} F3 remains destroyed',page.locator('[data-status="DESTROYED"]').count()==1)
   chk(f'{w} F3 not counted operational', 'Sprawne: 0' in page.locator('#groups').inner_text() and 'Mogą teraz strzelać: 0' in page.locator('#groups').inner_text())
   chk(f'{w} F3 message requires rebuild','wymaga jawnej odbudowy' in page.locator('#resolveMessage').inner_text())
   page.locator('#fixture').select_option('F1')
   page.locator('#resolveA').click()
   chk(f'{w} F1 temporary HEAT recoverable','PRZEGRZANIE' not in page.locator('#groups').inner_text() and 'ZACIĘCIE' in page.locator('#groups').inner_text())
   page.locator('#resolveB').click()
   chk(f'{w} F1 JAM recoverable','ZACIĘCIE' not in page.locator('#groups').inner_text())
   page.locator('#fixture').select_option('F2');page.locator('#modeB').click()
   chk(f'{w} F2 EMP accessible before disclosure','EMP' in page.locator('#groups').aria_snapshot())
   chk(f'{w} no horizontal overflow',page.evaluate('document.documentElement.scrollWidth <= innerWidth+1'))
   page.close()
  browser.close()
 chk('No browser JS errors',not problems)
 obj={'scope':'original research HTML Chromium, no Unity','source_sha256':hashlib.sha256(b).hexdigest(),'passed':sum(t[1] for t in checks),'total':len(checks),'checks':checks,'pageerrors':problems}
 (src.parent/'hud3-f3-results.json').write_text(json.dumps(obj,ensure_ascii=False,indent=2)+'\n')
 print(obj['passed'],obj['total'],obj['source_sha256'])
if __name__=='__main__':
 try:main()
 except Exception as e:print('FAIL',e,file=sys.stderr);sys.exit(1)
