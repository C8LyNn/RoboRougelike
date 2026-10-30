#!/usr/bin/env python3
"""Independent W24 physical Unity .unitypackage tar.gz binary receiver.
Never imports build_unitypackage. Independent exact W23 physical original
OBJ/MTL SHA, true 13 Unity entries/GUIDs/paths and no foreign overwrite,
2 actual original C# content checks, 8 intentionally invalid cases RED.
"""
import argparse,copy,gzip,hashlib,io,json,pathlib,re,tarfile,sys
ID="B01-31-W24-NATIVE-ARMOR-UNITYPACKAGE-001";VER="P17-W24-R1"
BASE="B0131ArmorModule1x2x2";ROOT="Assets/B01_31/W24ArmorStates";MODELS=ROOT+"/Models"
STATES=("OK","DAMAGED","DESTROYED")
HASHES={"OK":"bb5cef04d44497d2f2bb27c7f0bacc4acda8990e504200282352c3245c17c5d3",
"DAMAGED":"6575a6eea08b68951b6ef57c86aaf9ccfdb45a0abc3ca5a229901877c1d171f7",
"DESTROYED":"32950c0a822b9f063b0f8e4792fb31dc114463e6bc53d2a1daad46ae909d34f5"}
MTL="5b72c141dac3efd9bd56c2e1d6db3933d244c6608533b04693b8d87985cadc27"
TRIS={"OK":204,"DAMAGED":264,"DESTROYED":288}
SHA=lambda b:hashlib.sha256(b).hexdigest()
def need(x,msg):
    if not x:raise ValueError("W24_INDEPENDENT_ACTUAL_UNITY_ASSET_REJECT "+msg)
def inspect_tar(blob):
    need(blob[:2]==b"\x1f\x8b" and len(blob)>10000,"actual gzip unitypackage")
    stream=io.BytesIO(blob)
    members={}
    with tarfile.open(fileobj=stream,mode="r:gz") as t:
        names=t.getnames()
        need(len(names)==len(set(names)),"duplicate tar member names overwrite")
        for m in t:
            need(m.isfile() and not m.issym() and not m.islnk()
                 and m.uid==0 and m.gid==0 and m.mtime==0,
                 "package tar must only contain fixed metadata read-only files")
            need(bool(re.fullmatch("[a-f0-9]{32}/(pathname|asset.meta|asset)",m.name)),
                 "Unity .unitypackage paths must be opaque 32 hex GUID folder only")
            members[m.name]=t.extractfile(m).read()
    return members
def source_expected(repo,orig):
    previous=orig/"B0131W23"
    w23=json.loads((previous/"receipt.json").read_text())
    need(w23["package_id"]=="B01-31-W23-ARMOR-OBJ-IMPORT-BRIDGE-001"
         and w23["version"]=="P17-W23-R1" and w23["real_distribution_zip_members"]==8,
         "original exact W23 source receipt not authentic")
    listed={r["name"]:r["sha256"] for r in w23["files"]}
    files={}
    for st in STATES:
        filename=BASE+"-"+st+"-W23.obj";data=(previous/filename).read_bytes()
        need(SHA(data)==HASHES[st]==listed[filename],
             "original immutable W23 OBJ SHA changed")
        need(sum(line.startswith(b"f ") for line in data.splitlines())==TRIS[st],
             "original W23 actual triangle positions missing")
        files[MODELS+"/"+filename]=data
    filename=BASE+"-W23-shared.mtl";data=(previous/filename).read_bytes()
    need(SHA(data)==MTL==listed[filename] and data.count(b"newmtl ")==6,
         "actual original six glTF names MTL changed")
    files[MODELS+"/"+filename]=data
    for filename in ("original-pbr-materials.json",
                     BASE+"-W23-iso-three-states.png",
                     BASE+"-W23-face-three-states.png"):
        data=(previous/filename).read_bytes()
        need(SHA(data)==listed[filename],"physical original W23 receipt or PNG changed")
        files[MODELS+"/"+filename]=data
    for name in (ROOT+"/B0131W24ArmorDamageVisual.cs",
                 ROOT+"/Editor/B0131W24CreateArmorVisualPrefab.cs"):
        files[name]=(repo/name).read_bytes()
    folders=("Assets/B01_31",ROOT,MODELS,ROOT+"/Editor")
    for name in folders:files[name]=None
    need(len(files)==13,"physical whole Unity original asset set")
    return files
def entrymap(members):
    bypath={};guids={}
    for name,val in members.items():
        guid,typ=name.split("/")
        if typ!="pathname":continue
        path=val.decode("utf-8").rstrip("\n")
        need(val==(path+"\n").encode() and path.startswith("Assets/B01_31/")
             and ".." not in path and "\\" not in path and "//" not in path,
             "no absolute, traversing or foreign Unity assets")
        need(guid not in guids and path not in bypath,
             "no duplicate Unity GUID/pathname collisions")
        guids[guid]=path
        bypath[path]=guid
    need(len(bypath)==13 and len(guids)==13,"exact 13 Unity source path/GUID pairs")
    need(len(members)==len(bypath)*3-4,
         "real package file path/meta/asset pairs and four folder path/meta entries")
    for path,guid in bypath.items():
        m=members.get(guid+"/asset.meta")
        need(m is not None and re.search(rb"(?m)^guid: "+guid.encode()+rb"$",m),
             "native .meta GUID mismatch")
        if path in ("Assets/B01_31","Assets/B01_31/W24ArmorStates",
                    MODELS,ROOT+"/Editor"):
            need(b"folderAsset: yes" in m and guid+"/asset" not in members,
                 "real Unity folder metadata only, no arbitrary serialized source")
        else:
            need(guid+"/asset" in members,"all original Unity source bytes embedded")
    return bypath
def check_entries(members,expected,repo):
    paths=entrymap(members)
    need(set(paths)==set(expected),"actual expected W23+two scripts and folders exactly")
    for path,raw in expected.items():
        guid=paths[path]
        metaref=repo/(path+".meta")
        if metaref.is_file():
            need(members[guid+"/asset.meta"]==metaref.read_bytes(),
                 "exact checked-in own runtime C# and folder GUID")
        if raw is not None:
            need(members[guid+"/asset"]==raw,"physically embedded original Unity source bytes changed")
    native=members[paths[ROOT+"/B0131W24ArmorDamageVisual.cs"]+"/asset"]
    editor=members[paths[ROOT+"/Editor/B0131W24CreateArmorVisualPrefab.cs"]+"/asset"]
    need(b"SetVisualPhase" in native and b"ConfigureVisuals" in native
         and b"ArmorVisualPhase" in native and
         b"Never" not in native[:0] and
         b"File.OpenRead" in editor and
         b"EditorUtility.DisplayDialog" in editor and
         b"PrefabUtility.SaveAsPrefabAsset" in editor and
         b"AssetDatabase.LoadAssetAtPath<GameObject>" in editor and
         b"File.Exists(Output)" in editor,
         "genuine native visual only component and manual opt-in source")
    need(b"AssetDatabase.DeleteAsset" not in editor and
         b"AssetDatabase.DeleteAsset" not in native and
         b"DeleteDirectory" not in editor and
         b"UnityEditor" not in native,
         "no destructive, native runtime editor reference or auto-asset deletion")
    for state in STATES:
        asset=MODELS+"/"+BASE+"-"+state+"-W23.obj"
        data=members[paths[asset]+"/asset"]
        need(SHA(data)==HASHES[state],"source exact original OBJ always required")
        need(sum(l.startswith(b"f ") for l in data.splitlines())==TRIS[state],
             "source real OBJ faces preserved")
    return paths
def bad_inputs(members,expected,repo):
    paths=entrymap(members)
    target=MODELS+"/"+BASE+"-DAMAGED-W23.obj"
    g=paths[target]
    bad=[]
    def changed(key,value):
        n=dict(members);n[key]=value;return n
    bad.append(changed(g+"/asset",members[g+"/asset"]+b"# mutated\n"))
    bad.append(changed(g+"/pathname",b"Assets/Pack/ALTERNATE.obj\n"))
    bad.append(changed(g+"/pathname",b"Assets/B01_31/../Pack/foreign.obj\n"))
    bad.append(changed(g+"/asset.meta",members[g+"/asset.meta"].replace(g.encode(),b"0"*32)))
    n=dict(members);del n[g+"/asset"];bad.append(n)
    n=dict(members);n["f"*32+"/asset"]=b"INVALID EXTERNAL";bad.append(n)
    c=paths[ROOT+"/B0131W24ArmorDamageVisual.cs"]
    bad.append(changed(c+"/asset",b"public class FakeRuntime{}"))
    e=paths[ROOT+"/Editor/B0131W24CreateArmorVisualPrefab.cs"]
    bad.append(changed(e+"/asset",members[e+"/asset"].replace(b"EditorUtility.DisplayDialog",b"NO_CONSENT_DIALOG")))
    rejects=0
    for i,test in enumerate(bad):
        try:check_entries(test,expected,repo)
        except (ValueError,KeyError,UnicodeError):rejects+=1
        else:raise AssertionError("W24 false GREEN mutated Unity package member "+str(i))
    need(rejects==8,"8 physically tampered Unity native source examples rejected")
    return rejects
def verify(repo,original,out):
    originalfiles=source_expected(repo,original)
    package=(out/"B0131W24-Armor-ThreeStates-NATIVE-OBJ-Visual.unitypackage").read_bytes()
    receipt=json.loads((out/"receipt.json").read_text())
    need(receipt["package_id"]==ID and receipt["version"]==VER and
         receipt["actual_unitypackage_SHA256"]==SHA(package) and
         receipt["actual_unitypackage_bytes"]==len(package) and
         receipt["original_immutable_W23_OBJ_SHA256"]==HASHES and
         receipt["original_immutable_W23_MTL_SHA256"]==MTL and
         receipt["native_Unity_G1"]=="NOT_RUN" and
         receipt["formal_G0"]=="PENDING" and
         receipt["G2"]=="HOLD" and receipt["integrated"] is False,
         "true original W24 physical receipt lineage and honest gates")
    members=inspect_tar(package)
    paths=check_entries(members,originalfiles,repo)
    need(receipt["real_Unity_asset_paths_to_GUIDs"]==paths and
         len(set(paths.values()))==13,
         "real manifest maps same 13 Unity GUID paths without collision")
    rejects=bad_inputs(members,originalfiles,repo)
    print(json.dumps({"status":"PASS",
      "real_importable_Unity_targz_package_SHA256":SHA(package),
      "physical_Unity_package_paths":len(paths),
      "physically_embedded_immutable_W23_OBJ_source_faces":TRIS,
      "real_native_CSharp_sources":2,"real_4_Unity_folder_meta_GUIDs":4,
      "unique_32hex_asset_GUIDs":len(set(paths.values())),
      "actual_directory_traversal_and_overwrite_MUTANTS_RED":rejects,
      "Unity_native":"NOT_RUN"}))
if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--repo",required=True);p.add_argument("--original",required=True)
    p.add_argument("--output",required=True);a=p.parse_args()
    try:verify(pathlib.Path(a.repo),pathlib.Path(a.original),pathlib.Path(a.output))
    except Exception as e:
        print("W24_INDEPENDENT_UNITY_PACKAGE_RED "+type(e).__name__+": "+str(e),file=sys.stderr);raise
