#!/usr/bin/env python3
"""Real Chromium original R2 stale-status RED witness -> corrected R3 GREEN."""
import hashlib
import json
import os
import pathlib
import sys
from playwright.sync_api import sync_playwright

ROOT = pathlib.Path(__file__).resolve().parent
manifest = json.loads((ROOT / 'manifest.json').read_text('utf-8'))
for filename, pin in manifest['files'].items():
    raw = (ROOT / filename).read_bytes()
    if len(raw) != pin['bytes'] or hashlib.sha256(raw).hexdigest() != pin['sha256']:
        print('FAIL_CLOSED source mismatch: ' + filename, file=sys.stderr)
        sys.exit(2)

samples = []
def check(name, actual):
    samples.append({'case': name, 'status': 'PASS' if actual else 'FAIL'})
    if not actual:
        print('FAIL ' + name, file=sys.stderr)

html_template = (ROOT / 'demo.html').read_text('utf-8')
# Insert an original-source fixture override just before the UI starts.
# The production fixture source stays byte-identical, and the injected records
# represent a caller presenting a corrupted later snapshot after a valid one.
injected = '''
window.__originalFixtureList = RecoveryFixtures.list;
window.RecoveryFixtures = { list() {
 const a = window.__originalFixtureList();
 const invalidQuota = JSON.parse(JSON.stringify(a[1]));
 invalidQuota.label = 'W27: brak dowodu wygranej';
 invalidQuota.freeAvailable = true; invalidQuota.victoryId = null;
 a.push(invalidQuota);
 const invalidQuote = JSON.parse(JSON.stringify(a[1]));
 invalidQuote.label = 'W27: cena sprzeczna';
 invalidQuote.parts[1].purchasePrice = 1;
 invalidQuote.parts[1].rebuildQuote = 1;
 a.push(invalidQuote);
 const invalidCore = JSON.parse(JSON.stringify(a[0]));
 invalidCore.label = 'W27: Core niespójny';
 invalidCore.coreAlive = false; invalidCore.canDeploy = true;
 a.push(invalidCore);
 return a;
}};
'''

def html_with_ui(ui):
    html = html_template
    for filename, content in (
        ('readout_engine.js', (ROOT / 'readout_engine.js').read_text('utf-8')),
        ('fixtures.js', (ROOT / 'fixtures.js').read_text('utf-8') + '\n' + injected),
        ('ui.js', ui),
    ):
        html = html.replace('<script src="' + filename + '"></script>', '<script>' + content + '</script>')
    return html

old_html = html_with_ui((ROOT / 'r2-original/ui.js').read_text('utf-8'))
new_html = html_with_ui((ROOT / 'ui.js').read_text('utf-8'))
with sync_playwright() as p:
    browser = p.chromium.launch(executable_path=os.getenv('CHROMIUM_PATH') or None,
                                headless=True, args=['--no-sandbox'])
    for width, height in ((320,720), (768,1024), (1280,720), (1920,1080)):
        old = browser.new_page(viewport={'width':width,'height':height})
        old.set_content(old_html, wait_until='load')
        old.locator('.scenario').nth(1).click()
        prior_free = old.locator('#free').inner_text()
        old.locator('.scenario').nth(5).click()
        check(f'{width} original R2 rejection visible', 'Odmowa odczytu' in old.locator('#status').inner_text())
        check(f'{width} original R2 RED retains false previous free', old.locator('#free').inner_text() == prior_free)
        check(f'{width} original R2 RED retains previous parts', old.locator('.part').count() > 0)
        old.locator('#export').click()
        check(f'{width} original R2 RED exposes stale JSON', not old.locator('#exportPanel').is_hidden())
        old.close()

        page = browser.new_page(viewport={'width':width,'height':height})
        errors = []
        page.on('pageerror', lambda exc: errors.append(str(exc)))
        page.set_content(new_html, wait_until='load')
        check(f'{width} fixed normal initial state', page.evaluate('window.__readoutDebug().route') == 'NORMAL')
        for idx, code in ((5, 'UNVERIFIED_FREE_QUOTA'), (6, 'QUOTE_CONTRADICTION'), (7, 'CORE_CONTRADICTION')):
            page.locator('.scenario').nth(1).click()
            check(f'{width} valid before reject {idx}', page.evaluate('window.__readoutDebug().route') == 'LOANER_AVAILABLE')
            page.locator('#export').click()
            check(f'{width} prior JSON visible {idx}', not page.locator('#exportPanel').is_hidden())
            page.locator('.scenario').nth(idx).click()
            check(f'{width} rejection code {idx}', code in page.locator('#status').inner_text())
            check(f'{width} invalidates identity {idx}', 'Brak wiarygodnego' in page.locator('#identity').text_content())
            check(f'{width} invalidates win {idx}', 'Brak danych' in page.locator('#victory').inner_text())
            check(f'{width} invalidates free {idx}', 'Brak danych' in page.locator('#free').inner_text())
            check(f'{width} clears stale parts {idx}', page.locator('.part').count() == 0)
            check(f'{width} clears stale JSON {idx}', page.locator('#exportPanel').is_hidden() and not page.locator('#json').inner_text())
            check(f'{width} export disabled {idx}', page.locator('#export').is_disabled())
            check(f'{width} all buttons unpressed {idx}', page.locator('.scenario[aria-pressed="true"]').count() == 0)
            check(f'{width} internal state zero {idx}', page.evaluate('window.__readoutDebug().route') is None)
            check(f'{width} no residual JSON after reject {idx}', page.locator('#exportPanel').is_hidden())
            page.locator('.scenario').first.click()
            check(f'{width} valid recovery {idx}', page.evaluate('window.__readoutDebug().route') == 'NORMAL' and page.locator('#export').is_enabled())
        check(f'{width} no browser exceptions', not errors)
        check(f'{width} no document overflow', page.evaluate('document.documentElement.scrollWidth <= window.innerWidth+1'))
        if width == 1280:
            page.screenshot(path=str(ROOT / 'evidence-r3-1280x720.png'), full_page=True)
        page.close()
    browser.close()
passed = sum(x['status'] == 'PASS' for x in samples)
(ROOT / 'stale-results.json').write_text(json.dumps({'pass':passed,'fail':len(samples)-passed,'tests':samples}, ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(f'R2_RED_WITNESSES_AND_R3_CHROMIUM {passed}/{len(samples)} ' + ('PASS' if passed==len(samples) else 'FAIL'))
sys.exit(0 if passed==len(samples) else 1)
