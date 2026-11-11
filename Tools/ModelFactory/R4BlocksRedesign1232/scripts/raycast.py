"""numpy ray casting + the approved contact-patch test (port of build_blocks.patch_test) for file-space or Blender-space triangles."""
import numpy as np

def ray_hits(orig, dirn, tris, tmax=np.inf):
    """orig (R,3), dirn (3,) unit, tris (T,3,3) -> nearest hit distance per ray (inf if none)"""
    v0, v1, v2 = tris[:, 0], tris[:, 1], tris[:, 2]; e1 = v1 - v0; e2 = v2 - v0
    p = np.cross(dirn, e2); det = np.einsum("ij,ij->i", e1, p)
    ok = np.abs(det) > 1e-14; inv = np.where(ok, 1.0/np.where(ok, det, 1.0), 0.0)
    out = np.full(len(orig), np.inf)
    for r in range(len(orig)):
        s = orig[r] - v0; u = np.einsum("ij,ij->i", s, p)*inv
        q = np.cross(s, e1); v = (q @ dirn)*inv; t = np.einsum("ij,ij->i", e2, q)*inv
        m = ok & (u >= -1e-9) & (v >= -1e-9) & (u + v <= 1 + 1e-9) & (t > 1e-9) & (t <= tmax)
        if m.any(): out[r] = t[m].min()
    return out

def frame_bl(n):
    n = np.asarray(n, float); n = n/np.linalg.norm(n)
    u = np.cross([0, 0, 1], n) if abs(n[2]) < 0.9 else np.cross([1, 0, 0], n)
    u = u/np.linalg.norm(u); return u, np.cross(n, u), n

DIRS_BL = {"right": (1, 0, 0), "left": (-1, 0, 0), "up": (0, 0, 1), "down": (0, 0, -1), "forward": (0, -1, 0), "back": (0, 1, 0)}
G, H = 0.55, 0.26

def patch_test(tris_bl, cell, d, off, half, N=9):
    """identical sampling / thresholds to the approved Blender patch_test; tris in Blender axes"""
    n = np.array(DIRS_BL[d], float); u, v, _ = frame_bl(n); c = np.array(cell, float)*G + n*H
    O = []
    for i in range(N):
        for j in range(N):
            a = (off[0] - half[0]*0.85 + 2*half[0]*0.85*i/(N - 1))*G; b = (off[1] - half[1]*0.85 + 2*half[1]*0.85*j/(N - 1))*G
            O.append(c + u*a + v*b + n*0.2)
    t = ray_hits(np.array(O), -n, tris_bl, 0.5)
    k = 0.2 - t                                    # signed height of the hit above the contact plane
    miss = int(np.isinf(t).sum()); prot = int((k > 0.0005).sum()); flush = int(((k <= 0.0005) & (k > -0.0010)).sum()); rec = int((np.isfinite(t) & (k <= -0.0010)).sum())
    return {"flush_pct": round(100*flush/(N*N), 1), "recessed_pct": round(100*rec/(N*N), 1), "protruding": prot, "missing": miss}
