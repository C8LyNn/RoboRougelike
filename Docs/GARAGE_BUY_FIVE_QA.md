# Zakup pięciu części w garażu

Baza `10b908a6` (Draft PR #1140). Szybka lista i pełny katalog w obu układach mają dodatkowy przycisk zakupu pięciu sztuk. Pokazuje łączny koszt bez rabatu; jest nieaktywny przy braku złomu, mniej niż pięciu wolnych miejscach, blokadzie technologii albo bonusie dostępnym wyłącznie z nagród. Każdy egzemplarz przechodzi przez istniejącą operację zakupu i ma własny identyfikator. Po pełnym zakupie wybierany jest egzemplarz do montażu.

Zakup pięciu jest sekwencją dotychczasowych operacji, **nie transakcją atomową**: jeśli w nietypowej sytuacji jedna z kolejnych operacji zostanie odrzucona, wcześniejsze zakupy pozostaną w magazynie, a komunikat poda faktyczną liczbę i koszt. Nie ma ukrytych dopłat ani zmiany cen.

- Kompilacja Roslyn runtime i testów Editor z istniejącymi referencjami Unity: **PASS**, kody 0/0. Logi `Artifacts/Local/CSharp-GarageBuyFive-20261108/`.
- Poza Unity wywołano rzeczywistą skompilowaną kontrolę warunków: **7/7** przypadków, w tym granica miejsc, brak złomu, blokada, bonus i przepełnienie arytmetyczne.
- Test Unity rzeczywistego zakupu pięciu oddzielnych części, kosztu i wyboru jest przygotowany, ale **NOT RUN**. Sama transakcja w grze, częściowe niepowodzenie, GUI, rozdzielczości i Windows Player: **NOT RUN**.
- Brak nowego buildu. Przy przyszłym teście użyj odizolowanego profilu; sprawdź warianty kół i pancerza, zakup pojedynczy i pięciu, brak środków i 195/196 zajętych miejsc.
