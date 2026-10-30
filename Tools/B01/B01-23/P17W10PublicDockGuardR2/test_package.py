#!/usr/bin/env python3
import hashlib,io,sys,tempfile,zipfile
from pathlib import Path
import package
raw=Path(sys.argv[1]).read_bytes();n=0
def check(v,name):
    global n
    if not v:raise AssertionError("FALSE_GREEN "+name)
    n+=1;print("PACK_PASS "+name)
def reject(raw,name):
    global n
    try:package.verify(raw)
    except (ValueError,KeyError,AssertionError,zipfile.BadZipFile):
        n+=1;print("PACK_PASS rejected "+name);return
    raise AssertionError("UNSAFE_ACCEPT "+name)
m=package.verify(raw)
check(m["native_unity"]=="NOT_RUN" and m["codex_ack"]==False,"explicit native/ACK honesty")
check(m["original_legacy_blob"]==package.FROZEN_OLD_BLOB,"frozen old source pinned")
check(package.build()[0]==raw,"deterministic original exact source archive")
with zipfile.ZipFile(io.BytesIO(raw)) as z:members=[(p,z.read(p)) for p in z.namelist()]
def change(f):
    b=io.BytesIO()
    with zipfile.ZipFile(b,"w") as z:
        for name,data in f(members):z.writestr(package.zi(name),data)
    return b.getvalue()
reject(change(lambda a:a[1:]),"missing original source")
reject(change(lambda a:a+[a[0]]),"duplicate original member")
reject(change(lambda a:a+[("../file.cs",b"BAD")]),"ZIP slip")
reject(change(lambda a:a+[("Assets/R3/Codex.cs",b"BAD")]),"unexpected canonical overwrite")
reject(change(lambda a:[(p,x+b"MODIFIED" if p.endswith("DebrisRigPlayground.cs") else x) for p,x in a]),"mutated protected API")
reject(change(lambda a:[(p,x.replace(b'"codex_ack": false',b'"codex_ack": true') if p=="PACKAGE_MANIFEST.json" else x) for p,x in a]),"forged ACK")
with tempfile.TemporaryDirectory(prefix="w10-real-source-") as root:
    temp=Path(root);dest=temp/"stage"
    package.stage(raw,dest)
    check((dest/package.FILES[0]).is_file(),"physically staged actual producer source")
    try:package.stage(raw,dest)
    except ValueError:check(True,"refuse existing staging overwrite")
    else:raise AssertionError("unsafe overwrite")
    tampered=dest/package.FILES[0];saved=tampered.read_bytes();tampered.write_bytes(b"changed")
    try:package.rollback(raw,dest)
    except ValueError:check(tampered.exists(),"refuse user-modified file")
    else:raise AssertionError("lost modified file")
    tampered.write_bytes(saved)
    external=dest/"Assets/foreign";external.mkdir()
    try:package.rollback(raw,dest)
    except ValueError:check(external.exists(),"refuse even foreign empty directory")
    else:raise AssertionError("lost foreign directory")
    external.rmdir()
    foreign=dest/"Assets/codex.txt";foreign.write_text("keep")
    try:package.rollback(raw,dest)
    except ValueError:check(foreign.exists(),"refuse foreign user file")
    else:raise AssertionError("lost user file")
    foreign.unlink()
    package.rollback(raw,dest);check(not dest.exists(),"safe external rollback")
print("ACTUAL_ORIGINAL_PHYSICAL_DELTA_TESTS_PASS="+str(n)+" ZIP_SHA256="+hashlib.sha256(raw).hexdigest())
