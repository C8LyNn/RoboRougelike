"""Locations for the #1235 rollout. The #1232 pilot library (sibling directory) is imported, never modified."""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
PKG = os.path.dirname(HERE)
PILOT = os.path.join(os.path.dirname(PKG), "R4BlocksRedesign1232")
sys.path.insert(0, os.path.join(PILOT, "scripts"))
import paths as P1                      # reference checkouts (R4_REF) + pinned commits of the approved art branches
REF, REF_SHA, ref_glb, ref_file = P1.REF, P1.REF_SHA, P1.ref_glb, P1.ref_file
PILOT_COMMIT = "5f4470d5cb148bcae358f6d09ec0f20e02590946"
LABEL = "A_Sleek"
def new_dir(kind): return os.path.join(PKG, LABEL, f"R4B_{kind}")
def pilot_dir(kind): return os.path.join(PILOT, LABEL, f"R4B_{kind}")
