BLOCKS_PREVIEW — podgląd klocków R4 (30 klocków + pojazd testowy)
=================================================================

JAK OTWORZYĆ
1. Rozpakuj ZIP w dowolnym miejscu.
2. Kliknij dwukrotnie index.html. Modele ładują się same — bez wybierania folderów,
   bez GitHub API. Wszystkie prawdziwe pliki GLB klocków i pojazdu testowego jest osadzonych w models.js
   (base64) i parsowanych w pamięci, więc działa to także z file://.

INTERNET
- NIE jest potrzebny. Biblioteka 3D three.js r128 (licencja MIT) jest dołączona w lib/.
  Z internetu pobierane są tylko czcionki Google Fonts — bez sieci strona użyje
  czcionek systemowych.
- Wymagany jest WebGL w przeglądarce. Gdyby nie działał: offline.html — rendery z Blendera, bez internetu.

GDYBY PRZEGLĄDARKA BLOKOWAŁA PLIKI LOKALNE
- Windows: uruchom start_preview.bat (wymaga Pythona), potem otwórz http://localhost:8940/
- macOS/Linux: ./start_preview.sh   albo   python3 -m http.server 8940
  w tym folderze, potem http://localhost:8940/

CO JEST W ŚRODKU
- index.html        podgląd 3D: lista z miniaturami, obrót/zoom, wybór klipu,
                    prędkość, pauza, pętla, oś klatek, komórka 0,55 m, sockety,
                    szereg wszystkich klocków obok siebie (także jako czarne sylwetki).
- offline.html      rendery klocków i pojazdu testowego (działa bez internetu).
- models.js         dane: GLB klocków i pojazdu (base64), porty, wyniki QA, miniatury.
- glb/              te same GLB jako zwykłe pliki (np. do Blendera / gltf-viewer).
- manifests/        *_QA.json (komórki, porty, ContactMask, testy).
- renders/, compare/, thumbs/

STATUS
- Blender QA: BLENDER_PASS 30/30; test zgodności z bronią i kołami: PASS.  Unity: UNITY_NOT_RUN (nie testowano w Unity).
- Źródła .blend/.fbx i pełne raporty: Tools/ModelFactory/R4Blocks/ w Draft PR.
