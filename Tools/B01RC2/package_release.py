"""One complete RC2 archive; refuse overwrite; verify every extracted byte."""
import hashlib,json,pathlib,zipfile
r=pathlib.Path(__file__).resolve().parents[2];build=r/'Builds/B01-UNIFIED-RC2';out=r.parent/'Releases/B01_UNIFIED_RC2_20261026';out.mkdir(parents=True,exist_ok=True)
name='RoboRougelike_B01_UNIFIED_RC2_Windows_x64';archive=out/(name+'.zip');extract=out/(name+'-VerifiedExtract')
assert not archive.exists() and not extract.exists(),'Never overwrite historical release artifacts'
for p in ['RoboRougelike.exe','UnityPlayer.dll','RoboRougelike_Data','MonoBleedingEdge']:assert (build/p).exists(),p
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
readme='''RoboRougelike B01 UNIFIED RC2 / Windows x64
Unity 6000.3.14f1 / one shared game

Wypakuj CALY ZIP do jednego folderu i uruchom RoboRougelike.exe.
Nie przenos samego EXE: folder Data, MonoBleedingEdge i biblioteki DLL musza pozostac obok.

Wybierz pojazd i kliknij Przygotuj pojazd (lub Enter). W garazu zamontuj/napraw czesci,
a potem wybierz trase wyprawy, generowana arene lub obrone reaktora. Poligon jest w tym samym garazu.
Wspolne pojazdy, stan czesci, waluta i nagrody: jeden bonus + od 0 do 3 czesci przeciwnika.
Po zwyciestwie wroc do garazu i wybierz kolejna aktywnosc. Nowa proba resetuje bieg wyprawy.

Sterowanie: WASD jazda/hamowanie; mysz celowanie; LPM ogien; Esc pauza/wznowienie.
Garaz: wskaz rzeczywisty element, wybierz narzedzie i obracaj czesc X/Y/Z przyciskami.
PPM nie ma przypisanej drugiej grupy broni w tej wersji. Zapis trwalej wyprawy nie jest obslugiwany.
Wyjscie: Alt+F4.

Aktywne: dotychczasowy R3/R6/PHY, garaz/1x2x2/XYZ, bron/obrona/sensory/napedy,
6-etapowa wyprawa, 3-falowa obrona reaktora, generowana CombatArena R3.4.1,
MAPGENWORLD1.0.1 oraz kanoniczne rozliczenie nagrod z KEEP3 i stabilnymi ID czesci.
Poza tym RC: nowy BUILD R1.3, RAC, CAMERA R1.2.1, LEGENDARY i ECONREBUILD.

Release Candidate do odbioru. Brak merge main/develop i brak deklaracji G2.
Znany C0000005 przy automatyzacji zamykania nie zostal uznany za naprawiony.
Raport i blokery: Draft PR258 https://github.com/C8LyNn/RoboRougelike/pull/258
Kod gry:46b42f62d591ad4aeb9dbfdeda9997044c6b5e1f
'''
files=sorted(p for p in build.rglob('*') if p.is_file())
with zipfile.ZipFile(archive,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for p in files:z.write(p,p.relative_to(build).as_posix())
 z.writestr('START_HERE.txt',readme)
with zipfile.ZipFile(archive) as z:
 assert z.testzip() is None
 for i in z.infolist():assert (extract/i.filename).resolve().is_relative_to(extract.resolve())
 z.extractall(extract)
rows=[]
for p in files:
 rel=p.relative_to(build).as_posix();digest=sha(p);assert digest==sha(extract/rel),rel
 rows.append({'path':rel,'bytes':p.stat().st_size,'sha256':digest})
receipt={'source':'46b42f62d591ad4aeb9dbfdeda9997044c6b5e1f','zip':str(archive),'zipBytes':archive.stat().st_size,'zipSHA256':sha(archive),'extracted':str(extract),'exe':str(extract/'RoboRougelike.exe'),'files':rows,'status':'CRC_AND_ALL_EXTRACTED_BYTES_PASS; actual launch recorded separately'}
(out/'START_HERE.txt').write_text(readme);(out/(name+'.sha256')).write_text(receipt['zipSHA256']+'  '+archive.name+'\n');(out/(name+'-receipt.json')).write_text(json.dumps(receipt,indent=2)+'\n')
print(json.dumps({k:v for k,v in receipt.items() if k!='files'},indent=2))
