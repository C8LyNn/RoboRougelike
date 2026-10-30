#!/usr/bin/env python3
"""Physical original Unity 6 Resources prefab for procedural dual-recoil, source-only CI.
Deterministic ZIP identical on Windows + Linux; never writes canonical Assets.
"""
import argparse, hashlib, json, re, zipfile
from pathlib import Path
PKG="B01-32-DUAL-RECOIL-ANIM-001"
VERSION="1.0.0"
ROOT="Assets/B0132DualRecoil"
SRC=Path("Tools/B01-32/P17/W07DualRecoil")
SHA=lambda b:hashlib.sha256(b).hexdigest()
GUID=lambda path:SHA((PKG+":"+VERSION+":"+path).encode())[:32]
def require(v,message):
    if not v:raise ValueError("DUAL_RECOIL_IMPORT_REJECT "+message)
def folder_meta(p):
    return (f"fileFormatVersion: 2\nguid: {GUID(p)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()
def mono_meta(p):
    return (f"fileFormatVersion: 2\nguid: {GUID(p)}\nMonoImporter:\n  externalObjects: {{}}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {{instanceID: 0}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()
def other_meta(p,kind):
    return (f"fileFormatVersion: 2\nguid: {GUID(p)}\n{kind}:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()
def prefab(source):
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
  m_Name: B0132_DualRecoil
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
""").replace("@GUID@",GUID(source)).encode()
def make(out):
    require(not Path(ROOT).exists(),"producer must not overwrite real Assets")
    paths={}
    for p in [ROOT,ROOT+"/Runtime",ROOT+"/Resources",ROOT+"/Tests",ROOT+"/Tests/PlayMode"]:
        paths[p+".meta"]=folder_meta(p)
    for name in ["DualRecoilCore.cs","B0132DualRecoilVisual.cs"]:
        p=ROOT+"/Runtime/"+name
        paths[p]=(SRC/"Runtime"/name).read_bytes().replace(b"\r\n",b"\n")
        paths[p+".meta"]=mono_meta(p)
    test=ROOT+"/Tests/PlayMode/DualRecoilPlayModeTests.cs"
    paths[test]=(SRC/"Tests"/"DualRecoilPlayModeTests.cs").read_bytes().replace(b"\r\n",b"\n")
    paths[test+".meta"]=mono_meta(test)
    main_asm=ROOT+"/Runtime/B0132.DualRecoil.Runtime.asmdef"
    test_asm=ROOT+"/Tests/PlayMode/B0132.DualRecoil.PlayModeTests.asmdef"
    paths[main_asm]=(json.dumps({
        "name":"B0132.DualRecoil.Runtime",
        "rootNamespace":"RoboRougelike.B0132P17.DualRecoil",
        "references":[],"includePlatforms":[],"excludePlatforms":[],
        "allowUnsafeCode":False,"overrideReferences":False,
        "precompiledReferences":[],"autoReferenced":True,
        "defineConstraints":[],"versionDefines":[],"noEngineReferences":False},indent=2)+"\n").encode()
    paths[test_asm]=(json.dumps({
        "name":"B0132.DualRecoil.PlayModeTests",
        "rootNamespace":"RoboRougelike.B0132P17.DualRecoil.Tests",
        "references":["B0132.DualRecoil.Runtime"],"includePlatforms":[],
        "excludePlatforms":[],"allowUnsafeCode":False,"overrideReferences":False,
        "precompiledReferences":[],"autoReferenced":False,
        "defineConstraints":[],"versionDefines":[],"noEngineReferences":False,
        "optionalUnityReferences":["TestAssemblies"]},indent=2)+"\n").encode()
    for p in [main_asm,test_asm]:paths[p+".meta"]=other_meta(p,"AssemblyDefinitionImporter")
    pf=ROOT+"/Resources/B0132_DualRecoil.prefab"
    paths[pf]=prefab(ROOT+"/Runtime/B0132DualRecoilVisual.cs")
    paths[pf+".meta"]=other_meta(pf,"PrefabImporter")
    require(len(paths)==17,"unexpected 17-member exact package size: "+str(len(paths)))
    guids=[GUID(p[:-5]) for p in paths if p.endswith(".meta")]
    require(len(guids)==11 and len(set(guids))==11,"11 importer metas unique")
    out.mkdir(parents=True,exist_ok=True)
    archive=out/"B0132-DualRecoil-UnityImport.zip"
    with zipfile.ZipFile(archive,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
        for path,bytes_ in sorted(paths.items()):
            i=zipfile.ZipInfo(path,date_time=(2020,1,1,0,0,0))
            i.compress_type=zipfile.ZIP_DEFLATED
            i.create_system=3
            i.external_attr=0o100644<<16
            z.writestr(i,bytes_,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
    with zipfile.ZipFile(archive) as z:
        require(z.testzip() is None,"real output CRC corruption")
        require(set(z.namelist())==set(paths),"incorrect ZIP inventory")
        for p,d in paths.items():require(z.read(p)==d,"physical bytes changed "+p)
    receipt={
        "package_id":PKG,"version":VERSION,"native_unity":"NOT_RUN",
        "game_integration":False,"import_namespace":ROOT,
        "source_paths":[str(SRC/"Runtime/DualRecoilCore.cs"),str(SRC/"Runtime/B0132DualRecoilVisual.cs"),str(SRC/"Tests/DualRecoilPlayModeTests.cs")],
        "zip_sha256":SHA(archive.read_bytes()),"zip_bytes":archive.stat().st_size,
        "actual_unity_files":len(paths),"meta_guid_count":len(guids),
        "file_hashes":[dict(path=p,sha256=SHA(d),length=len(d)) for p,d in sorted(paths.items())]
    }
    (out/"UNITY_IMPORT_RECEIPT.json").write_text(json.dumps(receipt,indent=2)+"\n")
    print("DUAL_RECOIL_REAL_UNITY_PACKAGE_PASS",len(paths),"physical Unity assets",len(guids),"original GUIDs")
    print("DUAL_RECOIL_REAL_ZIP_SHA256",receipt["zip_sha256"])
    print("NATIVE_UNITY_NOT_RUN / NO_CANONICAL_GAME_CHANGES")
if __name__=="__main__":
    ap=argparse.ArgumentParser()
    ap.add_argument("--output",default="build/DualRecoilW07")
    args=ap.parse_args();make(Path(args.output))
