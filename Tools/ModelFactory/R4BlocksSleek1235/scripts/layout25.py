"""Robot layouts for the #1235 before/after boards (all 30 R4 building blocks, multi-cell parts included).
Coordinates = file / glTF axes of the approved models: x = Port_right(+), y = up, z = Port_forward(+). One cell = 0.55 m.
Cells and ports of every block are read from the APPROVED PR #963 QA records - nothing about the interface is defined here."""
import itertools, json, os
import numpy as np
import paths25

G = 0.55
DIR = {"right": (1, 0, 0), "left": (-1, 0, 0), "up": (0, 1, 0), "down": (0, -1, 0), "forward": (0, 0, 1), "back": (0, 0, -1)}
NAME = {v: k for k, v in DIR.items()}
OPP = {"right": "left", "left": "right", "up": "down", "down": "up", "forward": "back", "back": "forward"}
PILOT = ["Cube", "Wedge", "Tetra", "CornerWedge", "HalfBlock"]
WAVES = {1: ["InnerCorner", "HalfWedge", "LongWedge", "WideWedge", "Pyramid", "HexColumn", "OctColumn", "Plate1x2", "Plate2x2", "Beam2", "Beam3", "Slab2x2", "BigCube2"],
         2: ["RoundEdge", "RoundCorner", "Cylinder", "Cone", "Dome", "RoundTop"], 3: ["Step", "CornerL3", "Tee4"], 4: ["Arch", "CoveEdge", "TrussCube"]}
NEW25 = [k for w in (1, 2, 3, 4) for k in WAVES[w]]
ALL30 = PILOT + NEW25

def b2f_cell(c): return (int(c[0]), int(c[2]), -int(c[1]))               # Blender cell (i, j, k) -> file cell
def approved_qa(kind): return json.load(open(paths25.ref_file("blocks", f"R4B_{kind}", f"R4B_{kind}_QA.json")))
INFO = {}
for _k in ALL30:
    _q = approved_qa(_k)
    INFO[_k] = {"cells": [b2f_cell(c) for c in _q["cells"]], "ports": [(b2f_cell(p["cell"]), p["dir"], "full" if p["kind"] == "full" else "part") for p in _q["ports"]],
                "ports_raw": _q["ports"], "mask": _q["contact_mask_local_anchor"]}

def rotations():
    out = []
    for perm in itertools.permutations(range(3)):
        for signs in itertools.product((1, -1), repeat=3):
            M = np.zeros((3, 3), int)
            for r in range(3): M[r, perm[r]] = signs[r]
            if round(np.linalg.det(M)) == 1: out.append(M)
    out.sort(key=lambda M: -int(np.trace(M)))
    return out
ROTS = rotations()
def rot_y(k): return [np.eye(3, dtype=int), np.array([[0, 0, 1], [0, 1, 0], [-1, 0, 0]]), np.array([[-1, 0, 0], [0, 1, 0], [0, 0, -1]]), np.array([[0, 0, -1], [0, 1, 0], [1, 0, 0]])][k % 4]
def rdir(M, d): return NAME[tuple(int(x) for x in M @ np.array(DIR[d]))]
def rcell(M, c): return tuple(int(x) for x in M @ np.array(c))

def ori(fwd="forward", up="up"):
    """rotation that sends the block's own forward (+z) to `fwd` and its own up (+y) to `up`"""
    F = np.array(DIR[fwd]); U = np.array(DIR[up]); assert F @ U == 0
    return np.stack([np.cross(U, F), U, F], axis=1).astype(int)

def sig(kind, M=None):
    """ports of the anchor cell after rotation: {dir: 'full' | 'part'}"""
    M = np.eye(3, dtype=int) if M is None else M
    return {rdir(M, d): k for c, d, k in INFO[kind]["ports"] if c == (0, 0, 0)}

def find(kind, want):
    """first rotation whose anchor-cell port signature is exactly `want` (for the corner-type blocks and their mirror images)"""
    for M in ROTS:
        if sig(kind, M) == want: return M
    raise ValueError((kind, want))

class Robot:
    def __init__(self, name, title=""): self.name = name; self.title = title; self.parts = []; self.cells = {}
    def put(self, kind, cell, M=None, paint="hull"):
        M = np.eye(3, dtype=int) if M is None else np.array(M); cell = tuple(int(c) for c in cell); idx = len(self.parts)
        cells = [tuple(int(x) for x in np.array(cell) + np.array(rcell(M, c))) for c in INFO[kind]["cells"]]
        for c in cells:
            assert c not in self.cells, (kind, cell, "cell taken", c, self.parts[self.cells[c]].get("kind") or self.parts[self.cells[c]].get("model"))
        for c in cells: self.cells[c] = idx
        self.parts.append(dict(type="block", kind=kind, cell=cell, rot=M, paint=paint, cells=cells)); return self
    def free(self, cell): return tuple(cell) not in self.cells
    def module(self, cls, model, cell, rot=None, extra_cells=(), mount=None):
        cell = tuple(cell); M = np.eye(3, dtype=int) if rot is None else np.array(rot); idx = len(self.parts)
        for c in (cell,) + tuple(tuple(x) for x in extra_cells):
            assert c not in self.cells, (c, model); self.cells[c] = idx
        self.parts.append(dict(type=cls, model=model, cell=cell, rot=M, mount=mount, cells=[cell] + [tuple(x) for x in extra_cells])); return self
    def mat4(self, p):
        m = np.eye(4); m[:3, :3] = p["rot"]; m[:3, 3] = np.array(p["cell"], float)*G; return m
    def count(self):
        c = {}
        for p in self.parts:
            k = p.get("kind") or p["type"]; c[k] = c.get(k, 0) + 1
        return c
    def block_ports(self, p):
        """world ports of a block part: [(cell, dir, 'full' | 'part')]"""
        return [(tuple(int(x) for x in np.array(p["cell"]) + np.array(rcell(p["rot"], c))), rdir(p["rot"], d), k) for c, d, k in INFO[p["kind"]]["ports"]]


# ================================================================================================ helpers used by the layouts
I3 = np.eye(3, dtype=int)
ALONG_Z = rot_y(3)                 # block's own +x (the long axis of Beam / Plate1x2 / WideWedge) -> world forward
def inner_of(x): return "left" if x > 0 else "right"
def outer_of(x): return "right" if x > 0 else "left"
WHEEL_L = "wheels:R4M_WheelL_Side_Offroad"; WHEEL_M = "wheels:R4M_WheelM_Side_Offroad"


# ------------------------------------------------------------------------------------------------ robot 1: Glacis Rover, updated
def rover():
    """R1 'Glacis Rover' (layout of the #1232 pilot, rebuilt with the full catalogue): slab / beam belly, long-wedge glacis,
    rounded casemate skirt, plated roof with a sensor dome."""
    R = Robot("R1_GlacisRover", "R1 „Glacis Rover” — układ pilota z pełnym katalogiem")
    stripe = lambda x: "accent" if x == 0 else "hull"
    # level 0: belly out of 2x2 slabs and beams
    for x0 in (-2, 1):
        for z0 in (-3, -1, 1): R.put("Slab2x2", (x0, 0, z0), paint="belly")
        R.put("Beam2", (x0, 0, 3), paint="belly"); R.put("Beam2", (x0, 0, 4), paint="belly")
    R.put("Beam3", (0, 0, -3), ALONG_Z, "belly"); R.put("Beam3", (0, 0, 0), ALONG_Z, "belly"); R.put("Beam2", (0, 0, 3), ALONG_Z, "belly")
    for x in (-1, 0, 1):
        R.put("Wedge", (x, 0, 5), find("Wedge", {"up": "full", "back": "full", "left": "part", "right": "part"}), "belly")
        R.put("Wedge", (x, 0, -4), find("Wedge", {"up": "full", "forward": "full", "left": "part", "right": "part"}), "belly")
    for sx in (-2, 2):
        R.put("CornerWedge", (sx, 0, 5), find("CornerWedge", {"up": "full", "back": "part", inner_of(sx): "part"}), "belly")
        R.put("CornerWedge", (sx, 0, -4), find("CornerWedge", {"up": "full", "forward": "part", inner_of(sx): "part"}), "belly")
    # level 1: hull sides as beams, long-wedge glacis over two cells
    for sx in (-2, 2):
        R.put("Beam3", (sx, 1, -3), ALONG_Z, "hull"); R.put("Beam3", (sx, 1, 0), ALONG_Z, "hull"); R.put("Cube", (sx, 1, 3), paint="hull")
        R.put("Wedge", (sx, 1, 4), find("Wedge", {"down": "full", inner_of(sx): "full", "forward": "part", "back": "part"}), "hull")
        R.put("Tetra", (sx, 1, 5), find("Tetra", {"down": "part", "back": "part", inner_of(sx): "part"}), "hull")
        R.put("CornerWedge", (sx, 1, -4), find("CornerWedge", {"down": "full", "forward": "part", inner_of(sx): "part"}), "hull")
    for x in (-1, 0, 1):
        R.put("Beam3", (x, 1, -3), ALONG_Z, stripe(x)); R.put("Beam3", (x, 1, 0), ALONG_Z, stripe(x)); R.put("Cube", (x, 1, 3), paint=stripe(x))
        R.put("LongWedge", (x, 1, 4), I3, stripe(x))
        R.put("Wedge", (x, 1, -4), find("Wedge", {"down": "full", "forward": "full", "left": "part", "right": "part"}), stripe(x))
    # level 2: casemate with a rounded skirt
    for x in (-1, 0, 1):
        R.put("Beam3", (x, 2, -2), ALONG_Z, stripe(x))
        R.put("RoundEdge", (x, 2, 1), I3, stripe(x))
        R.put("RoundEdge", (x, 2, -3), find("RoundEdge", {"down": "full", "forward": "full", "left": "part", "right": "part"}), stripe(x))
    for sx in (-2, 2):
        for z in (-2, -1, 0): R.put("RoundEdge", (sx, 2, z), find("RoundEdge", {"down": "full", inner_of(sx): "full", "forward": "part", "back": "part"}), "hull")
        R.put("RoundCorner", (sx, 2, 1), find("RoundCorner", {"down": "part", "back": "part", inner_of(sx): "part"}), "hull")
        R.put("RoundCorner", (sx, 2, -3), find("RoundCorner", {"down": "part", "forward": "part", inner_of(sx): "part"}), "hull")
    # level 3: roof plates and sensor dome between the two turrets
    for sx in (-1, 1):
        R.put("Plate1x2", (sx, 3, -2), ALONG_Z, "hull"); R.put("HalfBlock", (sx, 3, 0), paint="hull")
    R.put("Dome", (0, 3, -1), paint="accent")
    # fenders
    for sx in (-3, 3):
        for z in range(-3, 4): R.put("HalfBlock", (sx, 1, z), find("HalfBlock", {"up": "full", "left": "part", "right": "part", "forward": "part", "back": "part"}), "belly")
    for z in (-3, 0, 3):
        R.module("wheel", WHEEL_L, (3, 0, z), rot=I3, mount="left"); R.module("wheel", WHEEL_L, (-3, 0, z), rot=rot_y(2), mount="right")
    R.module("weapon", "weapons:R4W_Railgun", (0, 3, 0), mount="down"); R.module("weapon", "weapons:R4W_RocketLauncher", (0, 3, -2), mount="down")
    R.module("weapon", "weapons:R4W_Basic", (-1, 2, 2), mount="down"); R.module("weapon", "weapons:R4W_Basic", (1, 2, 2), mount="down")
    return R


# ------------------------------------------------------------------------------------------------ robot 2: connectors / spaced armour
def skiff():
    """R2 'Outrigger Skiff': narrow core hull, real PR #1186 STRUT outriggers carrying the wheels, spaced armour held off the hull on
    STRUT / ROD connectors; truss braces, rounded armour tops, octagonal weapon plinth."""
    R = Robot("R2_OutriggerSkiff", "R2 „Outrigger Skiff” — prawdziwe łączniki ROD / STRUT")
    stripe = lambda x: "accent" if x == 0 else "hull"
    for x in (-1, 0, 1):
        R.put("Beam3", (x, 0, -2), ALONG_Z, "belly"); R.put("Beam2", (x, 0, 1), ALONG_Z, "belly")
        R.put("Beam3", (x, 1, -2), ALONG_Z, stripe(x)); R.put("Beam2", (x, 1, 1), ALONG_Z, stripe(x))
        R.put("Wedge", (x, 0, 3), find("Wedge", {"up": "full", "back": "full", "left": "part", "right": "part"}), "belly")
        R.put("RoundEdge", (x, 1, 3), I3, stripe(x))
        R.put("Wedge", (x, 0, -3), find("Wedge", {"up": "full", "forward": "full", "left": "part", "right": "part"}), "belly")
        R.put("Wedge", (x, 1, -3), find("Wedge", {"down": "full", "forward": "full", "left": "part", "right": "part"}), stripe(x))
    for sx in (-1, 1):
        R.put("Plate1x2", (sx, 2, -2), ALONG_Z, "hull"); R.put("Plate1x2", (sx, 2, 1), ALONG_Z, "hull")
    R.put("HalfBlock", (0, 2, -2), paint="accent"); R.put("HalfBlock", (0, 2, 2), paint="accent")
    R.put("OctColumn", (0, 2, 0), paint="accent")
    for sx, ry in ((1, 1), (-1, 3)):
        inner = inner_of(sx)
        for z in (-2, 2):
            R.module("connector", "connectors:R4C_STRUT_STANDARD", (2*sx, 0, z), rot=rot_y(ry), extra_cells=[(3*sx, 0, z), (4*sx, 0, z)], mount=inner)
            R.module("wheel", WHEEL_M, (5*sx, 0, z), rot=rot_y(0 if sx > 0 else 2), mount=inner)
            R.put("RoundCorner", (4*sx, 1, z), find("RoundCorner", {"down": "part", "back" if z > 0 else "forward": "part", inner: "part"}), "hull")
        R.put("Beam3", (4*sx, 0, -1), ALONG_Z, "hull")
        for z in (-1, 0, 1): R.put("RoundEdge", (4*sx, 1, z), find("RoundEdge", {"down": "full", inner: "full", "forward": "part", "back": "part"}), "hull" if z else "accent")
        R.module("connector", "connectors:R4C_ROD_SHORT", (2*sx, 1, 0), rot=rot_y(ry), extra_cells=[(3*sx, 1, 0)], mount=inner)
        for z in (-1, 1): R.put("TrussCube", (2*sx, 1, z), paint="belly")
    R.module("weapon", "weapons:R4W_Heavy", (0, 3, 0), mount="down")
    R.module("weapon", "weapons:R4W_Shotgun", (0, 2, 1), mount="down")
    R.module("weapon", "weapons:R4W_Basic", (0, 2, -1), rot=rot_y(2), mount="down")
    return R


# ------------------------------------------------------------------------------------------------ robot 3: large hover carrier
def leviathan():
    """R3 'Leviathan': 200+ part hover carrier. Rounded chines and bow (RoundEdge / RoundCorner), slab + big-cube core, arched bulwark
    with truss railings, bridge tower from L / T pieces with an arch doorway and cove fillets, engine nacelles (Cylinder + Cone)."""
    R = Robot("R3_Leviathan", "R3 „Leviathan” — duży kadłub z klockami wielokomórkowymi, obłymi i wklęsłymi")
    ZI = range(-6, 7)                                              # interior stations
    RE = lambda want: find("RoundEdge", want)
    # ---- y = 0: belly
    for sx in (-3, 3):
        for z in ZI: R.put("RoundEdge", (sx, 0, z), RE({"up": "full", inner_of(sx): "full", "forward": "part", "back": "part"}), "belly")
        R.put("RoundCorner", (sx, 0, 7), find("RoundCorner", {"up": "part", "back": "part", inner_of(sx): "part"}), "belly")
        R.put("CornerWedge", (sx, 0, -7), find("CornerWedge", {"up": "full", "forward": "part", inner_of(sx): "part"}), "belly")
    for x0 in (-2, 1):
        for z0 in range(-6, 6, 2): R.put("Slab2x2", (x0, 0, z0), paint="belly")
        R.put("Beam2", (x0, 0, 6), paint="belly")
    for z0 in (-6, -3, 0, 3): R.put("Beam3", (0, 0, z0), ALONG_Z, "belly")
    R.put("Cube", (0, 0, 6), paint="belly")
    for x in range(-2, 3):
        R.put("RoundEdge", (x, 0, 7), RE({"up": "full", "back": "full", "left": "part", "right": "part"}), "belly")
        R.put("Wedge", (x, 0, -7), find("Wedge", {"up": "full", "forward": "full", "left": "part", "right": "part"}), "belly")
    # ---- y = 1 / 2: main hull
    for sx in (-3, 3):
        for z0 in (-6, -3, 0, 3): R.put("Beam3", (sx, 1, z0), ALONG_Z, "hull")
        R.put("Cube", (sx, 1, 6), paint="hull")
        R.put("RoundEdge", (sx, 1, 7), RE({"back": "full", inner_of(sx): "full", "up": "part", "down": "part"}), "hull")       # vertical axis: rounded bow corner
        R.put("RoundEdge", (sx, 1, -7), RE({"forward": "full", inner_of(sx): "full", "up": "part", "down": "part"}), "hull")
    for x0 in (-2, 1):
        for z0 in range(-6, 6, 2): R.put("BigCube2", (x0, 1, z0), paint="hull")
        R.put("Beam2", (x0, 1, 6), paint="hull"); R.put("Beam2", (x0, 2, 6), paint="hull")
    for y in (1, 2):
        for z0 in (-6, -3, 0, 3): R.put("Beam3", (0, y, z0), ALONG_Z, "accent")
        R.put("Cube", (0, y, 6), paint="accent")
    R.put("Cube", (-2, 1, 7), paint="hull"); R.put("Beam3", (-1, 1, 7), paint="hull"); R.put("Cube", (2, 1, 7), paint="hull")
    R.put("Beam3", (-1, 1, -7), paint="hull")
    CYL_Z = find("Cylinder", {"forward": "part", "back": "part"})
    for sx in (-2, 2):
        R.put("Cylinder", (sx, 1, -7), CYL_Z, "accent"); R.put("Cone", (sx, 1, -8), find("Cone", {"forward": "part"}), "belly")
    # ---- y = 2: bulwark with arched scuppers, rounded bow top, sloped stern
    ARCH_X = find("Arch", {"up": "full", "forward": "full", "back": "full", "left": "part", "right": "part"})
    for sx in (-3, 3):
        for z in range(-5, 6): R.put("Arch", (sx, 2, z), ARCH_X, "hull")
        for z in (-6, 6): R.put("OctColumn", (sx, 2, z), paint="accent")
        R.put("RoundCorner", (sx, 2, 7), find("RoundCorner", {"down": "part", "back": "part", inner_of(sx): "part"}), "hull")
        R.put("CornerWedge", (sx, 2, -7), find("CornerWedge", {"down": "full", "forward": "part", inner_of(sx): "part"}), "hull")
    for x in range(-2, 3): R.put("RoundEdge", (x, 2, 7), I3, "accent" if x == 0 else "hull")
    WW = ori("back", "up")
    R.put("WideWedge", (-1, 2, -7), WW, "hull"); R.put("WideWedge", (2, 2, -7), WW, "hull")
    R.put("Wedge", (0, 2, -7), find("Wedge", {"down": "full", "forward": "full", "left": "part", "right": "part"}), "accent")
    # ---- bridge tower (x -2..2, z -5..-2)
    R.put("CornerL3", (-2, 3, -5), I3, "hull"); R.put("CornerL3", (2, 3, -5), rot_y(3), "hull")
    R.put("CornerL3", (-2, 3, -2), rot_y(1), "hull"); R.put("CornerL3", (2, 3, -2), rot_y(2), "hull")
    R.put("Arch", (0, 3, -2), I3, "accent")                          # doorway onto the fore deck
    R.put("Cube", (0, 3, -5), paint="accent")
    R.put("Slab2x2", (-1, 3, -4), paint="hull"); R.put("Beam2", (1, 3, -4), ALONG_Z, "hull")
    R.put("Tee4", (0, 4, -4), I3, "accent")
    for sx in (-1, 1): R.put("RoundEdge", (sx, 4, -3), I3, "hull")
    for x in (-1, 0, 1):
        R.put("CoveEdge", (x, 4, -2), I3, "hull") if x else R.put("CoveEdge", (x, 4, -2), I3, "accent")
        R.put("CoveEdge", (x, 4, -5), find("CoveEdge", {"down": "full", "forward": "full", "left": "part", "right": "part"}), "hull")
    for sx in (-2, 2):
        for z in (-4, -3): R.put("CoveEdge", (sx, 4, z), find("CoveEdge", {"down": "full", inner_of(sx): "full", "forward": "part", "back": "part"}), "hull")
        R.put("OctColumn", (sx, 4, -2), paint="accent"); R.put("Pyramid", (sx, 5, -2), paint="hull")
        R.put("InnerCorner", (sx, 4, -5), find("InnerCorner", {"down": "full", "forward": "full", inner_of(sx): "full", "up": "part", "back": "part", outer_of(sx): "part"}), "hull")
    R.put("Dome", (0, 5, -4), paint="hull")
    for sx in (-1, 1):
        R.put("HexColumn", (sx, 5, -4), paint="accent"); R.put("Cone", (sx, 6, -4), paint="hull")
    # ---- fore deck (z -1..6)
    R.put("Step", (0, 3, -1), I3, "accent")
    RT_Z = find("RoundTop", {"down": "full", "forward": "part", "back": "part", "left": "part", "right": "part"}) if False else rot_y(1)
    for z in (0, 1, 2, 3, 4): R.put("RoundTop", (0, 3, z), RT_Z, "accent")
    R.put("HalfWedge", (0, 3, 5), I3, "accent")
    for sx in (-2, 2):
        R.put("LongWedge", (sx, 3, -1), I3, "hull")
        R.put("Plate2x2", (sx if sx < 0 else 1, 3, 3), paint="hull")
        R.put("Plate2x2", (sx if sx < 0 else 1, 3, 5), paint="hull")
    for sx in (-1, 1):
        R.put("HalfBlock", (sx, 3, -1), paint="hull"); R.put("Plate1x2", (sx, 3, 0), ALONG_Z, "hull")
    # ---- bulwark railing and posts
    for sx in (-3, 3):
        for z in (-5, -3, -1, 1, 3, 5): R.put("TrussCube", (sx, 3, z), paint="hull")
        R.put("Pyramid", (sx, 3, -6), paint="hull")
        R.put("Tetra", (sx, 3, 6), find("Tetra", {"down": "part", "back": "part", inner_of(sx): "part"}), "hull")
    # ---- real modules: hover engines and boosters under the belly, weapons on deck and bridge
    for sx in (-2, 2):
        for z in (-5, -2, 1, 4): R.module("movement", "movement:R4M_HoverEngine", (sx, -1, z), mount="up")
    for sx in (-1, 1): R.module("movement", "movement:R4M_Booster", (sx, -1, -6), mount="up")
    R.module("weapon", "weapons:R4W_Railgun", (0, 5, -3), mount="down")
    for sx in (-2, 2):
        R.module("weapon", "weapons:R4W_Heavy", (sx, 3, 2), mount="down"); R.module("weapon", "weapons:R4W_Basic", (sx, 3, 1), mount="down")
    R.module("weapon", "weapons:R4W_RocketLauncher", (-1, 3, 2), mount="down"); R.module("weapon", "weapons:R4W_RocketLauncher", (1, 3, 2), mount="down")
    return R

ROBOTS = {"R1_GlacisRover": rover, "R2_OutriggerSkiff": skiff, "R3_Leviathan": leviathan}
