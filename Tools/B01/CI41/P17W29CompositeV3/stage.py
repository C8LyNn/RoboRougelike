#!/usr/bin/env python3
"""B01-41 W29 original #826 plus #792/#780/#750 composite controlled source test.
Produces ORIGINAL production bytes from immutable commits; adapts ONLY #822 QA fixture
from V2 names to V3 in disposable scratch, never producer/game/Save.
"""
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
PACKAGE = "CI41-P17W29-EXACT-826-792-V7-COMPOSITE-001"
SHA = {
    "SAVE_V3": "99fb08619c78b3834bb4c0450499babf991f218a",
    "SAVE_OLD": "2b7983f0f9e09dd1d56906b379a3f7af7b02f21a",
    "PHYSICS": "16cb2a7b5158605b15191b6074f9e392e0d1ebd3",
    "ECON_V7": "b4df10a7ad7e6c758f8331013f6b216b3a3689a3",
    "OWNER": "b8ca29c7707cd6b4338d0b9772d74dee676d49d2",
    "WORLD": "33094f154c7b91119b36d58aab285e6e83c5de57",
    "RIG": "d94400d95d13db7f27e1533df05c9292d5380431",
    "COLD_R5": "60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb",
    "PHYS_QA": "e66968408b583404cd50250f53e655ea75a085fc",
    "GAME_W27": "ed8cf4bcc131688b9a93968a89683d2496586683",
    "STRICT_REF": "f5db40bf293fe9582ec7c6fcb3515aeff6fcf937",
}
# Exact original Git blob SHA-1, independently measured, no mutable branch checkouts.
PINS = {
    "SaveV3.cs": ("SAVE_V3", "Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs", "247ab912b73a0e6f17e86aa1a3c74343cb4d9af6"),
    "SessionV3.cs": ("SAVE_V3", "Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/CanonicalWholeGameSessionV3.cs", "927b286e76a358716d69e76da5d8351c2564d613"),
    "EconomyPolicy.cs": ("ECON_V7", "Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs", "53ebce6c07388dc9c8a8ed79b6de4d3ccbee4773"),
    "EconomySection.cs": ("ECON_V7", "Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs", "d8b71de91db36295b6220e9c4baad71eb54455f6"),
    "Gateway.cs": ("OWNER", "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs", "21733278b6ab0669fccb6d085e0b6cf242792de2"),
    "Lease.cs": ("OWNER", "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs", "a9331dd620336d8c42165d134dc8a5e05ed51873"),
    "Snapshot.cs": ("WORLD", "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs", "b52e511c370f5fca56c74029050844dc8cbb7f9a"),
    "RigSpec.cs": ("RIG", "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs", "b680ed0ec969ac23203fa55eab7b90f8daa86e43"),
    "Debris.cs": ("RIG", "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs", "fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
    "AtomicDockLedger.cs": ("PHYSICS", "Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs", "b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9"),
    "PhysicalRigPort.cs": ("PHYSICS", "Assets/B0123CanonicalRigPortW24/Runtime/CanonicalPhysicalRigPortW24.cs", "7623f698b6e644b8d8247a011b453bf40adc2227"),
    "RigHydrator.cs": ("PHYSICS", "Assets/B0123CanonicalRigPortW24/Runtime/CanonicalRigLedgerHydratorW24.cs", "eb21cb12605e94d17dc7decdf040c9009ca0aafe"),
    "UnityShim.cs": ("PHYSICS", "Tools/B01/B01-23/P17W25PhysicalRollbackR2/ManagedUnitySourceOnlyShim.cs", "53d16eb828772532f81a7af1ed2ed3a712b0e9e4"),
    "CompositeQAOriginal.cs": ("PHYS_QA", "Tools/B01/B01-62/P17W27CompositeOriginalPhysicalABI/Program.cs", "38ff2da6ee8de262180e41c95d3c5610a2928a01"),
    "prepare_hierarchy_shim.py": ("PHYS_QA", "Tools/B01/B01-62/P17W27CompositeOriginalPhysicalABI/prepare_hierarchy_shim.py", "a729300f8d37457e182f36fdce2a8cccbb5cc398"),
    "R5NativeUnityFixture.cs": ("COLD_R5", "Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs", "2508a04f57b04447112d2777ce4e3cd7478ed40e"),
}
SUBSTITUTIONS = (
    ("B01.CanonicalWholeGameAdapter002", "B01.CanonicalWholeGameAdapter003"),
    ("B01.WholeGameEconomy002", "B01.WholeGameEconomy003"),
    ("CanonicalWholeGameSessionV2", "CanonicalWholeGameSessionV3"),
    ("WholeGameEconomySlotV2", "WholeGameEconomySlotV3"),
)
UNSUPPORTED = (
    re.compile(r"\bSHA256\s*\.\s*HashData\s*\("),
    re.compile(r"\bConvert\s*\.\s*ToHexString\s*\("),
    re.compile(r"\bFile\s*\.\s*Move\s*\([^)]*,[^)]*,\s*true\s*\)"),
)
CORE = [
    "SaveV3.cs", "SessionV3.cs", "EconomyPolicy.cs", "EconomySection.cs",
    "Gateway.cs", "Lease.cs", "Snapshot.cs", "RigSpec.cs", "Debris.cs",
    "AtomicDockLedger.cs", "PhysicalRigPort.cs", "RigHydrator.cs", "UnityShim.cs",
]
ORIGINAL_ONLY = "No original source is modified. Only QA Program.cs and a synthetic test-only Unity shim adapt in scratch."

def git(*args):
    return subprocess.check_output(("git", *args), cwd=ROOT, stderr=subprocess.PIPE)

def git_blob(data):
    return hashlib.sha1(b"blob " + str(len(data)).encode() + b"\0" + data).hexdigest()

def validate_path(path):
    if not path or path.startswith("/") or "\\" in path or "\0" in path or ".." in Path(path).parts:
        raise ValueError("unsafe producer path")
    return path

def original(name):
    key, path, oid = PINS[name]
    ref = SHA[key]
    if not re.fullmatch("[0-9a-f]{40}", ref + "") or not re.fullmatch("[0-9a-f]{40}", oid):
        raise ValueError("non-immutable producer")
    b = git("show", ref + ":" + validate_path(path))
    if git_blob(b) != oid:
        raise AssertionError("original Git OID mismatch: " + name)
    return b

def adapt_qa(test):
    for old, new in SUBSTITUTIONS:
        count = test.count(old)
        if count < 1:
            raise AssertionError("original QA schema drift: " + old)
        test = test.replace(old, new)
    if re.search(r"(WholeGameEconomySlotV2|CanonicalWholeGameSessionV2|WholeGameEconomy002|CanonicalWholeGameAdapter002)", test):
        raise AssertionError("legacy V2 class retained")
    return test

def assert_new_writer(source):
    for pattern in UNSUPPORTED:
        if pattern.search(source):
            raise AssertionError("legacy API in new #826")
    if "File.Replace(staging,authoritative,null)" not in source:
        raise AssertionError("atomic replace removed")
    if "File.Move(staging,authoritative)" not in source:
        raise AssertionError("first write moved from two-arg API")
    if "File.Delete(authoritative)" in source:
        raise AssertionError("destructive replacing existing save")
    if "B0162G02" not in source or "save.envelope" not in source:
        raise AssertionError("on-disk envelope contract changed")
    return True

def project(staging, filename, framework, qa):
    compilefiles = CORE + (["CompositeQAAdapted.cs"] if qa else [])
    elements = "".join('<Compile Include="source/'+x+'" />' for x in compilefiles)
    xml = ('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
           '<TargetFramework>'+framework+'</TargetFramework>'
           '<OutputType>'+('Exe' if qa else 'Library')+'</OutputType>'
           '<LangVersion>9.0</LangVersion><Nullable>enable</Nullable>'
           '<EnableDefaultCompileItems>false</EnableDefaultCompileItems>'
           '<NoWarn>CS8618;CS8625</NoWarn><TreatWarningsAsErrors>true</TreatWarningsAsErrors>'
           '</PropertyGroup><ItemGroup>'+elements+'</ItemGroup></Project>')
    (staging / filename).write_text(xml, encoding="utf-8")

def stage(output):
    source = output / "source"
    source.mkdir(parents=True, exist_ok=False)
    records = {}
    for name in PINS:
        b = original(name)
        records[name] = {"producer": SHA[PINS[name][0]], "git_oid": PINS[name][2],
                         "path": PINS[name][1], "sha256": hashlib.sha256(b).hexdigest()}
        (source / name).write_bytes(b)
    save = (source / "SaveV3.cs").read_text(encoding="utf-8-sig")
    session = (source / "SessionV3.cs").read_text(encoding="utf-8-sig")
    assert_new_writer(save)
    if any(p.search(session) for p in UNSUPPORTED):
        raise AssertionError("old API still in V3 session")
    # Original source QA adapted only by existing V2 -> new V3 public symbol names.
    qa = adapt_qa((source / "CompositeQAOriginal.cs").read_text(encoding="utf-8-sig"))
    (source / "CompositeQAAdapted.cs").write_text(qa, encoding="utf-8")
    # Test-only fake Unity shim kept separate from real Unity engine.
    patch = subprocess.run([sys.executable, str(source / "prepare_hierarchy_shim.py"),
                            str(source / "UnityShim.cs")],
                           capture_output=True, text=True)
    (output / "shim-change.log").write_text(patch.stdout + patch.stderr, encoding="utf-8")
    if patch.returncode or "HIERARCHICAL_DESTROY_PATCH=APPLIED" not in patch.stdout:
        raise AssertionError("test-only original shim adaptation failed")
    project(output,"Composite826PhysicalNetStandard21.csproj","netstandard2.1",False)
    project(output,"Composite826PhysicalSourceQA.csproj","net8.0",True)
    manifest = {"package": PACKAGE, "pins": records,
                "selected_active_save": SHA["SAVE_V3"], "rejected_legacy": SHA["SAVE_OLD"],
                "gameplay_base_sha": SHA["GAME_W27"], "only_one_writer": True,
                "strict_787_store": "AUDIT_ONLY_NOT_INSTALLED",
                "r5_fixture": "PINNED_ONLY_NATIVE_NOT_RUN",
                "producer_gameplay_modified": False, "source_qa_program": "QA_ONLY_V2_TO_V3_PUBLIC_NAMES",
                "UnityEngine": "SYNTHETIC_HIERARCHY_SHIM_NOT_PHYSX",
                "formal_composite_G0": "HOLD_PENDING_B01_40_61_AND_REAL_RUNTIME_BINDER",
                "Unity_G1": "NOT_RUN", "Windows_Player": "NOT_RUN",
                "codeX_ack": "NONE", "game_integrated": False}
    (output / "composite-provenance.json").write_text(json.dumps(manifest,indent=2,sort_keys=True)+"\n")
    print("CI41_W29_IMMUTABLE_ORIGINAL_SOURCES_PASS",len(records))
    print("CI41_W29_UNITY_API_V3_REPLACEMENT_SOURCE_POLICY_PASS")
    print("CI41_W29_TEST_ONLY_PHYSICAL_QA_ADAPTED_NO_PRODUCER_MODIFICATION")
    print("CI41_W29_NATIVE_PHYSX_R5_NOT_RUN_FORMAL_G0_HOLD")

if __name__=="__main__":
    if len(sys.argv)!=2:
        sys.exit("usage: stage.py <new-empty-output-path>")
    try:
        out=Path(sys.argv[1]).resolve()
        if out.exists():
            raise ValueError("refuse existing destination even if empty")
        out.mkdir(parents=True)
        stage(out)
    except (ValueError, AssertionError, subprocess.CalledProcessError, OSError) as exc:
        print("CI41_W29_FAIL_CLOSED",str(exc),file=sys.stderr)
        sys.exit(2)
