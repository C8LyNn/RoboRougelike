"""Headless three.js (r128, SwiftShader) renderer for REAL GLB files. Used for all #1232 preview renders."""
import os, json, subprocess, time, atexit, io, socket
from playwright.sync_api import sync_playwright
from PIL import Image

class Renderer:
    def __init__(self, root, port=None):
        if port is None:
            s = socket.socket(); s.bind(("127.0.0.1", 0)); port = s.getsockname()[1]; s.close()
        self.port = port
        self.srv = subprocess.Popen(["python3", "-m", "http.server", str(port), "--bind", "127.0.0.1"], cwd=root, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        atexit.register(self.close); time.sleep(0.8)
        self.pw = sync_playwright().start()
        self.br = self.pw.chromium.launch(executable_path="/usr/bin/google-chrome", args=["--no-sandbox", "--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"])
        self.pg = self.br.new_page(viewport={"width": 1600, "height": 1000})
        self.pg.on("console", lambda m: print("[console]", m.type, m.text[:300]) if m.type in ("error","warning") and "404" not in m.text else None)
        self.pg.goto(f"http://127.0.0.1:{port}/web/render.html"); self.pg.wait_for_function("window.ready===true")
    def render(self, scene, path, ss=2, quality=92):
        sc = dict(scene); W, H = sc.get("width", 1280), sc.get("height", 720); sc["width"], sc["height"] = W*ss, H*ss
        self.pg.set_viewport_size({"width": W*ss, "height": H*ss})
        info = self.pg.evaluate("sc => window.renderScene(sc)", sc)
        import base64
        png = base64.b64decode(self.pg.evaluate("() => document.querySelector('canvas').toDataURL('image/png').split(',')[1]"))
        im = Image.open(io.BytesIO(png)).convert("RGB")
        if ss != 1: im = im.resize((W, H), Image.LANCZOS)
        os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
        im.save(path, quality=quality) if path.lower().endswith(".jpg") else im.save(path)
        return info
    def close(self):
        try: self.br.close(); self.pw.stop()
        except Exception: pass
        try: self.srv.terminate()
        except Exception: pass
