"""#1239 Visual Polish Pass 2: material character. Same zones / texture sets as pass 1; changes: Toon substructure is navy (cream read as plastic),
Brutal gets a second steel finish (heat-blued / oiled, zone R4P_Wear - proposal) next to brushed armour steel, polished steel and cast iron,
Sleek armour is a satin metallic lacquer with a stronger emission."""
from qstyle import ZONE_ORDER, TEXTURES, CHARACTER, CHARACTER_PL, VCOL, CORE_BEVEL
import qstyle as _Q
ZONE = {k: dict(v) for k, v in _Q.ZONE.items()}; ZONE["B"]["rust"] = "R4P_Wear"
MAT = {k: dict(v) for k, v in _Q.MAT.items()}
MAT["A"]["R4P_Base"] = ((0.40, 0.44, 0.51), 0.75, 0.26); MAT["A"]["R4P_Secondary"] = ((0.045, 0.052, 0.068), 0.75, 0.32)
MAT["B"]["R4P_Wear"] = ((0.115, 0.095, 0.075), 1.0, 0.36); MAT["B"]["R4P_Metal"] = ((0.78, 0.78, 0.79), 1.0, 0.24)
MAT["E"]["R4P_Trim"] = ((0.022, 0.032, 0.115), 0.0, 0.46)
GLOW_STRENGTH = {"A": 4.0, "B": 1.5, "C": 0.0, "D": 1.0, "E": 2.0}
CORE = {"A": "sec", "B": "dark", "C": "dark", "D": "sec", "E": "dark"}
