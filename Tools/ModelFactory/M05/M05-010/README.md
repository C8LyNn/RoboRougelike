# MODEL-05 / M05-010 — Radiator pionowy wysokowydajny

**Katalog:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`. **Claim:** `Docs/ModelFactory/claims/M05-010.json` (commit `a2583c5ddcbd109a4cb98ba0da233ce59b38bb0b`, 2026-10-24 19:49:10.762 UTC). **Agent:** MODEL-05. **Zakres:** wyłącznie model wizualny; bez nowych mechanik chłodzenia i bez modyfikacji scen, ProjectSettings lub cudzych paczek.

## Konstrukcja i rozróżnienie od M05-009

M05-009 to **boczna chłodnica cieczowa** z wentylatorem kanałowym. M05-010 to **pionowy radiator wysokowydajny do górnego gniazda**: wysoka pionowa kaseta z **30 niezależnymi cienkimi lamelami**, dwoma kolektorami, pionowymi rurami chłodziwa, ceramicznym zwieńczeniem, klatką turbinki i **7-łopatkowym przednim wirnikiem**. To odrębny model i odrębny sposób montażu, nie duplikat mechaniki. Osiem materiałów: grafit, tytan, miedź, ceramika, guma, świecące chłodziwo, bursztynowe wskaźniki, stop łopatek.

**Skala:** 1 Blender unit = 1 m. **Pivot całego modułu:** `MOUNT_TOP_SOCKET` w `(0,0,0)` na środku dolnej płyty. **Osie:** +Y do góry, +Z ku tyłowi; turbinka skierowana ku -Z. Dodatkowe puste punkty: `COOLANT_IN`, `COOLANT_OUT`, `FAN_AXIS`. **Pivot animacji:** `ROTOR_PIVOT` w `(0,0.780,-0.263)`, oś obrotu **lokalna +Z**. Klucze: frame 1 = 0°, frame 31 = 180°, frame 61 = 360° przy 60 FPS, interpolacja liniowa. Animowanych jest 12 siatek: 7 łopatek, piasta, 4 znaczniki. Klatka, przewody i lamelki są nieruchome.

**Prześwity:** wewnętrzny promień osłony turbinki 184 mm; maksymalny promień zamiatania łopatek <175 mm, co daje co najmniej 9 mm luzu radialnego w każdej fazie. Pierścienie klatki leżą na dwóch płaszczyznach Z, poza osiowym zakresem wirnika. Cztery punkty montażowe śrub są na płycie. Mocowanie stacjonarnego trzpienia w obracającej się piaście jest zamierzonym połączeniem łożyskowym, nie błędem kolizyjnym.

## Pliki i uruchomienie

- `generate_m05_010.py` — kompletny proceduralny generator `bpy`, animacja, materiały, GLB/FBX/BLEND, manifest QA.
- `qa_blender_m05_010.py` — rzeczywisty test w Blenderze: dwa wykonania w tej samej scenie, kontrola nieusuwania obcych obiektów, brak namnażania obiektów i materiałów, pełny obrót, pomiar prześwitu i parsowanie animowanego GLB.
- `.github/workflows/ci-model-m05-010.yml` — odizolowany pipeline QA, generuje binarne assety i publikuje artefakt `M05-010-vertical-radiator-GLB-FBX-BLEND-QA`.

```sh
blender -b -t 2 --python Tools/ModelFactory/M05/M05-010/qa_blender_m05_010.py
blender -b -t 2 --python Tools/ModelFactory/M05/M05-010/generate_m05_010.py -- --out build/M05-010
```

**Do Unity 6:** pobierz GLB/FBX/BLEND z artefaktu GitHub Actions, zaimportuj do odrębnego folderu assetów i sprawdź osie, skalę, materiały URP, animację oraz orientację punktu mocowania. Nie dodawaj nowych skryptów gameplay ani mechanik chłodzenia. Dopóki rzeczywisty test w Unity nie zostanie wykonany, status `Unity EditMode/PlayMode = NOT RUN`.

**Zakres QA:** pipeline rzeczywiście uruchamia Blender (gdy runner ma instalację), a nie tylko kompiluje Python. Wykonanie i wyniki należy potwierdzić w GitHub Actions. Brak wykonania Blendera oznacza `NOT RUN`, nie PASS. Kontrola wizualna w Unity/URP i PlayMode wymagają osobnego testu.
