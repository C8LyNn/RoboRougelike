#!/usr/bin/env python3
"""M04-003 lower ducted fan: reproducible bpy + CPython generator.

The exact, fully readable Blender Python source is packed losslessly in the
adjacent generate_m04_003.source.zlib.b64 file. This lightweight launcher
verifies SHA-256 and executes that source in the current Python process.
To recover the editable source:
    python generate_m04_003.py --extract-source
See README.md for mesh specification and Unity axis conventions.
"""
from pathlib import Path
import base64
import hashlib
import sys
import zlib

HERE = Path(__file__).resolve().parent
PACK = HERE / "generate_m04_003.source.zlib.b64"
EXPECTED_SHA256 = "932cbd194c0fef2eb3d8b9802c27e49d17eeda0868ac12de5afb7e369a290bbf"


def unpack_source():
    data = zlib.decompress(base64.b64decode(PACK.read_text(encoding="ascii").strip()))
    digest = hashlib.sha256(data).hexdigest()
    if digest != EXPECTED_SHA256:
        raise RuntimeError("M04-003 generator payload hash mismatch: " + digest)
    return data


def main():
    source = unpack_source()
    if "--extract-source" in sys.argv:
        destination = HERE / "generate_m04_003_readable.py"
        destination.write_bytes(source)
        print("Extracted editable generator:", destination)
        return
    exec(compile(source, str(HERE / "generate_m04_003_readable.py"), "exec"),
         {"__name__": "__main__", "__file__": str(HERE / "generate_m04_003_readable.py")})


if __name__ == "__main__":
    main()
