#!/usr/bin/env python3
"""B01-62: fail-closed ORIGINAL Unity C# source-route audit of optional safe reattach.
A real source-code integration gate, not a Unity/PhysX runtime test.
"""
import argparse
import hashlib
import json
import re
import subprocess
from pathlib import Path

R1 = "cb2b3a76e84266e2730fffc4ab048c1039f0652f"
R2 = "cdcaea750fda1a9d7db6018e34970f4b9f206427"
R3 = "99dfbd25d471f9983f9472b12d3725f4c002f7e9"
FILES = {
    "playground": (R2, "Assets/B0123DebrisPlayground001/Runtime/DebrisRigPlayground.cs",
                   "e4e31c32cb5abda2fef987efa4b45fd61e828312"),
    "safegate": (R3, "Assets/B0123ReattachClearance001/Runtime/DebrisSafeReattachGate.cs",
                 "7c7e96a10d69149119309863a0c2a9bdab829072"),
    "policy": (R3, "Assets/B0123ReattachClearance001/Runtime/DockClearancePolicy.cs",
               "de0a847dbecacb92ad63a09ca12f5c9c372d597a"),
    "installer": (R3, "Assets/B0123ReattachClearance001/Editor/SafeReattachDemoInstaller.cs",
                  "5892f7e31d1e9fa07c124d63324c796b5abc282b"),
    "ledger": (R1, "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",
               "fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
}
class SourceDrift(RuntimeError):
    pass

def git_show(repo, ref, path):
    return subprocess.check_output(["git", "-C", str(repo), "show", ref+":"+path])

def git_blob(data):
    return hashlib.sha1(b"blob "+str(len(data)).encode("ascii")+b"\x00"+data).hexdigest()

def fetch_original(repo):
    sources = {}
    for key, (commit, path, expected) in FILES.items():
        source = git_show(repo, commit, path)
        actual = git_blob(source)
        if actual != expected:
            raise SourceDrift("FAIL_CLOSED_SOURCE_BLOB_DRIFT "+key+" "+actual)
        rev = subprocess.check_output(
            ["git","-C",str(repo),"rev-parse",commit+":"+path]).decode().strip()
        if rev != expected:
            raise SourceDrift("FAIL_CLOSED_GIT_OBJECT_DRIFT "+key)
        sources[key] = source.decode("utf-8-sig")
    return sources

def mask_non_code(s):
    """Preserve original offsets while masking C# strings, char literals and comments."""
    out = list(s)
    i = 0
    while i < len(s):
        kind = None
        if s.startswith("//",i):
            kind = "line"; end = s.find("\n",i)
            if end < 0: end = len(s)
        elif s.startswith("/*",i):
            kind = "block"; at=s.find("*/",i+2)
            if at<0: raise SourceDrift("UNTERMINATED_BLOCK_COMMENT")
            end=at+2
        elif s.startswith('@"',i):
            kind = "verbatim";j=i+2
            while j < len(s):
                if s[j]=='"':
                    if j+1<len(s) and s[j+1]=='"':j+=2;continue
                    j+=1;break
                j+=1
            end=j
            if end==len(s) and s[-1]!='"':raise SourceDrift("BAD_VERBATIM")
        elif s[i] in "\"'":
            kind = "string";quote=s[i];j=i+1
            while j < len(s):
                if s[j]=="\\":j+=2;continue
                if s[j]==quote:j+=1;break
                j+=1
            if j>len(s) or (j==len(s) and s[-1]!=quote):
                raise SourceDrift("BAD_STRING")
            end=j
        if kind is not None:
            for j in range(i,end):
                if out[j]!="\n":out[j]=" "
            i=end
        else:
            i+=1
    return "".join(out)

def method(source, name, ret):
    masked=mask_non_code(source)
    pat=r"\b(?:public|private|protected|internal)\s+(?:static\s+)?"+re.escape(ret)+r"\s+"+re.escape(name)+r"\s*\([^{};]*\)\s*\{"
    matches=list(re.finditer(pat,masked))
    if len(matches)!=1:
        raise SourceDrift("AMBIGUOUS_OR_MISSING_METHOD "+name+" "+str(len(matches)))
    idx=matches[0].end()-1
    depth=0
    for j in range(idx,len(masked)):
        if masked[j]=="{":depth+=1
        if masked[j]=="}":
            depth-=1
            if depth==0:
                return source[idx+1:j],masked[idx+1:j]
    raise SourceDrift("UNBALANCED_METHOD "+name)

def find(regex,s):
    return re.search(regex,s,re.DOTALL) is not None

def inspect_sources(src):
    w07_gui,w07_gui_code=method(src["playground"],"OnGUI","void")
    w07_reattach,w07_re_code=method(src["playground"],"Reattach","bool")
    w08_gui,w08_gui_code=method(src["safegate"],"OnGUI","void")
    w08_safe,w08_safe_code=method(src["safegate"],"TrySafeReattach","bool")
    w08_inspect,w08_inspect_code=method(src["safegate"],"Inspect","DockVerdict")
    installer,installer_code=method(src["installer"],"Install","void")
    policy,policy_code=method(src["policy"],"Evaluate","DockVerdict")
    ledger,ledger_code=method(src["ledger"],"TryDetach","bool")

    original_ui_has_unsafe_button = find(r'GUILayout\.Button\(\s*"Reattach"\s*\)', w07_gui)
    original_ui_calls_unchecked_reattach = find(r'\bReattach\s*\(', w07_gui_code)
    original_ui_bypass=original_ui_calls_unchecked_reattach
    original_rearms_on_reattach=all(find(re.escape(x),w07_re_code) for x in (
        "rig.Body.isKinematic = true", "rig.Part.transform.SetParent(",
        "rig.Detached = false", "rig.Emitter.RearmAfterReattach("))
    policy_checks_reach_and_contacts=all(
        find(re.escape(x),policy_code) for x in
        ("QuerySaturated","OutOfReach","Blocked","penetrationToleranceMetres"))
    has_real_overlap=find(r'\bPhysics\.OverlapBoxNonAlloc\s*\(',w08_inspect_code)
    has_real_penetration=find(r'\bPhysics\.ComputePenetration\s*\(',w08_inspect_code)
    check_call=re.search(
        r'\bif\s*\(\s*Inspect\s*\(\s*demo\s*,\s*index\s*\)\s*!=\s*DockVerdict\.Allowed\s*\)\s*return\s+false\s*;',
        w08_safe_code)
    gated_call=list(re.finditer(r'\bdemo\s*\.\s*Reattach\s*\(\s*index\s*\)',w08_safe_code))
    guarded_safe_method=bool(check_call and len(gated_call)==1
                             and gated_call[0].start()>check_call.end()
                             and find(r'\breturn\s+demo\s*\.\s*Reattach\s*\(\s*index\s*\)\s*;',w08_safe_code))
    safe_ui_calls_gate=find(r'\bTrySafeReattach\s*\(\s*demo\s*,\s*i\s*\)',w08_gui_code)
    installed_opt_in=find(r'\bUndo\.AddComponent\s*<\s*DebrisSafeReattachGate\s*>\s*\(',installer_code)
    installer_rewrites_old_ui=find(r'\bReattach\s*\(\s*index\s*\)',installer_code)
    original_ledger_has_only_memory_seen=find(r'\bHashSet\s*<\s*long\s*>\s+seen\b',src["ledger"])
    original_ledger_used_for_rearm=find(r'\bRearmAfterReattach\s*\(',w07_re_code)
    valid_safe_branch=all((policy_checks_reach_and_contacts,has_real_overlap,
                           has_real_penetration,guarded_safe_method,safe_ui_calls_gate))
    if not (original_rearms_on_reattach and original_ledger_has_only_memory_seen
            and original_ledger_used_for_rearm and installed_opt_in
            and not installer_rewrites_old_ui and valid_safe_branch):
        raise SourceDrift("ORIGINAL_SOURCE_CONTRACT_OR_SAFE_BRANCH_DRIFT")
    return {
        "old_gui_direct_recomposition_bypass": original_ui_bypass,
        "old_gui_button":original_ui_has_unsafe_button,
        "old_gui_calls_reposition_and_rearm":original_ui_calls_unchecked_reattach,
        "original_reposition_rearms_stable_id":original_rearms_on_reattach,
        "safe_gate_checks_real_physics_observations":has_real_overlap and has_real_penetration,
        "safe_gate_calls_original_reattach_only_after_allow":guarded_safe_method,
        "safe_ui_calls_gate":safe_ui_calls_gate,
        "installer_opt_in_only_no_original_ui_replace":installed_opt_in and not installer_rewrites_old_ui,
        "release_integration_guard":"HOLD_UNGUARDED_W07_UI" if original_ui_bypass else "NO_STATIC_UI_BYPASS_DETECTED_NEEDS_NATIVE_REVIEW",
        "fully_protected_native_playground":False,
        "unity_physx_executed":False,
    }

def report(original):
    checked=inspect_sources(original)
    return {
        "schema":"b01-62-reattach-bypass-gate-v1",
        "sources":{key:{"sha":v[0],"path":v[1],"git_blob":v[2]}
                   for key,v in sorted(FILES.items())},
        "actual":checked,
        "guard_rule":"HOLD if original OnGUI exposes Reattach(i) outside the new safe gate",
        "gate_result":"SOURCE_INTEGRATION_HOLD" if checked["old_gui_direct_recomposition_bypass"]
                      else "SOURCE_ONLY_NO_UI_BYPASS_DETECTED",
        "original_source_modified":False,
        "native_unity":"NOT_RUN",
        "original_b01_62_save_ledger":"#457 SOURCE_ONLY; no canonical save integration",
    }

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--repo",required=True)
    p.add_argument("--output",required=True)
    p.add_argument("--expect-hold",action="store_true")
    a=p.parse_args()
    dest=Path(a.output)
    if dest.exists() or not dest.parent.is_dir():
        raise SourceDrift("RECEIPT_OUTPUT_MUST_BE_NEW_AND_PARENT_EXIST")
    obj=report(fetch_original(a.repo))
    if a.expect_hold and obj["gate_result"]!="SOURCE_INTEGRATION_HOLD":
        raise SourceDrift("EXPECTED_UI_BYPASS_DISAPPEARED_REVIEW_CHANGE")
    data=(json.dumps(obj,sort_keys=True,indent=2,ensure_ascii=True)+"\n").encode()
    dest.write_bytes(data)
    print("B01_62_REATTACH_SOURCE_GATE="+obj["gate_result"])
    print("SOURCE_RECEIPT_SHA256="+hashlib.sha256(data).hexdigest())
