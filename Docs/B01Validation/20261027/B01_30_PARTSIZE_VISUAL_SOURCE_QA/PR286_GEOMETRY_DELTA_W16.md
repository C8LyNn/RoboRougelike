# B01-30 / W16 — rzeczywiste porównanie geometrii PR #281 → PARTSIZE PR #286

**Klasa wyniku: QA ONLY / input dla B01-23, nie paczka produkcyjna ani zatwierdzony BUILD adapter.**
**Kod źródłowy PARTSIZE:** [Draft #286](https://github.com/C8LyNn/RoboRougelike/pull/286) dokładnie `2b27c3255e6979a291652103bbb7dac2f6c425c5`, bez jakichkolwiek zmian po stronie B01-30. **Canonical R3/R41:** [#272](https://github.com/C8LyNn/RoboRougelike/pull/272) `9cd5a5272a9ebda130a97a5f1f73ce3eaf24b976`. **Poprzednie QA [#281](https://github.com/C8LyNn/RoboRougelike/pull/281)**: analityczne bryły z oryginalnych danych C#.
Nie utożsamiać `source-predicted` z rzeczywistym `Unity Collider.bounds` ani `Physics.ComputePenetration` — silnik nie został uruchomiony.

## Sprawdzona zgodność faktycznych dwóch implementacji

- **PARTSIZE R1 faktycznie istnieje**, nie tylko schemat: `PartSizeContract.cs`, `PartSizeBaselineCatalog.cs`, `PartSizeNativePhysics.cs`, `PartSizeDemoEntry.cs`, rzeczywisty `.unity` i `.prefab` oraz testy.
- Zgodne **Grid = 0.55m, core Box = 0.52m**, podpisane XYZ = Unity X prawo, Y góra, Z przód; Yaw 0/90/180/270, `Rotate` Z→X→Y dla dowolnych ćwierćobrotów. Wąskie części **1×1×1**, sześć kanonicznych szerokich rodzajów **2×1×2** (1.07×0.52×1.07m) z pivotem local-box-center **(0.275,0,0.275)m** względem kotwicy pierwszej komórki. Testy oryginalnego C# pozostawiają te obrysy jako źródłowe, bez automatycznej zmiany w R3.
- Katalog **19/19 kanonicznych R3PartKind**, ale **11 niemierzonych fizycznie** nadal prawidłowo zwraca `UNMEASURED_PHYSICAL_ENVELOPE`: Wheel, Gun, Suspension, Booster, JumpDrive, Blink, Gyroscope, OmniDrive, MagneticGrapple, HoverEngine, CrawlerTrack. **Nie są automatycznie akceptowane do montażu**. Source-geometry fixture poniżej pokazuje, dlaczego.
- PR286 `PartSizeNativePhysics.Validate` faktycznie sprawdza `Collider.bounds` każdego zadeklarowanego dziecka względem logicznego globalnego AABB, owner transform/scale, inventory w obrębie ownera i `Physics.ComputePenetration` dla dostarczonych colliderów sąsiednich. Jest to **kod źródłowy**, nie wykonana fizyka; weryfikacja hosta i natywne Unity `NOT_RUN`.
- **Rzeczywista demonstracja w źródłach:** scena `PARTSIZE001_Demo.unity` i prefab zawierają zgodny stable `m_Script.guid = b0123100000000000000000000000007`; skrypt tworzy runtime kamerę, *trzy* przykłady Cube(OK), wide obrócony Y90(OK), Oversize1.2m(negative OK) z GUI. Scena nie zawiera modelu/rig Wheel, track, gun, punktów socket, konturu komórek ani dwóch pojazdów. URP `Shader.Find` ma runtime fallback, ale brak podpiętego assetu materiałowego gwarantującego shader w Playerze. Render 720p/1080p, import `.unity`, kolor i czytelność z widoku gracza: **NOT_RUN / NOT_VERIFIED**. Stwierdzenia „na pewno czytelne” byłoby fikcją.

## Porównanie obwiedni: źródłowy pivot, logiczne komórki, collider i render

8 source-predicted rodzajów × 4 yaw = **32 rekordy**; poniżej yaw=0, wszystkie metry. Typ `GunHeavy` jest **tylko wariantem ciężkiej lufy**, a nie wszystkich typów `Gun`. Bryły obejmują root collider + rzeczywiste lokalne child primitive transforms z R41, nie przechwycenie Unity `Collider.bounds`.

| R3 source variant | Logiczny footprint | Source-predicted physical AABB X×Y×Z | Source-predicted wyjście poza komórki X/Y/Z | Pivot/strefa szczególna |
|---|---|---|---|---|
| Armor | 1×1×1 | 0.52×0.52×0.52 | 0 / 0 / 0 | root centrum |
| Wheel | 1×1×1 | **0.52×0.91×0.78** | 0 / **0.375** / **0.115** | root 0; koło centrum child local Y -0.5 × 0.52 = **-0.26m**, convex `MeshCollider` |
| CrawlerTrack | 1×1×1 | **0.52×0.676×0.936** | 0 / **0.141** / **0.193** | child local Y=-0.55 × 0.52=**-0.286m**, `BoxCollider` |
| GunHeavy | 1×1×1 | **0.52×0.676×1.352** | 0 / **0.141** / **0.817** | child gun rotation pivot Y=**+0.312m**; lufa zasięg Z max **+1.092m**; dynamic yaw/pitch nieweryfikowany |
| Suspension | 1×1×1 | 0.52×0.52×0.52 | 0 / 0 / 0 | tłok renderu wystaje **0.245m** w dół; jego collider usunięty |
| HoverEngine | 1×1×1 | 0.52×0.5772×0.52 | 0 / **0.0422** / 0 | dolny child, prześwit pod pojazdem odrębny |
| Booster | 1×1×1 | 0.52×0.52×0.78 | 0 / 0 / **0.245** | tylna geometria Z min **-0.52m**; exhaust exclusion strefa odrębna |
| BonusModule | 2×1×2 | 1.07×0.52×1.07 | 0 / 0 / 0 | root cell anchor, box center **(+.275,0,+.275)m**, fizyczny luz po obu stronach szerokiego obszaru 0.015m |

**Dodatkowe dwie analityczne, dodatnie objętości przecięcia** (źródłowe BoxCollider/BoxCollider przy +Z o jedną komórkę, 0.55m, yaw0): `CrawlerTrack` vs blok **0.008182304m³**; `GunHeavy` vs blok **0.006749184m³**. Obie wskazują silny przypadek regresyjny dla realnego `Physics.ComputePenetration`, **nie są jeszcze wynikiem PhysX ani dowodem, że taką konfigurację da się legalnie zamontować**. Dla koła źródłowy convex `MeshCollider` wymaga osobnego testu, nie można wnioskować ze skrzynki AABB o trójkątach.

## Konkretny wniosek o poprawności deklarowanego footprintu

Nie zalecam B01-23 po prostu podmiany `1×1×1` na `2×2×2` dla całej kategorii — **wystawanie jest kierunkowe względem pivotu**:
- **Wheel:** source volume Y[-0.65,+0.26], Z[-0.39,+0.39]m. Root-centered 1-cell ma -0.275/+0.275m; potrzebne byłyby ujemne i dodatnie komórki względem root lub jawnie zatwierdzona obwiednia funkcjonalna poza occupancy. Przy niezmienionym pivoce sugerowany do *dalszego testowania* rozkład komórek ma podpisane zakresy **Y {-1,0}; Z {-1,0,+1}**, nie obecne dodatnio kotwiczone `Dimensions=(1,2,3)`. Nie wprowadzać go automatycznie.
- **CrawlerTrack:** Y[-.416,+.26], Z[-.468,+.468]m — analogiczna asymetria: podpisane **Y{-1,0}, Z{-1,0,+1}** jako kandydat do fizycznego dopuszczenia lub odrębnej przejezdnej strefy ruchowej.
- **GunHeavy:** Y[-.26,+.416], Z[-.26,+1.092]m. Minimalny statyczny axis-aligned unrotated kandydat przy źródłowym anchoringu ma zakresy **Y{0,1}, Z{0,1,2}**, czyli `1×2×3`; **nie wystarczy** po ruchu `GunPivot` (yaw/pitch / muzzle clearance) ani automatycznie nie nadaje socketów/ownership.
- **HoverEngine:** źródłowy child Y min -.3172m wymaga ujemnego Y lub odrębnej zamierzonej przestrzeni napędu; **Booster:** child Z min -.52m wymaga ujemnego Z lub strefy wylotu. **Suspension:** fizyczny box fit PASS, ale *wizualny* child musi pozostać strefą dekoracyjną, nigdy fałszywym punktem fizycznego styku.
- **BonusModule / 5 pozostałych wide:** zachować 2×1×2 / jedno HP / offset .275m, nie skalować do 0.55 box. Ocena **logicznej zewnętrznej ściany** `IsExposedFace` ≠ dowód autoryzowanego styku/socketa z sąsiadem. Owner musi dostarczyć face normal, rzeczywisty collider shape i typed contact, przy rotacjach także X/Z.

Dodatnie testy fizyczne mogą nadal wymagać zamierzonych wyjątków ground contact; samo AABB nie stanowi dowodu penetracji.

## Pilne brakujące przypadki negatywne dla B01-23 / B01-60

1. **E-NEIGHBOR-OMITTED:** dwa realne zachodzące na siebie BoxCollidery *odrębnych ownerów* w identycznej komórce. Wywołaj `PartSizeNativePhysics.Validate` ze **źle wypełnionym** `fullExternalBroadphase = []` i `completeExternalBroadphase = true`. W obecnym kodzie nie ma `Physics.OverlapBox/Sphere/Capsule` na weryfikację kompletności; gdy reszta warunków jest prawidłowa, kod może zwrócić `null`. **To ryzyko fałszywej autoryzacji w host adapterze**, a nie wykonany FAIL w Unity. Dodaj kontrolę pobierania rzeczywistych kandydatów z broadphase, albo jawny, testowalny kontrakt poświadczania kompletności, z negatywnym testem; nie wystarczy test z `completeExternalBroadphase=false`, który już istnieje.
2. **E-TREAD-BOX:** jeden `CrawlerTrack` + sąsiedni +Z standardowy block, identyczna pozycja i 0° w prawdziwym Unity. Loguj `Physics.ComputePenetration`, depth, oba actual `Collider.bounds`, target pose oraz realny skutek próby montażu. Oczekiwany raport: *fizycznie nielegalny przypadek wykryty*, chyba że host kontrakt świadomie dopuszcza taką konfigurację.
3. **E-HEAVY-GUN-BOX:** analogiczny +Z block; powtórz obrót konstrukcji i rzeczywisty yaw/pitch `GunPivot` (0°/45°/90°). Prawdziwy LOS/muzzle clearance ≠ statyczny box.
4. **E-WHEEL-CONVEX:** cztery obroty/mirror + obce sąsiednie primitive Box; rzeczywisty `MeshCollider.convex`, nie AABB intersection jako dowód kontaktu.
5. **E-SUSPENSION-DECOR:** piston renderer pod logiczną komórką; zachowaj brak Collider przy dekoracji i brak możliwości autoryzowania socketów na tłoku; porównaj realny Renderer.bounds, collider inventory oraz działający skok zawieszenia.
6. **E-PIVOT-WIDE:** 2×1×2 w yaw 0/90/180/270 oraz ćwierćobroty X/Z; potwierdź `PartSizeGrid.Occupied`, rzeczywisty `Collider.bounds`, `IsExposedFace`, ownership i położenie lokalnego środka (0.275,0,0.275), a także że nie da się zamontować na wewnętrznej ścianie.
7. **E-NONUNIT-PARENT:** parenting pod skalą różną od (1,1,1) lub yaw innym niż ćwierćobrot; native walidator ma odrzucić `NONCANONICAL_OWNER_TRANSFORM` i nie przepuścić na podstawie wyłącznie globalnego AABB.
8. **E-URP-PLAYER-DEMO:** otwórz rzeczywistą scenę na Unity6000.3.14f1 w 1280×720 i 1920×1080, zrzut UI i trzech figur, kontrola magenty i statusu oczekiwanego czerwonego *poprawnie odrzuconego* przypadku. Dodaj ghost-cell wireframe + pivot/face gizmos oraz jeden autentyczny wheel/track do demonstracji czytelności; obecne 3 cuboidy nie walidują zawieszenia/ruchu.

## Faktycznie wykonane

- W GitHub Actions z *rzeczywistego źródła PR286* z niezmienionego SHA skompilowano **oryginalne C# `PartSizeContract.cs` i `PartSizeBaselineCatalog.cs` bez atrap**; źródłowe konserwatywne AABB 8×4 poddano temu faktycznemu kontraktowi: **32/32 PASS zgodności werdyktów**, w tym **20 oczekiwanych BLOCKED i 12 oczekiwanych source-fits**. Test katalogu potwierdził 11 niezmierzonych fizycznie.
- Testy wcześniejszego #281 source-transform **27/27 PASS**; porównawcze negatywne źródłowe **17/17 PASS**, m.in. celowo uszkodzona stała siatki, CSV fałszywie raportujący PASS, podmieniony GUID demo, untyped broadphase i fikcyjny rodzaj R3.
- *Wykonano:* źródłowe compile C# (.NET 8), logiczny test footprintu, walidacja kompletności i identyczności źródeł, analiza YAML prefab/scena (GUID), JSON wejścia do geometrii. *NIE wykonano:* Unity natywny Editor/PlayMode, RenderPipeline, real Collider.bounds, physics/computePenetration, 2 pojazdy, odbiór G2.
- Własne pliki QA **wyłącznie** `Docs/B01Validation/20261027/B01_30_PARTSIZE_VISUAL_SOURCE_QA/**` i własny workflow. PR #286 i źródła Codexa niezmienione. Wg [HQ #276](https://github.com/C8LyNn/RoboRougelike/issues/276) brak prawa samodzielnej zmiany B01-23 lub merge.

**Handoff:** B01-23 nie powinien przyznawać physical PASS 11 unmeasured lub podejmować automatycznego resize; przed akceptacją montażu potrzebne rzeczywiste native sweep, precyzyjna polityka celowo wystających elementów, zaufany external broadphase i czytelny demonstrator. **G2 HOLD / NO MERGE**.
