"""REAL Chromium Playwright regression, automated operator inputs ONLY — not human sessions."""
import json, os, pathlib, sys, tempfile
from playwright.sync_api import sync_playwright
html=pathlib.Path(sys.argv[1]).resolve()
assert html.is_file()
with sync_playwright() as p:
 browser=p.chromium.launch(headless=True, executable_path=os.environ.get('CHROME_BIN') or None,
                           args=['--no-sandbox'] if os.environ.get('CHROME_BIN') else [])
 page=browser.new_page(accept_downloads=True, viewport={'width':1280,'height':800})
 errors=[]
 page.on('pageerror',lambda e:errors.append(str(e)))
 page.set_content(html.read_text(encoding='utf8'),wait_until='load')
 assert page.locator('#intro').is_visible()
 assert page.locator('#task').is_hidden()
 page.locator('#begin').click()
 assert 'Wymagane' in page.locator('#intro-problem').inner_text()
 page.locator('#consent').check()
 page.locator('#private').check()
 page.locator('#build').fill('z'*40)
 page.locator('#begin').click()
 assert page.locator('#task').is_hidden(), 'invalid sha should not open test session'
 sha='a'*40
 page.locator('#build').fill(sha)
 page.locator('#begin').click()
 assert page.locator('#task').is_visible()
 assert page.evaluate('document.activeElement.id')=='step-title'
 # Real user-facing form negative: first task FAIL with no issue must reject.
 page.locator('#outcome').select_option('FAIL')
 page.locator('#seconds').fill('55')
 page.locator('#save').click()
 assert 'Niespójny wynik' in page.locator('#problem').inner_text()
 assert 'Zadanie 1 / 8' in page.locator('#step-title').inner_text()
 # Record one observed low-severity genuine synthetic browser FAIL (not a human).
 page.locator('#issue').select_option('DOCK')
 page.locator('#severity').select_option('P2')
 page.locator('#save').click()
 assert 'Zadanie 2 / 8' in page.locator('#step-title').inner_text()
 assert page.evaluate('document.activeElement.id')=='step-title'
 # Record one blocking task; observation is not a pass.
 page.locator('#outcome').select_option('BLOCKED')
 page.locator('#save').click()
 assert 'Zadanie 3 / 8' in page.locator('#step-title').inner_text()
 for j in range(3,9):
  assert f'Zadanie {j} / 8' in page.locator('#step-title').inner_text()
  page.locator('#outcome').select_option('PASS')
  page.locator('#seconds').fill('90')
  page.locator('#assists').fill('0')
  page.locator('#save').click()
 assert page.locator('#done').is_visible()
 assert page.evaluate('document.activeElement.id')=='done-title'
 assert 'INCOMPLETE' in page.locator('#results').inner_text()
 assert 'BLOCKED 1' in page.locator('#results').inner_text()
 with tempfile.TemporaryDirectory() as folder:
  with page.expect_download() as dl:
   page.locator('#download-json').click()
  downloaded=dl.value
  assert downloaded.suggested_filename=='B01-13-observation.json'
  filename=pathlib.Path(folder)/downloaded.suggested_filename
  downloaded.save_as(str(filename))
  blob=json.loads(filename.read_text(encoding='utf8'))
  assert blob['source']=='OPERATOR_SELF_REPORTED_NO_INDEPENDENT_AUTHENTICATION'
  assert blob['actualParticipantsCertifiedByTool'] is False
  assert blob['summary']['gate']=='INCOMPLETE'
  assert len(blob['submittedRows'])==8
  assert blob['submittedRows'][0]['issue']=='DOCK'
  assert blob['submittedRows'][1]['outcome']=='BLOCKED'
  assert blob['buildSha']==sha
  assert 'participantName' not in json.dumps(blob)
  with page.expect_download() as dl:
   page.locator('#download-csv').click()
  downloaded=dl.value
  assert downloaded.suggested_filename=='B01-13-observation.csv'
  fn=pathlib.Path(folder)/downloaded.suggested_filename
  downloaded.save_as(str(fn))
  txt=fn.read_text(encoding='utf8')
  assert len(txt.strip().splitlines())==9
  assert 'build_sha' in txt and 'participant_name' not in txt
  assert '"PT02","BLOCKED"' in txt
 page.locator('#clear-done').click()
 assert page.locator('#intro').is_visible()
 assert page.evaluate('document.activeElement.id')=='intro-title'
 assert page.locator('#consent').is_checked() is False
 assert page.locator('#build').input_value()==''
 # A second browser navigation at mobile dimensions is legible and blocked until consent.
 page.set_viewport_size({'width':360,'height':740})
 assert page.locator('#intro').is_visible()
 assert page.locator('#task').is_hidden()
 assert page.evaluate('document.documentElement.scrollWidth <= window.innerWidth + 2')
 assert not errors,errors
 browser.close()
print('BROWSER_W27_PASS_REAL_CHROMIUM consent gate + invalid SHA + invalid observed record + focus + 8 operator-entry rows + distinct blocked/failed outcomes + real JSON/CSV downloads + erase + mobile, 0 JS exceptions; NOT A HUMAN SESSION')
