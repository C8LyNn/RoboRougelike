#!/usr/bin/env python3
"""Patch immutable install-only host ports IN ARTIFACTS. No writes to upstream/active game."""
import hashlib,json,pathlib,sys
root=pathlib.Path(__file__).resolve().parents[3]
src=root/"Tools/B01/ECONLEGENDARY001/_upstream"
dest=root/"Tools/B01/ECONLEGENDARY001/_receiver"
out=root/(sys.argv[1] if len(sys.argv)>1 else "artifacts/econlegendary")
dest.mkdir(parents=True,exist_ok=True)
out.mkdir(parents=True,exist_ok=True)
receiver_artifact=out/"receiver"
receiver_artifact.mkdir(parents=True,exist_ok=True)
patches={
 "R3LegendaryPort.install.cs":[
  ("using RoboRougelike.B01.Legendary;","using RoboRougelike.B01.Legendary;\nusing RoboRougelike.B01.Economy.LegendaryGuard;"),
  ("public sealed class R3LegendaryPort : IAuthoritativeR3Port {","public sealed class R3LegendaryPort : IAuthoritativeR3Port, ISharedR3PhysicalOwnerIdentity {"),
  (" private readonly R3VehicleModel model;"," private readonly R3VehicleModel model;\n public object AuthoritativeR3Model => model;")
 ],
 "R3EconRebuildPort.install.cs":[
  ("using RoboRougelike.B01.Economy.Rebuild;","using RoboRougelike.B01.Economy.Rebuild;\nusing RoboRougelike.B01.Economy.LegendaryGuard;"),
  ("public sealed class EconRebuildR3Port : IAuthoritativeRebuildPort","public sealed class EconRebuildR3Port : IAuthoritativeRebuildPort, ISharedR3PhysicalOwnerIdentity"),
  (" private readonly R3VehicleModel model;"," private readonly R3VehicleModel model;\n public object AuthoritativeR3Model => model;")
 ]
}
files=[]
for name,edits in patches.items():
 old=(src/name).read_text()
 current=old
 for needle,substitute in edits:
  assert current.count(needle)==1,(name,needle)
  current=current.replace(needle,substitute)
 assert "AuthoritativeR3Model => model;" in current
 (dest/name).write_text(current)
 (receiver_artifact/name).write_text(current)
 assert (receiver_artifact/name).read_bytes()==(dest/name).read_bytes(),"Receiver artifact mismatch"
 files.append({"file":name,"upstream_sha256":hashlib.sha256(old.encode()).hexdigest(),
 "receiver_sha256":hashlib.sha256(current.encode()).hexdigest(),
 "changed_lines":len(current.splitlines())-len(old.splitlines())})
(out/"receiver-bridge-receipt.json").write_text(json.dumps({
 "purpose":"COMPILE_ONLY_RECEIVER_REFERENCE_NOT_INSTALLED",
 "upstream_legend":"dffabbc6960304fdb33322b5a23c884feac804c6",
 "upstream_econ":"12572117a3dce369257feabc57021cde58023369",
 "files":files,"native_unity":"NOT_RUN","g2":"HOLD"},indent=2)+"\n")
assert sorted(x.name for x in receiver_artifact.glob("*.cs"))==sorted(patches),"Receiver source missing"
print("RECEIVER_PATCH_SOURCE_PASS",len(files),"R3 PORTS")
print("RECEIVER_ARTIFACT_PASS",len(files),"INSTALL_ONLY_CSHARP_FILES")
