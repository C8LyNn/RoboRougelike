#!/usr/bin/env python3
"""W34 B01-22 exact original #923 one-store long campaign source scope preflight."""
import json,subprocess
from pathlib import Path
base=Path(__file__).resolve().parents[3]
here=Path(__file__).resolve().parent
m=json.loads((here/"manifest.json").read_text())
def git(*xs):
    return subprocess.check_output(["git",*xs],cwd=base,text=True).strip()
assert m["parent_source_923_sha"]=="e6eb24b7a45a66a5fa5d039f59c80b7e4be4439b"
assert m["game_runtime_changes"]==0 and m["new_writers"]==0
changed={}
for line in git("diff","--name-status",m["parent_source_923_sha"],"HEAD").splitlines():
    mode,p=line.split("\t",1)
    changed[p]=mode
assert changed=={p:"A" for p in m["added_paths"]},("INVALID_OWNER_SCOPE",changed)
assert len(changed)==6,("EXPECTED_SIX_ADD_PATHS",len(changed))
for p,oid in m["pinned_own_blobs"].items():
    assert git("rev-parse","HEAD:"+p)==oid,("MISMATCHED_QA_BLOB",p)
for p,oid in m["original_923_product_blobs"].items():
    assert git("rev-parse","HEAD:"+p)==oid,("ORIGINAL_923_PRODUCT_MUTATED",p)
for p,blob in m["original_immutable_input_blobs"].items():
    file=base/p
    assert file.is_file() and git("hash-object",str(file))==blob,("INVALID_ORIGINAL_FOREIGN_GIT_BLOB",p)
assert m["native_g1"]=="NOT_RUN"
assert m["owner_policy_834"]=="PENDING"
assert m["real_game_physics_adapter"]=="NOT_IMPLEMENTED"
assert m["world_save_owner"]=="ORIGINAL_B01_62_923"
assert "ReadClosedSlot" in (here/"Program.cs").read_text()
assert "TryStageTrustedVictory" in (here/"Program.cs").read_text()
assert "TryReserveOneShotLoaner" in (here/"Program.cs").read_text()
assert "W34_923_LOANER_SPENT_SOFTLOCK_RISK_SOURCE_RED" in (here/"Program.cs").read_text()
print("W34_923_EXACT_SOURCE_SCOPE_PASS original #923 0 runtime edits; 6 QA-only added; one real original atomic writer; original frozen V7; independent native G1 NOT_RUN")
