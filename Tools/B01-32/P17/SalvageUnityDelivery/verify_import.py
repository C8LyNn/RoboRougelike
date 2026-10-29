#!/usr/bin/env python3
"""Physical ZIP receiver tests, including deliberately modified bytes and GUID.
Source-only checks; no false Unity Editor, Player or GPU execution assertion.
"""
import argparse, hashlib, io, json, re, zipfile
from pathlib import Path
from PIL import Image

ROOT="Assets/B0132SalvageBeacon"
SHA=lambda b:hashlib.sha256(b).hexdigest()
GUID=re.compile(rb"(?m)^guid: ([a-f0-9]{32})$")
def require(v,why):
    if not v:raise ValueError("RECEIVER_FAIL "+why)
def audit(data,receipt):
    recorded={r["path"]:r for r in receipt["files"]}
    require(set(recorded)==set(data),"source file inventory modified")
    for path,contents in data.items():
        require(path.startswith(ROOT) and "../" not in path,"unsafe archive filename")
        require(len(contents)==recorded[path]["size"],"source file length drift "+path)
        require(SHA(contents)==recorded[path]["sha256"],"source file SHA drift "+path)
    paths=[f"{ROOT}/Resources/Sprites/beacon-{i}.png" for i in range(8)]
    guids={}
    for p in data:
        if not p.endswith(".meta"):continue
        values=GUID.findall(data[p])
        require(len(values)==1,"metadata GUID missing "+p)
        guid=values[0].decode()
        require(guid not in guids,"metadata GUID collision "+p)
        guids[guid]=p
    require(len(guids)==20,"20 original Unity importer GUIDs expected")
    for p in paths:
        image=Image.open(io.BytesIO(data[p]))
        require(image.mode=="RGBA" and image.size==(512,512),"RGBA source size "+p)
        require(image.getchannel("A").getbbox() is not None,"transparent-empty "+p)
        meta=data[p+".meta"]
        require(b"spriteMode: 1" in meta and b"textureType: 2" in meta,
                "Sprite Single import properties "+p)
        require(b"TextureImporter:" in meta,"Unity TextureImporter required")
    prefab=data[f"{ROOT}/Resources/B0132_SalvageBeaconLoop.prefab"].decode()
    refs=re.findall(r"(?m)^  - \\{fileID: 21300000, guid: ([a-f0-9]{32}), type: 3\\}$",prefab)
    require(len(refs)==8,"actual prefab must serialize eight and only eight Sprite refs")
    require(refs==[GUID.search(data[p+".meta"]).group(1).decode() for p in paths],
            "prefab sprite GUID or animation frame order mismatch")
    script=f"{ROOT}/Runtime/B0132SalvageBeaconLoop.cs"
    assert GUID.search(data[script+".meta"])
    require(("m_Script: {fileID: 11500000, guid: "+
             GUID.search(data[script+".meta"]).group(1).decode()+", type: 3}") in prefab,
            "real Unity MonoBehaviour script GUID missing")
    require("m_Sprite: {fileID: 21300000, guid: "+refs[0] in prefab,
            "first visible SpriteRenderer frame mismatch")
    require(b"PrefabImporter:" in data[f"{ROOT}/Resources/B0132_SalvageBeaconLoop.prefab.meta"],
            "real prefab importer missing")
    clock=data[f"{ROOT}/Runtime/SalvageBeaconClock.cs"].decode()
    runtime=data[script].decode()
    for token in ["ApplyDamage(","AddForce(","SaveRun(","GrantReward(","CollectLoot(",
                  "R5EnemyDirector","R6VehicleController","VfxPoolManager"]:
        require(token not in clock and token not in runtime,"canonical gameplay mutation "+token)
    require("visual.sprite = null" in runtime and "visual.enabled = false" in runtime,
            "VFX OFF must clear stale sprite and disable renderer")
    assert b"[UnityTest]" in data[f"{ROOT}/Tests/PlayMode/SalvageBeaconPlayModeTests.cs"]
    return {"asset_count":len(data),"meta_guids":len(guids),"sprites":len(refs),
            "PlayMode_authored":2,"native_unity":"NOT_RUN"}

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--output",default="build/B0132SalvageUnity")
    args=ap.parse_args()
    out=Path(args.output)
    receipt=json.loads((out/"SOURCE_RECEIPT.json").read_text())
    zbytes=(out/"B0132-SalvageBeacon-Unity-SpriteLoop.zip").read_bytes()
    require(SHA(zbytes)==receipt["zip_sha256"],"ZIP SHA256 was modified")
    with zipfile.ZipFile(io.BytesIO(zbytes)) as z:
        require(z.testzip() is None,"ZIP CRC mismatch")
        require(len(z.namelist())==len(set(z.namelist())),"duplicate source path ZIP")
        data={p:z.read(p) for p in z.namelist()}
    result=audit(data,receipt)
    print("UNITY_ASSET_ORIGINAL_RECEIVER_PASS",json.dumps(result,sort_keys=True))
    # Negative evidence: same test really rejects physically modified data.
    tests=0
    for label,modify in [
        ("PNG_BYTE_TAMPER", lambda a: a.__setitem__(
            f"{ROOT}/Resources/Sprites/beacon-0.png",
            a[f"{ROOT}/Resources/Sprites/beacon-0.png"]+b"x")),
        ("META_GUID_SWAP", lambda a: a.__setitem__(
            f"{ROOT}/Resources/Sprites/beacon-1.png.meta",
            a[f"{ROOT}/Resources/Sprites/beacon-0.png.meta"])),
        ("PREFAB_SPRITE_REMOVAL", lambda a: a.__setitem__(
            f"{ROOT}/Resources/B0132_SalvageBeaconLoop.prefab",
            a[f"{ROOT}/Resources/B0132_SalvageBeaconLoop.prefab"].replace(
                b"  - {fileID: 21300000,",b"  - {fileID: 99999999,",1))),
        ("MISSING_SOURCE_META",lambda a: a.pop(
            f"{ROOT}/Resources/Sprites/beacon-7.png.meta"))
    ]:
        tamper=data.copy();modify(tamper)
        try:audit(tamper,receipt)
        except (ValueError,KeyError,AssertionError):
            tests+=1;print("NEGATIVE_ORIGINAL_RECEIVER_REJECT_PASS",label)
        else:raise AssertionError("RECEIVER_UNEXPECTED_GREEN "+label)
    assert tests==4
    print("PHYSICAL_IMPORT_ZIP_AND_FOUR_NEGATIVES_PASS",result["asset_count"],"actual assets, 4/4 deliberate byte mutations denied")
if __name__=="__main__":main()
