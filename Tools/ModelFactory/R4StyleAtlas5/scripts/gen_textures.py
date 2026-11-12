"""#1239 surface-finish detail maps (256 px, 32 levels, tiling, generated). They are MULTIPLIED over the paint colour of a zone, so every zone stays
recolourable. A: hairline tech grid, B: brushed / cast steel, D: grime, rust blooms and scratches, E: painted three-band light ramp (mapped by
height). C has no texture by design. usage: python3 gen_textures.py"""
import os, numpy as np
from PIL import Image
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "textures"); N = 256
def vnoise(n, cells, seed):
    r = np.random.RandomState(seed); g = r.rand(cells, cells); x = np.linspace(0, cells, n, endpoint=False); i = x.astype(int); f = x - i; f = f*f*(3 - 2*f)
    a = g[np.ix_(i % cells, i % cells)]; b = g[np.ix_(i % cells, (i + 1) % cells)]; c = g[np.ix_((i + 1) % cells, i % cells)]; d = g[np.ix_((i + 1) % cells, (i + 1) % cells)]
    return (a*(1 - f[None, :]) + b*f[None, :])*(1 - f[:, None]) + (c*(1 - f[None, :]) + d*f[None, :])*f[:, None]
def fbm(seed, octs=(4, 8, 16, 32, 64)): return sum(vnoise(N, c, seed + k)/2**k for k, c in enumerate(octs))/sum(1/2**k for k in range(len(octs)))
def save(name, rgb): os.makedirs(OUT, exist_ok=True); Image.fromarray((np.round(np.clip(rgb, 0, 1)*31)/31*255).astype(np.uint8)).save(os.path.join(OUT, name), optimize=True); print(name, rgb.shape, round(float(rgb.mean()), 3))
def main():
    y, x = np.mgrid[0:N, 0:N]/N
    # A: clean lacquer, hairline grid every 1/8 tile + faint micro hex-like dots
    a = np.ones((N, N)); a[(np.abs((x*8) % 1 - 0.5) > 0.49) | (np.abs((y*8) % 1 - 0.5) > 0.49)] = 0.90; a -= 0.03*((np.sin(x*2*np.pi*64)*np.sin(y*2*np.pi*64)) > 0.6)
    save("A_detail.png", np.stack([a, a, a*1.0], -1))
    # B: brushed steel streaks along u + cast speckle + darker oil pooling
    r = np.random.RandomState(7); streak = np.repeat(r.rand(N, 1), N, 1); streak = 0.5*streak + 0.5*np.roll(streak, 3, 0); b = 0.80 + 0.16*streak + 0.05*(r.rand(N, N) - 0.5) - 0.18*np.clip(fbm(11) - 0.55, 0, 1)*2
    save("B_detail.png", np.stack([b, b, b*1.02], -1))
    # D: grime + rust blooms (warm tint) + scratches
    g = 0.42 + 0.55*fbm(21); rust = np.clip((fbm(33, (3, 6, 12, 24, 48)) - 0.52)*5, 0, 1); r = np.random.RandomState(5); scr = np.ones((N, N))
    for _ in range(60):
        x0, y0 = r.randint(0, N, 2); L = r.randint(10, 60); ang = r.uniform(-0.5, 0.5) + (np.pi/2 if r.rand() < 0.3 else 0); t = np.arange(L)
        scr[(y0 + (t*np.sin(ang)).astype(int)) % N, (x0 + (t*np.cos(ang)).astype(int)) % N] = r.uniform(1.15, 1.45)
    g = g*scr; d = np.stack([g*(1 - 0.25*rust) + 0.38*rust, g*(1 - 0.55*rust) + 0.14*rust, g*(1 - 0.75*rust) + 0.04*rust], -1)
    save("D_detail.png", d)
    # E: painted light ramp, v = height (0 bottom .. 1 top): three bands with wobbly brush edges and a top highlight
    wob = 0.035*np.sin(x*2*np.pi*3 + 1.0) + 0.02*np.sin(x*2*np.pi*7); h = 1 - y + wob; e = np.where(h > 0.72, 1.0, np.where(h > 0.34, 0.86, 0.68)); e = np.where(h > 0.93, 1.0, e) + 0.02*(fbm(41) - 0.5)
    save("E_ramp.png", np.stack([e, e*0.98, e*1.0], -1))
if __name__ == "__main__": main()
