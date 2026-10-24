# M01-001 — Czterolufowy blaster bębnowy

**Katalog:** [M01-001](https://github.com/C8LyNn/RoboRougelike/blob/feature/rrl-model-factory-queue-v1/Docs/ModelFactory/CATALOG_V1.md)
**Claim:** [M01-001.json](https://github.com/C8LyNn/RoboRougelike/blob/feature/rrl-model-factory-queue-v1/Docs/ModelFactory/claims/M01-001.json)
**Rodzaj:** wyłącznie wizualny asset, bez zmian mechaniki WPN. **Główne mocowanie:** górne gniazdo.

## Pliki

- `generate_m01_001.py` — parametryczny skrypt `bpy` tworzący 61 osobnych części i obrót zespołu czterech luf wokół osi Y; po uruchomieniu w Blenderze eksportuje animowany FBX, GLB oraz .blend.
- `M01-001.obj` + `M01-001.mtl` — **rzeczywisty, wygenerowany** statyczny model podglądowy w metrach; OBJ można zaimportować do Unity bez Blendera. Nie zawiera animacji (format OBJ nie przechowuje animacji obiektowej).
- `README.md` — kontrakt montażu, uruchomienie, QA i ograniczenia.

## Design / integracja

Styl: masywny industrialny blaster; grafitowy korpus, miedziane asymetryczne panele, cztery stalowe lufy, emisyjne bursztynowe pierścienie, jasne ceramiczne detale, zewnętrzna osłona ochronna.

- Blender: +Z = góra, -Y = przód/muzzle, 1 jednostka = 1 m.
- Origin całego zespołu: `(0,0,0)`, środek stopy mocującej do górnego gniazda pojazdu. Nie jest to jeszcze adapter do konkretnego runtime'owego portu Unity.
- Oś rotora: prosta równoległa do Y, przechodząca przez `(0,-0.57,0.78)`; obiekt `RRL_M01_001_ROTOR_Y_AXIS`.
- Cztery lufy są oddalone promieniowo o 0.24 m od osi; maksymalny promień obwiedni wirującej: `0.24 + 0.107 = 0.347 m`. Wewnętrzny promień nieruchomej osłony: `0.455 - 0.042 = 0.413 m`. Nominalny prześwit promieniowy: **0.066 m**.
- Klatki 1–49, 24 FPS, 360° na cykl. Animacja obrotu jest w źródłowym pliku Blender; OBJ jest statycznym ujęciem.
- Rozmiary statycznego OBJ (AABB): x od -0.5525 do +0.5525 m; y od -1.7365 do +0.7325 m; z od 0 do +1.2770 m. Przybliżona wielkość: **1.105 × 2.469 × 1.277 m**.
- Przewidziane 6 materiałów: `Graphite`, `Gunmetal`, `Copper`, `CeramicBlack`, `AmberEmission`, `CeramicSilver`. Plik MTL zawiera emisyjne `Ke`, lecz rzeczywista poświata w Unity wymaga skonfigurowania materiału URP i ewentualnego bloom.

## Blender (animowany FBX)

```bash
blender -b --python generate_m01_001.py -- --output ./export
```

Oczekiwane pliki: `export/M01-001.blend`, `export/M01-001.fbx`, `export/M01-001.glb`.
Zaimportuj **FBX** do `Assets/` w Unity 6, sprawdź importowane materiały i klip animacji, dopiero potem zintegruj model z istniejącą mechaniką. GLB jest formatem pomocniczym i może wymagać dodatkowego importera w Unity. Dla szybkiego statycznego podglądu można skopiować OBJ i MTL do tego samego folderu `Assets/`.

## QA wykonane w tym przebiegu

- **PASS — analiza zapisanego OBJ odczytanego z GitHub:** 61 obiektów, 4076 wierzchołków, 7952 trójkąty, 6 nazw materiałów.
- **PASS — walidacja siatki:** wszystkie 61 obiektów mają domknięte krawędzie (każda niedirected krawędź należy do dokładnie dwóch trójkątów) i dodatnią objętość skierowaną; brak nieprawidłowych indeksów i zerowych pól trójkątów.
- **PASS — analityczny prześwit radialny rotora:** 0.066 m do nieruchomych pierścieni ochronnych; cztery wyloty nie nakładają się (odległość sąsiadów ok. 0.339 m > średnica 0.214 m).
- **NOT RUN — Blender/bpy:** brak Blendera w środowisku wykonawczym; plik .blend/.fbx/.glb nie został wyeksportowany ani przetestowany.
- **NOT RUN — Unity 6:** brak edytora Unity; nie potwierdzono importu, odtwarzania animacji ani działania w grze.
- **NOT RUN — dynamiczna detekcja kolizji całego złożenia:** analityczne prześwity nie zastępują wizualnego testu animacji, szczególnie w obszarze tylnej pokrywy.
- **UWAGA:** plik OBJ jest faktycznym modelem 3D, ale statycznym. Zastosowanie go jako finalnego animowanego assetu wymaga eksportu FBX w Blenderze i integracji w Unity.

## Reguły repo

Wszystkie zmiany ograniczają się do `Tools/ModelFactory/M01/M01-001/`. Brak modyfikacji `Assets/`, `ProjectSettings/`, istniejących WPN, scen, `main` i `develop`. Wydanie wyłącznie przez osobny DRAFT PR.

## Poprawka hierarchii i animacji — 2026-10-24

W pierwszej wersji źródłowej znacznik `MOUNT_TOP_SOCKET` nie był rodzicem statycznych siatek ani osi obrotu bębna. W eksporcie glTF/FBX groziło to utworzeniem wielu obiektów na głównym poziomie zamiast pojedynczego montowalnego prefabrykatu. Poprawiony generator podpina wszystkie stałe siatki i `ROTOR_Y_AXIS` do `MOUNT_TOP_SOCKET` z zachowaniem globalnych transformacji. Dodatkowa asercja sprawdza brak osieroconych części.

Zastąpiono dwie klatki animacji (0°→360°) regularnymi klatkami co 3 ramki (22,5°). Daje to poprawną interpolację po konwersji do kwaternionów w formatach glTF/Unity, bez zredukowania obrotu do pozornej bezczynności. Zakres nadal obejmuje 48 ramek przy 24 FPS.

**Ograniczenie QA:** statyczny przegląd kodu wykonany, natomiast Blender/Unity i właściwy FBX/GLB runtime pozostają NOT RUN. Zmiany są przeznaczone do macierzystego Draft PR #36 po weryfikacji, nie należy scalać ich bezpośrednio do develop/main.
