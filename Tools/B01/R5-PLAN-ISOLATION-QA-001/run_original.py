#!/usr/bin/env python3
"""Compile and execute frozen Codex original R5 C# against frozen B01-10 deck.

The original C# source files and JSON are verified as immutable Git blobs
and copied into an OUTSIDE-REPOSITORY temporary .NET8 project. The only
Unity API replacements are explicit test doubles in this QA-only folder.
No canonical Unity workspace or in-game asset is touched.
"""
import argparse
import json
import shutil
import subprocess
import tempfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
QA=Path(__file__).resolve().parent
CODEX="682dc0c5c13615577334375ffb6c27be5cdebf4a"
OWNER="c44f98bb5328848f1d9fc15ed19d6facf3cbee39"
FILES=[
(CODEX,"Assets/R5/R5RunPlan.cs","6fde58f1fc621cde6488d9ee81c3510aa729839a","R5RunPlan.cs"),
(CODEX,"Assets/R1/R1RunDefinition.cs","5665e64d3d665c67d352eb5a9809a66fc5136698","R1RunDefinition.cs"),
(CODEX,"Assets/R3/R3EnemyArchetype.cs","66cd1ee734b129117b3753d1205469a0496cb673","R3EnemyArchetype.cs"),
(OWNER,"Research/B01-10/P17/R5_EncounterDeck_BAL17.json",
 "762009d1bde7978aeb5c842dd08d2efdddcec646","R5_EncounterDeck_BAL17.json"),
]
def run(cmd,**kw):
    return subprocess.run([str(x) for x in cmd],check=True,capture_output=True,**kw)
def original(commit,path,blob):
    raw=run(["git","-C",ROOT,"show",f"{commit}:{path}"]).stdout
    actual=run(["git","-C",ROOT,"hash-object","--stdin"],input=raw).stdout.decode().strip()
    if actual!=blob:
        raise RuntimeError(f"IMMUTABLE_ORIGINAL_GIT_BLOB_MISMATCH {path}: {actual}")
    return raw

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--output",required=True)
    args=parser.parse_args()
    output=Path(args.output).resolve()
    output.parent.mkdir(parents=True,exist_ok=True)
    if shutil.which("dotnet") is None:
        raise RuntimeError("DOTNET8_REQUIRED")
    for rev in (CODEX,OWNER):
        run(["git","-C",ROOT,"fetch","--no-tags","origin",rev])
    with tempfile.TemporaryDirectory(prefix="b0160-original-r5-") as td:
        tmp=Path(td)
        for commit,rel,blob,dest in FILES:
            (tmp/dest).write_bytes(original(commit,rel,blob))
        for file in ("Program.cs","Harness.csproj","UnityJsonShim.cs"):
            shutil.copyfile(QA/file,tmp/file)
        intermediate=tmp/"r5-output.json"
        result=run(["dotnet","run","--project",tmp/"Harness.csproj",
                    "--configuration","Release","--",tmp/"R5_EncounterDeck_BAL17.json",
                    intermediate],cwd=tmp)
        parsed=json.loads(intermediate.read_text(encoding="utf-8"))
        parsed.update({
            "original_codex_game_sha":CODEX,
            "original_b01_10_producer_sha":OWNER,
            "original_git_blobs":{p:b for _,p,b,_ in FILES},
            "qa_tool_source":"Tools/B01/R5-PLAN-ISOLATION-QA-001/",
            "compile_test_host":"dotnet8; explicitly shimmed Unity API; no native Unity editor",
            "source_git_verified":True
        })
        output.write_text(json.dumps(parsed,indent=2,ensure_ascii=False,sort_keys=True)+"\n",
                          encoding="utf-8")
        print(result.stdout.decode())
        print("B0160_PASS17_R5_ORIGINAL_GIT_SHA_VERIFIED="+json.dumps(
            {"codex":CODEX,"producer":OWNER,
             "original_csharp_blobs":[x[2] for x in FILES[:3]],
             "original_deck_blob":FILES[-1][2]},sort_keys=True))
        print("B0160_PASS17_R5_FINAL_RECEIPT="+json.dumps(parsed,sort_keys=True))
if __name__=="__main__":
    main()
