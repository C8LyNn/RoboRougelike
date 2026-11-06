WEAPONS_8_PREVIEW — podgląd 8 broni R4 v2 (Issue #938 / PR #939)
=================================================================

JAK OTWORZYĆ
1. Rozpakuj ZIP w dowolnym miejscu.
2. Kliknij dwukrotnie index.html. Modele ładują się same — bez wybierania folderów,
   bez GitHub API. Wszystkie 8 prawdziwych plików GLB jest osadzonych w models.js
   (base64) i parsowanych w pamięci, więc działa to także z file://.

INTERNET
- NIE jest potrzebny. Biblioteka 3D three.js r128 (licencja MIT) jest dołączona w lib/.
  Z internetu pobierane są tylko czcionki Google Fonts — bez sieci strona użyje
  czcionek systemowych.
- Wymagany jest WebGL w przeglądarce. Gdyby nie działał: offline.html — rendery z Blendera
  (hero + kluczowe pozy każdego klipu) i porównanie v1/v2, bez internetu.

GDYBY PRZEGLĄDARKA BLOKOWAŁA PLIKI LOKALNE
- Windows: uruchom start_preview.bat (wymaga Pythona), potem otwórz http://localhost:8940/
- macOS/Linux: ./start_preview.sh   albo   python3 -m http.server 8940
  w tym folderze, potem http://localhost:8940/

CO JEST W ŚRODKU
- index.html        podgląd 3D: lista z miniaturami, obrót/zoom, wybór klipu,
                    prędkość, pauza, pętla, oś klatek, komórka 0,55 m, sockety,
                    szereg wszystkich 8 obok siebie (także jako czarne sylwetki),
                    tablica sylwetek z 4 kamer i wynik IoU.
- offline.html      rendery hero + pozy klipów + porównanie v1/v2 (działa bez internetu).
- models.js         dane: 8 GLB (base64), manifesty klipów, wyniki QA, miniatury.
- glb/              te same 8 GLB jako zwykłe pliki (np. do Blendera / gltf-viewer).
- manifests/        *_animation_manifest.json (24 fps, zakresy klatek, pętle) i *_QA.json.
- renders/, compare/, thumbs/, silhouettes/

STATUS
- Blender QA: BLENDER_PASS dla wszystkich 8 (v2: 7 przebudowanych, Basic bez zmian).  Unity: UNITY_NOT_RUN (nie testowano w Unity).
- Źródła .blend/.fbx i pełne raporty: Tools/ModelFactory/R4Weapons938/ w Draft PR #939.
