#!/usr/bin/env python3
"""Validate real Unity NUnit XML and Windows Player payload; never infer PASS from ZIP existence."""
import argparse
import hashlib
import json
import tempfile
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

def digest(path):
    h=hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda:stream.read(1024*1024),b""): h.update(chunk)
    return h.hexdigest()

def validate_xml(base):
    if not base.is_dir(): raise ValueError("Unity artifacts folder absent: "+str(base))
    reports=[]
    for path in sorted(base.rglob("*.xml")):
        root=ET.parse(path).getroot()
        if root.tag!="test-run": continue
        cases=root.findall(".//test-case")
        if not cases or root.get("result")!="Passed":
            raise ValueError("NUnit not complete PASS: "+str(path))
        results=[case.get("result") for case in cases]
        if any(result!="Passed" for result in results):
            raise ValueError("Failed, skipped or inconclusive NUnit: "+str(path))
        for attr,num in (("total",len(cases)),("passed",len(cases)),
                         ("failed",0),("skipped",0),("inconclusive",0)):
            if attr in root.attrib and int(root.get(attr))!=num:
                raise ValueError("NUnit aggregate mismatch: "+str(path)+" "+attr)
        reports.append({"path":str(path),"cases":len(cases),"sha256":digest(path)})
    if not reports: raise ValueError("NO NUnit3 test-run XML. Unity NOT VERIFIED.")
    return {"kind":"ACTUAL_EXECUTED_UNITY_XML","passed":sum(x["cases"] for x in reports),
            "files":reports}

def zip_build(root,destination):
    exe=root/"RoboRougelike.exe"
    data=root/"RoboRougelike_Data"
    if not exe.is_file() or exe.stat().st_size==0 or not data.is_dir():
        raise ValueError("Missing genuine Windows Player EXE/Data; build NOT accepted")
    paths=sorted(p for p in root.rglob("*") if p.is_file())
    if any(p.is_symlink() for p in root.rglob("*")):
        raise ValueError("Windows Player symlinks prohibited")
    if len(paths)<3: raise ValueError("Windows Player unexpectedly incomplete")
    destination.parent.mkdir(parents=True,exist_ok=True)
    with zipfile.ZipFile(destination,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=6) as z:
        for path in paths:
            rel=path.relative_to(root).as_posix()
            info=zipfile.ZipInfo(rel,(1980,1,1,0,0,0))
            info.compress_type=zipfile.ZIP_DEFLATED
            z.writestr(info,path.read_bytes())
    with zipfile.ZipFile(destination) as z:
        if z.testzip() is not None or len(z.namelist())!=len(paths):
            raise ValueError("Windows ZIP integrity / member count mismatch")
    report={"kind":"REAL_WINDOWS_PLAYER_BUILD_ZIP_NOT_RUNTIME_PASS",
            "zip":str(destination),"zip_sha256":digest(destination),
            "files":len(paths),"exe_sha256":digest(exe)}
    destination.with_suffix(".receipt.json").write_text(json.dumps(report,sort_keys=True)+"\n",encoding="utf-8")
    return report

def selftest():
    with tempfile.TemporaryDirectory() as d:
        tmp=Path(d)
        xml=tmp/"result.xml"
        xml.write_text('<test-run result="Passed" total="1" passed="1" failed="0"><test-case result="Passed"/></test-run>')
        assert validate_xml(tmp)["passed"]==1
        xml.write_text('<test-run result="Passed" total="1" passed="0" failed="1"><test-case result="Failed"/></test-run>')
        try: validate_xml(tmp)
        except ValueError: pass
        else: raise AssertionError("False NUnit acceptance")
        xml.unlink()
        try: validate_xml(tmp)
        except ValueError: pass
        else: raise AssertionError("No XML accepted")
        build=tmp/"win"
        build.mkdir()
        try: zip_build(build,tmp/"bad.zip")
        except ValueError: pass
        else: raise AssertionError("No EXE accepted")
        (build/"RoboRougelike_Data").mkdir()
        (build/"RoboRougelike_Data"/"globalgamemanagers").write_bytes(b"test-data")
        (build/"UnityPlayer.dll").write_bytes(b"test-unity")
        (build/"RoboRougelike.exe").write_bytes(b"test-exe")
        report=zip_build(build,tmp/"good.zip")
        assert report["files"]==3 and len(report["zip_sha256"])==64
        assert zipfile.ZipFile(tmp/"good.zip").testzip() is None
    return {"kind":"SOURCE_TEST_ONLY","positive_nunit":1,"negative_nunit":2,
            "negative_build":1,"zip_fake_fixture_positive":1}

def main():
    arg=argparse.ArgumentParser()
    choice=arg.add_mutually_exclusive_group(required=True)
    choice.add_argument("--selftest",action="store_true")
    choice.add_argument("--xml-root",type=Path)
    choice.add_argument("--package-build",type=Path)
    arg.add_argument("--dest",type=Path)
    args=arg.parse_args()
    if args.selftest: result=selftest()
    elif args.xml_root: result=validate_xml(args.xml_root)
    else:
        if args.dest is None: arg.error("--dest required with --package-build")
        result=zip_build(args.package_build,args.dest)
    print(json.dumps(result,sort_keys=True))
if __name__=="__main__":main()
