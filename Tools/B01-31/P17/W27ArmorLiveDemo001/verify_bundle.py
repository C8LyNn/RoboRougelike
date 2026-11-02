#!/usr/bin/env python3
"""Independent W27 original native Unity package reader; doesn't import author.
Exact byte-for-byte original 8 asset/20 tar members, 8 negative hostile cases,
physical source ABI immutable P12/W24/W25, actual PlayMode UnityTest methods
and explicit opt-in additive demo scene factory. NOT Unity Editor execution.
"""
from __future__ import annotations
import argparse,hashlib,io,json,pathlib,re,tarfile,sys
ROOT="Assets/B01_31/W27ArmorLiveDemo"
PACKAGE="B01-31-W27-REAL-P12-ARMOR-PLAYMODE-DEMO-001"
SOURCES={
"Assets/Pack/P12_PipelineSafeDamageable.cs":"544bed274b616b91fd98c45d2b194b2f3a2d112a",
"Assets/B01_31/W24ArmorStates/B0131W24ArmorDamageVisual.cs":"8cf3664b9b6e5b0fd6623be7c3f4e72fd9164664",
"Assets/B01_31/W25P12DamageVisualBinding/B0131W25P12DamageVisualBinding.cs":"e1622abf22319e4448b53a0630d40387ade7a2b2"
}
FOLDERS={ROOT,ROOT+"/Editor",ROOT+"/Tests",ROOT+"/Tests/PlayMode"}
ASSETS=FOLDERS|{
ROOT+"/B0131W27ArmorLiveDemoDriver.cs",
ROOT+"/Editor/B0131W27CreateArmorLiveDemoScene.cs",
ROOT+"/Tests/PlayMode/B0131W27ArmorRealP12PlayModeTests.cs",
ROOT+"/Tests/PlayMode/RoboRougelike.B0131.W27.NativePlayModeTests.asmdef"
}
sha=lambda b:hashlib.sha256(b).hexdigest()
def blob(b):return hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()
def need(x,m):
    if not x:raise ValueError("W27_INDEPENDENT_REAL_UNITY_RECEIVER_RED: "+m)
def tarread(raw):
    need(raw[:2]==b"\x1f\x8b" and len(raw)>1500,"real package gzip bytes")
    contents={}
    with tarfile.open(fileobj=io.BytesIO(raw),mode="r:gz") as tar:
        for item in tar:
            need(item.isfile() and not item.issym() and not item.islnk() and
                 re.fullmatch(r"[0-9a-f]{32}/(asset|asset.meta|pathname)",item.name)
                 and item.name not in contents,
                 "no unsafe tar symlink, duplicate, escape path or unknown types")
            contents[item.name]=tar.extractfile(item).read()
    need(len(contents)==20,"precisely 4 folder×2 + 4 own assets×3 actual tar members")
    return contents
def inspect(entries,repo):
    mapping={}
    for name,raw in entries.items():
        guid,kind=name.split("/")
        if kind!="pathname":continue
        path=raw.decode().rstrip("\n")
        need(raw==(path+"\n").encode() and path in ASSETS and
             "\\" not in path and ".." not in path,"original named source whitelist")
        need(path not in mapping,"no path collision")
        mapping[path]=guid
    need(len(mapping)==len(ASSETS)==8 and len(set(mapping.values()))==8,
         "8 unique owned paths/GUID")
    valid=set()
    for path,guid in mapping.items():
        valid|={guid+"/pathname",guid+"/asset.meta"}
        meta=(repo/(path+".meta")).read_bytes()
        need(entries[guid+"/asset.meta"]==meta and
             ("guid: "+guid).encode() in meta,"original source .meta GUID and binary exactness")
        if path in FOLDERS:
            need(b"folderAsset: yes" in meta and guid+"/asset" not in entries,
                 "original Unity folder path contains no asset")
        else:
            valid.add(guid+"/asset")
            need(entries[guid+"/asset"]==(repo/path).read_bytes(),
                 "actual original source CSharp/asmdef bytes preserved")
    need(set(entries)==valid,"must reject any extra unowned/orphan/foreign tar member")
    code=entries[mapping[ROOT+"/B0131W27ArmorLiveDemoDriver.cs"]+"/asset"]
    need(b"testPart.ApplyDamage(" in code and b"testPart.RestoreHealthRatio(" in code
         and b"IsReady" in code and b"OnGUI" in code,
         "genuine user-playable original P12 damage driver, not shader-only mockup")
    scene=entries[mapping[ROOT+"/Editor/B0131W27CreateArmorLiveDemoScene.cs"]+"/asset"]
    need(b"EditorUtility.DisplayDialog(" in scene and
         b"NewSceneSetup.EmptyScene, NewSceneMode.Additive" in scene and
         b"AssetDatabase.LoadAssetAtPath<GameObject>(W24Prefab)" in scene and
         b"PrefabUtility.InstantiatePrefab(" in scene and
         b"SaveScene(created, Output" in scene and
         b"File.Exists(Output)" in scene and
         b"File.Exists(Output + \\".meta\\")" in scene,
         "real manual opt-in new scene builder without overwrite")
    test=entries[mapping[ROOT+"/Tests/PlayMode/B0131W27ArmorRealP12PlayModeTests.cs"]+"/asset"]
    need(test.count(b"[UnityTest]")==5 and
         b"Call(t.source, \\"ApplyDamage\\"" in test and
         b"Call(t.source, \\"RestoreHealthRatio\\"" in test and
         b"RuntimeType(" in test and b"UnityTearDown" in test and
         b"Assembly-CSharp" in test and b"SetDamageThreshold" in test,
         "five genuine original P12/W24/W25 reflective Unity NUnit tests")
    asm=json.loads(entries[mapping[ROOT+"/Tests/PlayMode/RoboRougelike.B0131.W27.NativePlayModeTests.asmdef"]+"/asset"])
    need(asm["name"]=="RoboRougelike.B0131.W27.NativePlayModeTests"
         and asm["references"]==[] and "UNITY_INCLUDE_TESTS" in asm["defineConstraints"]
         and "TestAssemblies" in asm["optionalUnityReferences"],
         "test assembly safe, no unresolvable Assembly-CSharp direct references")
    return mapping
def hostile_negative(entries,repo):
    mapping=inspect(entries,repo)
    p=ROOT+"/B0131W27ArmorLiveDemoDriver.cs";guid=mapping[p]
    def patch(path,new):
        r=dict(entries);r[path]=new;return r
    case=[
      patch(guid+"/asset",entries[guid+"/asset"]+b"// mutated\n"),
      patch(guid+"/pathname",b"Assets/Pack/FOREIGN.cs\n"),
      patch(guid+"/pathname",b"Assets/B01_31/W27ArmorLiveDemo/../W25P12DamageVisualBinding/FALSE.cs\n"),
      patch(guid+"/asset.meta",entries[guid+"/asset.meta"].replace(guid.encode(),b"f"*32)),
      patch(guid+"/asset",b"public class FakeNative{}"),
      patch(guid+"/pathname",b"Assets/B01_31/W27ArmorLiveDemo\\\\WRONG.cs\n"),
    ]
    removed=dict(entries);del removed[guid+"/asset"];case.append(removed)
    added=dict(entries);added["f"*32+"/asset"]=b"FOREIGN";case.append(added)
    count=0
    for n,attempt in enumerate(case):
        try:inspect(attempt,repo)
        except (ValueError,KeyError,UnicodeError,json.JSONDecodeError):count+=1
        else:raise AssertionError("FALSE GREEN original corrupt unitypackage tar "+str(n))
    need(count==8,"8 corrupted Unity-package payloads rejected")
    return count
def main(repo,out):
    for path,digest in SOURCES.items():
        need(blob((repo/path).read_bytes())==digest,"original selected P12/W24/W25 ABI changed")
    path=out/"B0131W27-P12-Armor-LiveDemo-PlayMode-DELTA.unitypackage"
    raw=path.read_bytes()
    rec=json.loads((out/"receipt.json").read_text())
    need(rec["package_id"]==PACKAGE and rec["version"]=="P17-W27-R1"
         and rec["w25_parent"]=="dfff0aeaa7098a16621387557a04836bf873fd73"
         and rec["real_unitypackage_sha256"]==sha(raw)
         and rec["real_unitypackage_bytes"]==len(raw)
         and rec["exact_original_source_Git_blobs"]==SOURCES
         and rec["new_only_unity_assets"]==8 and rec["actual_tar_members"]==20
         and rec["native_unity_G1"]=="NOT_RUN"
         and rec["formal_G0"]=="PENDING"
         and rec["integrated"] is False,
         "real physical package receipt + honest original-SHA gates")
    entries=tarread(raw)
    mapping=inspect(entries,repo)
    need(rec["source_member_bytes_and_GUID"] and
         len(rec["source_member_bytes_and_GUID"])==8 and
         all(next(x for x in rec["source_member_bytes_and_GUID"] if x["path"]==p)["guid"]==guid
             for p,guid in mapping.items()),
         "original native Unity GUID manifest matches physical output")
    negative=hostile_negative(entries,repo)
    print(json.dumps({"status":"PASS","original_P12_W24_W25_Git_blobs":True,
       "real_Unitypackage_SHA256":sha(raw),
       "source_real_Unity_GUID_assets":len(mapping),
       "actual_TAR_members":len(entries),
       "five_native_Unity_PlayMode_tests_AUTHORED_NOT_RUN":5,
       "malformed_unitypackage_mutations_rejected":negative,
       "Unity_native":"NOT_RUN"}))
if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("--repo",required=True);p.add_argument("--out",required=True)
    a=p.parse_args()
    try:main(pathlib.Path(a.repo),pathlib.Path(a.out))
    except Exception as exc:print("W27_INDEPENDENT_RECEIVER_FAILED "+str(exc),file=sys.stderr);raise
