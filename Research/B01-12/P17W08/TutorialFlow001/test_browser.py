#!/usr/bin/env python3
"""Run original offline tutorial HTML in actual Chromium. No game/native Unity claims."""
from pathlib import Path
import json,hashlib,sys,os
from playwright.sync_api import sync_playwright
ROOT=Path(__file__).resolve().parent
MAN=json.loads((ROOT/'manifest.json').read_text())
for name,entry in MAN['files'].items():
    b=(ROOT/name).read_bytes()
    if len(b)!=entry['bytes'] or hashlib.sha256(b).hexdigest()!=entry['sha256']:
        print('FAIL_CLOSED source mismatch: '+name,file=sys.stderr)
        raise SystemExit(2)
html=(ROOT/'tutorial.html').read_text(encoding='utf-8')
engine=(ROOT/'tutorial_engine.js').read_text(encoding='utf-8')
assert html.count('<script src="tutorial_engine.js"></script>') == 1
html=html.replace('<script src="tutorial_engine.js"></script>','<script>'+engine+'</script>')
rows=[]
errors=[]
def ck(label,condition):
    rows.append({'case':label,'pass':bool(condition)})
    if not condition:print('FAIL '+label,file=sys.stderr)
with sync_playwright() as playwright:
    browser_path=os.environ.get('CHROMIUM_PATH','/usr/bin/chromium')
    if not Path(browser_path).exists():
        browser_path=playwright.chromium.executable_path
    browser=playwright.chromium.launch(executable_path=browser_path,headless=True,args=['--no-sandbox'])
    for w,h in [(320,720),(768,1024),(1280,720),(1920,1080)]:
        page=browser.new_page(viewport={'width':w,'height':h})
        page.on('pageerror',lambda err: errors.append(str(err)))
        page.set_content(html,wait_until='load')
        tag=str(w)
        get=lambda:page.evaluate('window.__tutorialDebug()')
        action=lambda value:page.locator(f'[data-action="{value}"]').click()
        ck(tag+'/renders',page.locator('h1').inner_text()=='Interaktywny samouczek działań')
        ck(tag+'/initial_counter',page.locator('#counter').inner_text()=='0 / 6')
        ck(tag+'/initial_progress',page.locator('#progress').get_attribute('aria-valuenow')=='0')
        ck(tag+'/initial_skip_disabled',page.locator('#skip').is_disabled())
        ck(tag+'/initial_back_disabled',page.locator('#back').is_disabled())
        ck(tag+'/six_step_rows',page.locator('#steps li').count()==6)
        ck(tag+'/no_horizontal_overflow',page.evaluate('document.documentElement.scrollWidth<=window.innerWidth'))
        action('fire_a')
        ck(tag+'/wrong_phase_rejected',get()['state']['index']==0 and page.locator('#feedback').get_attribute('data-code')=='WRONG_ACTION')
        page.locator('[data-action="select_core"]').focus()
        page.keyboard.press('Enter')
        ck(tag+'/keyboard_accepts_action',get()['state']['index']==1)
        ck(tag+'/aria_live_feedback',page.locator('#feedback').get_attribute('aria-live')=='polite')
        ck(tag+'/progress_17',page.locator('#progress').get_attribute('aria-valuenow')=='17')
        action('mount_wheel')
        ck(tag+'/garage_2_done',get()['state']['index']==2)
        ck(tag+'/optional_skip_enabled',page.locator('#skip').is_enabled())
        page.locator('#skip').click()
        ck(tag+'/skip_marks_step',get()['state']['history'][2]['result']=='skipped')
        ck(tag+'/phase_combat',page.locator('#phase').inner_text()=='WALKA')
        action('claim_reward')
        ck(tag+'/wrong_reward_rejected',get()['state']['index']==3)
        action('fire_a')
        ck(tag+'/combat_fired',get()['state']['index']==4)
        ck(tag+'/optional_dodge_enabled',page.locator('#skip').is_enabled())
        action('dodge')
        ck(tag+'/dodge_done',get()['state']['index']==5)
        ck(tag+'/final_skip_disabled',page.locator('#skip').is_disabled())
        action('claim_reward')
        ck(tag+'/complete',get()['state']['status']=='complete')
        ck(tag+'/complete_progress_100',page.locator('#progress').get_attribute('aria-valuenow')=='100')
        ck(tag+'/restart_enabled',page.locator('#restart').is_enabled())
        ck(tag+'/history_complete_six',len(get()['state']['history'])==6)
        page.locator('#back').click()
        ck(tag+'/back_opens_final_step',get()['state']['index']==5)
        ck(tag+'/back_not_game_rollback',page.locator('#feedback').inner_text().find('nie są odwracane')>=0)
        action('claim_reward')
        ck(tag+'/replaying_final_works',get()['state']['status']=='complete')
        page.locator('#restart').click()
        ck(tag+'/restart_resets_progress',get()['state']['index']==0 and get()['state']['history']==[])
        ck(tag+'/restart_resets_progressbar',page.locator('#progress').get_attribute('aria-valuenow')=='0')
        action('select_core');page.locator('#reset').click()
        ck(tag+'/reset_midflow',get()['state']['index']==0 and page.locator('#feedback').get_attribute('data-code')=='RESET')
        ck(tag+'/final_no_horizontal_overflow',page.evaluate('document.documentElement.scrollWidth<=window.innerWidth'))
        if w==1280:page.wait_for_timeout(250);page.screenshot(path=str(ROOT/'evidence-1280x720.png'),full_page=True)
        page.close()
    browser.close()
report={'package':'B01-12-TUTORIALFLOW-001/R1','viewport_count':4,'total':len(rows),'passed':sum(x['pass'] for x in rows),'failed':[x for x in rows if not x['pass']],'js_errors':errors,'cases':rows,'native_unity':'NOT_RUN'}
(ROOT/'browser-results.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='cases'},ensure_ascii=False))
raise SystemExit(0 if not report['failed'] and not errors else 1)
