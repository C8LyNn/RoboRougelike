#!/usr/bin/env python3
"""Chromium integration tests for EXACT original ImpactLens HTML logic and DOM."""
import json
from pathlib import Path
from playwright.sync_api import sync_playwright

HTML = Path(__file__).with_name('index.html')

def run():
    results=[]
    with sync_playwright() as play:
        browser=play.chromium.launch(headless=True, executable_path='/usr/bin/chromium', args=['--no-sandbox'])
        try:
            for viewport in ((320,720),(720,960),(1280,800),(1920,1080)):
                page=browser.new_page(viewport={'width':viewport[0],'height':viewport[1]})
                errors=[]
                page.on('pageerror',lambda e:errors.append(str(e)))
                page.set_content(HTML.read_text(encoding='utf-8'), wait_until='load')
                def check(name, expr):
                    if not expr: raise AssertionError(f'{viewport}: {name}')
                    results.append({'test':f'{viewport[0]}x{viewport[1]}/{name}','status':'PASS'})
                check('no JS exception',not errors)
                check('initial state',page.locator('#progress').inner_text()=='Zdarzenie 0 z 5')
                check('parts initial',page.locator('.part:not(.empty)').count()==5)
                page.locator('#next').click();check('step HP',page.locator('[aria-label="Osłona, 72 z 100 HP"]').count()==1)
                page.keyboard.press('n');page.keyboard.press('n');check('weapon destroyed',page.locator('#weaponStatus').inner_text()=='Broń A: NIEDOSTĘPNA')
                check('critical alert',page.locator('#noticeTitle').inner_text()=='Grupa broni A niedostępna')
                page.locator('#density').click();check('compact toggle',page.locator('#density').get_attribute('aria-pressed')=='true')
                page.locator('#reset').click();check('reset restores hp',page.locator('#weaponStatus').inner_text()=='Broń A: sprawna')
                page.locator('#scenario').select_option('friendly');page.locator('#next').click();check('friendly-fire attribution','sojusznik' in page.locator('#noticeDescription').inner_text())
                page.locator('#all').click();check('all steps reached',page.locator('#progress').inner_text()=='Zdarzenie 4 z 4')
                check('end next disabled',page.locator('#next').is_disabled())
                page.locator('#scenario').select_option('collapse');page.locator('#all').click();check('core destroyed',page.locator('#coreStatus').inner_text()=='Rdzeń: ZNISZCZONY')
                check('no overflow',page.evaluate('document.documentElement.scrollWidth<=innerWidth'))
                # Validate the browser's ORIGINAL step() function (not a copied port).
                cases=[
                    ('duplicate event',"(()=>{let s=ImpactLens.start();let e=ImpactLens.scenarios.duel[0];s=ImpactLens.step(s,e);try{ImpactLens.step(s,e);return false}catch{return true}})()"),
                    ('unknown part',"(()=>{try{ImpactLens.step(ImpactLens.start(),{id:'x',type:'hit',part:'missing',amount:1,source:'enemy'});return false}catch{return true}})()"),
                    ('negative damage',"(()=>{try{ImpactLens.step(ImpactLens.start(),{id:'x',type:'hit',part:'core',amount:-1,source:'enemy'});return false}catch{return true}})()"),
                    ('blocked nonzero',"(()=>{try{ImpactLens.step(ImpactLens.start(),{id:'x',type:'blocked',part:'core',amount:1,source:'enemy'});return false}catch{return true}})()"),
                    ('state immutability',"(()=>{const s=ImpactLens.start();const t=ImpactLens.step(s,ImpactLens.scenarios.duel[0]);return s.parts[3].hp===100&&t.parts[3].hp===72})()"),
                    ('damage capped at 0',"(()=>{const s=ImpactLens.start();const t=ImpactLens.step(s,{id:'cap',type:'hit',part:'core',amount:9999,source:'enemy'});return t.parts.find(p=>p.id==='core').hp===0})()")
                ]
                for name,script in cases:check(name,page.evaluate(script))
                check('still no JS exception',not errors)
                page.close()
        finally:browser.close()
    return results

if __name__=='__main__':
    result=run()
    out=Path(__file__).parent.parent/'TEST_EVIDENCE'/'browser-results.json'
    out.parent.mkdir(parents=True,exist_ok=True)
    out.write_text(json.dumps({'environment':'Chromium headless via Playwright (local Linux)','total':len(result),'passed':len(result),'failed':0,'tests':result},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(f'PASS {len(result)}/{len(result)}; saved {out}')
