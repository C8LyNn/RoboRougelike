"""#1239 Hero Benchmark Pass: art direction pulled towards named benchmarks (quality / character targets, no asset is copied).
 A Sleek   - Destiny 2 / Armored Core VI: satin ceramic armour over a dark gunmetal frame, titanium mechanics, thin cold light, clear coat
 B Brutal  - heavy industrial / military machinery: painted olive-grey plate, bare and blued steel, cast iron, hazard yellow, hard chamfers
 C Arcade  - Astroneer clarity: soft off-white bodies, ONE saturated accent, teal second accent, dark slate rubber; matte, no textures, big round edges
 D Scrap   - Crossout: faded khaki and red-oxide sheets, rust, bare steel, soot; rough PBR, hard thin edges
 E Toon    - Overwatch + Roboquest: saturated orange armour, off-white panels, dark blue-grey undersuit, cyan light; round edges; banded light + outline"""
from rstyle import ZONE_ORDER, TEXTURES, CHARACTER, CHARACTER_PL, CORE_BEVEL
import rstyle as _R
ZONE = {k: dict(v) for k, v in _R.ZONE.items()}
MAT = {  # zone: (rgb, metallic, roughness[, coat])
 "A": {"R4P_Base": ((0.50, 0.51, 0.52), 0.30, 0.34, 0.7), "R4P_Secondary": ((0.030, 0.033, 0.040), 0.90, 0.36), "R4P_Trim": ((0.008, 0.009, 0.012), 0.20, 0.30), "R4P_Metal": ((0.52, 0.49, 0.45), 1.0, 0.26)},
 "B": {"R4P_Base": ((0.20, 0.215, 0.185), 0.70, 0.50), "R4P_Secondary": ((0.80, 0.45, 0.02), 0.30, 0.52), "R4P_Trim": ((0.045, 0.047, 0.05), 0.95, 0.60), "R4P_Metal": ((0.80, 0.80, 0.81), 1.0, 0.22), "R4P_Wear": ((0.115, 0.095, 0.075), 1.0, 0.34)},
 "C": {"R4P_Base": ((0.74, 0.74, 0.70), 0.0, 0.85), "R4P_Secondary": ((0.90, 0.30, 0.03), 0.0, 0.80), "R4P_Trim": ((0.040, 0.048, 0.062), 0.0, 0.90), "R4P_Metal": ((0.04, 0.42, 0.46), 0.0, 0.75)},
 "D": {"R4P_Base": ((0.155, 0.135, 0.075), 0.60, 0.92), "R4P_Secondary": ((0.22, 0.055, 0.035), 0.55, 0.92), "R4P_Trim": ((0.020, 0.019, 0.018), 0.25, 1.0), "R4P_Metal": ((0.21, 0.20, 0.185), 1.0, 0.66), "R4P_Wear": ((0.19, 0.085, 0.035), 0.20, 1.0)},
 "E": {"R4P_Base": ((0.92, 0.36, 0.02), 0.0, 0.45), "R4P_Secondary": ((0.82, 0.80, 0.72), 0.0, 0.50), "R4P_Trim": ((0.022, 0.030, 0.055), 0.0, 0.50), "R4P_Metal": ((0.42, 0.50, 0.60), 0.30, 0.35)},
}
GLOW_STRENGTH = {"A": 6.0, "B": 2.0, "C": 1.2, "D": 1.2, "E": 4.0}
GLOW_RGB = {"A": (0.45, 0.82, 1.0), "B": None, "C": None, "D": None, "E": (0.15, 0.85, 1.0)}             # A and E use one signature light colour on every part
CORE = {"A": "sec", "B": "dark", "C": "dark", "D": "sec", "E": "dark"}
BEVEL = {"A": dict(width=0.0040, segments=2), "B": dict(width=0.0032, segments=1), "C": dict(width=0.014, segments=3), "D": dict(width=0.0022, segments=1), "E": dict(width=0.010, segments=3)}   # real geometry, built in Blender
VCOL = dict(_R.VCOL)
VCOL["A"] = dict(flat=0.94, cavity=(0.40, 0.43, 0.50), gamma=1.0, edge=(1.0, 1.0, 1.0), edge_amount=0.35, dist=0.12)
VCOL["C"] = dict(flat=1.0, cavity=(0.62, 0.64, 0.70), gamma=0.8, edge=(1.0, 1.0, 1.0), edge_amount=0.0, dist=0.18)
VCOL["E"] = dict(flat=1.0, cavity=(0.55, 0.42, 0.58), gamma=0.8, edge=(1.0, 1.0, 1.0), edge_amount=0.0, dist=0.18)
TOON = {"E": dict(bands=[(0.0, 0.50), (0.30, 0.78), (0.62, 1.0)], rim=0.30, outline_m=0.0055)}          # look-dev shader of the renders; the game needs the same in URP
