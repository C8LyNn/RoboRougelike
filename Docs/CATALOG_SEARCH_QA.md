# Wyszukiwarka w katalogu części

Baza: `202cd013` (Draft PR #1135). W pełnym katalogu garażu dodano pole wyszukiwania w układzie pionowym i poziomym. Lista jest wspólna dla obu układów, zachowuje istniejące grupy oraz grupy bonusów z magazynu. Dopasowanie nie wymaga wielkich liter ani polskich znaków, a pusty wynik ma komunikat. Zmiana tekstu czyści poprzedni wybór i potwierdzenie demontażu, aby niewidoczna grupa nie pozostała celem zakupu lub montażu. Wyszukiwanie nie zmienia magazynu, złomu ani zapisów.

## Rzeczywista weryfikacja

- Kompilacja Roslyn runtime i testów Editor ze wcześniejszymi referencjami Unity: **PASS**, kody 0/0; logi lokalne `Artifacts/Local/CSharp-CatalogSearchVerified-20261108/`.
- Bez Unity, na rzeczywistym skompilowanym C#: **8/8** prób filtrowania, w tym `kolo` → `Koło`, `ZYRO` → `Żyroskop`, brak wyniku i pusty tekst. Wynik `offline-results.json` w tym samym katalogu.
- Bez Unity, na rzeczywistym skompilowanym modelu i budowanej liście: **46 różnych grup**, w tym 12 kół, 7 pancerzy i 9 broni. Porównanie źródeł potwierdziło zachowanie 17 dodatkowych wariantów pancerza i kół.
- Testy Unity przygotowane: wyszukiwanie, pełność grup oraz wyczyszczenie wyboru bez zmiany magazynu i złomu. Wykonanie **NOT RUN**; także rendering, fokus tekstowy, klawiatura, interakcje w obu orientacjach i prawdziwy Player **NOT RUN**.
- Nie utworzono buildu. Po późniejszej zgodzie na test należy sprawdzić wpisywanie, kasowanie, brak wyników, ponowne otwarcie i zakup w obu układach bez naruszania zapisów gracza.
