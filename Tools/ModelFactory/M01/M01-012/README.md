# M01-012 — Pneumatyczna wyrzutnia śrub

Katalog V1: **M01-012**; kolejka **M01**; model **Pneumatyczna wyrzutnia śrub**; mocowanie **górne gniazdo**; ruch **tłok pneumatyczny**; wyróżnik **odsłonięty zbiornik ciśnieniowy**.

## Sylwetka i materiały

Asymetryczna konstrukcja: długa lufa z prawdziwym otworem, lewostronny zewnętrzny zbiornik ciśnieniowy z czterema miedzianymi opaskami i manometrem, prawostronny cylindryczny siłownik pneumatyczny z wysuwanym chromowanym tłokiem. Segmenty ceramicznej osłony lufy, frezowane nity, wsporniki i akcenty turkusowe/bursztynowe. 7 materiałów: Graphite, Brushed_Titanium, Ivory_Ceramic, Copper, Black_Rubber, Pressure_Cyan, Warning_Amber.

## Układ współrzędnych / Unity 6

**1 jednostka = 1 m.** +Z = kierunek strzału, +Y = góra, +X = prawa strona. Pivot i nadrzędny obiekt `MOUNT_TOP_SOCKET` znajdują się dokładnie w (0,0,0), na płaszczyźnie górnego gniazda pojazdu. W Unity należy zweryfikować osie po imporcie FBX/GLB. Nie ma mechaniki strzelania ani pocisków.

Animowany obiekt nadrzędny `PNEUMATIC_RAM` przesuwa chromowany tłok, głowicę i wskaźnik o **90 mm w lokalnej osi −Z** (klatki 1→12), utrzymuje pozycję do klatki 23 i wraca do klatki 40. Animacja ma 61 klatek przy 60 FPS. Tłok ma promień 20 mm i wchodzi w **rzeczywisty pusty kanał** o promieniu wewnętrznym 32 mm; nominalny radialny prześwit to 12 mm. Obudowa cylindra nie jest pełną bryłą zasłaniającą tłok.

## Generowanie

```bash
blender -b --factory-startup --python-exit-code 1 \
  --python Tools/ModelFactory/M01/M01-012/generate_m01_012.py \
  -- --out /tmp/m01-012
python3 Tools/ModelFactory/M01/M01-012/validate_m01_012.py /tmp/m01-012
```

Źródło: `generate_m01_012.py`. Niezależny walidator eksportów: `validate_m01_012.py`. Własny workflow: `.github/workflows/ci-model-m01-012.yml`. Po **faktycznym pomyślnym wykonaniu** CI umieszcza BLEND, FBX, GLB, OBJ, MTL i QA JSON w `generated/` wyłącznie na gałęzi M01-012 oraz publikuje artefakt GitHub Actions. Samo istnienie skryptu nie oznacza wygenerowanego modelu.

## QA / ograniczenia

Generator kontroluje zamknięte siatki, dodatnie objętości, trójkąty o dodatnim polu, materiały i 61 klatek ruchu. W każdej klatce sprawdza przecięcia AABB ruchomych części z nieruchomymi. Dla tłoka i wydrążonego cylindra dopuszcza konserwatywne nakładanie AABB, **wyłącznie po sprawdzeniu geometrii promieniowej otworu**. Walidator niezależnie czyta realne pliki, kontroluje sygnatury BLEND/FBX/GLB, strukturę animacji GLB, OBJ i materiały MTL.

**NOT RUN, dopóki nie ma niezależnego dowodu:** import Unity 6, EditMode/PlayMode, fizyka kolizji, ręczna ocena renderu. Kontrola AABB i analityczne sprawdzenie wydrążonego cylindra nie zastępują pełnego testu siatek w silniku.

Zakres wyłącznie wizualny: nie zmienia WPN-001..006, scen, wspólnych prefabów, ProjectSettings, mechaniki ani gameplayu. Model powstaje na własnej gałęzi i w Draft PR do `develop`.
