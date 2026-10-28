#!/usr/bin/env python3
"""MAPSAFE actual Codex-authored LD02 fixtures: real backup/restore/undo."""
import argparse
import json
from pathlib import Path
import tempfile
import mapsafe as m

def run(fixtures):
    result=[]
    with tempfile.TemporaryDirectory(prefix="mapsafe-ld02-") as temp:
        root=Path(temp); maps=root/"Maps"; vault=root/"Vault"
        maps.mkdir();vault.mkdir()
        for stem,name,n in (("LD02_ARENA","Arena Smoke",168),("LD02_COVER_CHOICE","Cover Smoke",173)):
            original=(fixtures/(stem+".json")).read_bytes()
            loaded=json.loads(original)
            assert loaded["version"]==2 and len(loaded["items"])==n
            assert m.check_map(original)==2
            target=maps/(name+".json");target.write_bytes(original)
            snap=m.backup(maps,vault,name)
            assert m.verify(vault,snap["id"])[1]==original
            target.unlink()
            assert not m.restore(maps,vault,name,snap["id"])["replaced"]
            assert target.read_bytes()==original
            changed=json.dumps({"version":2,"items":[{"kind":0,"x":4,"z":3,"rotation":0}]}).encode()
            target.write_bytes(changed)
            receipt=m.restore(maps,vault,name,snap["id"],True,m.digest(changed))
            assert target.read_bytes()==original
            assert m.verify(vault,receipt["undo_snapshot_id"])[1]==changed
            assert not list(maps.glob("*.tmp"))
            result.append({"map":stem,"items":n,"sha256":m.digest(original),
                           "backup":"PASS","verify":"PASS","no_clobber_restore":"PASS",
                           "explicit_sha_replace":"PASS","undo_copy":"PASS"})
    return {"nativeUnity":"NOT_RUN","sourceMaps":2,"results":result}

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--fixtures",type=Path,required=True)
    p.add_argument("--output",type=Path,required=True)
    a=p.parse_args()
    v=run(a.fixtures)
    a.output.write_text(json.dumps(v,indent=2)+"\n",encoding="utf-8")
    print("MAPSAFE_ORIGINAL_LD02_PASS="+json.dumps(v))
