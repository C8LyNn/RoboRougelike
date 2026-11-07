import sys, os, zipfile, bpy, hashlib
a = sys.argv[sys.argv.index("--") + 1:]; ROOT, ZIP, SUBS = a[0], a[1], a[2].split(",")
sc = bpy.context.scene; sc.render.image_settings.file_format = 'JPEG'; sc.render.image_settings.quality = 90
tmp = ZIP + "_jpg"; os.makedirs(tmp, exist_ok=True)
with zipfile.ZipFile(ZIP, "w", zipfile.ZIP_DEFLATED) as z:
    for sub in SUBS:
        base = os.path.join(ROOT, sub)
        for dp, dn, fs in os.walk(base):
            if "__pycache__" in dp: continue
            for f in sorted(fs):
                p = os.path.join(dp, f); rel = os.path.relpath(p, ROOT)
                if f.endswith((".blend1", ".pyc", ".log")): continue
                if f.lower().endswith(".png"):
                    im = bpy.data.images.load(p, check_existing=False)
                    if im.size[0] > 1600: im.scale(1600, int(im.size[1]*1600/im.size[0]))
                    q = os.path.join(tmp, rel[:-4].replace(os.sep, "__") + ".jpg"); im.save_render(q, scene=sc); bpy.data.images.remove(im)
                    z.write(q, rel[:-4] + ".jpg")
                else: z.write(p, rel)
    for f in ("compat_assembly.json",):
        if "compat" in SUBS and os.path.exists(os.path.join(ROOT, f)): z.write(os.path.join(ROOT, f), f)
print("PACK_DONE")
