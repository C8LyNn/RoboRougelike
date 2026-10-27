"""Preserve native results and logs, omitting machine/licensing identifiers."""
import gzip,hashlib,json,pathlib,re,shutil,xml.etree.ElementTree as ET
r=pathlib.Path(__file__).resolve().parents[2];out=r/'Docs/WeaponReticles/Evidence';out.mkdir(parents=True,exist_ok=True)
summary=[]
for folder in sorted((r/'Artifacts/Local').glob('FM270-RETICLE-*')):
    if not folder.is_dir():continue
    dest=out/folder.name;dest.mkdir(exist_ok=True);raw=[]
    for p in folder.iterdir():
        if not p.is_file():continue
        if p.suffix=='.log':
            b=p.read_bytes();raw.append(dict(file=p.name,rawSHA256=hashlib.sha256(b).hexdigest(),bytes=len(b)))
            lines=b.decode('utf-8-sig',errors='replace').splitlines()
            lines=['[machine/licensing/connection identifier omitted]' if re.search(r'(Machine Id:|Player connection \[|Serial number:|User ID:|Access.?Token|licensee|licens.*serial|user.*email)',line,re.I) else line.rstrip() for line in lines]
            (dest/(p.name+'.gz')).write_bytes(gzip.compress(('\n'.join(lines)+'\n').encode(),mtime=0))
        elif p.suffix in ('.json','.xml'):shutil.copy2(p,dest/p.name)
    (dest/'raw-file-hashes.json').write_text(json.dumps(raw,indent=2)+'\n',encoding='utf-8')
    for p in folder.glob('*-process.json'):
        row=json.loads(p.read_text(encoding='utf-8-sig'));xml=folder/(row['phase']+'.xml')
        if xml.exists():
            root=ET.parse(xml).getroot();row['nunit']={k:int(root.get(k)) for k in ('total','passed','failed','skipped')}
            row['failures']=[dict(name=c.get('fullname'),message=c.findtext('failure/message')) for c in root.findall('.//test-case') if c.get('result')=='Failed']
        row['evidence']=dest.relative_to(r).as_posix();summary.append(row)
(out/'summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(nativePhases=len(summary),evidence=str(out)),indent=2))
