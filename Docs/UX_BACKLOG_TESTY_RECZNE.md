# Krótka lista do ręcznego sprawdzenia

Ta lista dotyczy gałęzi `fix/garage-purchase-receipt` i powiązanych zmian
z [Draft PR #1303](https://github.com/C8LyNn/RoboRougelike/pull/1303).
Nowy build nie został przygotowany. W Unity uruchom `Assets/scene 1.unity`;
sprawdzenie w edytorze nie zastępuje osobnej próby Windows Playera.

1. **Garaż → części:** kup jedną część i pakiet pięciu, wskaż miejsce montażu.
   Czy koszt jest czytelny, a po chwili powraca opis aktualnego widma?
2. **Garaż → edycja:** spróbuj zdjąć lub obrócić rdzeń, napraw część i obróć
   zamontowane koło. Czy widać wynik, a czerwone widmo dalej wyjaśnia blokadę?
3. **Garaż → aktywności:** gdy dostępne są tylko niektóre areny, sprawdź,
   czy wyjaśnienie jest zrozumiałe i zgadza się z wyszarzonymi przyciskami.
   W finale sprawdź komunikat o ograniczeniu dodatkowych aktywności.
4. **Poligon → pauza:** strzałkami wybierz powrót do garażu i potwierdź Enterem;
   osobno sprawdź reset oraz ponowne wejście. Jeśli masz pada, sprawdź krzyżak
   i A/B. Dodawanie przeciwników w podmenu nadal wymaga myszy.
5. **Wyprawa → koniec:** po zwycięstwie lub porażce sprawdź, czy podsumowanie
   i informacja o nowym losowaniu pojazdów są zrozumiałe.

Przy każdej uwadze zanotuj ekran, wykonane kroki i oczekiwany wynik. Warto
też odnotować rozdzielczość i to, czy problem pojawia się zawsze. Lista ma
pomóc w decyzjach o dalszym UX, nie potwierdza jakości wizualnej ani pełnej
regresji — szczegóły wyników i pozostałych kontroli są w `UX_BACKLOG_POSTEP.md`.
