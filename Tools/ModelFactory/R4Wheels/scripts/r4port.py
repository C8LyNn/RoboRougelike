# RoboRougelike R4 "Robocraft-style" mounting port - one standard snap interface for every weapon and movement part.
# The port is a square chamfered plate whose OUTER face is flush with the in-game block face (0.26 m from the cell
# centre = BlockSize 0.52 / 2 on the 0.55 m grid), centred on that face (the game's face-centre port, R3MountingGrid).
# Outer face: flat contact patch with recessed coupling ring, centre socket and 4 locating holes (nothing protrudes).
# Inner face: lip frame, 4 corner bolts, hazard corner ticks, team-red forward index.
# Build order: (1) trim everything beyond FACE-0.002 on the port side, (2) plate + dressing.
import math
from mathutils import Matrix
import r4lib as L

FACE = 0.260          # outer face of the port = block face
HALF = 0.2545         # plate half-size (just inside the R41 0.255 plate so side faces never coincide)
THICK = 0.016
CH = 0.040


def _csq(hs, c, z):
    return [(x, y, z) for (x, y) in ((hs, hs - c), (hs - c, hs), (-hs + c, hs), (-hs, hs - c), (-hs, -hs + c), (-hs + c, -hs), (hs - c, -hs), (hs, -hs + c))]


def _map(objs, sign):
    """pieces are authored with the port normal +Z (Blender up); sign -1 = bottom face (rotate 180 deg about Y: forward stays forward)"""
    if sign < 0:
        R_ = Matrix.Rotation(math.pi, 4, 'Y')
        for o in objs: o.data.transform(R_)


def port(target, work, sign=+1, trim=True, half=HALF):
    M = L.M
    try:
        work.name
    except Exception:
        work = None
    if trim:
        t = L.Part(work, "portcut")
        c = L.box("port_trim", -0.40, 0.40, -0.40, 0.40, FACE - 0.002, FACE + 0.20, M["Armor_Dark"], t.D)
        _map([c], sign); t.build(target)
    p = L.Part(work, "port")
    z0, z1 = FACE - THICK, FACE
    made = [L.hull("port_plate", _csq(half, CH, z0) + _csq(half, CH, z1), M["Armor_Dark"], p.U, bevel=0.002)]
    # inner lip frame (visible band under / above the plate)
    hi = half - 0.012
    for (a, b, c_, d) in ((-hi, hi, hi - 0.022, hi), (-hi, hi, -hi, -hi + 0.022), (hi - 0.022, hi, -hi + 0.022, hi - 0.022), (-hi, -hi + 0.022, -hi + 0.022, hi - 0.022)):
        made.append(L.box("port_lip", a, b, c_, d, z0 - 0.007, z0 + 0.002, M["Armor_Gunmetal"], p.U, bevel=0.002))
    for (x, y) in ((0.212, 0.212), (-0.212, 0.212), (0.212, -0.212), (-0.212, -0.212)):
        made.append(L.hexbolt("port_bolt", (x, y, z0 - 0.0060), (x, y, z0 - 0.0125), M["Steel_Light"], p.Pc, r=0.0075))
    # contact face recesses (stay below FACE)
    made.append(L.tube("port_groove", 0.092, 0.072, (0, 0, z1 - 0.0045), (0, 0, z1 + 0.010), M["Armor_Dark"], p.D, seg=24))
    made.append(L.cyl("port_socket", 0.034, (0, 0, z1 - 0.0065), (0, 0, z1 + 0.010), M["Armor_Dark"], p.D, seg=24))
    for (x, y) in ((0.150, 0.150), (-0.150, 0.150), (0.150, -0.150), (-0.150, -0.150)):
        made.append(L.cyl("port_pin_hole", 0.013, (x, y, z1 - 0.007), (x, y, z1 + 0.010), M["Armor_Dark"], p.D, seg=12))
    made.append(L.tube("port_glow", 0.0905, 0.0735, (0, 0, z1 - 0.0042), (0, 0, z1 - 0.0022), M["Energy_Hot"], p.Pc, seg=24))
    made.append(L.cyl("port_core", 0.0325, (0, 0, z1 - 0.0062), (0, 0, z1 - 0.0040), M["Steel_Light"], p.Pc, seg=24))
    # side faces: hazard ticks at the 4 corners, team-red forward index (Blender -Y = forward)
    for sx in (-1, 1):
        for sy in (-1, 1):
            made.append(L.box("port_haz", sx*(half - 0.0005), sx*(half + 0.0018), sy*(half - CH - 0.050), sy*(half - CH - 0.010), z0 + 0.003, z1 - 0.003, M["Hazard_Yellow"], p.Pc))
            made.append(L.box("port_haz", sx*(half - CH - 0.050), sx*(half - CH - 0.010), sy*(half - 0.0005), sy*(half + 0.0018), z0 + 0.003, z1 - 0.003, M["Hazard_Yellow"], p.Pc))
    made.append(L.box("port_fwd", -0.030, 0.030, -half - 0.0018, -half + 0.0005, z0 + 0.003, z1 - 0.003, M["TeamColor_Red"], p.Pc))
    made.append(L.box("port_fwd_notch", -0.010, 0.010, -half - 0.010, -half + 0.008, z1 - 0.0045, z1 + 0.010, M["Armor_Dark"], p.D))
    _map(made, sign)
    p.build(target)
    target["mount_port"] = f"R4 Robocraft-style port: {2*half:.3f} m square, outer face {'+' if sign > 0 else '-'}{FACE:.3f} m (block face), face-centre coupling"
    return target
