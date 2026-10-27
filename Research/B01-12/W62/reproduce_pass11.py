#!/usr/bin/env python3
"""B01-12: reproducible Chromium AX/keyboard/reflow receiver, pinned to original D03 HTML.
Research-only. No game Unity/PhysX or participant claims.
Run from repo root: python Research/B01-12/W62/reproduce_pass11.py
"""
from pathlib import Path
import hashlib, json, sys
from playwright.sync_api import sync_playwright

SOURCE = Path(__file__).resolve().parents[1] / 'W50' / 'D03-critical-warnings.html'
EXPECTED = {'sha256':'e1cd125765c1ce8b625a1fd5036d4a7c3777b612ed43f847dc2a93d215fe8573',
            'gitblob':'d5d71cd01c33b544704ea041f887efb1dd32952d'}
checks=[]
def expect(name, condition):
    checks.append({'name':name,'pass':bool(condition)})
    if not condition: raise AssertionError(name)

def main():
    b=SOURCE.read_bytes(); sha=hashlib.sha256(b).hexdigest(); blob=hashlib.sha1(b'blob '+str(len(b)).encode()+b'\0'+b).hexdigest()
    expect('source SHA256',sha==EXPECTED['sha256']);expect('source Git blob',blob==EXPECTED['gitblob'])
    errors=[]
    with sync_playwright() as p:
        browser=p.chromium.launch(headless=True,executable_path='/usr/bin/chromium',args=['--no-sandbox'])
        for w in [240,320,390,1280,1920]:
            page=browser.new_page(viewport={'width':w,'height':720})
            page.on('pageerror',lambda e:errors.append(str(e)))
            page.set_content(b.decode("utf-8"),wait_until="load")
            page.locator('#viewB').click();page.locator('#participantMode').check()
            ax=page.locator('body').aria_snapshot()
            expect(f'{w} B count accessible','ROBOT NIEGOTOWY — 2 KRYTYCZNE PROBLEMY' in ax)
            expect(f'{w} B answer hidden','Broń: BRAK SPRAWNEJ' not in ax and 'Mobilność: BRAK SPRAWNEJ' not in ax)
            expect(f'{w} facilitator inaccessible','Brak obu' not in ax and 'Tryb uczestnika' not in ax)
            cdp=page.context.new_cdp_session(page)
            raw=' '.join(str(n.get('name',{}).get('value','')) for n in cdp.send('Accessibility.getFullAXTree')['nodes'] if not n.get('ignored'))
            expect(f'{w} CDP AX no pre-answer','Broń: BRAK SPRAWNEJ' not in raw and 'Mobilność: BRAK SPRAWNEJ' not in raw)
            expect(f'{w} collapsed no overflow',page.evaluate('document.documentElement.scrollWidth<=innerWidth+1'))
            page.keyboard.press('Tab')
            expect(f'{w} focus disclosure',page.evaluate('document.activeElement?.id')=='expandAll')
            page.keyboard.press('Space')
            expect(f'{w} disclosure open',page.locator('#expandAll').get_attribute('aria-expanded')=='true')
            ax=page.locator('body').aria_snapshot()
            expect(f'{w} expanded reveals both','Broń: BRAK SPRAWNEJ' in ax and 'Mobilność: BRAK SPRAWNEJ' in ax)
            expect(f'{w} expanded no overflow',page.evaluate('document.documentElement.scrollWidth<=innerWidth+1'))
            page.keyboard.press('Space')
            expect(f'{w} disclosure closed',page.locator('#expandAll').get_attribute('aria-expanded')=='false')
            page.keyboard.press('Escape')
            expect(f'{w} ESC restores moderator',page.locator('#moderatorControls').is_visible() and page.evaluate('document.activeElement?.id')=='participantMode')
            page.locator('#viewA').click();page.locator('#participantMode').check()
            ax=page.locator('body').aria_snapshot()
            expect(f'{w} A shows both','Broń: BRAK SPRAWNEJ' in ax and 'Mobilność: BRAK SPRAWNEJ' in ax)
            page.close()
        browser.close()
    expect('no Chromium JavaScript exception',not errors)
    result={'scope':'original HTML browser, not Unity or humans','source_sha256':sha,'gitblob':blob,
            'passed':sum(x['pass'] for x in checks),'total':len(checks),'checks':checks,'page_errors':errors}
    print(json.dumps(result,ensure_ascii=False,indent=2))
    return 0

if __name__=='__main__':
    try:sys.exit(main())
    except Exception as exc:
        print(json.dumps({'status':'FAIL','checks':checks,'error':str(exc)},ensure_ascii=False),file=sys.stderr)
        sys.exit(1)
