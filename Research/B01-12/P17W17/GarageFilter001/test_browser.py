#!/usr/bin/env python3
"""B01-12 GARAGE-FILTER-001 original byte-pinned real Chromium DOM verification."""
import hashlib, json, os, pathlib, sys, traceback
from playwright.sync_api import sync_playwright
ROOT = pathlib.Path(__file__).resolve().parent

def verify():
    manifest = json.loads((ROOT / 'manifest.json').read_text(encoding='utf-8'))
    for name, expected in manifest['sources'].items():
        data = (ROOT / name).read_bytes()
        if len(data) != expected['bytes'] or hashlib.sha256(data).hexdigest() != expected['sha256']:
            raise ValueError('FAIL_CLOSED ' + name)
    return manifest

try:
    verify()
except Exception as exc:
    print(str(exc), file=sys.stderr)
    sys.exit(2)

html = (ROOT / 'demo.html').read_text(encoding='utf-8')
# Inject the EXACT SHA-pinned sources in-memory, because this host blocks file:// navigation.
# The DOM and JS are executed in real Chromium; no mock browser or substitute engine.
for name in ('catalog.js', 'filter_engine.js', 'ui.js'):
    needle = '<script src="' + name + '"></script>'
    if html.count(needle) != 1:
        print('FAIL_CLOSED unexpected HTML script count: ' + name, file=sys.stderr)
        sys.exit(2)
    source = (ROOT / name).read_text(encoding='utf-8').replace('</script', '<\\/script')
    html = html.replace(needle, '<script>' + source + '</script>')

cases = []
def check(name, fn):
    try:
        fn()
        cases.append({'name': name, 'pass': True})
    except Exception as exc:
        cases.append({'name': name, 'pass': False, 'error': str(exc)})

def equal(a, b):
    assert a == b, f'expected {b!r}, got {a!r}'

def true(a):
    assert a, 'expected truthy'

def query(page, q):
    page.locator('#query').fill(q)
    page.locator('#search').click()

def count(page):
    return page.locator('.part').count()

with sync_playwright() as p:
    browser = p.chromium.launch(headless=True, executable_path=os.environ.get('CHROMIUM_PATH') or p.chromium.executable_path, args=['--no-sandbox'])
    for width,height in [(320,720),(768,1024),(1280,720),(1920,1080)]:
        page = browser.new_page(viewport={'width':width,'height':height}, device_scale_factor=1)
        errors=[]
        page.on('pageerror', lambda err: errors.append(str(err)))
        page.set_content(html, wait_until='load')
        prefix=f'{width}x{height} '
        check(prefix+'initial 12 records', lambda:equal(count(page),12))
        check(prefix+'safe screen width', lambda:true(page.evaluate('document.documentElement.scrollWidth <= innerWidth')))
        check(prefix+'initial status', lambda:equal(page.locator('#status').inner_text(),'Filtry zastosowane bezpiecznie'))
        query(page, 'type:weapon mass<=20 sort:cost')
        check(prefix+'weapon mass filter result',lambda:equal(count(page),2))
        check(prefix+'weapon sorted ids',lambda:equal(page.locator('.part').evaluate_all('(xs)=>xs.map(x=>x.dataset.id)'),['gun-02','gun-03']))
        check(prefix+'facets driven by original engine',lambda:equal(page.locator('.facet strong').all_inner_texts(),['0','2','0','0','0']))
        page.locator('#export').click()
        check(prefix+'export schema', lambda:equal(json.loads(page.locator('#json').inner_text())['schema'],1))
        check(prefix+'export ids', lambda:equal(json.loads(page.locator('#json').inner_text())['ids'],['gun-02','gun-03']))
        check(prefix+'copy enabled', lambda:true(page.locator('#copy').is_enabled()))
        query(page,'type:???')
        check(prefix+'malformed filter fail-closed',lambda:equal(count(page),0))
        check(prefix+'malformed filter status',lambda:true('Odrzucono zapytanie' in page.locator('#status').inner_text()))
        check(prefix+'export invalidated',lambda:equal(page.locator('#json').inner_text(),'—'))
        check(prefix+'copy disabled',lambda:true(page.locator('#copy').is_disabled()))
        page.locator('#reset').click()
        check(prefix+'reset results',lambda:equal(count(page),12))
        page.locator('button[data-query="owned:yes cost<=150 sort:-energy"]').click()
        check(prefix+'owned preset',lambda:equal(count(page),5))
        page.locator('#query').fill('tag:repair')
        page.locator('#query').press('Enter')
        check(prefix+'keyboard Enter filter',lambda:equal(page.locator('.part').first.get_attribute('data-id'),'util-01'))
        query(page,'mass>=99999')
        check(prefix+'empty valid query shows guidance',lambda:true('Nie znaleziono' in page.locator('.empty').inner_text()))
        check(prefix+'empty valid is not error',lambda:true('Odrzucono' not in page.locator('#status').inner_text()))
        query(page,'<img src=x onerror=alert(1)>')
        check(prefix+'markup rejected',lambda:equal(count(page),0))
        check(prefix+'no injected IMG',lambda:equal(page.locator('.part img').count(),0))
        query(page,'"lekki karabin"')
        check(prefix+'quoted phrase',lambda:equal(page.locator('.part').first.get_attribute('data-id'),'gun-02'))
        check(prefix+'no horizontal document overflow after filters',lambda:true(page.evaluate('document.documentElement.scrollWidth <= innerWidth')))
        check(prefix+'aria live status available',lambda:equal(page.locator('#status').get_attribute('aria-live'),'polite'))
        check(prefix+'no page exceptions',lambda:equal(errors,[]))
        if width==1280:
            query(page,'owned:yes cost<=150 sort:-energy')
            page.screenshot(path=str(ROOT/'evidence-1280x720.png'),full_page=True)
        page.close()
    browser.close()

passed=sum(x['pass'] for x in cases)
result={'package':'B01-12-GARAGE-FILTER-001/R1','runner':'chromium-playwright','passed':passed,'total':len(cases),'exit':0 if passed==len(cases) else 1,'cases':cases,'viewport_sizes':[[320,720],[768,1024],[1280,720],[1920,1080]]}
(ROOT/'browser-results.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('CHROMIUM',str(passed)+'/'+str(len(cases)), 'PASS' if passed==len(cases) else 'FAIL')
for case in cases:
    if not case['pass']:
        print(case['name'], case['error'], file=sys.stderr)
sys.exit(0 if passed==len(cases) else 1)
