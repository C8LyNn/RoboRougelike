#!/usr/bin/env python3
"""Check physical original Unity ZIP, imports, script GUID links and 4 mutants."""
import argparse, hashlib, io, json, re,zipfile
from pathlib import Path
ROOT="Assets/B0132RepairWeld"
SHA=lambda d:hashlib.sha256(d).hexdigest()
GUID=re.compile(rb"(?m)^guid: ([0-9a-f]{32})\r?$")
def require(q,msg):
    if not q:raise ValueError("REPAIR_WELD_RECEIVER_REJECT "+msg)

def audit(files,receipt):
    source={x["path"]:x for x in receipt["files"]}
    require(set(files)==set(source),"original archive paths mutated")
    require(len(files)==22 and len(source)==22,"original import inventory length")
    for p,data in files.items():
        require(p.startswith(ROOT+"/") or p==ROOT+".meta","path escaped original isolated Unity namespace")
        require(data and len(data)==source[p]["bytes"] and SHA(data)==source[p]["sha256"],"SHA256 source drift "+p)
    guids={}
    for path,content in files.items():
        if not path.endswith(".meta"):continue
        m=GUID.search(content)
        require(m is not None,"original GUID missing "+path)
        h=m.group(1).decode()
        require(h not in guids,"GUID collision "+path)
        guids[h]=path
    require(len(guids)==11,"expected unique 11 metadata GUIDs")
    script=ROOT+"/Runtime/B0132RepairWeldVfx.cs"
    match=GUID.search(files[script+".meta"])
    prefab=files[ROOT+"/Resources/B0132_RepairWeld.prefab"].decode()
    require(("m_Script: {fileID: 11500000, guid: "+match.group(1).decode()+", type: 3}") in prefab,
            "physical prefab missing original VFX MonoBehaviour fileID/GUID")
    require("vfxEnabled: 1" in prefab and "m_Name: B0132_RepairWeld" in prefab,
            "prefab incorrect original serialized component")
    require("m_Component:\n  - component: {fileID: 101}\n  - component: {fileID: 102}" in prefab,
            "prefab missing Transform and MonoBehaviour")
    core=files[ROOT+"/Runtime/WeldVisualCore.cs"].decode()
    adapter=files[script].decode()
    require("LineRenderer" in adapter and "core.Cancel();" in adapter and "frame.Sparks[i]" in adapter,
            "original procedural 8-spark Unity renderer missing")
    require("CoolSeconds = 0.65f" in core and "SetProgress" in core and "SetVisualProgress" in adapter,
            "original core progress/cooling contract missing")
    require("TestAssemblies" in files[ROOT+"/Tests/PlayMode/B0132.RepairWeld.PlayModeTests.asmdef"].decode(),
            "native test asmdef missing NUnit TestAssemblies")
    return len(files),len(guids)
def main():
    p=argparse.ArgumentParser();p.add_argument("--output",default="build/RepairWeldW06")
    out=Path(p.parse_args().output)
    receipt=json.loads((out/"UNITY_IMPORT_RECEIPT.json").read_text())
    raw=(out/"B0132-RepairWeld-UnityImport.zip").read_bytes()
    require(SHA(raw)==receipt["zip_sha256"],"physical outer ZIP hash drift")
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        require(z.testzip() is None,"bad physical ZIP")
        require(len(z.namelist())==len(set(z.namelist())),"duplicate physical path")
        files={p:z.read(p) for p in z.namelist()}
    c,g=audit(files,receipt)
    print("REPAIR_WELD_IMPORT_RECEIVER_PASS",c,"actual file bytes,",g,"unique GUID,",1,"actual prefab linked original script")
    cases=[
      ("BYTE_TAMPER",lambda f:f.__setitem__(ROOT+"/Runtime/WeldVisualCore.cs",f[ROOT+"/Runtime/WeldVisualCore.cs"]+b"tamper")),
      ("MISSING_META",lambda f:f.pop(ROOT+"/Runtime/B0132RepairWeldVfx.cs.meta")),
      ("WRONG_GUID",lambda f:f.__setitem__(ROOT+"/Runtime/B0132RepairWeldVfx.cs.meta",
        f[ROOT+"/Runtime/WeldVisualCore.cs.meta"])),
      ("PREFAB_SCRIPT_BROKEN",lambda f:f.__setitem__(ROOT+"/Resources/B0132_RepairWeld.prefab",
        f[ROOT+"/Resources/B0132_RepairWeld.prefab"].replace(b"fileID: 11500000",b"fileID: 99990000")))
    ]
    for label,fn in cases:
        altered=files.copy();fn(altered)
        try:audit(altered,receipt)
        except (ValueError,KeyError):
            print("REPAIR_WELD_NEGATIVE_REJECT_PASS",label)
        else:raise AssertionError("FALSE_GREEN_NOT_REJECTED "+label)
    print("REPAIR_WELD_NEGATIVE_RECEIVER_PASS 4/4 intentional original source corruptions")
if __name__=="__main__":main()
