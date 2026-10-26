# RoboRougelike — MAP-SYSTEM R6 / Junction Planning & Drive Evidence

Paczka **inkrementalna względem R5, ale kompletna**: zawiera pełną zawartość R5/R4 i historyczną referencję R3. Nie importuj R5 i R6 równocześnie do dwóch folderów — **R6 zastępuje katalog `MapSystemR4`**.

## Nowości
- `MapR6JunctionPlanner.cs`: analiza połączeń dwóch dróg przez jeden węzeł (próbkowana krzywizna, nachylenie, obwiednia);
- `MapR6JunctionGizmos.cs`: wizualizacja kandydata manewru w scenie (zielony łuk); **bez fizycznych colliderów**;
- `MapR6DriveEvidence.cs`: pasywna telemetria rzeczywistego `Rigidbody`, wykrywanie braku postępu przy żądanym ruchu, tilt, dystans; ręczny eksport JSON;
- testy Unity EditMode/PlayMode i testy offline Python;
- raport referencyjnych skrętów na rzeczywistej mapie `quarry_03`;
- instrukcja integracji i raport dowodowy.

**Cel świadomy:** w R6 nie zastępujemy jeszcze geometrii skrzyżowań produkcyjną, zszytą siatką colliderów. Istniejące pady węzłów zapewniają bazową geometrię. R6 wykrywa i wizualizuje manewry, które trzeba przebudować, zanim uznamy je za przejezdne.

## Import
1. Utwórz osobną gałąź lub kopię Unity 6000.3.14f1 URP.
2. Skopiuj `Assets/RoboRougelike/MapSystemR4` wraz z `.meta` po uprzednim usunięciu poprzedniej wersji tego samego katalogu.
3. Nie kopiuj `R3_Reference`, `Docs` ani `Tools` do `Assets` bez potrzeby.
4. W Unity uruchom menu `Tools > RoboRougelike > MAP R6 > Create Demo In Current Scene` i Play.
5. `MapR6DriveEvidenceRecorder` powstaje automatycznie na testowym roverze. W Inspectorze można ręcznie wyeksportować JSON telemetrii.
6. Dla rzeczywistego pojazdu skonfiguruj adapter wywołujący `SetDriverDemand01` oraz dopiero wtedy zbieraj realne dane.

## Status
- Testy offline: zob. `Docs/R6_QA_EXECUTION_REPORT.md`.
- Kompilacja Unity, PlayMode, rzeczywisty drive test: **NOT RUN** w niniejszym środowisku.
- Repozytorium GitHub i produkcyjny projekt **niezmienione**.

Dodatkowo R6 zawiera `MapR6RouteValidator` (sprawdza kolejne skręty) oraz korektę `lower_road.padWidth` 12 → 18 m w mapie v6. Szczegóły: `Docs/R6_GEOMETRY_FIX_PL.md`.

- `MapR6JunctionClearanceProbe`: G1b — sprawdzenie kolizji planowanego skrętu z przeszkodami na wskazanych warstwach.
