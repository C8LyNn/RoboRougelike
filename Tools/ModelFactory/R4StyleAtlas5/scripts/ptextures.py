"""#1239 pilot 2: generated, tiling texture SETS per style (one tile = one 0.52 m block face, tri-planar):
  <S>_base.png   base colour detail, MULTIPLIED over the paint colour of the zone (zones stay recolourable)
  <S>_orm.png    glTF ORM: R occlusion (1), G roughness, B metallic - multiplied by the zone factors
  <S>_normal.png tangent-space normal map (OpenGL / glTF convention), isotropic detail only (tri-planar safe)
A: lacquer with hairline panel grid and micro dimples; B: cast / brushed steel, oil, scratches; D: chipped paint, rust blooms, bare-metal
scratches, dents; E: painted light ramp (mapped by height) with brush strokes - no ORM / normal by design; C: no textures by design.
usage: python3 ptextures.py"""
import os, numpy as np
from PIL import Image
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "pilot2", "textures")
def vnoise(n, cells, seed):
    r = np.random.RandomState(seed); g = r.rand(cells, cells); x = np.linspace(0, cells, n, endpoint=False); i = x.astype(int); f = x - i; f = f*f*(3 - 2*f)
    a = g[np.ix_(i % cells, i % cells)]; b = g[np.ix_(i % cells, (i + 1) % cells)]; c = g[np.ix_((i + 1) % cells, i % cells)]; d = g[np.ix_((i + 1) % cells, (i + 1) % cells)]
    return (a*(1 - f[None, :]) + b*f[None, :])*(1 - f[:, None]) + (c*(1 - f[None, :]) + d*f[None, :])*f[:, None]
def fbm(n, seed, octs=(4, 8, 16, 32, 64)): return sum(vnoise(n, c, seed + k)/2**k for k, c in enumerate(octs))/sum(1/2**k for k in range(len(octs)))
def sstep(a, b, x): t = np.clip((x - a)/(b - a), 0, 1); return t*t*(3 - 2*t)
def blur(a, k=1):
    for _ in range(k): a = (a + np.roll(a, 1, 0) + np.roll(a, -1, 0) + np.roll(a, 1, 1) + np.roll(a, -1, 1))/5
    return a
def lines(n, count, seed, lmin, lmax, horiz=0.7):
    r = np.random.RandomState(seed); m = np.zeros((n, n))
    for _ in range(count):
        x0, y0 = r.randint(0, n, 2); L = r.randint(lmin, lmax); ang = r.uniform(-0.35, 0.35) + (0 if r.rand() < horiz else np.pi/2); t = np.arange(L); w = r.uniform(0.5, 1.0)
        m[(y0 + (t*np.sin(ang)).astype(int)) % n, (x0 + (t*np.cos(ang)).astype(int)) % n] = np.maximum(m[(y0 + (t*np.sin(ang)).astype(int)) % n, (x0 + (t*np.cos(ang)).astype(int)) % n], w*np.sin(np.pi*t/L)**0.5)
    return m
def normal_from_height(h, k):
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1))/2; gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0))/2; nx, ny, nz = -k*gx, k*gy, np.ones_like(h); l = np.sqrt(nx*nx + ny*ny + nz*nz)
    return np.stack([nx/l*0.5 + 0.5, ny/l*0.5 + 0.5, nz/l*0.5 + 0.5], -1)
def save(name, rgb, bits=5):
    q = 2**bits - 1; os.makedirs(OUT, exist_ok=True); Image.fromarray((np.round(np.clip(rgb, 0, 1)*q)/q*255).astype(np.uint8)).save(os.path.join(OUT, name), optimize=True)
    print(f"{name:14s} {rgb.shape[0]} px  mean {np.round(rgb.reshape(-1, 3).mean(0), 3)}  {os.path.getsize(os.path.join(OUT, name))//1024} KB")
def one(v, shape): return np.full(shape, v)
def main():
    # ---------------- A  Sleek: lacquer, hairline panel grid every quarter tile, micro dimple field, clear-coat variation
    n = 256; y, x = np.mgrid[0:n, 0:n]/n
    grid = ((np.abs((x*4 + 0.5) % 1 - 0.5) < 0.012) | (np.abs((y*4 + 0.5) % 1 - 0.5) < 0.012)).astype(float); dim = (np.sin(x*2*np.pi*32)*np.sin(y*2*np.pi*32)); dimple = sstep(0.55, 0.9, dim)
    tone = 0.97 + 0.03*fbm(n, 3, (2, 4, 8)); base = tone*(1 - 0.22*grid) - 0.025*dimple
    save("A_base.png", np.stack([base, base, np.minimum(1, base*1.01)], -1), 8)
    rough = 0.72 + 0.16*fbm(n, 5, (4, 8, 16, 32)) + 0.28*grid + 0.10*dimple
    save("A_orm.png", np.stack([one(1.0, rough.shape), rough, one(1.0, rough.shape)], -1), 6)
    save("A_normal.png", normal_from_height(blur(-0.9*grid - 0.25*dimple, 1), 2.2), 6)
    # ---------------- B  Brutal: cast and brushed steel - streaks, pits, oil pooling, bright scratches
    n = 512; r = np.random.RandomState(7); streak = np.repeat(r.rand(n, 1), n, 1); streak = np.clip((streak + np.roll(streak, 2, 0) + np.roll(streak, 5, 0))/3 + 0.35*(fbm(n, 9, (8, 16)) - 0.5), 0, 1)
    pits = sstep(0.715, 0.78, blur(r.rand(n, n), 1)); oil = sstep(0.56, 0.74, fbm(n, 11, (3, 6, 12, 24))); scr = blur(lines(n, 150, 13, 30, 200, 0.6), 0)
    base = (0.66 + 0.30*streak)*(1 - 0.40*pits)*(1 - 0.45*oil) + 0.25*scr
    save("B_base.png", np.stack([base, base, base*1.02], -1))
    rough = np.clip(0.80 + 0.14*(streak - 0.5) + 0.20*pits - 0.34*oil - 0.40*scr, 0.25, 1); metal = np.clip(1.0 - 0.18*oil - 0.25*pits, 0, 1)
    save("B_orm.png", np.stack([one(1.0, rough.shape), rough, metal], -1), 6)
    save("B_normal.png", normal_from_height(blur(-1.0*pits - 0.5*scr + 0.6*(fbm(n, 15, (16, 32, 64)) - 0.5) + 0.15*streak, 1), 3.0), 6)
    # ---------------- D  Scrap: chipped paint over steel, rust blooms with bare-metal rims, scratches, grime, dents
    f1 = fbm(n, 33, (5, 10, 20, 40, 80)); rust = sstep(0.555, 0.615, f1); rim = sstep(0.535, 0.555, f1)*(1 - rust)*0.7; scr = lines(n, 220, 5, 20, 160, 0.5)*(1 - rust); grime = fbm(n, 21, (4, 8, 16, 32, 64))
    r = np.random.RandomState(19); drip = np.repeat(sstep(0.82, 0.97, r.rand(1, n)), n, 0)*sstep(0.35, 0.75, fbm(n, 27, (2, 4, 8))); drip = blur(drip, 1)*(1 - rust)
    speck = blur(r.rand(n, n), 1); paint = (0.42 + 0.34*grime)*(1 - 0.35*drip); bare = np.clip(rim + scr, 0, 1); rv = 0.60 + 0.55*speck
    R = paint*(1 - rust) + rust*0.80*rv; Gc = paint*(1 - rust) + rust*0.42*rv; B = paint*(1 - rust) + rust*0.20*rv
    R = R*(1 - bare) + bare*0.95; Gc = Gc*(1 - bare) + bare*0.93; B = B*(1 - bare) + bare*0.90; R = R + 0.25*drip*0.6; Gc = Gc + 0.25*drip*0.25
    save("D_base.png", np.stack([R, Gc, B], -1))
    rough = np.clip(0.78 + 0.16*grime + 0.25*rust - 0.45*bare, 0.3, 1); metal = np.clip(0.30*(1 - rust) + 0.75*bare, 0, 1)
    save("D_orm.png", np.stack([one(1.0, rough.shape), rough, metal], -1), 6)
    dents = fbm(n, 41, (2, 3, 5)); h = 1.6*(dents - 0.5) + 0.55*rust*(speck - 0.5)*2 - 0.35*bare + 0.2*(grime - 0.5)
    save("D_normal.png", normal_from_height(blur(h, 1), 5.0), 6)
    # ---------------- E  Toon: painted light ramp (v = height in the envelope): cool shadow band, mid tone, light top, soft highlight, brush strokes
    n = 256; y, x = np.mgrid[0:n, 0:n]/n; wob = 0.04*np.sin(x*2*np.pi*3 + 1.0) + 0.022*np.sin(x*2*np.pi*7 + 0.4); hgt = 1 - y + wob
    band = np.where(hgt > 0.70, 1.0, np.where(hgt > 0.33, 0.84, 0.62)); band = np.where((hgt > 0.90) & (hgt < 0.955), 1.0, band); stroke = np.repeat(np.random.RandomState(4).rand(1, n), n, 0); stroke = 0.02*(blur(stroke + 0.3*fbm(n, 44, (8, 16)), 6) - 0.6)
    v = band + stroke; cool = (band < 0.7).astype(float)
    save("E_base.png", np.stack([v*(1 - 0.10*cool), v*(1 - 0.14*cool), np.minimum(1, v*(1 + 0.10*cool))], -1), 8)
if __name__ == "__main__": main()
