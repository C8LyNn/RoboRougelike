# RRL-W4 — Oddzielne nagrody, punkty styku i obracanie części

**Status:** IMPLEMENTED SOURCE / UNITY QA NOT RUN / DRAFT.
**Zależność:** PR #1 (nagrody i złom) → PR #17 (fizyczne moduły 1×2×2) → ten W4.
**Bezpieczeństwo:** bez automatycznego merge do develop/main. Nie nadpisywać aktywnego worktree Codexa.

## Reguły nagród

Każde zwycięstwo udostępnia DWA niezależne wybory na jednym ekranie:

- **Dokładnie 1 z 3 bonusów.** Bonus jest fizycznym modułem 1×2×2, zapisuje się w magazynie i działa dopiero po montażu.
- **Od 0 do 3 części pokonanych przeciwników**, niezależnie od jednego obowiązkowego bonusu.
- Możliwa suma to 1, 2, 3 albo 4 przedmioty. Jeden bonus NIE ZAJMUJE limitu odzysku części.
- Złom za niewybrane części zgodnie z `R4SalvageLedger`; premia złomu za walkę pozostaje bez zmian.
- W także ostatnim starciu oferowane są trzy moduły, choć po wygranej wyprawa się kończy (testować ekran końca).

Przyciski i liczniki oddzielone: `MODUŁ: 1/1` i `CZĘŚCI: n/3`. Potwierdzenie jest nieaktywne do wybrania modułu. Ostateczną walidację robi też `R1RunController` i `R1RunProgress`.

## Siatka i orientacja

Przyszłościowa forma bazuje na częściach zajmujących zbiór komórek, a nie jednym world-space boxie. Aktualne proste klocki pozostają 1×1×1. Moduł bonusowy jest 1×2×2, jeden logical item / jeden mesh / jeden fizyczny HP/collider. Lokalny kształt wyznaczony przez offsety `(0,0,0),(1,0,0),(0,0,1),(1,0,1)`.

- Kąty 0/90/180/270° wokół osi X, Y, Z; wszystkie trzy osie działają dla części magazynowych i kupowanych.
- `RotationQuarterTurns` jest serializowaną częścią `R3PartState` i przenosi się przez Copy / DeepCopy / magazyn / montaż / demontaż.
- `R3MountingGrid.Rotate` tworzy deterministyczne całkowitoliczbowe offsety; obrót pionowy zmienia footprint w pionowy blok 2×2×1.
- Garaż: **Q/E** obrót wokół Y, **R/F** wokół X, **Z/X** wokół Z (w lewo / w prawo według klawisza). Dodatkowe trzy przyciski obracania o +90° i podgląd X/Y/Z.
- Bez rotacji ciągłej, bez asymetrycznych skal oraz bez zmiany identyfikatora przy obrocie.
- Kliknięcie dowolnego pola istniejącego wielokomórkowego bonusu umożliwia naprawę i demontaż CAŁOŚCI. Przy ustawianiu nowego bonusu podgląd proponuje dostępną pozycję po obróceniu.

## Punkty styku (inspiracja systemami voxel builderów)

Każda zajmowana komórka może mieć do sześciu portów w środku swoich ścian: ±X, ±Y, ±Z. Maskę `ContactMask` przechowujemy w LOKALNYM układzie części. Obrót przenosi maskę do świata; port na ścianie lewej części łączy się tylko z portem na PRZECIWLEGŁEJ ścianie części sąsiedniej.

- Brak zgodnego kontaktu z żywą konstrukcją oznacza brak montażu (nawet jeżeli odległość geometryczna jest mała).
- Kolizja obrysu, wyjście poza siatkę i przekroczenie limitu zajętych pól blokują montaż.
- Własne komórki tego samego wielokomórkowego modułu zawsze stanowią jedną bryłę; ich wewnętrzne połączenia NIE wymagają portów.
- Bazowe części mają maskę `111111` (sześć portów) — dzięki temu istniejący design gra poprawnie, a bardziej złożone kształty mogą otrzymywać maski ograniczone.
- `P12PipelineSafeVehicleStructure` używa tej samej walidacji par portów przy flood-fill od rdzenia i przy dzieleniu odłączonych fragmentów. Pasywne gniazda bonusu reprezentują komórki konstrukcyjne BEZ dodatkowych colliderów i HP. Zniszczenie modułu może więc powodować odłączenie innych części pojazdu.
- W ghost-preview widoczne są małe niekolizyjne znaczniki punktów mocowania w każdej odsłoniętej ścianie.

## Wymagane testy Unity

1. Unity 6000.3.14f1: kompilacja, pełny EditMode/PlayMode, Windows build. Przeciążony/nieaktywny CI/Unity activation = NOT RUN, nie PASS.
2. Zwycięstwo: wybierz jeden bonus i dokładnie 3 różne części. Cztery przedmioty trafiają do magazynu; bonus nie zmniejsza liczby części. Wybór 2 bonusów lub 4 części musi być odrzucony.
3. Nie można zatwierdzić ekranu zwycięstwa bez bonusu. Powtórz na finałowym bossie; ma być wyświetlona oferta trzech bonusów, a po potwierdzeniu stan Won.
4. W garażu sprawdź 2×2×1 w poziomie, następnie pionowy moduł 2×2×1 po obrocie o X/Z. Jeden przedmiot, jeden collider, jeden wspólny HP.
5. Sprawdź 6 kierunków punktów styku — po obróceniu klocka/kontaktu maska przechodzi na nową ścianę. Włącz maskę tylko z jednej strony w teście, odrzucaj niewłaściwą ścianę.
6. Umieść moduł w środku pancerza tak, żeby dotykał innych klocków; sprawdź `R3VehicleModel.Connected` i odłączanie fragmentów przez graf P12 po zniszczeniu mostu z portów.
7. Każda część po kupieniu/zdobyciu ma być obrotowa w garage; ponowne zdjęcie i montaż zachowują jej rotację w magazynie, dopóki gracz nie zmieni obrotu w podglądzie.
8. Testuj strzelanie w obrócony fizyczny bonus (każde z czterech pól trafia to samo HP), dezaktywację bonusu po 0 HP / detach i fizykę odpadniętych fragmentów.
9. Dla normalnego pojazdu bez zmienionych masek brak regresji: ta sama łączność pancerza i kół, działają MOV001–009.
10. Sprawdź kamera, podgląd portów, uszkodzone ghosty, przewijanie magazynu, grę w 1280×720, kliknięcie UI bez zmiany orientacji.
11. `Assets/Editor/R3ContactAndRewardTests.cs` plus `R3PhysicalBonusModuleTests.cs`: raport rzeczywistych wyników, nie samych plików.

## Znane ograniczenia i decyzje

- **To system PUNKTÓW KONTAKTU, a nie fizycznych zawiasów/weld joint.** Gra ma jeden Rigidbody pojazdu, a połączenia sprawdzane są na siatce. Szeroki katalog części z kształtami nieregularnymi i portami częściowymi będzie rozszerzeniem na tej bazie.
- Automatyczna pozycja podglądu wybiera pierwszy legalny anchor sąsiadujący z klikniętą ścianą. Docelowo można dodać dokładniejsze przesuwanie ghosta/wybór konkretnego portu.
- Nie dodano jeszcze łączenia wielopunktowego z pojemnością/siłą każdego portu, ani zmiany siły trzymania w zależności od HP/siły klejenia; wymagają balansu.
- Obecne modele 3D to proste bryły/proxy. W dalszej wersji poszczególne części mają otrzymać skomplikowane wizualia bez zmiany abstrakcji kształtu/portów.
- Konflikty logiczne z istniejącymi PR-ami i lokalnymi zmianami Codexa mogą wymagać rozwiązywania przy integracji, nawet jeżeli praca jest izolowana w osobnej gałęzi.

**Nie scalać do develop/main bez potwierdzonych testów kompilacji, rozgrywki i P12.**
