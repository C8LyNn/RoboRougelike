#!/usr/bin/env python3
"""B01-60 PASS15: actual original B01-12 HUD3 browser combat-state transitions.

Fetch an IMMUTABLE B01-12 HTML Git blob, serve via file:// to real Chromium
Playwright, execute UI controls, capture screenshots and strict observations.
Research-only HTML, no human study or canonical Unity runtime.
"""
import hashlib
import json
import subprocess
import sys
import tempfile
from pathlib import Path

SOURCE_SHA = "f7169efa7291641fbae7f5e81a224adc586d56f8"
SOURCE_PATH = "Research/B01-12/W70/HUD3-overflow.html"
SOURCE_BLOB = "e73b57e52ca0706bcf739340f5b06dd66b55e104"
ROOT = Path(__file__).resolve().parents[3]

def cmd(*args):
    return subprocess.run([str(x) for x in args], check=True, stdout=subprocess.PIPE,
                          stderr=subprocess.PIPE).stdout

def original_source(tmp):
    cmd("git", "-C", ROOT, "fetch", "--no-tags", "origin", SOURCE_SHA)
    html = cmd("git", "-C", ROOT, "show", f"{SOURCE_SHA}:{SOURCE_PATH}")
    out = tmp / "original-b01-12-HUD3.html"
    out.write_bytes(html)
    actual = cmd("git", "-C", ROOT, "hash-object", out).decode().strip()
    if actual != SOURCE_BLOB:
        raise AssertionError(f"ORIGINAL_B01_12_HTML_BLOB_CHANGED:{actual}")
    return out, hashlib.sha256(html).hexdigest()

def main():
    from playwright.sync_api import sync_playwright
    cases = []
    evidence = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else Path(tempfile.mkdtemp(prefix="b0160-hud3-"))
    evidence.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="b0160-hud3-source-") as td:
        original, sha256 = original_source(Path(td))
        with sync_playwright() as p:
            browser = p.chromium.launch(headless=True)
            page = browser.new_page(viewport={"width":720,"height":1000},device_scale_factor=1)
            errors=[]
            page.on("pageerror", lambda exc: errors.append(str(exc)))
            page.goto(original.as_uri(), wait_until="load")
            def record(name, observed, expected=True, expected_red=False):
                correct=(observed == expected)
                cases.append({"case":name,"observed":observed,"expected":expected,
                              "status":("KNOWN_RESEARCH_DEFECT_RED" if expected_red and correct
                                        else "BROWSER_PASS" if correct else "UNEXPECTED_FAIL")})
            def grp(letter):
                return page.locator(f'.group[aria-label="Grupa {letter}"]').inner_text()
            def ids():
                return page.locator('#topRows .slot').evaluate_all(
                    "(nodes)=>nodes.map(x=>Number(x.dataset.gunId))")
            def select(f):
                page.select_option("#fixture",f)
            select("F1")
            record("F1_TWO_GROUP_ALARMS_BOTH_VISIBLE",("PRZEGRZANIE" in grp("A") and
                "ZACIĘCIE" in grp("B") and "Nie mogą teraz strzelać: 1" in grp("A")
                and "Nie mogą teraz strzelać: 1" in grp("B")))
            record("F1_TOP_PRIORITY_EXPOSES_GUN_5_AND_6",ids()==[5,6])
            page.screenshot(path=str(evidence/"f1-two-groups.png"),full_page=True)
            select("F5")
            record("F5_TWO_ALERTS_ONLY_A",("PRZEGRZANIE" in grp("A") and "EMP" in grp("A")
                and "Brak alarmów" in grp("B") and ids()==[3,5]))
            page.click("#resolveA")
            record("F5_FIRST_RESOLUTION_LEAVES_EMP",("PRZEGRZANIE" not in grp("A") and
                "EMP" in grp("A") and ids()[0]==5))
            page.click("#resolveA")
            record("F5_SECOND_RESOLUTION_CLEAR",("Brak alarmów" in grp("A")
                and "Nie mogą teraz strzelać: 0" in grp("A")))
            select("F6")
            page.click("#swap")
            record("F6_SWAP_MAINTAINS_A_B_ALARM_PROVENANCE",("ZACIĘCIE" in grp("A")
                and "PRZEGRZANIE" in grp("B")
                and page.locator('#topRows .slot[data-gun-id="6"]').inner_text().startswith("A /")
                and page.locator('#topRows .slot[data-gun-id="5"]').inner_text().startswith("B /")))
            page.click("#resolveA")
            record("F6_RESOLVE_A_DOES_NOT_CLEAR_B",("Brak alarmów" in grp("A") and
                "PRZEGRZANIE" in grp("B")))
            select("F2")
            page.click("#modeB")
            record("F2_12_WEAPONS_PRIORITY_EMP_RELOAD",("12 przypisanych" in
                page.locator("#total").inner_text() and ids()==[9,10]))
            page.click("#expand")
            record("F2_OVERFLOW_REVEALS_10_REST",
                (page.locator("#expand").get_attribute("aria-expanded")=="true" and
                page.locator("#overflow li").count()==10 and
                page.locator("#overflow").is_visible()))
            page.click("#resolveA")
            record("F2_A_CLEARED_B_RELOAD_PERSISTS",("Brak alarmów" in grp("A") and
                "PRZEŁADOWANIE" in grp("B")))
            page.click("#resolveB")
            record("F2_ALL_READY_AFTER_BOTH_RESOLUTIONS",
                ("Brak alarmów" in grp("A") and "Brak alarmów" in grp("B") and
                 "12 może strzelać" in page.locator("#total").inner_text()))
            select("F3")
            record("F3_DESTROYED_ZERO_HP_INITIAL_NONOPERATIONAL",(
                "Przypisane: 1 · Sprawne: 0 · Mogą teraz strzelać: 0" in grp("A") and
                page.locator('#topRows .slot[data-status="DESTROYED"]').count()==1))
            page.screenshot(path=str(evidence/"f3-before-resolve.png"),full_page=True)
            page.click("#resolveA")
            revived=("Przypisane: 1 · Sprawne: 1 · Mogą teraz strzelać: 1" in grp("A") and
                     page.locator('#topRows .slot[data-status="READY"]').count()==1)
            # Expected-RED observation: removing a status alarm silently resurrects a 0HP gun.
            # This may be an intentional moderator simulation control. Owner must resolve semantics.
            record("F3_DESTROYED_ZERO_HP_CAN_BE_REVIVED_BY_REMOVE_ALARM",revived,
                   expected_red=True)
            page.screenshot(path=str(evidence/"f3-after-resolve.png"),full_page=True)
            select("F8")
            record("F8_ZERO_WEAPONS_NO_PHANTOM_FIRE",(
                "0 przypisanych" in page.locator("#total").inner_text() and
                "Przypisane: 0" in grp("A") and "Przypisane: 0" in grp("B")
                and "Brak zamontowanej broni" in page.locator("#topRows").inner_text()))
            record("NO_JS_EXCEPTIONS",len(errors)==0)
            browser.close()
    receipt={"role":"B01-60","source_owner":"B01-12","source_commit":SOURCE_SHA,
             "original_html_git_blob":SOURCE_BLOB,"original_html_sha256":sha256,
             "engine":"Playwright Chromium real browser","test_mode":"RESEARCH_ONLY_NO_HUMAN",
             "native_unity":"NOT_RUN","cases":cases,"page_errors":errors,
             "unexpected":sum(x["status"]=="UNEXPECTED_FAIL" for x in cases),
             "research_red":sum(x["status"]=="KNOWN_RESEARCH_DEFECT_RED" for x in cases)}
    (evidence/"receipt.json").write_text(json.dumps(receipt,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print("B0160_HUD3_RESULT_JSON="+json.dumps(receipt,ensure_ascii=False,sort_keys=True))
    if receipt["unexpected"]:
        raise SystemExit("UNEXPECTED_DYNAMIC_HUD3_BROWSER_REGRESSION")
    print("B0160_HUD3_EXECUTED=YES NATIVE_UNITY=NOT_RUN")
if __name__ == "__main__":
    main()
