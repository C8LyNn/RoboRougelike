# RoboRougelike — zaległa weryfikacja natywna, 8 listopada

Unity 6000.3.14f1, Windows x64. Kod gry buildu: `c332e6a51acf17b7b9ab535dbd899388a20b3806`. Późniejszy commit dodał instrukcję ręczną. Wyniki dotyczą wskazanych wersji: żaden z celowanych przebiegów nie jest pełną regresją.

| Kontrola | Rzeczywisty wynik |
| --- | --- |
| Pierwsza grupa EditMode na `2bddb755` | 50/51 PASS, exit 2. Test wraku miał błąd obiektu przechwyconego przez iterator po przeładowaniu domeny; poprawiony test: osobno 1/1 PASS, exit 0. [Oryginał](QA/Catchup_20261108/EditMode-first.xml), [powtórka](QA/Catchup_20261108/EditMode-wreck-fixed.xml). |
| Energia, AI, zapis na `06b050e1` | 34/35 PASS, exit 2. Test zakupu używał przez refleksję nieaktualnej sygnatury; po dostosowaniu test: osobno 1/1 PASS, exit 0. [Oryginał](QA/Catchup_20261108/EditMode-energy-save-ai.xml), [powtórka](QA/Catchup_20261108/EditMode-purchase-fixed.xml). |
| Modele broni i kół oraz początkowa kamera na `a229a9fe` | 16/16 EditMode PASS, exit 0. [XML](QA/Catchup_20261108/EditMode-art-camera.xml). |
| Zawieszenie i strzelba na `a229a9fe` | Wybrane testy samodzielnych modułów: 5/5 PlayMode PASS, exit 0. [XML](QA/Catchup_20261108/PlayMode-suspension-shotgun.xml). |
| Wskazówka napędu w garażu | 1/1 EditMode PASS, exit 0 po zmianie. [XML](QA/Catchup_20261108/EditMode-drive-hint.xml). |
| Mocowanie i zgodność kół na `d6a104e3` | 9/9 EditMode PASS, exit 0: porty boczne i górne po obrocie, migracja starego zapisu, koło samonastawne w zapisie oraz brak przenikania opony wahacza przez pancerz. [XML](QA/Catchup_20261108/EditMode-wheel-mount-d6a1.xml). |
| Windows x64 `c332e6a5` | Unity build exit 0. Archiwum wypakowano i zweryfikowano SHA-256 wszystkich 212 plików. Wypakowany Player: sterowany API garaż, zdjęcie i ponowne zamontowanie broni i kół, blokada niegotowego pojazdu oraz wejście do walki PASS, exit 0. [Wynik](QA/Catchup_20261108/Player-garage.json). |
| Cold Save → zamknięcie → Load | Osobny Player diagnostyczny `a229a9fe`, dodatkowy symbol `B01_UNIFIED_RC2_ACCEPTANCE` i jawny izolowany slot: dwa osobne procesy exit 0. Zachowane ID, HP, złom, energia, bateria i uszkodzone koło. [Save](QA/Catchup_20261108/Player-cold-save.json), [Load](QA/Catchup_20261108/Player-cold-load.json). To bieżący system gry, nie odbiór niezintegrowanego #923. |
| Broń w Playerze | Osobny Player diagnostyczny `a229a9fe`: cztery bronie, blokada przez własny pancerz i koszty energii PASS, exit 0. Sterowanie przez API. [Wynik](QA/Catchup_20261108/Player-weapons.json). |
| Kamera i jazda w Playerze | Osobny Player diagnostyczny `a229a9fe`, Light, 1280×720, GPU Direct3D11: 979 klatek, 35 zaobserwowanych strzałów, 0 przecięć promienia z własnym robotem, 0 próbek przeniknięcia kamery w świat; exit 0. Kamera była obracana programowo, a nie przez człowieka. Maksymalny całkowity krok obrazu w tej próbie wyniósł 4,305 m podczas sterowanego ruchu/zoomu; wynik testu dotyczy ograniczenia osobnej składowej podążania, nie dowodzi braku wszystkich skoków. [Dane](QA/Catchup_20261108/Player-camera.json). |
| Ciężki na rampie | Diagnoza na realnym pojeździe: bez silnika zatrzymał się u podnóża; z silnikiem i sterowaną korektą kierunku dotarł na szczyt. [Opis](HEAVY_RANGE_RAMP_DIAGNOSTIC_20261108.md), [prosto](QA/Catchup_20261108/EditMode-ramp-straight.xml), [z korektą](QA/Catchup_20261108/EditMode-ramp-steered.xml). |

Rzeczywiste obrazy z renderowania kamery: [garaż](QA/Catchup_20261108/Player-garage-camera-only.png), [poligon](QA/Catchup_20261108/Player-range-camera-only.png). Są to obrazy z tekstury kamery 3D, **bez IMGUI/HUD**. Nie dowodzą czytelności przycisków ani ręcznego sterowania. Zrzuty całego ukrytego okna były czarne, a podłączenie narzędzia okien przekroczyło limit oczekiwania.

Pełna ręczna wyprawa, faktyczne kliknięcia i wizualny odbiór UI na różnych rozdzielczościach: **NOT RUN**. Wczesnych nieudanych przebiegów nie zastąpiono statusem PASS całego zestawu. Historyczny `C0000005` pozostaje niewyjaśniony; udane zamknięcia testowych procesów nie dowodzą jego usunięcia.

[Build do testu](https://drive.google.com/file/d/1F2hnYZHJfzfS4cgGi1Ph_TxVmP5mIV22/view?usp=drivesdk), 46 060 470 B, SHA-256 `7c792cf43ed6a2a9eeb54b6bc7fc7c64379b243d3b4f6561ca2d794cd470c286`. [Krótka instrukcja testowania](TEST_RECZNY_20261108.md). Pełne lokalne Editor.log, Player.log, CSV i kody zakończenia: `Artifacts/Local/NativeCatchup-*`.
