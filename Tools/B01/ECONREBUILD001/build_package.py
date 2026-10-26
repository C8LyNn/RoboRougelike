#!/usr/bin/env python3
"""Build deterministic immutable Unity folder ZIP with per-source SHA256 (not Unity G1)."""
import hashlib,json,pathlib,subprocess,sys,zipfile
root=pathlib.Path(__file__).resolve().parents[3]
output=(root/(sys.argv[1] if len(sys.argv)>1 else "artifacts/econ-rebuild")).resolve()
output.mkdir(parents=True,exist_ok=True)
head=subprocess.check_output(["git","rev-parse","HEAD"],text=True,cwd=root).strip()
files=sorted(p.decode() for p in subprocess.check_output(["git","ls-files","-z"],cwd=root).split(b"\0")
 if p and (p.startswith(b"Assets/B01Packages/ECONREBUILD001") or
           p==b"Assets/B01Packages.meta" or p.startswith(b"Tools/B01/ECONREBUILD001/") or
           p==b".github/workflows/b01-econ-rebuild.yml"))
assert files,"empty source"
unity=[p for p in files if p.startswith("Assets/B01Packages/ECONREBUILD001") or p=="Assets/B01Packages.meta"]
assert len(unity)>=10
guids=[]
for p in unity:
 if p.endswith((".cs",".asmdef")):assert p+".meta" in unity, "missing meta "+p
 if p.endswith(".meta"):
  found=[line[5:].strip() for line in (root/p).read_text().splitlines() if line.startswith("guid:")]
  assert len(found)==1 and len(found[0])==32,"invalid meta "+p
  guids.extend(found)
assert len(set(guids))==len(guids),"GUID collision"
entries=[{"path":p,"bytes":(root/p).stat().st_size,"sha256":hashlib.sha256((root/p).read_bytes()).hexdigest()} for p in files]
(output/"source-manifest.json").write_text(json.dumps({"schema":"b01.econ-rebuild.source.v1","commit_sha":head,
 "upstream_issue":136,"entries":entries},indent=2)+"\n")
(output/"SHA256SUMS.txt").write_text("".join(f"{e['sha256']}  {e['path']}\n" for e in entries))
name=output/"ECONREBUILD001_Unity_Source.zip"
with zipfile.ZipFile(name,"w") as f:
 for p in unity:
  x=zipfile.ZipInfo(p,(1980,1,1,0,0,0));x.create_system=3;x.external_attr=0o100644<<16
  x.compress_type=zipfile.ZIP_STORED
  f.writestr(x,(root/p).read_bytes())
digest=hashlib.sha256(name.read_bytes()).hexdigest()
(output/"ZIP_SHA256.txt").write_text(digest+"  "+name.name+"\n")
(output/"package-receipt.json").write_text(json.dumps({"schema":"b01.econ-rebuild.package.v1",
 "commit_sha":head,"unity_files":len(unity),"source_files":len(files),"unique_guids":len(guids),
 "inner_zip_sha256":digest,"inner_zip_bytes":name.stat().st_size,
 "unity_native":"NOT_RUN","g2":"HOLD"},indent=2)+"\n")
print(f"PACKAGE_PASS unity_files={len(unity)} guids={len(guids)} SHA256={digest}")
