#!/usr/bin/env python3
"""RUNSCORE001 — byte-exact deterministic, fail-closed physical Codex source handoff.
Source package ONLY; no native Unity tests or Codex acceptance implied.
"""
import argparse
import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent
FILES = (
    "RunScore.cs", "RunScoreCollector.cs", "Program.cs",
    "RunScore.Runner.csproj", "example-run.json", "README.md", "MANIFEST.json",
)
UNITY = {
    "RunScore.cs": "UnityInstall/Assets/B01RunScore/RunScore.cs",
    "RunScoreCollector.cs": "UnityInstall/Assets/B01RunScore/RunScoreCollector.cs",
}
OTHER = {
    "Program.cs": "Evidence/Program.cs",
    "RunScore.Runner.csproj": "Evidence/RunScore.Runner.csproj",
    "example-run.json": "Evidence/example-run.json",
    "README.md": "Evidence/README.md",
    "MANIFEST.json": "Evidence/MANIFEST.json",
}
META_PREFIX = "UnityInstall/Assets/B01RunScore"
SOURCE_RECEIPT = "SOURCE_RECEIPT.json"
ARCHIVE_NAME = "B01-10_RUNSCORE001_R2_UNITY_SOURCE.zip"


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def git_blob(data):
    return hashlib.sha1(b"blob " + str(len(data)).encode("ascii") + b"\0" + data).hexdigest()


def guid(name):
    return hashlib.sha256(("B01-10/RUNSCORE001/R2/" + name).encode()).hexdigest()[:32]


def meta(name, folder=False):
    if folder:
        return ("fileFormatVersion: 2\n"
                "guid: " + guid(name) + "\n"
                "folderAsset: yes\n"
                "DefaultImporter:\n"
                "  externalObjects: {}\n"
                "  userData: \n"
                "  assetBundleName: \n"
                "  assetBundleVariant: \n").encode()
    return ("fileFormatVersion: 2\n"
            "guid: " + guid(name) + "\n"
            "MonoImporter:\n"
            "  externalObjects: {}\n"
            "  serializedVersion: 2\n"
            "  defaultReferences: []\n"
            "  executionOrder: 0\n"
            "  icon: {instanceID: 0}\n"
            "  userData: \n"
            "  assetBundleName: \n"
            "  assetBundleVariant: \n").encode()


def inputs():
    manifest = json.loads((ROOT / "MANIFEST.json").read_text(encoding="utf-8"))
    if manifest.get("packageId") != "B01-10-RUNSCORE001/R2":
        raise ValueError("Wrong source package/version")
    if manifest.get("actualCI", {}).get("nativeUnity") != "NOT_RUN":
        raise ValueError("Fake native Unity pass in producer manifest")
    if manifest.get("status", "").find("CODEX_ACK_PENDING") < 0:
        raise ValueError("Producer manifest may not claim CODEX ACK")
    if set(manifest["codeFiles"]) != set(FILES) - {"MANIFEST.json"}:
        raise ValueError("Exact source list does not match manifest")
    source = {}
    for name in FILES:
        content = (ROOT / name).read_bytes()
        if name != "MANIFEST.json":
            expected = manifest["codeFiles"][name]["blob"]
            if git_blob(content) != expected:
                raise ValueError("Git object drift in " + name)
        source[name] = content
    return source, manifest


def expected(source, ref):
    if not re.fullmatch(r"[a-f0-9]{40}", ref):
        raise ValueError("Exact 40-char source SHA is required")
    m = {UNITY.get(n, OTHER.get(n)): source[n] for n in FILES}
    m[META_PREFIX + ".meta"] = meta("B01RunScore", True)
    for n, dest in UNITY.items():
        m[dest + ".meta"] = meta(n)
    rows = [{"path": name, "sha256": sha256(blob), "bytes": len(blob)}
            for name, blob in sorted(m.items())]
    receipt = {
        "package_id": "B01-10-RUNSCORE001/R2",
        "immutable_source_sha": ref,
        "source_pr": "https://github.com/C8LyNn/RoboRougelike/pull/401",
        "real_native_unity_test": "NOT_RUN",
        "independent_G0": "PENDING",
        "independent_QA": "PENDING",
        "codex_ack": "PENDING",
        "integrated": False,
        "contents": rows,
        "rollback": "Copy only UnityInstall/Assets/B01RunScore + matching folder meta; delete them on rollback; original Codex Assets unchanged.",
        "claim": "Producer executable source archive, NOT a tested Unity Player and not accepted by Codex"
    }
    m[SOURCE_RECEIPT] = (json.dumps(receipt, sort_keys=True, ensure_ascii=False,
                                    separators=(",", ":")) + "\n").encode("utf-8")
    return m


def write_zip(path, members):
    with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_STORED, allowZip64=False) as out:
        for name, content in sorted(members.items()):
            info = zipfile.ZipInfo(name, date_time=(1980, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_STORED
            info.create_system = 3
            info.external_attr = 0o100644 << 16
            out.writestr(info, content)


def verify(archive, members):
    with zipfile.ZipFile(archive, "r") as z:
        names = z.namelist()
        if sorted(names) != sorted(members) or len(names) != len(set(names)):
            raise ValueError("ZIP member names missing/duplicated or injected")
        if any(".." in n or n.startswith("/") or "\\" in n for n in names):
            raise ValueError("Untrusted ZIP path")
        for name in members:
            i = z.getinfo(name)
            if i.compress_type != zipfile.ZIP_STORED or i.date_time != (1980, 1, 1, 0, 0, 0):
                raise ValueError("ZIP not reproducibly stored")
            if (i.external_attr >> 16) != 0o100644 or z.read(name) != members[name]:
                raise ValueError("ZIP bytes/mode drift: " + name)
        receipt = json.loads(z.read(SOURCE_RECEIPT))
        if receipt.get("real_native_unity_test") != "NOT_RUN" or receipt.get("codex_ack") != "PENDING":
            raise ValueError("Falsified Unity/ACK receipt")
    return True


def negatives(dest, members):
    failed_as_expected = []
    # Real corruption of original C# member
    bad = dict(members)
    key = UNITY["RunScoreCollector.cs"]
    bad[key] = bad[key] + b"\n// INJECTED\n"
    write_zip(dest, bad)
    try:
        verify(dest, members)
        raise AssertionError("Corrupt C# source incorrectly passed")
    except ValueError:
        failed_as_expected.append("source_tamper")
    # Fake Unity native test claim inside signed-by-content receipt
    bad = dict(members)
    old = json.loads(bad[SOURCE_RECEIPT])
    old["real_native_unity_test"] = "PASS"
    bad[SOURCE_RECEIPT] = (json.dumps(old, sort_keys=True,
                         ensure_ascii=False, separators=(",", ":")) + "\n").encode()
    write_zip(dest, bad)
    try:
        verify(dest, members)
        raise AssertionError("Forged Unity verdict incorrectly passed")
    except ValueError:
        failed_as_expected.append("fake_unity_pass")
    # Path traversal/double source injection
    bad = dict(members)
    bad["../Assets/R1/R1RunController.cs"] = b"// malicious collision\n"
    write_zip(dest, bad)
    try:
        verify(dest, members)
        raise AssertionError("Injected game overwrite incorrectly passed")
    except ValueError:
        failed_as_expected.append("unsafe_game_path")
    # Valid content but bad preserved Unity .meta
    bad = dict(members)
    bad[META_PREFIX + "/RunScore.cs.meta"] = meta("WrongRunScore.cs")
    write_zip(dest, bad)
    try:
        verify(dest, members)
        raise AssertionError("GUID drift incorrectly passed")
    except ValueError:
        failed_as_expected.append("meta_GUID_drift")
    return failed_as_expected


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("mode", choices=("pack", "verify", "negatives"))
    p.add_argument("--sha", required=True)
    p.add_argument("--archive", required=True)
    p.add_argument("--report", required=True)
    args = p.parse_args()
    source, _ = inputs()
    members = expected(source, args.sha)
    archive = Path(args.archive)
    archive.parent.mkdir(parents=True, exist_ok=True)
    if args.mode == "pack":
        write_zip(archive, members)
    if args.mode in ("pack", "verify"):
        verify(archive, members)
        result = {"passed": True, "mode": args.mode, "zip_sha256": sha256(archive.read_bytes()),
                  "members": len(members), "source_sha": args.sha,
                  "unity_import_sources": 2, "unity_meta_files": 3,
                  "native_unity": "NOT_RUN", "codex_ack": "PENDING"}
    else:
        failed_as_expected = negatives(archive, members)
        result = {"passed": len(failed_as_expected) == 4,
                  "mode": args.mode, "negative_checks": failed_as_expected,
                  "source_sha": args.sha, "native_unity": "NOT_RUN"}
        if not result["passed"]:
            raise ValueError("Negative cases incomplete")
    Path(args.report).write_text(json.dumps(result, sort_keys=True, indent=2)+"\n", encoding="utf-8")
    print("B01-10 RUNSCORE SOURCE_ZIP " + json.dumps(result, sort_keys=True))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError, zipfile.BadZipFile) as e:
        print("RUNSCORE SOURCE_ZIP FAIL: " + str(e), file=sys.stderr)
        sys.exit(2)
