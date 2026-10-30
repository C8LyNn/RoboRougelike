#!/usr/bin/env python3
"""Original standalone Chromium UI + negative import regression; not native Unity."""
import hashlib
import json
import sys
import os
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parent
HTML = ROOT / 'controls.html'
MANIFEST = ROOT / 'manifest.json'
manifest = json.loads(MANIFEST.read_text(encoding='utf-8'))
source = HTML.read_bytes()
if (hashlib.sha256(source).hexdigest() != manifest['html_sha256']
        or len(source) != manifest['html_bytes']
        or hashlib.sha256(Path(__file__).read_bytes()).hexdigest() != manifest['test_sha256']):
    print('FAIL_CLOSED HTML source / manifest mismatch', file=sys.stderr)
    sys.exit(2)

results = []
page_errors = []
def check(case, truth):
    results.append({'case': case, 'pass': bool(truth)})
    if not truth:
        print('FAIL', case, file=sys.stderr)

def key(page, action, code):
    page.locator(f'[data-bind="{action}"]').click()
    page.keyboard.press(code)

def value(page, action):
    return page.locator(f'[data-bind="{action}"]').get_attribute('aria-label')

with sync_playwright() as playwright:
    browser = playwright.chromium.launch(headless=True, executable_path='/usr/bin/chromium', args=['--no-sandbox'])
    for width, height in [(320,720), (768,1024), (1280,720), (1920,1080)]:
        page = browser.new_page(viewport={'width': width, 'height': height})
        page.on('pageerror', lambda e: page_errors.append(str(e)))
        page.set_content(source.decode('utf-8'))
        prefix = str(width)
        check(prefix+'/initial_actions', page.locator('#assignedCount').inner_text()=='13')
        check(prefix+'/initial_no_conflicts', page.locator('#collisionCount').inner_text()=='0')
        check(prefix+'/locked_mouse_buttons', page.get_by_text('Stały').count()==2)
        check(prefix+'/default_forward', 'W' in value(page,'forward'))
        # Keyboard-only interaction: Enter activates a focused action, then physical key commits it.
        page.locator('[data-bind="camera"]').focus()
        page.keyboard.press('Enter')
        page.keyboard.press('v')
        check(prefix+'/keyboard_only_remap','V' in value(page,'camera'))
        page.locator('#undo').click()
        check(prefix+'/keyboard_only_undo','C' in value(page,'camera'))
        check(prefix+'/no_horizontal_overflow', page.evaluate('document.documentElement.scrollWidth<=window.innerWidth'))
        key(page,'forward','z')
        check(prefix+'/remap', 'Z' in value(page,'forward'))
        check(prefix+'/remap_notice','Z' in page.locator('#status').inner_text())
        page.locator('#undo').click()
        check(prefix+'/undo','W' in value(page,'forward'))
        page.locator('#redo').click()
        check(prefix+'/redo','Z' in value(page,'forward'))
        page.locator('#export').click()
        saved = page.locator('#profile').input_value()
        doc = json.loads(saved)
        check(prefix+'/versioned_export', doc['version']==1 and doc['bindings']['forward']=='KeyZ')
        check(prefix+'/export_full_actions',len(doc['bindings'])==13)
        key(page,'forward','x')
        check(prefix+'/export_invalidated',page.locator('#profile').input_value()=='' and 'Ponownie' in page.locator('#exportStatus').inner_text())
        page.locator('#undo').click()
        check(prefix+'/undo_after_export','Z' in value(page,'forward'))
        page.locator('#profile').fill(saved)
        page.locator('#import').click()
        check(prefix+'/import_valid','zaimportowany' in page.locator('#status').inner_text())
        check(prefix+'/import_value','Z' in value(page,'forward'))
        page.locator('#reset').click()
        check(prefix+'/reset_defaults','W' in value(page,'forward'))
        key(page,'reverse','w')
        check(prefix+'/conflict_detected',page.locator('#conflict').is_visible())
        check(prefix+'/conflict_before_confirm','S' in value(page,'reverse') and 'W' in value(page,'forward'))
        page.locator('#cancelSwap').click()
        check(prefix+'/cancel_collision','S' in value(page,'reverse') and page.locator('#collisionCount').inner_text()=='0')
        key(page,'reverse','w')
        page.locator('#swap').click()
        check(prefix+'/atomic_swap', 'W' in value(page,'reverse') and 'S' in value(page,'forward'))
        check(prefix+'/no_collision_after_swap',page.locator('#collisionCount').inner_text()=='0')
        page.locator('#undo').click()
        check(prefix+'/undo_swap','W' in value(page,'forward') and 'S' in value(page,'reverse'))
        # Capture cancellation and rejection of browser/system keys.
        page.locator('[data-bind="boost"]').click()
        page.keyboard.press('F5')
        check(prefix+'/reject_reserved', 'nieobsługiwany' in page.locator('#status').inner_text())
        page.keyboard.press('Escape')
        check(prefix+'/escape_cancel', 'Zmiana anulowana.' in page.locator('#status').inner_text())
        check(prefix+'/escape_no_change','Lewy Shift' in value(page,'boost'))
        page.locator('#tab-garage').click()
        key(page,'garagePick','w')
        check(prefix+'/cross_context_repeat_allowed','W' in value(page,'garagePick'))
        check(prefix+'/cross_context_no_collision',page.locator('#collisionCount').inner_text()=='0')
        page.locator('#export').click()
        valid = json.loads(page.locator('#profile').input_value())
        bad_cases = [
            ('bad_json','{'),
            ('wrong_version',json.dumps({**valid,'version':2})),
            ('extra_key',json.dumps({**valid,'unexpected':True})),
            ('missing_action',json.dumps({'version':1,'bindings':{k:v for k,v in valid['bindings'].items() if k!='garagePick'}})),
            ('unknown_action',json.dumps({'version':1,'bindings':{**valid['bindings'],'random':'KeyJ'}})),
            ('locked_mouse_changed',json.dumps({'version':1,'bindings':{**valid['bindings'],'fireA':'KeyP'}})),
            ('unsupported_key',json.dumps({'version':1,'bindings':{**valid['bindings'],'garagePick':'F5'}})),
            ('duplicate_same_context',json.dumps({'version':1,'bindings':{**valid['bindings'],'garageRotate':'KeyW'}})),
            ('invalid_type',json.dumps({'version':1,'bindings':{**valid['bindings'],'garagePick':42}})),
        ]
        for name, payload in bad_cases:
            page.locator('#profile').fill(payload)
            page.locator('#import').click()
            check(prefix+'/'+name+'_reject','Import odrzucony' in page.locator('#status').inner_text())
            check(prefix+'/'+name+'_atomic','W' in value(page,'garagePick') and page.locator('#collisionCount').inner_text()=='0')
        page.locator('#reset').click()
        check(prefix+'/reset_garage','E' in value(page,'garagePick'))
        check(prefix+'/responsive_final',page.evaluate('document.documentElement.scrollWidth<=window.innerWidth'))
        if width==1280:
            page.screenshot(path=str(ROOT / 'evidence-1280x720.png'), full_page=True)
        page.close()
    browser.close()
summary = {'package':'B01-12-CONTROLS-001/R1', 'source_sha256':hashlib.sha256(source).hexdigest(),
           'total':len(results), 'pass':sum(x['pass'] for x in results),
           'failed':[x['case'] for x in results if not x['pass']], 'js_errors':page_errors,
           'native_unity':'NOT_RUN','checks':results}
(ROOT/'test-results.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in summary.items() if k!='checks'},indent=2))
sys.exit(0 if not summary['failed'] and not page_errors else 1)

