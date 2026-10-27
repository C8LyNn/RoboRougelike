import hashlib
import json
import pathlib
import subprocess
import zipfile

root = pathlib.Path(__file__).resolve().parents[2]
build = root / 'Builds/FULL-MERGE270'
release = root.parent / 'Releases/ROBOROUGELIKE_EXIT273_FINAL_20261027'
release.mkdir(exist_ok=True)
archive = release / 'RoboRougelike_EXIT273_Windows_x64.zip'
extract = release / 'RoboRougelike_EXIT273_Windows_x64-VerifiedExtract'
if archive.exists() or extract.exists():
    raise SystemExit('Existing release retained; choose a new version before packaging')
source = subprocess.check_output(['git', '-C', str(root), 'rev-parse', 'HEAD'], text=True).strip()
instructions = '''ROBOROUGELIKE — EXIT273
Wypakuj cały ZIP i uruchom RoboRougelike.exe.
Menu główne: Przygotuj pojazd -> garaż -> wybierz aktywność.
Esc otwiera pauzę również w garażu i przy wyborze aktywności.
Pauza: Wznów / Powrót do menu głównego / Wyjdź z gry.
Menu główne: widoczny przycisk Wyjdź z gry.
Pauza klawiaturą: strzałki góra/dół i Enter; Esc wznawia.
Pauza padem: Start, krzyżak, A zatwierdza, B wznawia.
Alt+F4 i X okna korzystają ze zwykłego zamykania Unity.
Powrót do menu kończy bieżącą wyprawę (brak zapisu trwałego).
WASD jazda, LPM ogień, PPM celowanie; w garażu PPM obraca widok.
Zachowano mechaniki pełnej integracji PR #272; modele prototypowe wyłączone.
'''
(release / 'START_HERE.txt').write_text(instructions, encoding='utf-8')
files = sorted(p for p in build.rglob('*') if p.is_file() and not any('DoNotShip' in s for s in p.parts))
required = ['RoboRougelike.exe', 'UnityPlayer.dll', 'RoboRougelike_Data/globalgamemanagers', 'MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll']
for name in required:
    if not (build / name).is_file():
        raise SystemExit('Missing runtime file: ' + name)
with zipfile.ZipFile(archive, 'x', compression=zipfile.ZIP_DEFLATED, compresslevel=6) as z:
    for path in files:
        z.write(path, path.relative_to(build).as_posix())
    z.write(release / 'START_HERE.txt', 'START_HERE.txt')
with zipfile.ZipFile(archive) as z:
    if z.testzip() is not None:
        raise SystemExit('ZIP CRC failed')
    z.extractall(extract)
def sha(path):
    return hashlib.file_digest(path.open('rb'), 'sha256').hexdigest()
manifest = []
for path in files:
    relative = path.relative_to(build)
    digest = sha(path)
    if sha(extract / relative) != digest:
        raise SystemExit('Extract mismatch: ' + str(relative))
    manifest.append(dict(path=relative.as_posix(), size=path.stat().st_size, sha256=digest))
receipt = dict(source=source, archive=str(archive), extract=str(extract), bytes=archive.stat().st_size, sha256=sha(archive), crc='PASS', extractedFiles='PASS', files=manifest)
(release / 'release-receipt.json').write_text(json.dumps(receipt, indent=2), encoding='utf-8')
(release / 'SHA256SUMS.txt').write_text(receipt['sha256'] + '  ' + archive.name + '\n', encoding='ascii')
print(json.dumps({k:v for k,v in receipt.items() if k != 'files'}, indent=2))
