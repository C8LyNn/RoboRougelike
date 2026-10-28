#!/usr/bin/env python3
"""Independent ORIGINAL B01-12 garage HTML keyboard focus regression RED->GREEN.

Physically executes owner ORIGINAL before/after Git blobs in Chromium.
Checks keyboard focus continuity after render() replaces part controls, and
the next Enter key action. NOT human study / canonical game / Unity test.
"""
import json
import subprocess
import sys
import tempfile
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[3]
PATH = "Research/B01-12/W74/garage-readiness.html"
VERSIONS = {
    "before": ("ff0a7366bd12222cf0a547c82b7715177fd13aa4",
               "564354278cce2de03cf20a9787e1da88c3bfef05"),
    "after": ("74e5b8b158ec5068cdfcee557bf8e13bb1b96477",
              "c9fb2cf145546bb70c3b0ecad23ddeca38faae3f"),
}

def git(*argv):
    p=subprocess.run(["git","-C",str(ROOT),*argv],check=True,capture_output=True)
    return p.stdout

def get_originals(root):
    files={}
    for label,(commit,blob) in VERSIONS.items():
        git("fetch","--no-tags","origin",commit)
        raw=git("show",f"{commit}:{PATH}")
        f=root/f"owner-{label}.html"
        f.write_bytes(raw)
        sha=git("hash-object",f).decode().strip()
        if sha!=blob:
            raise AssertionError(f"ORIGINAL_GIT_BLOB_DRIFT {label} {sha}")
        files[label]=f
    return files

def main():
    evidence=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else Path(tempfile.mkdtemp(prefix="b0160_garage_"))
    evidence.mkdir(parents=True,exist_ok=True)
    cases=[]
    with tempfile.TemporaryDirectory(prefix="b0160-garage-source-") as td, sync_playwright() as pw:
        files=get_originals(Path(td))
        browser=pw.chromium.launch(headless=True)
        for width in (320,720):
            for label in ("before","after"):
                page=browser.new_page(viewport={"width":width,"height":1000})
                page.goto(files[label].as_uri(),wait_until="load")
                problems=[]
                page.on("pageerror",lambda err:problems.append(str(err)))
                expectation=(label=="after")
                def record(name,got,expected=expectation):
                    # For original BEFORE, regressions are expected RED. For AFTER, actual UI must work.
                    matched=(got==expected)
                    cases.append({"width":width,"version":label,"test":name,
                        "observed":got,"expected":expected,
                        "status":("GREEN_AFTER_FIX" if label=="after" and matched else
                                  "EXPECTED_RED_BEFORE_FIX" if label=="before" and matched else
                                  "UNEXPECTED_FAIL")})
                def focus(key,action):
                    return page.evaluate("""([key,action])=>{
                        const e=document.activeElement;
                        return e?.tagName==='BUTTON'&&e.dataset.key===key&&e.dataset.action===action;
                    }""",[key,action])
                def click(key,action):
                    page.locator(f'[data-slot="{key}"] button[data-action="{action}"]').click()
                def reset():
                    page.select_option("#preset","both")
                reset()
                record("TIPS_OFF_BOTH_CRITICAL_VISIBLE",page.locator("#tips").is_checked()==False and
                       set(page.locator(".critical .code").all_text_contents())==
                       {"CRITICAL_MOBILITY","CRITICAL_WEAPON"},expected=True)
                click("gun","toggle")
                record("MOUNT_PRESERVES_KEYBOARD_FOCUS",focus("gun","toggle"))
                page.keyboard.press("Enter")
                record("ENTER_TOGGLES_FOCUSED_MOUNTED_GUN",
                       "Nie zamontowano" in page.locator('[data-slot="gun"] [data-status="gun"]').inner_text())
                reset()
                click("gun","toggle")
                click("gun","damage")
                record("DAMAGE_FOCUS_MOVES_TO_REPAIR",focus("gun","repair"))
                page.keyboard.press("Enter")
                record("ENTER_REPAIRS_ZERO_HP",
                       "100 HP" in page.locator('[data-slot="gun"] [data-status="gun"]').inner_text() and
                       "CRITICAL_WEAPON" not in page.locator(".critical .code").all_text_contents())
                record("NO_CONSOLE_JS_EXCEPTION",len(problems)==0,expected=True)
                page.screenshot(path=str(evidence/f"garage-{label}-{width}.png"),full_page=True)
                page.close()
        browser.close()
    receipt={
      "qa":"B01-60","owner":"B01-12","original_owner_before":VERSIONS["before"][0],
      "original_owner_after":VERSIONS["after"][0],
      "original_blobs":{k:v[1] for k,v in VERSIONS.items()},
      "browser":"original HTML in real Playwright Chromium",
      "native_unity":"NOT_RUN","human_participants":0,
      "cases":cases,"unexpected":sum(x["status"]=="UNEXPECTED_FAIL" for x in cases)
    }
    (evidence/"garage-keyboard-receipt.json").write_text(json.dumps(receipt,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print("B0160_GARAGE_BROWSER_JSON="+json.dumps(receipt,sort_keys=True,ensure_ascii=False))
    if receipt["unexpected"]:
        raise SystemExit("GARAGE_ORIGINAL_SOURCE_UNEXPECTED_DIFFERENCE")
    print("B0160_GARAGE_BROWSER_ORIGINAL_RED_TO_GREEN=PASS; UNITY_NOT_RUN")
if __name__=="__main__":
    main()
