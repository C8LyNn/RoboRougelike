#!/usr/bin/env python3
"""Read-only source+GUID receipt, no directory changes."""
import hashlib, json, re, subprocess, pathlib, sys
ROOT=pathlib.Path(__file__).resolve().parents[4]
HERE=pathlib.Path(__file__).resolve().parent
BASE="5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"
def main():
    m=json.loads((HERE/"manifest.json").read_text(encoding="utf-8"))
    assert m["package"]=="B01-11-RECOVERYHUD001/R1"
    assert m["base_commit"]==BASE
    entries=m["files"]
    assert len(entries)==14 and len({e["path"] for e in entries})==14
    recorded=set()
    for e in entries:
        p=e["path"]
        assert p and ".." not in pathlib.PurePosixPath(p).parts
        assert (p.startswith("Assets/B01Packages/RECOVERYHUD001") or
            p.startswith("Tools/B01/B01-11/P17W24RecoveryHud001/") or
            p==".github/workflows/b01-11-p17w24-recoveryhud.yml")
        f=ROOT/p
        assert f.is_file() and not f.is_symlink(),p
        b=f.read_bytes()
        assert len(b)==e["bytes"] and hashlib.sha256(b).hexdigest()==e["sha256"],p
        recorded.add(p)
    expected=recorded|{"Tools/B01/B01-11/P17W24RecoveryHud001/manifest.json"}
    diff=subprocess.check_output(["git","diff","--name-only",BASE,"HEAD"],cwd=ROOT,text=True).splitlines()
    assert set(diff)==expected,sorted(set(diff)^expected)
    own=sorted((ROOT/"Assets/B01Packages/RECOVERYHUD001").rglob("*.meta"))
    assert len(own)==5,len(own)
    guids=[]
    for f in own:
        match=re.search(r"^guid: ([0-9a-f]{32})$",f.read_text(),re.MULTILINE)
        assert match,f
        guids.append(match.group(1))
    assert len(set(guids))==5
    for p in ["Runtime/RecoveryNotice.cs","Runtime/RecoveryNoticePanel.cs","Runtime/B01.RecoveryHud.asmdef"]:
        assert (ROOT/"Assets/B01Packages/RECOVERYHUD001"/(p+".meta")).exists(),p
    coll={}
    for f in (ROOT/"Assets").rglob("*.meta"):
        if f.is_symlink(): continue
        match=re.search(r"^guid: ([0-9a-fA-F]{32})$",f.read_text(encoding="utf-8",errors="replace"),re.MULTILINE)
        if match:coll.setdefault(match.group(1).lower(),[]).append(str(f.relative_to(ROOT)))
    for g in guids:assert len(coll.get(g,[]))==1,(g,coll.get(g))
    print("MANIFEST_PASS",len(entries),"GUID_PASS",len(guids),"EXACT_SCOPE_PASS",len(expected))
if __name__=="__main__":
    try:main()
    except Exception as exc:
        print("SOURCE_G0_BLOCKED",repr(exc),file=sys.stderr)
        sys.exit(2)
