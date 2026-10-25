#!/usr/bin/env python3
"""Executable contract shape QA, NOT Unity code and NOT ownership policy implementation."""
import json,re,sys
from pathlib import Path
root=Path(__file__).parent
schema=json.loads((root/"definition.schema.json").read_text())
data=json.loads((root/"examples.json").read_text())
cases=json.loads((root/"design_cases.json").read_text())
defined=set()
def check(e):
    required=set(schema["required"])
    assert required<=set(e)
    assert set(e)<=set(schema["properties"])
    assert re.fullmatch(r"LEG-(WPN|MOV|DEF|SUP|DEP|ECO)-\d{3}",e["legendaryDefinitionId"])
    assert e["legendaryDefinitionId"] not in defined
    defined.add(e["legendaryDefinitionId"])
    assert e["maxEquippedSameDefinitionPerVehicle"]==1
    assert e["rarityTier"]=="Legendary"
    assert all(isinstance(e["mountFootprintCells"][k],int) and e["mountFootprintCells"][k]>0 for k in ("x","y","z"))
    assert e["massKg"]>0 and e["hitPoints"]>0
    assert len(e["tradeoffs"])>=2 and len(set(e["tradeoffs"]))==len(e["tradeoffs"])
    assert e["ability"]["cooldownSeconds"]>=0
    assert e["socketCategory"] in ["WeaponBase","Structural","Axle","MobilityHub"]
    assert not set(e["optionalFutureSystems"])-{"heat","energy","EMP"}
for e in data["definitions"]:check(e)
assert len(defined)==2
assert len(cases)>=15 and len({c["id"] for c in cases})==len(cases)
assert any(c["id"]=="equip_second_same_type_on_same_vehicle" and not c["expected_allowed"] for c in cases)
assert any(c["id"]=="inventory_two_owned_copies" and c["expected_allowed"] for c in cases)
assert any(c["id"]=="equip_legendary_weapon_and_distinct_drive" and c["expected_allowed"] for c in cases)
assert data["semantics"]["worldwideLegendaryLimit"] is None
assert data["semantics"]["zeroHpMountedCountsAsEquipped"]
assert data["semantics"]["inventoryDuplicatesAllowed"]
print("LEGENDARY_CORE_DESIGN_CONTRACT_QA_PASS definitions=%d cases=%d data_only=true"%(len(defined),len(cases)))
