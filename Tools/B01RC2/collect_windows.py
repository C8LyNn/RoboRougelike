"""Archive actual Player receipts and images; distinguish API/Input/OS evidence."""
import gzip, hashlib, json, pathlib, re, shutil
from PIL import Image, ImageStat

r = pathlib.Path(__file__).resolve().parents[2]
out = r / 'Docs/B01_RC2/Windows'
out.mkdir(parents=True, exist_ok=True)
summary = []
for folder in sorted((r / 'Artifacts/Local').glob('RC2-Windows-*')):
    launch_file = folder / 'launch.json'
    if not launch_file.exists():
        continue
    dest = out / folder.name
    dest.mkdir(exist_ok=True)
    launch = json.loads(launch_file.read_text(encoding='utf-8-sig'))
    row = dict(launch)
    process = folder / 'process.json'
    row['process'] = json.loads(process.read_text(encoding='utf-8-sig')) if process.exists() else None
    row['results'] = []
    row['screenshots'] = []
    row['logErrorLines'] = []
    for p in sorted(folder.iterdir()):
        if p.suffix == '.json':
            shutil.copy2(p, dest / p.name)
            if p.name == 'human-confirmation.json':
                manual = json.loads(p.read_text(encoding='utf-8-sig'))
                row['manualAcceptance'] = {'status': manual['status'], 'file': p.name,
                    'checks': manual['checks'], 'nativeExitCode': manual['nativeExitCode']}
            if p.name.endswith('-result.json'):
                result = json.loads(p.read_text(encoding='utf-8-sig'))
                checks = result.get('checks', [])
                row['results'].append({
                    'file': p.name, 'status': result.get('status'),
                    'method': result.get('method'), 'errors': result.get('errors'),
                    'observations': len(checks),
                    'distinctChecks': len({c['name'] for c in checks}),
                    'failedChecks': [c for c in checks if not c['passed']],
                    'telemetry': {k: v for k, v in result.items() if k not in ('checks', 'errors', 'method')}
                })
        elif p.suffix == '.log':
            lines = p.read_text(encoding='utf-8-sig', errors='replace').splitlines()
            lines = ['[connection identifiers omitted]' if s.startswith('Player connection [')
                     else 'Machine Id: [omitted]' if s.lstrip().startswith('Machine Id:') else s for s in lines]
            row['logErrorLines'].extend(s for s in lines if re.search(r'Exception:|NullReference|MissingReference|Error:|C0000005|Access Violation', s, re.I))
            (dest / (p.name + '.gz')).write_bytes(gzip.compress(('\n'.join(lines) + '\n').encode(), mtime=0))
        elif p.suffix == '.png':
            shutil.copy2(p, dest / p.name)
            with Image.open(p) as im:
                rgb = im.convert('RGB')
                means = ImageStat.Stat(rgb).mean
                row['screenshots'].append({'file': p.name, 'width': im.width, 'height': im.height,
                    'meanRGB': means, 'nonBlack': max(means) > 2,
                    'sha256': hashlib.sha256(p.read_bytes()).hexdigest()})
        elif p.suffix == '.csv' and p.stat().st_size:
            shutil.copy2(p, dest / p.name)
    row['evidence'] = str(dest.relative_to(r)).replace('\\', '/')
    summary.append(row)
(out / 'windows-summary.json').write_text(json.dumps(summary, indent=2) + '\n', encoding='utf-8', newline='\n')
print(json.dumps([{k:v for k,v in s.items() if k in ('mode','width','height','process','results','logErrorLines','evidence')} for s in summary], indent=2))
