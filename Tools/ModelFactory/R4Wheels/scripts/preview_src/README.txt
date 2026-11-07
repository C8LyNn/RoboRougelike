WHEELS_12_PREVIEW — podgląd 12 wariantów kół R4
=================================================================

JAK OTWORZYĆ
1. Rozpakuj ZIP w dowolnym miejscu.
2. Kliknij dwukrotnie index.html. Modele ładują się same — bez wybierania folderów,
   bez GitHub API. Wszystkie 12 prawdziwych plików GLB jest osadzonych w models.js
   (base64) i parsowanych w pamięci, więc działa to także z file://.

INTERNET
- NIE jest potrzebny. Biblioteka 3D three.js r128 (licencja MIT) jest dołączona w lib/.
  Z internetu pobierane są tylko czcionki Google Fonts — bez sieci strona użyje
  czcionek systemowych.
- Wymagany jest WebGL w przeglądarce. Gdyby nie działał: offline.html — nagrania MP4
  wszystkich animacji (z Blendera), plus rendery i kluczowe pozy w folderze renders/.

GDYBY PRZEGLĄDARKA BLOKOWAŁA PLIKI LOKALNE
- Windows: uruchom start_preview.bat (wymaga Pythona), potem otwórz http://localhost:8940/
- macOS/Linux: ./start_preview.sh   albo   python3 -m http.server 8940
  w tym folderze, potem http://localhost:8940/

CO JEST W ŚRODKU
- index.html        podgląd 3D: lista z miniaturami, obrót/zoom, wybór klipu,
                    prędkość, pauza, pętla, oś klatek, komórka 0,55 m, sockety,
                    szereg wszystkich 12 obok siebie (także jako czarne sylwetki),
                    tablica sylwetek z 4 kamer i wynik IoU.
- offline.html      MP4 wszystkich osi czasu (działa bez internetu).
- models.js         dane: 12 GLB (base64), manifesty klipów, wyniki QA, miniatury.
- glb/              te same 12 GLB jako zwykłe pliki (np. do Blendera / gltf-viewer).
- manifests/        *_animation_manifest.json (24 fps, zakresy klatek, pętle) i *_QA.json.
- video/, renders/, thumbs/, silhouettes/

STATUS
- Blender QA: BLENDER_PASS dla wszystkich 12.  Unity: UNITY_NOT_RUN (nie testowano w Unity).
- Źródła .blend/.fbx i pełne raporty: Tools/ModelFactory/R4Wheels/ w Draft PR.
