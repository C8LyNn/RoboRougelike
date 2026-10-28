#!/usr/bin/env python3
"""These are ONLY synthetic NEGATIVE PARSER tests, NOT Unity/Player tests."""
import json
import copy
import tempfile
from pathlib import Path
from validate_player_receipts import CASES, validate

SRC="231005c6d0351945a8ee6684f3856af118f0e94f"
BUILD="7299dcc5fd63ac042a0ffcd97b69d02fb8744649"
ZIP="ec3193bb3ac24398c496ea65ad12aa653321db160a4dc74e8f618e3cc7ad6d1d"

def positive():
    r={"source_sha":SRC,"build_sha":BUILD,"player_archive_sha256":ZIP,
       "platform":"Windows x64 Player","unity_version":"6000.3.14f1",
       "runtime_status":"ACTUALLY_EXECUTED","cases":{}}
    docs={}
    for name,c in CASES.items():
        r["cases"][name]={"exit_code":0,"command":"game.exe -batchmode "+c["flag"]+" output", "json_file":name+"/"+c["file"],"log_file":name+"/Player.log"}
        obj={"status":"PASS","error":"",**{k:True for k in c["fields"]},**{k:2 for k in c["ints"]},**{k:"non-empty" for k in c["strs"]}}
        if name=="mov009":obj["force"]=123.4
        if name=="editor":obj["maxZ"]=4.5
        docs[name]=obj
    return r,docs

def run_test(root,edit,expected_error,desc):
    r,docs=positive()
    edit(r,docs)
    (root/"receipt.json").write_text(json.dumps(r),encoding="utf-8")
    for name,c in CASES.items():
        base=root/name;base.mkdir(exist_ok=True)
        (base/c["file"]).write_text(json.dumps(docs[name]),encoding="utf-8")
        (base/"Player.log").write_text("synthetic validation log; NEVER native evidence",encoding="utf-8")
    errors=validate(root,SRC,BUILD)
    ok=any(expected_error in e for e in errors) if expected_error else len(errors)==0
    print(("PASS" if ok else "FAIL"),desc, "|",(errors[0] if errors else "structure only"))
    if not ok: raise AssertionError(desc+":"+str(errors))

def main():
    with tempfile.TemporaryDirectory() as td:
        root=Path(td)
        cases=[
            (lambda r,d:None,"","synthetic structurally-valid baseline"),
            (lambda r,d:r.update(source_sha="f"*40),"source SHA mismatch","wrong game SHA"),
            (lambda r,d:r.update(build_sha="f"*40),"player build SHA mismatch","wrong player build"),
            (lambda r,d:r.update(runtime_status="NOT_RUN"),"runtime_status","not-run receipt"),
            (lambda r,d:r.update(player_archive_sha256="not-a-hash"),"complete player archive","missing archive hash"),
            (lambda r,d:r.update(unity_version="6000.0"),"Unity version","wrong Unity version"),
            (lambda r,d:r["cases"]["garage"].update(exit_code=False),"exit_code","boolean exit_code fail-open"),
            (lambda r,d:r["cases"]["garage"].update(exit_code=1),"exit_code","nonzero exit"),
            (lambda r,d:r["cases"]["mov009"].update(command="game.exe -batchmode"),"command","missing test flag"),
            (lambda r,d:r["cases"]["editor"].update(json_file="../elsewhere.json"),"path escapes","path traversal"),
            (lambda r,d:d["garage"].update(status="FAIL"),"status/error","self-reported fail"),
            (lambda r,d:d["garage"].update(removedGun=False),"removedGun","individual false state"),
            (lambda r,d:d["garage"].update(removedWheels=False),"removedWheels","boolean masquerades as int"),
            (lambda r,d:d["mov009"].update(force=float('nan')),"force","NaN as fake force"),
            (lambda r,d:d["editor"].update(maxZ=0),"maxZ","no drive movement"),
            (lambda r,d:r["cases"].pop("editor"),"exactly garage/mov009/editor","missing case"),
            (lambda r,d:r["cases"]["garage"].update(log_file="../../outside.log"),"path escapes","log traversal"),
            (lambda r,d:d["mov009"].update(hoverStatus=""),"hoverStatus","missing observed status"),
        ]
        for edit,err,name in cases:run_test(root,edit,err,name)
        print("RESULT: "+str(len(cases))+"/"+str(len(cases))+" SYNTHETIC PARSER TESTS PASS; NATIVE PLAYER NOT_RUN")

if __name__=="__main__": main()
