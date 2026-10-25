#!/usr/bin/env python3
"""Deterministic Unity-package ZIP and SHA256 provenance. Native Unity tests NOT_RUN."""
import hashlib,json,pathlib,subprocess,sys,zipfile
root=pathlib.Path(__file__).resolve().parents[3]
out=(root/(sys.argv[1] if len(sys.argv)>1 else "artifacts/legendary-unity")).resolve()
out.mkdir(parents=True,exist_ok=True)
head=subprocess.check_output(["git","rev-parse","HEAD"],cwd=root,text=True).strip()
paths=sorted(p.decode() for p in subprocess.check_output(["git","ls-files","-z"],cwd=root).split(b"\0")
 if p and (p.startswith(b"Assets/B01Packages/LEGENDARY001") or p==b"Assets/B01Packages.meta" or p.startswith(b"Tools/B01/LEGENDARY-UNITY-001/") or p==b".github/workflows/b01-legendary-unity.yml"))
unity=[p for p in paths if p.startswith("Assets/B01Packages/LEGENDARY001") or p=="Assets/B01Packages.meta"]
assert len(unity)>10
guids=[]
for p in unity:
 if p.endswith((".cs",".asmdef")): assert p+".meta" in unity, "missing .meta for "+p
 if p.endswith(".meta"):
  lines=[x[5:].strip() for x in (root/p).read_text().splitlines() if x.startswith("guid:")]
  assert len(lines)==1 and len(lines[0])==32
  guids+=lines
assert len(guids)==len(set(guids)),"duplicate Unity GUID"
manifest=[{"path":p,"size":(root/p).stat().st_size,"sha256":hashlib.sha256((root/p).read_bytes()).hexdigest()} for p in paths]
(out/"source-manifest.json").write_text(json.dumps({"head_sha":head,"source_contract_sha":"93f290acca26839792a5bec8ea42415ac83932f3","files":manifest},indent=2)+"\n")
(out/"source-sha256.txt").write_text("".join(f'{x["sha256"]}  {x["path"]}\n' for x in manifest))
zippath=out/"LEGENDARY-UNITY-001.zip"
with zipfile.ZipFile(zippath,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
 for p in unity:
  info=zipfile.ZipInfo(p,date_time=(1980,1,1,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED
  info.external_attr=0o100644<<16
  z.writestr(info,(root/p).read_bytes(),compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
digest=hashlib.sha256(zippath.read_bytes()).hexdigest()
(out/"zip-sha256.txt").write_text(f"{digest}  LEGENDARY-UNITY-001.zip\n")
(out/"package-receipt.json").write_text(json.dumps({"head_sha":head,"files":len(unity),"guid_count":len(guids),"zip_size":zippath.stat().st_size,"zip_sha256":digest,"unity_native":"NOT_RUN","g2":"HOLD"},indent=2)+"\n")
print("PACKAGE_PASS",len(unity),"files",len(guids),"GUIDs, ZIP_SHA256",digest)
