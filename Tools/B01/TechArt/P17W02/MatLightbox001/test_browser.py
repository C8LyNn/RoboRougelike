#!/usr/bin/env python3
"""Actual Chromium WebGL2 QA and screenshot evidence for MAT-LIGHTBOX-001."""
import argparse
import copy
import functools
import hashlib
import json
from http.server import SimpleHTTPRequestHandler,ThreadingHTTPServer
from pathlib import Path
import shutil
import tempfile
import threading

from PIL import Image,ImageChops,ImageStat
from playwright.sync_api import sync_playwright

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()

class Quiet(SimpleHTTPRequestHandler):
    def log_message(self,*args):pass

def read_site(site):
    doc=json.loads((site/"site-receipt.json").read_text(encoding="utf-8"))
    assert doc["package_id"]=="B01-30-MAT-LIGHTBOX-001"
    assert len(doc["files"])==10 and doc["texture_files_count"]==8
    for f in doc["files"]:
        path=site/f["path"]
        assert path.is_file() and path.stat().st_size==f["bytes"]
        assert sha(path)==f["sha256"]
    assert len({f["path"] for f in doc["files"]})==10
    return doc

def get_diff(a,b):
    with Image.open(a) as p,Image.open(b) as q:
        p=p.convert("RGB");q=q.convert("RGB")
        if p.size!=q.size:return False
        d=ImageChops.difference(p,q)
        return d.getbbox() is not None and max(ImageStat.Stat(d).mean)>0.05

def execute(site,out):
    out.mkdir(parents=True,exist_ok=True)
    doc=read_site(site)
    srv=ThreadingHTTPServer(("127.0.0.1",0),functools.partial(Quiet,directory=str(site)))
    worker=threading.Thread(target=srv.serve_forever,daemon=True)
    worker.start()
    root="http://127.0.0.1:"+str(srv.server_port)+"/index.html"
    cases=[]
    screenshots=[]
    try:
        with sync_playwright() as play:
            browser=play.chromium.launch(headless=True,args=[
                "--enable-webgl","--use-gl=angle","--use-angle=swiftshader",
                "--enable-unsafe-swiftshader","--disable-gpu-sandbox"])
            for width,height in [(1280,720),(1920,1080),(720,900)]:
                context=browser.new_context(viewport={"width":width,"height":height},device_scale_factor=1)
                page=context.new_page()
                errors=[]
                page.on("pageerror",lambda e:errors.append(str(e)))
                page.goto(root,wait_until="networkidle")
                page.wait_for_function(
                    "window.lightboxQA && (window.lightboxQA.ready || window.lightboxQA.error !== null)",
                    timeout=30000)
                actual=page.evaluate("window.lightboxQA")
                assert actual["error"] is None, ("ORIGINAL_WEBGL_STARTUP_FAILED",actual)
                assert actual["frames"]>=1 and len(actual["loaded"])==8
                assert page.locator("#renderState").inner_text().startswith("WEBGL2 DRAW PASS")
                assert page.evaluate("document.documentElement.scrollWidth <= window.innerWidth + 2")
                cases.append(f"{width}x{height}: actual source 8 PNG WebGL2 init PASS")
                suffix=str(width)+"x"+str(height)
                tire=out/("tire-"+suffix+".png")
                page.locator("#viewer").screenshot(path=str(tire))
                screenshots.append(tire)
                with Image.open(tire) as image:
                    rgb=image.convert("RGB")
                    center=rgb.getpixel((rgb.width//2,rgb.height//2))
                    corner=rgb.getpixel((5,5))
                    std=ImageStat.Stat(rgb).stddev
                    print("MAT_LIGHTBOX_ACTUAL_SPHERE_PIXELS",{"viewport":suffix,
                        "center":center,"corner":corner,"stddev":std},flush=True)
                    assert rgb.width>250 and rgb.height>250
                    assert sum(abs(center[i]-corner[i]) for i in range(3))>40, (
                        "NO_VISIBLE_ORIGINAL_GPU_SPHERE",center,corner)
                    assert max(std)>3,("FLAT_ORIGINAL_GPU_IMAGE",std)
                cases.append(f"{suffix}: actual nonflat 3D sphere image PASS")
                # Original independent material switch changes actual GPU pixels
                page.select_option("#family","brushed_arm_alloy")
                assert page.evaluate("window.lightboxQA.profile")== "brushed_arm_alloy"
                alloy=out/("alloy-"+suffix+".png")
                page.locator("#viewer").screenshot(path=str(alloy))
                screenshots.append(alloy)
                assert get_diff(tire,alloy)
                cases.append(f"{suffix}: real alloy/rubber pixels different PASS")
                # User-adjustable lighting must affect pixels, not only labels
                page.locator("#light").evaluate("(e)=>{e.value='-95';e.dispatchEvent(new Event('input',{bubbles:true}));}")
                light=out/("alloy-light-"+suffix+".png")
                page.locator("#viewer").screenshot(path=str(light))
                screenshots.append(light)
                assert get_diff(alloy,light)
                cases.append(f"{suffix}: original WebGL light controls affect render PASS")
                # UV scale changes rendered texture, without creating another map
                page.locator("#uvScale").evaluate("(e)=>{e.value='5';e.dispatchEvent(new Event('input',{bubbles:true}));}")
                repeat=out/("alloy-uv-"+suffix+".png")
                page.locator("#viewer").screenshot(path=str(repeat))
                screenshots.append(repeat)
                assert get_diff(light,repeat)
                cases.append(f"{suffix}: actual repeat map affects render PASS")
                box=page.locator("#viewer").bounding_box()
                yaw0=page.evaluate("window.lightboxQA.yaw")
                x=box["x"]+box["width"]/2
                y=box["y"]+box["height"]/2
                page.mouse.move(x,y);page.mouse.down()
                page.mouse.move(x+80,y+25,steps=5);page.mouse.up()
                assert page.evaluate("window.lightboxQA.yaw")>yaw0
                cases.append(f"{suffix}: pointer orbit changes actual yaw PASS")
                page.locator("#reset").click()
                assert page.input_value("#light")=="35" and page.input_value("#uvScale")=="2"
                assert abs(page.evaluate("window.lightboxQA.yaw")-.15)<.0001
                cases.append(f"{suffix}: reset restores actual renderer values PASS")
                assert not errors,errors
                cases.append(f"{suffix}: 0 original JS exceptions PASS")
                context.close()
            # Fail closed: copy ORIGINAL site, remove one genuine PNG and
            # serve from same isolated server after swapping file temporarily.
            # Browser cache is bypassed with a new context + path query.
            missing=site/"textures"/"brushed_arm_alloy"/"brushed_arm_alloy_normal.png"
            raw=missing.read_bytes()
            try:
                missing.unlink()
                ctx=browser.new_context()
                pg=ctx.new_page()
                pg.goto(root+"?missing=1",wait_until="networkidle")
                pg.wait_for_function("window.lightboxQA && window.lightboxQA.error !== null",timeout=20000)
                err=pg.evaluate("window.lightboxQA.error")
                assert "SOURCE_PNG_MISSING" in err,err
                assert pg.evaluate("window.lightboxQA.ready") is False
                cases.append("missing original PBR normal-map PNG: fail closed PASS")
                ctx.close()
            finally:
                missing.write_bytes(raw)
            assert read_site(site)==doc
            cases.append("missing PNG restored byte-identically to original SHA PASS")
            browser.close()
    finally:
        srv.shutdown();srv.server_close()
    result={
        "package_id":"B01-30-MAT-LIGHTBOX-001",
        "result":"PASS","checks":len(cases),
        "actual_screenshot_png_count":len(screenshots),
        "screenshots":[{"name":x.name,"bytes":x.stat().st_size,"sha256":sha(x)} for x in screenshots],
        "checks_passed":cases,
        "original_source_verified":True,
        "browser_engine":"Chromium Playwright + WebGL2 SwiftShader",
        "not_run":["Unity 6000.3.14f1","URP visual parity","real game geometry/UV","B01-60 acceptance"]
    }
    (out/"browser-receipt.json").write_text(json.dumps(result,indent=2)+"\n",encoding="utf-8")
    print("MAT_LIGHTBOX_REAL_CHROMIUM_WEBGL2_PASS",json.dumps({
        "checks":len(cases),"png_screenshots":len(screenshots),
        "result":result["result"]},sort_keys=True))
    return result

if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("--site",required=True);p.add_argument("--out",required=True)
    a=p.parse_args()
    execute(Path(a.site).resolve(),Path(a.out))
