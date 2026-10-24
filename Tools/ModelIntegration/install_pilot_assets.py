#!/usr/bin/env python3
"""SHA-256 verified installer for the three exact W01/H01 pilot GLB assets."""
import argparse
import hashlib
import json
import os
import tempfile
import zipfile
from pathlib import Path

EXPECTED = {
 "Assets/RRLImportedModels/W01/W01_R2_Game_L.glb": "22d3ce734493f13eb314fc48c2ee8ad9b1608e33acdfbd45081e1b0a248f58af",
 "Assets/RRLImportedModels/W01/W01_R2_Game_R.glb": "9cef884ec51bb6854ffd3e8bd575a62f876b2c4a83c33926e5d41d0952d9ec50",
 "Assets/RRLImportedModels/H01/H01_V4_SideDrive_Animated.glb": "8d20c13e9c472f2d943e3055bf5042f9d8d2dbf4489b3c74160aef26c61869db",
}
def digest(data):
    return hashlib.sha256(data).hexdigest()
def install(archive, project, write=False, expected=None):
    expected = EXPECTED if expected is None else expected
    root = Path(project).resolve()
    if not (root/"Assets").is_dir() or not (root/"ProjectSettings/ProjectVersion.txt").is_file():
        raise ValueError("Not a Unity project root")
    checked = {}
    with zipfile.ZipFile(archive) as z:
        for name, checksum in expected.items():
            info = z.getinfo(name)
            if not 0 < info.file_size <= 2_000_000:
                raise ValueError("Unexpected size: " + name)
            data = z.read(info)
            if data[:4] != b"glTF" or digest(data) != checksum:
                raise ValueError("GLB header or SHA-256 mismatch: " + name)
            checked[name] = data
    states = {}
    for name, data in checked.items():
        dest = root/name
        if dest.is_symlink() or not dest.parent.resolve().is_relative_to(root):
            raise ValueError("Invalid destination: " + str(dest))
        if dest.exists() and digest(dest.read_bytes()) != digest(data):
            raise FileExistsError("Refuse unexpected overwrite: " + str(dest))
        states[name] = "already-present" if dest.exists() else "pending"
    if write:
        for name, data in checked.items():
            dest = root/name
            if dest.exists():
                continue
            dest.parent.mkdir(parents=True, exist_ok=True)
            with tempfile.NamedTemporaryFile(mode="wb", dir=dest.parent, delete=False) as tmp:
                tmp.write(data)
                temp_path = tmp.name
            try:
                os.replace(temp_path, dest)
            finally:
                if os.path.exists(temp_path):
                    os.unlink(temp_path)
            states[name] = "installed"
    return {"status":"PASS","mode":"INSTALL" if write else "VERIFY_ONLY","assets":states}
def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--archive", required=True)
    p.add_argument("--project", required=True)
    p.add_argument("--install", action="store_true")
    a = p.parse_args()
    try:
        result = install(a.archive,a.project,a.install)
    except (OSError,ValueError,KeyError,zipfile.BadZipFile) as e:
        print(json.dumps({"status":"BLOCKED","reason":str(e)}))
        return 2
    print(json.dumps(result,indent=2))
    return 0
if __name__ == "__main__":
    raise SystemExit(main())
