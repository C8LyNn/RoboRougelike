#!/usr/bin/env python3
"""REAL headless Chromium interaction smoke for B01-32 owned research HTML.
No Unity/browser game integration claims. Requires playwright chromium installed.
"""
import json
import sys
from pathlib import Path
from playwright.sync_api import sync_playwright

root=Path(__file__).resolve().parents[2]
html=(root / "Research/B01-32/P15/MuzzleTrail/viewer.html").resolve()
out=Path(sys.argv[1]) if len(sys.argv)>1 else root / "build/b01-32-p15-art"
out.mkdir(parents=True,exist_ok=True)
issues=[]
checks=[]
def expect(ok,label):
    if not ok:raise AssertionError(label)
    checks.append(label)
    print("CHROMIUM_CONTROL_PASS",label)

with sync_playwright() as playwright:
    browser=playwright.chromium.launch(headless=True,args=["--no-sandbox","--allow-file-access-from-files"])
    page=browser.new_page(viewport={"width":1280,"height":1000},device_scale_factor=1)
    page.on("pageerror", lambda error: issues.append(str(error)))
    page.goto(html.as_uri())
    image=page.locator("#keyframe")
    page.wait_for_function("document.querySelector('#keyframe').complete && document.querySelector('#keyframe').naturalWidth > 0")
    expect(image.get_attribute("src")=="frames/00-idle.svg", "initial inactive frame")
    expect(page.locator("#seek").input_value()=="0", "initial slider")
    page.locator("#next").click()
    expect(image.get_attribute("src")=="frames/01-charge.svg", "next button")
    page.locator("#prev").click()
    expect(image.get_attribute("src")=="frames/00-idle.svg", "previous button")
    page.locator("#seek").evaluate("(el) => {el.value='3';el.dispatchEvent(new Event('input',{bubbles:true}))}")
    page.wait_for_function("document.querySelector('#keyframe').naturalWidth>0")
    expect(image.get_attribute("src")=="frames/03-peak.svg", "scrub to peak")
    page.screenshot(path=str(out / "real-chromium-viewer-peak.png"),full_page=True)
    page.locator("#off").click()
    expect(image.get_attribute("src")=="frames/00-idle.svg", "VFX OFF returns idle")
    expect(page.locator("#play").get_attribute("aria-pressed")=="false","OFF pauses playback")
    page.locator("#play").click()
    expect(page.locator("#play").get_attribute("aria-pressed")=="true","play starts")
    page.wait_for_timeout(280)
    expect(image.get_attribute("src")!="frames/00-idle.svg","timer advances SVG frame")
    page.locator("#play").click()
    expect(page.locator("#play").get_attribute("aria-pressed")=="false","pause stops playback")
    page.locator("#off").click()
    page.set_viewport_size({"width":720,"height":880})
    page.wait_for_function("document.querySelector('#keyframe').complete && document.querySelector('#keyframe').naturalWidth > 0")
    page.screenshot(path=str(out / "real-chromium-viewer-off-720.png"),full_page=True)
    expect(not issues, "no Chromium JavaScript page errors: "+repr(issues))
    browser.close()

(out / "chromium_receipt.json").write_text(json.dumps({"browser":"Playwright real headless Chromium",
    "checks":checks,"count":len(checks),"javascript_errors":issues,"unity":"NOT_RUN"},indent=2)+"\n")
print(f"BROWSER_INTERACTION_PASS {len(checks)}/{len(checks)} actual Chromium input controls and screenshot")
