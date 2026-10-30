'use strict';
// Actual headless Chromium interaction tests against ORIGINAL historical Git source.
// The old R1 is an immutable negative control: expected regression, not a product PASS.
const {test,before,after}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const cp=require('node:child_process');
const {chromium}=require('playwright');
const ROOT=path.resolve(__dirname,'../../../../');
const PATH='Research/B01-11/P17/W22-FRAME-SAFETY-001/index.html';
const BASE='c27fd4ecef6df52b0b8086cc2234904918fa1c4b';
const source=fs.readFileSync(path.join(ROOT,'Research/B01-11/P17/W22-FRAME-SAFETY-001/frame.js'),'utf8');
const current=fs.readFileSync(path.join(ROOT,PATH),'utf8');
const original=cp.execFileSync('git',['show',BASE+':'+PATH],{cwd:ROOT,encoding:'utf8'});
let browser;
const errors=[];
before(async()=>{browser=await chromium.launch({headless:true});});
after(async()=>{if(browser)await browser.close();});
async function open(html){
  const page=await browser.newPage({viewport:{width:1100,height:850}});
  page.on('pageerror',e=>errors.push(String(e)));
  // Inline the exact original source, no server, third-party requests or file permissions.
  assert.equal(html.includes('<script src="frame.js"></script>'),true);
  await page.setContent(html.replace('<script src="frame.js"></script>','<script>'+source+'</script>'),{waitUntil:'load'});
  await page.locator('#metric').waitFor();
  return page;
}
async function adjusted(page){
  await page.locator('#portrait').click();
  await page.locator('#weapon-left').click();
  await page.locator('#weapon-right').click();
  await page.locator('#fov').click();
  await page.locator('#auto').click();
}
async function snapshot(page){
  return page.evaluate(()=>{
    const $=id=>document.getElementById(id);
    return {width:$('frame').width,height:$('frame').height,left:$('weapon-left').getAttribute('aria-pressed'),
      right:$('weapon-right').getAttribute('aria-pressed'),fov:$('fov').textContent,
      status:$('headline').textContent,metric:$('metric').textContent,
      issues:[...$('issues').children].map(x=>x.textContent)};
  });
}
test('EXACT original R1 Git source still has the reset interaction defect',()=>{
  assert.match(original,/id="reset" type="button">Przywróć kamerę/);
  assert.doesNotMatch(original,/id="reset-all"/);
  assert.match(original,/\$\('reset'\)\.addEventListener\('click',\(\)=>\{const keepAspect=/);
});
for(const viewport of ['wide','portrait']){
  test('ORIGINAL R1 expected-RED observed in real Chromium: reset destroys FOV and loadout ('+viewport+')',async()=>{
    const page=await open(original);
    try{
      if(viewport==='portrait')await page.locator('#portrait').click();
      await page.locator('#weapon-left').click();
      await page.locator('#weapon-right').click();
      await page.locator('#fov').click();
      const before=await snapshot(page);
      assert.equal(before.left,'false');assert.equal(before.right,'false');assert.equal(before.fov,'FOV: 70°');
      await page.locator('#reset').click();
      const after=await snapshot(page);
      // The product behavior is RED: these actual original R1 values should have stayed.
      assert.equal(after.left,'true');assert.equal(after.right,'true');assert.equal(after.fov,'FOV: 55°');
      assert.equal(after.width,before.width);assert.equal(after.height,before.height);
    }finally{await page.close();}
  });
  test('R2 camera-only reset retains FOV, two weapon states and viewport ('+viewport+')',async()=>{
    const page=await open(current);
    try{
      if(viewport==='portrait')await page.locator('#portrait').click();
      await page.locator('#weapon-left').click();
      await page.locator('#weapon-right').click();
      await page.locator('#fov').click();
      const before=await snapshot(page);
      assert.equal(before.left,'false');assert.equal(before.right,'false');assert.equal(before.fov,'FOV: 70°');
      await page.locator('#auto').click();
      await page.locator('#reset').click();
      const after=await snapshot(page);
      assert.equal(after.left,'false');assert.equal(after.right,'false');assert.equal(after.fov,'FOV: 70°');
      assert.equal(after.width,before.width);assert.equal(after.height,before.height);
      assert.match(await page.locator('#recommendation').textContent(),/tylko kamerę/);
      assert.ok(after.metric.includes('aktywnych modułów'));
    }finally{await page.close();}
  });
  test('R2 explicit full reset changes all experiment state back to defaults ('+viewport+')',async()=>{
    const page=await open(current);
    try{
      if(viewport==='portrait')await page.locator('#portrait').click();
      await page.locator('#weapon-left').click();
      await page.locator('#weapon-right').click();
      await page.locator('#fov').click();
      await page.locator('#reset-all').click();
      const after=await snapshot(page);
      assert.equal(after.width,900);assert.equal(after.height,506);
      assert.equal(after.left,'true');assert.equal(after.right,'true');assert.equal(after.fov,'FOV: 55°');
      assert.equal(after.issues.length,4);
      assert.match(await page.locator('#recommendation').textContent(),/całe doświadczenie/);
    }finally{await page.close();}
  });
}
test('R2 camera reset does not erase different left/right weapon states',async()=>{
  const page=await open(current);
  try{
    await page.locator('#weapon-left').click();
    await page.locator('#reset').click();
    assert.equal(await page.locator('#weapon-left').getAttribute('aria-pressed'),'false');
    assert.equal(await page.locator('#weapon-right').getAttribute('aria-pressed'),'true');
    assert.equal((await page.locator('#issues li').count()),3);
  }finally{await page.close();}
});
test('R2 FOV remains 40 after camera-only reset',async()=>{
  const page=await open(current);
  try{
    await page.locator('#fov').click();await page.locator('#fov').click();
    assert.equal(await page.locator('#fov').textContent(),'FOV: 40°');
    await page.locator('#reset').click();
    assert.equal(await page.locator('#fov').textContent(),'FOV: 40°');
  }finally{await page.close();}
});
test('R2 preserves accessible toggles and result status after 50 interactions',async()=>{
  const page=await open(current);
  try{
    for(let i=0;i<50;i++){
      await page.locator(i%2?'#weapon-left':'#weapon-right').click();
      if(i%10===0)await page.locator('#reset').click();
    }
    assert.equal(await page.locator('#status').getAttribute('role'),'status');
    assert.equal(await page.locator('#status').getAttribute('aria-live'),'polite');
    assert.equal((await page.locator('#metric').textContent()).includes('aktywnych modułów'),true);
  }finally{await page.close();}
});
test('R2 can use keyboard for true semantic independent reset buttons',async()=>{
  const page=await open(current);
  try{
    await page.locator('#weapon-left').focus();
    await page.keyboard.press('Enter');
    assert.equal(await page.locator('#weapon-left').getAttribute('aria-pressed'),'false');
    await page.locator('#reset').focus();await page.keyboard.press('Space');
    assert.equal(await page.locator('#weapon-left').getAttribute('aria-pressed'),'false');
    await page.locator('#reset-all').focus();await page.keyboard.press('Enter');
    assert.equal(await page.locator('#weapon-left').getAttribute('aria-pressed'),'true');
  }finally{await page.close();}
});
test('Chromium interactions completed without uncaught page JavaScript exceptions',()=>assert.deepEqual(errors,[]));
