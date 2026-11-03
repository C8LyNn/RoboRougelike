#!/usr/bin/env python3
"""B01-60 independent Chromium QA of the *original* COURSE-DRIVE-001 HTML.
This is a real browser test, NOT a Unity/PhysX/human-playtest certificate.
"""
import argparse
import hashlib
import json
import re
import subprocess
import sys
import traceback
from pathlib import Path
from playwright.sync_api import sync_playwright

UPSTREAM="1a98ea3e1fc26c889c100aa1f554c804c3a47663"
EXPECTED_HTML="1d35d69e6963e0d45046b0f66951beb78f5378b9a00fd19bce18fda31e328990"
BASE="Research/B01-20/P17/COURSE-DRIVE-001/"
BLOBS={
 "core.mjs":"1cf95532ece6ae59a033fd5ef9a795d4b33baab5",
 "viewer-app.js":"79481ff2959f04236e67ce26cbe04c3cf85c7f16",
 "viewer.template.html":"90d44d5ebf49036d2da0eba0c1c8c8b5e0853f29",
 "build_viewer.py":"54a62a89cc535ae5473f8188a0b32d747735a08f",
}
POS=re.compile(r"pozycja X (-?\d+\.\d+) / Z (-?\d+\.\d+)")
TIME=re.compile(r" · (\d+\.\d+) s ·")
def need(ok,reason):
 if not ok: raise AssertionError(reason)
def coordinate(s):
 m=POS.search(s)
 need(m is not None,"Position missing: "+s)
 return tuple(map(float,m.groups()))
def elapsed(s):
 m=TIME.search(s)
 need(m is not None,"Elapsed time missing: "+s)
 return float(m.group(1))
def git(*arg):
 return subprocess.check_output(["git",*arg],text=True).strip()

def main():
 ap=argparse.ArgumentParser()
 ap.add_argument("--html",required=True)
 ap.add_argument("--output",required=True)
 ap.add_argument("--screenshots",required=True)
 a=ap.parse_args()
 html=Path(a.html).resolve()
 out=Path(a.output).resolve()
 shots=Path(a.screenshots).resolve()
 out.parent.mkdir(parents=True,exist_ok=True)
 shots.mkdir(parents=True,exist_ok=True)
 report={"package":"B01-60-COURSE-DRIVE-BROWSER-QA-001","source_pr":365,
         "original_source_commit":UPSTREAM,"expected_html_sha256":EXPECTED_HTML,
         "unity":"NOT_RUN","physx":"NOT_RUN","human_playtest":"NOT_RUN",
         "game_integrated":False,"cases":[],"errors":[],"screenshots":[]}
 def case(name,fn):
  try:
   detail=fn()
   report["cases"].append({"name":name,"status":"PASS","detail":detail})
   print("PASS",name,detail,flush=True)
  except Exception as e:
   report["cases"].append({"name":name,"status":"FAIL","error":str(e)})
   report["errors"].append({"name":name,"trace":traceback.format_exc()})
   print("FAIL",name,str(e),flush=True)
 def provenance():
  git("cat-file","-e",UPSTREAM+"^{commit}")
  need(subprocess.run(["git","merge-base","--is-ancestor",UPSTREAM,"HEAD"]).returncode==0,
       "Producer commit is not ancestor")
  for file,sha in BLOBS.items():
   need(git("rev-parse","HEAD:"+BASE+file)==sha,"Changed original producer source: "+file)
  actual=hashlib.sha256(html.read_bytes()).hexdigest()
  report["actual_html_sha256"]=actual
  need(actual==EXPECTED_HTML,"Wrong original binary HTML sha256: "+actual)
  return {"original_pinned_blobs":len(BLOBS),"html_sha256":actual}
 case("Q00_exact_pinned_original_source_and_generated_HTML",provenance)
 if report["errors"]:
  out.write_text(json.dumps(report,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
  return 1
 with sync_playwright() as pw:
  browser=pw.chromium.launch(headless=True)
  context=browser.new_context(viewport={"width":1280,"height":720},device_scale_factor=1)
  page=context.new_page()
  errors=[]; network=[]
  page.on("pageerror",lambda e: errors.append(str(e)))
  page.on("request",lambda r: network.append(r.url) if r.url.startswith(("http:","https:")) else None)
  page.clock.install()
  page.goto(html.as_uri(),wait_until="load")
  page.clock.run_for(250)
  def data_test():
   maps=page.evaluate("JSON.parse(document.getElementById('maps-data').textContent)")
   need(len(maps)==18,"Map count not 18")
   need(len({m["geometryHash"] for m in maps})==18,"Not 18 unique original maps")
   need(len({m["variantId"] for m in maps})==3,"Not three course families")
   need(len({m["seed"] for m in maps})==6,"Not six seeds")
   need(page.locator("#course option").count()==18,"Incomplete map selection")
   need("CHECKPOINT" in page.locator("#status").inner_text(),"Rendering loop did not initialize")
   need(not errors,"Uncaught JavaScript on load: "+str(errors))
   return {"original_maps":18,"source_variants":3,"seeds":6}
  case("Q01_original_18_maps_and_live_canvas_UI",data_test)
  def all_selections():
   values=page.locator("#course option").evaluate_all("es=>es.map(e=>e.value)")
   for profile in ("Light","Heavy"):
    page.locator("#profile").select_option(profile)
    for v in values:
     page.locator("#course").select_option(v)
     page.clock.run_for(90)
     need("CHECKPOINT 0/" in page.locator("#status").inner_text(),
          "Cannot initialize "+profile+" "+v)
     need("Profil "+profile in page.locator("#caption").inner_text(),
          "Wrong profile selected "+profile+" "+v)
   return {"real_chromium_initializations":36}
  case("Q02_select_18_courses_x_light_and_heavy",all_selections)
  def keyboard():
   page.locator("#auto").uncheck()
   page.locator("#profile").select_option("Light")
   page.locator("#course").select_option(index=0)
   page.clock.run_for(100)
   start=coordinate(page.locator("#status").inner_text())
   page.keyboard.down("w")
   page.clock.run_for(1400)
   page.keyboard.up("w")
   state=page.locator("#status").inner_text()
   after=coordinate(state)
   need(after[1]>start[1]+0.5,"W failed to advance on actual map")
   need(elapsed(state)>1.0,"Frame clock did not progress")
   page.locator("#pause").click()
   need(page.locator("#pause").inner_text()=="Wznów","Pause label mismatch")
   paused=page.locator("#status").inner_text()
   page.clock.run_for(1200)
   still=page.locator("#status").inner_text()
   need(coordinate(paused)==coordinate(still),"Player moves while paused")
   need(elapsed(paused)==elapsed(still),"Time moves while paused")
   page.keyboard.press("Space")
   need(page.locator("#pause").inner_text()=="Pauza","Space resume failed")
   page.keyboard.press("r")
   page.clock.run_for(100)
   reset=page.locator("#status").inner_text()
   need("CHECKPOINT 0/" in reset,"R did not reset checkpoint")
   need(elapsed(reset)<0.5,"R did not reset timer")
   need(abs(coordinate(reset)[1]-start[1])<0.25,"R did not reset position")
   return {"start":start,"after_W":after,"pause":True,"space":True,"R":True}
  case("Q03_real_keyboard_W_pause_space_R",keyboard)
  def autopilot():
   page.locator("#course").select_option(index=0)
   page.locator("#auto").check()
   results={}
   for profile in ("Light","Heavy"):
    page.locator("#profile").select_option(profile)
    page.clock.run_for(30000)
    text=page.locator("#status").inner_text()
    need(text.startswith("META!"),"Autopilot did not finish "+profile+": "+text)
    collisions=int(re.search(r"kolizje (\d+)",text).group(1))
    need(collisions==0,"Unexpected original route collision "+profile)
    results[profile]={"simulated_seconds":elapsed(text),"collisions":collisions}
   page.locator("#auto").uncheck()
   return results
  case("Q04_rendered_light_and_heavy_checkpoint_autopilot",autopilot)
  def responsive():
   pics=[]
   for w,h in ((320,720),(768,1024),(1280,720),(1920,1080)):
    page.set_viewport_size({"width":w,"height":h})
    page.clock.run_for(120)
    size=page.evaluate("""() => ({doc:document.documentElement.scrollWidth,
      viewport:innerWidth, canvas:document.querySelector('#arena').getBoundingClientRect().width})""")
    need(size["doc"]<=size["viewport"]+1,"Horizontal overflow "+str((w,h,size)))
    need(100<size["canvas"]<=w,"Canvas overflow "+str((w,h,size)))
    path=shots/("course-drive-%sx%s.png"%(w,h))
    page.screenshot(path=str(path),full_page=True)
    raw=path.read_bytes()
    need(raw.startswith(b"\x89PNG\r\n\x1a\n") and len(raw)>10000,
         "Empty or invalid screenshot "+str(path))
    pics.append({"viewport":"%sx%s"%(w,h),"filename":path.name,"bytes":len(raw),
                 "sha256":hashlib.sha256(raw).hexdigest()})
   report["screenshots"]=pics
   return {"real_chromium_png_screenshots":len(pics),"horizontal_overflow":0}
  case("Q05_responsive_real_png_320_768_1280_1920",responsive)
  def offline():
   need(not errors,"Browser JS errors: "+str(errors))
   need(not network,"Offline HTML made external HTTP requests: "+str(network))
   return {"javascript_exceptions":0,"external_http_requests":0}
  case("Q06_no_uncaught_browser_JS_or_external_network",offline)
  context.close()
  browser.close()
 report["summary"]={"pass":sum(x["status"]=="PASS" for x in report["cases"]),
                    "fail":sum(x["status"]=="FAIL" for x in report["cases"]),
                    "native_unity":"NOT_RUN","browser":"EXECUTED"}
 out.write_text(json.dumps(report,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
 print("QA_RECEIPT="+str(out),flush=True)
 return 1 if report["summary"]["fail"] else 0

if __name__=="__main__":
 sys.exit(main())
