"""Silhouette similarity check of the 11 movement models from their Blender silhouette renders.
usage: python3 silhouette_compare.py <R4Movement940 dir>  -> compare/silhouette_check.json, silhouettes_128.png, thumbnails_128.png"""
import itertools, json, os, sys
import numpy as np
from PIL import Image, ImageDraw

ROOT = sys.argv[1]
N = ["Wheel", "CrawlerTrack", "HoverEngine", "OmniDrive", "Suspension", "Booster", "JumpDrive", "Blink", "Gyroscope", "MagneticGrapple", "IndependentSteering"]
V = ["gameplay", "side", "front", "top"]


def load(n, v):
    im = Image.open(os.path.join(ROOT, f"R4M_{n}", "renders", "silhouette", f"R4M_{n}_sil_{v}.png"))
    if im.mode in ("RGBA", "LA"):
        bg = Image.new("RGBA", im.size, (255, 255, 255, 255)); bg.alpha_composite(im.convert("RGBA")); im = bg
    return np.asarray(im.convert("L")) < 128


def norm(a, s=64):
    ys, xs = np.where(a); a = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    h, w = a.shape; m = max(h, w); sq = np.zeros((m, m), bool); sq[(m - h)//2:(m - h)//2 + h, (m - w)//2:(m - w)//2 + w] = a
    return np.asarray(Image.fromarray(sq.astype(np.uint8)*255).resize((s, s), Image.BILINEAR)) > 127


def main():
    Mk = {n: {v: norm(load(n, v)) for v in V} for n in N}
    pairs = {}
    for a, b in itertools.combinations(N, 2):
        iou = {v: round(float((Mk[a][v] & Mk[b][v]).sum()/max(1, (Mk[a][v] | Mk[b][v]).sum())), 3) for v in V}
        iou["mean"] = round(sum(iou[v] for v in V)/4, 3)
        pairs[f"R4M_{a}|R4M_{b}"] = iou
    top = sorted(pairs.items(), key=lambda kv: -kv[1]["mean"])
    out = {"method": "black silhouettes from 4 cameras (gameplay 3/4, side, front, top), cropped to bbox, 64 px, IoU per view; pair flagged if mean IoU > 0.85",
           "most_similar": top[:8], "flagged": [k for k, v in top if v["mean"] > 0.85], "pairs": pairs}
    os.makedirs(os.path.join(ROOT, "compare"), exist_ok=True)
    json.dump(out, open(os.path.join(ROOT, "compare", "silhouette_check.json"), "w"), indent=1)
    W = 128; board = Image.new("RGB", (W*4 + 150, W*len(N)), (24, 26, 30)); d = ImageDraw.Draw(board)
    th = Image.new("RGB", (W*len(N), W), (24, 26, 30))
    for i, n in enumerate(N):
        d.text((6, i*W + W//2 - 6), n, fill=(230, 230, 230))
        for j, v in enumerate(V):
            board.paste(Image.fromarray((~norm(load(n, v), 120)).astype(np.uint8)*255).convert("RGB").resize((W - 8, W - 8)), (150 + j*W + 4, i*W + 4))
        t = Image.open(os.path.join(ROOT, f"R4M_{n}", "renders", f"R4M_{n}_34front.jpg")).convert("RGB")
        t = t.crop(((t.width - t.height)//2, 0, (t.width + t.height)//2, t.height)).resize((W, W), Image.LANCZOS); th.paste(t, (i*W, 0))
    board.save(os.path.join(ROOT, "compare", "silhouettes_128.png"), optimize=True)
    th.save(os.path.join(ROOT, "compare", "thumbnails_128.png"), optimize=True)
    print("flagged", out["flagged"], "top", top[:3])


if __name__ == "__main__":
    main()
