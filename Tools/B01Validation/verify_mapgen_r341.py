"""Read-only exact-source/manifest/GUID validation, not native QA acceptance."""
import hashlib,importlib.util,json,re,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[2]
pin='5be31039411f89f17654b1c332f8692883fa03cc'
spec=importlib.util.spec_from_file_location('producer_mapgen_gate',root/'Tools/B01/MAPGEN001/package_gate.py')
gate=importlib.util.module_from_spec(spec);spec.loader.exec_module(gate)
manifest=json.loads((root/gate.MANIFEST).read_text(encoding='utf-8'))
full=gate.snap();source_count,qa_count=gate.verify(manifest,full)
paths=subprocess.check_output(['git','ls-tree','-r','--name-only',pin,'--','Assets/B01Packages.meta','Assets/B01Packages/MAPGEN001.meta','Assets/B01Packages/MAPGEN001','Docs/B01/MAPGEN001','Tools/B01/MAPGEN001'],cwd=root).decode().splitlines()
records=[]
for path in paths:
 data=subprocess.check_output(['git','show',pin+':'+path],cwd=root)
 assert (root/path).read_bytes()==data,path
 records.append({'path':path,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest(),'matchesPinnedSource':True,'inProducerFileMap':path in full})
guids={};collisions=[]
for path in (root/'Assets').rglob('*.meta'):
 match=re.search(r'(?m)^guid: ([0-9a-f]{32})\s*$',path.read_text(encoding='utf-8-sig'))
 if not match:continue
 name=path.relative_to(root).as_posix()
 if match[1] in guids:collisions.append([guids[match[1]],name,match[1]])
 guids[match[1]]=name
out=root/'Docs/MAPGEN001R341NativeQA';out.mkdir(exist_ok=True)
report={'source':pin,'target':'4c9236a4cdc27814ea2e7679872ec4a524150cc8','manifestSourceCount':source_count,'manifestQaToolsCount':qa_count,'completeImportedFiles':records,'outsideManifestFileMap':[r['path'] for r in records if not r['inProducerFileMap']],'guidCount':len(guids),'guidCollisions':collisions,'sourceIntegrityOnly':True,'independentG1G2Approval':False}
(out/'source-integrity.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'sourceFiles':source_count,'qaTools':qa_count,'entireSourceFilesMatched':len(records),'outsideManifestFileMap':report['outsideManifestFileMap'],'guidCollisions':collisions}))
