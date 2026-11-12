"""#1239 pilot 2: material character of the five styles (palette, PBR factors, texture SETS: base colour + ORM + normal) and paint zones.
Colours are linear RGB. glTF semantics: roughness = factor x ORM.G, metallic = factor x ORM.B, base colour = factor x texture."""
ZONE_ORDER = ["R4P_Base", "R4P_Secondary", "R4P_Trim", "R4P_Metal", "R4P_Glow", "R4P_Wear"]      # R4P_Wear = proposal (rust / exposed worn steel), only Scrap uses it
_Z = dict(plate="R4P_Base", plate_wall="R4P_Trim", frame="R4P_Secondary", bevel="R4P_Secondary", corner="R4P_Secondary", base="R4P_Base", sec="R4P_Secondary", dark="R4P_Trim", metal="R4P_Metal", glow="R4P_Glow", patch="R4P_Secondary", rust="R4P_Trim", bolt="R4P_Metal")
ZONE = {"A": dict(_Z), "B": dict(_Z, frame="R4P_Trim", bevel="R4P_Trim", corner="R4P_Trim"), "C": dict(_Z, plate_wall="R4P_Base", frame="R4P_Base", bevel="R4P_Base", corner="R4P_Base"),
        "D": dict(_Z, rust="R4P_Wear", plate_wall="R4P_Trim"), "E": dict(_Z)}
MAT = {  # zone: (rgb, metallic factor, roughness factor)
 "A": {"R4P_Base": ((0.80, 0.83, 0.86), 0.10, 0.24), "R4P_Secondary": ((0.085, 0.095, 0.12), 0.65, 0.30), "R4P_Trim": ((0.012, 0.014, 0.02), 0.30, 0.14), "R4P_Metal": ((0.70, 0.75, 0.80), 1.0, 0.16)},
 "B": {"R4P_Base": ((0.30, 0.31, 0.33), 1.0, 0.50), "R4P_Secondary": ((0.52, 0.27, 0.02), 0.55, 0.60), "R4P_Trim": ((0.05, 0.052, 0.056), 0.90, 0.66), "R4P_Metal": ((0.60, 0.60, 0.61), 1.0, 0.34)},
 "C": {"R4P_Base": ((0.36, 0.55, 0.68), 0.0, 0.88), "R4P_Secondary": ((0.80, 0.70, 0.46), 0.0, 0.88), "R4P_Trim": ((0.17, 0.21, 0.29), 0.0, 0.92), "R4P_Metal": ((0.72, 0.75, 0.78), 0.0, 0.85)},
 "D": {"R4P_Base": ((0.115, 0.10, 0.065), 0.65, 0.95), "R4P_Secondary": ((0.050, 0.065, 0.058), 0.65, 0.95), "R4P_Trim": ((0.018, 0.017, 0.016), 0.25, 1.0), "R4P_Metal": ((0.16, 0.155, 0.145), 1.0, 0.80), "R4P_Wear": ((0.17, 0.08, 0.035), 0.20, 1.0)},
 "E": {"R4P_Base": ((0.82, 0.13, 0.04), 0.0, 0.55), "R4P_Secondary": ((0.93, 0.80, 0.50), 0.0, 0.60), "R4P_Trim": ((0.045, 0.06, 0.20), 0.0, 0.55), "R4P_Metal": ((0.62, 0.72, 0.86), 0.25, 0.35)},
}
TEXTURES = {  # tiling sets, one tile = one 0.52 m block face (tri-planar); E: base colour = painted light ramp mapped by height
 "A": dict(base="A_base.png", orm="A_orm.png", normal="A_normal.png", normal_strength=0.35, uv="triplanar"),
 "B": dict(base="B_base.png", orm="B_orm.png", normal="B_normal.png", normal_strength=1.0, uv="triplanar"),
 "C": dict(base=None, orm=None, normal=None, normal_strength=0.0, uv="triplanar"),
 "D": dict(base="D_base.png", orm="D_orm.png", normal="D_normal.png", normal_strength=1.3, uv="triplanar"),
 "E": dict(base="E_base.png", orm=None, normal=None, normal_strength=0.0, uv="height"),
}
GLOW_STRENGTH = {"A": 1.6, "B": 0.6, "C": 0.0, "D": 0.4, "E": 0.8}
CHARACTER = {
 "A": "advanced technology: smooth compound housings, lacquer / ceramic, graphite frames, light strips and rings",
 "B": "heavy machinery: bare gun-metal, exposed mechanisms, bolts, ribs, gussets, cast surface",
 "C": "simple readable volumes, very few parts, calm matte colours, no emission, no textures (by design)",
 "D": "salvage: dark worn sheet metal, rust, welded patches, pipes, asymmetry, rough PBR",
 "E": "soft exaggerated proportions, big friendly forms, saturated hero colours, painted light ramp, few details",
}
CHARACTER_PL = {
 "A": "zaawansowana technologia: gładkie, złożone obudowy, lakier / ceramika, grafitowe ramy, paski i pierścienie światła",
 "B": "ciężka maszyna: goła stal, odsłonięte mechanizmy, śruby, żebra, zastrzały, odlew",
 "C": "proste, czytelne bryły, bardzo mało części, spokojne matowe kolory, bez emisji i bez tekstur (celowo)",
 "D": "złom: ciemne zużyte blachy, rdza, spawane łaty, rury, asymetria, surowy PBR",
 "E": "miękkie przerysowane proporcje, duże przyjazne formy, nasycone kolory, malowana rampa światła, mało detali",
}
