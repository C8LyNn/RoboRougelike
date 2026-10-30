import hashlib,json,re,subprocess
from pathlib import Path
from test_gallery import transform
ROOT=Path(__file__).resolve().parents[4]
REF="b1d4e341d846c9397e236809a1d7e4c4fff9c935"
P="Research/B01-22/P17/EQUIPMENT-KITS-001/catalog.json"
def verify_manifest():
    manifest = json.loads((Path(__file__).parent/"manifest.json").read_text(encoding="utf8"))
    rows = manifest.get("git_blob_hashes", [])
    if len(rows) != 19:
        raise SystemExit("FAIL: incomplete Git blob manifest")
    names = set()
    for row in rows:
        name = row["path"]
        if (name in names or ".." in Path(name).parts or not
            (name.startswith("Assets/B01KitGallery001") or
             name.startswith("Tools/B01/B01-22/KITGALLERY001/") or
             name == ".github/workflows/b01-22-w03-kitgallery.yml")):
            raise SystemExit("FAIL: unsafe/duplicate manifest path")
        names.add(name)
        actual = subprocess.run(["git", "rev-parse", "HEAD:"+name], cwd=ROOT,
            check=True, capture_output=True, text=True, encoding="ascii").stdout.strip()
        if actual != row["git_blob_sha1"] or not (ROOT/name).is_file():
            raise SystemExit("FAIL: Git original blob mismatch "+name)
    return len(rows)

original=json.loads(subprocess.run(["git","show",REF+":"+P],cwd=ROOT,check=True,capture_output=True,encoding="utf-8",errors="strict",text=True).stdout)
fixture=json.loads((Path(__file__).parent/"original_source_fixture.json").read_text(encoding="utf8"))
asset=ROOT/"Assets/B01KitGallery001/Data/KitGallery.json"
if fixture!=original or json.loads(asset.read_text(encoding="utf8"))!=transform(original):
    raise SystemExit("FAIL: original source and Unity asset differ")
items=["Assets/B01KitGallery001","Assets/B01KitGallery001/Data","Assets/B01KitGallery001/Editor","Assets/B01KitGallery001/Data/KitGallery.json","Assets/B01KitGallery001/Editor/KitGalleryWindow.cs","Assets/B01KitGallery001/Editor/KitComparisonRules.cs"]
guids=[]
for name in items:
    f=ROOT/(name+".meta")
    match=re.search(r"(?m)^guid: ([0-9a-f]{32})$",f.read_text(encoding="utf8"))
    if not match: raise SystemExit("FAIL: missing stable GUID "+name)
    guids.append(match.group(1))
if len(set(guids))!=6:raise SystemExit("FAIL: duplicate GUID")
files_verified=verify_manifest()
print(json.dumps(dict(result="PASS",kits=10,meta_guids=6,git_blobs=files_verified,producer_sha=REF,unity_asset_sha256=hashlib.sha256(asset.read_bytes()).hexdigest())))
