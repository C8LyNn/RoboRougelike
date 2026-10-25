#!/usr/bin/env python3
"""B01 PACKAGE-AUTO-001: read-only validation and transactional Unity import into stamped disposable git worktrees.

Python 3.10+ stdlib only. It NEVER checks out or writes into arbitrary game directories.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import stat
import subprocess
import sys
import tempfile
import time
import uuid
import zipfile

VERSION = "1.0.0"
MARKER = ".b01-package-auto-sandbox.json"
JOURNAL_DIR = ".b01-package-auto"
GUID_RE = re.compile(rb"(?m)^guid: ([0-9a-fA-F]{32})\s*$")
SHA_RE = re.compile(r"^[0-9a-f]{40,64}$")
ASSET_EXTENSIONS = (".cs", ".asmdef", ".shader", ".unity", ".prefab", ".asset", ".mat", ".png", ".jpg", ".fbx", ".controller", ".anim", ".compute", ".cginc", ".uxml", ".uss")

class GateError(Exception):
    def __init__(self, code, detail):
        super().__init__(detail)
        self.code = code


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def dump(path, obj):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    # Replace journal atomically: interrupted writers cannot produce half JSON.
    temp = path.with_name(path.name + "." + uuid.uuid4().hex + ".tmp")
    with temp.open("x", encoding="utf-8") as fp:
        json.dump(obj, fp, ensure_ascii=False, sort_keys=True, indent=2)
        fp.write("\n")
        fp.flush()
        os.fsync(fp.fileno())
    os.replace(temp, path)


def read_json(path):
    try:
        return json.loads(Path(path).read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise GateError("INVALID_JSON", f"{path}: {exc}") from exc


def valid_path(name):
    p = PurePosixPath(name)
    if (not name or name != p.as_posix() or name.startswith("/") or "\\" in name or
        ":" in name or any(s in ("", ".", "..") for s in name.split("/")) or
        name.endswith("/") or "\x00" in name):
        raise GateError("UNSAFE_ZIP_PATH", name)
    return name


def verify_package(config, archive):
    config = dict(config)
    archive = Path(archive)
    if not archive.is_file():
        raise GateError("ZIP_NOT_FOUND", str(archive))
    data = archive.read_bytes()
    actual = sha256(data)
    expected = config.get("zip_sha256")
    if expected and actual != expected:
        raise GateError("ZIP_SHA256_MISMATCH", f"expected {expected}, got {actual}")
    if config.get("zip_bytes") is not None and len(data) != config["zip_bytes"]:
        raise GateError("ZIP_SIZE_MISMATCH", f"expected {config['zip_bytes']}, got {len(data)}")
    try:
        zf = zipfile.ZipFile(archive)
    except (zipfile.BadZipFile, OSError) as exc:
        raise GateError("INVALID_ZIP", str(exc)) from exc
    with zf:
        infos = zf.infolist()
        names = [valid_path(i.filename) for i in infos]
        if len(names) != len(set(names)):
            raise GateError("ZIP_DUPLICATE_PATH", "Duplicate entries")
        if config.get("zip_entries") is not None and len(infos) != config["zip_entries"]:
            raise GateError("ZIP_INCOMPLETE", f"expected {config['zip_entries']} entries, got {len(infos)}")
        if any(stat.S_IFMT(i.external_attr >> 16) == stat.S_IFLNK for i in infos):
            raise GateError("ZIP_SYMLINK_FORBIDDEN", "ZIP contains a symlink")
        if any(i.file_size > 32_000_000 for i in infos) or sum(i.file_size for i in infos) > 200_000_000:
            raise GateError("ZIP_SIZE_LIMIT", "Uncompressed package too large")
        if zf.testzip() is not None:
            raise GateError("ZIP_CRC_FAIL", "At least one ZIP member has invalid CRC")
        contents = {name: zf.read(name) for name in names}
    mpath = config.get("manifest_path")
    if mpath not in contents:
        raise GateError("MANIFEST_MISSING", str(mpath))
    try:
        manifest = json.loads(contents[mpath])
    except json.JSONDecodeError as exc:
        raise GateError("MANIFEST_INVALID", str(exc)) from exc
    hashes = dict(manifest.get("source_file_sha256_by_path", {}))
    hashes.update(manifest.get("qa_tool_sha256_by_path", {}))
    if not hashes:
        hashes = dict(config.get("pinned_file_sha256_by_path", {}))
    if not hashes:
        raise GateError("MANIFEST_EMPTY", "No SHA256 file entries")
    lengths = manifest.get("source_file_size_by_path", {}) or config.get("pinned_file_sizes_by_path", {})
    if set(hashes) != (set(contents) - {mpath}):
        raise GateError("MANIFEST_SCOPE_MISMATCH", f"missing hashes={sorted(set(contents)-{mpath}-set(hashes))}, extra={sorted(set(hashes)-set(contents))}")
    if set(hashes) != set(lengths):
        raise GateError("MANIFEST_SIZE_SCOPE", "Missing or extra byte-length entries")
    for name, expected_hash in hashes.items():
        if not re.fullmatch(r"[a-f0-9]{64}", expected_hash):
            raise GateError("INVALID_SHA256_FIELD", name)
        if sha256(contents[name]) != expected_hash:
            raise GateError("FILE_SHA256_MISMATCH", name)
        if len(contents[name]) != lengths[name]:
            raise GateError("FILE_SIZE_MISMATCH", name)
    # Modern manifests also advertise Git blob IDs. A ZIP must match the real pinned blob identity.
    for path, blob in manifest.get("git_blob_sha1_by_path", {}).items():
        if path not in contents:
            raise GateError("GIT_BLOB_PATH_MISSING", path)
        payload = contents[path]
        raw = b"blob " + str(len(payload)).encode() + b"\x00" + payload
        if hashlib.sha1(raw).hexdigest() != blob:
            raise GateError("GIT_BLOB_SHA_MISMATCH", path)
    # Unity metadata pairing includes directories: for every nested directory we require sibling .meta
    assets = {name for name in names if name.startswith("Assets/")}
    for name in assets:
        if name.endswith(".meta"):
            if not GUID_RE.search(contents[name]):
                raise GateError("META_GUID_MISSING", name)
        elif name.endswith(ASSET_EXTENSIONS) and name + ".meta" not in assets:
            raise GateError("META_PAIR_MISSING", name)
        if "/" in name and name.startswith("Assets/"):
            components = name.split("/")
            for j in range(2, len(components)):
                directory = "/".join(components[:j]); m = directory + ".meta"
                if m not in assets:
                    raise GateError("META_DIRECTORY_MISSING", m)
    guids = {}
    for name in sorted(assets):
        if not name.endswith(".meta"):
            continue
        guid = GUID_RE.search(contents[name]).group(1).decode().lower()
        if guid in guids:
            raise GateError("SOURCE_GUID_COLLISION", f"{guid}: {name} vs {guids[guid]}")
        guids[guid] = name
    asmdef_names = set()
    asmdefs = {}
    for name in assets:
        if not name.endswith(".asmdef"):
            continue
        try:
            asm = json.loads(contents[name])
        except json.JSONDecodeError as exc:
            raise GateError("ASMDEF_INVALID", name) from exc
        if asm.get("name") in asmdef_names:
            raise GateError("ASMDEF_NAME_COLLISION", name)
        asmdef_names.add(asm.get("name"))
        asmdefs[name] = asm
    # References to package-owned assemblies must resolve inside the package; host references are reported.
    external_refs = sorted({ref for asm in asmdefs.values() for ref in asm.get("references", [])
                            if not ref.startswith("GUID:") and ref not in asmdef_names})
    for name, asm in asmdefs.items():
        if len(asm.get("references", [])) != len(set(asm.get("references", []))):
            raise GateError("ASMDEF_DUPLICATE_REF", name)
    required_shader = config.get("required_shader")
    if required_shader:
        if required_shader not in assets or required_shader + ".meta" not in assets:
            raise GateError("URP_SHADER_DEPENDENCY_MISSING", str(required_shader))
        shader = contents[required_shader]
        if b"UniversalPipeline" not in shader or b"Universal" not in shader:
            raise GateError("URP_SHADER_INVALID", required_shader)
        if not any(b"Resources.Load<Shader>" in data for name, data in contents.items() if name.endswith(".cs") and name.startswith("Assets/")):
            raise GateError("URP_SHADER_NOT_REFERENCED", "No Resources.Load<Shader> runtime use")
    return {"package": config.get("id"), "revision": config.get("revision"), "source_sha": config.get("source_sha"),
            "zip_sha256": actual, "zip_bytes": len(data), "zip_entries": len(names), "sha256_entries": len(hashes),
            "unity_guid_count": len(guids), "asmdefs": asmdefs, "external_assembly_refs": external_refs,
            "manifest_sha256": sha256(contents[mpath]), "status": "PASS_SOURCE_STATIC_NOT_NATIVE"}, contents


def git(*args, cwd):
    p = subprocess.run(["git", *args], cwd=cwd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
    if p.returncode:
        raise GateError("GIT_ERROR", f"git {' '.join(args)}: {p.stderr.strip()}")
    return p.stdout.strip()


def create_sandbox(repo, target, source_ref):
    repo = Path(repo).resolve(); target = Path(target).resolve()
    if target.exists() or repo == target or repo in target.parents:
        raise GateError("TARGET_NOT_DISPOSABLE", "Sandbox destination must be new and outside source repository")
    if not re.fullmatch("[0-9a-f]{40}", source_ref):
        raise GateError("SOURCE_SHA_REQUIRED", "Pin 40-character target source commit SHA")
    git("rev-parse", "--show-toplevel", cwd=repo)
    actual = git("rev-parse", "--verify", source_ref + "^{commit}", cwd=repo)
    if actual != source_ref:
        raise GateError("SOURCE_SHA_MISMATCH", source_ref)
    git("worktree", "add", "--detach", str(target), source_ref, cwd=repo)
    try:
        dump(target / MARKER, {"version": VERSION, "expected_head": source_ref, "sandbox_absolute_path": str(target),
                               "source_repo": str(repo), "sandbox_id": uuid.uuid4().hex})
    except BaseException:
        git("worktree", "remove", "--force", str(target), cwd=repo)
        raise
    return {"sandbox": str(target), "expected_head": source_ref, "created": True}


def validate_target(target, expected_head=None, require_clean=True):
    target = Path(target).resolve()
    marker_path = target / MARKER
    if not marker_path.is_file():
        raise GateError("NOT_ISOLATED_SANDBOX", f"Missing marker {marker_path}")
    marker = read_json(marker_path)
    if (marker.get("sandbox_absolute_path") != str(target) or not marker.get("sandbox_id") or
        not marker.get("expected_head")):
        raise GateError("SANDBOX_MARKER_INVALID", str(marker_path))
    gitroot = Path(git("rev-parse", "--show-toplevel", cwd=target)).resolve()
    if gitroot != target:
        raise GateError("TARGET_NOT_GIT_ROOT", str(gitroot))
    actual = git("rev-parse", "HEAD", cwd=target)
    expected = expected_head or marker["expected_head"]
    if actual != expected or actual != marker["expected_head"]:
        raise GateError("TARGET_SHA_MISMATCH", f"expected {expected}, got {actual}")
    # A worktree created with --detach always has a .git FILE. Reject ordinary repositories.
    if not (target / ".git").is_file():
        raise GateError("NOT_DISPOSABLE_WORKTREE", "Must use git worktree add --detach")
    if require_clean:
        dirt = git("status", "--porcelain", "--untracked-files=all", cwd=target).splitlines()
        unexpected = [line for line in dirt if not line[3:].replace("\\", "/").startswith(JOURNAL_DIR + "/")
                      and line[3:].strip('"') != MARKER]
        if unexpected:
            raise GateError("TARGET_DIRTY", "Unexpected changes: " + repr(unexpected[:8]))
    return {"target_sha": actual, "sandbox": str(target), "sandbox_id": marker["sandbox_id"]}


def _target_guids(target):
    guids = {}
    for p in (Path(target) / "Assets").rglob("*.meta") if (Path(target) / "Assets").exists() else []:
        if p.is_symlink():
            raise GateError("TARGET_SYMLINK", str(p))
        m = GUID_RE.search(p.read_bytes())
        if not m:
            raise GateError("TARGET_BAD_META", str(p))
        guid = m.group(1).decode().lower()
        rel = p.relative_to(target).as_posix()
        if guid in guids and guids[guid] != rel:
            raise GateError("TARGET_EXISTING_GUID_COLLISION", f"{guids[guid]} vs {rel} on {guid}")
        guids[guid] = rel
    return guids


def plan_import(config, archive, target, expected_head=None):
    if config.get("kind", "unity") != "unity":
        raise GateError("PROTOTYPE_NO_UNITY_INSTALL", "A domain-only package has no owned Unity Assets")
    info, contents = verify_package(config, archive)
    target = Path(target).resolve()
    info.update(validate_target(target, expected_head))
    files = {p: data for p, data in contents.items() if p.startswith("Assets/")}
    owned_prefixes = tuple(config.get("owned_prefixes", []))
    parent_files = set(config.get("preserve_parent_meta", []))
    owned_meta = set(config.get("owned_root_metas", []))
    if not owned_prefixes or any(not s.startswith("Assets/") for s in owned_prefixes):
        raise GateError("UNSAFE_OWNERSHIP_CONFIG", "Empty/invalid owned_prefixes")
    for path in files:
        if not path.startswith(owned_prefixes) and path not in parent_files and path not in owned_meta:
            raise GateError("NON_OWNED_PATH", path)
    target_guids = _target_guids(target)
    actions, skipped, problems = [], [], []
    required_version = config.get("unity_editor_version")
    if required_version:
        vpath = target / "ProjectSettings/ProjectVersion.txt"
        actual_version = None
        if vpath.is_file():
            match = re.search(r"(?m)^m_EditorVersion:\s*(\S+)", vpath.read_text(encoding="utf-8"))
            actual_version = match.group(1) if match else None
        if actual_version != required_version:
            problems.append({"type": "UNITY_VERSION_MISMATCH", "expected": required_version, "actual": actual_version})
    required_packages = config.get("required_packages", [])
    if required_packages:
        package_file = target / "Packages/manifest.json"
        if not package_file.is_file():
            problems.append({"type": "UNITY_PACKAGES_MISSING", "path": "Packages/manifest.json"})
        else:
            package_versions = read_json(package_file).get("dependencies", {})
            for package in required_packages:
                if package not in package_versions:
                    problems.append({"type": "UNITY_PACKAGE_MISSING", "name": package})
    for rel, data in sorted(files.items()):
        dst = target / rel
        if dst.is_symlink() or any(parent.is_symlink() for parent in dst.parents if parent != target.parent):
            problems.append({"type": "SYMLINK_TARGET", "path": rel}); continue
        if rel in parent_files and dst.exists():
            if not dst.is_file() or not GUID_RE.search(dst.read_bytes()):
                problems.append({"type": "PARENT_META_BAD", "path": rel})
            else:
                skipped.append({"path": rel, "reason": "PRESERVE_TARGET_PARENT_GUID", "source_guid": GUID_RE.search(data).group(1).decode(),
                                "target_guid": GUID_RE.search(dst.read_bytes()).group(1).decode()})
            continue
        if dst.exists():
            problems.append({"type": "TARGET_PATH_EXISTS_NO_OVERWRITE", "path": rel})
            continue
        if rel.endswith(".meta"):
            guid = GUID_RE.search(data).group(1).decode().lower()
            if guid in target_guids:
                problems.append({"type": "GUID_COLLISION", "path": rel, "guid": guid, "existing": target_guids[guid]})
        actions.append({"path": rel, "sha256": sha256(data), "bytes": len(data)})
    # Disallow injecting generic file paths under unsafe target roots.
    if len({x["path"] for x in actions}) != len(actions):
        problems.append({"type": "ACTION_DUPLICATE_PATH"})
    manual = ["Codex must ACK actual live game's HEAD, reserved paths and existing parent GUID before G0_TARGET can PASS.",
              "Codex must wire the package API to host R3/R4/Pack run selection and save/objective flow; never copy host code into package.",
              "Codex + B01-60 must run actual licensed Unity 6000.3.14f1 Edit/Play and Windows build/Player/functional smoke; static pass is not G1."]
    return {"verification": info, "target": str(target), "actions": actions, "preserved_parent": skipped,
            "conflicts": problems, "dry_run_ok": not problems, "manual_codex_tasks": manual,
            "G0_TARGET": "DISPOSABLE_STATIC_DRY_RUN_PASS_NOT_LIVE_ACK" if not problems else "BLOCKED_CONFLICT",
            "G2_HANDOFF": "HOLD"}, files


def restore(target, journal, allow_pending=True):
    target = Path(target).resolve()
    validate_target(target, require_clean=False)
    journal = Path(journal).resolve()
    folder = (target / JOURNAL_DIR).resolve()
    if folder not in journal.parents:
        raise GateError("JOURNAL_OUTSIDE_SANDBOX", str(journal))
    data = read_json(journal)
    if data.get("sandbox_id") != read_json(target / MARKER)["sandbox_id"]:
        raise GateError("JOURNAL_SANDBOX_MISMATCH", str(journal))
    if data.get("state") == "ROLLED_BACK":
        return {"restored": 0, "already_rolled_back": True}
    paths = data.get("intents", [])
    # All-or-nothing prevalidation before removing anything. Modified files never removed.
    for entry in paths:
        dst = target / valid_path(entry["path"])
        if dst.is_symlink():
            raise GateError("ROLLBACK_FILE_MODIFIED", str(dst))
        if dst.exists() and (not dst.is_file() or sha256(dst.read_bytes()) != entry["sha256"]):
            raise GateError("ROLLBACK_FILE_MODIFIED", str(dst))
    count = 0
    for entry in reversed(paths):
        dst = target / entry["path"]
        if dst.is_file():
            dst.unlink(); count += 1
        parent = dst.parent
        while parent != target and parent.exists():
            try: parent.rmdir()
            except OSError: break
            parent = parent.parent
    data["state"] = "ROLLED_BACK"
    dump(journal, data)
    return {"restored": count, "state": "ROLLED_BACK", "preserved_originals": data.get("preserved_parent", [])}


def stage(config, archive, target, expected_head=None, fault_after=0):
    plan, files = plan_import(config, archive, target, expected_head)
    if not plan["dry_run_ok"]:
        raise GateError("TARGET_PREFLIGHT_FAILED", json.dumps(plan["conflicts"]))
    target = Path(target).resolve()
    transaction = uuid.uuid4().hex
    journal = target / JOURNAL_DIR / (transaction + ".json")
    metadata = {"version": VERSION, "transaction": transaction, "sandbox_id": plan["verification"]["sandbox_id"],
                "source_sha": config.get("source_sha"), "target_head": plan["verification"]["target_sha"],
                "state": "INSTALLING", "intents": [], "preserved_parent": plan["preserved_parent"]}
    dump(journal, metadata)
    try:
        for number, action in enumerate(plan["actions"], 1):
            rel = action["path"]
            dst = target / rel
            # Write a durable intent BEFORE each write. A SIGKILL leaves recoverable journal.
            metadata["intents"].append(action)
            dump(journal, metadata)
            dst.parent.mkdir(parents=True, exist_ok=True)
            if dst.exists() or dst.is_symlink():
                raise GateError("TARGET_RACE_CONFLICT", rel)
            with dst.open("xb") as out:
                out.write(files[rel]); out.flush(); os.fsync(out.fileno())
            if fault_after and number >= fault_after:
                raise GateError("INJECTED_INSTALL_FAILURE", f"After {number} files")
        metadata["state"] = "INSTALLED"
        dump(journal, metadata)
        return {"installed": len(plan["actions"]), "preserved": len(plan["preserved_parent"]),
                "journal": str(journal), "G0_TARGET": "DISPOSABLE_STAGED_ONLY_NO_LIVE_ACK", "G2_HANDOFF": "HOLD"}
    except BaseException as exc:
        try: restore(target, journal)
        except BaseException as rollback_exc:
            raise GateError("INSTALL_AND_ROLLBACK_FAILED", f"install={exc}; rollback={rollback_exc}; journal={journal}") from exc
        raise


def main(argv=None):
    parser = argparse.ArgumentParser(description="B01 package automation — non-destructive by default")
    sub = parser.add_subparsers(dest="command", required=True)
    p = sub.add_parser("verify"); p.add_argument("--config", required=True); p.add_argument("--zip", required=True); p.add_argument("--report")
    p = sub.add_parser("sandbox"); p.add_argument("--repo", required=True); p.add_argument("--target", required=True); p.add_argument("--source-ref", required=True)
    p = sub.add_parser("preflight"); p.add_argument("--config", required=True); p.add_argument("--zip", required=True); p.add_argument("--target", required=True); p.add_argument("--expected-head"); p.add_argument("--report")
    p = sub.add_parser("stage"); p.add_argument("--config", required=True); p.add_argument("--zip", required=True); p.add_argument("--target", required=True); p.add_argument("--expected-head"); p.add_argument("--fault-after", type=int, default=0)
    p = sub.add_parser("rollback"); p.add_argument("--target", required=True); p.add_argument("--journal", required=True)
    a = parser.parse_args(argv)
    try:
        if a.command == "sandbox": result = create_sandbox(a.repo, a.target, a.source_ref)
        elif a.command == "rollback": result = restore(a.target, a.journal)
        else:
            config = read_json(a.config)
            if a.command == "verify": result = verify_package(config, a.zip)[0]
            elif a.command == "preflight": result = plan_import(config, a.zip, a.target, a.expected_head)[0]
            else: result = stage(config, a.zip, a.target, a.expected_head, a.fault_after)
        if getattr(a, "report", None): dump(a.report, result)
        print(json.dumps({"ok": True, "result": result}, indent=2, ensure_ascii=False))
        if a.command == "preflight" and not result["dry_run_ok"]: return 2
        return 0
    except GateError as exc:
        print(json.dumps({"ok": False, "error_code": exc.code, "message": str(exc)}, ensure_ascii=False), file=sys.stderr)
        return 2

if __name__ == "__main__":
    sys.exit(main())
