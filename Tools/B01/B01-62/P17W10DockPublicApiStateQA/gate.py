#!/usr/bin/env python3
"""B01-62 W10: independent exact-original Git source release gate for safe docking.
No Unity/PhysX execution; never treats an opt-in GUI guard as a public-API guard.
"""
import argparse
import hashlib
import json
import re
import subprocess
from pathlib import Path

PRODUCER_SHA = "763748f1be0c61356fb4462144640bb359364226"
ORIGINALS = {
    "w07": ("Assets/B0123DebrisPlayground001/Runtime/DebrisRigPlayground.cs", "e4e31c32cb5abda2fef987efa4b45fd61e828312"),
    "w08": ("Assets/B0123ReattachClearance001/Runtime/DebrisSafeReattachGate.cs", "7c7e96a10d69149119309863a0c2a9bdab829072"),
    "w09": ("Assets/B0123DockHardening001/Runtime/HardenedDockDemo.cs", "d46c6341db199d010f48de00f98250becd2d7c87"),
    "policy": ("Assets/B0123DockHardening001/Runtime/HardenedDockPolicy.cs", "6c7498a35ac49d9d49638b3b2550852547c67339"),
    "installer": ("Assets/B0123DockHardening001/Editor/HardenedDockInstaller.cs", "5a503c93c64e4e016bfd4b0e97feb6716af62ad8"),
    "ledger": ("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs", "fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
}
class GateError(ValueError):
    pass

def blob_sha(data):
    return hashlib.sha1(b"blob " + str(len(data)).encode() + b"\0" + data).hexdigest()

def originals(repo="."):
    src = {}
    for key,(path,sha) in ORIGINALS.items():
        data=subprocess.check_output(["git","-C",str(repo),"show","HEAD:"+path])
        if blob_sha(data)!=sha:
            raise GateError("FROZEN_SOURCE_DRIFT:"+key)
        src[key]=data.decode("utf-8-sig")
    return src

def masked(s):
    """Mask C# comments/strings without altering braces/offsets of code."""
    out=list(s)
    i=0
    while i<len(s):
        if s.startswith("//",i):
            end=s.find("\n",i)
            if end<0: end=len(s)
        elif s.startswith("/*",i):
            end=s.find("*/",i+2)
            if end<0: raise GateError("BAD_COMMENT")
            end+=2
        elif s.startswith('@"',i):
            j=i+2
            while j<len(s):
                if s[j]=='"':
                    if j+1<len(s) and s[j+1]=='"':j+=2;continue
                    j+=1;break
                j+=1
            end=j
        elif s[i] in ('"',"'"):
            quote=s[i];j=i+1
            while j<len(s):
                if s[j]=="\\":j+=2;continue
                if s[j]==quote:j+=1;break
                j+=1
            end=j
        else:
            i+=1;continue
        for j in range(i,min(end,len(s))):
            if out[j]!="\n":out[j]=" "
        i=end
    return "".join(out)

def method(source,name,return_type):
    c=masked(source)
    pattern=(r"\b(?:public|private|internal|protected)\s+(?:static\s+)?"
             +re.escape(return_type)+r"\s+"+re.escape(name)+r"\s*\([^;{}]*\)\s*\{")
    matches=list(re.finditer(pattern,c))
    if len(matches)!=1: raise GateError("AMBIGUOUS_METHOD:"+name+":"+str(len(matches)))
    begin=matches[0].end()-1
    depth=0
    for i in range(begin,len(c)):
        if c[i]=="{":depth+=1
        elif c[i]=="}":
            depth-=1
            if depth==0:return c[begin+1:i]
    raise GateError("UNBALANCED_METHOD:"+name)

def has(s,regex):
    return re.search(regex,s,re.S) is not None

def assess(src):
    w07=method(src["w07"],"Reattach","bool")
    old_gui=method(src["w07"],"OnGUI","void")
    arm=method(src["w09"],"Arm","bool")
    update=method(src["w09"],"Update","void")
    safe=method(src["w09"],"TryDock","bool")
    inspect=method(src["w09"],"Inspect","HardenedDockVerdict")
    audit=method(src["w09"],"AuditEveryLayer","DockVerdict")
    gui=method(src["w09"],"OnGUI","void")
    editor=method(src["installer"],"Install","void")
    policy=method(src["policy"],"Evaluate","HardenedDockVerdict")
    previous=method(src["w08"],"Inspect","DockVerdict")
    checks={
      "w09_arm_disables_both_legacy_behaviours":all(x in arm for x in ("demo.enabled = false;","previousGate.enabled = false;","armed = true;")),
      "w09_update_redisables_legacy_behaviours":all(has(update,x) for x in (r"demo\.enabled\s*=\s*false",r"previousGate\.enabled\s*=\s*false")),
      "w09_ui_uses_only_trydock":has(gui,r"\bTryDock\s*\(\s*i\s*\)") and not has(gui,r"\bReattach\s*\("),
      "w09_checks_inspect_before_reattach":has(safe,r"if\s*\(\s*Inspect\s*\(\s*index\s*\)\s*!=\s*HardenedDockVerdict\.Allowed\s*\)\s*return\s+false\s*;\s*return\s+demo\.Reattach\s*\(\s*index\s*\)\s*;"),
      "w09_requires_both_physics_and_old_gate":has(inspect,r"AuditEveryLayer\s*\(\s*index\s*\)") and has(inspect,r"previousGate\.Inspect\s*\(\s*demo\s*,\s*index\s*\)") and has(inspect,r"HardenedDockPolicy\.Evaluate\s*\("),
      "w09_all_layer_overlap_and_penetration":has(audit,r"Physics\.OverlapBoxNonAlloc\s*\(") and has(audit,r"HardenedDockPolicy\.AllPhysicsLayers") and has(audit,r"Physics\.ComputePenetration\s*\(") and has(src["policy"],r"const\s+int\s+AllPhysicsLayers\s*=\s*-1\s*;"),
      "w09_policy_fail_closed":all(x in policy for x in ("HardenedDockVerdict.NotArmed","HardenedDockVerdict.UnsafeLegacyUi","HardenedDockVerdict.InvalidFullLayerQuery","HardenedDockVerdict.NewAuditRejected","HardenedDockVerdict.PreviousGateRejected")),
      "w09_editor_opt_in_only":has(editor,r"Undo\.AddComponent\s*<\s*HardenedDockDemo\s*>\s*\("),
      "w08_actual_physics":has(previous,r"Physics\.OverlapBoxNonAlloc\s*\(") and has(previous,r"Physics\.ComputePenetration\s*\("),
      "w07_exposes_public_raw_reattach":has(masked(src["w07"]),r"\bpublic\s+bool\s+Reattach\s*\(\s*int\s+index\s*\)") and not any(x in w07 for x in ("Inspect(","TryDock(","ComputePenetration(","OverlapBoxNonAlloc(")),
      "w07_raw_reposition_and_id_rearm":all(x in w07 for x in ("rig.Body.isKinematic = true;","rig.Part.transform.SetParent(","rig.Detached = false;","rig.Emitter.RearmAfterReattach(")),
      "w07_legacy_gui_calls_raw_reattach":has(old_gui,r"(?<!\.)\bReattach\s*\(\s*i\s*\)"),
    }
    required=[k for k in checks if k not in ("w07_exposes_public_raw_reattach","w07_raw_reposition_and_id_rearm","w07_legacy_gui_calls_raw_reattach")]
    if not all(checks[k] for k in required):
        raise GateError("W09_SOURCE_GUARD_DRIFT:"+",".join(k for k in required if not checks[k]))
    if not checks["w07_raw_reposition_and_id_rearm"]:
        raise GateError("FROZEN_LEDGER_OR_REATTACH_MUTATED")
    return {
      "schema":"B01-62-W10-DOCK-API-STATE-QA-v1",
      "producer_sha":PRODUCER_SHA,
      "original_blobs":{k:{"path":v[0],"git_blob":v[1]} for k,v in sorted(ORIGINALS.items())},
      "checks":checks,
      "w09_demo_ui_source_guard":"OBSERVED",
      "public_reattach_host_guard":"HOLD_UNSAFE_PUBLIC_API" if checks["w07_exposes_public_raw_reattach"] else "MANUAL_REVIEW_REQUIRED",
      "original_legacy_source":"IMMUTABLE",
      "unity_native":"NOT_RUN",
      "codex_ack":"NOT_RECEIVED",
      "release_gate":"HOLD" }
def main():
    p=argparse.ArgumentParser()
    p.add_argument("--repo",default=".")
    p.add_argument("--receipt",required=True)
    p.add_argument("--release",action="store_true")
    a=p.parse_args()
    dst=Path(a.receipt)
    if dst.exists() or not dst.parent.is_dir():raise GateError("OUTPUT_MUST_BE_NEW")
    data=(json.dumps(assess(originals(a.repo)),indent=2,sort_keys=True)+"\n").encode("utf8")
    dst.write_bytes(data)
    print("B01_62_W10_RESULT=HOLD_UNSAFE_PUBLIC_API")
    print("B01_62_W10_RECEIPT_SHA256="+hashlib.sha256(data).hexdigest())
    return 3 if a.release else 0
if __name__=="__main__":
    raise SystemExit(main())
