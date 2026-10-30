#!/usr/bin/env python3
"""Independent W25 incremental Unity package binary receiver; no producer import.
Checks exact P12 and W24 original Git blobs, 3 native Unity GUIDs, 8 tar
members, original W25 runtime bytes, no foreign sources and 8 negative mutants.
"""
import argparse,hashlib,io,json,pathlib,re,tarfile,sys
ID="B01-31-P17W25-REAL-P12-ARMOR-VISUAL-BINDING-001"
ROOT="Assets/B01_31/W25P12DamageVisualBinding"
P12="Assets/Pack/P12_PipelineSafeDamageable.cs"
W24="Assets/B01_31/W24ArmorStates/B0131W24ArmorDamageVisual.cs"
P12_BLOB="544bed274b616b91fd98c45d2b194b2f3a2d112a"
W24_BLOB="8cf3664b9b6e5b0fd6623be7c3f4e72fd9164664"
sha=lambda b:hashlib.sha256(b).hexdigest()
def blob(b):return hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()
def need(x,msg):
    if not x:raise ValueError("W25_INDEPENDENT_REAL_DELTA_REJECT "+msg)
def readpkg(data):
    need(len(data)>1000 and data[:2]==b"\x1f\x8b","actual Unity gzip tar payload")
    entries={}
    with tarfile.open(fileobj=io.BytesIO(data),mode="r:gz") as tar:
        for member in tar:
            need(member.isfile() and not member.issym() and not member.islnk(),
                 "no tar folder/symlink injection")
            need(re.fullmatch("[a-f0-9]{32}/(asset|asset.meta|pathname)",member.name)
                 and member.name not in entries,
                 "safe unique Unity GUID pathname/asset/meta")
            entries[member.name]=tar.extractfile(member).read()
    need(len(entries)==8,"3 native Unity assets = 8 original members")
    return entries
def examine(entries,repo,strict=True):
    paths={}
    for name,contents in entries.items():
        guid,typ=name.split("/")
        if typ!="pathname":continue
        path=contents.decode().rstrip("\n")
        need(contents==(path+"\n").encode() and
             (path==ROOT or path.startswith(ROOT+"/"))
             and ".." not in path and "\\" not in path,
             "no foreign or unsafe original Unity asset path")
        need(path not in paths,"no duplicate Unity asset source paths")
        paths[path]=guid
    expected={ROOT,ROOT+"/B0131W25P12DamageVisualBinding.cs",ROOT+"/README.md"}
    need(set(paths)==expected and len(set(paths.values()))==3,
         "ONLY W25 new binder and README, never old original P12 or W24 copies")
    for path,guid in paths.items():
        original=repo/(path+".meta")
        need(entries.get(guid+"/asset.meta")==original.read_bytes()
             and ("guid: "+guid).encode() in original.read_bytes(),
             "real checked-in original Unity source .meta SHA and GUID")
        if path==ROOT:
            need(guid+"/asset" not in entries and b"folderAsset: yes" in original.read_bytes(),
                 "Unity module folder metadata only")
        else:
            source=(repo/path).read_bytes()
            need(entries.get(guid+"/asset")==source,
                 "real standalone W25 linked CSharp/README bytes unchanged")
    code=entries[paths[ROOT+"/B0131W25P12DamageVisualBinding.cs"]+"/asset"]
    need(b"source.Died += OnSourceDied;" in code and
         b"source.Died -= OnSourceDied;" in code and
         b"source.CurrentHp" in code and b"source.MaxHp" in code and
         b"source.IsDead" in code and
         b"visual.SetVisualPhase(" in code and
         b"ApplyDamage(" not in code and b"Save" not in code.split(b"private bool subscribed")[1].split(b"public bool IsBound")[0],
         "new actual binder must remain read-only, subscribe/unsubscribe and never call P12 damage")
    return paths
def reject_mutations(entries,repo):
    paths=examine(entries,repo)
    source=ROOT+"/B0131W25P12DamageVisualBinding.cs"
    guid=paths[source]
    meta=entries[guid+"/asset.meta"]
    test=[]
    def patched(p,value):
        d=dict(entries);d[p]=value;return d
    test.append(patched(guid+"/asset",entries[guid+"/asset"]+b"\n// hostile extra"))
    test.append(patched(guid+"/asset.meta",meta.replace(guid.encode(),b"f"*32)))
    test.append(patched(guid+"/pathname",b"Assets/Pack/rewritten.cs\n"))
    test.append(patched(guid+"/pathname",b"Assets/B01_31/W25P12DamageVisualBinding/../old.cs\n"))
    test.append(patched(guid+"/pathname",b"Assets/B01_31/W25P12DamageVisualBinding\\\\bad.cs\n"))
    d=dict(entries);del d[guid+"/asset"];test.append(d)
    d=dict(entries);d["f"*32+"/asset"]=b"FOREIGN";test.append(d)
    test.append(patched(guid+"/asset",entries[guid+"/asset"].replace(b"source.Died -= OnSourceDied;",b"// unsubscribe missing")))
    rejected=0
    for specimen in test:
        try:examine(specimen,repo)
        except (ValueError,KeyError,UnicodeError):rejected+=1
        else:raise AssertionError("W25 false GREEN malicious Unity binary member")
    need(rejected==8,"8 real corrupted Unity delta assets denied")
    return rejected
def main(repo,out):
    need(blob((repo/P12).read_bytes())==P12_BLOB,
         "original main P12 real damageable source SHA unchanged")
    need(blob((repo/W24).read_bytes())==W24_BLOB,
         "original W24 visual component source SHA unchanged")
    raw=(out/"B0131W25-P12-DamageToArmorVisual-DELTA.unitypackage").read_bytes()
    receipt=json.loads((out/"receipt.json").read_text())
    need(receipt["package_id"]==ID and receipt["version"]=="P17-W25-R1"
         and receipt["unitypackage_SHA256"]==sha(raw)
         and receipt["unitypackage_size"]==len(raw)
         and receipt["P12_original_Git_blob_SHA"]==P12_BLOB
         and receipt["W24_original_Git_blob_SHA"]==W24_BLOB
         and receipt["native_Unity_G1"]=="NOT_RUN"
         and receipt["formal_G0"]=="PENDING"
         and receipt["G2"]=="HOLD" and receipt["integrated"] is False,
         "exact original W25 physical SHA/provenance/gate receipt")
    entries=readpkg(raw)
    paths=examine(entries,repo)
    need(receipt["owned_three_GUID_path_entries"]==paths,
         "three real source native Unity GUIDs match written receipt")
    negatives=reject_mutations(entries,repo)
    print(json.dumps({"status":"PASS","actual_real_Unitypackage_SHA256":sha(raw),
         "actual_3_source_GUID_assets":len(paths),"actual_TAR_members":len(entries),
         "no_original_P12_or_W24_duplicate_import":True,
         "real_malicious_binary_mutations_rejected":negatives,
         "original_P12_and_W24_Git_blobs":True,
         "Unity_native":"NOT_RUN"}))
if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--repo",required=True);p.add_argument("--output",required=True)
    a=p.parse_args()
    try:main(pathlib.Path(a.repo),pathlib.Path(a.output))
    except Exception as exc:
        print("W25_INDEPENDENT_DELTA_RED "+str(exc),file=sys.stderr);raise
