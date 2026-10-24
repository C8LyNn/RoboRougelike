# M02-012 — Kompaktowy silnik V2 (MODEL-02)

**Katalog:** `feature/rrl-model-factory-queue-v1/Docs/ModelFactory/CATALOG_V1.md`.
**Claim:** `Docs/ModelFactory/claims/M02-012.json` na tej samej gałęzi.
**Claim CREATE:** `70f903816ff42877f500b0254c6fd388f25a45ad`,
`2026-10-24T21:46:51.808Z`, agent M02.
**Gałąź modelu:** `feature/rrl-model-m02-012-compact-v2-engine` z `develop`.
Bez zmian scen, prefabów, mechanik, ProjectSettings, main/develop.

## Projekt mechaniczny

Kompaktowy, dwucylindrowy silnik tłokowy V2. Dwie osie cylindrów odchylone
o ±38° od pionu, na rozdzielonych pozycjach Y. Wał korbowy obraca się wokół
osi **+Y w Blenderze** (w Unity, po konwersji glTF Y-up, jest to odpowiednia
oś obrotu eksportera). Dwa czopy korbowe mają przesunięcie fazowe 180°.
Oba tłoki pracują z **90 mm skokiem**; korbowód ma stałą długość **220 mm**.
Ruch tłoków i korbowodów wynika z dokładnego równania suwaka korbowego,
nie z niezależnych sinusoidalnych przesunięć. Wyjściowe koło zamachowe
obraca się razem z wałem.

Każdy cylinder ma **wycięty przedni sektor** ściany, aby tłok był widoczny
w trakcie ruchu. Sześć aluminiowych żeber radiatora na cylinder, oddzielne
głowice i świece, miedziane elementy i jasne akcenty sygnalizacyjne.
Dolna obudowa korbowa jest otwarta wokół wału, z pierścieniowymi łożyskami
i uszczelnieniami z prawdziwym otworem; wał ma promieniowy luz do uszczelnień.
Elementy `PISTON_RIG_L/R`, `CONROD_RIG_L/R`, `CRANK_EYE_RIG_L/R` oraz
`CRANKSHAFT_ROTOR` stanowią siedem niezależnie animowanych grup. Duże
oczka korbowodów mają otwory współosiowe z czopami wału, małe oczka
współosiowe ze sworzniami tłokowymi. Ramiona korbowodów nie wypełniają
otworów łożysk. Tarcze korbowe nachodzą na czopy o 10 mm.

**Jednostki:** 1 Blender unit = 1 metr.
**Pivot montażowy:** `MOUNT_CHASSIS_ORIGIN` w (0,0,0), dolny środek modułu.
**Punkty montażu:** `SNAP_CHASSIS_LEFT`, `SNAP_CHASSIS_RIGHT` i
`SNAP_OUTPUT_SHAFT`. Wymiary korpusu poniżej 1m.
**Materiały:** osiem odrębnych materiałów PBR (grafit, stal, aluminium,
miedź, guma, ceramika, cyjan i bursztyn).

## Regeneracja

Blender 4.x lub nowszy, bez zewnętrznych dodatków:

```bash
blender --background --factory-startup --python Tools/ModelFactory/M02/M02-012/generate_m02_012.py -- --out /tmp/M02-012
python3 Tools/ModelFactory/M02/M02-012/qa_m02_012.py --dir /tmp/M02-012
```

Skrypt usuwa tylko własną kolekcję `MODEL_02_M02_012` przed ponowną
generacją. Zapisuje `M02-012.blend`, `M02-012_animated.glb` i
`M02-012_qa.json`. Artefakty binarne są publikowane w odizolowanym
GitHub Actions `qa-m02-012-compact-v2.yml` — nie są automatycznie
dodawane do repo ani importowane do gry.

## QA i ograniczenia

Źródłowy walidator sprawdza 361 kątów wału × 2 banki = **722 pozycji
kinematycznych**, zachowanie długości korbowodu, odstęp tłoka od głowicy,
luz promieniowy, zamknięcie i niezerową objętość każdej siatki bazowej.
Niezależny `qa_m02_012.py` odczytuje rzeczywiste GLB i BLEND, sprawdza
materiały, nazwy pięciu animowanych grup, kanały animacji i raport
kinematyki. Wyeksportowane animacje i obrót są próbkowane w 91 klatkach.

**Uwaga:** kinematyczna kontrola kolizji nie jest pełną symulacją
kontaktów trójkątów ani testem fizyki w Unity. Zamierzone połączenia
łożysk, wału i obudowy są dopuszczalne. Ręczne oględziny oraz import do
Unity 6 / EditMode / PlayMode wymagają osobnej walidacji; bez dowodu
pozostają **NOT RUN**. Bez merge do develop.
