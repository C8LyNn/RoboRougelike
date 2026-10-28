#!/usr/bin/env python3
"""MAPSAFE-001: independent offline backup/restore for map-editor JSON files."""
import argparse, hashlib, json, os, re, shutil, sys, tempfile, uuid
from pathlib import Path
from datetime import datetime, timezone

MAX_BYTES=1024*1024
IDENT=re.compile(r"^[0-9a-f]{32}$")
SHA=re.compile(r"^[0-9a-f]{64}$")
DEVICES={"CON","PRN","AUX","NUL","CONIN$","CONOUT$"}

class Refused(Exception): pass

def require(ok, why):
    if not ok: raise Refused(why)

def safe_name(name):
    require(type(name) is str and 0<len(name)<=64,"Map name length")
    require(name==name.strip() and all(c.isalnum() or c in "_- " for c in name),"Noncanonical map name")
    u=name.upper()
    require(u not in DEVICES and not (len(u)==4 and u[:3] in ("COM","LPT") and u[3] in "123456789¹²³"),"Windows device name")

def symlink_guard(path):
    p=path.absolute()
    for part in (p,*p.parents):
        require(not part.is_symlink(),"Symlink refused")

def folder(path):
    symlink_guard(path)
    require(path.is_dir(),"Existing folder required")
    return path.resolve()

def target_path(maps,name):
    safe_name(name); maps=folder(maps)
    target=maps/(name+".json"); symlink_guard(target)
    for item in maps.iterdir():
        if item.name.casefold()==target.name.casefold() and item.name!=target.name:
            raise Refused("Casefold alias collision")
    return target

def read_bytes(path):
    symlink_guard(path)
    require(path.is_file() and path.stat().st_size<=MAX_BYTES,"Missing or oversized map")
    data=path.read_bytes()
    require(len(data)<=MAX_BYTES,"Map over 1MiB")
    return data

def check_map(data):
    try: doc=json.loads(data.decode("utf-8-sig"))
    except (ValueError,UnicodeError) as e: raise Refused("Corrupt JSON "+str(e))
    require(type(doc) is dict and type(doc.get("version")) is int and doc["version"] in (1,2),"Unsupported version")
    items=doc.get("items"); require(type(items) is list and len(items)<=4000,"Invalid items")
    ground,objects=set(),set()
    n_ground=n_objects=spawn=finish=0
    for p in items:
        require(type(p) is dict and all(type(p.get(k)) is int for k in ("kind","x","z","rotation")),"Invalid item")
        kind,x,z,rotation=(p[k] for k in ("kind","x","z","rotation"))
        require(0<=kind<=9 and abs(x)<=50 and abs(z)<=50 and 0<=rotation<4,"Invalid item position")
        scales=[1,1,1] if doc["version"]==1 else [p.get(k,1) for k in ("scaleX","scaleY","scaleZ")]
        require(all(type(s) is int and 1<=s<=4 for s in scales),"Invalid scale")
        cells=set()
        for dx in range(scales[0]):
            for dz in range(scales[2]):
                rx,rz=((dx,dz),(dz,-dx),(-dx,-dz),(-dz,dx))[rotation]
                a,b=x+rx,z+rz
                require(abs(a)<=50 and abs(b)<=50,"Footprint exceeds bounds")
                cells.add((a,b))
        layer=ground if kind==0 else objects
        require(layer.isdisjoint(cells),"Overlapping items")
        layer.update(cells)
        n_ground+=kind==0; n_objects+=kind!=0
        spawn+=kind==4; finish+=kind==5
    require(n_ground<=2000 and n_objects<=2000 and spawn<=1 and finish<=1,"Invalid object limits")
    return doc["version"]

def digest(data): return hashlib.sha256(data).hexdigest()

def vault_dir(vault,maps=None):
    vault=folder(vault)
    if maps is not None:
        maps=folder(maps)
        require(vault!=maps and maps not in vault.parents,"Vault must be outside maps folder")
    return vault

def save_snapshot(vault,name,body,kind,restorable):
    uid=uuid.uuid4().hex
    stage=vault/("."+uid+".stage")
    final=vault/uid
    stage.mkdir(mode=0o700)
    try:
        with (stage/"payload.json").open("xb") as f:
            f.write(body); f.flush(); os.fsync(f.fileno())
        info={"schema":1,"id":uid,"name":name,"sha256":digest(body),
              "bytes":len(body),"kind":kind,"restorable":restorable,
              "created_utc":datetime.now(timezone.utc).isoformat()}
        with (stage/"manifest.json").open("x",encoding="utf8") as f:
            json.dump(info,f,sort_keys=True,indent=2); f.flush(); os.fsync(f.fileno())
        require(not final.exists(),"Snapshot id conflict")
        stage.rename(final)
        return info
    finally:
        if stage.exists(): shutil.rmtree(stage)

def verify(vault,uid):
    require(bool(IDENT.fullmatch(uid)),"Invalid backup id")
    place=vault/uid; symlink_guard(place)
    require(place.is_dir(),"Snapshot missing")
    infofile=place/"manifest.json"; symlink_guard(infofile)
    require(infofile.is_file() and infofile.stat().st_size<8192,"Manifest missing or too large")
    try: info=json.loads(infofile.read_text("utf8"))
    except (UnicodeError,ValueError) as e: raise Refused("Invalid manifest: "+str(e))
    require(type(info) is dict and info.get("schema")==1 and info.get("id")==uid,"Invalid manifest identity")
    safe_name(info.get("name"))
    require(info.get("kind") in ("manual","pre-restore") and type(info.get("restorable")) is bool,"Invalid manifest kind")
    require(type(info.get("bytes")) is int and 0<=info["bytes"]<=MAX_BYTES and isinstance(info.get("sha256"),str) and SHA.fullmatch(info["sha256"]),"Bad size/hash")
    body=read_bytes(place/"payload.json")
    require(len(body)==info["bytes"] and digest(body)==info["sha256"],"Payload hash mismatch")
    if info["restorable"]: check_map(body)
    return info,body

def backup(maps,vault,name):
    path=target_path(maps,name)
    data=read_bytes(path); check_map(data)
    require(read_bytes(path)==data,"Map changed during backup")
    return save_snapshot(vault,name,data,"manual",True)

def restore(maps,vault,name,uid,replace=False,expect=None):
    info,data=verify(vault,uid)
    require(info["name"]==name and info["restorable"],"Wrong map name or invalid restore")
    target=target_path(maps,name)
    previous=None
    if target.exists():
        require(replace and isinstance(expect,str) and SHA.fullmatch(expect),"Existing map requires --replace and --expect-sha256")
        previous=read_bytes(target)
        require(digest(previous)==expect,"Map hash changed")
    else:
        require(not replace and expect is None,"Replacement options require existing map")
    undo=None
    if previous is not None:
        valid=True
        try: check_map(previous)
        except Refused: valid=False
        undo=save_snapshot(vault,name,previous,"pre-restore",valid)
    fd,filename=tempfile.mkstemp(prefix="."+name+".restore-",suffix=".tmp",dir=maps)
    temp=Path(filename)
    try:
        with os.fdopen(fd,"wb") as f:
            f.write(data); f.flush(); os.fsync(f.fileno())
        if previous is not None:
            require(target.exists() and digest(read_bytes(target))==expect,"Target changed during restore")
            os.replace(temp,target)
        else:
            os.link(temp,target) # atomic NO-CLOBBER on local NTFS/POSIX
    finally:
        if temp.exists(): temp.unlink()
    require(digest(read_bytes(target))==info["sha256"],"Restore verification failed")
    return {"name":name,"restored_from":uid,"sha256":info["sha256"],"replaced":previous is not None,
            "undo_snapshot_id":undo["id"] if undo else None}

def main():
    parser=argparse.ArgumentParser(description="MAPSAFE-001 offline map snapshot and recovery")
    subs=parser.add_subparsers(dest="action",required=True)
    for action in ("backup","restore","verify","list"):
        p=subs.add_parser(action); p.add_argument("--vault-dir",type=Path,required=True)
        if action in ("backup","restore"):
            p.add_argument("--maps-dir",type=Path,required=True)
            p.add_argument("--name",required=True)
        if action in ("verify","restore"): p.add_argument("--backup-id",required=True)
        if action=="restore":
            p.add_argument("--replace",action="store_true")
            p.add_argument("--expect-sha256")
    a=parser.parse_args()
    try:
        vault=vault_dir(a.vault_dir,getattr(a,"maps_dir",None))
        if a.action=="backup": info=backup(a.maps_dir,vault,a.name)
        elif a.action=="restore": info=restore(a.maps_dir,vault,a.name,a.backup_id,a.replace,a.expect_sha256)
        elif a.action=="verify":
            info,_=verify(vault,a.backup_id); info={"valid":True,**info}
        else:
            good=[]; bad=[]
            for p in sorted(vault.iterdir()):
                if IDENT.fullmatch(p.name):
                    try:
                        entry,_=verify(vault,p.name); good.append(entry)
                    except Refused as e: bad.append({"id":p.name,"error":str(e)})
            info={"snapshots":good,"invalid_snapshots":bad}
        print(json.dumps({"ok":True,**info},ensure_ascii=False)); return 0
    except (Refused,OSError) as e:
        print(json.dumps({"ok":False,"error":str(e)},ensure_ascii=False),file=sys.stderr)
        return 2

if __name__=="__main__": raise SystemExit(main())
