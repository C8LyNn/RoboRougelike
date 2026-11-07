# packs one finished weapon folder for the repo: everything except PNG renders, plus renders as JPG (max 1100 px, q90)
import sys, os, zipfile, bpy, hashlib
a = sys.argv[sys.argv.index("--") + 1:]
SRC, ZIP = a[0], a[1]
sc = bpy.context.scene; sc.render.image_settings.file_format = 'JPEG'; sc.render.image_settings.quality = 90
tmp = ZIP + "_jpg"; os.makedirs(tmp, exist_ok=True)
with zipfile.ZipFile(ZIP, "w", zipfile.ZIP_DEFLATED) as z:
    for dp, dn, fs in os.walk(SRC):
        for f in sorted(fs):
            p = os.path.join(dp, f); rel = os.path.relpath(p, SRC)
            if f.endswith((".blend1", ".pyc")) or f == "blender_stdout.log": continue
            if f.lower().endswith(".png") and "renders" in rel and "silhouette" not in rel:
                im = bpy.data.images.load(p, check_existing=False)
                if im.size[0] > 1100: im.scale(1100, int(im.size[1]*1100/im.size[0]))
                q = os.path.join(tmp, rel[:-4].replace(os.sep, "__") + ".jpg"); im.save_render(q, scene=sc); bpy.data.images.remove(im)
                z.write(q, rel[:-4] + ".jpg")
            else:
                z.write(p, rel)
print("PACK_DONE", hashlib.sha256(open(ZIP, "rb").read()).hexdigest())
