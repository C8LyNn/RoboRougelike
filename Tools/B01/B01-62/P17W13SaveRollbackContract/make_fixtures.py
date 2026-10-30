#!/usr/bin/env python3
"""Create QA only validated JSON evidence files for the original-W06 CLR roundtrip."""
from pathlib import Path
import sys
import save_contract as c
if len(sys.argv)!=2:raise SystemExit("Usage: python make_fixtures.py <new-dir>")
out=Path(sys.argv[1])
if out.exists():raise SystemExit("REFUSE_EXISTING_FIXTURE_DIR")
out.mkdir(parents=True)
a=c.fresh();d=c.sample_detached()
both=c.sample_detached()
both["parts"][1].update({"detached":True,"consumed":True,"parent_guid":None,"kinematic":False})
for name,s in (("attached",a),("detached",d),("both",both)):
    data=c.encode(s)
    if c.decode(data)!=s:raise RuntimeError("BROKEN_REFERENCE_ROUNDTRIP")
    (out/(name+".json")).write_bytes(data)
print("QA_CONTRACT_FIXTURES=3 VALIDATED_ONLY_NOT_GAME_SAVE")
