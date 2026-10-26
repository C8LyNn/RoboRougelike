import gzip,json,pathlib,shutil,xml.etree.ElementTree as ET
r=pathlib.Path(__file__).resolve().parents[2];out=r/'Docs/B01_RC2/Native';out.mkdir(parents=True,exist_ok=True);summary=[]
for folder in sorted((r/'Artifacts/Local').glob('RC2-*')):
 if not folder.is_dir():continue
 dest=out/folder.name;dest.mkdir(exist_ok=True)
 for p in folder.iterdir():
  if p.suffix not in ('.json','.xml','.log'):continue
  b=p.read_bytes()
  if p.suffix=='.log':
   lines=b.decode('utf-8-sig',errors='replace').splitlines();lines=['[connection identifiers omitted]' if s.startswith('Player connection [') else 'Machine Id: [omitted]' if s.lstrip().startswith('Machine Id:') else s for s in lines];b=('\n'.join(s.rstrip() for s in lines)+'\n').encode();(dest/(p.name+'.gz')).write_bytes(gzip.compress(b,mtime=0))
  else:(dest/p.name).write_bytes(b)
 for p in dest.glob('*-process.json'):
  receipt=json.loads(p.read_text(encoding='utf-8-sig'));xml=dest/(receipt['phase']+'.xml')
  if xml.exists():
   root=ET.parse(xml).getroot();receipt['nunit']={k:int(root.get(k)) for k in ('total','passed','failed','skipped')};receipt['cases']=[{'name':c.get('fullname'),'result':c.get('result'),'failure':c.findtext('failure/message')} for c in root.findall('.//test-case')]
  receipt['evidence']=str(dest.relative_to(r));summary.append(receipt)
(out/'native-summary.json').write_text(json.dumps(summary,indent=2)+'\n');print(json.dumps([{k:v for k,v in s.items() if k in ('stage','phase','exitCode','nunit','source')} for s in summary]))
