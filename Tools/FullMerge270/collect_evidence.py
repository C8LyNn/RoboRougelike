"""Archive actual native/Player evidence, retaining failures and individual case results."""
import gzip,hashlib,json,pathlib,re,shutil,xml.etree.ElementTree as ET
from PIL import Image,ImageStat
r=pathlib.Path(__file__).resolve().parents[2];out=r/'Docs/FullMerge270/Evidence';out.mkdir(parents=True,exist_ok=True)
native=[];windows=[]
for folder in sorted((r/'Artifacts/Local').glob('FM270-*')):
 if not folder.is_dir():continue
 dest=out/folder.name;dest.mkdir(exist_ok=True);screens=[];raw=[]
 for p in folder.iterdir():
  if not p.is_file():continue
  if p.suffix in ('.log','.csv'):
   b=p.read_bytes();raw.append({'file':p.name,'rawSHA256':hashlib.sha256(b).hexdigest(),'bytes':len(b)})
   if p.suffix=='.log':
    lines=b.decode('utf-8-sig',errors='replace').splitlines()
    lines=['[machine/licensing/connection identifier omitted]' if re.search(r'(Machine Id:|Player connection \[|Serial number:|User ID:|Access.?Token|licensee|licens.*serial|user.*email)',x,re.I) else re.sub(r'("serial"\s*:\s*)"[^"]*"',r'\1"[omitted]"',x.rstrip(),flags=re.I) for x in lines]
    b=('\n'.join(lines)+'\n').encode()
   (dest/(p.name+'.gz')).write_bytes(gzip.compress(b,mtime=0))
  elif p.suffix in ('.json','.xml'):shutil.copy2(p,dest/p.name)
  elif p.suffix=='.png':
   shutil.copy2(p,dest/p.name)
   with Image.open(p) as im:
    means=ImageStat.Stat(im.convert('RGB')).mean;screens.append({'file':p.name,'width':im.width,'height':im.height,'meanRGB':means,'nonBlack':max(means)>2,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
 (dest/'raw-file-hashes.json').write_text(json.dumps(raw,indent=2)+'\n',encoding='utf-8',newline='\n')
 if 'Windows' in folder.name:
  launch=folder/'launch.json'
  if not launch.exists():continue
  row=json.loads(launch.read_text(encoding='utf-8-sig'));row['screenshots']=screens
  row['process']=json.loads((folder/'process.json').read_text(encoding='utf-8-sig')) if (folder/'process.json').exists() else None
  row['results']=[json.loads(p.read_text(encoding='utf-8-sig')) for p in folder.glob('*result.json')]
  row['evidence']=dest.relative_to(r).as_posix();windows.append(row)
 else:
  for p in folder.glob('*-process.json'):
   row=json.loads(p.read_text(encoding='utf-8-sig'));xml=folder/(row['phase']+'.xml')
   if xml.exists():
    n=ET.parse(xml).getroot();row['nunit']={k:int(n.get(k)) for k in ('total','passed','failed','skipped')}
    row['cases']=[{'name':c.get('fullname'),'result':c.get('result'),'failure':c.findtext('failure/message')} for c in n.findall('.//test-case')]
   row['evidence']=dest.relative_to(r).as_posix();native.append(row)
for name,value in [('native-summary.json',native),('windows-summary.json',windows)]:
 (out/name).write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'native':[{'stage':x['stage'],'phase':x['phase'],'exitCode':x['exitCode'],'nunit':x.get('nunit')} for x in native],
 'windows':[{'mode':x['mode'],'vehicle':x.get('vehicle'),'width':x['width'],'exit':x['process']['exitCode'] if x['process'] else None,'status':[q.get('status') for q in x['results']]} for x in windows]},indent=2))
