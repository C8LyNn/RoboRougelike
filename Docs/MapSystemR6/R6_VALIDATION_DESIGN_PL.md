# MAP-SYSTEM R6 — bramki walidacyjne i kolejne kroki

## G0 — definicje i assety
- unikalne GUID `.meta`, poprawne nazwy asmdef, kompletne definicje JSON;
- spójność R3–R6 topologii; wersja mapy `6` i poprawka lower_road.padWidth;
- poprawność plików testowych. **Nie oznacza zgodności z kompilatorem C#.**

## G1a+ — geometria tras z uwzględnieniem manewrów
- `MapR6JunctionPlanner` sprawdza zorientowany łuk pomiędzy dwiema krawędziami spotykającymi się w węźle;
- `MapR6RouteValidator` sprawdza cały przebieg sekwencji krawędzi lub wyszukuje trasę przez BFS w stanach `(node, incomingEdge)`;
- rysowana ścieżka kandydata w Scene View nie jest dodatkowym fizycznym colliderem;
- nie jest jeszcze sprawdzane pełne pokrycie powierzchni pod całym pojazdem i styk wielu MeshColliderów;
- sama minimalna krzywizna nie wystarcza, by udowodnić poprawny skręt pod obciążeniem.

## G1b — kolizje
- `MapR6JunctionClearanceProbe` używa `MapR5TraversalProbe` na wirtualnym łuku skrętu;
- fizyka wymaga poprawnej maski przeszkód — maska 0 zwraca `InvalidConfiguration`;
- domyślnie wykluczamy nawierzchnię, pojazd i obiekty triggerowe;
- wynik `Clear` oznacza tylko brak wykrycia przeszkód w próbkowaniu — nie dowodzi, że pojazd przejedzie.

## G1c — fizyczny przejazd
- `MapR6DriveEvidenceRecorder` pobiera próbki pozycji/obrotu prawdziwego Rigidbody i zadeklarowany stopień żądania ruchu;
- klasyfikuje długi brak postępu przy żądanym ruchu jako podejrzenie utknięcia, nie automatyczny błąd geometrii;
- eksport dowodów JSON jest ręczny; nie przyznaje samodzielnie PASS;
- osobno wymagane: start i meta, przejazd przez węzły, pomiar kontaktu podłoża, ocena osi/sterowania, próba dla szerokiej i ciężkiej konstrukcji.

## Kryteria niezależnego QA (do kalibracji w Unity)
- **PASS** tylko po wykonaniu całej wymaganej trasy w realnym projekcie bez przejścia przez collidery i bez utraty zdolności do jej ukończenia;
- **FAIL** w razie stałej blokady, nieuniknionego zakleszczenia, błędnej topologii lub uszkodzonej logiki celów;
- **INCONCLUSIVE** gdy kierowca testowy, maski warstw lub instrumentacja nie działają wiarygodnie;
- **NOT RUN** jeśli brak Unity, builda lub logów.

## Znane ryzyka
1. Powierzchnie R5 oraz node pads są nadal osobnymi colliderami, bez fizycznie zszytych skrzyżowań.
2. Skręt może przebiegać po łuku geometrycznym, lecz poza podpartą nawierzchnią — potrzebny test obszaru podparcia, nie tylko sprawdzenie wolnej przestrzeni.
3. Zjawiska poślizgu, asymetrii, uszkodzeń kół i kontaktów z podłożem wymagają faktycznego symulatora.
4. Duża liczba debugowanych krawędzi obciąża Scene View; nie rysować wszystkich planów w każdej klatce.
5. Dodatkowa walidacja może odrzucać krótkie manewry, które dobry kierowca wykona przez cofanie — MVP nie planuje ruchu wieloetapowego.
