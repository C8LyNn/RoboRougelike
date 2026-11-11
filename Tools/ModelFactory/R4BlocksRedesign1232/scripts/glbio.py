"""Minimal pure-numpy GLB (glTF 2.0 binary) reader/writer used by the #1232 pilot tooling (no Blender needed)."""
import json, struct
import numpy as np

_CT = {5120: np.int8, 5121: np.uint8, 5122: np.int16, 5123: np.uint16, 5125: np.uint32, 5126: np.float32}
_NC = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}


def read_json_bin(path):
    b = open(path, "rb").read()
    assert b[:4] == b"glTF", path
    l = struct.unpack("<I", b[12:16])[0]
    j = json.loads(b[20:20 + l]); o = 20 + l
    bin_ = b""
    if o < len(b):
        l2 = struct.unpack("<I", b[o:o + 4])[0]; bin_ = b[o + 8:o + 8 + l2]
    return j, bin_


def accessor(j, bin_, i):
    a = j["accessors"][i]; bv = j["bufferViews"][a["bufferView"]]
    dt = np.dtype(_CT[a["componentType"]]); nc = _NC[a["type"]]
    off = bv.get("byteOffset", 0) + a.get("byteOffset", 0); stride = bv.get("byteStride", 0)
    if stride and stride != dt.itemsize * nc:
        raw = np.frombuffer(bin_, np.uint8, count=stride * (a["count"] - 1) + dt.itemsize * nc, offset=off)
        idx = (np.arange(a["count"])[:, None] * stride + np.arange(dt.itemsize * nc)[None, :]).ravel()
        arr = np.frombuffer(raw[idx].tobytes(), dt)
    else:
        arr = np.frombuffer(bin_, dt, count=a["count"] * nc, offset=off)
    arr = arr.reshape(a["count"], nc) if nc > 1 else arr
    if a.get("normalized"):
        arr = arr.astype(np.float32) / np.iinfo(dt).max
    return arr


def node_matrix(n):
    if "matrix" in n: return np.array(n["matrix"], float).reshape(4, 4).T
    m = np.eye(4)
    t = n.get("translation", [0, 0, 0]); r = n.get("rotation", [0, 0, 0, 1]); s = n.get("scale", [1, 1, 1])
    x, y, z, w = r
    R = np.array([[1 - 2*(y*y + z*z), 2*(x*y - z*w), 2*(x*z + y*w)], [2*(x*y + z*w), 1 - 2*(x*x + z*z), 2*(y*z - x*w)], [2*(x*z - y*w), 2*(y*z + x*w), 1 - 2*(x*x + y*y)]])
    m[:3, :3] = R * np.array(s)[None, :]; m[:3, 3] = t
    return m


class Model:
    """rest-pose model: prims = [dict(node, pos(world,float64 Nx3), nrm, idx(Mx3), material(name), mat(dict))], empties = {name: (matrix4, extras)}"""
    def __init__(self): self.prims = []; self.empties = {}; self.nodes = {}; self.json = None

    def all_pos(self): return np.concatenate([p["pos"] for p in self.prims]) if self.prims else np.zeros((0, 3))
    def tris(self):
        return np.concatenate([p["pos"][p["idx"]] for p in self.prims]) if self.prims else np.zeros((0, 3, 3))
    def tri_count(self): return int(sum(len(p["idx"]) for p in self.prims))
    def bounds(self):
        P = self.all_pos(); return P.min(0), P.max(0)


def load(path, skip_name=lambda n: False):
    j, bin_ = read_json_bin(path); M = Model(); M.json = j
    nodes = j.get("nodes", [])
    def walk(i, parent):
        n = nodes[i]; m = parent @ node_matrix(n); name = n.get("name", f"node{i}")
        M.nodes[name] = m
        if "mesh" in n and not skip_name(name):
            if "skin" in n: m = np.eye(4) if False else m
            for pr in j["meshes"][n["mesh"]]["primitives"]:
                if pr.get("mode", 4) != 4: continue
                pos = accessor(j, bin_, pr["attributes"]["POSITION"]).astype(np.float64)
                posw = pos @ m[:3, :3].T + m[:3, 3]
                nrm = None
                if "NORMAL" in pr["attributes"]:
                    nrm = accessor(j, bin_, pr["attributes"]["NORMAL"]).astype(np.float64) @ np.linalg.inv(m[:3, :3])
                    nrm /= np.maximum(np.linalg.norm(nrm, axis=1, keepdims=True), 1e-12)
                idx = accessor(j, bin_, pr["indices"]).astype(np.int64).reshape(-1, 3) if "indices" in pr else np.arange(len(pos)).reshape(-1, 3)
                mat = j["materials"][pr["material"]] if "material" in pr else {"name": "default"}
                M.prims.append(dict(node=name, pos=posw, nrm=nrm, idx=idx, material=mat.get("name", "?"), mat=mat))
        elif "mesh" not in n:
            M.empties[name] = (m, n.get("extras", {}))
        for c in n.get("children", []): walk(c, m)
    sc = j["scenes"][j.get("scene", 0)]
    for r in sc["nodes"]: walk(r, np.eye(4))
    return M


# ------------------------------------------------------------------ writer
class Writer:
    def __init__(self, generator="RoboRougelike #1232 pilot (pure-python)"):
        self.j = {"asset": {"version": "2.0", "generator": generator}, "scene": 0, "scenes": [{"nodes": []}], "nodes": [], "meshes": [],
                  "materials": [], "accessors": [], "bufferViews": [], "buffers": [{}]}
        self.bin = bytearray(); self._mat = {}

    def _view(self, arr, target=None):
        while len(self.bin) % 4: self.bin.append(0)
        off = len(self.bin); self.bin += arr.tobytes()
        v = {"buffer": 0, "byteOffset": off, "byteLength": arr.nbytes}
        if target: v["target"] = target
        self.j["bufferViews"].append(v); return len(self.j["bufferViews"]) - 1

    def _acc(self, arr, typ, target=None, minmax=False):
        arr = np.ascontiguousarray(arr); ct = {np.dtype(np.float32): 5126, np.dtype(np.uint16): 5123, np.dtype(np.uint32): 5125}[arr.dtype]
        a = {"bufferView": self._view(arr, target), "componentType": ct, "count": int(len(arr) if typ != "SCALAR" else arr.size), "type": typ}
        if minmax: a["min"] = [float(x) for x in arr.min(0)]; a["max"] = [float(x) for x in arr.max(0)]
        self.j["accessors"].append(a); return len(self.j["accessors"]) - 1

    def material(self, name, rgb, metal, rough, emit=None, estr=1.0, extras=None):
        if name in self._mat: return self._mat[name]
        m = {"name": name, "pbrMetallicRoughness": {"baseColorFactor": [float(rgb[0]), float(rgb[1]), float(rgb[2]), 1.0], "metallicFactor": float(metal), "roughnessFactor": float(rough)}}
        if emit is not None:
            m["emissiveFactor"] = [float(min(1.0, c)) for c in emit]
            if estr != 1.0:
                m["extensions"] = {"KHR_materials_emissive_strength": {"emissiveStrength": float(estr)}}
                self.j.setdefault("extensionsUsed", [])
                if "KHR_materials_emissive_strength" not in self.j["extensionsUsed"]: self.j["extensionsUsed"].append("KHR_materials_emissive_strength")
        if extras: m["extras"] = extras
        self.j["materials"].append(m); self._mat[name] = len(self.j["materials"]) - 1; return self._mat[name]

    def mesh(self, name, prims):
        """prims: list of dict(pos, nrm, uv(optional), idx, material(index))"""
        ps = []
        for p in prims:
            at = {"POSITION": self._acc(p["pos"].astype(np.float32), "VEC3", 34962, True), "NORMAL": self._acc(p["nrm"].astype(np.float32), "VEC3", 34962)}
            if p.get("uv") is not None: at["TEXCOORD_0"] = self._acc(p["uv"].astype(np.float32), "VEC2", 34962)
            idx = np.asarray(p["idx"]).ravel(); idx = idx.astype(np.uint16 if len(p["pos"]) < 65535 else np.uint32)
            ps.append({"attributes": at, "indices": self._acc(idx, "SCALAR", 34963), "material": p["material"], "mode": 4})
        self.j["meshes"].append({"name": name, "primitives": ps}); return len(self.j["meshes"]) - 1

    def node(self, name, mesh=None, translation=None, rotation=None, scale=None, parent=None, extras=None, matrix=None):
        n = {"name": name}
        if mesh is not None: n["mesh"] = mesh
        if translation is not None: n["translation"] = [float(x) for x in translation]
        if rotation is not None: n["rotation"] = [float(x) for x in rotation]
        if scale is not None: n["scale"] = [float(x) for x in scale]
        if matrix is not None: n["matrix"] = [float(x) for x in np.asarray(matrix).T.ravel()]
        if extras: n["extras"] = extras
        self.j["nodes"].append(n); i = len(self.j["nodes"]) - 1
        if parent is None: self.j["scenes"][0]["nodes"].append(i)
        else: self.j["nodes"][parent].setdefault("children", []).append(i)
        return i

    def save(self, path):
        while len(self.bin) % 4: self.bin.append(0)
        self.j["buffers"][0]["byteLength"] = len(self.bin)
        js = json.dumps(self.j, separators=(",", ":")).encode()
        while len(js) % 4: js += b" "
        with open(path, "wb") as f:
            f.write(b"glTF" + struct.pack("<II", 2, 12 + 8 + len(js) + 8 + len(self.bin)))
            f.write(struct.pack("<I", len(js)) + b"JSON" + js)
            f.write(struct.pack("<I", len(self.bin)) + b"BIN\x00" + bytes(self.bin))
