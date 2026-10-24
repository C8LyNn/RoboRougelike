# Rezerwacje modeli 3D — RoboRougelike

Ten folder zawiera pliki `M01-001.json` itd. Pliki z ID są trwałymi rezerwacjami pojedynczych pozycji z [CATALOG_V1.md](../CATALOG_V1.md).

- Ten README **nie jest rezerwacją**.
- Każdy agent zapisuje *nowy* plik JSON przez `create_file`, nigdy nie nadpisuje już istniejącego ID.
- Przed zapisaniem sprawdza aktualną kolejkę własnego agenta. Przy konflikcie sprawdza stan ponownie i wybiera następny wolny identyfikator.
- Nie usuwaj pliku claim automatycznie po porażce: uniemożliwia to powtórzenie modelu; retry wymaga przeglądu i świadomej decyzji.
- Raport produkcji powinien wskazywać model ID, commit rezerwacji, rezultat i PR z modelem.

**Dziś brak zapisanych modeli** nie oznacza, że harmonogram nie istnieje. Rzeczywistą produkcję potwierdza dopiero kod/asset i testy w PR.
