#!/usr/bin/env python3
"""Independently audit actual import ZIP and four byte-corruption mutants."""
import argparse,hashlib,io,json,re,zipfile
from pathlib import Path
ROOT="Assets/B0132ScanSweep"
SHA=lambda d:hashlib.sha256(d).hexdigest()
GUID=re.compile(rb"(?m)^guid: ([0-9a-f]{32})\r?$")
def require(v,msg):
    if not v:raise ValueError("SCAN_PHYSICAL_RECEIVER_RED "+msg)
def check(files,receipt):
    by_path={x["path"]:x for x in receipt["files"]}
    require(set(by_path)==set(files) and len(files)==20,"20 original Unity source inventory")
    for p,d in files.items():
        require(p.startswith(ROOT+"/") or p==ROOT+".meta","out-of-scope ZIP member "+p)
        require(SHA(d)==by_path[p]["sha256"] and len(d)==by_path[p]["bytes"],"source hash drift "+p)
    metas={p:GUID.search(d) for p,d in files.items() if p.endswith(".meta")}
    require(len(metas)==12 and all(metas.values()),"12 actual original metadata GUIDs")
    unique=[x.group(1).decode() for x in metas.values()]
    require(len(set(unique))==12,"original GUID collision")
    script=ROOT+"/Runtime/B0132ScanSweepVisual.cs.meta"
    guid=metas[script].group(1).decode()
    pf=files[ROOT+"/Resources/B0132_ScanSweep.prefab"].decode()
    require("m_Script: {fileID: 11500000, guid: "+guid+", type: 3}" in pf,"prefab script GUID not genuine")
    shader=files[ROOT+"/Shaders/B0132ScanSweepTransparentURP.shader"]
    require(b'Blend SrcAlpha OneMinusSrcAlpha' in shader and b'ZWrite Off' in shader and b'#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"' in shader,"original transparent URP shader lost")
    runtime=files[ROOT+"/Runtime/B0132ScanSweepVisual.cs"]
    require(b"ScanSweepCore.BuildSector" in runtime and b"MeshRenderer" in runtime and b"MeshFilter" in runtime,"actual procedural 3D mesh absent")
    for forbidden in [b"Physics.Raycast",b"AddComponent<Collider>",b"AddComponent<Rigidbody>",b"GrantReward(",b"ApplyDamage("]:
        require(forbidden not in runtime,"canonical gameplay mutation attempted")
    require(files[ROOT+"/Tests/PlayMode/ScanSweepPlayModeTests.cs"].count(b"[UnityTest]")==2,"2 actual authored native PlayMode cases missing")
    return len(files),len(metas)
def main():
    ap=argparse.ArgumentParser();ap.add_argument("--output",default="build/ScanSweepW08")
    out=Path(ap.parse_args().output);receipt=json.loads((out/"SOURCE_RECEIPT.json").read_text())
    raw=(out/"B0132-ScanSweep-UnityImport.zip").read_bytes()
    require(SHA(raw)==receipt["zip_sha256"],"physical ZIP byte SHA mismatch")
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        require(z.testzip() is None and len(z.namelist())==len(set(z.namelist())),"CRC/duplicate ZIP")
        files={p:z.read(p) for p in z.namelist()}
    v=check(files,receipt)
    print("SCAN_PHYSICAL_SOURCE_RECEIVER_PASS",v[0],"genuine Unity files",v[1],"meta GUIDs")
    malformed=[
        ("BROKEN_SHADER",lambda m:m.__setitem__(ROOT+"/Shaders/B0132ScanSweepTransparentURP.shader",b"broken")),
        ("MISSING_META",lambda m:m.pop(ROOT+"/Runtime/B0132ScanSweepVisual.cs.meta")),
        ("GUID_REUSE",lambda m:m.__setitem__(ROOT+"/Runtime/B0132ScanSweepVisual.cs.meta",m[ROOT+"/Runtime/ScanSweepCore.cs.meta"])),
        ("BROKEN_PREFAB_SCRIPT",lambda m:m.__setitem__(ROOT+"/Resources/B0132_ScanSweep.prefab",m[ROOT+"/Resources/B0132_ScanSweep.prefab"].replace(b"fileID: 11500000",b"fileID: 98760000")))
    ]
    for label,fn in malformed:
        m=files.copy();fn(m)
        try:check(m,receipt)
        except (ValueError,KeyError):print("SCAN_NEGATIVE_CORRUPTION_REJECT_PASS",label)
        else:raise AssertionError("ORIGINAL_SCAN_FALSE_GREEN "+label)
    print("SCAN_IMPORT_NEGATIVE_CONTROLS_PASS 4/4 actual deliberately corrupted input checks")
if __name__=="__main__":main()
