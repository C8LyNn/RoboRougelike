"""locations for the module library"""
import os, sys
import mkit as K
PKG = K.PKG; MOD = os.path.join(PKG, "modules"); TRAP = os.path.join(PKG, "world_traps")
def mdir(ID): return os.path.join(TRAP if ID.startswith("R4D_") else MOD, ID)
def glb(ID, lod=False): return os.path.join(mdir(ID), ID + ("_LOD1" if lod else "") + ".glb")
def ids():
    import modules as MD
    return [("R4D_" if d["group"] == "T" else "R4X_") + n for n, d in MD.ALL.items()]
def meta(ID):
    import modules as MD
    return MD.ALL[ID[4:]]
SLEEK = os.path.join(os.path.dirname(PKG), "R4BlocksSleek1235"); PILOT = K.paths25.PILOT
def cube_glb(): return os.path.join(PILOT, "A_Sleek", "R4B_Cube", "R4B_Cube.glb")
