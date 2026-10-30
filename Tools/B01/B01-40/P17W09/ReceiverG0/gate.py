#!/usr/bin/env python3
"""B01-40 independent exact-SHA physical source receiver, not native Unity QA."""
import argparse
import hashlib
import io
import json
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import zipfile

SQUAD_SHA = "4d13f76e6c0c08033e305900daf61c0a7267a725"
SQUAD_BASE = "f92c873bb1e397a598b133e107ae704e15f86768"
DEBRIS_SHA = "cb2b3a76e84266e2730fffc4ab048c1039f0652f"
DEBRIS_BASE = "5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"
CODEX_SHA = "203a0805251ed28293888e43f2d49f0df63767c6"
DEBRIS_ZIP_SHA256 = "e98ee8bb7a428f954290633240e8abb2434a566d46be17a6b96ae9c11aa473b2"
SQUAD_DIR = "Research/B01-24/P17/W06-SQUAD-PRESSURE-001/"
DEBRIS_DIR = "Assets/B0123DebrisRepulse001/"
DEBRIS_TOOL = "Tools/B01/B01-23/P17W06DebrisRepulse001/"
HEX32 = re.compile(r"^[a-f0-9]{32}$")


class GateError(Exception):
    pass


def require(condition, description):
    if not condition:
        raise GateError(description)


def git(root, *args):
    p = subprocess.run(["git", "-C", str(root), *args], capture_output=True, check=False)
    if p.returncode:
        raise GateError("git " + " ".join(args) + ": " + p.stderr.decode(errors="replace"))
    return p.stdout


def head(root, sha, label):
    actual = git(root, "rev-parse", "HEAD").decode().strip()
    require(actual == sha, label + " changed SHA: " + actual)


def changed(root, base):
    return set(git(root, "diff", "--name-only", base, "HEAD").decode().splitlines())


def bytes_at(root, path):
    return git(root, "show", "HEAD:" + path)


def sha256(value):
    return hashlib.sha256(value).hexdigest()


def git_blob(value):
    return hashlib.sha1(b"blob " + str(len(value)).encode() + b"\0" + value).hexdigest()


def guids(root):
    raw = git(root, "grep", "-n", "^guid: ", "HEAD", "--", "*.meta").decode(errors="replace")
    lookup = {}
    for line in raw.splitlines():
        m = re.match(r"HEAD:(.*?):[0-9]+:guid: ([a-f0-9]{32})$", line)
        require(m is not None, "malformed tracked Unity GUID line: " + line)
        path, uid = m.groups()
        lookup.setdefault(uid, []).append(path)
    return lookup


def verify_guids(root, prefix, expected, receiver):
    own = guids(root)
    owner_paths = [p for v in own.values() for p in v if p.startswith(prefix)]
    require(set(owner_paths) == set(expected), "missing/unexpected Unity .meta: " + repr(owner_paths))
    for key, paths in own.items():
        require(len(paths) == 1, "duplicate GUID in source tree: " + key + " " + repr(paths))
    target = guids(receiver)
    for uid, paths in own.items():
        if paths[0] in expected:
            require(uid not in target, "GUID already in Codex receiver: " + uid + " -> " + repr(target.get(uid)))
    return {p: next(uid for uid, paths in own.items() if paths == [p]) for p in expected}


def canonical_info(path):
    require(not path.startswith("/") and "\\" not in path and ".." not in path.split("/"), "unsafe member " + path)
    z = zipfile.ZipInfo(path, (1980, 1, 1, 0, 0, 0))
    z.compress_type = zipfile.ZIP_STORED
    z.create_system = 3
    z.external_attr = 0o100644 << 16
    return z


def make_zip(files):
    memory = io.BytesIO()
    with zipfile.ZipFile(memory, "w") as z:
        for name, data in sorted(files.items()):
            z.writestr(canonical_info(name), data)
    return memory.getvalue()


def verify_zip(data, original, manifest_name, expected_manifest):
    with zipfile.ZipFile(io.BytesIO(data), "r") as z:
        names = z.namelist()
        require(len(names) == len(set(names)), "duplicate ZIP member")
        require(set(names) == set(original) | {manifest_name}, "ZIP source membership mismatch")
        for name, content in original.items():
            require(z.read(name) == content, "ZIP member mismatch: " + name)
        require(json.loads(z.read(manifest_name)) == expected_manifest, "ZIP receiver manifest mismatch")


def stage_and_rollback(data, files):
    """Receiver-only staging/rollback: no in-repository writes, no deletions of changed files."""
    with tempfile.TemporaryDirectory(prefix="b01-40-g0-") as tmp:
        dest = Path(tmp) / "staging"
        dest.mkdir()
        with zipfile.ZipFile(io.BytesIO(data)) as z:
            for item in z.infolist():
                canonical_info(item.filename)
                p = dest / item.filename
                p.parent.mkdir(parents=True, exist_ok=True)
                require(not p.exists(), "duplicate stage file")
                p.write_bytes(z.read(item))
        original = {p.relative_to(dest).as_posix(): p.read_bytes() for p in dest.rglob("*") if p.is_file()}
        require(set(original) == set(files), "staged member mismatch")
        require(all(original[k] == files[k] for k in original), "stage byte mismatch")
        check = lambda: (
            {p.relative_to(dest).as_posix(): p.read_bytes() for p in dest.rglob("*") if p.is_file()} == original
            and not any(p.is_symlink() for p in dest.rglob("*"))
        )
        victim = dest / sorted(original)[0]
        victim.write_bytes(b"TAMPERED")
        require(not check(), "tamper should block rollback")
        victim.write_bytes(original[sorted(original)[0]])
        unowned = dest / "UNOWNED"
        unowned.write_text("foreign")
        require(not check(), "unowned member should block rollback")
        unowned.unlink()
        require(check(), "rollback verification rejected original stage")
        for p in sorted(dest.rglob("*"), key=lambda x: len(x.parts), reverse=True):
            if p.is_file():
                require(p.read_bytes() == original[p.relative_to(dest).as_posix()], "modified before rollback")
                p.unlink()
            else:
                p.rmdir()
        require(not list(dest.iterdir()), "rollback left files behind")


def squad_gate(source, receiver, output):
    head(source, SQUAD_SHA, "SQUAD")
    path = SQUAD_DIR + "manifest.json"
    manifest = json.loads(bytes_at(source, path))
    require(manifest["package_id"] == "B01-24-SQUAD-PRESSURE-001", "SQUAD identity")
    require(manifest["version"] == "1.0.0", "SQUAD version")
    records = manifest["files"]
    require(len(records) == 7, "SQUAD expected 7 pinned source blobs")
    paths = [entry["path"] for entry in records]
    require(len(set(paths)) == 7, "SQUAD duplicate pin")
    require(set(paths) | {path} == changed(source, SQUAD_BASE), "SQUAD owner diff scope")
    require(all(p.startswith(SQUAD_DIR) or p == ".github/workflows/b01-24-p17-w06-squad-pressure.yml" for p in paths), "SQUAD foreign path")
    originals = {path: bytes_at(source, path) for path in sorted(set(paths) | {path})}
    for row in records:
        require(git_blob(originals[row["path"]]) == row["blob"], "SQUAD pinned Git blob mismatch " + row["path"])
    require(manifest["original_codex"]["source_commit"] == CODEX_SHA, "SQUAD receiver SHA mismatch")
    orig_r3 = "Assets/R3/R3EnemyArchetype.cs"
    require(git(receiver, "rev-parse", "HEAD:" + orig_r3).decode().strip() ==
            manifest["original_codex"]["original_R3_git_blob"], "SQUAD Codex original R3 blob drift")
    meta = SQUAD_DIR + "SquadPressure.cs.meta"
    ids = verify_guids(source, SQUAD_DIR, [meta], receiver)
    require(manifest["test_receipt"]["unity_editor"] == "NOT_RUN", "SQUAD false native claim")
    require(manifest["intake"]["codex_ack"] == "PENDING", "SQUAD false ACK claim")
    receipt = {"schema": "b01-40-g0-receiver-v1", "package_id": manifest["package_id"],
               "version": manifest["version"], "producer_sha": SQUAD_SHA,
               "receiver_sha": CODEX_SHA, "producer_pr": 429, "owner": "B01-24",
               "source_status": "G0_SOURCE_ACCEPTED", "native_status": "G1_NOT_RUN",
               "codex_ack": False, "integrated": False,
               "import_blocker": "No producer Unity folder .meta or asmdef; Codex owns any assembly-safe import/adapter",
               "rollback": "Source-only ZIP: stage in clean external directory, remove only unchanged staged members",
               "files": [{"path": p, "bytes": len(v), "sha256": sha256(v), "git_blob": git_blob(v)}
                         for p, v in sorted(originals.items())],
               "unity_guids": ids}
    files = dict(originals)
    files["B01-40-RECEIVER-G0.json"] = (json.dumps(receipt, sort_keys=True, separators=(",", ":")) + "\n").encode()
    archive = make_zip(files)
    verify_zip(archive, originals, "B01-40-RECEIVER-G0.json", receipt)
    stage_and_rollback(archive, files)
    (output / "b01-40-squad-original-g0.zip").write_bytes(archive)
    receipt["physical_zip_sha256"] = sha256(archive)
    receipt["physical_zip_bytes"] = len(archive)
    return receipt


def debris_gate(source, receiver, output):
    head(source, DEBRIS_SHA, "DEBRIS")
    allowed = {".github/workflows/b01-23-p17w06-debris-repulse.yml"}
    allowed |= {p for p in git(source, "ls-tree", "-r", "--name-only", "HEAD").decode().splitlines()
                if p.startswith(DEBRIS_DIR) or p == "Assets/B0123DebrisRepulse001.meta" or p.startswith(DEBRIS_TOOL)}
    require(changed(source, DEBRIS_BASE) == allowed and len(allowed) == 14, "DEBRIS owner diff scope")
    metas = sorted(p for p in allowed if p.endswith(".meta"))
    require(len(metas) == 5, "DEBRIS expected five Unity GUIDs")
    ids = verify_guids(source, "Assets/B0123DebrisRepulse001", metas, receiver)
    asm = json.loads(bytes_at(source, DEBRIS_DIR + "Runtime/B0123.DebrisRepulse.Runtime.asmdef"))
    require(asm["name"] == "B0123.DebrisRepulse.Runtime", "DEBRIS unexpected asmdef name")
    require(asm["references"] == [] and asm["noEngineReferences"] is False and
            asm["autoReferenced"] is False, "DEBRIS unsafe asmdef dependency")
    script = source / DEBRIS_TOOL / "package.py"
    require(script.exists(), "missing original package builder")
    with tempfile.TemporaryDirectory(prefix="b01-40-debris-") as tmp:
        zip_path = Path(tmp) / "original-inner.zip"
        def producer(*args):
            p = subprocess.run([sys.executable, str(script), *args], cwd=source,
                               text=True, capture_output=True)
            require(p.returncode == 0, "DEBRIS producer physical " + repr(args) + "\n" + p.stdout + p.stderr)
            return p.stdout
        producer("build", "--archive", str(zip_path))
        archive = zip_path.read_bytes()
        require(sha256(archive) == DEBRIS_ZIP_SHA256, "DEBRIS immutable physical inner ZIP SHA256 mismatch")
        producer("verify", "--archive", str(zip_path))
        stage = Path(tmp) / "stage"
        producer("stage", "--archive", str(zip_path), "--destination", str(stage))
        require(len([p for p in stage.rglob("*") if p.is_file()]) == 14, "DEBRIS staged count")
        producer("rollback", "--archive", str(zip_path), "--destination", str(stage))
        require(not stage.exists(), "DEBRIS stage rollback failed")
        with zipfile.ZipFile(io.BytesIO(archive), "r") as z:
            manifest = json.loads(z.read("PACKAGE_MANIFEST.json"))
            require(manifest["source_sha"] == DEBRIS_SHA and manifest["codex_ack"] is False and
                    manifest["native_unity"] == "NOT_RUN", "DEBRIS false source/native/ACK claim")
            require(len(z.namelist()) == 14 and len(manifest["files"]) == 13, "DEBRIS member count")
            require(all(z.read(x["path"]) == bytes_at(source, x["path"]) and
                        sha256(z.read(x["path"])) == x["sha256"] and len(z.read(x["path"])) == x["bytes"]
                        for x in manifest["files"]), "DEBRIS source/manifest mismatch")
        (output / "b01-40-debris-original-g0.zip").write_bytes(archive)
    return {"schema": "b01-40-g0-receiver-v1", "package_id": "B01-23-DEBRIS-REPULSE-001",
            "version": "1.0.0", "producer_pr": 421, "owner": "B01-23",
            "producer_sha": DEBRIS_SHA, "receiver_sha": CODEX_SHA, "physical_zip_sha256": sha256(archive),
            "physical_zip_bytes": len(archive), "member_count": 14,
            "source_status": "G0_PHYSICAL_ACCEPTED", "native_status": "G1_NOT_RUN",
            "codex_ack": False, "integrated": False, "unity_guids": ids,
            "asmdef": asm["name"], "rollback": "Original producer ZIP stage/rollback on fresh external destination",
            "native_blocker": "No authorized Unity Editor/PhysX real Rigidbody execution; #439 source tests are not native"}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--squad", type=Path, required=True)
    ap.add_argument("--debris", type=Path, required=True)
    ap.add_argument("--receiver", type=Path, required=True)
    ap.add_argument("--out", type=Path, required=True)
    a = ap.parse_args()
    head(a.receiver, CODEX_SHA, "CODEX")
    a.out.mkdir(parents=True, exist_ok=True)
    results = [squad_gate(a.squad.resolve(), a.receiver.resolve(), a.out.resolve()),
               debris_gate(a.debris.resolve(), a.receiver.resolve(), a.out.resolve())]
    report = {"gate": "B01-40_PASS17_WAVE09", "native_unity": "NOT_RUN", "ci_kind": "GIT_ARTIFACT_INTEGRITY",
              "source_results": results, "hazardpulse_430": "HOLD_ON_444_RED_UNTIL_NEW_PRODUCER_SHA_AND_RETEST",
              "merge": False, "codex_ack": False}
    (a.out / "receiver-g0.json").write_text(json.dumps(report, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    for item in results:
        print(item["package_id"], item["source_status"], item["producer_sha"],
              "ZIP_SHA256", item["physical_zip_sha256"], "G1_NOT_RUN", "ACK_PENDING")
    print("B01-40 SOURCE G0 SUCCESS; Unity/PhysX/Player G1/G2 NOT_RUN; NO MERGE")


if __name__ == "__main__":
    try:
        main()
    except (GateError, OSError, ValueError, KeyError, zipfile.BadZipFile, subprocess.SubprocessError) as e:
        print("B01-40 G0 FAIL:", str(e), file=sys.stderr)
        sys.exit(1)
