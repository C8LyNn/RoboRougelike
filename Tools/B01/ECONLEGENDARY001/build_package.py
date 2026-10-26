#!/usr/bin/env python3
"""Full source provenance plus deterministic Unity Assets-only receiver ZIP. NO host changes."""
import hashlib,json,pathlib,subprocess,sys,zipfile
root=pathlib.Path(__file__).resolve().parents[3]
out=root/(sys.argv[1] if len(sys.argv)>1 else "artifacts/econlegendary")
out.mkdir(parents=True,exist_ok=True)
head=subprocess.check_output(["git","rev-parse","HEAD"],cwd=root,text=True).strip()
raw=subprocess.check_output(["git","ls-files","-z"],cwd=root).split(b"\0")
paths=sorted(x.decode() for x in raw if x and (
  x.startswith(b"Assets/B01Packages/ECONLEGENDARY001") or x==b"Assets/B01Packages.meta" or
  x.startswith(b"Tools/B01/ECONLEGENDARY001/") or x==b".github/workflows/b01-econlegendary-guard.yml"))
assets=sorted(p for p in paths if p.startswith("Assets/B01Packages/ECONLEGENDARY001") or p=="Assets/B01Packages.meta")
assert len(assets)>=6
guids=[]
for p in assets:
 if p.endswith((".cs",".asmdef")):assert p+".meta" in assets,p
 if p.endswith(".meta"):
  rows=[line[5:].strip() for line in (root/p).read_text().splitlines() if line.startswith("guid:")]
  assert len(rows)==1 and len(rows[0])==32,p
  guids.extend(rows)
assert len(set(guids))==len(guids),"Unity GUID collision"
source=[{"path":p,"bytes":(root/p).stat().st_size,"sha256":hashlib.sha256((root/p).read_bytes()).hexdigest()} for p in paths]
(out/"source-manifest.json").write_text(json.dumps({"producer_head":head,"upstream_econ":"12572117a3dce369257feabc57021cde58023369","upstream_legend":"dffabbc6960304fdb33322b5a23c884feac804c6","files":source},indent=2)+"\n")
(out/"source-sha256.txt").write_text("".join(f'{x["sha256"]}  {x["path"]}\n' for x in source))
zipPath=out/"ECONLEGENDARY-GUARD-001.zip"
with zipfile.ZipFile(zipPath,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
 for p in assets:
  info=zipfile.ZipInfo(p,date_time=(1980,1,1,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED
  info.external_attr=0o100644<<16
  z.writestr(info,(root/p).read_bytes(),compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
h=hashlib.sha256(zipPath.read_bytes()).hexdigest()
(out/"zip-sha256.txt").write_text(h+"  "+zipPath.name+"\n")
(out/"package-receipt.json").write_text(json.dumps({"head":head,"files":len(assets),"unique_guids":len(guids),"inner_zip_bytes":zipPath.stat().st_size,"inner_zip_sha256":h,"native_unity":"NOT_RUN","g2":"HOLD"},indent=2)+"\n")
print("PACKAGE_PASS",len(assets),"assets",len(guids),"guids",h)
