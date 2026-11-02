#!/usr/bin/env python3
"""B01-62 W28: forbid rewriting #797, pin original #780 V7, prove new ABI path."""
import hashlib,json,subprocess,sys
from pathlib import Path
BASE="2b7983f0f9e09dd1d56906b379a3f7af7b02f21a"
V7="b4df10a7ad7e6c758f8331013f6b216b3a3689a3"
OLD="Tools/B01/B01-62/P17W25WholeGameEconomyV7/"
NEW="Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/"
FILES={"ApprovedEconomyPolicyV7.cs":("Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs","53ebce6c07388dc9c8a8ed79b6de4d3ccbee4773"),
"CanonicalEconomySectionV7.cs":("Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs","d8b71de91db36295b6220e9c4baad71eb54455f6")}
OWN=("WholeGameEconomySlotV3.cs","CanonicalWholeGameSessionV3.cs","Program.cs",
"WholeGameEconomyUnityV3QA.csproj","WholeGameEconomyUnityV3Abi.csproj",
"source_guard.py","README.md","manifest.json")
WORKFLOW=".github/workflows/b01-62-w28-unity-compatible-whole-game-v3.yml"
def git(*args):return subprocess.check_output(["git",*args],stderr=subprocess.PIPE).decode().strip()
def main(dest):
    base=Path(__file__).parent; out=Path(dest)
    if out.exists():raise AssertionError("REFUSE_OCCUPIED_RECEIPT")
    subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],check=True)
    expected={NEW+x for x in OWN}|{WORKFLOW}
    changed=set(git("diff","--name-only",BASE,"HEAD").splitlines())
    if changed!=expected:raise AssertionError("W28_NOT_ONLY_9_NEW_FILES:"+repr(changed^expected))
    for path in (OLD+"WholeGameEconomySlotV2.cs",OLD+"CanonicalWholeGameSessionV2.cs",
                 "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs",
                 "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs",
                 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs"):
        if git("rev-parse",BASE+":"+path)!=git("rev-parse","HEAD:"+path):
            raise AssertionError("ORIGINAL_SOURCE_MODIFIED:"+path)
    for filename,(path,blob) in FILES.items():
        if git("rev-parse",V7+":"+path)!=blob:raise AssertionError("WRONG_V7_PRODUCER")
        data=(base/".source"/filename).read_bytes()
        if hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()!=blob:
            raise AssertionError("ORIGINAL_V7_BYTES_CHANGED:"+filename)
    slot=(base/"WholeGameEconomySlotV3.cs").read_text()
    session=(base/"CanonicalWholeGameSessionV3.cs").read_text()
    for forbidden in ("SHA256.HashData(","Convert.ToHexString(","File.Move(staging,authoritative,true)","File.Delete(authoritative)"):
        if forbidden in slot+session:raise AssertionError("UNITY_API_OR_UNSAFE_REPLACE:"+forbidden)
    for required in ("File.Replace(staging,authoritative,null)","File.Move(staging,authoritative)",
      "file.Flush(true)","FileOptions.WriteThrough","CanonicalEconomySectionV7.TryEncode",
      "CanonicalEconomySectionV7.TryDecode","B0162G02","slot.canonical-game-v2",
      "StringComparison.Ordinal","UnitySha256.Hash(data,0,data.Length-32)"):
        if required not in slot:raise AssertionError("LOST_V2_ATOMIC_CONTRACT:"+required)
    if "UnitySha256.LowerHex(UnitySha256.Hash(data))" not in session:
        raise AssertionError("SESSION_HASH_ABI_NOT_FIXED")
    obj={"package":"B01-62-W28-UNITY-ABI-WHOLE-GAME-V3","original_797":BASE,
         "economy_780":V7,"economy_producer_original_blobs":FILES,
         "changed_only_new_files":sorted(expected),"disk_magic":"B0162G02",
         "marker":"slot.canonical-game-v2","one_file":"save.envelope",
         "owner":"original #729 .owner","real_unity_g1":"NOT_RUN",
         "independent_G0":"PENDING","merged":False}
    raw=(json.dumps(obj,sort_keys=True,indent=2)+"\n").encode()
    out.write_bytes(raw)
    print("B0162_W28_PINNED_ORIGINAL_797_780=PASS")
    print("B0162_W28_ADD_ONLY_9_PATHS=PASS")
    print("B0162_W28_RECEIPT_SHA256="+hashlib.sha256(raw).hexdigest())
if __name__=="__main__":main(sys.argv[1])
