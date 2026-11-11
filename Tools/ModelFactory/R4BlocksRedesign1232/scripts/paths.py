"""Locations. R4_REF must contain shallow checkouts of the approved art branches:
   blocks (art/r4-building-blocks @03af520), weapons (art/938-r4-weapon-models @23718ab), movement (art/940-r4-movement-models @c6b655d),
   wheels (art/r4-wheel-variants @fb6910c), connectors (art/connector-triforce-1182 @22974e7)."""
import os
HERE = os.path.dirname(os.path.abspath(__file__))
PKG = os.path.dirname(HERE)
REF = os.environ.get("R4_REF", "/home/claude/refs")
LIB = {"blocks": "Tools/ModelFactory/R4Blocks", "weapons": "Tools/ModelFactory/R4Weapons938", "movement": "Tools/ModelFactory/R4Movement940",
       "wheels": "Tools/ModelFactory/R4Wheels", "connectors": "Tools/ModelFactory/R4ConnectorRods1182"}
REF_SHA = {"blocks": "03af5207143da76faeeb8004543a51f26c7f9c0d", "weapons": "23718ab9f3ecb579d224b928a25f713ddfdd8319", "movement": "c6b655d67c90ef91d7799460a0c0101756c4b975",
           "wheels": "fb6910c29acd07989f1ad2a36b8ec5709ebfc9ce", "connectors": "22974e7d92aef00ff7b9fb470ab73e261ff0b507"}
def ref_glb(lib, name, suffix=""): return os.path.join(REF, lib, LIB[lib], name, f"{name}{suffix}.glb")
def ref_file(lib, *p): return os.path.join(REF, lib, LIB[lib], *p)
def new_dir(style_label, kind): return os.path.join(PKG, style_label, f"R4B_{kind}")
