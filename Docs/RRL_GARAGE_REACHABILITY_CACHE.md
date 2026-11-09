# Garaż — ponowne wyznaczanie sieci styków

Baza 3dfa96af / PR #1187. R3 zachowuje wynik istniejącego `BuildPlacementRules.Reachable` pomiędzy sprawdzeniami punktów montażu, jeżeli identyfikatory, kolejność, stan fizyczny, definicje, pozycje i obroty wszystkich części są identyczne. Po zmianie dowolnego z tych danych wywoływany jest ten sam autorytatywny algorytm co wcześniej. Kontrola montażu ani fizyki nie została zastąpiona.

Unity 6000.3.14f1 EditMode: 4/4 PASS, exit 0, `large_graph_cache.xml`: istniejące punkty, demontaż, użycie wielokrotne oraz diagnostyczne wyłączenie i przywrócenie rdzenia. Struktura 324 klocków bez uzbrojenia i napędu: pierwsze zapytanie 337 ms, ponowne przy niezmienionej konstrukcji 6 ms; dla 81 klocków 27/2 ms. Pojedyncze pomiary w testowym środowisku Unity, nie pomiar FPS ani pełnej rozgrywki. Pierwsze obliczenie grafu nadal jest kosztowne. Podczas zwykłego montażu przebudowa pojazdu może ponownie naliczyć cały graf.

Nie podniesiono limitu 180 pól, nie potwierdzono jazdy konstrukcją 324 klocków, nie wykonano pełnej regresji ani buildu.
