#!/usr/bin/env python3
"""Actual original binary receipt + adversarial mutations; NOT native Unity."""
import hashlib
import io
from pathlib import Path
import sys
import zipfile
import pack
raw=Path(sys.argv[1]).read_bytes()
checks=0
def positive(t,name):
    global checks
    if not t:raise AssertionError(name)
    checks+=1;print("PASS "+name)
def reject(data,name):
    global checks
    try:pack.verify(data)
    except (AssertionError,ValueError,KeyError,zipfile.BadZipFile,RuntimeError):
        checks+=1;print("PASS rejects "+name);return
    raise AssertionError("FALSE_ACCEPT "+name)
positive(pack.verify(raw)["native_unity"]=="NOT_RUN","original source receipt")
positive(pack.build()[0]==raw,"same original 4 member ZIP bytes")
positive(len(raw)>1000,"nonempty original binary")
with zipfile.ZipFile(io.BytesIO(raw)) as z:
    original=[(n,z.read(n)) for n in z.namelist()]
def mutate(transform):
    buf=io.BytesIO()
    with zipfile.ZipFile(buf,"w") as z:
        for name,data in transform(original):z.writestr(pack.zi(name),data)
    return buf.getvalue()
reject(mutate(lambda x:x+[("Assets/R3/Game.cs",b"fake")]),"unexpected Codex source")
reject(mutate(lambda x:x+[x[0]]),"duplicate entry")
reject(mutate(lambda x:x+[("../escape.cs",b"bad")]),"zip traversal")
reject(mutate(lambda x:x[1:]),"missing original file")
reject(mutate(lambda x:[(p,d+b"\n" if p.endswith("MeasuredSupportCapture.cs") else d) for p,d in x]),"source tamper")
reject(mutate(lambda x:[(p,d.replace(b'"codex_ack": false',b'"codex_ack": true') if p=="PACKAGE_MANIFEST.json" else d) for p,d in x]),"forged ACK")
print("ACTUAL_BINARY_ARCHIVE_CHECKS_PASS="+str(checks)+" SHA256="+hashlib.sha256(raw).hexdigest())
