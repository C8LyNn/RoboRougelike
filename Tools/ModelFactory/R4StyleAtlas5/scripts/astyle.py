"""#1239 style tables: geometry language, default materials, three paint presets and motion character of the five styles."""
import math
STYLES = ["A", "B", "C", "D", "E"]
LABEL = {"A": "A_Sleek", "B": "B_Brutal", "C": "C_Arcade", "D": "D_Scrap", "E": "E_Toon"}
NAME = {"A": "Sleek Futuristic", "B": "Brutal Industrial", "C": "Arcade Geometric", "D": "Scrap Wasteland", "E": "Heroic Toon"}
# geometry parameters of the 'plate on a bevelled frame' generators (metres). s = part scale 0.2..1
GEO = {
 # w + g + margin + chamfer <= 0.040 m is the limit set by the approved contact ruler (a full port must stay >= 80 % flush)
 "A": dict(w=0.020, seg=4, g=0.004, margin=0.006, chamfer=0.004, rho=0.040, cseg=4, wall_smooth=True),     # precise round fillets, rounded-rect plates
 "B": dict(w=0.010, seg=1, g=0.008, margin=0.012, chamfer=0.002, rho=0.070, cseg=1, wall_smooth=False),    # hard chamfer, deep step, octagonal plates
 "C": dict(w=0.036, seg=3, g=0.0, margin=0.0, chamfer=0.0, rho=0.0, cseg=1, wall_smooth=True),             # one soft roll, no panel lines at all
 "D": dict(w=0.008, seg=1, g=0.005, margin=0.008, chamfer=0.003, rho=0.016, cseg=1, wall_smooth=False),    # thin bent sheet edges, clipped sheets
 "E": dict(w=0.030, seg=6, g=0.004, margin=0.004, chamfer=0.002, rho=0.095, cseg=8, wall_smooth=True),     # big roll, pill-shaped plates
}
LATHE_N = {"A": 1.2, "B": 0.75, "C": 0.7, "D": 0.6, "E": 1.2}          # multiplier of the authored segment count
LATHE_MIN = {"A": 10, "B": 8, "C": 8, "D": 7, "E": 10}
LATHE_FLAT = {"A": False, "B": False, "C": False, "D": True, "E": False}   # D: hand-cut facets
FAT = {"A": 0.85, "B": 1.15, "C": 1.25, "D": 1.0, "E": 1.35}           # thickness of rods / torus tubes
# group -> paint zone per style (the SAME five zones in every style)
_Z = {"plate": "R4P_Base", "plate_wall": "R4P_Trim", "frame": "R4P_Secondary", "bevel": "R4P_Secondary", "corner": "R4P_Secondary",
      "base": "R4P_Base", "sec": "R4P_Secondary", "dark": "R4P_Trim", "metal": "R4P_Metal", "glow": "R4P_Glow", "patch": "R4P_Secondary", "rust": "R4P_Trim", "bolt": "R4P_Metal"}
ZONE = {s: dict(_Z) for s in STYLES}
ZONE["B"].update(frame="R4P_Trim", bevel="R4P_Trim", corner="R4P_Trim")            # coal frame, steel panels
ZONE["C"].update(frame="R4P_Base", bevel="R4P_Base", corner="R4P_Base", plate_wall="R4P_Base")   # one colour per solid
ZONE["E"].update(frame="R4P_Secondary", bevel="R4P_Secondary", corner="R4P_Secondary", plate_wall="R4P_Trim")   # cream rim around every colour block
ZONE_ORDER = ["R4P_Base", "R4P_Secondary", "R4P_Trim", "R4P_Metal", "R4P_Glow"]
# default materials per style: zone -> (rgb linear, metallic, roughness). Glow colour stays per asset; strength per style.
MAT = {
 "A": {"R4P_Base": ((0.78, 0.80, 0.82), 0.15, 0.22), "R4P_Secondary": ((0.20, 0.22, 0.26), 0.45, 0.30), "R4P_Trim": ((0.03, 0.034, 0.042), 0.60, 0.35), "R4P_Metal": ((0.62, 0.68, 0.74), 0.95, 0.15)},
 "B": {"R4P_Base": ((0.30, 0.32, 0.34), 0.90, 0.42), "R4P_Secondary": ((0.50, 0.17, 0.03), 0.55, 0.50), "R4P_Trim": ((0.045, 0.047, 0.05), 0.85, 0.55), "R4P_Metal": ((0.52, 0.53, 0.54), 1.00, 0.30)},
 "C": {"R4P_Base": ((0.55, 0.75, 0.90), 0.0, 0.85), "R4P_Secondary": ((0.98, 0.86, 0.55), 0.0, 0.85), "R4P_Trim": ((0.30, 0.34, 0.45), 0.0, 0.90), "R4P_Metal": ((0.88, 0.90, 0.93), 0.0, 0.80)},
 "D": {"R4P_Base": ((0.20, 0.18, 0.14), 0.45, 0.88), "R4P_Secondary": ((0.13, 0.15, 0.13), 0.40, 0.90), "R4P_Trim": ((0.16, 0.07, 0.03), 0.20, 0.97), "R4P_Metal": ((0.17, 0.165, 0.155), 0.85, 0.70)},
 "E": {"R4P_Base": ((0.95, 0.58, 0.42), 0.0, 0.60), "R4P_Secondary": ((0.97, 0.93, 0.82), 0.0, 0.65), "R4P_Trim": ((0.22, 0.24, 0.42), 0.0, 0.65), "R4P_Metal": ((0.74, 0.80, 0.90), 0.35, 0.40)},
}
GLOW_STRENGTH = {"A": 1.6, "B": 0.5, "C": 0.0, "D": 0.35, "E": 0.9}       # C: flat colour, no emission
# three paint presets per style (player paint replaces Base / Secondary; Trim / Metal stay style defaults; glow tint separate)
PRESETS = {
 "A": {"neutral": {"R4P_Base": (0.78, 0.80, 0.82), "R4P_Secondary": (0.20, 0.22, 0.26)}, "team": {"R4P_Base": (0.03, 0.17, 0.58), "R4P_Secondary": (0.85, 0.87, 0.90)}, "bright": {"R4P_Base": (0.02, 0.55, 0.50), "R4P_Secondary": (0.10, 0.10, 0.12)}},
 "B": {"neutral": {"R4P_Base": (0.30, 0.32, 0.34), "R4P_Secondary": (0.50, 0.17, 0.03)}, "team": {"R4P_Base": (0.36, 0.05, 0.04), "R4P_Secondary": (0.62, 0.55, 0.10)}, "bright": {"R4P_Base": (0.62, 0.46, 0.06), "R4P_Secondary": (0.06, 0.06, 0.07)}},
 "C": {"neutral": {"R4P_Base": (0.55, 0.75, 0.90), "R4P_Secondary": (0.98, 0.86, 0.55)}, "team": {"R4P_Base": (0.95, 0.52, 0.50), "R4P_Secondary": (0.97, 0.95, 0.90)}, "bright": {"R4P_Base": (0.60, 0.88, 0.62), "R4P_Secondary": (0.96, 0.70, 0.86)}},
 "D": {"neutral": {"R4P_Base": (0.20, 0.18, 0.14), "R4P_Secondary": (0.13, 0.15, 0.13)}, "team": {"R4P_Base": (0.26, 0.07, 0.05), "R4P_Secondary": (0.22, 0.20, 0.15)}, "bright": {"R4P_Base": (0.10, 0.17, 0.24), "R4P_Secondary": (0.30, 0.24, 0.08)}},
 "E": {"neutral": {"R4P_Base": (0.95, 0.58, 0.42), "R4P_Secondary": (0.97, 0.93, 0.82)}, "team": {"R4P_Base": (0.42, 0.62, 0.96), "R4P_Secondary": (0.99, 0.90, 0.55)}, "bright": {"R4P_Base": (0.72, 0.55, 0.92), "R4P_Secondary": (0.70, 0.96, 0.86)}},
}
MOTION = {"A": "fluid and precise: long ease in / out, no overshoot", "B": "heavy and mechanical: slow start, hard stop, damped settle", "C": "simple, fast, readable: quick ease, short hold, no secondary motion",
          "D": "raw and uneven: stutter, rattle, slight drift (seeded)", "E": "expressive: anticipation, overshoot, springy settle"}
TEXTURE = {"A": "A_detail.png", "B": "B_detail.png", "C": None, "D": "D_detail.png", "E": "E_ramp.png"}       # multiplied over the paint colour; C is flat by design
UV_MODE = {"A": "triplanar", "B": "triplanar", "C": "triplanar", "D": "triplanar", "E": "height"}


def _smooth(u): return u*u*(3 - 2*u)
def _noise(seed, x):
    def h(i): return math.sin((i*127.1 + seed*311.7))*43758.5453 % 1.0
    i = math.floor(x); f = x - i; return (h(i)*(1 - f) + h(i + 1)*f)*2 - 1


def ease(style, u, seed=0.0):
    """0 -> 1 transition, exact at both ends; the functional end values are identical in every style"""
    u = min(1.0, max(0.0, u))
    if u in (0.0, 1.0): return u
    if style == "A": return _smooth(u)
    if style == "B":
        if u < 0.72: return (u/0.72)**2.4
        t = (u - 0.72)/0.28; return 1.0 + 0.055*math.sin(2*math.pi*2*t)*(1 - t)**2
    if style == "C": return 1.0 if u > 0.6 else 1 - (1 - u/0.6)**3                              # quick ease-out, then a clear hold
    if style == "D": return min(1.2, max(-0.2, _smooth(u**1.3) + 0.07*_noise(seed, u*9.0)*math.sin(math.pi*u)))
    if u < 0.22: return -0.16*math.sin(math.pi*u/0.22)                                      # E: anticipation
    t = (u - 0.22)/0.78; c1 = 2.2; return 1 + (c1 + 1)*(t - 1)**3 + c1*(t - 1)**2             # back-out overshoot


def shake(style, u, seed=0.0):
    """-1..1 decaying shake used by the cosmetic Hit clip (zero at both ends)"""
    env = (1 - u)**2*math.sin(math.pi*min(1.0, u*6))
    if style == "A": return env*math.sin(2*math.pi*3*u)*0.6
    if style == "B": return env*math.sin(2*math.pi*1.5*u)
    if style == "C": return (1.0 if u < 0.25 else (-0.6 if u < 0.5 else (0.3 if u < 0.75 else 0.0)))*(0.0 if u in (0.0, 1.0) else 1.0)
    if style == "D": return env*(math.sin(2*math.pi*4*u) + 0.8*_noise(seed, u*17))*0.7
    return env*math.sin(2*math.pi*2*u)*1.5
