#!/usr/bin/env python3
"""Real Chromium DOM tests for exact original source bytes; fail closed on tamper."""
import hashlib, json, sys
from pathlib import Path
from playwright.sync_api import sync_playwright
ROOT=Path(__file__).resolve().parent

def validate():
    try:
        manifest=json.loads((ROOT/'manifest.json').read_text(encoding='utf8'))
        for name,spec in manifest['files'].items():
            blob=(ROOT/name).read_bytes()
            if len(blob)!=spec['bytes'] or hashlib.sha256(blob).hexdigest()!=spec['sha256']:
                raise ValueError('SOURCE_MISMATCH '+name)
    except Exception as ex:
        print('FAIL_CLOSED '+str(ex),file=sys.stderr)
        sys.exit(2)
validate()
checks=[];js_errors=[]
def ck(name,ok):
    checks.append({'name':name,'status':'PASS' if ok else 'FAIL'})
    if not ok: print('FAIL',name,file=sys.stderr)
with sync_playwright() as playwright:
    browser=playwright.chromium.launch(executable_path='/usr/bin/chromium',headless=True,args=['--no-sandbox'])
    doc=(ROOT/'demo.html').read_text(encoding='utf8')
    engine=(ROOT/'recap_engine.js').read_text(encoding='utf8')
    ui=(ROOT/'ui.js').read_text(encoding='utf8')
    # Test byte-identical original JS in Chromium while avoiding blocked file:// on ephemeral operators.
    doc=doc.replace('<script src="recap_engine.js"></script>', '<script>'+engine+'</script>')
    doc=doc.replace('<script src="ui.js"></script>', '<script>'+ui+'</script>')
    for w,h in [(320,720),(768,1024),(1280,720),(1920,1080)]:
        ctx=browser.new_context(viewport={'width':w,'height':h})
        page=ctx.new_page();page.on('pageerror',lambda err: js_errors.append(str(err)))
        page.set_content(doc,wait_until='load')
        pre=f'{w}x{h}'
        def C(name,fn):
            try: ck(pre+' '+name,bool(fn()))
            except Exception as exc:print('EXCEPTION',pre,name,str(exc),file=sys.stderr);ck(pre+' '+name,False)
        C('root heading',lambda:page.locator('h1').inner_text().startswith('Po bitwie'))
        C('outcome defeat',lambda:page.locator('#stat-result').inner_text()=='Porażka')
        C('effective damage',lambda:page.locator('#stat-damage').inner_text()=='295')
        C('repaired HP',lambda:page.locator('#stat-healed').inner_text()=='15')
        C('module losses',lambda:page.locator('#stat-lost').inner_text()=='3/4')
        C('all 10 events',lambda:page.locator('.event').count()==10)
        C('aria initial selection',lambda:page.locator('.event[aria-pressed="true"]').count()==1)
        C('damage cause',lambda:page.locator('#cause').inner_text().startswith('W tym zdarzeniu'))
        page.locator('#type').select_option('detach')
        C('detach filter count',lambda:page.locator('.event').count()==3)
        C('detach title accessible',lambda:page.locator('.event').first.get_attribute('aria-pressed')=='true')
        C('causal detail present',lambda:'Ostatnie trafienie: Wróg' in page.locator('#cause').inner_text())
        page.locator('#source').select_option('terrain')
        C('intersection type/source',lambda:page.locator('.event').count()==1)
        C('structural cause',lambda:'Uderzenie' in page.locator('#cause').inner_text())
        page.locator('#query').fill('brak')
        C('no match',lambda:page.locator('.event').count()==0)
        C('empty message',lambda:'Brak wyników' in page.locator('#selection').inner_text())
        page.locator('#query').fill('Lewe')
        C('search filter restored',lambda:page.locator('.event').count()==1)
        page.locator('#reset').click()
        C('reset all event filters',lambda:page.locator('.event').count()==10)
        C('reset query',lambda:page.locator('#query').input_value()=='')
        page.locator('.event').nth(3).click()
        C('selected event',lambda:page.locator('.event[aria-pressed="true"]').get_attribute('data-event-id')=='e04')
        C('causality is shown',lambda:'10 ms wcześniej' in page.locator('#cause').inner_text())
        page.locator('#export').click()
        exported=page.locator('#json').input_value()
        C('valid export JSON',lambda:json.loads(exported)['schema']=='b01-12.runrecap.v1')
        C('export 10 events',lambda:len(json.loads(exported)['events'])==10)
        page.locator('#json').fill('{"foo":1}')
        page.locator('#load').click()
        C('invalid import rejected',lambda:'odrzucony' in page.locator('#status').inner_text())
        C('atomic unchanged',lambda:page.locator('#stat-damage').inner_text()=='295')
        safe=json.loads(exported);safe['runId']='CUSTOM01';safe['events']=safe['events'][:2];safe['outcome']='victory';page.locator('#json').fill(json.dumps(safe));page.locator('#load').click()
        C('valid import accepted',lambda:'CUSTOM01' in page.locator('#status').inner_text())
        C('new count',lambda:page.locator('.event').count()==2)
        C('new outcome',lambda:page.locator('#stat-result').inner_text()=='Wygrana')
        C('new damage',lambda:page.locator('#stat-damage').inner_text()=='85')
        C('new detach count',lambda:page.locator('#stat-lost').inner_text()=='0/4')
        page.locator('#json').fill(exported.replace('Pancerz boczny','<img src=x onerror=alert(1)>'))
        page.locator('#load').click()
        C('injection rejected',lambda:'odrzucony' in page.locator('#status').inner_text())
        C('injection atomic',lambda:page.locator('#stat-damage').inner_text()=='85')
        C('no reflected HTML',lambda:page.locator('img').count()==0)
        C('no horizontal overflow',lambda:page.evaluate('document.documentElement.scrollWidth <= innerWidth + 1'))
        C('JS exception free',lambda:len(js_errors)==0)
        if w==1280:page.screenshot(path=str(ROOT/'evidence-1280x720.png'),full_page=True)
        ctx.close()
    browser.close()
receipt={'package':'B01-12-RUNRECAP-001/R1','suite':'chromium','pass':sum(x['status']=='PASS' for x in checks),'fail':sum(x['status']=='FAIL' for x in checks),'jsErrors':js_errors,'cases':checks,'viewports':['320x720','768x1024','1280x720','1920x1080']}
(ROOT/'browser-results.json').write_text(json.dumps(receipt,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(f"{receipt['pass']}/{len(checks)} real Chromium checks PASS; JS errors {len(js_errors)}")
sys.exit(1 if receipt['fail'] or js_errors else 0)
