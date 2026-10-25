#!/usr/bin/env python3
"""Build a reproducible full GitHub-owned QA source ZIP from immutable hash manifest."""
import argparse,hashlib,json,zipfile
from pathlib import Path
def sha256(b):return hashlib.sha256(b).hexdigest()
def main():
 p=argparse.ArgumentParser();p.add_argument("--repo",type=Path,required=True);p.add_argument("--out",type=Path,required=True);p.add_argument("--report",type=Path,required=True)
 a=p.parse_args();repo=a.repo.resolve()
 manifest=json.loads((repo/"Tools/B01/QA-NATIVE-001/manifest.json").read_text(encoding="utf-8"))
 if manifest.get("schema")!="b01-60-qa-native-manifest-v1":raise SystemExit("BAD_MANIFEST_SCHEMA")
 items=manifest.get("files",{})
 if not items:raise SystemExit("EMPTY_FILE_MANIFEST")
 paths=[]
 for name,expect in sorted(items.items()):
  rel=Path(name)
  if rel.is_absolute() or ".." in rel.parts or "\\" in name:raise SystemExit("UNSAFE_MANIFEST_PATH: "+name)
  file=(repo/rel).resolve()
  if not file.is_relative_to(repo) or not file.is_file() or file.is_symlink():raise SystemExit("MISSING_OR_UNSAFE_FILE: "+name)
  data=file.read_bytes()
  if len(data)!=expect["bytes"] or sha256(data)!=expect["sha256"]:raise SystemExit("MANIFEST_FILE_MISMATCH: "+name)
  paths.append((name,data))
 # Self-hash omission is deliberate. Include manifest itself in archive without recursive hash.
 mpath=repo/"Tools/B01/QA-NATIVE-001/manifest.json"
 paths.append(("Tools/B01/QA-NATIVE-001/manifest.json",mpath.read_bytes()))
 a.out.parent.mkdir(parents=True,exist_ok=True)
 with zipfile.ZipFile(a.out,"w",compression=zipfile.ZIP_STORED) as zipf:
  for name,data in sorted(paths):
   info=zipfile.ZipInfo(name,(2026,10,25,0,0,0))
   info.compress_type=zipfile.ZIP_STORED
   info.create_system=3
   info.external_attr=0o644<<16
   zipf.writestr(info,data,compress_type=zipfile.ZIP_STORED)
 receipt={"schema":"b01-60-package-zip-receipt-v1","package_id":manifest["package_id"],"sha256":sha256(a.out.read_bytes()),"bytes":a.out.stat().st_size,"entries":len(paths),"unity_native":"NOT_RUN","g2":"HOLD"}
 a.report.parent.mkdir(parents=True,exist_ok=True)
 a.report.write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n",encoding="utf-8")
 print(json.dumps(receipt,sort_keys=True))
if __name__=="__main__":main()
