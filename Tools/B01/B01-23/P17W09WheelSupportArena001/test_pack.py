#!/usr/bin/env python3
import hashlib
import io
from pathlib import Path
import sys
import tempfile
import zipfile
import pack
raw=Path(sys.argv[1]).read_bytes()
count=0
def check(ok,name):
    global count
    if not ok:raise AssertionError("FAIL "+name)
    count+=1;print("PASS "+name)
def reject(contents,name):
    global count
    try:pack.verify(contents)
    except (ValueError,KeyError,AssertionError,zipfile.BadZipFile):
        count+=1;print("PASS rejects "+name);return
    raise AssertionError("FALSE_POSITIVE "+name)
check(pack.verify(raw)["native_unity"]=="NOT_RUN","true native status")
check(pack.build()[0]==raw,"cross-rebuild identical original bytes")
with zipfile.ZipFile(io.BytesIO(raw)) as z:
    original=[(n,z.read(n)) for n in z.namelist()]
check(len(original)==9,"8 original source + 1 embedded receipt")
def mutate(f):
    buffer=io.BytesIO()
    with zipfile.ZipFile(buffer,"w") as z:
        for p,data in f(original):z.writestr(pack.zi(p),data)
    return buffer.getvalue()
reject(mutate(lambda items:items[1:]),"missing source")
reject(mutate(lambda items:items+[items[0]]),"duplicate source")
reject(mutate(lambda items:items+[("../escape.cs",b"danger")]),"path escape")
reject(mutate(lambda items:items+[("Assets/R3/garage.cs",b"danger")]),"unauthorized game file")
reject(mutate(lambda items:[(p,d+b"MODIFIED" if p.endswith("WheelRigRecipe.cs") else d) for p,d in items]),"producer source tamper")
reject(mutate(lambda items:[(p,d.replace(b'"codex_ack": false',b'"codex_ack": true') if p=="PACKAGE_MANIFEST.json" else d) for p,d in items]),"forged Codex claim")
with tempfile.TemporaryDirectory(prefix="p17-w09-test-") as d:
    root=Path(d)/"original";pack.stage(raw,root)
    check((root/pack.FILES[1]).is_file(),"real source file physically staged")
    pack.rollback(raw,root);check(not root.exists(),"safe original rollback")
    root=Path(d)/"dirty";pack.stage(raw,root)
    changed=root/pack.FILES[1];old=changed.read_bytes();changed.write_bytes(b"tampered")
    try:pack.rollback(raw,root)
    except ValueError:check(changed.exists(),"refuse changed user file")
    else:raise AssertionError("missing refusal")
    changed.write_bytes(old);pack.rollback(raw,root)
    root=Path(d)/"foreign";pack.stage(raw,root)
    outsider=root/"Assets/R3/SAFE.txt";outsider.parent.mkdir();outsider.write_text("kept")
    try:pack.rollback(raw,root)
    except ValueError:check(outsider.exists(),"refuse foreign user file")
    else:raise AssertionError("unsafe removal")
    outsider.unlink();outsider.parent.rmdir();pack.rollback(raw,root)
    root=Path(d)/"foreign-dir";pack.stage(raw,root)
    (root/"Assets/ExternalEmptyFolder").mkdir()
    try:pack.rollback(raw,root)
    except ValueError:check((root/"Assets/ExternalEmptyFolder").exists(),"refuse foreign empty directory")
    else:raise AssertionError("unsafe empty folder removal")
    (root/"Assets/ExternalEmptyFolder").rmdir();pack.rollback(raw,root)
print("REAL_W09_ZIP_STAGE_NEGATIVE_CHECKS_PASS="+str(count)+" HASH="+hashlib.sha256(raw).hexdigest())
