#!/usr/bin/env node
'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const {chromium}=require('playwright-core');
(async()=>{
 const candidate=[process.env.CHROME_PATH,'/usr/bin/google-chrome','/usr/bin/chromium'].find(p=>p&&fs.existsSync(p));
 if(!candidate)throw Error('Chromium/Chrome executable unavailable; browser tests NOT_RUN, not PASS');
 const browser=await chromium.launch({headless:true,executablePath:candidate,args:['--no-sandbox']});
 const cases=[];let failed=0;
 try{
  for(const [width,height] of [[320,720],[720,960],[1280,800],[1920,1080]]){
   const page=await browser.newPage({viewport:{width,height},acceptDownloads:true});const jsErrors=[];
   page.on('pageerror',e=>jsErrors.push(String(e)));
   await page.goto('file://'+path.join(__dirname,'index.html'),{waitUntil:'load'});
   const check=async(name,fn)=>{try{assert.ok(await fn(),name);cases.push({test:width+'x'+height+'/'+name,status:'PASS'});}catch(e){failed++;cases.push({test:width+'x'+height+'/'+name,status:'FAIL',error:String(e)});}};
   await check('original core loaded',()=>page.evaluate(()=>!!window.FrameUXCore));
   await check('example is explicitly synthetic',async()=>(await page.locator('#imageMeta').innerText()).includes('SYNTETYCZNY'));
   await check('dimensions 1280x720',()=>page.evaluate(()=>document.querySelector('#surface').width===1280&&document.querySelector('#surface').height===720));
   await check('blank annotations',async()=>await page.locator('#annotations li').count()===0);
   await page.locator('#note').fill('<img src=x onerror=alert(1)>');
   await page.locator('#addManual').click();
   await check('add manual',async()=>await page.locator('#annotations li').count()===1);
   await check('safe text insertion',()=>page.evaluate(()=>document.querySelectorAll('#annotations img').length===0));
   await check('one P1',async()=>(await page.locator('#summary').innerText()).includes('P1 1'));
   await check('status announces added',async()=>(await page.locator('#status').innerText()).includes('Dodano'));
   const downloadPromise=page.waitForEvent('download');await page.locator('#exportJSON').click();const download=await downloadPromise;
   const disk=await download.path(),json=JSON.parse(fs.readFileSync(disk,'utf8'));
   await check('export JSON contains 1 real record',()=>json.schema==='frame-ux-001/v1'&&json.annotations.length===1);
   await check('export excludes pixels',()=>!Object.hasOwn(json,'pixelData')&&!Object.hasOwn(json,'imageData'));
   await page.locator('#annotations li button').click();
   await check('removal in UI',async()=>await page.locator('#annotations li').count()===0);
   await page.locator('#importInput').setInputFiles({name:'review.json',mimeType:'application/json',buffer:Buffer.from(JSON.stringify(json))});
   await page.locator('#annotations li').first().waitFor({state:'visible',timeout:5000});
   await check('reimport returns annotation',async()=>await page.locator('#annotations li').count()===1);
   await page.locator('#importInput').setInputFiles({name:'bad.json',mimeType:'application/json',buffer:Buffer.from('{"notSchema":true}')});
   await check('malformed schema rejected',async()=>(await page.locator('#status').innerText()).includes('odrzucony'));
   await check('rejected import preserves state',async()=>await page.locator('#annotations li').count()===1);
   await page.locator('#reset').click();
   await check('reset clears all',async()=>await page.locator('#annotations li').count()===0);
   const box=await page.locator('#surface').boundingBox();const x1=box.x+box.width*.14,x2=box.x+box.width*.30,y1=box.y+box.height*.20,y2=box.y+box.height*.37;
   await page.mouse.move(x1,y1);await page.mouse.down();await page.mouse.move(x2,y2,{steps:5});await page.mouse.up();
   await check('pointer drag adds new rectangle',async()=>await page.locator('#annotations li').count()===1);
   await check('rectangle coordinates normalized',()=>page.evaluate(()=>{const a=window.FrameUXReview.getState().annotations[0];return a.x>=0&&a.y>=0&&a.x+a.w<=1&&a.y+a.h<=1}));
   await page.locator('#zones').click();
   await check('reference zones toggle',async()=>await page.locator('#zones').getAttribute('aria-pressed')==='false');
   await check('no horizontal page overflow',()=>page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth+1));
   await check('zero browser errors',()=>jsErrors.length===0);
   await page.close();
  }
 }finally{await browser.close();}
 const report={suite:'FRAME-UX-001 actual Chromium interactive browser tests',browser:candidate,passed:cases.length-failed,failed,cases};
 console.log(JSON.stringify(report,null,2));if(failed)process.exitCode=1;
})().catch(e=>{console.error(e);process.exitCode=1;});
