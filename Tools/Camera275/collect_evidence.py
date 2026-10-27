"""Keep actual native/Player failures and independent results, scrub log identifiers."""
import gzip,hashlib,json,pathlib,re,shutil,xml.etree.ElementTree as ET
from PIL import Image,ImageStat
r=pathlib.Path(__file__).resolve().parents[2];out=r/'Docs/Camera275/Evidence';out.mkdir(parents=True,exist_ok=True)
native=[];windows=[]
folders=sorted(p for p in (r/'Artifacts/Local').iterdir() if p.is_dir() and ('CAM275' in p.name or p.name.startswith('FM270-Windows-')))
for folder in folders:
    # Do not snapshot actively written logs or manufacture a completion receipt.
    phases=list(folder.glob('*-process.json'))
    if 'Windows' in folder.name or folder.name.startswith(('CAM275-Aim-','CAM275-Manual-')):
        if not (folder/'process.json').is_file():continue
    elif not phases:continue
    dest=out/folder.name;dest.mkdir(exist_ok=True);raw=[];screens=[]
    for p in folder.iterdir():
        if not p.is_file():continue
        if p.suffix in ('.log','.csv'):
            data=p.read_bytes();raw.append(dict(file=p.name,rawSHA256=hashlib.sha256(data).hexdigest(),bytes=len(data)))
            if p.suffix=='.log':
                lines=data.decode('utf-8-sig',errors='replace').splitlines()
                lines=['[machine/licensing/connection identifier omitted]' if re.search(r'(Machine Id:|Player connection \[|Serial number:|User ID:|Access.?Token|licensee|licens.*serial|user.*email)',line,re.I) else re.sub(r'("serial"\s*:\s*)"[^"]*"',r'\1"[omitted]"',line.rstrip(),flags=re.I) for line in lines]
                data=('\n'.join(lines)+'\n').encode()
            (dest/(p.name+'.gz')).write_bytes(gzip.compress(data,mtime=0))
        elif p.suffix in ('.json','.xml'):shutil.copy2(p,dest/p.name)
        elif p.suffix=='.png':
            shutil.copy2(p,dest/p.name)
            with Image.open(p) as im:
                means=ImageStat.Stat(im.convert('RGB')).mean
                screens.append(dict(file=p.name,width=im.width,height=im.height,meanRGB=means,nonBlack=max(means)>2,sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
    (dest/'raw-file-hashes.json').write_text(json.dumps(raw,indent=2)+'\n',encoding='utf-8')
    if (folder/'launch.json').exists():
        row=json.loads((folder/'launch.json').read_text(encoding='utf-8-sig'));row['screenshots']=screens
        row['process']=json.loads((folder/'process.json').read_text(encoding='utf-8-sig'))
        row['results']=[json.loads(p.read_text(encoding='utf-8-sig')) for p in folder.glob('*result.json')]
        row['evidence']=dest.relative_to(r).as_posix();windows.append(row)
    else:
        for p in phases:
            row=json.loads(p.read_text(encoding='utf-8-sig'));xml=folder/(row['phase']+'.xml')
            if xml.exists():
                n=ET.parse(xml).getroot();row['nunit']={key:int(n.get(key)) for key in ('total','passed','failed','skipped')}
                row['cases']=[dict(name=c.get('fullname'),result=c.get('result'),failure=c.findtext('failure/message')) for c in n.findall('.//test-case')]
            row['evidence']=dest.relative_to(r).as_posix();native.append(row)
for name,rows in [('native-summary.json',native),('windows-summary.json',windows)]:
    (out/name).write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(nativePhases=len(native),windowsProcesses=len(windows),evidence=str(out)),indent=2))
