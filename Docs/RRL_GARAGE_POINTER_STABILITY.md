# Stabilność wskazywania punktów montażu

Baza: `cdbde039`. Użytkownik zgłosił, że to samo miejsce pozwala albo nie pozwala na montaż różnych części. Odtworzono jedną przyczynę w rzeczywistym garażu Unity: promień skierowany w obręb widocznego znacznika styku pod kątem trafiał w bok jego cienkiego triggera. PhysX zwracał wtedy normalną bocznej krawędzi, a walidator poprawnie odrzucał ją jako niewłaściwą stronę. Ten sam punkt z góry był akceptowany.

Odbiornik nadal wymaga dokładnego collidera zarejestrowanego, dostępnego punktu; oblicza punkt na płaszczyźnie widocznego znacznika i przyjmuje go tylko, gdy promień nadchodzi od przodu i trafia wewnątrz jego rozmiaru. Normalna i punkt podlegają tej samej walidacji punktu styku; walidacja kolizji, ograniczeń części i obsługi ekonomii nie zmienia się.

Unity 6000.3.14f1, EditMode z wejściem do Play Mode:
- `garage_pointer_red.xml`: 1/1 FAIL przed poprawką; komunikat „brak zgodnego punktu styku”. `garage_pointer_diagnostic.log` wskazuje zarejestrowany socket z normalną krawędzi `(1,0,0)` zamiast powierzchni.
- `garage_pointer_alltypes.xml`: 3/3 PASS, exit 0; pancerz, bateria, broń i koło; porównanie tego samego punktu pod kątem, rzeczywisty montaż i zdjęcie, ponowne wskazanie po przebudowie; wskazanie poza widocznym polem nadal odrzucone.
- `garage_pointer_regression.xml`: 12/12 PASS, exit 0; w tym nieautoryzowane podparcie, kolizje, odsłanianie punktu po zdjęciu części i obrót pojazdu. Ten przebieg wykonano przed dodaniem dwóch dodatkowych testów typów, bez dalszych zmian kodu produkcyjnego.
- Pierwsza próba kompilacji poprawki wykryła CS1628; poprawiono przechwycenie parametru i ponowne kompilacje oraz testy przeszły.

Nie przeprowadzono pełnej regresji, testu ręcznego Windows ani nowego buildu. Jest to potwierdzona naprawa jednego odtworzonego powodu; nie jest dowodem, że inne przyczyny podobnego objawu nie istnieją.
