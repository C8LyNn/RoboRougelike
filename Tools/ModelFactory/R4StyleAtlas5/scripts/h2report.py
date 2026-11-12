"""#1239 Hero Benchmark Pass 2: writes hero2/README.md (prose below + tables from the build reports and QA). usage: python3 h2report.py"""
import os, json, glob
import astyle as ST
PKG = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); H2 = os.path.join(PKG, "hero2"); H1 = os.path.join(PKG, "hero1"); P2 = os.path.join(PKG, "polish2")
CID = dict(Gun="R4W_Basic", Wheel="R4M_WheelM_Side_Offroad", Battery="R4X_Battery")
def qa(root, s, t):
    p = os.path.join(root, "variants", ST.LABEL[s], CID[t], CID[t] + "_QA.json"); return json.load(open(p)) if os.path.exists(p) else None
def rp(s, t): return json.load(open(os.path.join(H2, "report", ST.LABEL[s] + "_" + CID[t] + ".json")))
HEAD = '''# #1239 R4 Style Atlas 5 — HERO BENCHMARK PASS 2 (high-fidelity modelling)

Status: **propozycja do odbioru właściciela**. Blender: {npass}/15 `BLENDER_PASS`. Testy GLB: {tests}. Unity: **UNITY_NOT_RUN**. Windows player: **WINDOWS_PLAYER_NOT_RUN**.
Zakres: tylko `Tools/ModelFactory/R4StyleAtlas5/` (`hero2/` + skrypty). Bez zmian w `Assets/`, kodzie gry, fizyce, montażu, Save/Load, identyfikatorach i
parametrach broni. Wcześniejsze dostawy (`atlas/`, `pilot2/`, `polish1/`, `polish2/`, `hero1/`) są nietknięte.

15 modeli: **Basic Gun, Wheel, Battery × 5 stylów**, oraz pięć robotów złożonych z nich i z istniejących części konstrukcyjnych.

## Najkrócej: co się zmieniło w metodzie

Poprzednie iteracje składały model z pudeł, walców i płyt i fazowały krawędzie. Ta iteracja ma inny potok (wszystko w Blenderze, skrypty w `scripts/`):

1. **Model źródłowy high-poly** (`hf.py`, `h2defs.py`) — każda część jest zaprojektowana osobno: profile krzywych obracane w bryły, kadłuby
   rozpinane na zmiennych przekrojach (powierzchnie dwukrzywiznowe), cięcia Boolean (okna, szczeliny, otwory), powierzchnie Subdivision, szyki
   kołowe, rury po krzywych, sprężyny śrubowe, ściegi spawów.
2. **Siatka do gry** — ten sam projekt w rozdzielczości gry, bez drobnicy; normalne ważone.
3. **Wypalanie** (`hbake.py`, Cycles) z high-poly na siatkę gry: normal map, AO, maska krawędzi, identyfikator materiału, normalna w przestrzeni
   obiektu, pozycja. Unikalne UV, jeden atlas na model (2048² broń i koło, 1024² bateria).
4. **Skóra modelu** (`hskin.py`) — base / ORM / normal / emisja składane z wypalonych map: zużycie na wypukłych krawędziach, brud w zagłębieniach,
   rdza i odpryski tam, gdzie wskazuje geometria tej konkretnej części, szum liczony w przestrzeni 3D (nie powtarza się między modelami, nie pęka na szwach UV).
5. **Kontrakt bez zmian** — węzły, pivoty, porty, obwiednia, klipy i zdarzenia są z zatwierdzonego assetu; eksport, animacja i QA robi ten sam
   `pbuild.py` co w poprzednich etapach.

Każdy model ma: plik źródłowy `.blend` (high-poly + siatka gry + LOD1, `source/`), FBX + GLB + LOD1 (`variants/`), zestaw tekstur (`textures/`).

## Co obejrzeć

| Plansze (`boards/`) | Wymóg z zadania |
|---|---|
| `01_A…E_reference.jpg` | 1. referencja obok nowego renderu |
| `02a–d_before_after_*.jpg` | 2. BEFORE / AFTER z Hero Pass 1 (robot, broń, napęd, energia) |
| `03_neutral.jpg` | 3. render neutralny, bez efektownego światła |
| `01_*`, `02*` | 4. pełny render artystyczny |
| `04_views_gun/wheel/battery.jpg` | 5. front / bok / 3/4 / góra |
| `05_materials.jpg`, `09_textures.jpg` | 6. zbliżenia materiałów, zestawy tekstur |
| `06_forms_gun/wheel/battery.jpg` | 7. siatka i duże formy: high-poly, siatka gry, wireframe, tekstury, LOD1 |
| `07_gameplay_distance.jpg` | 8. kamera rozgrywki 1080p; 9. czytelność z 20 / 36 / 60 m (wycinki 1:1) |
| `08_mechanisms_gun/wheel/battery.jpg` | 10. działanie mechanizmów (klatki klipów) |

Plansza `06_forms_*` odpowiada też na pytanie „co wynika z geometrii, co z tekstury, co ze światła”: kolumny 1–3 to sama geometria w glinie,
kolumna 4 to ta sama siatka z teksturami w neutralnym świetle; światło artystyczne jest dopiero na planszach `01` i `02`.

## Analiza referencji i przełożenie na kontrakt R4

Referencje (`reference/`, główne to warianty High Detail) są ilustracjami art-direction, nie modelami. Wspólne dla wszystkich pięciu: pojazd o
rzeźbionym kadłubie z błotnikami, **wielkie koła** z wyraźną piastą, **działo większe niż kabina** na niskim pierścieniu wieży, bateria jako
**ogniwo w klatce** ze świecącym rdzeniem.

| | A Sleek | B Brutal | C Arcade | D Scrap | E Toon |
|---|---|---|---|---|---|
| Duże bryły | warstwy białych skorup na ciemnej ramie | graniaste odlewy, grube płyty | kilka dużych sfazowanych brył | skrzynki i blachy różnego pochodzenia | miękkie, napompowane bryły |
| Przekroje | opływowe, klinowe | ośmiokąty, prostokąty | ośmiokąty z dużą fazą | proste, krzywe z gięcia i wgnieceń | owale, grube zaokrąglenia |
| Broń | osłona lufy z oknem i szewronami, pierścień wieży ze światłem | hamulec wylotowy ze szczelinami, siłowniki oporopowrotnika | krótka lufa, kołnierz, listwa świetlna | perforowany płaszcz, rury, szmata | wielka osłona, „oko”, skośne szczeliny |
| Koło | piasta wielowarstwowa, łuki światła | kołnierz ze śrubami, żółta opaska | jeden pierścień akcentu | zardzewiała felga, łata na śrubach | pierścień z segmentów, emblemat |
| Materiał | ceramika / lakier, gunmetal, mało zużycia | stal o kilku wykończeniach, obity lakier | mat, czyste kolory | rdza, odpryski, spawy, brud | czysty kolor, miękki gradient, jasna krawędź |
| Detal | średni i drobny, gęsty przy mechanice | średni, ciężki (śruby, żebra) | prawie wyłącznie duży | drobny wszędzie, ale z materiału | duży i średni |
| Znak technologii | cyjan: paski, łuki, szewrony | bursztynowe ogniwa, hydraulika | cyjanowe okna | bursztynowe lampy, rury | niebieskie soczewki |
| Z dystansu | biel + czerń + cyjan | czerń + żółć | pomarańcz + granat + biel | brąz + czerwone / żółte łaty | pomarańcz + krem + granat |

**Przeszkody kontraktu — nie obchodzone dekoracją:**

- **Broń.** Zatwierdzony rig daje lufie obrys Ø 80 mm (`Barrel`), kołysce 0,28 × 0,48 × 0,15 m (`Pitch`), wieży 0,38 m szerokości i 0,20 m
  wysokości (`Yaw`), a dolną połowę komórki (0,52 × 0,52 × 0,28 m) zajmuje podstawa (`Body`). Na referencjach lufa ma średnicę rzędu 0,25 m i
  cała broń jest dłuższa niż komórka. Tu masę przeniesiono do osłony kołyski (dozwolonej), lufa zostaje w swoim obrysie, punkt strzału i gniazda
  są nieruszone. **Opcja game-designowa:** osobna część „Heavy Gun” o obwiedni 1 × 2 × 1 komórki albo podniesienie limitu `Barrel` do Ø 140 mm —
  to decyzja o kolizji i balansie, nie o grafice.
- **Koło.** Obrys Ø 0,78 × 0,34 m dobrze pasuje do referencji; zawieszenie mieści się tylko w 0,11 m między płytą montażową a oponą, więc
  wahacze i amortyzatory są krótkie.
- **Kadłub.** Roboty są z klocków 0,52 m z kosmetyczną warstwą osłon; rzeźbionych błotników i maski z referencji nie da się zbudować z obecnych
  części. **Opcja:** rodzina części „pancerz kształtowy” (błotnik, maska, osłona burty) jako zwykłe, montowalne części.
- **Bateria.** Mieści się w komórce; ogniwo ma nieprzezroczyste „podświetlone szkło” (emisja w teksturze), bo format assetu nie niesie przezroczystości.

## Decyzje projektowe per styl

**A — Sleek Futuristic.** Broń: podstawa z ciemnego rdzenia i ceramicznej skorupy podzielonej szczelinami na cztery panele, pierścień światła,
wieża z opływowymi osłonami jarzma i dwiema płetwami czujników, rama + ceramiczny dach i policzki, pomarańczowy kołnierz, osłona lufy z
szewronami światła, pierścień fazowy (obraca się i cofa przy strzale), dwie klapy chłodzenia. Koło: toczona ciemna felga, segmentowy pierścień
ceramiczny, cztery łuki światła, kołpak; owiewka mocowania, pomarańczowa sprężyna, aureola i dwie klapy hamulca. Bateria: poziome ogniwo między
ośmiokątnymi grodziami, obrotowa tuleja ze szczelinami, ceramiczna pokrywa z uchwytem.

**B — Brutal Industrial.** Broń: ośmiokątny odlew podstawy z wieńcem zębatym, płyty czopów z otworami ulżeniowymi, kuty zamek, płyta pancerna w
żółci ostrzegawczej, dwa siłowniki hydrauliczne z chromowanymi tłoczyskami, kołnierz odrzutu i rączka zamka (ruchome), koło zębate napędu, węże,
ośmiokątna lufa z hamulcem wylotowym ze szczelinami. Koło: opona o kwadratowym barku, kołnierz ze śrubami, opaska, nakrętka piasty; zwrotnica z
żebrami, siłownik, tarcza z zaciskiem, wąż. Bateria: agregat — korpus z żebrami, wentylator w osłonie, dwa bursztynowe ogniwa w klatkach,
stycznik, klatka z rur, węże.

**C — Arcade.** Mało części, każda duża: ośmiokątna podstawa z pomarańczowym pierścieniem, kremowe kapsuły na granatowym rdzeniu, suwadło,
listwa pięciu świateł, krótka lufa z kołnierzem. Koło: osiem dużych klocków na rząd, jeden pierścień akcentu z dwoma łukami światła. Bateria:
kanister z dużym oknem, narożniki, pokrywka. Bez tekstury zużycia — tylko miękki cień w zagłębieniach.

**D — Scrap Wasteland.** Każda część z innych elementów: broń — podstawa ze spawanych arkuszy z łatami, dwa różne wsporniki, butla gazowa na
opasce, korba, skrzynkowy zamek z płytą w pasy i zieloną płytą „z innej maszyny”, perforowany płaszcz, luźna pokrywa, rura, lufa z rury z
perforowaną tuleją i szmatą. Koło: każdy klocek bieżnika inny, część urwana; felga z otworami, przykręcona łata, resor piórowy, fartuch. Bateria:
rama z kątownika, beczka, chłodnica, dwie lampy w koszach, wygięta rura, manometr, klema rozruchowa. Materiał: rdza, odpryski lakieru, brud i
zacieki liczone z geometrii; pasy ostrzegawcze malowane w przestrzeni obiektu.

**E — Heroic Toon.** Broń „z twarzą”: granatowa podstawa z pomarańczowym pasem i łapami, kremowe naramienniki, duża pomarańczowa osłona z
kremowym „czołem”, soczewka po każdej stronie, trzy szczeliny światła, sześciokątny dzwon wylotowy (pęcznieje przy strzale), antenka. Koło: gruba
zaokrąglona opona z dwunastoma szewronami, piasta z trzech segmentów pierścienia, kremowe łuki, emblemat. Bateria: szklany dzban energii w
granatowej kołysce z pomarańczowymi klamrami. Tekstura: gradient jaśniejszy ku górze i jasna linia na krawędziach; pasma światła i kontur nadal
są shaderem sceny.
'''
GAPS = '''
## Co nadal różni każdą część od referencji

Przyczyna: **G** geometria, **M** materiał / tekstura, **S** shader, **L** światło / post-process, **K** kontrakt rozgrywki.

| Część | Co różni | Przyczyna | Co dalej |
|---|---|---|---|
| A · broń | mała i przysadzista zamiast długiego działa; mniej warstw paneli i brak widocznego wnętrza mechaniki | K, G | decyzja o obrysie; drugi poziom paneli i wnęki z mechaniką |
| A · koło | piasta ma 3 warstwy zamiast kilkunastu elementów; bieżnik prostszy | G | dodatkowe pierścienie i zaciski; rzeźbiony bieżnik |
| A · bateria | forma pozioma, nie pionowy sześciokątny zasobnik; szkło nieprzezroczyste | G, S | wariant pionowy; shader szkła w URP |
| A · całość | brak poświaty i odbić mokrej posadzki z ilustracji | L | bloom i odbicia w URP |
| B · broń | hamulec i siłowniki są, ale skala mała; mniej śrub, przewodów i odlewniczych faktur | K, G, M | jw. obrys; detal faktury odlewu w normal mapie |
| B · koło | klocki opony mniej rzeźbione; brak napisów i zużycia bieżnika | G, M | bieżnik z profilu, maska zużycia czoła opony |
| B · bateria | brak chłodnicy z lamelami i trzeciego ogniwa | G | lamela jako geometria źródła |
| C · wszystkie | fazy mniej „soczyste”, brak miękkiego światła z ilustracji | G, L | większe fazy na największych bryłach; światło |
| D · broń | za mało warstw nakładających się blach, rur i drobnicy; spawy tylko w normal mapie | G | więcej nakładek i łączników; spawy jako geometria LOD0 |
| D · koło | opona za czysta, brak pęknięć i błota; felga bez wgnieceń | M, G | maska błota, deformacja felgi |
| D · bateria | brak chłodnicy z blachy falistej i gęstej plątaniny rur | G | dodatkowe rury i klamry |
| D · materiał | rdza jest wiarygodna w rozmieszczeniu, ale nie ma faktury złuszczonej farby i zacieków o jakości ilustracji | M | wyższa rozdzielczość masek, warstwy farby |
| E · broń | proporcje bliskie, ale skala mała; brak dużego emblematu | K, G | jw. obrys |
| E · koło | bliskie; brak miękkiego cienia między segmentami | S | AO w shaderze toon |
| E · bateria | bliskie; szkło bez głębi | S | shader szkła |
| E · całość | pasma światła, kontur i światło konturowe nie są w assetach | S | shader toon w URP |
| roboty | kadłub z klocków, nie rzeźbiony; cube i deployer są z poprzedniej iteracji (tylko przemalowane w scenie) | K, G | części pancerza kształtowego; kolejne typy w tym potoku |

## Uczciwie o wyniku

To jest zmiana metody i wyraźny krok w geometrii i materiałach, ale **nie poziom ilustracji referencyjnych**. Ilustracje mają gęstość detalu
i światło, których nie odda 15 modeli zrobionych w jednej iteracji, a ich proporcje łamią kontrakt części. Najbliżej referencji są koła i
baterie; najdalej — bronie (skala) i roboty jako całość (kadłub). Styl D zyskał najwięcej na materiale, styl A na formie; oba nadal są wyraźnie
prostsze niż wzorzec.

Ograniczenia techniczne tej dostawy:
- tekstury są składane proceduralnie z wypalonych map — nie były malowane ręcznie;
- AO jest w kanale R tekstury ORM, ale glTF z Blendera nie podpina go jako `occlusionTexture` — do ustawienia w Unity;
- emisja jest w teksturze; siła emisji i bloom zależą od URP;
- LOD1 to planarne uproszczenie siatki gry (zostaje 60–92 % trójkątów LOD0, płaskie ściany i lica montażowe bez zmian), nie osobny projekt — redukcja jest skromna;
- cube, deployer i warstwa kadłuba nie były przedmiotem tej iteracji; w scenach AFTER dostają tylko paletę tej iteracji;
- mechanizmy to węzły i ścieżki z Visual Polish Pass 2 (zatwierdzone klipy i czasy), z nową geometrią.
'''
def main():
    L = []; npass = 0; rows = []; trow = []
    for t in ("Gun", "Wheel", "Battery"):
        for s in ST.STYLES:
            q = qa(H2, s, t); r = rp(s, t); q1 = qa(H1, s, t); q0 = qa(P2, s, t); ok = bool(q and q["status"] == "BLENDER_PASS"); npass += ok
            t1 = q1["triangles"] if q1 else None; lo = q["triangles"] if q else r["tris_lo_total"]
            rows.append(f"| {t} · {s} | {r['tris_hi_total']:,} | {lo:,} | {(q or {}).get('triangles_lod1', r['tris_lod1']):,} | {t1:,} | {100*(lo - t1)/t1:+.0f} % | {q0['triangles'] if q0 else '—':,} | {(q or {}).get('status', 'NOT_BUILT')} |".replace(",", " "))
    tp = os.path.join(H2, "tests", "hero2_glb_tests.json"); tests = "nie uruchomione"
    if os.path.exists(tp): j = json.load(open(tp)); tests = f"{j['assets_pass']}/{j['assets']} typów PASS"
    out = HEAD.format(npass=npass, tests=tests)
    out += "\n## Liczby\n\n| Model | Trójkąty źródła (high-poly) | Siatka gry (LOD0) | LOD1 | Hero Pass 1 (LOD0) | Zmiana vs Hero Pass 1 | Polish 2 (LOD0) | Blender |\n|---|---|---|---|---|---|---|---|\n" + "\n".join(rows) + "\n"
    st = {}
    for s in ST.STYLES:
        p = os.path.join(H2, "renders", ST.LABEL[s], "scene_stats.json")
        if os.path.exists(p): st[s] = json.load(open(p))["shots"]
    if st:
        out += "\n| Robot (32 części gry) | " + " | ".join(ST.STYLES) + " |\n|---|---|---|---|---|---|\n| trójkąty BEFORE (Hero Pass 1) | " + " | ".join(f"{st[s]['1_hero_before']['triangles']:,}".replace(",", " ") for s in ST.STYLES) + " |\n| trójkąty AFTER | " + " | ".join(f"{st[s]['1_hero_after']['triangles']:,}".replace(",", " ") for s in ST.STYLES) + " |\n"
    out += "\nSiatka gry jest lżejsza niż w Hero Pass 1 w 14 z 15 modeli (wyjątek: koło Arcade, +22 %), mimo że forma jest bogatsza: drobny detal jest w normal mapie, a fazy są jednosegmentowe z normalnymi ważonymi. Roboty AFTER mają o 5–43 % mniej trójkątów niż BEFORE. Tekstury na model: base, ORM, emisja (JPEG) i normal (PNG) + maska malowania; 2048² dla broni i koła, 1024² dla baterii. LOD1 ma własną kopię 512 px.\n"
    out += GAPS + '''
## Odtworzenie

```
cd Tools/ModelFactory/R4StyleAtlas5/scripts
blender -b --factory-startup --python h2run.py -- WORK SCRIPTS SCENE HERO1_WORK R4_REF_DIR RUN        # cały przebieg: 15 x h2model.py -> pbuild.py -> h2scene.py (sceny i kadry z h2gen.py)
blender -b --factory-startup --python h2model.py -- OUT TEMPLATES Gun|Wheel|Battery A..E 2048    # pojedynczy model: źródło, siatka gry, wypalanie, skóra, LOD1
blender -b --factory-startup --python pbuild.py -- VARIANTS OUT/mesh_source R4_REF_DIR "R4W_Basic,R4M_WheelM_Side_Offroad,R4X_Battery" ABCDE
python3 h2gen.py                                                                                 # sceny robotów i kadry
blender -b --factory-startup --python h2scene.py -- OUT SCENE_SRC KIT_SRC SHOTS HERO1_VARIANTS HERO1_VARIANTS HERO2_VARIANTS TEXTURES KIT_OUT
python3 ptests.py hero2 && python3 h2boards.py && python3 h2report.py
```
`TEMPLATES` to `hero1/mesh_source` (kontrakt, węzły mechanizmów i klipy); katalogi `mesh_source/` i `scene_source/` są w `.gitignore`.

## Pytania do właściciela

1. Czy ten poziom geometrii i materiałów jest wystarczającą bazą, żeby tym potokiem zrobić kolejne typy części?
2. Broń: zostajemy przy obrysie Ø 80 mm, czy otwieramy temat większego obrysu / osobnej części „Heavy Gun”?
3. Kadłub: czy chcesz rodziny części „pancerz kształtowy”, bez której roboty nie zbliżą się do sylwetek z ilustracji?
4. Styl E i szkło baterii: czy Codex odtwarza shader toon i shader szkła w URP?
'''
    open(os.path.join(H2, "README.md"), "w", encoding="utf-8").write(out); print("README", len(out), "chars", npass, "pass")
if __name__ == "__main__": main()
