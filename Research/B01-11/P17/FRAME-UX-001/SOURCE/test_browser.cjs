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

   // Actual bytes: generate a real raster PNG and upload it through the original file input.
   const inputPngBase64=await page.evaluate(()=>{
    const c=document.createElement('canvas');c.width=640;c.height=360;
    const g=c.getContext('2d');g.fillStyle='#1374aa';g.fillRect(0,0,640,360);
    g.fillStyle='#f5b842';g.fillRect(290,150,60,60);
    return c.toDataURL('image/png').split(',')[1];
   });
   const pngBytes=Buffer.from(inputPngBase64,'base64');
   await page.locator('#imageInput').setInputFiles({name:'native-ui-sample.png',mimeType:'image/png',buffer:pngBytes});
   await page.waitForFunction(()=>document.querySelector('#imageMeta').textContent.includes('640×360'));
   await check('real PNG file input updates canvas dimensions',()=>page.evaluate(()=>document.querySelector('#surface').width===640&&document.querySelector('#surface').height===360));
   await check('real PNG file name retained',async()=>(await page.locator('#imageMeta').innerText()).includes('native-ui-sample.png'));
   await check('new image clears stale annotations',()=>page.evaluate(()=>window.FrameUXReview.getState().annotations.length===0));
   await check('uploaded source pixels actually drawn',()=>page.evaluate(()=>{
     const p=document.querySelector('#surface').getContext('2d').getImageData(10,10,1,1).data;
     return p[0]===19&&p[1]===116&&p[2]===170;
   }));
   // A superficially valid JSON report must fail closed on a DIFFERENT original canvas size.
   await page.locator('#importInput').setInputFiles({name:'wrong-frame.json',mimeType:'application/json',buffer:Buffer.from(JSON.stringify(json))});
   await page.waitForFunction(()=>document.querySelector('#status').textContent.includes('Import odrzucony'));
   await check('different image dimensions reject imported JSON',async()=>(await page.locator('#status').innerText()).includes('dimensions do not match'));
   await check('wrong-dimension import preserves current image',()=>page.evaluate(()=>document.querySelector('#surface').width===640&&document.querySelector('#surface').height===360));
   await check('wrong-dimension import preserves empty records',()=>page.evaluate(()=>window.FrameUXReview.getState().annotations.length===0));
   await page.locator('#x').fill('0.25');await page.locator('#y').fill('0.3');await page.locator('#w').fill('0.2');await page.locator('#h').fill('0.15');
   await page.locator('#note').fill('Obscured warning in actual imported frame');
   await page.locator('#addManual').click();
   await check('annotate imported PNG, not the mock',()=>page.evaluate(()=>window.FrameUXReview.getState().annotations.length===1&&window.FrameUXReview.getState().width===640));
   const pngDownloadPromise=page.waitForEvent('download');
   await page.locator('#exportPNG').click();
   const exportedPNG=await pngDownloadPromise;
   const pngOutput=fs.readFileSync(await exportedPNG.path());
   await check('real PNG export header and 640x360 dimensions',()=>pngOutput.subarray(0,8).equals(Buffer.from('89504e470d0a1a0a','hex'))&&pngOutput.readUInt32BE(16)===640&&pngOutput.readUInt32BE(20)===360);
   await check('exported PNG preserves original imported pixel data',()=>page.evaluate(async encoded=>{
     const pic=new Image();pic.src='data:image/png;base64,'+encoded;await pic.decode();
     const c=document.createElement('canvas');c.width=pic.naturalWidth;c.height=pic.naturalHeight;c.getContext('2d').drawImage(pic,0,0);
     const p=c.getContext('2d').getImageData(10,10,1,1).data;
     return p[0]===19&&p[1]===116&&p[2]===170;
   },pngOutput.toString('base64')));
   // Invalid MIME must be refused without altering the already loaded image or annotations.
   await page.locator('#imageInput').setInputFiles({name:'unsupported.txt',mimeType:'text/plain',buffer:Buffer.from('not a picture')});
   await page.waitForFunction(()=>document.querySelector('#status').textContent.includes('Niepoprawny format'));
   await check('invalid image MIME rejected',async()=>(await page.locator('#status').innerText()).includes('Niepoprawny format'));
   await check('invalid image input preserves annotated screenshot',()=>page.evaluate(()=>window.FrameUXReview.getState().annotations.length===1&&document.querySelector('#surface').width===640));

   await check('no horizontal page overflow',()=>page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth+1));
   await check('zero browser errors',()=>jsErrors.length===0);
   await page.close();
  }
 }finally{await browser.close();}
 const report={suite:'FRAME-UX-001 actual Chromium interactive browser tests',browser:candidate,passed:cases.length-failed,failed,cases};
 console.log(JSON.stringify(report,null,2));if(failed)process.exitCode=1;
})().catch(e=>{console.error(e);process.exitCode=1;});
