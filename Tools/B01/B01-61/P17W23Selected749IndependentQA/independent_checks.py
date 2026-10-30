#!/usr/bin/env python3
"""Additional B01-61 source-boundary assertions, no copied producer test program."""
from pathlib import Path
import re,subprocess
R=Path(__file__).resolve().parents[4]
P=R/"Tools/B01/B01-62/P17W23OneAuthorityV3"
STORE=(P/"CanonicalSingleAuthorityStoreV3.cs").read_text()
SESSION=(P/"CanonicalSlotSessionV3.cs").read_text()
GATE=(R/"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs").read_text()
LEASE=(R/"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs").read_text()
checks={
"single original lease acquired before store created":SESSION.find("CanonicalWorldSlotLease.TryAcquire")>=0 and SESSION.find("CanonicalWorldSlotLease.TryAcquire")<SESSION.find("CanonicalSingleAuthorityStoreV3.TryCreateNew"),
"session binds same acquired owner object to gateway and full Save store":"new CanonicalWorldGateway(gate,factory,this,authority)" in SESSION and "CanonicalSingleAuthorityStoreV3.TryCreateNew(dir,run,guid,owner" in SESSION,
"store requires live bound owner for reads and writes":"authority.Live" in STORE,
"store never opens independent slot lease":not re.search(r'FileStream\s*\([^;]*slot\.lease',STORE,re.S) and 'private const string Marker = "slot.canonical-v3"' in STORE,
"session still holds authority on quarantined unpublished world":"if(!HasWorld&&!restoring&&!Quarantined)" not in SESSION and "Unretired.Add(this)" in SESSION,
"false or throwing physical retirement proof cannot free slot":"TryCloseAfterPhysicalRetirement" in SESSION and "allOwned" in SESSION,
"storage never claims independent lifetime lock":"private readonly CanonicalWorldSlotLease authority" in STORE and not re.search(r"new\s+FileStream\s*\(",STORE),
"API gateway requires real selected owner lease":"CanonicalWorldSlotLease" in GATE and "BindOnce" in GATE,
"original OS owner acquired exclusively":"FileShare.None" in LEASE,
"full envelope carries generation, slot, payload and checksum":all(x in STORE for x in ("bw.Write(generation)","bw.Write(slotId.ToByteArray())","bw.Write(raw)","SHA256.HashData")),
"foreign legacy slot marker segregated":'slot.canonical-v3' in STORE and "WrongOwner" in STORE,
"source-level unresolved true private frozen game adapter": "does not gain access" in SESSION and "Rig[]" in SESSION
}
for k,v in checks.items(): print(("PASS" if v else "FAIL")+" "+k)
print("W23_SELECTED_749_ADDITIONAL_SOURCE_BOUNDARY_PASS="+str(sum(checks.values()))+" FAIL="+str(len(checks)-sum(checks.values())))
raise SystemExit(0 if all(checks.values()) else 1)
