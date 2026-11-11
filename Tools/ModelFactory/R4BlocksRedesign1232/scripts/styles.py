"""Art-direction parameters for the #1232 pilot. Both directions use the SAME solids, ports and contact planes."""
# locked R41 v6.1 palette values (copied from r4lib.LOCKED of the approved pipeline)
R41 = {"Armor_Dark": ((0.035, 0.038, 0.045), 0.55, 0.45), "Armor_Gunmetal": ((0.16, 0.17, 0.18), 0.35, 0.55),
       "Armor_Plate_Light": ((0.27, 0.28, 0.285), 0.30, 0.58), "Steel_Light": ((0.55, 0.62, 0.68), 0.90, 0.25),
       "TeamColor_Red": ((0.60, 0.035, 0.025), 0.35, 0.38), "Hazard_Yellow": ((0.95, 0.60, 0.03), 0.15, 0.50)}
_ZONE = {"plate": "R4P_Base", "plate_wall": "R4P_Trim", "frame": "R4P_Secondary", "bevel": "R4P_Secondary", "corner": "R4P_Secondary"}
STYLES = {
 # A "Sleek": round edge fillets (constant 20 mm bevel width, 4 segments), rounded-corner plates, soft shoulder; frame = paintable piping
 "A": dict(label="A_Sleek", g=0.004, w=0.020, seg=4, margin=0.006, chamfer=0.004, rho=0.040, cseg=4, wall_smooth=True, zone=_ZONE,
           zone_default={"R4P_Base": "Armor_Plate_Light", "R4P_Secondary": "Armor_Gunmetal", "R4P_Trim": "Armor_Dark"}),
 # B "Industrial": hard single chamfers (10 mm), clipped-corner plates, wide dark chassis frame
 "B": dict(label="B_Industrial", g=0.005, w=0.010, seg=1, margin=0.014, chamfer=0.003, rho=0.045, cseg=1, zone=_ZONE,
           zone_default={"R4P_Base": "Armor_Gunmetal", "R4P_Secondary": "Armor_Dark", "R4P_Trim": "Armor_Dark"}),
}
ZONE_ORDER = ["R4P_Base", "R4P_Secondary", "R4P_Trim"]
