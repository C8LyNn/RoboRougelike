"""#1239 Hero Benchmark Pass 2: skins. Composes base colour / ORM / normal / emission of ONE model from its baked maps (numpy only, runs in Blender or
in the cloud). Wear is placed by the geometry of the model: convex edges (edge x ao), cavities (1 - ao), up-facing and low surfaces (wn, pos) and 3D
noise evaluated at the baked object-space position, so patterns do not repeat between models and do not break at UV seams."""
import numpy as np


def _hash(ix, iy, iz, seed):
    h = (ix*374761393 + iy*668265263 + iz*2147483647 + seed*1274126177) & 0xFFFFFFFF; h = ((h ^ (h >> 13))*1274126177) & 0xFFFFFFFF; return ((h ^ (h >> 16)) & 0xFFFF)/65535.0


def noise3(p, freq, seed=0):
    """value noise at 3D points p (..., 3), 0..1"""
    q = p*freq; i = np.floor(q).astype(np.int64); f = q - i; f = f*f*(3 - 2*f); out = 0
    for dx in (0, 1):
        for dy in (0, 1):
            for dz in (0, 1):
                w = (f[..., 0] if dx else 1 - f[..., 0])*(f[..., 1] if dy else 1 - f[..., 1])*(f[..., 2] if dz else 1 - f[..., 2])
                out = out + w*_hash(i[..., 0] + dx, i[..., 1] + dy, i[..., 2] + dz, seed)
    return out


def fbm(p, freq, octaves=4, seed=0, gain=0.5):
    out = 0; amp = 1.0; tot = 0.0
    for o in range(octaves): out = out + amp*noise3(p, freq*(2**o), seed + o*17); tot += amp; amp *= gain
    return out/tot


def ss(a, b, x): t = np.clip((x - a)/(b - a + 1e-9), 0, 1); return t*t*(3 - 2*t)
def mix(a, b, t): t = t[..., None] if (np.ndim(t) == 2 and np.ndim(a) != 2) else t; return a*(1 - t) + b*t
def to_srgb(x): x = np.clip(x, 0, 1); return np.where(x <= 0.0031308, x*12.92, 1.055*np.power(x, 1/2.4) - 0.055)


def zone_masks(idmap, zones, zcol):
    C = np.array([zcol[z] for z in zones], np.float32); d = ((idmap[..., None, :] - C[None, None])**2).sum(-1); k = d.argmin(-1); return {z: (k == i) for i, z in enumerate(zones)}


def compose(maps, env, pal, fx, seed=0):
    """maps: dilated bake maps. pal = {zone: (rgb, metallic, roughness)}. fx = wear recipe of the style (all optional):
      edge_wear   (amount, rgb, metallic, roughness)   bare material on worn convex edges of painted zones
      cavity      (amount, rgb)                         dirt / shadow multiplied into cavities
      dust        (amount, rgb)                         on up-facing surfaces
      rust        (amount, rgb_dark, rgb_light)         in cavities, on edges and low on the part, broken up by noise
      grime       (amount, rgb)                         streaks running down vertical faces
      scratches   amount                                fine roughness variation on metal and paint
      zones_paint list of zones that carry paint (wear applies to them)"""
    from hf import ZONES, ZCOL
    Z = zone_masks(maps["id"], ZONES, ZCOL); H, W = maps["ao"].shape[:2]; ao = maps["ao"][..., 0]; edge = np.clip(maps["edge"][..., 0], 0, 1); wn = maps["wn"]*2 - 1
    lo, hi = np.array(env[0], np.float32), np.array(env[1], np.float32); P = maps["pos"]*(hi - lo) + lo
    base = np.zeros((H, W, 3), np.float32); metal = np.zeros((H, W), np.float32); rough = np.full((H, W), 0.5, np.float32); emis = np.zeros((H, W, 3), np.float32)
    for z, m in Z.items():
        if z in pal: rgb, me, ro = pal[z]; base[m] = rgb; metal[m] = me; rough[m] = ro
    paint = np.zeros((H, W), bool)
    for z in fx.get("zones_paint", ["base", "sec"]): paint |= Z[z]
    convex = edge*ss(0.55, 0.9, ao); cavity = 1 - ss(0.25, 0.95, ao); up = np.clip(wn[..., 2], 0, 1); low = 1 - np.clip(maps["pos"][..., 2], 0, 1)
    n1 = fbm(P, 9.0, 4, seed + 1); n2 = fbm(P, 38.0, 3, seed + 2); n3 = fbm(P, 150.0, 2, seed + 3)
    base *= (0.94 + 0.12*n1)[..., None]                                                                   # slow tonal variation: never one flat colour
    rough = np.clip(rough + (n2 - 0.5)*0.10, 0.02, 1.0)
    if "scratches" in fx:
        s = fx["scratches"]; sc = ss(0.72, 0.9, fbm(P*np.array([1.0, 6.0, 1.0], np.float32), 60.0, 2, seed + 7)); rough = np.clip(rough + sc*s*0.25*np.where(metal > 0.5, 1.0, 0.6), 0.02, 1.0)
    if "cavity" in fx:
        a, rgb = fx["cavity"]; base = mix(base, base*np.array(rgb, np.float32), np.clip(cavity*a, 0, 1)); rough = np.clip(rough + cavity*a*0.15, 0, 1)
    if "grime" in fx:
        a, rgb = fx["grime"]; side = 1 - np.abs(wn[..., 2]); st = fbm(P*np.array([14.0, 14.0, 1.2], np.float32), 1.0, 3, seed + 11); g = np.clip(side*ss(0.45, 0.8, st)*a, 0, 1)*(~Z["glow"]); base = mix(base, np.array(rgb, np.float32), g*0.7); rough = np.clip(rough + g*0.2, 0, 1)
    if "dust" in fx:
        a, rgb = fx["dust"]; d = np.clip(up**2*a*(0.5 + 0.8*n1), 0, 1)*(~Z["glow"]); base = mix(base, np.array(rgb, np.float32), d*0.6); rough = np.clip(rough + d*0.3, 0, 1); metal = metal*(1 - d*0.7)
    if "stripes" in fx:                                                                                    # painted warning stripes on one zone, in object space (not a UV pattern)
        z, rgb, f = fx["stripes"]; st = (np.floor((P[..., 1] + P[..., 2]*0.9 + P[..., 0]*0.3)*f) % 2 == 0) & Z[z]; base[st] = np.array(rgb, np.float32)*(0.9 + 0.2*n1[st])[:, None]
    if "gradient" in fx:                                                                                   # hand-painted look: lighter towards the top of the part
        g = fx["gradient"][0]; base *= (1 - g + 2*g*np.clip(maps["pos"][..., 2], 0, 1))[..., None]
    if "edge_light" in fx:                                                                                 # stylised highlight line on convex edges
        a, k = fx["edge_light"]; w = np.clip(ss(1 - a, 1.0, convex), 0, 1)*paint; base = np.clip(base + (base*k + 0.04)*w[..., None], 0, 1)
    if "chips" in fx:                                                                                      # paint flaked off in patches, most where edges and cavities are
        a, rgb, me, zs = fx["chips"]; zm = np.zeros((H, W), bool)
        for z in zs: zm |= Z[z]
        c = ss(1.05 - a, 1.12 - a, fbm(P, 22.0, 4, seed + 21)*0.75 + 0.35*convex + 0.15*cavity)*zm; base = mix(base, np.array(rgb, np.float32)*(0.8 + 0.4*n2)[..., None], c); metal = mix(metal, me, c); rough = mix(rough, 0.6, c)
    if "edge_wear" in fx:
        a, rgb, me, ro = fx["edge_wear"]; w = np.clip(ss(1 - a, 1 - a*0.4, convex*(0.55 + 0.9*n2)), 0, 1)*paint; base = mix(base, np.array(rgb, np.float32), w); metal = mix(metal, me, w); rough = mix(rough, ro, w)
    if "rust" in fx:
        a, dark, light = fx["rust"]; drive = np.clip(0.60*cavity + 0.50*convex + 0.30*low + 0.20*(1 - up), 0, 1); nL = fbm(P, 4.0, 3, seed + 31); r = ss(1.00 - a, 1.16 - a, drive*0.85 + nL*0.55 + (n2 - 0.5)*0.30 + (n3 - 0.5)*0.15)*(~Z["glow"])*(~Z["rubber"])*(~Z["glass"])
        col = mix(np.array(dark, np.float32), np.array(light, np.float32), ss(0.3, 0.8, n2)[..., None]*np.ones(3, np.float32)); base = mix(base, col, r); metal = metal*(1 - r); rough = mix(rough, 0.92, r)
    g = Z["glow"]; gc = np.array(pal["glow"][0] if "glow" in pal else (1, 1, 1), np.float32); emis[g] = gc; base[g] = gc*0.5
    gl = Z["glass"]; core = 0.45 + 0.55*fbm(P*np.array([1.0, 1.0, 3.0], np.float32), 14.0, 3, seed + 41)                 # lit glass: an energy cell seen through its wall
    emis[gl] = gc*(0.07 + 0.20*core[gl])[:, None]; base[gl] = gc*0.08; rough[gl] = 0.06; metal[gl] = 0.0
    occ = np.clip(0.35 + 0.65*ao, 0, 1); orm = np.stack([occ, np.clip(rough, 0.02, 1), np.clip(metal, 0, 1)], -1)
    return dict(base=to_srgb(base), orm=orm, normal=maps["nrm"], emission=to_srgb(emis), masks=dict(convex=convex, cavity=cavity, paint=paint.astype(np.float32)))
