#!/usr/bin/env python3
"""B01-11 independent exact-blob v2 UX browser review; research only, NO Unity."""
import hashlib
import json
import os
from pathlib import Path
from playwright.sync_api import sync_playwright

SOURCE_SHA = "c920bd3bba7c3b05173225d6d2148bef08e1f319"
EXPECTED_SHA256 = "116660582df599d0d337110e8956a81666b3065a868ae882a435151a43df084e"
EXPECTED_BLOB_SHA1 = "bc17deba3c96307c8179a0ff564b26b3855287ce"
SOURCE = Path(os.getenv("B01_UX_HTML", "/tmp/b01-12-v2-exact.html"))
OUT = Path(os.getenv("B01_UX_OUT", "Research/B01-11/UXU5/output"))
OUT.mkdir(parents=True, exist_ok=True)
data = SOURCE.read_bytes()
assert hashlib.sha256(data).hexdigest() == EXPECTED_SHA256, "SOURCE_SHA256_MISMATCH_FAIL_CLOSED"
blob = hashlib.sha1(b"blob " + str(len(data)).encode() + b"\x00" + data).hexdigest()
assert blob == EXPECTED_BLOB_SHA1, "SOURCE_BLOB_SHA1_MISMATCH_FAIL_CLOSED"
html = data.decode("utf-8")
results = []

def check(cond, message):
    if not cond:
        raise AssertionError(message)

def case(browser, key, fn, width=1440, height=960):
    context = browser.new_context(viewport={"width": width, "height": height})
    page = context.new_page()
    errors = []
    page.on("pageerror", lambda error: errors.append(str(error)))
    try:
        page.set_content(html, wait_until="load")
        fn(page)
        check(not errors, "JS_PAGE_ERRORS: " + repr(errors))
        page.screenshot(path=str(OUT / (key + ".png")), full_page=True)
        results.append({"case": key, "status": "PASS", "browser_js_errors": errors})
        print("PASS", key)
    except Exception as e:
        page.screenshot(path=str(OUT / (key + "-FAIL.png")), full_page=True)
        results.append({"case": key, "status": "FAIL", "message": str(e), "browser_js_errors": errors})
        print("FAIL", key, repr(e))
    finally:
        context.close()

def cell(p, x, y):
    return p.locator(f'.cell[data-x="{x}"][data-y="{y}"]')

def ux01(p):
    p.locator("#preset").select_option("ready")
    check("MOUNT_AUTHORITY: UNVERIFIED" in p.locator("#researchReady").inner_text(), "No persistent UNKNOWN authority")
    check("UNVERIFIED" in p.locator("#warnings").inner_text(), "Green false-ready functional summary")
    p.locator('[data-kind="left"]').click()
    cell(p, 6, 2).hover()
    check("UNMEASURED_PHYSICAL_ENVELOPE" in p.locator("#placement").inner_text(), "Unknown not explicitly explained")
    check(p.locator("#placement").get_attribute("data-severity") == "unknown", "Unknown looks valid")
    cell(p, 6, 2).click()
    check("WSTRZYMANO" in p.locator("#mockInsertFeedback").inner_text(), "Unknown inserted without opt-in")
    p.locator("#mockInsert").check()
    cell(p, 6, 2).click()
    check("NIEZWERYFIKOWANY" in p.locator("#mockInsertFeedback").inner_text(), "Opt-in fixture incorrectly described as mounted")
    check("MOUNT_AUTHORITY: UNVERIFIED" in p.locator("#researchReady").inner_text(), "Opt-in became approved")
    # Persistent critical information must survive ordinary tips toggle
    p.locator("#preset").select_option("minimal")
    p.locator("#tips").uncheck()
    check("Brak działającej mobilności" in p.locator("#warnings").inner_text(), "Critical mobility hidden")
    check("Brak działającej broni" in p.locator("#warnings").inner_text(), "Critical weapon hidden")

def ux02(p):
    p.locator("#preset").select_option("minimal")
    cell(p, 6, 2).hover()
    for option, code in [
        ("missing_socket", "TYPED_SOCKET_UNVERIFIED"),
        ("missing_broadphase", "INCOMPLETE_EXTERNAL_BROADPHASE"),
        ("physics_query", "PHYSICS_QUERY_UNVERIFIED"),
        ("penetration", "PHYSICAL_PENETRATION"),
    ]:
        p.locator("#reasonFixture").select_option(option)
        check(code in p.locator("#layers").inner_text(), f"Reason not in layered context: {code}")
        check(code in p.locator("#placement").inner_text(), f"Reason not at point of action: {code}")
        check("NIEZWERYFIKOWANY" in p.locator("#layers").inner_text() or "ODRZUCONE" in p.locator("#placement").inner_text(), "Mock accepted as physical success")
    check(p.locator("#layers .layer").count() == 3, "Not three separate layers")
    check("MOUNT_AUTHORITY: UNVERIFIED" in p.locator("#researchReady").inner_text(), "Mock approved")

def ux03(p):
    p.locator('[data-variant="c"]').click()
    p.locator("#preset").select_option("overlap")
    check("LOGICAL_OCCUPIED" in p.locator("#placement").inner_text(), "Compact variant loses reason")
    check(p.locator("#whyButton").is_visible(), "No compact details control")
    p.locator("#whyButton").click()
    check(p.locator("#whyButton").get_attribute("aria-expanded") == "true", "Details affordance not expanded")
    check(p.locator("#whyPanel").is_visible(), "Detailed cause not visible")
    check("LOGICAL_OCCUPIED" in p.locator("#whyPanel").inner_text(), "Wrong expanded cause")
    p.locator("#whyButton").click()
    check(p.locator("#whyButton").get_attribute("aria-expanded") == "false", "Details cannot collapse")

def ux04(p):
    p.locator("#tabChl").click()
    p.locator("#begin").click()
    p.locator("#skip").click()
    check("ODRZUCONO" in p.locator("#chlActionResult").inner_text(), "Skip rejection not prominent")
    check("CP 1" in p.locator("#chlActionResult").inner_text(), "No next allowed CP")
    check("0/4" in p.locator("#currentCp").inner_text(), "Skip accidentally credited")
    p.locator("#reverse").click()
    check("wstecz" in p.locator("#chlActionResult").inner_text().lower(), "Reverse not explained")
    p.locator("#graze").click()
    check("koło" in p.locator("#chlActionResult").inner_text().lower(), "Wheel-only rejection not explained")
    p.locator("#reviewReason").select_option("FALL")
    p.locator("#fail").click()
    check("FAILED" in p.locator("#currentState").inner_text(), "Failure state missing")
    check("FALL" in p.locator("#chlState").inner_text(), "Failure reason missing")
    check("Powtórz próbę" in p.locator("#retry").inner_text(), "No prominent retry")
    check(p.locator("#retry").get_attribute("class") == "primary", "Retry not primary at failure")

def ux05(p):
    p.locator("#tabChl").click()
    p.locator("#rigHeavy").click()
    p.locator("#begin").click()
    check("HEAVY-01" in p.locator("#attemptId").inner_text(), "Missing initial heavy attempt ID")
    for _ in range(4):
        p.locator("#next").click()
    p.locator("#finish").click()
    check("COMPLETED" in p.locator("#currentState").inner_text(), "No completed terminal state")
    check("HEAVY-01" in p.locator("#lastResult").inner_text(), "Win attributed incorrectly")
    p.locator("#rigLight").click()
    check(p.locator("#rigHeavy").get_attribute("aria-pressed") == "true", "Terminal rig switched without Retry")
    check("ODMOWA" in p.locator("#chlActionResult").inner_text(), "No visible switch explanation")
    p.locator("#retry").click()
    check("READY" in p.locator("#currentState").inner_text() and "0/4" in p.locator("#currentCp").inner_text(), "Retry carries state forward")
    p.locator("#rigLight").click()
    p.locator("#begin").click()
    check("LIGHT-01" in p.locator("#attemptId").inner_text(), "Light new attempt counter wrong")
    check("HEAVY-01" in p.locator("#lastResult").inner_text(), "Prior result lost/misattributed")
    p.locator("#pause").click()
    check(p.locator("#next").is_disabled(), "CP accepted in pause")
    p.locator("#retry").click()
    check("READY" in p.locator("#currentState").inner_text(), "Paused Retry failed")
    # Modal keyboard trap and focus return (v1 regression)
    p.locator("#tabGarage").click()
    p.locator("#launch").click()
    check(p.locator("#closeDialog").evaluate("(el)=>document.activeElement===el"), "Modal focus not moved")
    p.keyboard.press("Tab")
    check(p.locator("#confirmMock").evaluate("(el)=>document.activeElement===el"), "Modal Tab did not cycle")
    p.keyboard.press("Escape")
    check(not p.locator("#dialogWrap").evaluate("(el)=>el.classList.contains('open')"), "Escape did not close modal")
    check(p.locator("#launch").evaluate("(el)=>document.activeElement===el"), "Focus not restored to launcher")

with sync_playwright() as p:
    browser = p.chromium.launch(headless=True, args=["--no-sandbox"])
    case(browser, "UX-01-UNKNOWN-and-critical", ux01)
    case(browser, "UX-02-layered-reasons", ux02)
    case(browser, "UX-03-compact-cause", ux03)
    case(browser, "UX-04-CHL-failure", ux04)
    case(browser, "UX-05-retry-rig-a11y", ux05, 390, 844)
    browser.close()

receipt = {
    "agent": "B01-11", "task": "UX.U5-PASS3-BROWSER-INDEPENDENT",
    "source_commit": SOURCE_SHA,
    "html_git_blob_sha1": blob, "html_sha256": hashlib.sha256(data).hexdigest(),
    "scope": "Original exact HTML via real Chromium; JS UI only, no Unity/PhysX, no human participants",
    "tests": results,
    "passed": sum(x["status"] == "PASS" for x in results),
    "failed": sum(x["status"] == "FAIL" for x in results),
    "native_unity": "NOT_RUN", "human_users": "NOT_RUN",
}
(OUT / "receipt.json").write_text(json.dumps(receipt, indent=2, ensure_ascii=False), encoding="utf-8")
print(json.dumps({"passed": receipt["passed"], "failed": receipt["failed"]}))
if receipt["failed"]:
    raise SystemExit(1)
