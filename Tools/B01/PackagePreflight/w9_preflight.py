#!/usr/bin/env python3
"""W9 independent static source audit. NOT Unity compilation."""
import hashlib,json,re,subprocess
from pathlib import Path

CASES=[
("CAMERA","894d3488dedb69e2b8d89683d750bbdbfedf4e55","d48108f56d3fe0790ad715683e77b2de9219e4b1","Assets/B01Packages/CAMERA001/","Docs/B01/CAMERA001/manifest.json","files","git_blob_sha1"),
("BUILD","d575b79e262ec9b65702608d6b6e28972da0d21c","489676414a3eb45fe70124098e3778a81d0aaff2","Assets/B01Construction/BUILD001/","Docs/B01/BUILD001/manifest.json","blob_sha1_and_bytes","git_blob_sha1"),
("RAC","6c1aa7c2351a29695e4288dddad2565cdec3682a","fd9a3f4dda08eaba56d9f74007bc35dc0809b67e","Assets/RoboRougelikeMechanics/RAC001CheckpointRace/","Docs/ParallelMechanics/RAC-001/manifest.r3.json","git_blob_sha1_and_bytes","blob_sha1")]
def git(*args,stdin=None):
    return subprocess.run(["git",*args],input=stdin,check=True,capture_output=True).stdout
def blob(sha,path):return git("show",sha+":"+path)
def tree(sha,prefix):
    out={}
    for line in git("ls-tree","-rz",sha,"--",prefix).split(b"\0"):
        if not line:continue
        part,path=line.split(b"\t",1)
        mode,typ,obj=part.decode().split()
        if typ=="blob":out[path.decode()]=obj
    return out
def directives(s):
    active=[];errs=[]
    for n,line in enumerate(s.splitlines(),1):
        m=re.match(r"^\s*#\s*(if|elif|else|endif)\b",line)
        if not m:continue
        op=m.group(1)
        if op=="if":active.append([n,False])
        elif not active:errs.append("STRAY_"+op.upper()+"_"+str(n))
        elif op=="else":
            if active[-1][1]:errs.append("DUPLICATE_ELSE_"+str(n))
            active[-1][1]=True
        elif op=="elif" and active[-1][1]:errs.append("ELIF_AFTER_ELSE_"+str(n))
        elif op=="endif":active.pop()
    return errs+["MISSING_ENDIF_"+str(n) for n,_ in active]
def asmdef(a):
    refs=a.get("references",[])
    if not isinstance(refs,list):return ["INVALID_ASMDEF"]
    errs=[]
    if len(set(refs))!=len(refs):errs.append("DUPLICATE_REFERENCE")
    if "TestAssemblies" in a.get("optionalUnityReferences",[]) and any(x in refs for x in ("UnityEngine.TestRunner","UnityEditor.TestRunner")):errs.append("DOUBLE_TEST_RUNNER")
    return errs
def audit(case,sha):
    name,_,_,prefix,manifest_path,key,blob_key=case
    files=tree(sha,prefix);findings=[];integrity=[];seen={}
    for p in sorted(files):
        data=blob(sha,p)
        if p.endswith((".cs",".asmdef",".unity")) and p+".meta" not in files:integrity.append(p+":MISSING_META")
        if p.endswith(".cs"):findings.extend(p+":"+x for x in directives(data.decode("utf8")))
        if p.endswith(".asmdef"):
            try:findings.extend(p+":"+x for x in asmdef(json.loads(data)))
            except Exception as e:integrity.append(p+":INVALID_ASMDEF_JSON:"+str(e))
        if p.endswith(".meta"):
            match=re.search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$",data.decode("utf8"))
            if match is None:integrity.append(p+":BAD_GUID")
            elif match.group(1) in seen:integrity.append(p+":GUID_COLLISION_WITH_"+seen[match.group(1)])
            else:seen[match.group(1)]=p
    try:
        m=json.loads(blob(sha,manifest_path))
        entries=m[key]
        for p,record in entries.items():
            data=blob(sha,p)
            git_sha1=git("hash-object","--stdin",stdin=data).decode().strip()
            if git_sha1!=record[blob_key] or len(data)!=record["bytes"]:integrity.append(p+":MANIFEST_BLOB_BYTES_MISMATCH")
            if record.get("sha256") is not None and hashlib.sha256(data).hexdigest()!=record["sha256"]:integrity.append(p+":MANIFEST_SHA256_MISMATCH")
        for p in files:
            if p not in entries:integrity.append(p+":ASSET_NOT_IN_MANIFEST")
    except Exception as e:integrity.append("MANIFEST_NOT_VERIFIABLE:"+repr(e))
    return {"revision_sha":sha,"package":name,"findings":findings,"integrity":integrity,"files":len(files),"native_unity":"NOT_RUN"}
def main():
    results=[];bad=[]
    for case in CASES:
        name,redsha,fixsha,*_=case
        red=audit(case,redsha);fix=audit(case,fixsha)
        pattern="DOUBLE_TEST_RUNNER" if name!="RAC" else "MISSING_ENDIF"
        ok=sum(pattern in x for x in red["findings"])==(2 if name!="RAC" else 1) and not fix["findings"] and not fix["integrity"]
        results.append({"package":name,"baseline":red,"rework":fix,"static_result":"PASS" if ok else "FAIL"})
        if not ok:bad.append(name)
        print(f"W9_{name}: RED={red['findings']} FIXED={fix['findings']} FIXED_INTEGRITY={fix['integrity']} STATIC_PASS={ok}")
    out=Path("w9-preflight-evidence");out.mkdir(exist_ok=True)
    (out/"W9_STATIC_REWORK_QA.json").write_text(json.dumps({"dispatch":"HQ-B01-20261025-W9-SOURCE-REPAIR","scope":"GIT_COMMITTED_SOURCE_STATIC_ONLY","packages":results,"failed":bad,"native_compile":"NOT_RUN","editmode":"NOT_RUN","playmode":"NOT_RUN","G2":"HOLD"},indent=2)+"\n")
    print(f"W9_STATIC={3-len(bad)}/3 PASS; NATIVE_UNITY=NOT_RUN G2=HOLD")
    return 0 if not bad else 2
if __name__=="__main__":raise SystemExit(main())
