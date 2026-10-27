#!/usr/bin/env python3
"""Research-only, original HTML real-Chromium negative+positive regression.

Run: python3 Research/B01-12/W66/test_d03_session_reset.py
Optional: --baseline-html <prior-PASS10-HTML-at-c7ce17d0> verifies old bug is RED.
No Unity/people/production GUI claims.
"""
from __future__ import annotations
import argparse, hashlib, json, sys
from pathlib import Path
from playwright.sync_api import sync_playwright

SRC = Path(__file__).resolve().parents[1] / 'W50' / 'D03-critical-warnings.html'
EXPECTED_SHA256 = '5aaece056ffed04985553c9607b2e0fafb4198bfa12c8789fe502607e51f1f11'
EXPECTED_GIT_BLOB = '889dbd70137aa7eebc0d8b82d01b3bab6ff6ca1d'
BASELINE_SHA256 = 'e1cd125765c1ce8b625a1fd5036d4a7c3777b612ed43f847dc2a93d215fe8573'
WIDTHS = [240, 320, 390, 720, 1080, 1280, 1920]
checks = []
errors = []

def verify(label, condition):
    ok = bool(condition)
    checks.append({'case':label,'pass':ok})
    if not ok: raise AssertionError(label)

def blob_sha(b):
    return hashlib.sha1(b'blob ' + str(len(b)).encode('ascii') + b'\0' + b).hexdigest()

def accessible(page):
    return page.locator('body').aria_snapshot()

def hidden_answers(page):
    s=accessible(page)
    return ('Broń: BRAK SPRAWNEJ' not in s and 'Mobilność: BRAK SPRAWNEJ' not in s and 'Konstrukcja: ODŁĄCZONA' not in s)

def browser_tests(p, html):
    browser=p.chromium.launch(executable_path='/usr/bin/chromium',headless=True,args=['--no-sandbox'])
    for w in WIDTHS:
        page=browser.new_page(viewport={'width':w,'height':844 if w<400 else 720})
        page.on('pageerror',lambda e:errors.append(str(e)))
        page.set_content(html,wait_until='load')
        page.locator('#viewB').click()
        page.locator('#participantMode').check()
        verify(f'{w} initial B collapsed',page.locator('#expandAll').get_attribute('aria-expanded')=='false')
        verify(f'{w} initial B hides answers',hidden_answers(page))
        page.keyboard.press('Tab')
        verify(f'{w} focus expand',page.evaluate('document.activeElement?.id')=='expandAll')
        page.keyboard.press('Space')
        verify(f'{w} B expanded',page.locator('#expandAll').get_attribute('aria-expanded')=='true')
        verify(f'{w} expanded both visible','Broń: BRAK SPRAWNEJ' in accessible(page) and 'Mobilność: BRAK SPRAWNEJ' in accessible(page))
        page.keyboard.press('Escape')
        verify(f'{w} ESC moderator focus',not page.locator('#participantMode').is_checked() and page.evaluate('document.activeElement?.id')=='participantMode')
        page.keyboard.press('Space')  # focused checkbox: enter participant again, no moderator click
        verify(f'{w} re-enter in B',page.locator('#participantMode').is_checked())
        verify(f'{w} re-enter B collapsed',page.locator('#expandAll').get_attribute('aria-expanded')=='false' and not page.locator('#allReasons').is_visible())
        verify(f'{w} re-enter answers hidden',hidden_answers(page))
        verify(f'{w} re-enter focus warnings',page.evaluate('document.activeElement?.id')=='warnings')
        verify(f'{w} no horizontal overflow',page.evaluate('document.documentElement.scrollWidth<=innerWidth+1'))
        # test that opening changes accessible tree and hides next session on DOM re-render
        page.keyboard.press('Tab');page.keyboard.press('Enter')
        verify(f'{w} keyboard re-open',page.locator('#expandAll').get_attribute('aria-expanded')=='true')
        page.keyboard.press('Escape')
        # New subject, different scenario; switching preset forces render, should still be neutral
        page.locator('[data-preset="driveOnly"]').click()
        page.locator('#participantMode').check()
        verify(f'{w} fixture changes without state leak',page.locator('#expandAll').get_attribute('aria-expanded')=='false' and 'ROBOT NIEGOTOWY' in accessible(page))
        verify(f'{w} one blocker remains concealed', 'Mobilność: BRAK SPRAWNEJ' not in accessible(page) and 'Broń: BRAK SPRAWNEJ' not in accessible(page))
        page.close()
    browser.close()

def baseline_negative(p, old):
    """Expected-RED original negative fixture: old source must disclose on session-2."""
    assert hashlib.sha256(old).hexdigest() == BASELINE_SHA256, 'not original PASS10 file'
    browser=p.chromium.launch(executable_path='/usr/bin/chromium',headless=True,args=['--no-sandbox'])
    page=browser.new_page()
    page.set_content(old.decode(),wait_until='load')
    page.locator('#viewB').click();page.locator('#participantMode').check()
    page.locator('#expandAll').click();page.keyboard.press('Escape')
    page.locator('#participantMode').check()
    exposed=page.locator('#allReasons').is_visible() and 'Broń: BRAK SPRAWNEJ' in accessible(page)
    browser.close()
    return {'original_unchanged_sha256':BASELINE_SHA256,'original_old_second_session_leak_reproduced':exposed,
            'verdict':'EXPECTED_RED' if exposed else 'UNEXPECTED_NOT_RED'}

def main():
    args=argparse.ArgumentParser()
    args.add_argument('--baseline-html',type=Path)
    args.add_argument('--json-out',type=Path)
    x=args.parse_args()
    data=SRC.read_bytes()
    verify('original updated SHA256 pinned',hashlib.sha256(data).hexdigest()==EXPECTED_SHA256)
    verify('original updated git blob pinned',blob_sha(data)==EXPECTED_GIT_BLOB)
    result={'scope':'research HTML real Chromium; NOT native Unity or humans','source_sha256':EXPECTED_SHA256,'git_blob':EXPECTED_GIT_BLOB}
    with sync_playwright() as p:
        if x.baseline_html:
            result['pre_fix_negative']=baseline_negative(p,x.baseline_html.read_bytes())
            verify('old original expected-RED reproduction',result['pre_fix_negative']['original_old_second_session_leak_reproduced'])
        browser_tests(p,data.decode('utf-8'))
    verify('0 JS exceptions',len(errors)==0)
    result.update({'passed':sum(t['pass'] for t in checks),'total':len(checks),'checks':checks,'page_errors':errors})
    if x.json_out:x.json_out.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps({'passed':result['passed'],'total':result['total'],'negative':result.get('pre_fix_negative'),'errors':errors},ensure_ascii=False))

if __name__=='__main__':
    try:main()
    except Exception as e:
        print(json.dumps({'status':'FAIL','error':str(e),'checks':checks},ensure_ascii=False),file=sys.stderr)
        sys.exit(1)
