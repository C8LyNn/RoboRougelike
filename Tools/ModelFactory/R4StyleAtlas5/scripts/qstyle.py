"""#1239 Visual Polish Pass 1: material character after the owner's review of pilot 2 ("toy / block look from a distance").
Same zones and texture sets as pilot 2; changed: grounded palettes with a clear value hierarchy (hull colour / dark substructure / one accent),
stronger metal for B, calmer and darker C, and baked per-vertex shading data (ambient occlusion + worn convex edges) multiplied over the
base colour (glTF COLOR_0) so dirt and wear follow the geometry of each model."""
from pstyle import ZONE_ORDER, ZONE, TEXTURES, CHARACTER, CHARACTER_PL
MAT = {
 "A": {"R4P_Base": ((0.40, 0.44, 0.51), 0.60, 0.30), "R4P_Secondary": ((0.050, 0.058, 0.075), 0.70, 0.34), "R4P_Trim": ((0.010, 0.012, 0.018), 0.40, 0.20), "R4P_Metal": ((0.72, 0.77, 0.82), 1.0, 0.18)},
 "B": {"R4P_Base": ((0.44, 0.45, 0.47), 1.0, 0.44), "R4P_Secondary": ((0.78, 0.42, 0.03), 0.35, 0.55), "R4P_Trim": ((0.060, 0.062, 0.066), 0.95, 0.62), "R4P_Metal": ((0.74, 0.74, 0.75), 1.0, 0.30)},
 "C": {"R4P_Base": ((0.045, 0.115, 0.18), 0.0, 0.70), "R4P_Secondary": ((0.55, 0.30, 0.05), 0.0, 0.68), "R4P_Trim": ((0.012, 0.015, 0.02), 0.0, 0.80), "R4P_Metal": ((0.30, 0.33, 0.36), 0.0, 0.60)},
 "D": {"R4P_Base": ((0.150, 0.125, 0.080), 0.65, 0.92), "R4P_Secondary": ((0.060, 0.080, 0.070), 0.65, 0.92), "R4P_Trim": ((0.020, 0.019, 0.018), 0.25, 1.0), "R4P_Metal": ((0.20, 0.195, 0.18), 1.0, 0.72), "R4P_Wear": ((0.19, 0.085, 0.035), 0.20, 1.0)},
 "E": {"R4P_Base": ((0.68, 0.065, 0.02), 0.0, 0.48), "R4P_Secondary": ((0.84, 0.60, 0.24), 0.0, 0.55), "R4P_Trim": ((0.020, 0.028, 0.10), 0.0, 0.50), "R4P_Metal": ((0.56, 0.66, 0.80), 0.35, 0.32)},
}
GLOW_STRENGTH = {"A": 3.0, "B": 1.2, "C": 0.0, "D": 0.8, "E": 1.6}
# baked vertex shading: colour = flat * lerp(cavity, 1, ao^gamma), convex worn edges pulled towards `edge` (all linear RGB multipliers of the base colour)
VCOL = {
 "A": dict(flat=0.90, cavity=(0.42, 0.46, 0.55), gamma=1.0, edge=(1.0, 1.0, 1.0), edge_amount=0.55, dist=0.12),
 "B": dict(flat=0.72, cavity=(0.22, 0.21, 0.20), gamma=1.3, edge=(1.0, 1.0, 1.0), edge_amount=0.95, dist=0.14),
 "C": dict(flat=0.96, cavity=(0.55, 0.58, 0.66), gamma=0.9, edge=(1.0, 1.0, 1.0), edge_amount=0.20, dist=0.14),
 "D": dict(flat=0.74, cavity=(0.26, 0.17, 0.11), gamma=1.5, edge=(1.0, 0.94, 0.84), edge_amount=0.85, dist=0.16),
 "E": dict(flat=0.90, cavity=(0.50, 0.36, 0.56), gamma=0.9, edge=(1.0, 1.0, 1.0), edge_amount=0.45, dist=0.16),
}
CORE = {"A": "sec", "B": "dark", "C": "dark", "D": "sec", "E": "dark"}            # the structural colour under the armour (cube core, mount plates)
CORE_BEVEL = {"A": 0.007, "B": 0.004, "C": 0.010, "D": 0.005, "E": 0.016}
