# MODEL-05 / M05-014 — Podnośnik dwuramienny

**Wyłącznie wizualny model** do RoboRougelike (Unity 6). Nie implementuje podnoszenia, chwytania ani kolizji gameplay. Z katalogu `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`.

## Rezerwacja

- ID: **M05-014**, agent **M05**, model **Podnośnik dwuramienny**.
- GitHub Contents API **CREATE** przed rozpoczęciem modelu: `2026-10-24T23:44:32.155Z`.
- Claim commit: `485918c4bf2a05a84d8cb09c152b35d441d91c8c`.
- Claim: `Docs/ModelFactory/claims/M05-014.json` na gałęzi kontrolnej.

## Projekt i integracja

Dwa symetryczne, niezależnie wychylane wysięgniki z podwójnymi palcami szczęk, opancerzonymi belkami, cylindrami hydraulicznymi, miedzianymi tłoczyskami, gumowymi nakładkami, obudowami serwomechanizmów, ceramicznymi panelami i podświetlanymi sygnalizatorami. Osiem odrębnych materiałów: grafit, tytan, ceramika, miedź, guma, żółty akcent ostrzegawczy, cyjanowa emisja i ciemne szkło. Emisja nie zastępuje geometrii.

- **Skala:** 1 jednostka Blender = 1 metr.
- **Osie:** +Z góra, +X prawa strona, **−Y tył pojazdu**.
- **Główny pivot/mocowanie:** `MOUNT_REAR_SOCKET` = (0,0,0), na tylnej płaszczyźnie gniazda.
- **Przeguby:** `M05_014_ARM_L_PIVOT` = (−0.255,−0.275,0.240) m, `M05_014_ARM_R_PIVOT` = (+0.255,−0.275,0.240) m.
- **Ruch:** symetryczne wychylenie wokół **lokalnej osi +Z**, od ±12° (szczęki zbliżone) do ±34° (otwarte). Klatki 1/16/30/48, 30 FPS.
- **Punkty robocze:** `M05_014_JAW_CONTACT_L` i `M05_014_JAW_CONTACT_R`; złącze zasilania `M05_014_POWER_PORT`.
- **Kolizje:** obwiednie wysięgników i palców są rozdzielone w 121 pozycjach od 12° do 34°; kontakt piast z łożyskami, śrub i okładzin z korpusem jest celowym połączeniem mechanicznym. Kontrola analityczna nie zastępuje pełnego mesh-vs-mesh collision detection ani inspekcji wizualnej.

## Generowanie i QA

```bash
python3 Tools/ModelFactory/M05/M05-014/validate_m05_014.py
blender --background --python-exit-code 1 --python Tools/ModelFactory/M05/M05-014/generate_m05_014.py -- --out build/M05-014 --repeat-check
python3 Tools/ModelFactory/M05/M05-014/validate_m05_014.py --exports build/M05-014
```

Generator usuwa wyłącznie własną kolekcję i odtwarza model bez przyrostu obiektów. Przy `--repeat-check` dwukrotnie wykonuje model w jednej scenie, porównuje metryki i sprawdza zachowanie obcego obiektu. Blender eksportuje animowane **GLB, FBX i BLEND**, a niezależny walidator sprawdza rzeczywiste pliki. GitHub Actions publikuje eksporty jako artefakty; bez pozytywnego logu CI **Blender/eksport = NOT RUN**.

**Unity:** import, URP, EditMode, PlayMode i ręczna ocena wizualna = **NOT RUN** do czasu ich faktycznego uruchomienia. FBX jest natywnie obsługiwany przez Unity; GLB wymaga importera glTF. Nie włączaj nowych mechanik, nie zmieniaj scen, ProjectSettings ani istniejących prefabów. Draft PR do `develop` bez scalenia.
