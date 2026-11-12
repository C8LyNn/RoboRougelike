# R4 Style Atlas 5 (#1239)

Eksperymentalna biblioteka pięciu stylów A/B/C/D/E dla istniejących assetów RoboRougelike. Tylko art: nic w `Assets/`,
bez kodu, bez integracji z Unity, bez scalania. Unity: **UNITY_NOT_RUN**. Windows player: **WINDOWS_PLAYER_NOT_RUN**.

## Visual Polish Pass 2 (najnowsze)

Sześć typów × 5 stylów z animacjami mechanizmów, warstwa kadłuba z właścicielem każdego fragmentu, manifest dla prototypu
runtime, testy BEFORE / AFTER w skali gry i test niszczenia — [`polish2/README.md`](polish2/README.md).

## Visual Polish Pass 1

Po warunkowej akceptacji kierunków pilota 2: poprawa spójności kadłuba, materiałów i światła oraz test w skali gry
(28 / 96 / 196 części, kamera rozgrywki, 1080p i 720p) z porównaniem BEFORE / AFTER — [`polish1/README.md`](polish1/README.md).

## Pilot 2 — pięć niezależnych konstrukcji (aktualny kierunek)

Po ocenie właściciela (style zbyt podobne, IoU sylwetek 0,96) kierunek został zmieniony: `pilot2/` zawiera 8 typów × 5 stylów,
każdy wariant zaprojektowany osobno, z zestawami tekstur PBR, planszami do odbioru i podglądem. Szczegóły, wyniki i
ograniczenia: [`pilot2/README.md`](pilot2/README.md). Wszystko poniżej opisuje **poprzednią, parametryczną wersję** (600
wariantów w `atlas/`), zachowaną bez zmian jako materiał porównawczy. Pozostałe 29 typów i przebudowa 120 wykonanych są
wstrzymane do odbioru artystycznego pilota 2.

## Stan na tę paczkę

Liczby poniżej pochodzą z `style_registry.json` i `tests/ATLAS_TEST_SUMMARY.md` — to one są źródłem prawdy, nie ten tekst.

| rodzina | pozycji w katalogu | wykonane ×5 | uwagi |
|---|---|---|---|
| klocki | 30 | 30 | interfejs z #963, porty i gniazda skopiowane z zatwierdzonych GLB |
| łączniki ROD / STRUT | 20 | 20 | budowane z manifestów węzłów i członów #1186 |
| moduły | 44 | 44 | interfejs = proposal z #1238 |
| wyrzutniki | 17 | 17 | klip `Deploy` z ładunkiem jako osobnym węzłem |
| pułapki świata | 6 | 6 | klipy `Arm` / `Triggered` / `Recovery` |
| broń | 9 | 1 (`R4W_Basic`) | pozostałe 8 nie rozpoczęte |
| napędy | 11 | 1 (`R4M_HoverEngine`) | pozostałe 10 nie rozpoczęte |
| koła | 12 | 1 (`R4M_WheelM_Side_Offroad`) | pozostałe 11 nie rozpoczęte |

Razem 120 z 149 pozycji, czyli 600 z 745 wariantów. Brakujące 29 pozycji to części z zatwierdzonym rigiem: każda wymaga
własnej definicji geometrii na nazwach i pivotach węzłów źródłowych. Potok dla nich działa (trzy wykonane przykłady),
brakuje samych definicji.

## Wyniki tej paczki

| | |
|---|---|
| Blender 5.3.0: budowa, eksport, reimport, QA | **600 / 600 BLENDER_PASS** |
| Testy numpy na GLB (T1–T6) | **120 / 120 assetów z kompletem 5/5** |
| Reimport animacji FBX i GLB (macierze węzłów w klatkach kluczowych) | zgodny w 600 / 600 |
| Średnie pokrycie sylwetek między stylami tego samego assetu (IoU, widok 3/4) | **0,96** |

Ostatni wiersz jest złą wiadomością i jest podany celowo: 0,96 oznacza, że **obrysy pięciu stylów są prawie
identyczne**. Najbardziej różnią się długie łączniki (0,87–0,90), najmniej części z rigiem (0,98–0,99) i klocki (0,97).
Style różnią się wyraźnie w trybie shaded i grayscale (materiał, tekstura, detal), słabo w trybie bez koloru i w
sylwetce. Kryterium „rozpoznawalny po samej sylwetce” **nie jest spełnione**.

## Najważniejsze zastrzeżenie

Po korekcie kierunku wymaganie brzmi: pięć stylów ma wyglądać jak pięć różnych gier, czytelnie bez koloru. **Ta paczka
spełnia to tylko częściowo.** Każdy asset ma jedną definicję układu części, a styl ją interpretuje: inne archetypy
brył, inny rodzaj i ilość detalu, pomijanie i powiększanie części, inne tekstury i ruch. Ogólny układ modelu jest
jednak wspólny dla pięciu stylów. Osobno zaprojektowanej konstrukcji per styl tu nie ma. Szczegóły i lista słabych
miejsc: `STYLE_BIBLES.md`.

Ocena wzrokowa należy do właściciela: `compare/5way/<Id>.jpg` pokazuje każdy asset w pięciu stylach i pięciu trybach
(shaded, grayscale, bez koloru i tekstur, wireframe, sylwetka).

## Jak dodać nowy model do wszystkich pięciu stylów

1. Dopisać jedną funkcję w `scripts/adefs.py` (albo w osobnym pliku importowanym przez `adefs`), złożoną z prymitywów
   `akit` — tak jak definicje modułów w `R4ModulesSleek1x1/scripts/modules.py`.
2. Zarejestrować ją: `reg(Id, fn, family, env=..., ...)`. Dla części z zatwierdzonym rigiem: `_iface(...)` z mapą nazw
   węzłów źródłowych.
3. `python3 agen.py <Id>` → `abuild.py` w Blenderze → `atests.py` → `aboards.py` → `aregistry.py`.

Istniejące modele nie są przy tym przebudowywane.

## Co robi potok

- `akit.py` — zestaw prymitywów interpretowany przez aktywny styl; `astyle.py` — parametry, materiały, presety, krzywe
  ruchu; `aanim.py` — zestawy klipów i ślady; `adefs.py` — rejestr assetów.
- `agen.py` — geometria LOD0 per węzeł + LOD1, UV, ślady animacji → `mesh_source/` (nie jest w repozytorium: 220 MB
  JSON, odtwarzalne skryptem).
- `abuild.py` (Blender 5.3.0) — budowa, eksport `.blend` / `.fbx` / `.glb` + LOD1, rendery, QA.
  Dla części z rigiem importuje zatwierdzony GLB, kopiuje z niego gniazda i **przenosi animację funkcjonalną klatka w
  klatkę**; styl zmienia tempo tylko wewnątrz klipów przejściowych (`aanim.WARPABLE`), klipy ze zdarzeniem gry
  (`Fire`, pętle, przemiatanie celowania) są identyczne we wszystkich stylach.
- `atests.py` — niezależne testy numpy na wyeksportowanych GLB.
- `aboards.py` — plansze porównawcze i metryki; `aregistry.py` — macierz assetów.

## QA — co jest sprawdzane

W Blenderze, per wariant (`<Id>_QA.json`): topologia każdego obiektu i LOD1, obrys, granice węzłów względem źródła
(części z rigiem), kontakty LOD0 i LOD1 miarą zatwierdzonych klocków, zgodność gniazd ze źródłem, przemiatanie
animacji, szwy pętli, czy każdy animowany obiekt faktycznie się rusza, reimport FBX i GLB z porównaniem macierzy
węzłów w klatkach kluczowych.

Na GLB (`tests/ATLAS_TEST_SUMMARY.md`): T1 pliki i budżet, T2 obrys, T3 wymienność (te same węzły i gniazda w pięciu
stylach), T4 kanały animacji, T5 tekstury, T6 pięć faktycznie różnych siatek.

## Animacje

- Części statyczne (klocki, łączniki, moduły bez ruchomych części): `Idle`, `Hit`, `Damaged`, `Recover` — ruch tylko
  wizualnego korpusu, ograniczony do 5 mm w najdalszym narożniku; korzeń i gniazda stoją.
- Moduły aktywne: `Idle`, `Activate`, `Hit`. Wyrzutniki: `Idle`, `Deploy` (zdarzenia `Deploy_Open` 27, `Deploy_Out` 63,
  `Deploy_Reset` 85), `Hit`. Pułapki: `Idle`, `Arm`, `Triggered`, `Recovery`.
- Części z rigiem: klipy i zakresy klatek ze źródłowego manifestu.
- Pięć stylów ma te same klatki zdarzeń i różne krzywe (płynne / ciężkie z dobiciem / szybkie z pauzą / nierówne z
  drganiem / z antycypacją i przeregulowaniem).

To są klipy odtwarzane w pliku. Nie jest to potwierdzenie działania w grze.

## Materiały i tekstury

Pięć stref: `R4P_Base`, `R4P_Secondary`, `R4P_Trim` oraz `R4P_Metal` i `R4P_Glow` (te dwie nadal **proposal**).
Mapa detalu stylu (`textures/`, 256 px) jest mnożona przez kolor strefy, więc malowanie działa bez zmiany tekstury.
Trzy presety malowania per styl są w `astyle.PRESETS`. Nie ma map chropowatości, metaliczności ani normalnych.
W GLB tekstura jest osadzona; `.fbx` i `.blend` odwołują się do `textures/` po nazwie pliku.

## Czego nie ma w tej paczce

- 29 części z rigiem (8 broni w tym R41 Automatic, 10 napędów, 11 kół).
- Robotów 100 / 200 / 400 części w pięciu stylach, widoków areny, wspólnego viewera 5 stylów, raportu wydajności.
- Importu do Unity i jakichkolwiek twierdzeń o FPS czy draw callach.

## Decyzje właściciela

1. Czy obecna siła rozróżnienia stylów wystarcza, czy potrzebne są osobne konstrukcje per styl (wtedy: które assety
   najpierw).
2. `R4M_*` czy `R4X_*` dla ośmiu rodzajów ruchu.
3. Kontakty, mocowania i strefy `R4P_Metal` / `R4P_Glow` z #1238 nadal są proposal.
