#!/usr/bin/env python3
"""Physically build original pinned B01-32 salvage art into 8 importable Unity
Sprite Single assets + a real serialized, animated cosmetic prefab.
Fail closed on Git provenance drift; never write in canonical Assets.
"""
import argparse, hashlib, io, json, re, subprocess, sys, tempfile, zipfile
from pathlib import Path
from PIL import Image, ImageChops

PACKAGE="B01-32-SALVAGE-UNITY-001"
OWNER="9956a9281ccbd93f8c115796f90f86d3bd0a0ca1"
BASE="5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"
SRC="Research/B01-32/P17/SalvageBeacon"
RENDER="Tools/B01-32/P17/SalvageBeacon/render.py"
ORIGINAL_RENDER_BLOB="ba41b0fee60eafd1085044620c10748b71829178"
ORIGINAL_MANIFEST_BLOB="1835ec9059c075724bc7936339aef6e9cf7f9938"
TEMPLATE="Assets/TutorialInfo/Icons/URP.png.meta"
TEMPLATE_BLOB="0f2cab05ff467a6889a91c7fa17c485696f52ea0"
SRC_LOCAL="Tools/B01-32/P17/SalvageUnityDelivery"
PKG="Assets/B0132SalvageBeacon"
REPO=Path.cwd()
sha256=lambda b:hashlib.sha256(b).hexdigest()
sha1=lambda b:hashlib.sha1(b).hexdigest()

def ok(condition,reason):
    if not condition: raise ValueError("SALVAGE_RECEIVER_REJECT "+reason)
def git(*args):
    p=subprocess.run(["git",*args],cwd=REPO,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    ok(p.returncode==0,"Git command "+repr(args)+" "+p.stderr.decode(errors="replace")[-250:])
    return p.stdout.strip()
def git_data(commit,path,expected=None):
    blob=git("rev-parse",commit+":"+path).decode()
    if expected:ok(blob==expected,"git blob drift "+path)
    raw=git("show",commit+":"+path)
    ok(sha1(b"blob "+str(len(raw)).encode()+b"\\0"+raw)==blob,"git object bytes mismatch "+path)
    return raw
def guid(relative):
    return sha256((PACKAGE+":v1:"+relative).encode())[:32]
def folder_meta(relative):
    return (f"fileFormatVersion: 2\\nguid: {guid(relative)}\\nfolderAsset: yes\\nDefaultImporter:\\n  externalObjects: {{}}\\n  userData:\\n  assetBundleName:\\n  assetBundleVariant:\\n").encode()
def mono_meta(relative):
    return (f"fileFormatVersion: 2\\nguid: {guid(relative)}\\nMonoImporter:\\n  externalObjects: {{}}\\n  serializedVersion: 2\\n  defaultReferences: []\\n  executionOrder: 0\\n  icon: {{instanceID: 0}}\\n  userData:\\n  assetBundleName:\\n  assetBundleVariant:\\n").encode()
def importer_meta(relative,kind):
    return (f"fileFormatVersion: 2\\nguid: {guid(relative)}\\n{kind}:\\n  externalObjects: {{}}\\n  userData:\\n  assetBundleName:\\n  assetBundleVariant:\\n").encode()
def yaml_prefab(sprite_paths,loop_path):
    sprites="".join("  - {fileID: 21300000, guid: "+guid(s)+", type: 3}\\n" for s in sprite_paths)
    first=guid(sprite_paths[0]);script=guid(loop_path)
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &100100
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 100101}}
  - component: {{fileID: 100102}}
  - component: {{fileID: 100103}}
  m_Layer: 0
  m_Name: B0132_SalvageBeaconLoop
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &100101
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100100}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!212 &100102
SpriteRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100100}}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_Sprite: {{fileID: 21300000, guid: {first}, type: 3}}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_FlipX: 0
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {{x: 5.12, y: 5.12}}
  m_AdaptiveModeThreshold: 0.5
  m_SpriteTileMode: 0
  m_WasSpriteAssigned: 1
  m_MaskInteraction: 0
  m_SpriteSortPoint: 0
--- !u!114 &100103
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100100}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {script}, type: 3}}
  m_Name:
  m_EditorClassIdentifier:
  frames:
{sprites}  vfxEnabled: 1
""".encode()

def make():
    ap=argparse.ArgumentParser()
    ap.add_argument("--output",default="build/B0132SalvageUnity")
    args=ap.parse_args()
    out=Path(args.output).resolve()
    out.mkdir(parents=True,exist_ok=True)
    ok(not (REPO/"Assets/B0132SalvageBeacon").exists(),"canonical Assets path already occupied")
    ok(git("rev-parse",OWNER).decode()==OWNER,"immutable art source absent; fetch it first")
    template=git_data(BASE,TEMPLATE,TEMPLATE_BLOB).decode()
    ok("spriteMode: 0" in template and "TextureImporter:" in template,"unapproved Unity texture importer template")
    m=json.loads(git_data(OWNER,SRC+"/manifest.json",ORIGINAL_MANIFEST_BLOB))
    ok(m["package_id"]=="B01-32-SALVAGE-BEACON-001" and len(m["frames"])==8,"original source manifest differs")
    local={}
    with tempfile.TemporaryDirectory() as temp:
        dest=Path(temp)
        original=[RENDER,SRC+"/manifest.json"]+[SRC+"/"+e["source"] for e in m["frames"]]
        for p in original:
            data=git_data(OWNER,p,ORIGINAL_RENDER_BLOB if p==RENDER else ORIGINAL_MANIFEST_BLOB if p==SRC+"/manifest.json" else None)
            target=dest/p;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
        rendered=dest/"render";rendered.mkdir()
        x=subprocess.run([sys.executable,str(dest/RENDER),str(rendered)],cwd=dest,capture_output=True,text=True,timeout=180)
        ok(x.returncode==0,"original art renderer FAILED "+(x.stdout+x.stderr)[-900:])
        print(x.stdout)
        original_receipt=json.loads((rendered/"receipt.json").read_text())
        ok(original_receipt["package_id"]==m["package_id"],"original raster receipt mismatch")
        atlas=Image.open(rendered/"salvage-beacon-atlas.png").convert("RGBA")
        ok(atlas.size==(2048,1024),"original atlas dimensions")
        sprites=[]
        for i in range(8):
            png=(rendered/"frames"/f"beacon-{i}.png").read_bytes()
            image=Image.open(io.BytesIO(png)).convert("RGBA")
            ok(image.size==(512,512) and image.getchannel("A").getbbox()!=None,"empty/wrong PNG "+str(i))
            topLeft=((i%4)*512,(i//4)*512)
            tile=atlas.crop((topLeft[0],topLeft[1],topLeft[0]+512,topLeft[1]+512))
            ok(ImageChops.difference(image,tile).getbbox() is None,"sprite pixel parity "+str(i))
            sprites.append((f"{PKG}/Resources/Sprites/beacon-{i}.png",png))
        local.update(sprites)
    for p,data in sprites:
        meta=template.replace("guid: 727a75301c3d24613a3ebcec4a24c2c8", "guid: "+guid(p))
        meta=meta.replace("spriteMode: 0","spriteMode: 1").replace("spriteGenerateFallbackPhysicsShape: 1","spriteGenerateFallbackPhysicsShape: 0")
        ok(("guid: "+guid(p)) in meta and "spriteMode: 1" in meta,"Sprite Single metadata mismatch")
        local[p+".meta"]=meta.encode()
    for directory in [PKG,PKG+"/Runtime",PKG+"/Resources",PKG+"/Resources/Sprites",PKG+"/Tests",PKG+"/Tests/PlayMode"]:
        local[directory+".meta"]=folder_meta(directory)
    for fname in ["SalvageBeaconClock.cs","B0132SalvageBeaconLoop.cs"]:
        rel=f"{PKG}/Runtime/{fname}"
        local[rel]=(REPO/SRC_LOCAL/"Runtime"/fname).read_bytes()
        local[rel+".meta"]=mono_meta(rel)
    testrel=f"{PKG}/Tests/PlayMode/SalvageBeaconPlayModeTests.cs"
    local[testrel]=(REPO/SRC_LOCAL/"Tests/PlayMode/SalvageBeaconPlayModeTests.cs").read_bytes()
    local[testrel+".meta"]=mono_meta(testrel)
    runtime_asm=f"{PKG}/Runtime/B0132.SalvageUnity.Runtime.asmdef"
    test_asm=f"{PKG}/Tests/PlayMode/B0132.SalvageUnity.PlayModeTests.asmdef"
    local[runtime_asm]=(json.dumps({"name":"B0132.SalvageUnity.Runtime",
     "rootNamespace":"RoboRougelike.B0132P17.SalvageUnity",
     "references":[],"includePlatforms":[],"excludePlatforms":[],"allowUnsafeCode":False,
     "overrideReferences":False,"precompiledReferences":[],"autoReferenced":True,
     "defineConstraints":[],"versionDefines":[],"noEngineReferences":False},indent=2)+"\\n").encode()
    local[test_asm]=(json.dumps({"name":"B0132.SalvageUnity.PlayModeTests",
     "rootNamespace":"RoboRougelike.B0132P17.SalvageUnity.Tests",
     "references":["B0132.SalvageUnity.Runtime"],"includePlatforms":[],
     "excludePlatforms":[],"allowUnsafeCode":False,"overrideReferences":False,
     "precompiledReferences":[],"autoReferenced":False,
     "defineConstraints":[],"versionDefines":[],"noEngineReferences":False,
     "optionalUnityReferences":["TestAssemblies"]},indent=2)+"\\n").encode()
    for path in [runtime_asm,test_asm]:local[path+".meta"]=importer_meta(path,"AssemblyDefinitionImporter")
    prefab=f"{PKG}/Resources/B0132_SalvageBeaconLoop.prefab"
    local[prefab]=yaml_prefab([p for p,_ in sprites],f"{PKG}/Runtime/B0132SalvageBeaconLoop.cs")
    local[prefab+".meta"]=importer_meta(prefab,"PrefabImporter")
    ok(len(set(guid(p) for p in local if p.endswith(".meta")))==len([p for p in local if p.endswith(".meta")]),"GUID collision")
    local_guids=set()
    for mpath in (REPO/"Assets").rglob("*.meta"):
        raw=mpath.read_bytes()
        m=re.search(rb"(?m)^guid: ([a-f0-9]{32})",raw)
        if m:local_guids.add(m.group(1).decode())
    for path in local:
        if path.endswith(".meta"):ok(guid(path[:-5]) not in local_guids,"game GUID collision "+path)
    receipt={"package_id":PACKAGE,"version":"1.0.0",
     "original_owner_sha":OWNER,"original_manifest_git_blob":ORIGINAL_MANIFEST_BLOB,
     "original_renderer_git_blob":ORIGINAL_RENDER_BLOB,
     "unity_target":"6000.3.14f1 URP","native_unity":"NOT_RUN","integrated":False,
     "frame_count":len(sprites),"frame_seconds":0.12,
     "source_atlas_sha256":original_receipt["atlas"]["sha256"],
     "unity_sprite_paths":[p for p,_ in sprites],
     "asset_count":len(local),"files":[{"path":p,"size":len(d),"sha256":sha256(d)} for p,d in sorted(local.items())]}
    zip_path=out/"B0132-SalvageBeacon-Unity-SpriteLoop.zip"
    with zipfile.ZipFile(zip_path,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
        for p,data in sorted(local.items()):
            info=zipfile.ZipInfo(p,date_time=(2020,1,1,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED;info.external_attr=0o100644<<16
            z.writestr(info,data,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
    with zipfile.ZipFile(zip_path) as z:
        ok(z.testzip() is None,"physical ZIP corrupted")
        ok(set(z.namelist())==set(local),"ZIP files changed")
        for p,d in local.items():ok(z.read(p)==d,"ZIP bytes mismatch "+p)
    receipt["zip_sha256"]=sha256(zip_path.read_bytes());receipt["zip_bytes"]=zip_path.stat().st_size
    (out/"SOURCE_RECEIPT.json").write_text(json.dumps(receipt,indent=2)+"\\n",encoding="utf8")
    (out/"SOURCE_SHA256.txt").write_text("".join(sha256(d)+"  "+p+"\\n" for p,d in sorted(local.items())),encoding="utf8")
    print("SALVAGE_PHYSICAL_UNITY_PACKAGE_PASS",len(local),"actual source assets, 8 PNG Sprite Singles, 1 animated prefab, 8 direct serialized sprite refs")
    print("SALVAGE_UNITY_ZIP_SHA256",receipt["zip_sha256"])
    print("NATIVE_UNITY_NOT_RUN / CODEX_ACK_PENDING / NO_GAME_MUTATION")
if __name__=="__main__": make()
