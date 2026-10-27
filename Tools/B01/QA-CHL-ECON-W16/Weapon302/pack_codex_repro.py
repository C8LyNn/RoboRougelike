#!/usr/bin/env python3
"""Build one verified, deterministic, network-free Codex R3 item-loss repro ZIP.

Sources are exact original Git blobs fetched and verified by prepare.py;
never bundle any B01-written competing R3 or R4 production implementation.
--smoke extracts the final archive and executes BOTH original-source harnesses.
"""
import argparse
import hashlib
import json
import pathlib
import shutil
import subprocess
import sys
import tempfile
import zipfile

HERE=pathlib.Path(__file__).resolve().parent
ARCHIVE_ROOT="B01-22-CODEX-R3-REPRO"
ORIGINAL_PR302="d44fa09c227dffca1c1f8832383bbd80430bba90"
CODEX_PR303="c89744d5ab43f4a795abe62527d94439be573693"
SOURCES={
  "Assets/R3/R3VehicleModel.cs":"6d8b1c01449f531970f2e284154ddfce911151a8",
  "Assets/R4/R4WeaponDefinition.cs":"f4044c053f5aebb055b8ba0df0026ea156246e44",
}
LOCAL=[
 "Program.cs",
 "SecurityContract.cs",
 "RealWeaponEconomyQA.csproj",
 "SecurityContract.csproj",
 "OutsideUnityApi.cs",
 "verify_existing_red.py",
 "_pinned/real-source-receipt.json",
 "_pinned/Assets/R3/R3VehicleModel.cs",
 "_pinned/Assets/R4/R4WeaponDefinition.cs",
]

def sha256(raw):
    return hashlib.sha256(raw).hexdigest()

def git_blob(raw):
    return hashlib.sha1(b"blob "+str(len(raw)).encode("ascii")+b"\0"+raw).hexdigest()

README="""# B01-22 / CODEX ORIGINAL R3 MALFORMED GUN ITEM-LOSS REPRO

Original published Codex PR #302 HEAD: d44fa09c227dffca1c1f8832383bbd80430bba90
At verified Codex PR #303 HEAD c89744d5ab43f4a795abe62527d94439be573693
the R3 and R4 source Git blobs were byte-identical. No fix observed.

This package contains the ORIGINAL two C# production sources, hash checked
as Git blobs before packaging. All other C# files are EXPLICIT QA harnesses
or Unity/mount signature doubles used only for compiling original economy C#.
NOT a Unity engine, gameplay integration, Windows Player, or save/load run.

Requirements: .NET 8 SDK and Python >=3.10; no GitHub or internet needed.

Run from THIS extracted directory:

  dotnet build Weapon302/RealWeaponEconomyQA.csproj -c Release
  dotnet run --project Weapon302/RealWeaponEconomyQA.csproj -c Release --no-build -- evidence/bug-witness.json
  dotnet build Weapon302/SecurityContract.csproj -c Release
  python Weapon302/verify_existing_red.py

First executable: nine original-source scenarios; R05–R07 report
DEFECT_REPRODUCED (not product PASS). The second executable intentionally
exits 1 because three strict security expectations fail on the original
buggy source. The Python verifier succeeds ONLY if it confirms 3/3 RED,
and prints "NOT_SECURITY_FIXED". This is a witness, not a patch.

E01: ReceiveSalvage rejects invalid R4WeaponKind before adding any item.
E02: RepairInventory on already-corrupt gun rejects without throwing.
E03: Dismantle on already-corrupt gun NEVER removes GUID before validating
     payout, nor throws or mutates Scrap.

Observed current bug: invalid salvage accepted; Dismantle removes item
first, then R4WeaponDefinition.Get throws; Scrap not granted.

Future CORRECTED Codex original-source regression: replace the two SOURCE
copies ONLY after pinning and validating new owner Git SHA; rebuild; require
3/3 security contract GREEN with NO RED, and valid economics control PASS.
The bundled verify_existing_red.py must NOT be used as a post-fix acceptance
criterion: it intentionally expects the PRE-FIX RED state.

Code owner: Codex. B01-22 only authored isolated executable QA. No merge/G2.
"""

def package_members():
    members={}
    original_receipt=json.loads((HERE/"_pinned/real-source-receipt.json").read_text("utf-8"))
    if original_receipt.get("pin")!=ORIGINAL_PR302:
        raise RuntimeError("WRONG_ORIGINAL_CODEX_PIN")
    receipts={row["path"]:row for row in original_receipt["files"]}
    if set(receipts)!=set(SOURCES):
        raise RuntimeError("ORIGINAL_RECEIPT_FILES_NOT_TWO_EXPECTED_SOURCES")
    for relative in LOCAL:
        raw=(HERE/relative).read_bytes()
        members[f"{ARCHIVE_ROOT}/Weapon302/{relative}"]=raw
    for p,expected in SOURCES.items():
        raw=members[f"{ARCHIVE_ROOT}/Weapon302/_pinned/{p}"]
        if git_blob(raw)!=expected or receipts[p]["git_blob"]!=expected or sha256(raw)!=receipts[p]["sha256"]:
            raise RuntimeError("SOURCE_HAS_CHANGED_GIT_OR_SHA256 "+p)
    members[f"{ARCHIVE_ROOT}/README.md"]=README.encode("utf-8")
    metadata={
      "schema":"b01-22-codex-r3-immutable-offline-repro-1",
      "published_codex_pr302":ORIGINAL_PR302,
      "pr303_verified_same_R3_R4_blobs":CODEX_PR303,
      "original_git_blobs":SOURCES,
      "source_status":"UNFIXED",
      "strict_contract_after_test":"EXPECTED_RED_3_OF_3",
      "correct_security_contract_pass_count":0,
      "unity_native":"NOT_RUN",
      "real_save_load":"NOT_RUN",
      "contents":[{"path":name,"sha256":sha256(raw),"bytes":len(raw)}
                  for name,raw in sorted(members.items())],
    }
    members[f"{ARCHIVE_ROOT}/MANIFEST.json"]=(json.dumps(metadata,indent=2,sort_keys=True)+"\n").encode("utf-8")
    return members,metadata

def make_zip(members,dest):
    with zipfile.ZipFile(dest,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as archive:
        for path,raw in sorted(members.items()):
            info=zipfile.ZipInfo(path,date_time=(1980,1,1,0,0,0))
            info.compress_type=zipfile.ZIP_DEFLATED
            info.external_attr=(0o100644<<16)
            info.create_system=3
            archive.writestr(info,raw,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)

def verify_archive(archive_path,members):
    with zipfile.ZipFile(archive_path) as zf:
        found=zf.namelist()
        if found!=sorted(members) or zf.testzip() is not None:
            raise RuntimeError("ZIP_MEMBER_OR_CRC_MISMATCH")
        for path,raw in members.items():
            if zf.read(path)!=raw:
                raise RuntimeError("ZIP_MEMBER_BYTE_MISMATCH "+path)
    return sha256(archive_path.read_bytes())

def smoke_unpacked(archive_path):
    with tempfile.TemporaryDirectory(prefix="b01-codex-repro-") as d:
        with zipfile.ZipFile(archive_path) as f:
            for info in f.infolist():
                if not info.filename.startswith(ARCHIVE_ROOT+"/") or ".." in pathlib.PurePosixPath(info.filename).parts:
                    raise RuntimeError("BAD_MEMBER_PATH")
            f.extractall(d)
        cwd=pathlib.Path(d)/ARCHIVE_ROOT
        commands=[
          ["dotnet","build","Weapon302/RealWeaponEconomyQA.csproj","-c","Release","--nologo"],
          ["dotnet","run","--project","Weapon302/RealWeaponEconomyQA.csproj",
           "-c","Release","--no-build","--","evidence/bug-witness.json"],
          ["dotnet","build","Weapon302/SecurityContract.csproj","-c","Release","--nologo"],
          [sys.executable,"Weapon302/verify_existing_red.py"],
        ]
        summary=[]
        for cmd in commands:
            process=subprocess.run(cmd,cwd=cwd,text=True,
                stdout=subprocess.PIPE,stderr=subprocess.STDOUT,check=False,timeout=160)
            tail=process.stdout[-5000:]
            if process.returncode!=0:
                raise RuntimeError("EXTRACTED_REPRO_SMOKE_FAILED "+str(cmd)+" exit "+
                                   str(process.returncode)+"\n"+tail)
            summary.append({"command":" ".join(cmd),"exit":process.returncode})
        actual=(cwd/"evidence/bug-witness.json")
        witness=json.loads(actual.read_text("utf-8"))
        if witness.get("total")!=9 or witness.get("confirmed_defect_reproductions")!=3 or witness.get("tests_fail")!=0:
            raise RuntimeError("UNPACKED_BUG_WITNESS_MISMATCH")
        strict=json.loads((cwd/"artifacts/chl-econ/weapon302-security-contract.json").read_text("utf-8"))
        if strict.get("security_contract_fail")!=3 or strict.get("security_contract_pass")!=0:
            raise RuntimeError("UNPACKED_STRICT_EXPECTED_RED_MISSING")
        print("EXTRACTED_OFFLINE_BUNDLE_ACTUALLY_COMPILED_AND_EXECUTED "+
              "ORIGINAL_R3R4_9_OF_9_AND_3_OF_3_EXPECTED_RED")
        return summary

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--smoke",action="store_true")
    parser.add_argument("--output",default="artifacts/chl-econ/Codex_R3_InvalidWeapon_OfflineRepro.zip")
    a=parser.parse_args()
    output=pathlib.Path(a.output)
    output.parent.mkdir(parents=True,exist_ok=True)
    members,meta=package_members()
    make_zip(members,output)
    first=verify_archive(output,members)
    with tempfile.TemporaryDirectory() as d:
        clone=pathlib.Path(d)/"rebuilt.zip"
        make_zip(members,clone)
        second=verify_archive(clone,members)
    if first!=second:
        raise RuntimeError("DETERMINISTIC_ZIP_MISMATCH")
    cmds=smoke_unpacked(output) if a.smoke else []
    receipt={
      "artifact_name":output.name,
      "sha256":first,
      "bytes":output.stat().st_size,
      "archive_members":len(members),
      "original_R3_R4_git_blobs":SOURCES,
      "pr302":ORIGINAL_PR302,
      "pr303_same_binary_sources":CODEX_PR303,
      "zip_rebuild_2x":"PASS",
      "zip_all_members_byte_crc":"PASS",
      "smoke_extracted_original_csharp":"PASS" if a.smoke else "NOT_RUN",
      "smoke_steps":cmds,
      "product_security_status":"DEFECT_STILL_OPEN_EXPECTED_RED",
      "security_fix_verified":False,
      "unity_native":"NOT_RUN",
    }
    evidence=output.parent/"weapon302-offline-repro-receipt.json"
    evidence.write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n",encoding="utf-8")
    print("CODEX_OFFLINE_REPRO_SINGLE_ARCHIVE_VERIFIED",output,
          "bytes",receipt["bytes"],"sha256",first,"members",receipt["archive_members"])
if __name__=="__main__":
    main()
