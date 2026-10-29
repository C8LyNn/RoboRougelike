#!/usr/bin/env python3
"""Source-only Unity importer metadata and original physical candidate guard.
Never mislabel this as Unity Editor compiled, native PhysX executed, or socket approved.
"""
import pathlib,re,json,sys
R=pathlib.Path(".")
OWN={
"Assets/PARTSIZE001/P17NativeProbe.meta":"6cd35d13ef2e4f16a1a26a0c0b550001",
"Assets/PARTSIZE001/P17NativeProbe/Editor.meta":"6cd35d13ef2e4f16a1a26a0c0b550002",
"Assets/PARTSIZE001/P17NativeProbe/Editor/B01.PARTSIZE001.P17NativeProbe.Editor.asmdef.meta":"6cd35d13ef2e4f16a1a26a0c0b550003",
"Assets/PARTSIZE001/P17NativeProbe/Editor/BracingPhysicalContract.cs.meta":"6cd35d13ef2e4f16a1a26a0c0b550004",
"Assets/PARTSIZE001/P17NativeProbe/Editor/BracingNativeColliderProbe.cs.meta":"6cd35d13ef2e4f16a1a26a0c0b550005",
}
RX=re.compile(r"^guid: ([0-9a-f]{32})$",re.M)
def ensure(value,message):
    if not value:raise ValueError(message)
def audit():
    for path,guid in OWN.items():
        meta=(R/path).read_text()
        ensure(RX.findall(meta)==[guid],"UNPINNED_UNITY_META:"+path)
        target=R/path[:-5]
        ensure(target.exists(),"MISSING_NATIVE_EDITOR_SOURCE:"+path)
        if path.endswith("Editor.meta") or path.endswith("P17NativeProbe.meta"):
            ensure(target.is_dir() and "folderAsset: yes" in meta,"BAD_FOLDER_META")
        elif path.endswith(".asmdef.meta"):
            ensure("AssemblyDefinitionImporter:" in meta and target.is_file(),"BAD_ASMDEF_META")
        else:
            ensure("MonoImporter:" in meta and target.is_file(),"BAD_CSHARP_META")
    count={g:[] for g in OWN.values()}
    for p in (R/"Assets").rglob("*.meta"):
        for guid in RX.findall(p.read_text(encoding="utf8")):
            if guid in count:count[guid].append(p.as_posix())
    ensure(all(count[g]==[path] for path,g in OWN.items()),"PROJECT_GUID_COLLISION")
    asm=json.loads((R/"Assets/PARTSIZE001/P17NativeProbe/Editor/B01.PARTSIZE001.P17NativeProbe.Editor.asmdef").read_text())
    ensure(asm["includePlatforms"]==["Editor"] and not asm["autoReferenced"] and
           asm["references"]==["B01.PARTSIZE001.Runtime"],"NOT_EDITOR_ISOLATED")
    code=(R/"Assets/PARTSIZE001/P17NativeProbe/Editor/BracingNativeColliderProbe.cs").read_text()
    for term in [
        "Physics.ComputePenetration","PartSizeNativePhysics.GridBounds",
        "PrefabUtility.InstantiatePrefab","PrefabUtility.IsPartOfPrefabAsset",
        "AssetDatabase.AssetPathToGUID","GetComponentsInChildren<Collider>(true)",
        "EditorSceneManager.NewScene","EditorSceneManager.CloseScene",
        'typedSocketStatus="NOT_RUN_NO_CODEX_AUTHORITY"',
        'movementSweepsStatus="NOT_RUN"',
        'damageDetachStatus="NOT_RUN"',
        'playerBuildStatus="NOT_RUN"']:
        ensure(term in code,"MISSING_ACTUAL_NATIVE_FALLOUT_GATE:"+term)
    for unsafe in ["GameObject.CreatePrimitive(","AddComponent<BoxCollider>(",
                   "MenuItem(\"B01/P17 Native Physical Probe/Create Free Game Vehicle"]:
        ensure(unsafe not in code,"DUMMY_OR_UNAUTHORISED_SOURCE:"+unsafe)
    manifests=["Flanker","Bulwark","Skirmisher"]
    counts=[15,16,12]
    actual=[]
    for name,n in zip(manifests,counts):
        path=R/f"Assets/PARTSIZE001/Bracing/P17/P17_{name}_Reinforced.json"
        data=json.loads(path.read_text())
        ensure(len(data["parts"])==n and
               data["nativePhysicalStatus"]=="UNMEASURED_COLLIDER_AND_SOCKET",
               "UNTRUSTED_OR_FAKE_BRACING_NATIVE_EVIDENCE")
        actual.extend(data["parts"])
    ensure(len(actual)==43 and sum(p["localId"].startswith("P17-BRACE-") for p in actual)==5,
           "BRACE_LAYOUT_SCOPE_DRIFT")
    for malformed in ["guid: ERROR","guid: "+"a"*31,"guid: "+"b"*32+"\nguid: "+"c"*32]:
        ensure(len(RX.findall(malformed))!=1,"BAD_GUID_MUTATION_FALSE_GREEN")
    return len(OWN),len(actual),3
if __name__=="__main__":
    try:
        meta,owners,negative=audit()
        print(f"B01_23_P17_NATIVE_PROBE_SOURCE_META_PASS {meta}/{meta} GUID, {owners}/{owners} real source owners, {negative}/{negative} mutated GUID; UNITY_EDITOR_COMPILE_NOT_RUN; PHYSX_NOT_RUN")
    except (OSError,ValueError,KeyError) as e:
        print("B01_23_P17_NATIVE_PROBE_META_FAIL "+str(e),file=sys.stderr)
        raise SystemExit(1)
