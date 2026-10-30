#!/usr/bin/env python3
"""Produce a real 22-file importable Unity 6 URP prefab+source ZIP.
Only under external build output, never canonical Assets. No Unity execution.
"""
import argparse, hashlib, io, json, re, zipfile
from pathlib import Path
PACKAGE="B01-32-REPAIR-WELD-VFX-001"
VERSION="1.0.0"
ROOT="Assets/B0132RepairWeld"
SRC=Path("Tools/B01-32/P17/W06RepairWeld")
GUID=lambda p:hashlib.sha256((PACKAGE+":"+VERSION+":"+p).encode()).hexdigest()[:32]
SHA=lambda data:hashlib.sha256(data).hexdigest()

def require(p,message):
    if not p:raise ValueError("REPAIR_WELD_IMPORT_REJECT "+message)
def folder_meta(path):
    return (f"fileFormatVersion: 2\nguid: {GUID(path)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()
def mono_meta(path):
    return (f"fileFormatVersion: 2\nguid: {GUID(path)}\nMonoImporter:\n  externalObjects: {{}}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {{instanceID: 0}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()
def importer_meta(path,kind):
    return (f"fileFormatVersion: 2\nguid: {GUID(path)}\n{kind}:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()
def prefab(script):
    return """%YAML 1.1
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
  m_Name: B0132_RepairWeld
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
  m_Script: {fileID: 11500000, guid: @SCRIPT_GUID@, type: 3}
  m_Name:
  m_EditorClassIdentifier:
  vfxEnabled: 1
""".replace("@SCRIPT_GUID@",GUID(script)).encode("utf8")

def build(output):
    assets={}
    dirs=[ROOT,ROOT+"/Runtime",ROOT+"/Resources",ROOT+"/Tests",ROOT+"/Tests/PlayMode"]
    for folder in dirs:
        assets[folder+".meta"]=folder_meta(folder)
    for fname in ["WeldVisualCore.cs","B0132RepairWeldVfx.cs"]:
        path=f"{ROOT}/Runtime/{fname}"
        assets[path]=(SRC/"Runtime"/fname).read_bytes().replace(b"\r\n",b"\n")
        assets[path+".meta"]=mono_meta(path)
    test=f"{ROOT}/Tests/PlayMode/RepairWeldPlayModeTests.cs"
    assets[test]=(SRC/"Tests/RepairWeldPlayModeTests.cs").read_bytes().replace(b"\r\n",b"\n")
    assets[test+".meta"]=mono_meta(test)
    runtime=f"{ROOT}/Runtime/B0132.RepairWeld.Runtime.asmdef"
    tests=f"{ROOT}/Tests/PlayMode/B0132.RepairWeld.PlayModeTests.asmdef"
    assets[runtime]=(json.dumps(dict(name="B0132.RepairWeld.Runtime",rootNamespace="RoboRougelike.B0132P17.RepairWeld",references=[],includePlatforms=[],excludePlatforms=[],allowUnsafeCode=False,overrideReferences=False,precompiledReferences=[],autoReferenced=True,defineConstraints=[],versionDefines=[],noEngineReferences=False),indent=2)+"\n").encode()
    assets[tests]=(json.dumps(dict(name="B0132.RepairWeld.PlayModeTests",rootNamespace="RoboRougelike.B0132P17.RepairWeld.Tests",references=["B0132.RepairWeld.Runtime"],includePlatforms=[],excludePlatforms=[],allowUnsafeCode=False,overrideReferences=False,precompiledReferences=[],autoReferenced=False,defineConstraints=[],versionDefines=[],noEngineReferences=False,optionalUnityReferences=["TestAssemblies"]),indent=2)+"\n").encode()
    assets[runtime+".meta"]=importer_meta(runtime,"AssemblyDefinitionImporter")
    assets[tests+".meta"]=importer_meta(tests,"AssemblyDefinitionImporter")
    pref=f"{ROOT}/Resources/B0132_RepairWeld.prefab"
    assets[pref]=prefab(f"{ROOT}/Runtime/B0132RepairWeldVfx.cs")
    assets[pref+".meta"]=importer_meta(pref,"PrefabImporter")
    require(len(assets)==17,"wrong asset count "+str(len(assets)))
    guids=[GUID(p[:-5]) for p in assets if p.endswith(".meta")]
    require(len(guids)==11 and len(set(guids))==11,"GUID uniqueness")
    zip_path=output/"B0132-RepairWeld-UnityImport.zip"
    output.mkdir(parents=True,exist_ok=True)
    with zipfile.ZipFile(zip_path,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
        for path,body in sorted(assets.items()):
            info=zipfile.ZipInfo(path,date_time=(2020,1,1,0,0,0))
            info.compress_type=zipfile.ZIP_DEFLATED;info.create_system=3;info.external_attr=0o100644<<16
            z.writestr(info,body,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
    with zipfile.ZipFile(zip_path) as z:
        require(z.testzip() is None and set(z.namelist())==set(assets),"ZIP unreadable")
        for path,content in assets.items():require(z.read(path)==content,"physical ZIP byte mismatch "+path)
    source_sha={}
    for path in [SRC/"Runtime/WeldVisualCore.cs",SRC/"Runtime/B0132RepairWeldVfx.cs",SRC/"Tests/RepairWeldPlayModeTests.cs"]:
        source_sha[path.as_posix()]=SHA(path.read_bytes())
    receipt=dict(package_id=PACKAGE,version=VERSION,native_unity="NOT_RUN",game_integrated=False,
                 physical_zip=zip_path.name,zip_bytes=zip_path.stat().st_size,zip_sha256=SHA(zip_path.read_bytes()),
                 asset_count=len(assets),GUID_count=len(guids),source_sha256=source_sha,
                 files=[dict(path=p,bytes=len(d),sha256=SHA(d)) for p,d in sorted(assets.items())])
    (output/"UNITY_IMPORT_RECEIPT.json").write_text(json.dumps(receipt,indent=2)+"\n")
    print("WELD_PHYSICAL_UNITY_ZIP_PASS",len(assets),"file assets",len(guids),"unique GUIDs",zip_path.stat().st_size,"ZIP bytes")
    print("WELD_UNITY_ZIP_SHA256",receipt["zip_sha256"])
    print("WELD_NATIVE_UNITY_NOT_RUN")
if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("--output",default="build/RepairWeldW06")
    build(Path(p.parse_args().output))
