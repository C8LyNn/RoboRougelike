"""Silhouette similarity of the 8 weapons per version from rest-pose GLB silhouettes (scripts/_sil.py output).
usage: python3 silhouette_compare_938.py <R4Weapons938 dir>   reads compare/sil/<ver>/, writes compare/silhouette_check.json + boards"""
import itertools, json, os, sys
import numpy as np
from PIL import Image, ImageDraw

ROOT = sys.argv[1]
N = ["R4W_Basic", "R4W_Heavy", "R4W_Shotgun", "R4W_Railgun", "R4W_BeamCutter", "R4W_RocketLauncher", "R4W_RotarySaw", "R4W_EmpEmitter"]
V = ["gameplay", "side", "front", "top"]
VERS = [("v1", "v1"), ("v2", "v2"), ("v3_port", "v3_port")]


def mask(p, s=64):
    a = np.asarray(Image.open(p).convert("L")) < 128
    ys, xs = np.where(a); a = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    h, w = a.shape; m = max(h, w); sq = np.zeros((m, m), bool); sq[(m - h)//2:(m - h)//2 + h, (m - w)//2:(m - w)//2 + w] = a
    return np.asarray(Image.fromarray(sq.astype(np.uint8)*255).resize((s, s), Image.BILINEAR)) > 127


def main():
    out = {"method": "black silhouettes of the rest-pose GLB, 4 orthographic cameras (gameplay 3/4 high, side, front, top), cropped to the silhouette bbox, squared, 64x64, IoU per view, mean of 4; pair flagged when mean IoU > 0.85",
           "threshold": 0.85, "versions": {"v1": "first commit of PR #939", "v2": "silhouette diversity pass", "v3_port": "v2 + standard R4 Robocraft-style square port on every base (current)"}}
    for key, sub in VERS:
        d = os.path.join(ROOT, "compare", "sil", sub)
        if not os.path.isdir(d): continue
        Mk = {n: {v: mask(os.path.join(d, f"{n}_sil_{v}.png")) for v in V} for n in N}
        pairs = {}
        for a, b in itertools.combinations(N, 2):
            iou = {v: float((Mk[a][v] & Mk[b][v]).sum()/max(1, (Mk[a][v] | Mk[b][v]).sum())) for v in V}
            iou["mean"] = sum(iou[v] for v in V)/4
            pairs[a + "|" + b] = {k: round(x, 3) for k, x in iou.items()}
        top = sorted(pairs.items(), key=lambda kv: -kv[1]["mean"])
        out[key] = {"pairs": pairs, "flagged": [k for k, p in top if p["mean"] > 0.85], "most_similar": top[:8],
                    "mean_of_all_pairs": round(float(np.mean([p["mean"] for p in pairs.values()])), 3)}
        print(key, out[key]["mean_of_all_pairs"], out[key]["flagged"], top[0][0], top[0][1]["mean"])
    out["current"] = "v3_port" if "v3_port" in out else "v2"
    json.dump(out, open(os.path.join(ROOT, "compare", "silhouette_check.json"), "w"), indent=1)
    # board: v1 vs current
    cur = out["current"]; W = 128; pad = 150

    def sil(sub, n, v):
        im = Image.open(os.path.join(ROOT, "compare", "sil", sub, f"{n}_sil_{v}.png")).convert("L")
        im = im.crop(im.point(lambda p: 255 if p < 128 else 0).getbbox())
        s = max(im.size) + 12; sq = Image.new("L", (s, s), 255); sq.paste(im, ((s - im.width)//2, (s - im.height)//2))
        return sq.resize((W, W), Image.LANCZOS).convert("RGB")
    board = Image.new("RGB", (pad + W*8 + 24, 40 + W*8), (24, 26, 30)); d = ImageDraw.Draw(board)
    d.text((pad, 8), "v1 (PR #939 first commit)", fill=(200, 200, 200)); d.text((pad + W*4 + 24, 8), f"{cur} (current)", fill=(242, 194, 48))
    for i, n in enumerate(N):
        d.text((6, 40 + i*W + W//2 - 6), n.replace("R4W_", ""), fill=(230, 230, 230))
        for j, v in enumerate(V):
            board.paste(sil("v1", n, v), (pad + j*W, 40 + i*W)); board.paste(sil(cur, n, v), (pad + W*4 + 24 + j*W, 40 + i*W))
    board.save(os.path.join(ROOT, "compare", "silhouettes_v1_v2_128.png"), optimize=True)


if __name__ == "__main__":
    main()
