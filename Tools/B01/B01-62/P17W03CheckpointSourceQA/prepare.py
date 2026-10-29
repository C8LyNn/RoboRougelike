#!/usr/bin/env python3
"""Generate isolated .NET8 compilable ORIGINAL Codex methods, fail closed on drift."""
import argparse
import pathlib
import subprocess

SOURCE = {
    "fixed": "203a0805251ed28293888e43f2d49f0df63767c6",
    "old": "a06dea84e11859a519a42f89c88b2dcdb055646f",
}
BLOBS = {
    "Assets/R1/R1RunCheckpoint.cs": "6eb8682c14329088d5698fc369009fb86b99b426",
}
MODEL_BLOBS = {
    "fixed": "29a85ae3ff0e761538c1937bf901d9a5ad573620",
    "old": "658b6377cf6f36496c1d555ff6b92447e58d23a9",
}
MODEL_PATH = "Assets/R3/R3ModelCheckpoint.cs"

def git(repo, *args):
    return subprocess.check_output(["git", "-C", str(repo), *args])

def extract(repo, output, mode):
    sha = SOURCE[mode]
    paths = dict(BLOBS)
    paths[MODEL_PATH] = MODEL_BLOBS[mode]
    output = pathlib.Path(output)
    output.mkdir(parents=True, exist_ok=True)
    for path, expected in paths.items():
        actual = git(repo, "rev-parse", f"{sha}:{path}").decode().strip()
        if actual != expected:
            raise RuntimeError(f"REJECT source Git drift {path}: {actual} != {expected}")
    source = git(repo, "show", f"{sha}:Assets/R1/R1RunCheckpoint.cs").decode("utf-8-sig")
    prefix_marker = "    public sealed partial class R1RunController"
    if source.count(prefix_marker) != 1:
        raise RuntimeError("Unexpected original controller boundary")
    prefix = source[:source.index(prefix_marker)]
    if prefix.count("public static class R1RunCheckpointStorage") != 1 or \
       prefix.count("public static R1RunCheckpoint Read(string path)") != 1 or \
       prefix.count("public static void Write(string path,R1RunCheckpoint document)") != 1:
        raise RuntimeError("Original complete R1 storage not found")
    # Preserve original storage and DTO byte-for-byte; ONLY drop the separate
    # Unity-heavy controller (not under test) and close its original namespace.
    (output / "OriginalRunCheckpoint.cs").write_bytes((prefix + "}\n").encode("utf-8"))
    model = git(repo, "show", f"{sha}:{MODEL_PATH}").decode("utf-8-sig")
    first = model.index("        static float Remaining(float at,float now)")
    last = model.index("        public R3ModelCheckpoint MakeCheckpoint(float now)", first)
    methods = model[first:last]
    exact_restore = "p.ReloadReadyAt=p.ReloadReadyAt>0?now+p.ReloadReadyAt:0;"
    if model.count(exact_restore) != 2:
        raise RuntimeError("Expected original mounted and inventory restore statements")
    # Original R3 SavePart (inclusive) reproduced without rewriting any method.
    # Extra bridge is test-only. Real Unity model TryRestoreCheckpoint is NOT executed.
    wrapper = ("using System;\nusing UnityEngine;\nnamespace RoboRougelike.R3 {\n"
               "public sealed partial class R3VehicleModel {\n"
               + methods
               + "public static R3PartState CallActualSavePart(R3PartState part,float now)"
                 " => SavePart(part,now);\n"
               "}\n}\n")
    (output / "OriginalSavePart.cs").write_bytes(wrapper.encode("utf-8"))
    root = pathlib.Path(__file__).resolve().parent
    for file in ("Shims.cs", "Program.cs", "Receiver.csproj"):
        (output / file).write_bytes((root / file).read_bytes())
    print("SOURCE_EXTRACTED_PINNED=PASS mode=" + mode
          + " commit=" + sha + " storage=" + BLOBS["Assets/R1/R1RunCheckpoint.cs"]
          + " model=" + MODEL_BLOBS[mode])

if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("--repo", required=True)
    p.add_argument("--out", required=True)
    p.add_argument("--mode", choices=SOURCE, required=True)
    a = p.parse_args()
    extract(a.repo, a.out, a.mode)
