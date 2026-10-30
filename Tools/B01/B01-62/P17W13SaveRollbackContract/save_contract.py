#!/usr/bin/env python3
"""B01-62 independent *reference contract* for #510 Save/Load. NOT a Unity serializer."""
import copy
import hashlib
import json
import math
import re

SELECTED="d94400d95d13db7f27e1533df05c9292d5380431"
VERSION="b01-62-secure-v2-state-fixture/1"
# Fixed QA-only identities; game must supply persistent identity assignment, NOT these GUIDs.
CATALOG=((2310001,"11111111111141118111111111111101"),
         (2310002,"22222222222242228222222222222202"))
MAX_BYTES=65536

class Rejected(ValueError):pass

def reject_if(condition,reason):
    if condition:raise Rejected(reason)

def canonical(obj):
    try:return (json.dumps(obj,sort_keys=True,separators=(",",":"),allow_nan=False,ensure_ascii=True)+"\n").encode("ascii")
    except (TypeError,ValueError) as e:raise Rejected("NON_CANONICAL_DATA") from e

def fresh():
    records=[]
    for part,guid in CATALOG:
        records.append({"id":part,"part_guid":guid,"detached":False,
         "consumed":False,"parent_guid":"00000000000040008000000000000001",
         "kinematic":True,"position":[float(part==2310002),0.0,0.0],
         "rotation":[0.0,0.0,0.0,1.0],
         "velocity":[0.0,0.0,0.0],"angular":[0.0,0.0,0.0]})
    return {"schema":VERSION,"selected_sha":SELECTED,
            "run_guid":"aaaaaaaaaaaa4aaa8aaaaaaaaaaaaaaa","generation":0,
            "transaction_active":False,"phase":"idle","parts":records}

def valid_vector(a,count,bound):
    return (isinstance(a,list) and len(a)==count and all(
        type(x) in (int,float) and math.isfinite(x) and abs(x)<=bound for x in a))

def validate(s):
    reject_if(not isinstance(s,dict) or set(s)!={"schema","selected_sha","run_guid","generation","transaction_active","phase","parts"},"SCHEMA_KEYS")
    reject_if(s["schema"]!=VERSION or s["selected_sha"]!=SELECTED,"SOURCE_VERSION")
    reject_if(not isinstance(s["run_guid"],str) or not re.fullmatch(r"[0-9a-f]{32}",s["run_guid"]),"RUN_GUID")
    reject_if(type(s["generation"]) is not int or not 0<=s["generation"]<2**31,"GENERATION")
    reject_if(s["transaction_active"] is not False or s["phase"]!="idle","LIVE_TRANSACTION")
    parts=s["parts"]
    reject_if(not isinstance(parts,list) or len(parts)!=len(CATALOG),"PART_COUNT")
    ids=set();guids=set()
    keys={"id","part_guid","detached","consumed","parent_guid","kinematic","position","rotation","velocity","angular"}
    for p in parts:
        reject_if(not isinstance(p,dict) or set(p)!=keys,"PART_SCHEMA")
        id=p["id"];g=p["part_guid"]
        reject_if(type(id) is not int or type(g) is not str or not re.fullmatch(r"[0-9a-f]{32}",g),"PART_ID_GUID_TYPE")
        reject_if(id in ids or g in guids,"DUPLICATE_ID_GUID")
        ids.add(id);guids.add(g)
        reject_if((id,g) not in CATALOG,"UNKNOWN_ID_GUID_PAIR")
        reject_if(type(p["detached"]) is not bool or type(p["consumed"]) is not bool or type(p["kinematic"]) is not bool,"BAD_STATE_TYPE")
        reject_if(p["detached"]!=p["consumed"],"LEDGER_STATE_MISMATCH")
        reject_if(p["detached"] == p["kinematic"],"PHYSICAL_STATE_MISMATCH")
        reject_if(p["parent_guid"]!=(None if p["detached"] else "00000000000040008000000000000001"),"PARENT_MISMATCH")
        for field,n,bound in (("position",3,1e6),("rotation",4,1),("velocity",3,1e4),("angular",3,1e4)):
            reject_if(not valid_vector(p[field],n,bound),"INVALID_VECTOR_"+field)
        reject_if(abs(sum(x*x for x in p["rotation"])-1)>0.001,"INVALID_QUATERNION")
    reject_if(ids!={x[0] for x in CATALOG},"MISSING_REQUIRED_ID")
    return True

def encode(state):
    validate(state)
    payload=canonical(state)
    envelope={"payload":state,"checksum_sha256":hashlib.sha256(payload).hexdigest()}
    raw=canonical(envelope)
    reject_if(len(raw)>MAX_BYTES,"TOO_BIG")
    return raw

def distinct_pairs(pairs):
    d={}
    for k,v in pairs:
        if k in d:raise Rejected("DUPLICATE_JSON_KEY")
        d[k]=v
    return d

def decode(raw):
    reject_if(not isinstance(raw,bytes) or len(raw)>MAX_BYTES,"INVALID_SIZE_OR_TYPE")
    try:
        outer=json.loads(raw.decode("utf-8"),parse_constant=lambda x: (_ for _ in ()).throw(Rejected("NONFINITE_JSON")),object_pairs_hook=distinct_pairs)
    except (ValueError,UnicodeError,TypeError) as e:raise Rejected("CORRUPT_SAVE") from e
    reject_if(not isinstance(outer,dict) or set(outer)!={"payload","checksum_sha256"},"ENVELOPE")
    reject_if(type(outer["checksum_sha256"]) is not str or not re.fullmatch(r"[0-9a-f]{64}",outer["checksum_sha256"]),"CHECKSUM_FORMAT")
    payload=outer["payload"]
    reject_if(not isinstance(payload,dict),"PAYLOAD")
    reject_if(hashlib.sha256(canonical(payload)).hexdigest()!=outer["checksum_sha256"],"BAD_CHECKSUM")
    validate(payload)
    return copy.deepcopy(payload)

def restore(target,raw,before_commit=None):
    """Prepare fully, then swap once. No mutating caller state until all validation and callbacks pass."""
    reject_if(not isinstance(target,dict),"BAD_TARGET")
    new_state=decode(raw)
    # A real Unity integration must also prepare *new* rig objects and W06 ledger off-scene
    # and only atomically commit after PhysX state is restorable. This code does not do that.
    if before_commit is not None:before_commit(copy.deepcopy(new_state))
    target.clear()
    target.update(new_state)
    return True

def sample_detached():
    s=fresh()
    s["parts"][0].update({"detached":True,"consumed":True,"parent_guid":None,
                         "kinematic":False,"velocity":[2.0,0.0,0.0]})
    validate(s)
    return s

def source_receipt():
    return {"schema":VERSION,"selected_sha":SELECTED,"kind":"QA_ONLY_REFERENCE","native_unity":"NOT_RUN",
            "game_save_load_implemented":False,"requires_idle_phase":True,
            "detached_id_is_consumed":True,"catalog":list(CATALOG)}
