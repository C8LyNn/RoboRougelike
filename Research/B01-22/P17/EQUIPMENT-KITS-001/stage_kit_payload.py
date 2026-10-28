#!/usr/bin/env python3
"""B01-22: safely stage verified equipment offers; no changes to Unity/game state."""
import argparse
import hashlib
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parent
PREFIX = "Research/B01-22/P17/EQUIPMENT-KITS-001/"
PAYLOADS = {"catalog.json", "kit_core.mjs", "kit_cli.mjs", "kit_core.test.mjs", "README.md",
            "INSTALL_FOR_CODEX.md", "KNOWN_LIMITATIONS.md", "TEST_PLAN.md", "CHANGELOG.md",
            "verify_host_source.py", "stage_kit_payload.py", "test_stage_kit_payload.py",
            "TEST_EVIDENCE/v8-direct.json"}
STAGED = {"catalog.json", "offers.json", "receipt.json"}


class IntakeError(ValueError):
    pass


def sha(data):
    return hashlib.sha256(data).hexdigest()


def json_bytes(value):
    return (json.dumps(value, ensure_ascii=False, sort_keys=True, indent=2) + "\n").encode("utf-8")


def no_symlink(path):
    path = Path(path)
    if any(p.is_symlink() for p in (path, *path.parents)):
        raise IntakeError("SYMLINK_REJECTED")


def verify_package(root=ROOT):
    root = Path(root)
    no_symlink(root)
    if not root.is_dir():
        raise IntakeError("SOURCE_NOT_DIRECTORY")
    try:
        manifest_bytes = (root / "manifest.json").read_bytes()
        manifest = json.loads(manifest_bytes)
    except (OSError, ValueError) as error:
        raise IntakeError("INVALID_MANIFEST") from error
    if manifest.get("schema") != "b01-package-v1" or manifest.get("package_id") != "B01-22-EQUIPMENT-KITS-001" or manifest.get("revision") != "1.0.2":
        raise IntakeError("INVALID_PACKAGE_VERSION")
    entries = manifest.get("files")
    if not isinstance(entries, list):
        raise IntakeError("MISSING_PAYLOAD_LIST")
    seen = set()
    for entry in entries:
        raw = entry.get("path") if isinstance(entry, dict) else None
        if not isinstance(raw, str) or not raw.startswith(PREFIX):
            raise IntakeError("PATH_OUTSIDE_PACKAGE")
        rel = raw[len(PREFIX):]
        path = Path(rel)
        if path.is_absolute() or rel != path.as_posix() or any(x in ("..", ".", "") for x in path.parts) or rel in seen:
            raise IntakeError("UNSAFE_OR_DUPLICATE_PATH")
        seen.add(rel)
        candidate = root / path
        no_symlink(candidate)
        try:
            data = candidate.read_bytes()
        except OSError as error:
            raise IntakeError("MISSING_PAYLOAD_" + rel) from error
        if sha(data) != entry.get("sha256") or len(data) != entry.get("bytes"):
            raise IntakeError("PAYLOAD_HASH_MISMATCH_" + rel)
    if seen != PAYLOADS:
        raise IntakeError("PAYLOAD_SET_MISMATCH")
    catalog = json.loads((root / "catalog.json").read_bytes())
    if catalog.get("package_id") != manifest["package_id"] or catalog.get("schema") != "b01-equipment-kits-v1" or catalog.get("currency") != "scrap" or catalog.get("no_mounted_positions") is not True:
        raise IntakeError("INVALID_CATALOG_SCHEMA")
    kits, prices, weapons = catalog.get("kits"), catalog.get("part_prices"), catalog.get("weapon_prices")
    if not isinstance(kits, list) or len(kits) != 10 or not isinstance(prices, dict) or not isinstance(weapons, dict):
        raise IntakeError("INVALID_KITS_OR_PRICES")
    seen_ids = set()
    for kit in kits:
        if kit.get("id") in seen_ids or not isinstance(kit.get("id"), str):
            raise IntakeError("DUPLICATE_KIT_ID")
        seen_ids.add(kit["id"])
        rows, weapon_types, item_count = set(), set(), 0
        for item in kit.get("parts", []):
            kind, count, weapon = item.get("kind"), item.get("count"), item.get("weapon")
            if type(count) is not int or count < 1 or count > 8:
                raise IntakeError("BAD_COUNT")
            if kind == "Core" or (kind not in prices and kind != "Gun"):
                raise IntakeError("UNKNOWN_PART")
            if kind == "Gun":
                if weapon not in weapons:
                    raise IntakeError("UNKNOWN_GUN")
                weapon_types.add(weapon)
            elif "weapon" in item:
                raise IntakeError("WEAPON_ON_NONGUN")
            key = (kind, weapon)
            if key in rows:
                raise IntakeError("DUPLICATE_PART_ROW")
            rows.add(key)
            item_count += count
        if not rows or len(weapon_types) > 2 or item_count > 12:
            raise IntakeError("INVALID_KIT_SIZE_OR_GUN_TYPES")
    return catalog, sha(manifest_bytes)


def make_offers(catalog, manifest_hash):
    output = []
    for kit in catalog["kits"]:
        rows = []
        for part in kit["parts"]:
            kind = part["kind"]
            weapon = part.get("weapon") if kind == "Gun" else None
            price = catalog["weapon_prices"][weapon] if kind == "Gun" else catalog["part_prices"][kind]
            rows.append({"part_kind": kind, "weapon_kind": weapon, "quantity": part["count"],
                         "reference_unit_scrap": price, "reference_line_scrap": price * part["count"]})
        output.append({"id": kit["id"], "title": kit["title"], "role": kit["role"],
                       "description": kit["description"], "parts": rows,
                       "reference_total_scrap": sum(p["reference_line_scrap"] for p in rows),
                       "item_count": sum(p["quantity"] for p in rows)})
    return {"schema": "b01-codex-kit-offers-v1", "package_id": catalog["package_id"],
            "source_manifest_sha256": manifest_hash, "source_codex_sha": catalog["provenance"]["codex_sha"],
            "data_only": True, "host_transaction_required": True, "has_mount_positions": False,
            "offers": output}


def stage(root, destination):
    catalog, manifest_hash = verify_package(root)
    destination = Path(destination)
    no_symlink(destination)
    if destination.exists():
        raise IntakeError("DESTINATION_EXISTS")
    if not destination.parent.is_dir():
        raise IntakeError("PARENT_MISSING")
    files = {"catalog.json": (Path(root) / "catalog.json").read_bytes(),
             "offers.json": json_bytes(make_offers(catalog, manifest_hash))}
    receipt = {"schema": "b01-kit-receipt-v1", "source_manifest_sha256": manifest_hash,
               "file_sha256": {name: sha(data) for name, data in sorted(files.items())},
               "modified_host": False}
    files["receipt.json"] = json_bytes(receipt)
    try:
        destination.mkdir(mode=0o700)
        for name, data in files.items():
            with (destination / name).open("xb") as output:
                output.write(data)
    except Exception:
        if destination.is_dir() and not destination.is_symlink():
            shutil.rmtree(destination)
        raise
    return receipt


def rollback(destination):
    destination = Path(destination)
    no_symlink(destination)
    if not destination.is_dir():
        raise IntakeError("STAGE_MISSING")
    files = list(destination.iterdir())
    if {f.name for f in files} != STAGED or any(not f.is_file() or f.is_symlink() for f in files):
        raise IntakeError("STAGE_HAS_UNEXPECTED_FILES")
    try:
        receipt = json.loads((destination / "receipt.json").read_bytes())
    except (OSError, ValueError) as error:
        raise IntakeError("RECEIPT_UNREADABLE") from error
    if receipt.get("schema") != "b01-kit-receipt-v1" or receipt.get("modified_host") is not False:
        raise IntakeError("RECEIPT_INVALID")
    hashes = receipt.get("file_sha256")
    if not isinstance(hashes, dict) or set(hashes) != {"catalog.json", "offers.json"}:
        raise IntakeError("RECEIPT_HASH_SET_INVALID")
    for name, expected in hashes.items():
        if sha((destination / name).read_bytes()) != expected:
            raise IntakeError("STAGED_DATA_CHANGED_" + name)
    shutil.rmtree(destination)
    return {"status": "ROLLED_BACK", "game_modified": False}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    group = p.add_mutually_exclusive_group(required=True)
    group.add_argument("--verify-only", action="store_true")
    group.add_argument("--stage")
    group.add_argument("--rollback")
    args = p.parse_args()
    try:
        if args.verify_only:
            catalog, manifest_hash = verify_package()
            result = {"status": "VERIFIED", "kits": len(catalog["kits"]), "manifest_sha256": manifest_hash}
        elif args.stage:
            result = {"status": "STAGED", "receipt": stage(ROOT, args.stage)}
        else:
            result = rollback(args.rollback)
        print(json.dumps(result, ensure_ascii=False, sort_keys=True))
    except (IntakeError, OSError, KeyError, TypeError, ValueError) as error:
        print(json.dumps({"status": "FAIL_CLOSED", "error": str(error)}, ensure_ascii=False))
        raise SystemExit(2)


if __name__ == "__main__":
    main()
