"""Robot layouts for the #1232 before/after comparison. Coordinates = file/glTF axes of the approved models:
x = Port_right(+)/Port_left(-), y = up, z = Port_forward(+)/Port_back(-). One cell = 0.55 m."""
import itertools, json, os
import numpy as np
import glbio

G = 0.55
DIR = {"right": (1, 0, 0), "left": (-1, 0, 0), "up": (0, 1, 0), "down": (0, -1, 0), "forward": (0, 0, 1), "back": (0, 0, -1)}
NAME = {v: k for k, v in DIR.items()}
OPP = {"right": "left", "left": "right", "up": "down", "down": "up", "forward": "back", "back": "forward"}

def rotations():
    out = []
    for perm in itertools.permutations(range(3)):
        for signs in itertools.product((1, -1), repeat=3):
            M = np.zeros((3, 3), int)
            for r in range(3): M[r, perm[r]] = signs[r]
            if round(np.linalg.det(M)) == 1: out.append(M)
    out.sort(key=lambda M: -int(np.trace(M)))       # identity first, then least twist
    return out
ROTS = rotations()
def rot_y(k): return [np.eye(3, dtype=int), np.array([[0, 0, 1], [0, 1, 0], [-1, 0, 0]]), np.array([[-1, 0, 0], [0, 1, 0], [0, 0, -1]]), np.array([[0, 0, -1], [0, 1, 0], [1, 0, 0]])][k % 4]

# canonical port signature of the five pilot blocks (identical in PR #963 and in the redesign) - file axes
SIG = {"Cube": {d: "full" for d in DIR},
       "Wedge": {"down": "full", "back": "full", "left": "part", "right": "part"},
       "Tetra": {"down": "part", "back": "part", "left": "part"},
       "CornerWedge": {"down": "full", "back": "part", "left": "part"},
       "HalfBlock": {"down": "full", "right": "part", "left": "part", "forward": "part", "back": "part"}}

def rsig(kind, M):
    return {NAME[tuple(int(x) for x in M @ np.array(DIR[d]))]: k for d, k in SIG[kind].items()}

def find_rot(kind, want):
    for M in ROTS:
        s = rsig(kind, M)
        if all(s.get(d) == k for d, k in want.items()) and (len(want) == len(s) or kind in ("Cube",) or all(d in s for d in want)):
            if kind in ("Wedge", "Tetra", "CornerWedge") and set(want) != set(d for d in s if kind != "Wedge" or s[d] == "full"): continue
            return M
    raise ValueError((kind, want))

class Robot:
    def __init__(self, name): self.name = name; self.parts = []; self.cells = {}
    def block(self, kind, cell, want=None, paint="hull"):
        M = np.eye(3, dtype=int) if want is None else find_rot(kind, want)
        cell = tuple(int(c) for c in cell)
        assert cell not in self.cells, (cell, self.cells.get(cell), kind)
        self.cells[cell] = len(self.parts)
        self.parts.append(dict(type="block", kind=kind, cell=cell, rot=M, paint=paint)); return self
    def module(self, cls, model, cell, rot=None, extra_cells=(), mount=None):
        """cls: weapon | wheel | connector ; mount = (neighbour cell direction the module is bolted to)"""
        cell = tuple(cell); M = np.eye(3, dtype=int) if rot is None else np.array(rot)
        idx = len(self.parts)
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

# ------------------------------------------------------------------------------------------------ robot 1: rover
def rover():
    """R1 'Glacis Rover': 6-wheel assault rover, 140+ hull blocks built only from the five pilot shapes."""
    R = Robot("R1_GlacisRover")
    X = range(-2, 3); ZB = range(-3, 4)
    stripe = lambda x: "accent" if x == 0 else "hull"
    # level 0: belly, prow underside, stern underside
    for x in X:
        for z in ZB: R.block("Cube", (x, 0, z), paint="belly")
    for x in (-1, 0, 1):
        R.block("Wedge", (x, 0, 4), {"up": "full", "back": "full"}, paint="belly")
        R.block("Wedge", (x, 0, -4), {"up": "full", "forward": "full"}, paint="belly")
    for sx, inner in ((-2, "right"), (2, "left")):
        R.block("CornerWedge", (sx, 0, 4), {"up": "full", "back": "part", inner: "part"}, paint="belly")
        R.block("CornerWedge", (sx, 0, -4), {"up": "full", "forward": "part", inner: "part"}, paint="belly")
    # level 1: main hull, upper glacis, stern slope
    for x in X:
        for z in ZB: R.block("Cube", (x, 1, z), paint=stripe(x))
    for x in (-1, 0, 1):
        R.block("Wedge", (x, 1, 4), {"down": "full", "back": "full"}, paint=stripe(x))
        R.block("Wedge", (x, 1, -4), {"down": "full", "forward": "full"}, paint=stripe(x))
    for sx, inner in ((-2, "right"), (2, "left")):
        R.block("CornerWedge", (sx, 1, 4), {"down": "full", "back": "part", inner: "part"}, paint="hull")
        R.block("CornerWedge", (sx, 1, -4), {"down": "full", "forward": "part", inner: "part"}, paint="hull")
    # level 2: casemate x -1..1, z -2..0 with a sloped armour skirt all round
    for x in (-1, 0, 1):
        for z in (-2, -1, 0): R.block("Cube", (x, 2, z), paint=stripe(x))
        R.block("Wedge", (x, 2, 1), {"down": "full", "back": "full"}, paint=stripe(x))
        R.block("Wedge", (x, 2, -3), {"down": "full", "forward": "full"}, paint=stripe(x))
    for sx, inner in ((-2, "right"), (2, "left")):
        for z in (-2, -1, 0): R.block("Wedge", (sx, 2, z), {"down": "full", inner: "full"}, paint="hull")
        R.block("Tetra", (sx, 2, 1), {"down": "part", "back": "part", inner: "part"}, paint="hull")
        R.block("Tetra", (sx, 2, -3), {"down": "part", "forward": "part", inner: "part"}, paint="hull")
    # level 3: roof armour plates (half blocks) around the turret ring
    for x in (-1, 1):
        for z in (-2, -1, 0): R.block("HalfBlock", (x, 3, z), {"down": "full"}, paint="hull")
    R.block("HalfBlock", (0, 3, -1), {"down": "full"}, paint="accent")
    # fenders over the wheels (half blocks on the hull side, upper half of the cell)
    for sx in (-3, 3):
        for z in range(-3, 4): R.block("HalfBlock", (sx, 1, z), {"up": "full"}, paint="belly")
    # real modules (unchanged models from PR #1018 / #939)
    WH = "wheels:R4M_WheelL_Side_Offroad"
    for z in (-3, 0, 3):
        R.module("wheel", WH, (3, 0, z), rot=np.eye(3, dtype=int), mount="left")
        R.module("wheel", WH, (-3, 0, z), rot=rot_y(2), mount="right")
    R.module("weapon", "weapons:R4W_Railgun", (0, 3, 0), mount="down")
    R.module("weapon", "weapons:R4W_RocketLauncher", (0, 3, -2), mount="down")
    R.module("weapon", "weapons:R4W_Basic", (-1, 2, 2), mount="down")
    R.module("weapon", "weapons:R4W_Basic", (1, 2, 2), mount="down")
    return R


# ------------------------------------------------------------------------------------------------ robot 2: connectors / spaced armour
def skiff():
    """R2 'Outrigger Skiff': narrow core hull, STRUT outriggers carrying the wheels, spaced armour walls held 2 cells off the
    hull on real PR #1186 STRUT / ROD connectors."""
    R = Robot("R2_OutriggerSkiff")
    stripe = lambda x: "accent" if x == 0 else "hull"
    # core hull 3 x 2 x 5 with wedge nose and tail
    for x in (-1, 0, 1):
        for z in range(-2, 3):
            R.block("Cube", (x, 0, z), paint="belly"); R.block("Cube", (x, 1, z), paint=stripe(x))
        R.block("Wedge", (x, 0, 3), {"up": "full", "back": "full"}, paint="belly")
        R.block("Wedge", (x, 1, 3), {"down": "full", "back": "full"}, paint=stripe(x))
        R.block("Wedge", (x, 0, -3), {"up": "full", "forward": "full"}, paint="belly")
        R.block("Wedge", (x, 1, -3), {"down": "full", "forward": "full"}, paint=stripe(x))
    # roof: half-block armour and a raised weapon plinth
    for z in (-2, -1, 1, 2):
        for x in (-1, 1): R.block("HalfBlock", (x, 2, z), {"down": "full"}, paint="hull")
    R.block("HalfBlock", (0, 2, -2), {"down": "full"}, paint="accent")
    R.block("HalfBlock", (0, 2, 2), {"down": "full"}, paint="accent")
    R.block("Cube", (0, 2, 0), paint="accent")
    for sx, inner, outer, ry in ((1, "left", "right", 1), (-1, "right", "left", 3)):
        # outriggers: STRUT_STANDARD from the hull side (node at x = +-2) to the outer node (x = +-4), front and rear
        for z in (-2, 2):
            R.module("connector", "connectors:R4C_STRUT_STANDARD", (2*sx, 0, z), rot=rot_y(ry), extra_cells=[(3*sx, 0, z), (4*sx, 0, z)], mount=inner)
            R.module("wheel", "wheels:R4M_WheelM_Side_Offroad", (5*sx, 0, z), rot=rot_y(0 if sx > 0 else 2), mount=inner)
            R.block("Tetra", (4*sx, 1, z), {"down": "part", "back" if z > 0 else "forward": "part", inner: "part"}, paint="hull")
        # spaced armour wall between the outer nodes, 2 cells off the hull
        for z in (-1, 0, 1):
            R.block("Cube", (4*sx, 0, z), paint="hull" if z else "accent")
            R.block("Wedge", (4*sx, 1, z), {"down": "full", inner: "full"}, paint="hull" if z else "accent")
        # ROD_SHORT braces from the hull side to the armour wall (upper row), ROD_STANDARD tie under the wall is not needed
        R.module("connector", "connectors:R4C_ROD_SHORT", (2*sx, 1, 0), rot=rot_y(ry), extra_cells=[(3*sx, 1, 0)], mount=inner)
        for z in (-1, 1):
            R.block("HalfBlock", (2*sx, 1, z), {inner: "full"}, paint="belly")
    R.module("weapon", "weapons:R4W_Heavy", (0, 3, 0), mount="down")
    R.module("weapon", "weapons:R4W_Shotgun", (0, 2, 1), mount="down")
    R.module("weapon", "weapons:R4W_Basic", (0, 2, -1), rot=rot_y(2), mount="down")      # tail gun, faces aft
    return R

ROBOTS = {"R1_GlacisRover": rover, "R2_OutriggerSkiff": skiff}
