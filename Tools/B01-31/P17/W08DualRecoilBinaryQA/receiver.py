#!/usr/bin/env python3
"""B01-31 independently validates real B01-32 #446 PNGs and Unity binary ZIP.
No producer renderer execution; does not touch game Assets or Codex code.
"""
import argparse,hashlib,io,json,math,re,subprocess,sys,zipfile
from pathlib import Path
from PIL import Image,ImageChops

SHA="d020db8bf43626dfecf979ffc47b57dd526a7ca0"
ZIP_HASH="0260a77c34fbde91c3349f6989afd390a6299a686fe472fe83432ed8aaf35d25"
BASE="Assets/B0132DualRecoil"
SRC="Tools/B01-32/P17/W07DualRecoil/"
NAMES=("rest","left_start","left_peak","left_recover","right_start","both","settle","vfx_off")
H=lambda b:hashlib.sha256(b).hexdigest()
GUID=re.compile(rb"(?m)^guid: ([0-9a-f]{32})\r?$")

def original(path):
    return subprocess.run(["git","show",SHA+":"+SRC+path],
                          check=True,stdout=subprocess.PIPE,
                          stderr=subprocess.PIPE).stdout.replace(b"\r\n",b"\n")

def valid_img(im):
    if im.size!=(512,512) or im.mode!="RGBA":return False
    a=im.getchannel("A"); box=a.getbbox()
    return (box is not None and box[0]>0 and box[1]>0
            and box[2]<512 and box[3]<512
            and a.getpixel((0,0))==0 and a.getpixel((511,511))==0
            and 5000<sum(p>32 for p in a.getdata())<190000
            and all(a.getpixel((x,270))==0 for x in (240,250,260,270)))

def delta(a,b,x0,x1):
    return ImageChops.difference(a.crop((x0,0,x1,512)).convert("RGB"),
                                 b.crop((x0,0,x1,512)).convert("RGB")).getbbox() is not None

def safe_paths(names):
    return (len(names)==17 and len(set(names))==17
            and len({p.casefold() for p in names})==17
            and all((p==BASE+".meta" or p.startswith(BASE+"/"))
                    and "\\" not in p and not p.startswith("/")
                    and all(s not in (".","..","") for s in p.split("/"))
                    for p in names))

def execute(root,out):
    results=[]
    def check(n,ok):results.append({"name":n,"status":"PASS" if ok else "FAIL"})
    frames=json.loads((root/"frames.json").read_text())
    p_receipt=json.loads((root/"PREVIEW_RECEIPT.json").read_text())
    z_receipt=json.loads((root/"UNITY_IMPORT_RECEIPT.json").read_text())
    check("8 immutable CSharp state names",len(frames)==8 and [f["name"] for f in frames]==list(NAMES))
    check("8 preview receipt names",[f["name"] for f in p_receipt["frames"]]==list(NAMES))
    check("preview identified as non-Unity CPU output","NOT Unity screenshot" in p_receipt["origin"])
    images={}
    for i,name in enumerate(NAMES):
        raw=(root/"preview"/(name+".png")).read_bytes()
        check("real PNG SHA "+name,H(raw)==p_receipt["frames"][i]["sha256"])
        check("real PNG header "+name,raw[:8]==b"\x89PNG\r\n\x1a\n")
        with Image.open(io.BytesIO(raw)) as im:
            im.load()
            check("original exact PNG mode/size "+name,im.format=="PNG" and im.mode=="RGBA" and im.size==(512,512))
            images[name]=im.copy()
        check("transparent alpha, bounded silhouette and empty lane divider "+name,valid_img(images[name]))
        state=frames[i]
        check("original CSharp state matches PNG metadata "+name,
              state["left"]==p_receipt["frames"][i]["left"]
              and state["right"]==p_receipt["frames"][i]["right"])
        check("finite CSharp positional and flash state "+name,
              all(math.isfinite(state[k]) for k in ("left","right","flashL","flashR"))
              and all(0<=state[k]<=.280001 for k in ("left","right"))
              and all(0<=state[k]<=1 for k in ("flashL","flashR")))
    a=images
    check("left shot changes only left lane",
          delta(a["rest"],a["left_start"],100,210)
          and not delta(a["rest"],a["left_start"],300,410))
    check("left start/peak distinctly animated",delta(a["left_start"],a["left_peak"],100,210))
    check("right shot changes right lane",delta(a["left_recover"],a["right_start"],300,410))
    check("dual shot animates both lanes",
          delta(a["right_start"],a["both"],100,210)
          and delta(a["right_start"],a["both"],300,410))
    check("VFX OFF exactly clears both animated offsets",a["rest"].tobytes()==a["vfx_off"].tobytes())
    check("left-only original model independent",frames[1]["left"]>0 and frames[1]["right"]==0)
    check("OFF original model reset",frames[-1]["left"]==0 and frames[-1]["right"]==0)
    sheet_data=(root/"DUAL_RECOIL_CONTACT_SHEET.png").read_bytes()
    check("real original contact SHA",H(sheet_data)==p_receipt["sheet_sha256"])
    with Image.open(io.BytesIO(sheet_data)) as sheet_im:
        check("real contact size and RGB mode",sheet_im.size==(2048,1024) and sheet_im.mode=="RGB")
        sheet=sheet_im.copy()
    for i,name in enumerate(NAMES):
        tile=Image.new("RGB",(512,512),(23,30,40))
        tile.paste(a[name],(0,0),a[name])
        x,y=(i%4)*512,(i//4)*512
        check("actual contact tile equals original PNG "+name,
              sheet.crop((x,y,x+512,y+512)).tobytes()==tile.tobytes())
    zip_data=(root/"B0132-DualRecoil-UnityImport.zip").read_bytes()
    check("original immutable producer Unity ZIP SHA",H(zip_data)==ZIP_HASH)
    check("real ZIP bytes match original source receipt",
          H(zip_data)==z_receipt["zip_sha256"] and len(zip_data)==z_receipt["zip_bytes"])
    with zipfile.ZipFile(io.BytesIO(zip_data)) as archive:
        names=archive.namelist()
        check("17 import-safe case distinct Unity files",safe_paths(names))
        check("original physical ZIP CRC",archive.testzip() is None)
        files={p:archive.read(p) for p in names}
    declared=z_receipt["file_hashes"]
    check("17 declared paths match real ZIP",len(declared)==17
          and set(i["path"] for i in declared)==set(names))
    for i in declared:
        p=i["path"];data=files[p]
        check("actual original Unity member hash "+p,
              H(data)==i["sha256"] and len(data)==i["length"])
    metas={p:GUID.search(d).group(1).decode()
           for p,d in files.items() if p.endswith(".meta") and GUID.search(d)}
    check("11 unique actual .meta GUIDs",len(metas)==11 and len(set(metas.values()))==11
          and len([p for p in files if p.endswith(".meta")])==11)
    for p,g in metas.items():
        expected=H(("B01-32-DUAL-RECOIL-ANIM-001:1.0.0:"+p[:-5]).encode())[:32]
        check("original deterministic GUID "+p,g==expected)
    for source,target in [
        ("Runtime/DualRecoilCore.cs",BASE+"/Runtime/DualRecoilCore.cs"),
        ("Runtime/B0132DualRecoilVisual.cs",BASE+"/Runtime/B0132DualRecoilVisual.cs"),
        ("Tests/DualRecoilPlayModeTests.cs",BASE+"/Tests/PlayMode/DualRecoilPlayModeTests.cs")
    ]:check("original immutable CSharp Git blob vs ZIP "+source,files[target]==original(source))
    main=BASE+"/Runtime/B0132DualRecoilVisual.cs"
    prefab=files[BASE+"/Resources/B0132_DualRecoil.prefab"]
    check("Unity serialized prefab references exact MonoImporter script",
          (b"guid: "+metas[main+".meta"].encode()) in prefab
          and b"m_Script: {fileID: 11500000" in prefab)
    asm=json.loads(files[BASE+"/Tests/PlayMode/B0132.DualRecoil.PlayModeTests.asmdef"])
    check("original PlayMode references isolated runtime",
          "B0132.DualRecoil.Runtime" in asm.get("references",[])
          and "TestAssemblies" in asm.get("optionalUnityReferences",[]))
    check("exact 2 original authored Unity PlayMode tests",
          files[BASE+"/Tests/PlayMode/DualRecoilPlayModeTests.cs"].count(b"[UnityTest]")==2)
    # Deliberate independent negative discriminators; never mutate real artifact.
    check("NEGATIVE opaque image rejected",
          not valid_img(Image.new("RGBA",(512,512),(2,3,4,255))))
    check("NEGATIVE broken ZIP member traversal rejected",
          not safe_paths(["../bad"]*17))
    check("NEGATIVE case insensitive duplicate path rejected",
          not safe_paths([BASE+"/A",BASE+"/a"]*8+[BASE+".meta"]))
    check("NEGATIVE modified binary SHA rejected",H(zip_data+b"x")!=ZIP_HASH)
    check("NEGATIVE mutated PNG SHA rejected",
          H((root/"preview/rest.png").read_bytes()+b"x")!=p_receipt["frames"][0]["sha256"])
    failed=[r["name"] for r in results if r["status"]=="FAIL"]
    result={"package_id":"B01-31-DUALRECOIL-BINARY-VISUAL-QA-001",
            "target_producer":"B01-32 #446","target_sha":SHA,
            "source_actions_run":41038102796,"source_artifact":12968263808,
            "original_zip_sha256":H(zip_data),
            "status":"FAIL" if failed else "PASS",
            "pass":len(results)-len(failed),"fail":len(failed),
            "checks":results,"native_unity":"NOT_RUN",
            "formal_B01_40_60_acceptance":"PENDING","integrated":False}
    out.mkdir(parents=True,exist_ok=True)
    (out/"receiver.json").write_text(json.dumps(result,indent=2)+"\n")
    for f in failed:print("FAILED_CHECK: "+f,file=sys.stderr)
    print(json.dumps({"status":result["status"],"pass":result["pass"],
                      "fail":result["fail"],"original_binary_tested":True}))
    return 1 if failed else 0

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--original",required=True);p.add_argument("--output",required=True)
    args=p.parse_args()
    sys.exit(execute(Path(args.original),Path(args.output)))
