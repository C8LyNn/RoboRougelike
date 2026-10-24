"""Package a complete retail player and verify a fresh extraction, without overwrites."""
import argparse
import hashlib
import json
import re
from pathlib import Path, PurePosixPath
import zipfile


def sha256(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def package(build, archive, extracted, source_sha, release_id='W5_RC1'):
    build, archive, extracted = build.resolve(), archive.resolve(), extracted.resolve()
    if archive.exists() or extracted.exists():
        raise ValueError('Archive/extraction destination must be new; preserve previous candidates.')
    if len(source_sha) != 40 or any(c not in '0123456789abcdef' for c in source_sha):
        raise ValueError('Provide the exact tested source commit SHA.')
    if not re.fullmatch(r'[A-Z0-9]+(?:_[A-Z0-9]+)*', release_id):
        raise ValueError('Release id must contain uppercase letters, numbers and underscores only.')
    required = ['RoboRougelike.exe', 'UnityPlayer.dll', 'UnityCrashHandler64.exe',
                'RoboRougelike_Data/globalgamemanagers',
                'RoboRougelike_Data/Managed/Assembly-CSharp.dll',
                'MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll']
    for name in required:
        if not (build / name).is_file() or (build / name).stat().st_size == 0:
            raise ValueError(f'Missing retail runtime dependency: {name}')
    files = {}
    for path in sorted(build.rglob('*')):
        if path.is_symlink():
            raise ValueError('Symlinks are not supported in release builds.')
        relative = path.relative_to(build)
        if not path.is_file() or path.suffix.lower() == '.pdb' or any('DoNotShip' in p or 'BackUpThisFolder' in p for p in relative.parts):
            continue
        files[relative.as_posix()] = path
    product = 'RoboRoguelike '+release_id.replace('_', ' ')
    report = ('Docs/OBJ001_INTEGRATION_QA.md' if release_id == 'OBJ001_RC1'
              else 'Docs/Physics/B01-CODEX-PHY-R1-REPORT.md' if release_id == 'B01_PHY_R1'
              else 'Docs/RRL_W5_RELEASE_QA.md' if release_id == 'W5_RC1'
              else 'Docs/RRL_PACKAGE_INTEGRATION_STATUS.md')
    manifest = {'product': product, 'platform': 'Windows x64',
                'unity': '6000.3.14f1', 'testedSource': source_sha,
                'files': {name: {'bytes': p.stat().st_size, 'sha256': sha256(p)} for name, p in files.items()}}
    readme = (product+' — Windows x64\n\n'
              'Wypakuj cały folder i uruchom RoboRougelike.exe.\n'
              'Zachowaj folder RoboRougelike_Data, MonoBleedingEdge i biblioteki DLL obok EXE.\n\n'
              'WASD: jazda i skręt; mysz: celowanie; LPM: strzał; Esc: pauza.\n'
              'Garaż: Q/E — obrót Y, R/F — obrót X, Z/X — obrót Z.\n'
              'Po walce wybierz jeden bonus i do trzech części; bonus działa po montażu.\n'
              'Alt+F4 zamyka grę.\n\n'
              'Znane ograniczenie RC: klient Windows UI Automation może wywołać błąd Unity przy zamykaniu.\n'
              f'Szczegółowy raport: {report} w repozytorium.\n')
    if release_id != 'W5_RC1':
        readme += ('\nBroń: T — przeładowanie. LPM — strzał, wiązka, piła lub impuls EMP.\n'
                   'W garażu swobodnym dostępny jest budżet testowy. Poligon oferuje\n'
                   'cel elektroniczny z opcjonalnym ostrzałem rakietowym, początkowo wyłączonym.\n')
    if release_id == 'B01_PHY_R1':
        readme += ('\nB01: napęd kół wymaga rzeczywistego kontaktu z terenem.\n'
                   'Podniesiony FullSupport MOV009 może unieść sztywno zamontowane koła;\n'
                   'obniż offset do 0 m, aby odzyskać kontakt i napęd.\n'
                   'Sprawny Core automatycznie podejmuje ograniczoną próbę powrotu\n'
                   'po utrzymującym się przewróceniu na podłożu; EMP blokuje zdolność.\n')
    if release_id == 'OBJ001_RC1':
        readme += ('\nObrona reaktora: wybierz pojazd, kliknij Obrona reaktora w menu,\n'
                   'przygotuj pojazd w garażu i kliknij Broń reaktora. Reaktor ma 600 HP;\n'
                   'przetrwaj trzy fale (2/2/3 przeciwników). Fale dzielą 3 sekundy.\n'
                   'Po zwycięstwie wybierz bonus i łupy, następnie wróć do garażu.\n'
                   'Obrona jest też dostępna w garażu wyprawy; finał pozostaje Oblężnikiem.\n'
                   'Poligon swobodny nie przekazuje swojego darmowego budżetu do wyprawy.\n'
                   'Esc: pauza i powrót do menu; po porażce Enter rozpoczyna nową próbę.\n')
    generated = {'README.txt': readme.encode('utf-8'),
                 'release-manifest.json': (json.dumps(manifest, ensure_ascii=False, indent=2)+'\n').encode('utf-8')}
    root = f'RoboRoguelike_{release_id}_Windows_x64'
    archive.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(archive, 'x', compression=zipfile.ZIP_DEFLATED, compresslevel=6) as output:
        for name, path in files.items():
            output.write(path, f'{root}/{name}')
        for name, content in generated.items():
            output.writestr(f'{root}/{name}', content)
    with zipfile.ZipFile(archive) as source:
        expected = {f'{root}/{name}' for name in [*files, *generated]}
        if set(source.namelist()) != expected or source.testzip() is not None:
            raise ValueError('ZIP contents/CRC validation failed.')
        for name in source.namelist():
            p = PurePosixPath(name)
            if p.is_absolute() or '..' in p.parts:
                raise ValueError('Unsafe ZIP member path.')
        source.extractall(extracted)
    destination = extracted / root
    for name, value in manifest['files'].items():
        path = destination / name
        if path.stat().st_size != value['bytes'] or sha256(path) != value['sha256']:
            raise ValueError(f'Extracted file differs: {name}')
    for name, content in generated.items():
        if (destination / name).read_bytes() != content:
            raise ValueError(f'Extracted manifest/readme differs: {name}')
    return {'archive': str(archive), 'bytes': archive.stat().st_size,
            'sha256': sha256(archive), 'runtimeFiles': len(files),
            'sourceCommit': source_sha, 'extractedExe': str(destination / 'RoboRougelike.exe'),
            'allExtractedHashesMatch': True}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--build', type=Path, required=True)
    parser.add_argument('--archive', type=Path, required=True)
    parser.add_argument('--extract', type=Path, required=True)
    parser.add_argument('--source-sha', required=True)
    parser.add_argument('--release-id', default='W5_RC1')
    args = parser.parse_args()
    print(json.dumps(package(args.build, args.archive, args.extract, args.source_sha, args.release_id), indent=2))
