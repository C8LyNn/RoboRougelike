// COURSE-DRIVE-001: actual Chromium page and keyboard/canvas evidence.
// Tests the EXACT standalone file generated from original C# MapGenPlanner outputs.
// Do not call this a Unity/PhysX run or a human playtest.
import test from "node:test";
import assert from "node:assert/strict";
import { chromium } from "playwright";
import { mkdirSync,statSync } from "node:fs";
import { resolve,join } from "node:path";
import { pathToFileURL } from "node:url";

const root=process.env.P17_BLUEPRINTS_DIR;
assert.ok(root,"P17_BLUEPRINTS_DIR is required");
const dir=resolve(root), target=join(dir,"COURSE-DRIVE-001.html");
const screenshots=join(dir,"browser-evidence");
mkdirSync(screenshots,{recursive:true});
const url=pathToFileURL(target).href;
function parse(s) {
  const p=/pozycja X (-?[\d.]+) \/ Z (-?[\d.]+)/.exec(s);
  const t=/· ([\d.]+) s ·/.exec(s);
  if (!p||!t)throw Error("INVALID_GAME_STATUS:"+s);
  return { x:Number(p[1]),z:Number(p[2]),t:Number(t[1]),text:s };
}
async function waitStep(page){await page.waitForTimeout(250);}
async function assertPageSafe(page,width){
  const d=await page.evaluate(()=>{
    const c=document.querySelector("canvas"),ctx=c?.getContext("2d");
    return {
      scroll:document.documentElement.scrollWidth,
      viewport:innerWidth,
      canvasWidth:c?.width,
      canvasHeight:c?.height,
      pixels:ctx?Array.from(ctx.getImageData(c.width/2,c.height/2,1,1).data):[],
      status:document.querySelector("#status")?.textContent||"",
      courseCount:document.querySelector("#course")?.options.length,
    };
  });
  assert.equal(d.courseCount,18,"real 18 original C# maps in browser selector");
  assert.ok(d.canvasWidth>=900 && d.canvasHeight>=800);
  assert.ok(d.scroll<=width+1,"horizontal screen overflow: "+JSON.stringify(d));
  assert.ok(d.status.includes("CHECKPOINT"),"actual runtime not initialized: "+d.status);
  assert.equal(d.pixels.length,4,"canvas 2D painting not available");
}
test("P17 genuine browser keyboard/control/canvas smoke at 720p and 1080p", {timeout:90000}, async()=>{
 const browser=await chromium.launch({headless:true,args:["--disable-gpu"]});
 try {
  for(const [width,height,name] of [[1280,720,"720p"],[1920,1080,"1080p"],[720,900,"portrait"]]){
   const page=await browser.newPage({viewport:{width,height},deviceScaleFactor:1});
   const errors=[];
   page.on("pageerror",e=>errors.push(e.message));
   page.on("console",m=>{if(m.type()==="error")errors.push("CONSOLE_ERROR:"+m.text());});
   page.on("request",req=>{if(/^https?:/i.test(req.url()))errors.push("NETWORK_REQUEST:"+req.url());});
   const response=await page.goto(url,{waitUntil:"load"});
   assert.equal(response?.status(),200);
   await page.waitForFunction(()=>document.querySelector("#status")?.textContent.includes("CHECKPOINT"),null,{timeout:10000});
   await assertPageSafe(page,width);
   const selection=await page.locator("#course option").allTextContents();
   assert.equal(selection.length,18);
   const start=parse(await page.locator("#status").innerText());
   await page.keyboard.down("w");await page.waitForTimeout(700);await page.keyboard.up("w");
   await waitStep(page);
   const moved=parse(await page.locator("#status").innerText());
   assert.ok(moved.z>start.z+0.3,"real W key did not move forward: "+JSON.stringify({start,moved}));
   await page.locator("#pause").click();
   await waitStep(page);
   const paused=parse(await page.locator("#status").innerText());
   assert.match(paused.text,/PAUZA/);
   await page.waitForTimeout(300);
   const paused2=parse(await page.locator("#status").innerText());
   assert.equal(paused2.t,paused.t,"pause must stop fixed-tick movement");
   assert.equal(paused2.z,paused.z,"pause must freeze movement");
   await page.locator("#pause").click();
   await page.locator("#reset").click();
   await waitStep(page);
   const reset=parse(await page.locator("#status").innerText());
   assert.ok(Math.abs(reset.z-start.z)<.01 && Math.abs(reset.x-start.x)<.01);
   assert.ok(reset.t<.3,"reset must clear timer");
   await page.selectOption("#profile","Heavy");
   await waitStep(page);
   assert.match(await page.locator("#caption").innerText(),/Profil Heavy/);
   await page.selectOption("#course",{index:17});
   await waitStep(page);
   assert.match(await page.locator("#caption").innerText(),/Profil Heavy/);
   assert.match(await page.locator("#caption").innerText(),/geometry [0-9a-f]{16}/);
   await page.screenshot({path:join(screenshots,"COURSE-DRIVE-"+name+".png"),fullPage:true});
   assert.ok(statSync(join(screenshots,"COURSE-DRIVE-"+name+".png")).size>5000,"empty real browser screenshot");
   assert.deepEqual(errors,[],"actual JS/window errors or unapproved external requests");
   await page.close();
  }
 } finally {await browser.close();}
 console.log("COURSE_DRIVE_REAL_CHROMIUM_GUI_720_1080_PORTRAIT_PASS=3");
});
test("P17 genuine browser autopilot progresses original waypoint, including one original Heavy map", {timeout:45000}, async()=>{
 const browser=await chromium.launch({headless:true,args:["--disable-gpu"]});
 try {
  const page=await browser.newPage({viewport:{width:1280,height:720}});
  const errors=[];
  page.on("pageerror",e=>errors.push(e.message));
  await page.goto(url,{waitUntil:"load"});
  await page.waitForFunction(()=>document.querySelector("#status")?.textContent.includes("CHECKPOINT"));
  await page.selectOption("#profile","Heavy");
  await page.locator("#auto").check();
  await page.waitForFunction(()=>document.querySelector("#status")?.textContent.includes("CHECKPOINT 1/"),null,{timeout:20000});
  assert.equal(await page.locator("#auto").isChecked(),true);
  assert.deepEqual(errors,[]);
  await page.screenshot({path:join(screenshots,"COURSE-DRIVE-heavy-autopilot.png"),fullPage:true});
  await browser.close();
 } finally {if(browser.isConnected())await browser.close();}
 console.log("COURSE_DRIVE_REAL_CHROMIUM_HEAVY_AUTOPILOT_PROGRESS_PASS");
});
