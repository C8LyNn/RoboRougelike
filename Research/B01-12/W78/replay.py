#!/usr/bin/env python3
"""B01-12 P16 reproducible Chromium garage-navigation smoke; NOT Unity."""
import hashlib,json,sys
from pathlib import Path
from playwright.sync_api import sync_playwright
ROOT=Path(__file__).resolve().parent
SRC=ROOT/'garage-navigation.html'
SHA='7548c18a86ef07736d551ba24c587f3842206705c80fbc41353ab1c0450dc4e4'
checks=[]
def verify(name,ok):
    checks.append({'case':name,'pass':bool(ok)})
    if not ok:raise AssertionError(name)
def run():
    b=SRC.read_bytes()
    verify('original HTML SHA256',hashlib.sha256(b).hexdigest()==SHA)
    errors=[]
    with sync_playwright() as p:
        browser=p.chromium.launch(headless=True,executable_path='/usr/bin/chromium',args=['--no-sandbox'])
        for w,h in [(320,720),(1280,720),(1920,1080)]:
            page=browser.new_page(viewport={'width':w,'height':h})
            page.on('pageerror',lambda e:errors.append(str(e)))
            page.set_content(b.decode('utf-8'))
            q=lambda s:page.locator(s)
            verify(f'{w} dual critical warnings initially',set(q('#warnings .code').all_inner_texts())=={'CRITICAL_WEAPON','CRITICAL_MOBILITY'})
            q('[data-category="weapon"]').click()
            verify(f'{w} category filter',q('#partControls article').count()==1)
            q('[data-slot="gun"] [data-action="toggle"]').click()
            q('[data-slot="gun"] [data-action="damage"]').click()
            verify(f'{w} 0HP stays critical','CRITICAL_WEAPON' in q('#warnings').inner_text())
            q('[data-slot="gun"] [data-action="toggle"]').click()
            q('[data-slot="gun"] [data-action="toggle"]').click()
            verify(f'{w} no free repair after remount','0 HP' in q('[data-slot="gun"] [data-status]').inner_text() and q('#wallet').inner_text()=='220')
            q('[data-slot="gun"] [data-action="repair"]').click()
            verify(f'{w} repair billed and focus',q('#wallet').inner_text()=='209' and page.evaluate('document.activeElement?.dataset.action')=='damage')
            q('[data-category="mobility"]').click()
            q('#drive').select_option('tracks')
            verify(f'{w} schematic names selected tracks',q('#schematic [data-piece="trackL"]').count()==1 and q('#schematic [data-piece="trackR"]').count()==1)
            q('[data-slot="trackL"] [data-action="toggle"]').click()
            q('[data-slot="trackR"] [data-action="toggle"]').click()
            verify(f'{w} paired tracks ready','CRITICAL_MOBILITY' not in q('#warnings').inner_text())
            q('[data-slot="trackR"] [data-action="damage"]').click()
            verify(f'{w} damaged right track red and warning', '0 HP' in q('#schematic [data-piece="trackR"]').inner_text() and 'CRITICAL_MOBILITY' in q('#warnings').inner_text())
            q('#drive').select_option('wheels')
            verify(f'{w} switching visual drive',q('#schematic [data-piece="trackL"]').count()==0 and q('#schematic [data-piece="left"]').count()==1)
            verify(f'{w} no horizontal overflow',page.evaluate('document.documentElement.scrollWidth<=innerWidth+1'))
            page.close()
        browser.close()
    verify('no JS page errors',not errors)
    print(json.dumps({'total':len(checks),'passed':sum(c['pass'] for c in checks),'errors':errors,'scope':'Chromium only','checks':checks},ensure_ascii=False,indent=2))
if __name__=='__main__':
    try:run()
    except Exception as e:
        print(json.dumps({'error':str(e),'checks':checks},ensure_ascii=False),file=sys.stderr)
        sys.exit(1)
