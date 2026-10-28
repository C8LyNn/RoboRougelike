#!/usr/bin/env python3
"""Real Chromium viewer QA for exact 8 original beacon assets (NOT Unity)."""
import json
import sys
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT=Path(__file__).resolve().parents[4]
SRC=ROOT/"Research/B01-32/P17/SalvageBeacon"
OUT=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else ROOT/"build/b01-32-p17-salvage"
OUT.mkdir(parents=True,exist_ok=True)
checks=[]
errors=[]
def check(value,name):
    assert value,name
    checks.append(name)
    print("CHROMIUM_REAL_PASS",name)
with sync_playwright() as playwright:
    browser=playwright.chromium.launch(headless=True,args=["--no-sandbox","--allow-file-access-from-files"])
    page=browser.new_page(viewport={"width":1080,"height":860},device_scale_factor=1)
    page.on("pageerror",lambda e:errors.append(str(e)))
    page.goto((SRC/"viewer.html").resolve().as_uri())
    page.wait_for_function("document.querySelector('#beacon').complete && document.querySelector('#beacon').naturalWidth===512")
    img=page.locator("#beacon")
    check(img.get_attribute("src")=="frames/beacon-0.svg","starts actual frame 0 source")
    check("Frame 0" in page.locator("#status").inner_text(),"initial phase label")
    check(img.is_visible(),"initial visible beacon")
    page.locator("#next").click()
    check(img.get_attribute("src")=="frames/beacon-1.svg","next activates next actual SVG")
    page.locator("#previous").click()
    check(img.get_attribute("src")=="frames/beacon-0.svg","previous reverses phase")
    page.locator("#previous").click()
    check(img.get_attribute("src")=="frames/beacon-7.svg","previous wraps back to frame 7")
    page.locator("#frame").evaluate("(el)=>{el.value='5';el.dispatchEvent(new Event('input',{bubbles:true}));}")
    check(img.get_attribute("src")=="frames/beacon-5.svg","frame input scrubs exact phase")
    page.locator("#play").click()
    check(page.locator("#play").get_attribute("aria-pressed")=="true","play starts animation")
    page.wait_for_function("document.querySelector('#beacon').getAttribute('src') !== 'frames/beacon-5.svg'",timeout=3500)
    check("frames/beacon-" in img.get_attribute("src"),"animation advances to another original frame")
    page.locator("#play").click()
    check(page.locator("#play").get_attribute("aria-pressed")=="false","pause stops animation")
    page.locator("#off").click()
    check(img.is_hidden(),"VFX OFF suppresses visual")
    check(page.locator("#off").get_attribute("aria-pressed")=="true","VFX OFF toggle exposes accessible state")
    check("VFX OFF" in page.locator("#status").inner_text(),"VFX OFF live text")
    page.locator("#play").click()
    check(page.locator("#play").get_attribute("aria-pressed")=="false","OFF does not restart animation")
    page.locator("#off").click()
    check(img.is_visible() and "VFX ON" in page.locator("#status").inner_text(),"ON resumes visible phase")
    page.locator("#frame").evaluate("(el)=>{el.value='2';el.dispatchEvent(new Event('input',{bubbles:true}));}")
    page.screenshot(path=str(OUT/"browser-1080-salvage-beacon.png"),full_page=True)
    check(page.locator(".controls").is_visible(),"1080 controls visible")
    page.set_viewport_size({"width":720,"height":880})
    page.screenshot(path=str(OUT/"browser-720-salvage-beacon.png"),full_page=True)
    check(page.locator("#next").is_visible() and page.locator("#frame").is_visible(),"720 controls visible")
    check(page.evaluate("document.documentElement.scrollWidth <= window.innerWidth+1"),"720 no horizontal overflow")
    check(not errors,"zero browser uncaught JS errors")
    browser.close()
receipt={"kind":"real-Chromium-original-beacon-HTML",
 "checks":checks,"passed":len(checks),"failed":0,
 "viewport_widths":[720,1080],"js_exceptions":errors,"unity_native":"NOT_RUN"}
(OUT/"browser-receipt.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf8")
print(f"BROWSER_WAVE02_ACCEPTANCE_DONE {len(checks)}/{len(checks)}")
