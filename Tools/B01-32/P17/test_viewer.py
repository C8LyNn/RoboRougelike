#!/usr/bin/env python3
"""Real Chromium interactions and 720px/1080px screenshot evidence, not Unity."""
import json
import sys
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT=Path(__file__).resolve().parents[3]
SOURCE=ROOT/"Research/B01-32/P17/PartDamageDecals"
OUTPUT=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else ROOT/"build/b01-32-p17"
OUTPUT.mkdir(parents=True,exist_ok=True)
checks=[]
errors=[]
def check(ok, what):
    assert ok, what
    checks.append(what)
    print("BROWSER_REAL_PASS",what)

with sync_playwright() as p:
    browser=p.chromium.launch(headless=True,args=["--no-sandbox","--allow-file-access-from-files"])
    page=browser.new_page(viewport={"width":1080,"height":860},device_scale_factor=1)
    page.on("pageerror",lambda err:errors.append(str(err)))
    page.goto((SOURCE/"viewer.html").resolve().as_uri())
    decal=page.locator("#decal")
    page.wait_for_function("document.querySelector('#decal').complete && document.querySelector('#decal').naturalWidth===256")
    check(decal.get_attribute("src")=="frames/clean-0.svg","starts actual clean SVG")
    check(page.locator("#status").inner_text()=="CLEAN / frame 0 / VFX ON","initial text matches frame")
    page.locator("button[data-stage='1']").click()
    check(decal.get_attribute("src")=="frames/abraded-0.svg","abraded transition loads real separate image")
    page.locator("button[data-stage='2']").click()
    check(decal.get_attribute("src")=="frames/fractured-0.svg","fracture transition")
    page.locator("button[data-stage='3']").click()
    check(decal.get_attribute("src")=="frames/critical-0.svg","critical transition")
    page.locator("#frame").evaluate("(e)=>{e.value='2';e.dispatchEvent(new Event('input',{bubbles:true}));}")
    check(decal.get_attribute("src")=="frames/critical-2.svg","manual animation scrub uses real frame")
    page.locator("#off").click()
    check(decal.is_hidden() and page.locator("#off").get_attribute("aria-pressed")=="true","OFF suppresses only cosmetic overlay")
    check("VFX OFF" in page.locator("#status").inner_text(),"OFF status visibly acknowledged")
    page.locator("#off").click()
    check(decal.is_visible(),"ON restores same critical visual state")
    page.locator("#frame").evaluate("(e)=>{e.value='0';e.dispatchEvent(new Event('input',{bubbles:true}));}")
    page.locator("#play").click()
    check(page.locator("#play").get_attribute("aria-pressed")=="true","play button starts live timer")
    page.wait_for_function("document.querySelector('#decal').getAttribute('src') !== 'frames/critical-0.svg'",timeout=4000)
    check(decal.get_attribute("src").startswith("frames/critical-"),"real frame advanced on timer")
    page.locator("#play").click()
    check(page.locator("#play").get_attribute("aria-pressed")=="false","pause stops timer")
    page.set_viewport_size({"width":1080,"height":860})
    page.screenshot(path=str(OUTPUT/"browser-1080-critical.png"),full_page=True)
    page.set_viewport_size({"width":720,"height":940})
    page.screenshot(path=str(OUTPUT/"browser-720-critical.png"),full_page=True)
    check(page.locator("button[data-stage='3']").is_visible(),"720px view retains all stage controls")
    page.locator("#off").click()
    page.screenshot(path=str(OUTPUT/"browser-720-vfx-off.png"),full_page=True)
    check(decal.is_hidden(),"720px VFX OFF still suppresses overlay")
    check(not errors,"no uncaught browser JS exceptions")
    browser.close()
receipt={"kind":"real-headless-Chromium","checks":checks,"count":len(checks),
         "viewport_widths":[720,1080],"console_js_errors":errors,"native_unity":"NOT_RUN"}
(OUTPUT/"browser-receipt.json").write_text(json.dumps(receipt,indent=2)+"\n")
print("BROWSER_ACCEPTANCE_DONE",len(checks),"/",len(checks),"real browser assertions")
