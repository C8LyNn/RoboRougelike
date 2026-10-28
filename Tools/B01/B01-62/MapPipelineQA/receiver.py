#!/usr/bin/env python3
"""B01-62 MAPPIPE-RECEIVER-001: execute original owners' CLIs against real SLALOM."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

SOURCE_NAMES = ("mapsafe.py", "mapdiff.py", "arena.json")
LABEL = "Slalom Verified"

class ReceiverError(Exception):
    pass

def require(ok, why):
    if not ok:
        raise ReceiverError(why)

def digest(b):
    return hashlib.sha256(b).hexdigest()

def cli(script, *args, exit_code=0):
    p = subprocess.run([sys.executable, str(script), *[str(a) for a in args]],
                       text=True, capture_output=True, timeout=45)
    require(p.returncode == exit_code,
            f"{script.name} exit {p.returncode}, expected {exit_code}: {p.stderr[-1200:]}")
    try:
        response = json.loads((p.stdout if not exit_code else p.stderr).strip())
    except ValueError as err:
        raise ReceiverError(f"{script.name} invalid CLI response: {err}")
    key = "ok" if script.name == "mapsafe.py" else "status"
    expected_value = (exit_code == 0) if key == "ok" else ("PASS" if exit_code == 0 else "REJECTED")
    require(response.get(key) == expected_value, f"{script.name} wrong result contract")
    return response

def load_sources(folder):
    root = Path(folder).resolve()
    paths = {n: root/n for n in SOURCE_NAMES}
    for name, p in paths.items():
        require(p.is_file() and not p.is_symlink(), "Missing source: "+name)
    initial = {k: digest(p.read_bytes()) for k, p in paths.items()}
    source = paths["arena.json"].read_bytes()
    doc = json.loads(source)
    require(doc.get("version") == 2 and len(doc["items"]) == 360,
            "Original SLALOM schema/count mismatch")
    barriers = [i for i in doc["items"] if i["kind"] == 8]
    require(len(barriers) == 4 and (barriers[0]["x"],barriers[0]["z"]) == (6,6),
            "Original barrier was changed")
    require(sum(i["kind"] == 0 for i in doc["items"]) == 252,
            "Original ground count changed")
    return paths, source, doc, initial

def controlled_change(source):
    doc = json.loads(source)
    candidates = [i for i,v in enumerate(doc["items"])
                  if v["kind"] == 8 and (v["x"],v["z"]) == (6,6)]
    require(len(candidates) == 1, "Barrier mutation is ambiguous")
    removed = doc["items"].pop(candidates[0])
    require(removed["scaleX"] == 1 and removed["scaleZ"] == 1,
            "Source barrier changed footprint")
    return (json.dumps(doc, ensure_ascii=False, sort_keys=True, indent=2)+"\n").encode()

def audit(source_dir, output_dir):
    src, original, parsed, hashes = load_sources(source_dir)
    out = Path(output_dir).resolve()
    require(out.is_dir() and out not in [p.parent for p in src.values()],
            "Output must be a separate existing directory")
    require(all(not (out/n).exists() for n in
                ("receipt.json","controlled-change.svg")), "Refusing to overwrite outputs")
    with tempfile.TemporaryDirectory(prefix="b01-62-mappipe-") as scratch:
        root = Path(scratch)
        maps, vault, delta, restored = (root/n for n in ("Maps","Vault","Delta","Identity"))
        for folder in (maps, vault, delta, restored):
            folder.mkdir()
        target = maps/(LABEL+".json")
        target.write_bytes(original)

        snapshot = cli(src["mapsafe.py"], "backup", "--maps-dir", maps,
                       "--vault-dir", vault, "--name", LABEL)
        sid = snapshot["id"]
        require(snapshot["sha256"] == digest(original), "Backup hash mismatch")
        verified = cli(src["mapsafe.py"], "verify", "--vault-dir", vault,
                       "--backup-id", sid)
        require(verified["valid"] and verified["sha256"] == digest(original),
                "Backup verification failed")
        changed = controlled_change(original)
        target.write_bytes(changed)
        altered_sha = digest(changed)

        cli(src["mapsafe.py"], "restore", "--maps-dir", maps, "--vault-dir", vault,
            "--name", LABEL, "--backup-id", sid, exit_code=2)
        require(target.read_bytes() == changed, "Unconfirmed restore clobbered map")
        cli(src["mapsafe.py"], "restore", "--maps-dir", maps, "--vault-dir", vault,
            "--name", LABEL, "--backup-id", sid, "--replace",
            "--expect-sha256", "0"*64, exit_code=2)
        require(target.read_bytes() == changed and len(list(vault.iterdir())) == 1,
                "Wrong-hash restore mutated target/vault")

        delta_cli = cli(src["mapdiff.py"], "--before", src["arena.json"],
                        "--after", target, "--out-dir", delta)
        details = json.loads((delta/"mapdiff.json").read_bytes())
        require(delta_cli["added"] == 0 and delta_cli["removed"] == 1 and
                delta_cli["changedCells"] == 1, "One-barrier diff not detected")
        require(len(details["removedItems"]) == 1 and
                tuple(details["removedItems"][0][k] for k in ("kind","x","z")) == (8,6,6),
                "Removed item mismatch")
        require(len(details["changedCells"]) == 1 and
                tuple(details["changedCells"][0][k] for k in ("x","z")) == (6,6),
                "Changed footprint mismatch")
        require(details["before"]["sha256"] == digest(original) and
                details["after"]["sha256"] == altered_sha, "Diff input hashes mismatched")
        svg = (delta/"mapdiff.svg").read_bytes()
        require(ET.fromstring(svg).tag == "{http://www.w3.org/2000/svg}svg",
                "Invalid generated SVG")

        rep = cli(src["mapsafe.py"], "restore", "--maps-dir", maps, "--vault-dir", vault,
                  "--name", LABEL, "--backup-id", sid, "--replace",
                  "--expect-sha256", altered_sha)
        require(rep["replaced"] and target.read_bytes() == original,
                "Restored bytes differ from original")
        uid = rep["undo_snapshot_id"]
        require(uid and uid != sid, "Distinct undo backup missing")
        undo = cli(src["mapsafe.py"], "verify", "--vault-dir", vault,
                   "--backup-id", uid)
        require(undo["sha256"] == altered_sha and
                (vault/uid/"payload.json").read_bytes() == changed,
                "Pre-restore bytes not preserved in undo")
        ident = cli(src["mapdiff.py"], "--before", src["arena.json"],
                    "--after", target, "--out-dir", restored)
        require(ident["noChanges"] and ident["changedCells"] == 0,
                "Restored map differs semantically")
        require(not list(maps.glob("*.tmp")) and not list(vault.glob("*.stage")),
                "Orphan temporary files")
        receipt = {
          "schema":"B01-62-MAPPIPE-RECEIVER-001/v1", "status":"PASS",
          "input":{"items":360,"groundItems":252,"beforeSHA256":digest(original),
                   "mutatedSHA256":altered_sha,"removed":{"kind":8,"x":6,"z":6}},
          "sourceSHA256":hashes,
          "results":{"backup":"PASS","verify":"PASS","unconfirmedRestore":"EXPECTED_REJECTION",
                     "staleHashRestore":"EXPECTED_REJECTION","mapdiff":"PASS",
                     "changedFootprintCells":1,"generatedSvgSHA256":digest(svg),
                     "explicitRestore":"PASS","undoBytePreservation":"PASS",
                     "restoredByteIdentity":"PASS","restoredSemanticIdentity":"PASS",
                     "orphanTempFiles":0},
          "unityNative":"NOT_RUN", "codexIntegrated":False}
        (out/"receipt.json").write_bytes(
            (json.dumps(receipt,sort_keys=True,indent=2)+"\n").encode("utf-8"))
        (out/"controlled-change.svg").write_bytes(svg)

    require({k:digest(p.read_bytes()) for k,p in src.items()} == hashes,
            "Original producer input bytes mutated")
    return receipt

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--source-dir",required=True)
    p.add_argument("--out-dir",required=True)
    a=p.parse_args()
    try:
        result=audit(a.source_dir,a.out_dir)
        print("MAPPIPE_ORIGINAL_OWNER_CLIS_PASS="+json.dumps(result,sort_keys=True))
        return 0
    except (ReceiverError,OSError,ValueError,subprocess.TimeoutExpired) as e:
        print("MAPPIPE_FAIL="+str(e),file=sys.stderr)
        return 2

if __name__=="__main__":
    raise SystemExit(main())
