# RoboRougelike — postęp backlogu UI/UX

Lista robocza do zadania [#943](https://github.com/C8LyNn/RoboRougelike/issues/943).
Punkty zaznaczone jako wykonane oznaczają **kod na gałęzi i celowane testy**,
nie automatyczny merge ani odbiór wizualny. PR-y poniżej są zależnymi Draft PR-ami.
Bazowy kod tej listy: `675fe0b80da184e4f8db8bf2c5f3e446ee8aeb63` (PR #1289).

## Wdrożone i sprawdzone celowanymi testami

- [x] Garaż: wyróżnienie wyboru, panel komunikatów i akcji — [#1284](https://github.com/C8LyNn/RoboRougelike/pull/1284), Unity EditMode 13/13.
- [x] Walka: mniejsze panele przeciwników i uzbrojenia, czytelniejsze ostrzeżenia — [#1285](https://github.com/C8LyNn/RoboRougelike/pull/1285), 18/18.
- [x] Celowanie: rozmiar panelu komunikatu i wyróżnienie częściowo niedostępnego strzału — [#1286](https://github.com/C8LyNn/RoboRougelike/pull/1286), 15/15.
- [x] Nagrody: wymagane wybory, miejsce w magazynie i liczbowy procent odzysku — [#1287](https://github.com/C8LyNn/RoboRougelike/pull/1287), 20/20.
- [x] Trasa: aktualny limit portfela w podglądzie nagrody i tekstowy status ostatniego etapu — [#1288](https://github.com/C8LyNn/RoboRougelike/pull/1288), 22/22 (w tym wybrane regresje nagród).
- [x] Magazyn nagród: anulowanie rozmontowania bez zmiany części i portfela — [#1289](https://github.com/C8LyNn/RoboRougelike/pull/1289), 7/7 (w tym test sceny sterowany API).
- [x] Podsumowanie wyprawy: zgodny z restartem opis nowego losowania pojazdu, poprawna nazwa licznika części i neutralny opis zwycięstwa — celowany EditMode 4/4, `run-summary-final.xml`.
- [x] Poligon kreatywny: informacja o braku napędu nie blokuje uruchomienia testu — celowany EditMode 5/5, `creative-drive-final.xml`.
- [x] Sekwencja garażu: montaż ostatniej sprawnej części → wskazanie wraku → odmowa montażu → jawna naprawa → montaż tego samego ID — 1/1 test sceny sterowany API, `mount-repair-no-closure.xml`.
- [x] Garaż: wynik zdejmowania lub naprawy nie znika natychmiast pod komunikatem widma; po chwili wraca aktualna wskazówka montażu — 5/5 celowanych testów sceny sterowanych API, `garage-feedback-green.xml`.
- [x] Celowana kontrola istniejącej energii i naprawy: regeneracja w ruchu i na postoju, pole ładowania, pasywna rezerwa, zapis energii oraz aktywny moduł naprawczy — 10/10 EditMode, `energy-repair-current.xml`.
- [x] Menu aktywności: wyjaśnienie blokady aren przy czterech przeciwnikach i niedostępności dodatkowych aktywności w finale — 9/9 EditMode (w tym test sceny sterowany API), `activity-hint-final.xml`. Pierwszy test po zmianie sprawdzał wielkość litery i zakończył się FAIL: `activity-hint-green.xml`.
- [x] Garaż: wynik obrotu zamontowanej części i odmowa niedozwolonego ustawienia koła pozostają widoczne także podczas montażu — 6/6 celowanych testów EditMode, `rotation-feedback-final.xml`.
- [x] Szybka naprawa z magazynu: koszt i wynik pojedynczej lub grupowej naprawy nie są natychmiast zasłaniane wskazówką widma — 5/5 testów sceny sterowanych API, `quick-repair-feedback-final.xml`.
- [x] Regresja importu modeli: test sprawdza komplet 49 oryginalnych modeli i ustawienia importu wszystkich 62 bieżących modeli, dopuszczając dalsze dodatki — 2/2 EditMode, `model-manifest-final.xml`. Poprzednie sztywne oczekiwanie 49 odtworzono jako FAIL w `model-manifest-red.xml`.
- [x] Pauza poligonu: osobne wybory resetu, powrotu do garażu, menu i wyjścia; przy unieruchomieniu domyślnie zaznaczony reset — 5/5 EditMode, `range-pause-final-v2.xml`. Przejście na scenie wykonano przez API; skrót R sprawdzono istniejącym testem wejścia.

Wyniki dotyczą odpowiednich SHA poszczególnych zmian, a nie pełnej regresji końcowej.
Oryginalne XML i logi znajdują się lokalnie w katalogu `RoboRougelike-LekkaStylizacja-QA`.

## Nadal otwarte

- [ ] Sprawdzić wszystkie powyższe zmiany wizualnie i interaktywnie w Windows Playerze, w tym przy 1920×1080, 1366×768, 1024×768, 768×1024 i 4K.
- [ ] Sprawdzić sekwencję montaż → naprawa wraku ręcznym wejściem w Playerze. Wcześniejsza wersja testu (`mount-repair-diagnostic.xml`) zakończyła się `NullReferenceException`; wersja bez przechwytywania lokalnych zmiennych przez lambdę przed przejściem do Play przeszła. Przyczyna wcześniejszego wyjątku pozostaje hipotezą.
- [ ] Sprawdzić pełny przepływ walka → nagrody → trasa → garaż jako gracz; celowane testy i sterowanie API nie zastępują tego przebiegu.
- [ ] Sprawdzić strzałki/Enter i pada w menu pauzy poligonu w Windows Playerze. Test wirtualnej klawiatury w bezokienkowym Unity nie zaobserwował `wasPressedThisFrame` mimo wciśniętego klawisza (`range-pause-diagnostic2.xml`); logika wyboru przeszła test sterowany API, co nie potwierdza fizycznego wejścia. Podmenu przeciwników nadal obsługuje mysz.
- [ ] Przeprowadzić odpowiednią regresję końcową na wspólnym SHA, po zebraniu zmian.
- [ ] Poddać zależne Draft PR-y przeglądowi i ustalić ich integrację; nie scalać do `main`/`develop` bez zgody.

Pełne zadanie #943 pozostaje otwarte: ta lista nie potwierdza jego odbioru ani
nie zastępuje całościowego audytu UX. Projektowe backlogi #942 i #1020 zawierają
również odrębne mechaniki i decyzje, których nie oznaczono tutaj jako wykonane.
