#!/usr/bin/env python3
"""Source-only exact-Codex-PR collision gate for already-produced physical Unity ZIP.

This is NOT an independent B01-40 acceptance and NOT native Unity. It reads
actual immutable Git tree and .meta bytes in one batch, checks 34 intended
asset paths and 20 original generated .meta GUIDs, then rejects mutations.
Never writes or touches any Codex/game Assets files.
"""
import argparse
import hashlib
import json
import re
import subprocess
import zipfile
from pathlib import Path

CODEX_REPORT_SHA = "f92c873bb1e397a598b133e107ae704e15f86768"
CODEX_GAMEPLAY_SHA = "203a0805251ed28293888e43f2d49f0df63767c6"
ROOT = "Assets/B0132SalvageBeacon"
GUID_RE = re.compile(rb"(?m)^guid: ([0-9a-f]{32})\r?$")
sha256 = lambda b: hashlib.sha256(b).hexdigest()

def require(condition, message):
    if not condition:
        raise ValueError("PINNED_CODEX_INTAKE_REJECT " + message)

def git(*args, data=None):
    proc = subprocess.run(["git", *args], input=data, stdout=subprocess.PIPE,
                          stderr=subprocess.PIPE, timeout=180)
    require(proc.returncode == 0, "git " + " ".join(args) + ": " +
            proc.stderr.decode("utf8", "replace")[-300:])
    return proc.stdout

def current_codex_assets(commit):
    actual = git("rev-parse", commit).decode().strip()
    require(actual == commit, "Codex commit is not checked out/fetched")
    entries = git("ls-tree", "-r", "-z", commit).split(b"\0")
    game_paths = set()
    meta_oids = []
    for entry in entries:
        if not entry:
            continue
        header, raw_path = entry.split(b"\t", 1)
        path = raw_path.decode("utf8")
        if not path.startswith("Assets/"):
            continue
        game_paths.add(path.casefold())
        if path.endswith(".meta"):
            oid = header.split()[2].decode()
            meta_oids.append((path, oid))
    require(len(game_paths) > 100, "suspiciously incomplete original Codex tree")
    require(len(meta_oids) > 100, "suspiciously incomplete Codex GUID metadata")
    # Git batch is one process on Windows+Linux; preserve original .meta bytes.
    response = git("cat-file", "--batch",
                   data=("".join(oid + "\n" for _, oid in meta_oids)).encode("ascii"))
    cursor = 0
    game_guids = {}
    for path, oid in meta_oids:
        end = response.index(b"\n", cursor)
        h = response[cursor:end]
        cursor = end + 1
        parts = h.split(b" ")
        require(parts[0].decode() == oid and parts[1] == b"blob",
                "Git batch metadata identity drift " + path)
        size = int(parts[2])
        blob = response[cursor:cursor + size]
        cursor += size
        require(response[cursor:cursor + 1] == b"\n",
                "Git batch payload framing mismatch " + path)
        cursor += 1
        match = GUID_RE.search(blob)
        if match:
            guid = match.group(1).decode()
            game_guids.setdefault(guid, []).append(path)
    require(cursor == len(response), "Git batch stream has unexpected extra bytes")
    return game_paths, game_guids, len(meta_oids)

def check_collision(generated_paths, generated_guids, existing_paths, existing_guids):
    folded = [p.casefold() for p in generated_paths]
    require(len(folded) == len(set(folded)), "generated duplicate/case-alias asset paths")
    require(len(generated_guids) == len(set(generated_guids)), "generated duplicate GUID")
    for path in generated_paths:
        require(path.startswith(ROOT + "/") or path == ROOT + ".meta",
                "unexpected outside-owned-package path " + path)
    # No foreign asset is allowed inside the package's target namespace.
    parent = ROOT.casefold()
    blockers = sorted(p for p in existing_paths
                      if p == parent + ".meta" or p.startswith(parent + "/"))
    require(not blockers, "Codex target namespace occupied " + repr(blockers[:6]))
    conflicts = sorted(set(folded).intersection(existing_paths))
    require(not conflicts, "Codex exact/case-insensitive asset paths collide " +
            repr(conflicts[:6]))
    clashes = sorted(set(generated_guids).intersection(existing_guids))
    require(not clashes, "Codex original GUID collision " +
            repr([(k, existing_guids[k][:2]) for k in clashes[:4]]))
    return True

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", default="build/B0132SalvageUnity")
    parser.add_argument("--codex-sha", default=CODEX_REPORT_SHA)
    args = parser.parse_args()
    require(args.codex_sha == CODEX_REPORT_SHA,
            "this receipt must pin explicitly reviewed #383 report HEAD")
    folder = Path(args.output)
    receipt = json.loads((folder / "SOURCE_RECEIPT.json").read_text())
    require(receipt["package_id"] == "B01-32-SALVAGE-UNITY-001",
            "wrong original source receiver")
    physical_zip = (folder / "B0132-SalvageBeacon-Unity-SpriteLoop.zip").read_bytes()
    require(sha256(physical_zip) == receipt["zip_sha256"],
            "immutable source ZIP SHA-256 does not match generated receipt")
    with zipfile.ZipFile(folder / "B0132-SalvageBeacon-Unity-SpriteLoop.zip") as z:
        generated_paths = z.namelist()
        metas = [p for p in generated_paths if p.endswith(".meta")]
        guids = []
        for path in metas:
            match = GUID_RE.search(z.read(path))
            require(match is not None, "generated .meta GUID missing " + path)
            guids.append(match.group(1).decode())
    require(len(generated_paths) == 34 and len(metas) == 20,
            "new physical Unity package inventory changed unexpectedly")
    game_paths, game_guids, meta_count = current_codex_assets(args.codex_sha)
    check_collision(generated_paths, guids, game_paths, game_guids)
    print("PINNED_CODEX_ORIGINAL_TREE_COLLISION_PASS",
          len(game_paths), "real game asset paths",
          meta_count, "game .meta blobs",
          len(game_guids), "game GUIDs",
          len(generated_paths), "staged paths",
          len(guids), "staged GUIDs")
    # Exercise the same collision oracle with real target data + deliberate faults.
    adversarial = [
        ("EXACT_TARGET_PATH", generated_paths + [next(iter(game_paths))],
         guids, game_paths, game_guids),
        ("SOURCE_NAMESPACE_OCCUPIED", generated_paths, guids,
         game_paths | {ROOT.casefold() + "/foreign.txt"}, game_guids),
        ("DUPLICATED_GENERATED_GUID", generated_paths,
         guids[:-1] + [guids[0]], game_paths, game_guids),
        ("GAME_GUID_COLLISION", generated_paths,
         guids[:-1] + [next(iter(game_guids))], game_paths, game_guids),
        ("CASE_ALIAS_IN_PACKAGE", generated_paths + [generated_paths[0].upper()],
         guids, game_paths, game_guids)
    ]
    for label, paths, g, existing_p, existing_g in adversarial:
        try:
            check_collision(paths, g, existing_p, existing_g)
        except ValueError:
            print("PINNED_CODEX_NEGATIVE_REJECT_PASS", label)
        else:
            raise AssertionError("PINNED_CODEX_COLLISION_FALSE_GREEN " + label)
    result = {
        "package_id": receipt["package_id"],
        "preflight_kind": "exact Git tree and original .meta bytes; NOT native Unity",
        "codex_pr": 383,
        "codex_report_sha": args.codex_sha,
        "gameplay_sha_reported_in_codex_pr": CODEX_GAMEPLAY_SHA,
        "source_zip_sha256": sha256(physical_zip),
        "game_asset_path_count": len(game_paths),
        "game_meta_file_count": meta_count,
        "game_unique_guid_count": len(game_guids),
        "staged_file_count": len(generated_paths),
        "staged_guid_count": len(guids),
        "casefold_path_guid_collisions": 0,
        "negative_tests": 5,
        "negative_rejections": 5,
        "native_unity": "NOT_RUN",
        "receiver_B01_40": "PENDING",
        "codex_ack": "PENDING"
    }
    (folder / "PINNED_CODEX_PREFLIGHT.json").write_text(
        json.dumps(result, indent=2) + "\n", encoding="utf8")
    print("PINNED_CODEX_G0_PREFLIGHT_PASS 34/34 paths, 20/20 GUIDs, 5/5 negative mutants; G1 NOT_RUN")

if __name__ == "__main__":
    main()
