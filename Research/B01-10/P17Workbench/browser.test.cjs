#!/usr/bin/env node
"use strict";
/* R5WB001 W02: actual Playwright Chromium browser QA, not Unity.
 Run from repository root:
 npm install --no-save --no-package-lock playwright@1.56.1
 npx playwright install chromium
 node Research/B01-10/P17Workbench/browser.test.cjs
 No network actions are made by the R5WB HTML itself. */
const assert=require("node:assert/strict");
const fs=require("node:fs");
const path=require("node:path");
const {pathToFileURL}=require("node:url");
const {chromium}=require("playwright");
const root=__dirname,html=path.join(root,"R5Workbench.html");
const shots=process.env.B01_10_BROWSER_SHOTS||
  path.join(root,".qa-screenshots");
const sizes=[{width:1280,height:720},{width:1920,height:1080},{width:720,height:900}];
let pass=0,fail=0; const evidence=[];
async function t(name,fn){
 try{await fn();pass++;evidence.push({name,status:"PASS"});console.log("PASS "+name);}
 catch(e){fail++;evidence.push({name,status:"FAIL",message:e.message});console.error("FAIL "+name+" "+e.stack);}
}
async function main(){
 fs.mkdirSync(shots,{recursive:true});
 const browser=await chromium.launch({headless:true});
 try{
  for(const viewport of sizes){
   const context=await browser.newContext({viewport,acceptDownloads:true});
   const page=await context.newPage();
   const jsErrors=[];
   page.on("pageerror",e=>jsErrors.push(e.message));
   await page.goto(pathToFileURL(html).href,{waitUntil:"load"});
   const label=viewport.width+"x"+viewport.height;
   await t(label+" initial six R5 stage options",async()=>{
    assert.equal(await page.locator("#rows tr").count(),6);
    assert.equal(await page.locator("#stage option").count(),6);
    assert.match(await page.locator("#indicators").innerText(),/Zgodny schemat R5/);
   });
   await t(label+" page fits viewport horizontally",async()=>{
    const dim=await page.evaluate(()=>({window:window.innerWidth,html:document.documentElement.scrollWidth,body:document.body.scrollWidth}));
    assert.ok(dim.html<=dim.window+2,JSON.stringify(dim));
    assert.ok(dim.body<=dim.window+2,JSON.stringify(dim));
   });
   await t(label+" add, title save, and enemy threat preview",async()=>{
    await page.locator("#stage").selectOption("2");
    await page.locator("#add").click();
    assert.equal(await page.locator("#rows tr").count(),7);
    await page.locator("#Title").fill("Nowe starcie z Workbench W02");
    await page.locator("#ScrapReward").fill("240");
    await page.locator("#save").click();
    assert.match(await page.locator("#error").innerText(),/Zapisano/);
    assert.ok((await page.locator("#rows").innerText()).includes("Nowe starcie z Workbench W02"));
    assert.ok((await page.locator("#rows").innerText()).includes("240"));
   });
   await t(label+" failed edit cannot corrupt active data",async()=>{
    const before=await page.locator("#rows").innerText();
    await page.locator("#MoveSpeed").fill("7");
    await page.locator("#save").click();
    assert.match(await page.locator("#error").innerText(),/Odmowa bez utraty danych/);
    assert.equal(await page.locator("#rows").innerText(),before);
   });
   await t(label+" actual JSON export, reset, re-import",async()=>{
    await page.locator("#export").click();
    const json=await page.locator("#json").inputValue();
    const catalog=JSON.parse(json);
    assert.equal(catalog.Stages.length,6);
    assert.equal(catalog.Stages.flatMap(x=>x.Options).length,7);
    await page.locator("#new").click();
    assert.equal(await page.locator("#rows tr").count(),6);
    await page.locator("#json").fill(json);
    await page.locator("#import").click();
    assert.equal(await page.locator("#rows tr").count(),7);
    assert.ok((await page.locator("#rows").innerText()).includes("Nowe starcie z Workbench W02"));
   });
   await t(label+" invalid JSON import refused atomically",async()=>{
    const before=await page.locator("#rows").innerText();
    await page.locator("#json").fill("{invalid JSON");
    await page.locator("#import").click();
    assert.match(await page.locator("#error").innerText(),/Odmowa bez utraty danych/);
    assert.equal(await page.locator("#rows").innerText(),before);
   });
   await t(label+" native browser download is game-format JSON",async()=>{
    const downloadPromise=page.waitForEvent("download");
    await page.locator("#download").click();
    const download=await downloadPromise;
    assert.equal(download.suggestedFilename(),"R5_Encounters_BAL17_Custom.json");
    const stream=await download.createReadStream();
    const chunks=[];
    for await (const chunk of stream)chunks.push(chunk);
    const doc=JSON.parse(Buffer.concat(chunks).toString("utf8"));
    assert.equal(doc.Stages.length,6);
    assert.equal(doc.Stages.flatMap(x=>x.Options).length,7);
   });
   await t(label+" undo reverts import",async()=>{
    await page.locator("#undo").click();
    assert.equal(await page.locator("#rows tr").count(),6);
   });
   await t(label+" no uncaught browser JS errors",async()=>assert.deepEqual(jsErrors,[]));
   const file=path.join(shots,"r5wb001_"+label+".png");
   await page.screenshot({path:file,fullPage:true});
   console.log("SCREENSHOT "+file+" "+fs.statSync(file).size+" bytes");
   await context.close();
  }
 }finally{await browser.close();}
 const report={name:"B01-10-R5WB001-W02-BROWSER",testEngine:"REAL Chromium + Playwright",result:{pass,fail,total:pass+fail},screenshots:sizes.map(s=>"r5wb001_"+s.width+"x"+s.height+".png"),cases:evidence};
 fs.writeFileSync(path.join(shots,"QA_BROWSER.json"),JSON.stringify(report,null,2)+"\n");
 console.log("TOTAL B01-10-R5WB001-W02 BROWSER PASS="+pass+" FAIL="+fail+" | CHROMIUM=ACTUALLY_EXECUTED | UNITY=NOT_RUN");
 if(fail)process.exitCode=1;
}
main().catch(e=>{console.error("FATAL "+e.stack);process.exitCode=1;});
