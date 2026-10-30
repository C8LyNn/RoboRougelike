#!/usr/bin/env python3
"""Execute the ACTUAL original P17 frozen C# source from verified binary ZIP via .NET8, not a mirror."""
import hashlib
from pathlib import Path
import subprocess
import sys
import tempfile
import xml.sax.saxutils as xml
import zipfile
import pack_deps as b
def main():
    raw=Path(sys.argv[1]).read_bytes()
    data=b.verify(raw)
    with tempfile.TemporaryDirectory(prefix="p17-original-source-") as tmp:
        root=Path(tmp)
        for p,v in data.items():
            file=root/p;file.parent.mkdir(parents=True,exist_ok=True);file.write_bytes(v)
        s=Path(__file__).with_name("OriginalSourceSmoke.cs").resolve()
        cs=[(root/"Assets/PARTSIZE001/Runtime/PartSizeContract.cs").resolve(),
            (root/"Assets/PARTSIZE001/Runtime/PartSizeBaselineCatalog.cs").resolve(),s]
        project=root/"Smoke.csproj"
        project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>9.0</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+xml.escape(str(p))+'" />' for p in cs)+'</ItemGroup></Project>')
        rigs=[str(root/p) for p in sorted(data) if p.endswith(".json")]
        p=subprocess.run(["dotnet","run","-c","Release","--project",str(project),"--",*rigs],capture_output=True,text=True)
        print(p.stdout,end="")
        if p.returncode or "ORIGINAL_CSHARP_RIGS_PASS=3" not in p.stdout:
            raise b.Hold("CSHARP_SOURCE_SMOKE_FAILED "+p.stderr[-1000:])
        print("REAL_ORIGINAL_PRODUCTION_BLOBS_COMPILED SHA256",hashlib.sha256(raw).hexdigest())
if __name__=="__main__":
    try: main()
    except (b.Hold,OSError) as e:print("HOLD",e,file=sys.stderr);sys.exit(2)
