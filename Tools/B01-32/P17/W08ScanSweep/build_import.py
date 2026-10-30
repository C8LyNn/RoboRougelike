#!/usr/bin/env python3
"""Real source-locked Unity 6 URP import archive. No canonical Assets writes or Unity claims."""
import argparse,hashlib,json,zipfile
from pathlib import Path
ROOT="Assets/B0132ScanSweep"
SRC=Path("Tools/B01-32/P17/W08ScanSweep")
PKG="B01-32-SCAN-SWEEP-VFX-001/R1"
SHA=lambda b:hashlib.sha256(b).hexdigest()
GUID=lambda p:SHA((PKG+":"+p).encode())[:32]
def require(v,why):
    if not v:raise ValueError("SCAN_UNITY_IMPORT_RED "+why)
def folder_meta(p):
    return (f"fileFormatVersion: 2\nguid: {GUID(p)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()
def source_meta(p,kind):
    if kind=="MonoImporter":
        return (f"fileFormatVersion: 2\nguid: {GUID(p)}\nMonoImporter:\n  externalObjects: {{}}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {{instanceID: 0}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()
    return (f"fileFormatVersion: 2\nguid: {GUID(p)}\n{kind}:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()
def prefab(p):
    return ("""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &100
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 101}
  - component: {fileID: 102}
  m_Layer: 0
  m_Name: B0132_ScanSweep
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &101
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &102
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: @GUID@, type: 3}
  m_Name:
  m_EditorClassIdentifier:
  visualsEnabled: 1
""").replace("@GUID@",GUID(p)).encode()
def make(out):
    require(not Path(ROOT).exists(),"this must not overwrite game asset directory")
    files={}
    for folder in [ROOT,ROOT+"/Runtime",ROOT+"/Shaders",ROOT+"/Resources",ROOT+"/Tests",ROOT+"/Tests/PlayMode"]:
        files[folder+".meta"]=folder_meta(folder)
    for name in ["ScanSweepCore.cs","B0132ScanSweepVisual.cs"]:
        p=ROOT+"/Runtime/"+name
        files[p]=(SRC/"Runtime"/name).read_bytes().replace(b"\r\n",b"\n")
        files[p+".meta"]=source_meta(p,"MonoImporter")
    test=ROOT+"/Tests/PlayMode/ScanSweepPlayModeTests.cs"
    files[test]=(SRC/"Tests/ScanSweepPlayModeTests.cs").read_bytes().replace(b"\r\n",b"\n")
    files[test+".meta"]=source_meta(test,"MonoImporter")
    shader=ROOT+"/Shaders/B0132ScanSweepTransparentURP.shader"
    files[shader]=(SRC/"Shaders/B0132ScanSweepTransparentURP.shader").read_bytes().replace(b"\r\n",b"\n")
    files[shader+".meta"]=source_meta(shader,"ShaderImporter")
    asm=ROOT+"/Runtime/B0132.ScanSweep.Runtime.asmdef"
    testasm=ROOT+"/Tests/PlayMode/B0132.ScanSweep.PlayModeTests.asmdef"
    files[asm]=(json.dumps(dict(name="B0132.ScanSweep.Runtime",
         rootNamespace="RoboRougelike.B0132P17.ScanSweep",references=[],
         includePlatforms=[],excludePlatforms=[],allowUnsafeCode=False,
         overrideReferences=False,precompiledReferences=[],autoReferenced=True,
         defineConstraints=[],versionDefines=[],noEngineReferences=False),indent=2)+"\n").encode()
    files[testasm]=(json.dumps(dict(name="B0132.ScanSweep.PlayModeTests",
         rootNamespace="RoboRougelike.B0132P17.ScanSweep.Tests",
         references=["B0132.ScanSweep.Runtime"],includePlatforms=[],
         excludePlatforms=[],allowUnsafeCode=False,overrideReferences=False,
         precompiledReferences=[],autoReferenced=False,defineConstraints=[],
         versionDefines=[],noEngineReferences=False,
         optionalUnityReferences=["TestAssemblies"]),indent=2)+"\n").encode()
    files[asm+".meta"]=source_meta(asm,"AssemblyDefinitionImporter")
    files[testasm+".meta"]=source_meta(testasm,"AssemblyDefinitionImporter")
    pf=ROOT+"/Resources/B0132_ScanSweep.prefab"
    files[pf]=prefab(ROOT+"/Runtime/B0132ScanSweepVisual.cs")
    files[pf+".meta"]=source_meta(pf,"PrefabImporter")
    require(len(files)==20,"expected 20 actual Unity source/import members, got "+str(len(files)))
    guids=[GUID(p[:-5]) for p in files if p.endswith(".meta")]
    require(len(guids)==12 and len(set(guids))==12,"expected 12 unique .meta GUIDs")
    out.mkdir(parents=True,exist_ok=True)
    archive=out/"B0132-ScanSweep-UnityImport.zip"
    with zipfile.ZipFile(archive,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
        for p,d in sorted(files.items()):
            info=zipfile.ZipInfo(p,date_time=(2020,1,1,0,0,0))
            info.create_system=3;info.compress_type=zipfile.ZIP_DEFLATED;info.external_attr=0o100644<<16
            z.writestr(info,d,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
    with zipfile.ZipFile(archive) as z:
        require(z.testzip() is None and set(z.namelist())==set(files),"ZIP CRC/member corruption")
        for p,d in files.items():require(z.read(p)==d,"actual ZIP byte mismatch "+p)
    receipt={"package_id":PKG,"native_unity":"NOT_RUN","game_integrated":False,
        "zip_sha256":SHA(archive.read_bytes()),"zip_bytes":archive.stat().st_size,
        "unity_import_members":len(files),"meta_count":len(guids),
        "files":[{"path":p,"bytes":len(d),"sha256":SHA(d)} for p,d in sorted(files.items())]}
    (out/"SOURCE_RECEIPT.json").write_text(json.dumps(receipt,indent=2)+"\n")
    print("SCAN_PHYSICAL_UNITY_PACKAGE_PASS",len(files),"original Unity import assets",len(guids),"stable GUIDs")
    print("SCAN_REAL_SOURCE_ZIP_SHA256",receipt["zip_sha256"])
    print("NATIVE_UNITY_EDITOR_NOT_RUN")
if __name__=="__main__":
    ap=argparse.ArgumentParser();ap.add_argument("--output",default="build/ScanSweepW08")
    make(Path(ap.parse_args().output))
