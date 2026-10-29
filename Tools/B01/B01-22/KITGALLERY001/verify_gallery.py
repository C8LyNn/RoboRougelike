import hashlib,json,re,subprocess
from pathlib import Path
from test_gallery import transform
ROOT=Path(__file__).resolve().parents[4]
REF="0d212a706c1e633b773d2e03b7b40c28bb63521b"
P="Research/B01-22/P17/EQUIPMENT-KITS-001/catalog.json"
original=json.loads(subprocess.run(["git","show",REF+":"+P],cwd=ROOT,check=True,capture_output=True,encoding="utf-8",errors="strict",text=True).stdout)
fixture=json.loads((Path(__file__).parent/"original_source_fixture.json").read_text(encoding="utf8"))
asset=ROOT/"Assets/B01KitGallery001/Data/KitGallery.json"
if fixture!=original or json.loads(asset.read_text(encoding="utf8"))!=transform(original):
    raise SystemExit("FAIL: original source and Unity asset differ")
items=["Assets/B01KitGallery001","Assets/B01KitGallery001/Data","Assets/B01KitGallery001/Editor","Assets/B01KitGallery001/Data/KitGallery.json","Assets/B01KitGallery001/Editor/KitGalleryWindow.cs"]
guids=[]
for name in items:
    f=ROOT/(name+".meta")
    match=re.search(r"(?m)^guid: ([0-9a-f]{32})$",f.read_text(encoding="utf8"))
    if not match: raise SystemExit("FAIL: missing stable GUID "+name)
    guids.append(match.group(1))
if len(set(guids))!=5:raise SystemExit("FAIL: duplicate GUID")
print(json.dumps(dict(result="PASS",kits=10,meta_guids=5,producer_sha=REF,unity_asset_sha256=hashlib.sha256(asset.read_bytes()).hexdigest())))
