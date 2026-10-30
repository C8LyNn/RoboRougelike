#!/usr/bin/env python3
"""Independently inspect physical ZIP/import ABI and reject 4 malicious data mutants."""
import argparse,hashlib,io,json,re,zipfile
from pathlib import Path
ROOT="Assets/B0132DualRecoil"
SHA=lambda b:hashlib.sha256(b).hexdigest()
GUID=re.compile(rb"(?m)^guid: ([a-f0-9]{32})\r?$")
def check(v,msg):
    if not v:raise ValueError("DUAL_RECOIL_RECEIVER_REJECT "+msg)
def inspect(files,receipt):
    expected={p["path"]:p for p in receipt["file_hashes"]}
    check(set(files)==set(expected) and len(files)==17,"inventory does not match 17 originals")
    for p,d in files.items():
        check(p.startswith(ROOT+"/") or p==ROOT+".meta","unsafe Unity import outside namespace")
        check(SHA(d)==expected[p]["sha256"] and len(d)==expected[p]["length"],
              "immutable original source changed "+p)
    metas=[p for p in files if p.endswith(".meta")]
    guids=[]
    for p in metas:
        value=GUID.search(files[p])
        check(value is not None,"missing import GUID "+p)
        guids.append(value.group(1).decode())
    check(len(guids)==11 and len(set(guids))==11,"11 unique original GUIDs expected")
    main=ROOT+"/Runtime/B0132DualRecoilVisual.cs"
    m=GUID.search(files[main+".meta"])
    prefab=files[ROOT+"/Resources/B0132_DualRecoil.prefab"].decode()
    check("m_Script: {fileID: 11500000, guid: "+m.group(1).decode()+", type: 3}" in prefab,
          "original prefab does not deserialize script reference")
    check("visualsEnabled: 1" in prefab,"original visual enabled setting lost")
    code=files[main].decode()
    check("BuildBoxMesh" in code and "MeshRenderer" in code and "MeshFilter" in code and
          "PlayLeftShot" in code and "PlayRightShot" in code,
          "original Unity 3D visual code or independent weapon APIs missing")
    check("CreatePrimitive" not in code and "AddComponent<Collider>" not in code and
          "AddComponent<Rigidbody>" not in code,
          "cosmetic-only barrel cannot add physics components")
    tests=files[ROOT+"/Tests/PlayMode/DualRecoilPlayModeTests.cs"].decode()
    check(tests.count("[UnityTest]")==2,"two actual authored original native Unity tests required")
    testasm=files[ROOT+"/Tests/PlayMode/B0132.DualRecoil.PlayModeTests.asmdef"].decode()
    check('"TestAssemblies"' in testasm,"PlayMode NUnit asmdef missing")
    return len(files),len(metas)
def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--output",default="build/DualRecoilW07")
    out=Path(ap.parse_args().output)
    receipt=json.loads((out/"UNITY_IMPORT_RECEIPT.json").read_text())
    zip_bytes=(out/"B0132-DualRecoil-UnityImport.zip").read_bytes()
    check(SHA(zip_bytes)==receipt["zip_sha256"],"physical ZIP SHA mismatch")
    with zipfile.ZipFile(io.BytesIO(zip_bytes)) as z:
        check(z.testzip() is None,"physical ZIP CRC failure")
        check(len(z.namelist())==len(set(z.namelist())),"duplicated zip members")
        files={p:z.read(p) for p in z.namelist()}
    result=inspect(files,receipt)
    print("DUAL_RECOIL_ORIGINAL_IMPORT_RECEIVER_PASS",result[0],"exact file bytes",result[1],"unique GUIDs")
    invalid=[
     ("SOURCE_BYTE_TAMPER",lambda f:f.__setitem__(ROOT+"/Runtime/DualRecoilCore.cs",f[ROOT+"/Runtime/DualRecoilCore.cs"]+b"x")),
     ("MISSING_META",lambda f:f.pop(ROOT+"/Runtime/B0132DualRecoilVisual.cs.meta")),
     ("DUPLICATE_GUID",lambda f:f.__setitem__(ROOT+"/Runtime/B0132DualRecoilVisual.cs.meta",f[ROOT+"/Runtime/DualRecoilCore.cs.meta"])),
     ("BROKEN_PREFAB_BINDING",lambda f:f.__setitem__(ROOT+"/Resources/B0132_DualRecoil.prefab",f[ROOT+"/Resources/B0132_DualRecoil.prefab"].replace(b"fileID: 11500000",b"fileID: 12345678")))
    ]
    for label,fn in invalid:
        altered=files.copy();fn(altered)
        try:inspect(altered,receipt)
        except (ValueError,KeyError):print("DUAL_RECOIL_NEGATIVE_REJECT_PASS",label)
        else:raise AssertionError("DUAL_RECOIL_FALSE_GREEN "+label)
    print("DUAL_RECOIL_NEGATIVE_SOURCE_MUTANTS_PASS 4/4 deliberately broken Unity assets correctly rejected")
if __name__=="__main__":main()
