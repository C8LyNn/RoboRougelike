# MAP-SYSTEM R5 — raport wykonanej walidacji

## Status

- **G0: PASS w zakresie offline** — struktura, schemat danych, Unity `.meta` i asmdef bez błędów wykrytych przez skrypty statyczne.
- **G1a: PASS w zakresie offline** — matematyka mapy, metadane nachyleń, logiczna łączność i krzywizny sample Quarry03.
- **G1b: NOT RUN w Unity** — kod `MapR5TraversalProbe` i testy PlayMode są przygotowane, lecz wymagają PhysicsScene / Unity.
- **G1c fizyczna jazda: NOT IMPLEMENTED** — brak automatycznego kierowcy pojazdu, kontroli przechyłu, kontaktu zawieszenia i stabilności manewrów.
- **G2 scenariusze: PARTIAL** — cztery sandboxowe warunki z R4; Survival i Boss nie posiadają produkcyjnego AI/spawnera.
- **Windows/standalone build: NOT RUN**.

## Wykonane kontrole

Pełny zapis z exit codes znajduje się w `R5_QA_OFFLINE_LOG.txt`.

| Kontrola | Wynik |
|---|---|
| R3↔R5 zgodność topologii i niezmienionych danych | PASS, exit 0 |
| R5 krzywe, nachylenia, minimalne promienie, reachable routes | PASS, exit 0 |
| R5 geometria zamkniętych siatek (indeksy i winding) | PASS, exit 0 — 8 siatek / 2080 trójkątów |
| R5 definicje plików C#, asmdef, unikalne meta GUID | PASS, exit 0 |
| R5 lokalne testy Python | **12/12 PASS**, exit 0 |
| R3 niezmienione testy Python | **8/8 PASS**, exit 0 |
| R3 przykładowe scenariusze | 8/8 zgodne z oczekiwaniem, exit 0 |
| Unity EditMode | **NOT RUN** |
| Unity PlayMode | **NOT RUN** |
| Kompilacja C# / build Windows | **NOT RUN** |

## Parametry nowego przykładowego układu

- `junction_upper`: maksymalne nachylenie ~4,11°, promień minimalny ~12,9 m.
- `bridge_exit`: maksymalne nachylenie ~2,87°, promień minimalny ~23,6 m.
- `junction_lower`: nachylenie 0°, promień minimalny ~27,9 m po korekcie.
- Pojazd referencyjny: szerokość 2,6 m, promień skrętu 4,5 m, wysokość 2,0 m (wartości demonstracyjne, niezmierzone z produkcyjnych prefabów).
- Zachowano: 7 węzłów, 8 połączeń, 3 stany mapy i 4 tryby R4.

## Znane ryzyka produkcyjne

1. Połączenia na padach mogą być geometrycznie przejezdne, ale mieć gwałtowne zmiany kierunku.
2. G1a nie zna rzeczywistego kształtu nierównomiernej konstrukcji pojazdu; reprezentuje ją profilem ograniczeń.
3. G1b przy nieprawidłowo ustawionej masce warstw może ignorować przeszkody albo zgłaszać drogi jako przeszkody. Przy masce `0` zwraca `InvalidConfiguration`.
4. Wyliczony promień krzywej nie oznacza, że rzeczywiste sterowanie pod obciążeniem wykona taki łuk.
5. Brak kalibracji fizyki, testów wydajności dużej liczby instancji map oraz integracji z VFX/MOV.
6. `MeshCollider` należy utrzymywać jako static collider; nie próbować używać tego niekonweksowego mesha jako dynamicznego Rigidbody.

## Zalecenie

**ACCEPT AS OFFLINE RESEARCH/PROTOTYPE ONLY.** Następna bramka wymaga rzeczywistego projektu Unity i testów przejazdu przed deklaracją gotowości do integracji z Gauntletem.
