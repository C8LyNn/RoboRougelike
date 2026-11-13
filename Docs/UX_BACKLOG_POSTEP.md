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

Wyniki dotyczą odpowiednich SHA poszczególnych zmian, a nie pełnej regresji końcowej.
Oryginalne XML i logi znajdują się lokalnie w katalogu `RoboRougelike-LekkaStylizacja-QA`.

## Nadal otwarte

- [ ] Sprawdzić wszystkie powyższe zmiany wizualnie i interaktywnie w Windows Playerze, w tym przy 1920×1080, 1366×768, 1024×768, 768×1024 i 4K.
- [ ] Powtórzyć scenariusz montaż → naprawa wraku: wcześniejszy test zakończył się `NullReferenceException` przed wejściem do garażu (`mount-repair-diagnostic.xml`); przyczyna niepotwierdzona.
- [ ] Sprawdzić pełny przepływ walka → nagrody → trasa → garaż jako gracz; celowane testy i sterowanie API nie zastępują tego przebiegu.
- [ ] Przeprowadzić odpowiednią regresję końcową na wspólnym SHA, po zebraniu zmian.
- [ ] Poddać zależne Draft PR-y przeglądowi i ustalić ich integrację; nie scalać do `main`/`develop` bez zgody.

Pełne zadanie #943 pozostaje otwarte: ta lista nie potwierdza jego odbioru ani
nie zastępuje całościowego audytu UX. Projektowe backlogi #942 i #1020 zawierają
również odrębne mechaniki i decyzje, których nie oznaczono tutaj jako wykonane.
