# RoboRougelike — wspólny katalog modeli 3D V1

**Data utworzenia:** 2026-10-24. **Status:** kolejka pilotażowa / nie są to gotowe assety.
**Repo:** C8LyNn/RoboRougelike. **Gałąź operacyjna z katalogiem i rezerwacjami:** `feature/rrl-model-factory-queue-v1`.
**Źródło prawdy:** ten plik na powyższej gałęzi (NIE wersja z main/develop). **Zakres:** 5 rozłącznych kolejek po 24 modele, łącznie 120 unikalnych identyfikatorów.

## Protokół przydzielania — OBOWIĄZKOWY

1. Odczytaj NA NOWO ten katalog z gałęzi `feature/rrl-model-factory-queue-v1`, bieżące istniejące assety/PR z repo i folder `Docs/ModelFactory/claims/` na TEJ SAMEJ gałęzi. Brak folderu na starcie = brak rezerwacji. Jeśli katalog jest niedostępny, zgłoś `BLOCKED_CATALOG` i nie wymyślaj pozycji.
2. Wolno wybierać WYŁĄCZNIE element z własnej kolejki M01/M02/M03/M04/M05. Wybierz najniższy dostępny numer `MXX-NNN`, którego plik rezerwacji nie istnieje i którego odpowiednik nie jest już faktycznie obecny w assetach/PR. Każda nazwa/ID jest unikalna globalnie.
3. **Przed generowaniem lub commitem** dokonaj atomowej rezerwacji przez GitHub Contents API operacją CREATE (nigdy update istniejącego): `Docs/ModelFactory/claims/<ID>.json` na gałęzi `feature/rrl-model-factory-queue-v1`. Zawartość JSON: `{"id":"M01-001","agent":"M01","status":"CLAIMED","claimed_at_utc":"<rzeczywisty timestamp>","model":"<nazwa>","run":"<id godziny>"}`. Wstaw faktyczne dane. Sukces odczytaj z prawdziwego wyniku API/commita.
4. Jeśli CREATE zwróci 409/422 lub plik już istnieje, odśwież listę claims i przejdź do KOLEJNEGO wolnego własnego ID; nigdy nie nadpisuj cudzego claim. Jeśli API rezerwacji nie działa, `BLOCKED_CLAIM` i **żadna deklaracja ukończenia nowego modelu**. To ogranicza ryzyko wyścigów — nie daje gwarancji atomowej transakcji wieloplikowej.
5. Po udanej rezerwacji przygotuj TYLKO ten model: kompletny generujący go skrypt Blender Python `bpy`, wielomateriałowy wygląd, poprawne punkty mocowania, prawidłowa topologia i — jeśli podano ruch — animacja z kontrolą kolizji. Nie dopisuj nowych mechanik ani nie zmieniaj scen lub ProjectSettings.
6. Rezultaty przechowuj na NOWEJ, wyłącznej gałęzi `feature/rrl-model-<ID lowercase>-<krótki-slug>` opartej na `develop`, pod `Tools/ModelFactory/MXX/<ID>/` i ewentualnie `Assets/RoboRougelikeModels/MXX/<ID>/` wraz z `.meta`. Otwórz DRAFT PR do `develop`; bez automatycznego merge/push na main. W PR i raporcie podawaj ID claim, ścieżki, commit SHA, rzeczywiste testy; niedostępny Blender/Unity = NOT RUN.
7. W razie duplikatu istniejącego modelu z tej listy nie reinterpretuj go samodzielnie w nowy, tylko utwórz jawne `SKIPPED_DUPLICATE` w rezerwacji (jeśli udało się zarezerwować) i wybierz następną pozycję. Model ma być nowy, nie kolejną kopią wcześniejszego.
8. Nigdy nie pobieraj drugi raz już zarezerwowanego ID, nawet gdy wcześniejszy agent zgłosił BLOCKED. Naprawy wykonuj jako kontynuację istniejącego ID/PR z wyraźnym zleceniem, nie jako nowy przydział. 
9. **Wynik cyklu:** co najwyżej jeden nowy zarezerwowany i produkowany model; gdy lista się skończy, `QUEUE_EXHAUSTED` i zero wymyślonych zadań. Nie obiecuj 5 gotowych plików co godzinę bez dowodów wykonania.

## Minimalne kryteria QA

- Skala Unity: 1 jednostka = 1 m; pivot na rzeczywistym punkcie montażu i oś animacji jawnie opisana. Geometria bez poszarpanych normalnych, przypadkowych ostrych krawędzi i niezamierzonych samoprzecięć.
- Co najmniej 3 odrębne materiały (metal, okładzina, akcent); emisja tam, gdzie uzasadniona, bez polegania na emisji jako jedynej formie.
- Ruchome części muszą mieć realistyczny zakres ruchu i nie przenikać przez obudowę w klatkach skrajnych. Nie każdy model wymaga osobnego systemu animacji Unity.
- Blenderowy skrypt ma działać wielokrotnie bez niekontrolowanego mnożenia obiektów i generować przewidywalny wynik; raportuj testy wykonane faktycznie.
- Istniejące mechaniki WPN-001..006, DEF-001..002, SEN-001 itp. już mają gałęzie/PR: te pozycje są koncepcjami **nowych modeli wizualnych**, nie ponowną implementacją ich gameplayu. Sprawdź aktualne PR przed zajęciem pozycji.
- Brak Blender/Unity/eksportera lub uprawnienia zapisu = jawny NOT RUN/BLOCKED, nigdy fikcyjny PASS.

## Kolejki — przydział wyłączny

### M01 — Uzbrojenie — tylko model/wizualizacja, bez nowej mechaniki walki

| ID | Model | Mocowanie | Ruch/animacja | Wyróżnik wizualny |
|---|---|---|---|---|
| M01-001 | Czterolufowy blaster bębnowy | górne gniazdo | obrót bębna | asymetryczne panele grafit/miedź |
| M01-002 | Krótka haubica tłokowa | górne gniazdo | odrzut suwadła | gruby stożkowy hamulec |
| M01-003 | Podwójny emiter łuku | górne gniazdo | przeskok osłon | dwa rozdzielone świecące rdzenie |
| M01-004 | Moździerz kasetowy | górne gniazdo | odchylanie lufy | pudełkowy zasobnik nabojów |
| M01-005 | Boczny miotacz tarcz | boczne gniazdo | obrót magazynka | płaski podwójny rotor |
| M01-006 | Wyrzutnia igieł ceramicznych | górne gniazdo | wysuwanie szyny | białe panele z ostrymi żebrami |
| M01-007 | Impulsowa armata pierścieniowa | górne gniazdo | ruch pierścieni | trzy neonowe pierścienie |
| M01-008 | Karabin chłodzony cieczą | górne gniazdo | przepływ na tłokach | zewnętrzny radiator i rurki |
| M01-009 | Trójsegmentowa wyrzutnia rakiet | górne gniazdo | otwieranie pokryw | trzy kwadratowe cele |
| M01-010 | Energetyczny widełkowy projektor | przednie gniazdo | rozwarcie wideł | otwarta brama z emissive |
| M01-011 | Długi karabin precyzyjny | górne gniazdo | ruch zamka | asymetryczne wsporniki |
| M01-012 | Pneumatyczna wyrzutnia śrub | górne gniazdo | ruch tłoka | widoczny zbiornik ciśnieniowy |
| M01-013 | Wyrzutnia siatki zakłócającej | górne gniazdo | obrót kasety | sześciokątne role prowadzące |
| M01-014 | Elektromagnetyczny impulsator | przednie gniazdo | wysuwany rdzeń | masywna sekcja ferrytowa |
| M01-015 | Dwukierunkowa wieżyczka strzelecka | górne gniazdo | obrót obu ramion | niskoprofilowa rama V |
| M01-016 | Rozkładane działo obrotowe | górne gniazdo | składane ramiona | segmentowe osłony |
| M01-017 | Szybkostrzelny miotacz kul | górne gniazdo | obrót podajnika | kulisty zasobnik |
| M01-018 | Armata kinetyczna z przeciwwagą | górne gniazdo | odrzut przeciwwagi | otwarty tylny mechanizm |
| M01-019 | Wyrzutnia dymna sześciotubowa | boczne gniazdo | obrót kasetki | sześć asymetrycznych tub |
| M01-020 | Krótki emitujący falę rezonator | przednie gniazdo | drżenie pierścienia | falisty profil promiennika |
| M01-021 | Kanister plazmy z dyszą | boczne gniazdo | praca zaworu | ciśnieniowa butla i zawory |
| M01-022 | Samonaprowadzający zasobnik dronów | górne gniazdo | otwieranie pokryw | układ komórek 2x3 |
| M01-023 | Wieżyczka o profilu klinowym | górne gniazdo | obrót wieży | pancerna dwuczęściowa osłona |
| M01-024 | Mechaniczny harpun kotwiczący | przednie gniazdo | wysuw grotu | linowy bęben i pazury |

### M02 — Napęd mechaniczny i koła

| ID | Model | Mocowanie | Ruch/animacja | Wyróżnik wizualny |
|---|---|---|---|---|
| M02-001 | Koło terenowe z segmentowym bieżnikiem | oś boczna | obrót koła | dzielone gumowe klocki |
| M02-002 | Koło wąskie do prędkości | oś boczna | obrót koła | gładki profil i lekkie szprychy |
| M02-003 | Koło balonowe na piasek | oś boczna | obrót koła | szeroki płaski bieżnik |
| M02-004 | Koło stalowe perforowane | oś boczna | obrót koła | otwory w feldze |
| M02-005 | Koło z rozkładanymi łopatkami | oś boczna | rozłożenie łopatek | płetwowe osłony |
| M02-006 | Koło kolczaste na rumowisko | oś boczna | obrót koła | wymienne stalowe wypustki |
| M02-007 | Para rolek podporowych | spód ramy | obrót rolek | dwukolorowe amortyzatory |
| M02-008 | Pojedynczy moduł gąsienicy | boczne gniazdo | obrót ogniw | ogniwa z klinowymi nakładkami |
| M02-009 | Trójkołowy wózek gąsienicowy | boczne gniazdo | obrót kół | wyraźna belka nośna |
| M02-010 | Wahacz niezależnego zawieszenia | boczne gniazdo | skok wahacza | zewnętrzna sprężyna |
| M02-011 | Zawieszenie podwójnego wahacza | boczne gniazdo | ugięcie | dwie belki i amortyzator |
| M02-012 | Kompaktowy silnik V2 | wewnątrz ramy | ruch tłoków | radiator aluminiowy |
| M02-013 | Silnik osiowy z wirnikiem | wewnątrz ramy | obrót wału | okrągły miedziany stojan |
| M02-014 | Jednostka napędu hybrydowego | wewnątrz ramy | obrót osłony | bateria i przekładnia |
| M02-015 | Skrzynia biegów z odkrytą zębatką | wewnątrz ramy | obrót kół zębatych | szkielet stalowy |
| M02-016 | Przekładnia stożkowa kątowa | spód ramy | obrót kół | odsłonięte sprzęgnięcie 90 stopni |
| M02-017 | Dyferencjał terenowy | pod osią | obrót półosi | osłona z żebrami |
| M02-018 | Mechaniczny hamulec tarczowy | przy kole | obrót tarczy | zacisk z ceramiczną tarczą |
| M02-019 | Hydrauliczny hamulec bębnowy | przy osi | ruch szczęk | okrągła obudowa z oknami |
| M02-020 | Układ kierowniczy z listwą | przód ramy | ruch listwy | gumowe manszety |
| M02-021 | Zwijana stopa podporowa | spód ramy | wysuw stopy | teleskopowa rura |
| M02-022 | Moduł osi skrętnej | spód ramy | skręt piasty | otwarte przeguby |
| M02-023 | Łańcuchowy napęd boczny | boczne gniazdo | ruch łańcucha | tarcze o różnym przełożeniu |
| M02-024 | Koło wielokierunkowe z rolkami | oś boczna | obrót kółka | ukośne rolki obwodowe |

### M03 — Ramy, pancerze, porty i mocowania

| ID | Model | Mocowanie | Ruch/animacja | Wyróżnik wizualny |
|---|---|---|---|---|
| M03-001 | Klatka bazowa 1x1x1 | ramowe łącza | brak | cztery skośne rozpórki |
| M03-002 | Belka teleskopowa 1x2x1 | ramowe łącza | wysuw segmentu | żółte oznaczenia skali |
| M03-003 | Rama mostowa 2x1x1 | ramowe łącza | brak | dwie masywne dźwigary |
| M03-004 | Wzmocniona rama X | ramowe łącza | brak | przekątne żebra |
| M03-005 | Adapter ośmiokątny uniwersalny | gniazda ramy | obrót kołnierza | osiem punktów montażu |
| M03-006 | Uchwyt boczny L | boczne gniazdo | brak | odcięte narożniki |
| M03-007 | Uchwyt pionowy T | górne gniazdo | brak | widoczne śruby |
| M03-008 | Płyta pancerza klinowego | przód ramy | brak | skośna powierzchnia trójwarstwowa |
| M03-009 | Płyta pancerza perforowanego | bok ramy | brak | ażurowa stal i siatka |
| M03-010 | Pancerz segmentowy schodkowy | przód ramy | brak | warstwowa płytka |
| M03-011 | Narożnik pochłaniający uderzenia | narożnik ramy | ugięcie osłony | gumowy zderzak |
| M03-012 | Zderzak sprężynujący U | przód ramy | skok zderzaka | dwie prowadnice |
| M03-013 | Osłona progu dolnego | spód ramy | brak | grube nity |
| M03-014 | Płoza podwozia ślizgowego | spód ramy | brak | długi spód ceramiczny |
| M03-015 | Klatka ochronna generatora | wewnątrz ramy | otwierana klapa | pręty i zatrzaski |
| M03-016 | Wspornik odwróconego trapezu | podwozie | brak | prześwity w belkach |
| M03-017 | Złącze szybkomocujące | gniazdo ramy | ruch rygla | kolorowe wskaźniki zamka |
| M03-018 | Kardanowy uchwyt ruchomego modułu | górne gniazdo | obrót 2 osi | pierścieniowy przegub |
| M03-019 | Osłona akumulatora przeciwodłamkowa | wewnątrz ramy | otwierana pokrywa | dwuskrzydłowa klapa |
| M03-020 | Kosz modułowy cargo | tył ramy | brak | siatka z prętów |
| M03-021 | Pałąk przeciwkapotażowy | góra ramy | brak | gięta rura w podkowie |
| M03-022 | Zewnętrzna kratownica boczna | bok ramy | brak | kratownica trójkątna |
| M03-023 | Blok montażowy 2x2 | ramowe łącza | brak | gniazda na sześciu płaszczyznach |
| M03-024 | Moduł mostkujący przerwę | ramowe łącza | wysuw belki | nakładane elementy nośne |

### M04 — Mobilność specjalna, wirniki, hover i dopalacze

| ID | Model | Mocowanie | Ruch/animacja | Wyróżnik wizualny |
|---|---|---|---|---|
| M04-001 | Wirnik boczny w osłonie | boczne gniazdo | obrót łopat | osłona kanałowa |
| M04-002 | Wirnik podwójny przeciwsobny | boczne gniazdo | dwa obroty | koncentryczne tarcze |
| M04-003 | Wentylator kanałowy dolny | spód ramy | obrót wirnika | duży pierścień |
| M04-004 | Dysza odrzutowa z wektorowaniem | tył ramy | wychył dyszy | ceramiczna gardziel |
| M04-005 | Dopalacz dwukomorowy | tył ramy | ruch klap | podwójne świecące gardziele |
| M04-006 | Dopalacz płaski szczelinowy | tył ramy | ruch żaluzji | szeroka płaska dysza |
| M04-007 | Silnik hover z czterema emiterami | spód ramy | pulsacja emiterów | symetryczne cztery dyski |
| M04-008 | Hover boczny stabilizujący | bok ramy | wychył emiterów | podwójne soczewki |
| M04-009 | Stabilizator żyroskopowy | wewnątrz ramy | obrót 3 osi | kuliste pierścienie |
| M04-010 | Stabilizator odśrodkowy | górne gniazdo | obrót przeciwwag | widoczne ciężarki |
| M04-011 | Rozkładana płetwa aerodynamiczna | bok ramy | rozkładanie | segmentowa powierzchnia |
| M04-012 | Lotka korekcyjna tylna | tył ramy | wychył lotki | pancerne żebra |
| M04-013 | Śruba wodna w pierścieniu | tył ramy | obrót śruby | zintegrowana kratka |
| M04-014 | Napęd ślimakowy do błota | spód ramy | obrót ślimaka | spiralny cylinder |
| M04-015 | Nóżka skokowa pneumatyczna | spód ramy | wysuw nogi | gruby tłok |
| M04-016 | Moduł mini-odrzutników narożnych | narożnik ramy | obrót dysz | cztery małe turbinki |
| M04-017 | Pierścień teleportacyjny wizualny | górne gniazdo | rotacja pierścienia | rozsuwane segmenty |
| M04-018 | Kotwica stabilizująca | spód ramy | wysuw kotwic | rozłożyste pazury |
| M04-019 | Turbina z rozkładanymi segmentami | boczne gniazdo | otwarcie obudowy | łuskowa osłona |
| M04-020 | Wysuwany booster manewrowy | tył ramy | wysuw silnika | prowadnice tłokowe |
| M04-021 | Ster ogonowy przegubowy | tył ramy | skręt na osi | dwuczęściowy fin |
| M04-022 | Mikrohover ślizgowy | spód ramy | pochylenie dysku | pojedynczy moduł skuterowy |
| M04-023 | Przepustnica jonowa | tył ramy | ruch przysłony | błękitne soczewki |
| M04-024 | Układ czterech dysz wektorowych | spód ramy | cztery wychyły | symetryczne rury |

### M05 — Sensory, energia i moduły użytkowe

| ID | Model | Mocowanie | Ruch/animacja | Wyróżnik wizualny |
|---|---|---|---|---|
| M05-001 | Radar obrotowy talerzowy | górne gniazdo | obrót anteny | segmentowany reflektor |
| M05-002 | Radar panelowy płaski | górne gniazdo | pochylenie panela | siatka emitera |
| M05-003 | Skaner optyczny podwójny | przednie gniazdo | obroty soczewek | dwa czerwone obiektywy |
| M05-004 | Lidar kopułowy | górne gniazdo | obrót głowicy | przezroczysta osłona |
| M05-005 | Generator tarczy pierścieniowej | wewnątrz ramy | pulsacja rdzenia | przezroczyste plastry |
| M05-006 | Projektor bariery kierunkowej | przednie gniazdo | rozstawienie ramion | dwie płyty |
| M05-007 | Akumulator kasetowy | wewnątrz ramy | wysuw szuflad | cztery ogniwa |
| M05-008 | Bateria cylindryczna | wewnątrz ramy | otwarcie klapy | grube przewody |
| M05-009 | Chłodnica cieczowa boczna | boczne gniazdo | ruch wentylatora | żeberkowy radiator |
| M05-010 | Radiator pionowy wysokowydajny | górne gniazdo | obrót turbinki | cienkie lamelki |
| M05-011 | Reaktor miniaturowy w klatce | wewnątrz ramy | obrót pierścienia | jasne jądro |
| M05-012 | Generator awaryjny | wewnątrz ramy | obrót prądnicy | żółte naklejki serwisowe |
| M05-013 | Moduł naprawczy z manipulatorem | boczne gniazdo | ruch ramienia | trzy przeguby |
| M05-014 | Podnośnik dwuramienny | tył ramy | rozwarcie szczęk | bliźniacze wysięgniki |
| M05-015 | Mechaniczny chwytak cargo | tył ramy | zacisk szczęk | trzy palce |
| M05-016 | System lin holowniczych | tył ramy | obrót bębna | hak i szpula |
| M05-017 | Zasobnik środków gaśniczych | bok ramy | ruch zaworu | okrągłe czerwone butle |
| M05-018 | Przenośny reflektor roboczy | górne gniazdo | pochylenie lampy | soczewka z daszkiem |
| M05-019 | Projektor holograficzny wizualny | górne gniazdo | rozsunięcie paneli | stożek emisyjny |
| M05-020 | Nawigacyjna antena kratowa | górne gniazdo | obrót anteny | składane kratki |
| M05-021 | Sygnałowy maszt składany | górne gniazdo | wysuw masztu | diodowe pierścienie |
| M05-022 | Moduł odzysku energii kinetycznej | wewnątrz ramy | obrót koła zamachowego | ciężki rotor |
| M05-023 | Kontener części zamiennych | tył ramy | otwieranie drzwi | zatrzaski i uchwyty |
| M05-024 | System chłodzenia kriogenicznego | wewnątrz ramy | praca tłoków | lodowe niebieskie rurki |

## Kontrola unikalności

- **Rozłączność:** żaden identyfikator nie występuje w dwóch kolejkach, żadna kolejka nie jest wspólna.
- **Rezerwacja trwała:** obecność pliku `claims/<ID>.json` oznacza, że numer został zajęty; obecny stan `status` nie przywraca go do puli.
- **Historia produkcji:** zapisuj link do osobnego PR/commita w claim przez update istniejącego pliku z SHA po odczycie, ale tylko w ramach własnego ID; nie warunkuj rozpoczęcia nowego zadania powodzeniem tej aktualizacji.
- **Wspólny branch kontrolny:** NIE scalaj ani nie usuwaj gałęzi kontrolnej, dopóki automatyzacje odczytują z niej katalog i claimy. Dokonuj zmian katalogu wyłącznie jako kontrolowana kolejna wersja.
