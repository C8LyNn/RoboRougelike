#!/usr/bin/env python3
"""Bundle original planner-produced data + real arcade source in one offline HTML file."""
import hashlib
import json
from pathlib import Path
import re
import sys

ROOT=Path(__file__).resolve().parent
EXPECTED_SOURCE="5be31039411f89f17654b1c332f8692883fa03cc"

def build(directory):
    directory=Path(directory)
    rows=json.loads((directory/"results.json").read_text(encoding="utf-8"))
    if rows.get("sourceSha")!=EXPECTED_SOURCE or rows.get("blueprints")!=18:
        raise ValueError("WRONG_OR_MISSING_ORIGINAL_CSHARP_DATA")
    blueprints=[]
    unique=set()
    for item in rows["generated"]:
        name=item["file"]
        if Path(name).name!=name or not name.startswith("P17-COURSE-"):
            raise ValueError("UNSAFE_SOURCE_FILENAME")
        m=json.loads((directory/name).read_text(encoding="utf-8"))
        if (m.get("sourceSha")!=EXPECTED_SOURCE or m.get("geometryHash")!=item["geometryHash"] or
           m.get("seed")!=item["seed"] or m.get("variantId")!=item["variant"] or
           m.get("recipe")!="ObstacleCourse" or m.get("nativeUnity")!="NOT_RUN"):
            raise ValueError("ORIGINAL_SOURCE_DATA_MISMATCH")
        if m["geometryHash"] in unique:raise ValueError("DUPLICATE_MAP")
        unique.add(m["geometryHash"])
        blueprints.append(m)
    if len(blueprints)!=18 or len(unique)!=18:raise ValueError("MISSING_MAPS")
    js_core=(ROOT/"core.mjs").read_text(encoding="utf-8")
    js_core=re.sub(r"(?m)^export ","",js_core)
    js_app=(ROOT/"viewer-app.js").read_text(encoding="utf-8")
    js=js_core+"\n"+js_app
    if "</script" in js.lower():raise ValueError("UNSAFE_SCRIPT_SENTINEL")
    data=json.dumps(blueprints,ensure_ascii=False,separators=(",",":"))
    data=data.replace("<","\\u003c")
    template=(ROOT/"viewer.template.html").read_text(encoding="utf-8")
    if template.count("__EMBEDDED_BLUEPRINTS__")!=1 or template.count("__EMBEDDED_JS__")!=1:
        raise ValueError("TEMPLATE_TOKENS_MISSING")
    html=template.replace("__EMBEDDED_BLUEPRINTS__",data).replace("__EMBEDDED_JS__",js)
    if "__EMBEDDED_" in html:raise ValueError("UNRESOLVED_TEMPLATE_TOKEN")
    dest=directory/"COURSE-DRIVE-001.html"
    dest.write_text(html,encoding="utf-8",newline="\n")
    receipt={"packageId":"COURSE-DRIVE-001","sourceSha":EXPECTED_SOURCE,
             "originalSourceBlueprints":18,"distinctGeometryHashes":18,
             "bundle":"COURSE-DRIVE-001.html",
             "bundleSHA256":hashlib.sha256(dest.read_bytes()).hexdigest(),
             "webApplication":"REAL_OFFLINE_HTML_JS",
             "arcadeLogic":"ACTUAL_NODE_TESTS_REQUIRED",
             "nativeUnity":"NOT_RUN","nativePhysX":"NOT_RUN"}
    (directory/"drive-receipt.json").write_text(
        json.dumps(receipt,indent=2,sort_keys=True)+"\n",encoding="utf-8",newline="\n")
    print("COURSE_DRIVE_ORIGINAL_18_MAPS_BUNDLED="+str(len(blueprints)))
    print("COURSE_DRIVE_HTML_SHA256="+receipt["bundleSHA256"])

if __name__=="__main__":
    if len(sys.argv)!=2:raise SystemExit("Usage: build_viewer.py original-CSharp-map-output-dir")
    build(sys.argv[1])
