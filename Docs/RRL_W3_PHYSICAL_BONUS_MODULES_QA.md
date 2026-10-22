# RRL-W3 — Fizyczne moduły bonusów 1×2×2 (zależne od RRL-W2)

**Status:** implementacja źródłowa gotowa do niezależnej kompilacji i testów Unity — NOT VERIFIED IN UNITY.
**Podstawa:** istniejąca gałąź `feature/unified-rewards-debris-density` (PR #1). Nie scalać W3 przed pozytywnym zakończeniem W2.

## Reguła produktu

Bonus wybrany po zwycięstwie to **jedna fizyczna część o wymiarach Y=1, X=2, Z=2**, a nie trwałe ulepszenie statystyk postaci. Nagroda:
1. Zajmuje **jeden z maksymalnie trzech slotów** nagród W2.
2. Wpada do magazynu **jako jedna sztuka, nie cztery części**; z własnym ID, HP i opisem.
3. Zajmuje komórki `(x,y,z), (x+1,y,z), (x,y,z+1), (x+1,y,z+1)`; nie można nachodzić na inne części, wyjść poza ograniczenia siatki ani montować w powietrzu.
4. Jest montowana i zdejmowana jednym działaniem; przy montażu jest jeden fizyczny mesh i jeden collider o rozmiarach ok. 1,07×0,52×1,07 m; siatka konstrukcji otrzymuje trzy niewidzialne, bezkolizyjne gniazda — nie stanowią osobnych nagród, części ekwipunku ani celów strzałów.
5. Bonus działa dopiero po **zamontowaniu, połączeniu ze sprawną konstrukcją i przy dodatnim HP**; zniszczenie, odczepienie lub zdjęcie dezaktywuje efekt. Odzyskanie/wymontowanie nie nadaje stałego mnożnika.
6. Kilka zamontowanych modułów kumuluje modyfikatory tylko na czas, w którym każdy z nich funkcjonuje.
7. Naprawa polowa (w starym kodzie efekt o zerowej implementacji) jest teraz pasywnym modułem przywracającym **2,5% HP żywych części na sekundę**, maksymalnie 8%/s przy wielu modułach; działa podczas walki lub poligonu; nie wskrzesza ani nie regeneruje odłączonych bloków.
8. Zniszczony moduł ma HP=0 po walce, nie daje bonusu i wymaga naprawy jak inne części. Nie jest możliwy zakup nowego modułu ze sklepu — tylko zdobycie w nagrodach.

## Zmienione kontrakty

- `Assets/R3/R3VehicleModel.cs`: `R3PartKind.BonusModule` dopisany na końcu enum, w `R3PartState` identyfikator/tytuł/efekt/wielkość bonusu, `ReceiveBonuses`, `OccupiedCells`, czteropolowe `Connected`, walidacja zajętości, naprawa i demontaż jako jednej części.
- `Assets/R3/R3BonusModuleGeometry.cs`: rzeczywista geometria i collider 1×2×2, 3 strukturalne gniazda pomocnicze.
- `Assets/R3/R3MountedBonusEffects.cs`: przełącza efekty w runtime na podstawie stanu żywych modułów, obsługuje naprawę polową.
- `Assets/R3/R3VehicleAssembly.cs` i `R3MountedPart.cs`: tworzą jedną fizyczną bryłę i przekazują definicję statów.
- `Assets/R1/R1RunProgress.cs` i `R1RunController.cs`: wybór nagród nie mutuje `Progress.Stats`; otrzymane moduły trafiają do inventory; wejście do kolejnej walki stosuje efektywne statystyki z żywych modułów.
- `Assets/R3/R3GarageView.cs` i `Assets/R4/R4RunHUD.cs`: czteropolowy podgląd, instalacja/naprawa/zdjęcie, opisy modułów.
- `Assets/Resources/R1/Upgrades/*.asset`: opisy funkcji aktywnych tylko po montażu.

## Wymagane QA w Unity 6000.3.14f1

1. Kompilacja Editor, EditMode, PlayMode oraz Windows build (z rzeczywistymi logami i exit code). Stare testy oczekujące permanentnej modyfikacji statystyk są nieaktualne; dostosować jawnie do nowych reguł.
2. Po zwycięstwie wybierz **jeden** bonus i **dwie** części; licznik 3/3. W garażu bonus jest w magazynie, ale obrażenia/HP pojazdu nie zmieniły się.
3. Spróbuj zamontować bonus na dwóch zajętych polach — odmowa. Spróbuj przy granicy x=6 — odmowa. Poprawny montaż x=1,y=2,z=0 na pojeździe Standard powinien zajmować cztery pola.
4. Sprawdź podgląd z góry i z boku: **jedna płaska bryła 1×2×2**, a nie cztery niezależne bloczki. Jeden pasek HP, jeden wpis magazynu.
5. Zamontuj moduł Damage 25%, uruchom walkę, porównaj obrażenia. Zdejmij w garażu — brak 25%. Zamontuj dwa moduły (stack), sprawdź mnożnik.
6. Zniszcz lub odłącz moduł podczas walki. Statystyki wracają do poziomu innych aktywnych modułów; martwy moduł nie działa. Przejdź do następnej walki — nie odzyskuje efektu bez naprawy.
7. Bonus Hull/Core: aktualizacja maksymalnego HP nie może leczyć zniszczonych bloków ani zwiększać HP przez wielokrotne przebudowy/podłączanie.
8. Naprawa polowa: regeneracja działa wyłącznie na żywych blokach po montażu. Nie regeneruje całkowicie zniszczonych, odłączonych ani w stanie Garage.
9. Wysadź bok pojazdu z modułem bonusowym: integralność P12 musi prawidłowo odłączyć całą bryłę; sprawdzić wyświetlanie, masę, brak błędów konsoli, brak niewidzialnych colliderów i kolizji z pociskami.
10. Testy: `R3PhysicalBonusModuleTests` oraz pełna regresja MOV-001..009 i W2. Nowych testów nie uznawać za wykonane bez wyników Unity.

## Znane ograniczenia

- Przygotowano API i runtime bez uruchomionej sesji Unity w bieżącym środowisku.
- Brak obracania footprintu nie stanowi funkcjonalnej straty (2×2 jest symetryczne w poziomie).
- Moduł naprawy zmienia działanie z niezaimplementowanego efektu jednorazowego na pasywne leczenie aktywne po montażu; ten wybór wymaga potwierdzenia balancingiem.
- Gniazda bez colliderów są konstrukcyjnymi proxy i muszą przejść testy P12 oraz profilu obciążenia R6.
- W3 jest zależne od otwartego PR #1. Nie przenosić W3 automatycznie na starsze `develop`.

**Merge gate: BLOCKED do rzeczywistej kompilacji i gameplay QA przez Codexa/operatora.**
