# Zbiorcza naprawa grupy części

Baza `f1d42902` (Draft PR #1137). W katalogu magazynu, w obu układach, można jedną operacją naprawić dostępne egzemplarze wybranej grupy. Przycisk pokazuje liczbę możliwych napraw i koszt; przy niepełnym budżecie wybierane są najtańsze naprawy i jest to oznaczone. Operacja dotyczy tylko wyświetlanej grupy, używa dotychczasowej wyceny i naprawy pojedynczej części oraz odświeża podgląd pojazdu raz. Nie zmienia cen ani zamontowanych modułów.

## Faktyczna kontrola

- Kompilacja Roslyn runtime i testów Editor z dotychczasowymi referencjami Unity: **PASS**, kody 0/0. Logi: `Artifacts/Local/CSharp-CatalogBatchRepairFinal-20261108/`.
- Poza Unity wykonano rzeczywisty skompilowany plan i istniejącą naprawę na modelu: trzy uszkodzone części, dwa złomu, naprawiono dwie najtańsze. Podgląd 2 Z zgadza się z faktycznym wydatkiem 2 Z; podgląd nie zmieniał części. Wynik: `Artifacts/Local/CSharp-CatalogBatchRepair-20261108/offline-results.json`. Pierwsza próba miała błędne wartości testowego HP w skrypcie PowerShell; po korekcie danych wykonano właściwą kontrolę. Po niej zmieniono wyłącznie podpis przycisku i ponownie skompilowano.
- Testy Unity przygotowane: zgodność kosztu, wybór najtańszych, naprawa wybranej grupy bez przebudowy pojazdu i geometria przycisku. Wykonanie **NOT RUN**; GUI, faktyczny Player, kontrola fokusu i rozdzielczości **NOT RUN**.
- Brak nowego buildu. Przy późniejszym teście użyj odizolowanego profilu, sprawdź obie orientacje, zerowy i częściowy budżet, wraki, różne grupy i zgodność pokazanej kwoty z wydatkiem.
