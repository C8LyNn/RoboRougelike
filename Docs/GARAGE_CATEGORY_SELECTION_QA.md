# Wybór kategorii w garażu

Zmiana kategorii szybkiej listy usuwa teraz poprzedni wybór do zakupu i montażu. Kliknięcie tej samej kategorii nie usuwa wybranego egzemplarza. Po zamknięciu pełnego katalogu szybka lista przełącza się na kategorię wybranej grupy. Przeglądanie nie kupuje ani nie usuwa części; nie zmieniono cen, zapisu ani zasad montażu.

Lokalna kompilacja Roslyn kodu gry i testów: exit 0/0. Bezokienne wywołania skompilowanego HUD w PowerShell/.NET: **26/26 PASS** dla przypisania kategorii, czyszczenia grupy, ponownego wyboru tej samej kategorii i synchronizacji po zamknięciu katalogu. Kontrola używa niezainicjalizowanego obiektu HUD i nie obejmuje rzeczywistego pojazdu; wyniki: `Artifacts/Local/CSharp-GarageCategory-Final2-20261108/results.json`.

Dodano test Unity obejmujący prawdziwy garaż, brak zakupu i montażu poprzedniej części oraz synchronizację katalogu. **Unity, Player i ręczne klikanie: NOT RUN** — zgodnie z bieżącą prośbą o lekką pracę. Istniejący system widma może po najechaniu na pojazd nadal pokazać czerwony obrys poprzedniego rodzaju, mimo że montaż bez wyboru jest zablokowany; wygląd wymaga sprawdzenia w Playerze. Build `c332e6a5` nie zawiera tej zmiany.
