"""#1239 Hero Benchmark Pass 4: palettes, wear recipes and survey baselines (pure data: used by the Blender-side skins and by the cloud-side generators).
Colours are linear RGB; (rgb, metallic, roughness) per material id of the source model. A palette key is a style letter, plus:
  Bo  Brutal hull proposal P (olive painted plate, the Pass 1 direction)          Dw  Scrap wheel (keeps the Pass 2 salvage colours and one striped plate)"""
import h2pal
BASELINE = {("Wheel", "A", "p"): "new", ("Wheel", "B", "p"): "old", ("Wheel", "B", "q"): "new", ("Wheel", "C", "p"): "new", ("Wheel", "D", "p"): "new", ("Wheel", "E", "p"): "new",
            ("Battery", "A", "p"): "old", ("Battery", "B", "p"): "old", ("Battery", "C", "p"): "old", ("Battery", "C", "q"): "new", ("Battery", "D", "p"): "old", ("Battery", "E", "p"): "new",
            ("Gun", "A", "p"): "old", ("Gun", "B", "p"): "new", ("Gun", "C", "p"): "new", ("Gun", "D", "p"): "old", ("Gun", "E", "p"): "new"}
P2 = h2pal.PAL; F2 = h2pal.FX
PAL = {"A": dict(P2["A"]), "C": dict(P2["C"]), "E": dict(P2["E"]),
       "B": dict(P2["B"], alt=((0.20, 0.075, 0.035), 0.0, 0.30), alt2=((0.50, 0.21, 0.07), 1.0, 0.34)),                                    # + glazed porcelain, copper
       "Bo": dict(P2["B"], base=((0.20, 0.215, 0.185), 0.7, 0.50), sec=((0.80, 0.45, 0.02), 0.3, 0.52), dark=((0.045, 0.047, 0.05), 0.95, 0.60)),   # hull proposal P: olive painted plate of Pass 1
       # Scrap follows the Pass 1 direction: ONE family (red oxide, brown, rust); the only off-colour is a single primer-grey repair plate (alt2)
       "D": dict(base=((0.185, 0.042, 0.028), 0.3, 0.80), sec=((0.165, 0.080, 0.045), 0.3, 0.84), alt=((0.125, 0.036, 0.026), 0.3, 0.86), alt2=((0.20, 0.195, 0.175), 0.3, 0.70), dark=((0.016, 0.014, 0.012), 0.6, 0.82),
                 metal=((0.105, 0.098, 0.090), 1.0, 0.55), bare=((0.34, 0.32, 0.30), 1.0, 0.42), rust=((0.070, 0.030, 0.014), 0.1, 0.92), rubber=((0.018, 0.016, 0.014), 0.0, 0.90), glow=((1.0, 0.36, 0.03), 0.0, 0.3), glass=((0.12, 0.05, 0.01), 0.0, 0.10)),
       "Dw": dict(P2["D"])}                                                                                                                 # the Scrap wheel keeps the Pass 2 direction (its own salvage colours, one striped plate)
FX = {"A": F2["A"], "B": F2["B"], "Bo": dict(F2["B"], chips=(0.42, (0.10, 0.10, 0.10), 1.0, ["sec", "base"]), zones_paint=["sec", "base"]), "C": F2["C"], "E": F2["E"],
      "D": {k: v for k, v in dict(F2["D"], chips=(0.55, (0.070, 0.034, 0.017), 0.1, ["base", "sec", "alt", "alt2"])).items() if k != "stripes"},
      "Dw": dict(F2["D"], stripes=("sec", (0.50, 0.46, 0.38), 16.0))}
def skin_key(typ, style, set_="p"): return "Dw" if (style == "D" and typ == "Wheel") else style
