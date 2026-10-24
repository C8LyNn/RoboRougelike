# M01-008 — Karabin chłodzony cieczą (Liquid-Cooled Rifle)

MODEL-01 / Uzbrojenie wizualne. Pozycja z obowiązującego katalogu:
https://github.com/C8LyNn/RoboRougelike/blob/feature/rrl-model-factory-queue-v1/Docs/ModelFactory/CATALOG_V1.md

Claim: Docs/ModelFactory/claims/M01-008.json na gałęzi kontrolnej.
Atomowy CREATE commit: c3389aca27e5bb90c87313de3720f000e58ffd80.
Czas: 2026-10-24T17:49:32.220Z.

## Sylwetka i hierarchia

Karabin długolufowy z rzeczywiście pustą lufą (otwór i ściana wewnętrzna),
miedzianym hamulcem wylotowym, asymetrycznymi ceramicznymi policzkami,
zewnętrznym radiatorem z 11 żebrami, dwiema wygiętymi ciągłymi rurkami
chłodziwa i dwoma odkrytymi tłokami pompy. Dwustronne tłoki pracują w
przeciwfazie w osobnych, otwartych prowadnicach. Model nie dodaje
mechaniki strzelania, temperatury ani chłodzenia.

Układ współrzędnych w Blenderze: metry; pivot mocowania
M01_008_MOUNT_TOP_Z0 dokładnie (0,0,0), płaszczyzna montażowa Z=0;
lufa skierowana w +Y, góra +Z. W eksporcie FBX Blender używa
axis_forward=-Z, axis_up=Y do Unity 6. GLB jest eksportowany
z domyślną konwersją osi glTF. Nie zakładaj, że osie pozostają identyczne
po imporcie — należy zweryfikować pivot, kierunek strzału i położenie
tłoków w Unity.

Ruch: dwa osobne obiekty-rodzice PUMP_L_SLIDE_Y i PUMP_R_SLIDE_Y.
Wychylenie w osi +Y/-Y: ±0,070 m; przeciwfaza 180°; animacja
klatki 1–61 przy 60 FPS. Tłoki mają otwarte prowadnice; niezależny
test sprawdza 61 pozycji, w tym obie skrajne, dla każdej pary
część ruchoma – nieruchoma (AABB, metoda zachowawcza). Brak kontaktu
jest wymagany. Elementy statyczne (śruby, tuleje, obudowy) mają
zamierzone styki konstrukcyjne.

Materiały: Carbon Graphite, Titanium Satin, Ceramic Ivory,
Copper Pipe, Coolant Cyan (emisja), Amber Service (emisja), Bore Black,
Rubber — 8. Emisja nie jest jedynym źródłem kontrastu.

## Generowanie

    python3 Tools/ModelFactory/M01/M01-008/generate_m01_008.py --self-test
    python3 Tools/ModelFactory/M01/M01-008/generate_m01_008.py --out /tmp/m01-008-static
    python3 Tools/ModelFactory/M01/M01-008/validate_m01_008.py /tmp/m01-008-static

    # Blender 4.x/5.x — rzeczywiste animowane GLB, FBX, biblioteka BLEND:
    blender -b --factory-startup --python-exit-code 1 \
      --python Tools/ModelFactory/M01/M01-008/generate_m01_008.py \
      -- --out /tmp/m01-008-blender
    python3 Tools/ModelFactory/M01/M01-008/validate_m01_008.py /tmp/m01-008-blender

Skrypt bez bpy generuje rzeczywiste statyczne OBJ/MTL i raport JSON,
ale nie animowany plik 3D. Blender tworzy GLB/FBX/BLEND tylko po
faktycznym uruchomieniu. Workflow .github/workflows/ci-model-m01-008.yml
uruchamia oba tryby, niezależnie waliduje wynik i zapisuje pliki
wyłącznie na własnej gałęzi feature/rrl-model-m01-008-liquid-cooled-rifle
oraz jako artefakt M01-008-liquid-cooled-rifle-3D.

## Minimalne bramki QA

- Każda składowa jest zamknięta i ma dodatnią objętość, nie ma
  zdegenerowanych trójkątów ani krawędzi użytych inaczej niż dwa razy.
- 61 próbek obu tłoków bez przecięć z nieruchomą obudową; kontrola
  na poziomie AABB, nie zastępuje ręcznej oceny renderu.
- OBJ/MTL: kompletność indeksów, zgodność materiałów, sumy SHA-256,
  poprawna skala i dokładnie wskazany punkt montażu.
- GLB: poprawna binarna struktura glTF 2, minimum 8 materiałów,
  dwa niezależne animowane węzły tłoków. FBX i BLEND muszą istnieć.
- Unity 6 import, EditMode/PlayMode, ocena wyglądu i test wydajności
  NOT RUN, dopóki nie zostaną faktycznie wykonane.

Skrypt usuwa tylko własną kolekcję i nieużywane własne siatki;
wielokrotne wykonanie nie usuwa cudzych scen. Nie modyfikuje WPN-001..006,
Assets wspólnych, scen, ProjectSettings, develop ani main.
