#!/usr/bin/env python3
"""Assert current pre-fix source is RED, NEVER mislabel 3 failed contracts as PASS."""
import json, subprocess, sys
from pathlib import Path

HERE=Path(__file__).resolve().parent
OUT=Path("artifacts/chl-econ/weapon302-security-contract.json")
p=subprocess.run(["dotnet","run","--project",str(HERE/"SecurityContract.csproj"),
                  "-c","Release","--no-build","--",str(OUT)],
                 stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,check=False)
print(p.stdout)
if not OUT.exists():
    sys.exit("ERROR: contract executable did not produce original-source receipt")
data=json.loads(OUT.read_text())
expected={"E01_REJECT_INVALID_ENUM_BEFORE_SALVAGE_COMMIT",
          "E02_PREEXISTING_CORRUPT_REPAIR_IS_FAIL_CLOSED_ATOMIC",
          "E03_PREEXISTING_CORRUPT_DISMANTLE_NEVER_LOSES_ITEM"}
cases=data["cases"]
present={x["id"] for x in cases}
if p.returncode!=1 or present!=expected or len(cases)!=3 or data["security_contract_fail"]!=3:
    sys.exit("SECURITY_CONTRACT_NOT_EXPECTED_PRE_FIX_RED: "+
             json.dumps({"exit":p.returncode,"outcomes":cases}))
print("EXPECTED_RED_CONFIRMED 3/3 ORIGINAL_CODEX_R3_CONTRACT_FAILURES; NOT_SECURITY_FIXED")
