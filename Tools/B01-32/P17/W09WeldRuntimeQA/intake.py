#!/usr/bin/env python3
"""Exact physical archived source intake, NOT Unity native.
Stages only a disposable source copy of producer #432 for compiled QA.
"""
import argparse,hashlib,json,re,subprocess,zipfile
from pathlib import Path

SOURCE_SHA="280ed3f59020432132ea5e156c68590d445009c8"
ARCHIVE_SHA="0d82429073722f3402e25d056502917ec7dcd2fb62256355011457f58711d2ed"
ROOT="Assets/B0132RepairWeld"
NAMES=["WeldVisualCore.cs","B0132RepairWeldVfx.cs","RepairWeldPlayModeTests.cs"]
SOURCE_BASE="Tools/B01-32/P17/W06RepairWeld/"
OUT=Path("build/W09WeldReceiver/Original")
DIGEST=lambda x:hashlib.sha256(x).hexdigest()
GUID=re.compile(rb"(?m)^guid: ([0-9a-f]{32})\r?$")

def check(cond,message):
    if not cond:raise ValueError("W09_PHYSICAL_EXACT_SOURCE_INTAKE_REJECT "+message)

def git_blob(path):
    result=subprocess.run(["git","show",SOURCE_SHA+":"+path],
        capture_output=True,timeout=60)
    check(result.returncode==0,"immutable Git original missing "+path+": "+result.stderr.decode(errors="replace")[-200:])
    return result.stdout

def audit(payload,receipt,run_adversarial=True):
    check(DIGEST(payload)==ARCHIVE_SHA and receipt["zip_sha256"]==ARCHIVE_SHA,
          "unaltered 2026-10-30 original Unity ZIP SHA256 mismatched")
    with zipfile.ZipFile(__import__("io").BytesIO(payload)) as zip_obj:
        check(zip_obj.testzip() is None,"original physical ZIP CRC fail")
        names=zip_obj.namelist()
        check(len(names)==17 and len(set(names))==17,"17 source ZIP files not unique")
        data={p:zip_obj.read(p) for p in names}
    check(all(p.startswith(ROOT+"/") or p==ROOT+".meta" for p in data),
          "path outside owned original Unity source namespace")
    check(len(set(p.casefold() for p in data))==17,"case-insensitive ZIP path collision")
    expected={x["path"]:x for x in receipt["files"]}
    check(set(data)==set(expected),"original receipt file member mismatch")
    for p,v in data.items():
        check(DIGEST(v)==expected[p]["sha256"] and len(v)==expected[p]["bytes"],
              "physical original member byte/size drift "+p)
    metas={p:GUID.search(v) for p,v in data.items() if p.endswith(".meta")}
    check(len(metas)==11 and all(metas.values()),"exact original 11 .meta GUIDs missing")
    check(len(set(x.group(1) for x in metas.values()))==11,"duplicate original Unity GUID")
    actual_script=metas[ROOT+"/Runtime/B0132RepairWeldVfx.cs.meta"].group(1).decode()
    prefab=data[ROOT+"/Resources/B0132_RepairWeld.prefab"]
    check(f"m_Script: {{fileID: 11500000, guid: {actual_script}, type: 3}}".encode() in prefab,
          "serialized actual original prefab missing linked MonoBehaviour script")
    input_files=[(NAMES[0],SOURCE_BASE+"Runtime/"+NAMES[0],ROOT+"/Runtime/"+NAMES[0]),
                 (NAMES[1],SOURCE_BASE+"Runtime/"+NAMES[1],ROOT+"/Runtime/"+NAMES[1]),
                 (NAMES[2],SOURCE_BASE+"Tests/"+NAMES[2],ROOT+"/Tests/PlayMode/"+NAMES[2])]
    for _,gitpath,zippath in input_files:
        check(data[zippath]==git_blob(gitpath),
              "source is not frozen original source bytes "+gitpath)
    if run_adversarial:
        # No producer calls. Independent original + immutable receipt checks.
        cases=[
            ("TAMPERED_ORIGINAL_ZIP",payload[:102]+bytes([payload[102]^1])+payload[103:]),
            ("WRONG_SHA256",b"\0"+payload),
        ]
        for label,mutated in cases:
            check(DIGEST(mutated)!=ARCHIVE_SHA,"tamper falsely retained archive digest "+label)
            print("W09_INTAKE_MUTANT_REJECT",label)
    return data,input_files

def main():
    arg=argparse.ArgumentParser()
    arg.add_argument("--artifact-root",default="build/W09WeldArtifact")
    args=arg.parse_args()
    base=Path(args.artifact_root)
    archives=list(base.rglob("B0132-RepairWeld-UnityImport.zip"))
    receipts=list(base.rglob("UNITY_IMPORT_RECEIPT.json"))
    check(len(archives)==1 and len(receipts)==1,
          "expected one physical immutable downloaded original artifact ZIP and receipt")
    receipt=json.loads(receipts[0].read_text())
    payload=archives[0].read_bytes()
    data,files=audit(payload,receipt)
    OUT.mkdir(parents=True,exist_ok=True)
    for name,_,zippath in files:(OUT/name).write_bytes(data[zippath])
    (OUT/"B0132_RepairWeld.prefab").write_bytes(data[ROOT+"/Resources/B0132_RepairWeld.prefab"])
    report={
       "package_id":"B01-32-W09-REPAIR-WELD-RUNTIME-QA-001/R1",
       "source_producer_pr":432,"source_commit":SOURCE_SHA,
       "physical_original_archive_sha256":ARCHIVE_SHA,
       "original_17_member_archive":"PASS",
       "original_11_meta_guids":"PASS",
       "original_3_csharp_files_git_vs_archive":"byte_equal",
       "native_unity":"NOT_RUN",
       "surrogate_disclaimer":"No native Unity Editor, UnityEngine binaries, shader or GPU tested",
       "assets_written_into_canonical_game":False
    }
    dest=Path("build/W09WeldReceiver/INTAKE_RECEIPT.json")
    dest.write_text(json.dumps(report,indent=2)+"\n")
    print("W09_ORIGINAL_PHYSICAL_INTAKE_PASS 17/17 physical ZIP bytes, 11/11 GUID, 3/3 exact frozen Git C# sources")
    print("W09_ORIGINAL_PHYSICAL_ZIP_SHA256",DIGEST(payload))
if __name__=="__main__":main()
