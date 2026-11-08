# Filtr posiadanych części w garażu

Baza: `01b9cf89` (Draft PR #1136). Szybka lista garażu otrzymała przełącznik „Filtr: wszystkie / posiadane” w układzie poziomym i pionowym. Filtr korzysta z tej samej listy typów co pełny katalog; pokazuje także zniszczone egzemplarze, które gracz może naprawić. Gdy grupa przestaje być posiadana, jej ukryty wybór jest usuwany. Jeżeli gracz wybierze nieposiadaną część w pełnym katalogu i wróci do garażu, filtr wraca do wszystkich, by ten wybór był widoczny. Zakup, ceny, montaż, naprawy i zawartość magazynu nie zostały zmienione.

## Rzeczywista weryfikacja

- Kompilacja Roslyn runtime i testów Editor ze wcześniejszymi referencjami Unity: **PASS**, kody 0/0; logi lokalne `Artifacts/Local/CSharp-GarageOwnedFilter-20261108/`.
- Poza Unity na rzeczywistym skompilowanym modelu i logice filtra: **5/5** prób kategorii i trybów; sprawdzono, że zniszczony posiadany pancerz pozostaje widoczny. HUD utworzono bez silnika, więc nie jest to test GUI.
- Test Unity przygotowany: filtr, zniszczona część, brak ukrytego wyboru i powrót z pełnego katalogu. Wykonanie **NOT RUN**. Interakcje, renderowanie, faktyczny montaż i Player **NOT RUN**.
- Nie przygotowano nowego buildu. Przy przyszłym teście sprawdź obie orientacje, pustą kategorię, zakup, montaż ostatniej części i powrót z pełnego katalogu.
