#!/usr/bin/env python3
"""Real headless Chromium: functional research garage UI, not actual Unity/C# tests."""
from pathlib import Path
import json, hashlib
from playwright.sync_api import sync_playwright

ROOT=Path(__file__).resolve().parent
SOURCE=ROOT/'garage-readiness.html'
HTML=SOURCE.read_text(encoding='utf-8')
EXPECTED_SHA256='933041ee92ae062ab6533e107bedc56eeec2884c661b79c924f604c5fddb5eef'
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==EXPECTED_SHA256, 'Original research HTML changed: pin source before retesting'
LOG=[]
def ck(label, truth):
    x=bool(truth); LOG.append({'label':label,'pass':x})
    if not x:raise AssertionError(label)
def issues(page):return page.locator('.critical')
def codes(page):return page.locator('.critical .code').all_text_contents()
def slot(page,key,action):page.locator(f'[data-slot="{key}"] button[data-action="{action}"]').click()

def run():
  errors=[]
  with sync_playwright() as p:
    browser=p.chromium.launch(headless=True,executable_path='/usr/bin/chromium',args=['--no-sandbox'])
    for width in (320,390,720,1280,1920):
      page=browser.new_page(viewport={'width':width,'height':900})
      page.on('pageerror',lambda err: errors.append(str(err)))
      page.set_content(HTML,wait_until='load')
      ck(f'{width} default critical count=2',issues(page).count()==2)
      ck(f'{width} both distinct visible',set(codes(page))=={'CRITICAL_WEAPON','CRITICAL_MOBILITY'})
      ck(f'{width} tips initially OFF and critical persistent',not page.locator('#tips').is_checked() and page.locator('#optional').is_hidden() and issues(page).count()==2)
      ck(f'{width} initial NOT READY', 'NOT READY' in page.locator('#topStatus').inner_text())
      ck(f'{width} accessible source warnings', 'Broń: brak sprawnej broni' in page.locator('#warnings').aria_snapshot() and 'Mobilność: brak sprawnego wybranego napędu' in page.locator('#warnings').aria_snapshot())
      ck(f'{width} no horizontal scroll',page.evaluate('document.documentElement.scrollWidth<=innerWidth+1'))
      page.locator('#checkStart').click()
      ck(f'{width} preview start blocked, two reasons', 'CRITICAL_WEAPON, CRITICAL_MOBILITY' in page.locator('#outcome').inner_text())
      ck(f'{width} preview no actual player', 'niczego nie uruchomiono' in page.locator('#outcome').inner_text().lower())
      slot(page,'gun','toggle')
      ck(f'{width} weapon installation leaves mobility',codes(page)==['CRITICAL_MOBILITY'])
      slot(page,'left','toggle');slot(page,'right','toggle')
      ck(f'{width} restore wheel pair gives READY',issues(page).count()==0 and 'READY' in page.locator('#topStatus').inner_text())
      slot(page,'right','damage')
      ck(f'{width} one side wheel HP0 loses mobility',codes(page)==['CRITICAL_MOBILITY'])
      slot(page,'right','repair')
      ck(f'{width} wheel repair returns READY',issues(page).count()==0)
      page.locator('#tips').check()
      ck(f'{width} optional hints ON',page.locator('#optional').is_visible())
      page.locator('#tips').uncheck()
      ck(f'{width} hints OFF again, critical render not cleared',page.locator('#optional').is_hidden() and issues(page).count()==0)
      page.locator('#drive').select_option('tracks')
      ck(f'{width} selected drive missing tracks',codes(page)==['CRITICAL_MOBILITY'])
      slot(page,'trackL','toggle');slot(page,'trackR','toggle')
      ck(f'{width} both tracks health make mobile',issues(page).count()==0)
      page.locator('#drive').select_option('omni')
      ck(f'{width} omni requires own part',codes(page)==['CRITICAL_MOBILITY'])
      slot(page,'omni','toggle')
      ck(f'{width} own omni functional',issues(page).count()==0)
      page.locator('#drive').select_option('hover')
      ck(f'{width} hover requires own part',codes(page)==['CRITICAL_MOBILITY'])
      slot(page,'hover','toggle')
      ck(f'{width} hover functional after mount',issues(page).count()==0)
      page.locator('#connection').check()
      ck(f'{width} structural independent',codes(page)==['CRITICAL_STRUCTURE'])
      page.locator('#preset').select_option('disconnected')
      ck(f'{width} disconnected preset preserves standalone third issue',codes(page)==['CRITICAL_STRUCTURE'])
      page.locator('#preset').select_option('zero')
      ck(f'{width} 0HP weapon and one-sided wheel = both',set(codes(page))=={'CRITICAL_WEAPON','CRITICAL_MOBILITY'})
      page.locator('#preset').select_option('ready')
      ck(f'{width} ready preset no fake warning',issues(page).count()==0)
      page.locator('#preset').select_option('mobility')
      ck(f'{width} mobility-only warns weapon',codes(page)==['CRITICAL_WEAPON'])
      page.locator('#backEditor').click()
      ck(f'{width} back editor allowed in unsafe state','Powrót do edytora' in page.locator('#outcome').inner_text())
      page.locator('#preset').select_option('both')
      page.locator('#tips').check()
      ck(f'{width} critical remain with hints ON',set(codes(page))=={'CRITICAL_WEAPON','CRITICAL_MOBILITY'})
      # Controlled negative: deliberate hiding one essential reason must be detected as broken, then re-render recovers
      page.evaluate("document.querySelector('[data-issue=\"mobility\"]').remove()")
      ck(f'{width} controlled negative detects missing mobility reason','CRITICAL_MOBILITY' not in codes(page))
      page.locator('#tips').uncheck()
      ck(f'{width} render restores independent critical reason',set(codes(page))=={'CRITICAL_WEAPON','CRITICAL_MOBILITY'})
      ck(f'{width} no overflow after changes',page.evaluate('document.documentElement.scrollWidth<=innerWidth+1'))
      ck(f'{width} game script did not throw',len(errors)==0)
      if width in (320,720,1280):
        page.screenshot(path=str(ROOT/'evidence'/f'garage-dual-{width}.png'),full_page=True)
      page.close()
    browser.close()
  out={'kind':'ACTUAL HEADLESS CHROMIUM PAGE.INTERACTIONS, NOT UNITY',
      'input_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
      'total':len(LOG),'passed':sum(x['pass'] for x in LOG),'failed':[x for x in LOG if not x['pass']],
      'checks':LOG,'javascript_errors':errors}
  (ROOT/'evidence'/'test_results.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
  print(json.dumps({k:v for k,v in out.items() if k!='checks'},ensure_ascii=False,indent=2))
if __name__=='__main__':run()
