#!/usr/bin/env python3
"""B01-40 W05: verify *downloaded* original #411 Windows/Ubuntu staging bytes.

The two actual Actions artifacts are named:
b01-22-ek103-receiver-windows-2025 and b01-22-ek103-receiver-ubuntu-24.04,
both from exact original producer run 40968578571 on SHA b1d4e341...
This reader never updates original artifacts, game source or receipts.
"""
import argparse
import hashlib
import io
import json
from pathlib import Path
import zipfile

from receive_kits_v103 import (CODEX,NEW,ARCHIVE,Reject,canon,digest,need)
FILES=("catalog.json","offers.json","receipt.json")
PRODUCER_RUN=40968578571
PRODUCER_WIN_ARTIFACT=12939655855
PRODUCER_UBUNTU_ARTIFACT=12939461321
def read_owner(directory):
    path=Path(directory).absolute()
    need(path.is_dir() and not path.is_symlink(),"OWNER_ARTIFACT_NOT_DIRECTORY")
    entries=list(path.iterdir())
    need({x.name for x in entries}==set(FILES) and len(entries)==3 and
         all(x.is_file() and not x.is_symlink() for x in entries),
         "ORIGINAL_PRODUCER_ARTIFACT_MISSING_EXTRA_OR_SYMLINK")
    content={name:(path/name).read_bytes() for name in FILES}
    receipt=json.loads(content["receipt.json"])
    # Authenticate bytes BEFORE parsing potentially corrupt/untrusted owner JSON.
    need(receipt["modified_host"] is False and
         receipt["file_sha256"]=={n:digest(content[n]) for n in ("catalog.json","offers.json")},
         "ORIGINAL_PRODUCER_RECEIPT_SHA_FORGED")
    offers=json.loads(content["offers.json"])
    need(len(offers["offers"])==10 and offers["data_only"] is True and
         offers["host_transaction_required"] is True and
         offers["source_codex_sha"]==CODEX and
         offers["source_manifest_sha256"]==receipt["source_manifest_sha256"],
         "ORIGINAL_PRODUCER_OFFERS_PROVENANCE_INVALID")
    catalog=json.loads(content["catalog.json"])
    need(catalog["version"]=="1.0.3" and
         catalog["provenance"]["codex_sha"]==CODEX and
         len(catalog["kits"])==10,"ORIGINAL_PRODUCER_CATALOG_DRIFT")
    return content

def check(windows,ubuntu,receiver):
    receiver=Path(receiver).absolute()
    need(receiver.is_dir() and not receiver.is_symlink(),
         "B01_40_RECEIVER_OUTPUT_UNAVAILABLE")
    actual=(receiver/ARCHIVE).read_bytes()
    receipt=json.loads((receiver/"qa-source-receipt.json").read_bytes())
    need(receipt["source_checks"]["source_SHA"]==NEW and
         receipt["inner_archive_sha256"]==digest(actual),
         "INDEPENDENT_RECEIVER_ZIP_SHA_MISMATCH")
    with zipfile.ZipFile(io.BytesIO(actual)) as z:
        need(len(z.namelist())==4 and
             sorted(z.namelist())==sorted(["staged-original/"+x for x in FILES]+
                                          ["source-receipt.json"]),
             "INDEPENDENT_RECEIVER_ZIP_MEMBER_SET_WRONG")
        generated={name:z.read("staged-original/"+name) for name in FILES}
    original_win=read_owner(windows)
    original_ubuntu=read_owner(ubuntu)
    for name in FILES:
        need(original_win[name]==original_ubuntu[name],
             "PRODUCER_WIN_UBUNTU_ACTUAL_FILE_BYTES_DIFFER:"+name)
        need(original_win[name]==generated[name],
             "PRODUCER_ACTUAL_FILE_VERSUS_B01_40_RECEIVER_DIFFER:"+name)
    result={
      "gate":"B01_40_INDEPENDENT_ORIGINAL_PRODUCER_BOTH_OS_BINARY_FILE_PARITY_PASS",
      "source_sha":NEW,
      "producer_exact_run":PRODUCER_RUN,
      "producer_actual_windows_artifact":PRODUCER_WIN_ARTIFACT,
      "producer_actual_ubuntu_artifact":PRODUCER_UBUNTU_ARTIFACT,
      "original_files":3,
      "original_producer_sha256":{n:digest(original_win[n]) for n in FILES},
      "b01_40_real_receiver_inner_zip_sha256":digest(actual),
      "same_original_file_bytes_win_ubuntu_receiver":True,
      "current_codex_original_sha":CODEX,
      "source_g0":"PASS","native_unity":"NOT_RUN",
      "b01_60_independent_qa":"PENDING",
      "Codex_ack":"PENDING","G2":"HOLD"}
    return result

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--windows",required=True)
    p.add_argument("--ubuntu",required=True)
    p.add_argument("--receiver",required=True)
    p.add_argument("--proof",required=True)
    x=p.parse_args()
    try:
        proof=Path(x.proof).absolute()
        need(not proof.exists() and proof.parent.is_dir() and
             proof.parent==Path(x.receiver).absolute(),
             "PROOF_MUST_BE_NEW_EXTERNAL_RECEIVER_FILE")
        result=check(x.windows,x.ubuntu,x.receiver)
        with proof.open("xb") as f:f.write(canon(result))
        print(json.dumps(result,sort_keys=True))
    except (Reject,OSError,KeyError,ValueError,zipfile.BadZipFile) as exc:
        print("B0140_W05_ORIGINAL_OWNER_ARTIFACT_BINARY_REJECT "+str(exc),file=__import__("sys").stderr)
        raise SystemExit(2)
if __name__=="__main__":main()
