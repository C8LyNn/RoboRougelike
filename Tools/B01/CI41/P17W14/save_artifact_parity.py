#!/usr/bin/env python3
"""CI41-SAVE-ARTIFACT-PARITY-001: real immutable archive parity + RED QA hold.

Original #544/#549 are *unselected proposals*. Successful source ZIP generation
never approves a Save/Load host, independent QA, Unity G1, or Codex integration.
"""
import argparse
import hashlib
import io
import json
import os
from pathlib import PurePosixPath,Path
import re
import struct
import sys
import urllib.request
import urllib.error
import urllib.parse
import zipfile

REPO="C8LyNn/RoboRougelike"
SELECTED_GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SPECS={
    "544":{"pr":544,"run":41082219558,
        "sha":"e77529262b8f03c6e9f0e25b642038c351872044",
        "inner_sha256":"3b7a4e8524732faafe0b770e557fb594af7a03997fc7d014e7d22ab99fce50dd",
        "inner_bytes":41644,"members":8,
        "artifacts":{"Windows":12989423688,"Linux":12990211693},
        "manifest_source_key":"own_source_sha","manifest_game_key":"only_game_candidate_sha",
        "manifest_members_key":"members",
        "release":"INDEPENDENT_SECURITY_QA_PENDING"},
    "549":{"pr":549,"run":41082728707,
        "sha":"6da2c523e03766cd5a6deaed0cb3cccf8765354b",
        "inner_sha256":"97656f6cb2a08820f8682079c9b896cd58ca9df0f94150ae9bdbf8e4b6a13eee",
        "inner_bytes":55682,"members":13,
        "artifacts":{"Windows":12991140072,"Linux":12989419205},
        "manifest_source_key":"own_head_sha","manifest_game_key":"selected_secure_v2_sha",
        "manifest_members_key":"files",
        "release":"SECURITY_RED_FROM_INDEPENDENT_550"},
}
QA_550={"run":41082794585,"head_sha":"be2f8e76a881481c904ecc368c17218c80bd1709",
        "artifact":12989159727,
        "marker":b"SAVE_CONTRACT_RELEASE_GATE_RED=MUTABLE_VALIDATED_SNAPSHOT_AND_EXPOSED_LEDGER"}
MAX_DOWNLOAD=5_000_000
MAX_MEMBERS=60
MAX_UNCOMPRESSED=2_000_000
SHA_RE=re.compile(r"^[0-9a-f]{64}$")

class Refuse(Exception):
    pass

def sha(data):
    return hashlib.sha256(data).hexdigest()

def check(ok,reason):
    if not ok: raise Refuse(reason)

def zip_files(raw,outer=False):
    check(type(raw) is bytes and len(raw)<=MAX_DOWNLOAD,"OVERSIZED_OR_MISSING_ZIP")
    try:
        z=zipfile.ZipFile(io.BytesIO(raw),"r")
    except (ValueError,zipfile.BadZipFile) as exc:
        raise Refuse("INVALID_ZIP") from exc
    files={}
    with z:
        infos=z.infolist()
        check(0<len(infos)<=MAX_MEMBERS,"BAD_ZIP_MEMBER_COUNT")
        # Critical cross-OS ZIP path safety: Python zipfile normalizes raw
        # backslash names to "/" on Windows while constructing ZipInfo.
        # Inspect ORIGINAL local and central directory header bytes first.
        central=z.start_dir
        for item in infos:
            check(central>=0 and raw[central:central+4]==b"PK\\x01\\x02",
                  "INVALID_RAW_CENTRAL_DIRECTORY")
            check(central+46<=len(raw),"TRUNCATED_RAW_CENTRAL_HEADER")
            name_len,extra_len,comment_len=struct.unpack_from("<HHH",raw,central+28)
            central_end=central+46+name_len+extra_len+comment_len
            check(central_end<=len(raw) and name_len>0,"TRUNCATED_RAW_CENTRAL_NAME")
            central_name=raw[central+46:central+46+name_len]
            central=central_end
            offset=item.header_offset
            check(offset>=0 and raw[offset:offset+4]==b"PK\\x03\\x04",
                  "INVALID_RAW_LOCAL_HEADER")
            check(offset+30<=len(raw),"TRUNCATED_RAW_LOCAL_HEADER")
            local_name_len,local_extra_len=struct.unpack_from("<HH",raw,offset+26)
            check(offset+30+local_name_len+local_extra_len<=len(raw) and local_name_len>0,
                  "TRUNCATED_RAW_LOCAL_NAME")
            local_name=raw[offset+30:offset+30+local_name_len]
            check(b"\\" not in central_name and b"\\" not in local_name,
                  "UNSAFE_ZIP_MEMBER_RAW_BACKSLASH")
            check(central_name==local_name,"CENTRAL_LOCAL_FILENAME_MISMATCH")
            p=item.filename
            check(p and not item.is_dir() and not p.startswith("/") and
                  "\\" not in p and "\x00" not in p and
                  ":" not in p and p==PurePosixPath(p).as_posix() and
                  not any(x in ("",".","..") for x in p.split("/")),
                  "UNSAFE_ZIP_MEMBER")
            check(p not in files,"DUPLICATE_ZIP_MEMBER")
            mode=(item.external_attr>>16)&0o170000
            check(mode in (0,0o100000),"ZIP_SYMLINK_OR_SPECIAL_MEMBER")
            check(0<=item.file_size<=MAX_UNCOMPRESSED,"ZIP_BOMB_MEMBER")
            check(sum(len(b) for b in files.values())+item.file_size<=MAX_DOWNLOAD,
                  "ZIP_BOMB_TOTAL")
            try:
                content=z.read(item)
            except (RuntimeError,zipfile.BadZipFile,ValueError) as exc:
                raise Refuse("CORRUPT_ZIP_FILE") from exc
            check(len(content)==item.file_size,"ZIP_LENGTH_MISMATCH")
            files[p]=content
    return files

def inspect_inner(variant,raw):
    check(variant in SPECS,"UNKNOWN_VARIANT")
    spec=SPECS[variant]
    check(len(raw)==spec["inner_bytes"],"INNER_SIZE_MISMATCH")
    check(sha(raw)==spec["inner_sha256"],"PINNED_INNER_SHA256_MISMATCH")
    entries=zip_files(raw)
    check(len(entries)==spec["members"] and "PACKAGE_MANIFEST.json" in entries,
          "UNEXPECTED_INNER_MEMBERS")
    try: manifest=json.loads(entries["PACKAGE_MANIFEST.json"].decode("utf-8"))
    except (ValueError,UnicodeError) as exc:raise Refuse("INVALID_INNER_MANIFEST") from exc
    check(type(manifest) is dict,"BAD_MANIFEST_SHAPE")
    check(manifest.get(spec["manifest_source_key"])==spec["sha"],"MANIFEST_PRODUCER_SHA_MISMATCH")
    check(manifest.get(spec["manifest_game_key"])==SELECTED_GAME,"SELECTED_GAME_SHA_MISMATCH")
    check(manifest.get("codex_ack") is False,"FORGED_CODEX_ACK")
    check(manifest.get("unity_native",manifest.get("native_unity"))=="NOT_RUN","FORGED_UNITY_G1")
    if variant=="544":check(manifest.get("canonical_save_restored") is False,"FORGED_GAME_SAVE_RESTORED")
    if variant=="549":check(manifest.get("savegame_host")=="NOT_INTEGRATED","FORGED_HOST_INTEGRATION")
    records=manifest.get(spec["manifest_members_key"])
    check(type(records) is list and len(records)==len(entries)-1,"BAD_MEMBER_MANIFEST_COUNT")
    verified=[]
    for record in records:
        check(type(record) is dict and type(record.get("path")) is str,"BAD_MEMBER_MANIFEST")
        name=record["path"]
        check(name!="PACKAGE_MANIFEST.json" and name in entries and name not in verified,
              "MISSING_OR_DUPLICATED_MANIFEST_FILE")
        content=entries[name]
        size=record.get("bytes") if variant=="544" else record.get("size")
        check(type(size) is int and size==len(content),"MANIFEST_FILE_BYTES_MISMATCH")
        check(type(record.get("sha256")) is str and SHA_RE.fullmatch(record["sha256"]) and
              sha(content)==record["sha256"],"MANIFEST_FILE_SHA256_MISMATCH")
        verified.append(name)
    check(set(verified)==set(entries)-{"PACKAGE_MANIFEST.json"},"INCOMPLETE_MANIFEST_MEMBERS")
    return {"producer_sha":spec["sha"],"inner_sha256":sha(raw),
            "bytes":len(raw),"members":len(entries),"verified_source_members":len(verified),
            "selected_game_sha":SELECTED_GAME,"manifest_native_unity":"NOT_RUN"}

def inspect_outer(variant,outer):
    contents=zip_files(outer,outer=True)
    nested=[(p,b) for p,b in contents.items() if p.lower().endswith(".zip")]
    check(len(nested)==1,"OUTER_MUST_CONTAIN_EXACTLY_ONE_SOURCE_ZIP")
    return inspect_inner(variant,nested[0][1])

def inspect_qa_550(outer):
    files=zip_files(outer,outer=True)
    check(any(QA_550["marker"] in content for content in files.values()),
          "MISSING_INDEPENDENT_550_SECURITY_RED_WITNESS")
    return "SOURCE_SECURITY_RED_REPRODUCED_BY_INDEPENDENT_550"

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self,req,fp,code,msg,headers,newurl):
        return None

def from_api(url,token="",binary=False):
    prefix=f"https://api.github.com/repos/{REPO}/actions/"
    check(url.startswith(prefix),"UNAPPROVED_API_ORIGIN")
    headers={"Accept":"application/vnd.github+json","User-Agent":"CI41-archive-parity"}
    if token:headers["Authorization"]="Bearer "+token
    request=urllib.request.Request(url,headers=headers)
    try:
        with urllib.request.build_opener(NoRedirect).open(request,timeout=25) as response:
            raw=response.read(MAX_DOWNLOAD+1)
            check(len(raw)<=MAX_DOWNLOAD,"OVERSIZED_GITHUB_RESPONSE")
            return raw if binary else json.loads(raw.decode("utf-8"))
    except urllib.error.HTTPError as exc:
        check(binary and exc.code in (301,302,303,307,308),"GITHUB_API_HTTP_ERROR")
        location=exc.headers.get("Location","")
    except (urllib.error.URLError,TimeoutError,ValueError) as exc:
        raise Refuse("GITHUB_API_FETCH_FAILED") from exc
    parsed=urllib.parse.urlparse(location)
    host=(parsed.hostname or "").lower()
    check(parsed.scheme=="https" and (host=="objects.githubusercontent.com"
        or host.endswith(".blob.core.windows.net")
        or host.endswith(".actions.githubusercontent.com")),
        "UNTRUSTED_ARTIFACT_REDIRECT")
    # Signed artifact URL is accessed WITHOUT sending the GitHub token.
    try:
        with urllib.request.urlopen(urllib.request.Request(location,headers={
            "User-Agent":"CI41-archive-parity"}),timeout=30) as response:
            raw=response.read(MAX_DOWNLOAD+1)
    except (urllib.error.URLError,TimeoutError) as exc:
        raise Refuse("ARTIFACT_DOWNLOAD_FAILED") from exc
    check(len(raw)<=MAX_DOWNLOAD,"OVERSIZED_ARTIFACT")
    return raw

def original_artifact(run_id,expected_sha,artifact_id,token):
    run=from_api(f"https://api.github.com/repos/{REPO}/actions/runs/{run_id}",token)
    check(type(run) is dict and run.get("id")==run_id and
          run.get("head_sha")==expected_sha and run.get("status")=="completed" and
          run.get("conclusion")=="success","ORIGINAL_RUN_NOT_EXACT_SUCCESS")
    metadata=from_api(f"https://api.github.com/repos/{REPO}/actions/artifacts/{artifact_id}",token)
    check(type(metadata) is dict and metadata.get("id")==artifact_id
          and metadata.get("expired") is False and
          metadata.get("workflow_run",{}).get("id")==run_id and
          metadata.get("workflow_run",{}).get("head_sha")==expected_sha,
          "ARTIFACT_RUN_SOURCE_IDENTITY_MISMATCH")
    return from_api(f"https://api.github.com/repos/{REPO}/actions/artifacts/{artifact_id}/zip",token,binary=True)

def verify_sources(token):
    receipts={}
    for name,spec in SPECS.items():
        examples={}
        for os_name,artifact_id in spec["artifacts"].items():
            data=original_artifact(spec["run"],spec["sha"],artifact_id,token)
            examples[os_name]=inspect_outer(name,data)
        check(examples["Windows"]==examples["Linux"],"CROSS_OS_ORIGINAL_SOURCE_ARCHIVE_DRIFT")
        receipts[name]={"source_archive_parity":"PASS",**examples["Windows"],
                        "security_gate":spec["release"]}
    qa=original_artifact(QA_550["run"],QA_550["head_sha"],QA_550["artifact"],token)
    proof=inspect_qa_550(qa)
    return {"package":"CI41-SAVE-ARTIFACT-PARITY-001","version":"1.0.0",
            "selected_game_sha":SELECTED_GAME,"proposals":receipts,
            "independent_550":proof,
            "selected_save_contract":"NONE_DECIDED_BY_CI41",
            "save_load_host":"HOLD","native_unity_g1":"NOT_RUN",
            "g0":"NOT_GRANTED_BY_CI41","g2":"HOLD",
            "codex_ack":False,"integrated":False,"release_gate":"HOLD"}

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--output",required=True)
    args=p.parse_args()
    try:
        output=verify_sources(os.environ.get("GITHUB_TOKEN",""))
        code=0
    except (Refuse,OSError,UnicodeError) as exc:
        output={"package":"CI41-SAVE-ARTIFACT-PARITY-001","source_archive_parity":"FAIL_CLOSED",
                "reason":str(exc),"selected_save_contract":"NONE","save_load_host":"HOLD",
                "native_unity_g1":"NOT_RUN","release_gate":"HOLD"}
        code=2
    result=json.dumps(output,indent=2,sort_keys=True)+"\n"
    Path(args.output).write_text(result,encoding="utf-8")
    print(result,end="")
    return code

if __name__=="__main__":
    sys.exit(main())
