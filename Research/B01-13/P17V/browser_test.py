import json, pathlib, sys, os
from playwright.sync_api import sync_playwright

html = pathlib.Path(sys.argv[1]).resolve()
assert html.is_file()
with sync_playwright() as p:
    browser = p.chromium.launch(headless=True, executable_path=os.environ.get('CHROME_BIN') or None,
                                args=['--no-sandbox'] if os.environ.get('CHROME_BIN') else [])
    page = browser.new_page(accept_downloads=True, viewport={'width': 1280, 'height': 900})
    errors=[]
    page.on('pageerror', lambda e:errors.append(str(e)))
    page.set_content(html.read_text(encoding="utf-8"),wait_until="load")
    page.locator('#start').wait_for()
    assert page.locator('#start').is_disabled()
    assert page.locator('#screen').is_hidden()
    page.locator('#consent').check()
    assert page.locator('#start').is_disabled()
    page.locator('#privacy').check()
    assert page.locator('#start').is_enabled()
    page.locator('#start').click()
    assert page.locator('#step').inner_text()=='Zadanie 1 z 8'
    assert page.evaluate('document.activeElement.id') == 'step', 'START_FOCUS_NOT_ON_TASK_HEADING'
    page.locator('#send').click()
    assert 'Wybierz odpowiedzi' in page.locator('#feedback').inner_text()
    for idx in range(8):
        assert page.locator('#step').inner_text()==f'Zadanie {idx+1} z 8'
        if idx == 1:
            page.locator('#skip').click()
            assert page.evaluate('document.activeElement.id') == 'step', 'SKIP_FOCUS_NOT_ON_NEXT_TASK'
        else:
            page.locator('input[name=deploy][value=YES]').check()
            page.locator('input[name=loaner][value=NO]').check()
            page.locator("input[name=quote][value=\"65\"]").check()
            page.locator('input[name=complete][value=NO]').check()
            page.locator('input[name=free][value=NO]').check()
            page.locator('#send').click()
            if idx != 7:
                assert page.evaluate('document.activeElement.id') == 'step', 'ANSWER_FOCUS_NOT_ON_NEXT_TASK'
    assert page.locator('#finish').is_visible()
    assert page.evaluate('document.activeElement.id') == 'finish-title', 'FINISH_FOCUS_NOT_ON_SUMMARY_HEADING'
    assert 'Wypełniono 8 z 8' in page.locator('#summary').inner_text()
    assert 'pominięte 1' in page.locator('#summary').inner_text()
    with page.expect_download() as d:
        page.locator('#json').click()
    data=json.loads(d.value.path().read_text())
    assert len(data['records'])==8
    assert data['score']['skipped']==1
    assert data['score']['maximum']==35
    assert data['notice']=='LOCAL_CLIENT_RESPONSES_NOT_AUTHENTICATED_HUMAN_STUDY'
    assert data['records'][1]['status']=='SKIP'
    assert data['records'][1]['answers'] is None
    with page.expect_download() as d:
        page.locator('#csv').click()
    csv=d.value.path().read_text()
    assert csv.count('T01')==1 and 'T08' in csv
    assert '\n' in csv
    page.locator('#erase').click()
    assert page.locator('#welcome').is_visible()
    assert page.evaluate('document.activeElement.id') == 'welcome-title', 'ERASE_FOCUS_NOT_ON_CONSENT_HEADING'
    assert page.locator('#start').is_disabled()
    assert not errors,errors
    page.set_viewport_size({'width':360,'height':780})
    assert page.locator('h1').is_visible()
    browser.close()
print('BROWSER_CHROMIUM_W25_PASS original flow + 4 verified focus transfers / consent / answer / skip / finished / erase / downloaded JSON+CSV / mobile / 0 JS errors')
