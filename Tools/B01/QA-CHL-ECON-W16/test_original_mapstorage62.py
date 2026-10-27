#!/usr/bin/env python3
"""B01-22 PASS12 cooperation with B01-62: compile ORIGINAL Codex MapStorage.cs.
This tests TryPath only; NO Unity or actual MapDraft JSON, NO durable inventory save.
SOURCE WITNESS, NOT a product security GREEN acceptance test.
"""
import hashlib
import json
import pathlib
import subprocess
import sys
import tempfile
import urllib.request

CODEx_HEAD = "231005c6d0351945a8ee6684f3856af118f0e94f"
ORIGINAL_BLOB = "f31a94c16e8cb244e66e39b0ac344b085095b630"
SOURCE_PATH = "Assets/MapEditor/MapStorage.cs"

PROJECT = """<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup>
  <OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable>
  <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
<ItemGroup>
  <Compile Include="OriginalMapStorage.cs" />
  <Compile Include="UnrelatedSignatures.cs" />
  <Compile Include="Program.cs" />
</ItemGroup>
</Project>
"""
SIGNATURES = """// Minimal unexecuted signatures to compile ORIGINAL MapStorage.cs outside Unity.
namespace UnityEngine {
  public static class Application {
    public static string persistentDataPath => System.IO.Path.GetTempPath();
  }
}
namespace RoboRougelike.MapEditor {
  public sealed class MapDraft {
    public string ToJson() => "{}"; // NEVER INVOKED
    public bool TryLoad(string input, out string error) {error = null;return false;} // NEVER INVOKED
  }
}
"""
PROGRAM = """using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using RoboRougelike.MapEditor;

static class Program {
  static void Assert(bool cond, string msg) { if(!cond)throw new Exception(msg); }
  static int Main(string[] args) {
    string root=Path.Combine(Path.GetTempPath(),"B01_22_MAP62_READONLY");
    string[] windowsReserved={"CON","con","PRN","AUX","NUL","COM1","COM9","LPT1","LPT9"};
    int acceptedReserved=0,positive=0,rejectedInvalid=0;
    foreach(string name in windowsReserved) {
      bool accepted=MapStorage.TryPath(root,name,out string path);
      Assert(accepted,"Original TryPath no longer admits "+name+"; update expected-RED witness");
      Assert(path==Path.Combine(root,name+".json"),"unexpected transformed reserved path");
      acceptedReserved++;
      Console.WriteLine("ORIGINAL_CSHARP_REPRO_RESERVED_ACCEPTED "+name);
    }
    foreach(string name in new[]{"Tor 1","CONSOLE","NUL-1"}) {
      Assert(MapStorage.TryPath(root,name,out string path),"positive rejected: "+name);
      Assert(path==Path.Combine(root,name+".json"),"positive path mismatch: "+name);
      positive++;
    }
    foreach(string name in new[]{"", ".", "..", "a/b", new string('a',65)}) {
      Assert(!MapStorage.TryPath(root,name,out _),"invalid control accepted: "+name);
      rejectedInvalid++;
    }
    Assert(MapStorage.TryPath(root,"Tor 1",out string a),"alias first");
    Assert(MapStorage.TryPath(root," Tor 1 ",out string b),"alias second");
    Assert(a==b,"prior D02 alias control unexpectedly no longer reproduced");
    // The alias is EXISTING D02, not a newly claimed independent defect.
    var report=new{
      test="B01-22_PASS12_B01-62_ORIGINAL_MAPSTORAGE_TYPATH",
      codex_head="231005c6d0351945a8ee6684f3856af118f0e94f",
      git_blob="f31a94c16e8cb244e66e39b0ac344b085095b630",
      compiled_original_csharp=true,compiled_unity_native="NOT_RUN",
      actual_windows_filesystem_write="NOT_RUN",
      actual_map_load_save="NOT_RUN",inventory_save="NOT_IMPLEMENTED",
      reserved_filenames_accepted=acceptedReserved, positive_controls=positive,
      negative_controls=rejectedInvalid, known_trim_alias_control=true,
      security_fix="NOT_FIXED", result="EXPECTED_BAD_INPUT_ACCEPTANCE_REPRODUCED"
    };
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0]))!);
    File.WriteAllText(args[0],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
    Console.WriteLine("ORIGINAL_MAPSTORAGE62_CSHARP 9/9 RESERVED_ACCEPTED_BAD "+
      positive+"/3 VALID_CONTROLS "+rejectedInvalid+
      "/5 INVALID_CONTROLS KNOWN_D02_ALIAS_REPRO NATIVE_NOT_RUN");
    return 0;
  }
}
"""

def blob_id(raw):
    return hashlib.sha1(b"blob " + str(len(raw)).encode("ascii") + b"\0" + raw).hexdigest()

def main():
    out = pathlib.Path("artifacts/chl-econ/mapstorage62-original-csharp.json")
    if len(sys.argv) > 1:
        out = pathlib.Path(sys.argv[1])
    url = ("https://raw.githubusercontent.com/C8LyNn/RoboRougelike/" +
           CODEx_HEAD + "/" + SOURCE_PATH)
    with urllib.request.urlopen(url, timeout=30) as f:
        data = f.read()
    actual = blob_id(data)
    if actual != ORIGINAL_BLOB:
        raise RuntimeError("ORIGINAL_CODEX_MAPSTORAGE_GIT_BLOB_MISMATCH " + actual)
    print("ORIGINAL_CODEX_MAPSTORAGE_GIT_BLOB_VERIFIED", actual,
          "BYTES", len(data), "SHA256", hashlib.sha256(data).hexdigest())
    with tempfile.TemporaryDirectory(prefix="b01-22-mapstorage62-") as folder:
        work = pathlib.Path(folder)
        (work / "Map62.csproj").write_text(PROJECT, encoding="utf8")
        (work / "OriginalMapStorage.cs").write_bytes(data)
        (work / "UnrelatedSignatures.cs").write_text(SIGNATURES, encoding="utf8")
        (work / "Program.cs").write_text(PROGRAM, encoding="utf8")
        subprocess.run(["dotnet", "run", "--project", str(work / "Map62.csproj"),
                        "-c", "Release", "--", str(out.resolve())],
                       check=True)
    receipt = json.loads(out.read_text(encoding="utf8"))
    assert receipt["reserved_filenames_accepted"] == 9
    assert receipt["positive_controls"] == 3
    assert receipt["negative_controls"] == 5
    print("B01_22_B01_62_ORIGINAL_CSHARP_EXPECTED_BAD_STILL_OPEN")

if __name__ == "__main__":
    main()
