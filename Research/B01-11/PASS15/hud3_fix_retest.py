#!/usr/bin/env python3
"""B01-11 / PASS15: independent FIXED HUD3 browser receiver, not Unity/human QA.
Run: python Research/B01-11/PASS15/hud3_receiver_pass15.py Research/B01-12/W70/HUD3-overflow.html
Requires playwright==1.57.0 and /usr/bin/chromium.
Exit 0 when all original functionality and all 12 previously RED blinding/help checks become GREEN on changed original Git blob.
"""
import hashlib, json, sys
from pathlib import Path
from playwright.sync_api import sync_playwright

SHA256='df857d206855437608ac94421a354ef9112366e0ac91d120ac03d772bb5a6884'
BLOB='e73b57e52ca0706bcf739340f5b06dd66b55e104'
path=Path(sys.argv[1]) if len(sys.argv)>1 else Path('Research/B01-12/W70/HUD3-overflow.html')
b=path.read_bytes()
assert hashlib.sha256(b).hexdigest()==SHA256,'SOURCE_SHA256_MISMATCH'
assert hashlib.sha1(b'blob '+str(len(b)).encode()+b'\0'+b).hexdigest()==BLOB,'SOURCE_GIT_BLOB_MISMATCH'
checks=[];js_errors=[]
def check(tag,value,expected_red=False):
    checks.append({'id':tag,'passed':bool(value),'expected_red':expected_red})
with sync_playwright() as p:
    browser=p.chromium.launch(executable_path='/usr/bin/chromium',headless=True,args=['--no-sandbox'])
    version=browser.version
    for w in (320,720,1280,1920):
        page=browser.new_page(viewport={'width':w,'height':800})
        page.on('pageerror',lambda e:js_errors.append(str(e)))
        page.set_content(b.decode('utf-8'),wait_until='load')
        for treatment in ('A','B'):
            page.locator('#mode'+treatment).click()
            page.locator('#fixture').select_option('F1')
            groups=page.locator('#groups').inner_text()
            top=page.locator('#topRows .slot')
            check(f'{w}/{treatment}/F1_urgent_both','PRZEGRZANIE: 1' in groups and 'ZACIĘCIE: 1' in groups)
            check(f'{w}/{treatment}/F1_top_ids',[top.nth(i).get_attribute('data-gun-id') for i in range(top.count())]==['5','6'])
            check(f'{w}/{treatment}/F1_aria','PRZEGRZANIE' in page.locator('body').aria_snapshot() and 'ZACIĘCIE' in page.locator('body').aria_snapshot())
            check(f'{w}/{treatment}/F1_no_x_overflow',page.evaluate('document.documentElement.scrollWidth <= innerWidth+1'))
            page.locator('#fixture').select_option('F2')
            groups2=page.locator('#groups').inner_text()
            check(f'{w}/{treatment}/F2_EMP_and_RELOAD','EMP: 1' in groups2 and 'PRZEŁADOWANIE: 1' in groups2)
            check(f'{w}/{treatment}/F2_no_x_overflow',page.evaluate('document.documentElement.scrollWidth <= innerWidth+1'))
        page.locator('#modeA').click()
        page.locator('#fixture').select_option('F0')
        page.locator('#participant').check()
        a=page.locator('body').aria_snapshot()
        check(f'{w}/A_participant_treatment_blind','A: stałe alerty grup' not in a)
        check(f'{w}/A_participant_truthful_help',not ('część informacji można rozwinąć' in a and page.locator('#expand').is_hidden()))
        page.keyboard.press('Escape')
        page.locator('#modeB').click()
        page.locator('#fixture').select_option('F1')
        page.locator('#participant').check()
        snapshot=page.locator('body').aria_snapshot()
        check(f'{w}/B_participant_treatment_blind','B: stałe alerty grup' not in snapshot)
        check(f'{w}/B_participant_alert_visible','PRZEGRZANIE' in snapshot and 'ZACIĘCIE' in snapshot)
        page.keyboard.press('Tab')
        check(f'{w}/B_participant_keyboard_focus',page.evaluate('document.activeElement.id')=='expand')
        page.keyboard.press('Space')
        check(f'{w}/B_participant_keyboard_expand',page.locator('#expand').get_attribute('aria-expanded')=='true')
        page.keyboard.press('Escape')
        check(f'{w}/B_participant_escape',page.evaluate('document.activeElement.id')=='participant')
        page.close()
    browser.close()
positive=[x for x in checks if not x['expected_red']]
negative=[x for x in checks if x['expected_red']]
summary={'status':'ORIGINAL_B01_12_RESEARCH_ONLY','source_sha256':SHA256,'git_blob':BLOB,'chromium':version,'widths':[320,720,1280,1920],'positive_pass':sum(x['passed'] for x in positive),'positive_total':len(positive),'repaired_study_checks':sum(x['passed'] for x in positive if 'participant_treatment_blind' in x['id'] or 'participant_truthful_help' in x['id']),'repaired_study_total':12,'red_remaining':[x['id'] for x in positive if not x['passed']],'js_errors':js_errors,'native_unity':'NOT_RUN','human_participants':0}
print(json.dumps(summary,ensure_ascii=False,indent=2))
if not(all(x['passed'] for x in positive) and len(negative)==0 and not js_errors):sys.exit(1)
