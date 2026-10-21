# MAP-SYSTEM R4 — Implementacja i dalszy projekt

## Zasada zakresu
Pierwsze prawdziwe geometryczne blockouty dróg, placów i ramp powstają ze struktury grafowej R3. Nie dotykamy istniejącego gameplayu.

## Koordynaty
Pozycje w `MAP_R4_quarry_03.json` są w jednostkach Unity: X/Z poziom, Y wysokość. **Skala hipotetyczna**. Geometria dróg powstaje jako płaski pas (quad) na wysokościach centrów węzłów. Nie generuje łuków i idealnych zakrętów; styk ukośnych pasów może wymagać dodatków.

## Przejezdność
Walidator `MapR4GraphValidator` sprawdza: wymagane poziomy przejazdu, profil rozmiarów, promień manewru deklarowany w krawędzi, deklarowaną maksymalną stromiznę i wymagane capability. Każde takie kryterium jest wyłącznie logicznym filtrem.

`declaredSlopeDeg` jest metadanymi przeniesionymi z R3. Rzeczywisty kąt nachylenia siatki wynika z delty Y i odległości XZ. DANE MOGĄ SIĘ RÓŻNIĆ! Przed użyciem gry produkcyjnej wymagana jest kontrola różnic i odpowiedni sposób kalibracji.

## Złącza prefabów
`MapR4ModuleSocket` umieść na dziecku prefab-root. Forward pokazuje na zewnątrz prefab-u. Złącza mogą się połączyć, gdy wysokość/szerokość są zgodne. `MapR4SocketPlacement.TryAlign()` obraca/przesuwa instancję do socketu docelowego. Nie sprawdza kolizji ani obsługi zakrętów; potrzebny moduł Collider Clearance Validator.

## Stan mapy
`MapR4StateController.TrySetState()` zmienia aktywność konkretnych segmentów drogi. Logiczny G1a i stan aktywności obiektów muszą być testowane łącznie. Scena nie usuwa padów na końcu drogi, gdy most ulegnie zniszczeniu.

## Wyzwania
- Time Attack: checkpoint_upper -> finish_east; czas maksymalny 180 s.
- Escape: most jako nieprzejezdny od początku; dotarcie do exit_east.
- Survival: odliczanie 30 s **bez generowania wrogów**.
- Boss: **brak bossa w paczce**; zwycięstwo wyłącznie po `NotifyBossDefeated()`.
To demonstracja uniwersalnego przepływu zdarzeń, nie kompletne wersje trybów.

## Ograniczenia Input System
`MapR4PrototypeDriver` korzysta z legacy `Input.GetAxisRaw`. Projekty ustawione wyłącznie na New Input System mogą wymagać `Active Input Handling = Both` w ustawieniach Player albo zastąpienia sterownika nowym adapterem.

## Następny R5
1. Kalibracja realnych rozmiarów pojazdów i definicja grup referencyjnych.
2. Generator krzywych/korytarzy geometrycznych + lokalne obroty złączy.
3. Static PhysicsScene BoxCast/OverlapBox dla obwiedni.
4. Actual-drive PlayMode scena — nagrywanie rezultatów przebiegów.
5. Prawdziwe falowe AI, boss i reward system przez adaptery.
6. Dynamiczna aktualizacja tras po zmianie geometrii.
7. Profile wydajności sceny i zniszczalnych przeszkód.

## Wczesna geometria G1b
Dodano `MapR4ClearanceProbe.CheckStraightCorridor()`, która próbuje wykryć kolizje na zadanej warstwie przeszkód za pomocą `Physics.CheckBox`. Wymaga jawnego `obstacleLayerMask`; `0` oznacza brak konfiguracji i skutkuje wynikiem negatywnym. Warstw dróg i podłoża **nie wolno** dodawać do tej maski. To wstępna próba obwiedni, nie symulacja jazdy i nie test promienia skrętu.

Przykład poziomu może mieć przejścia krzyżujące się w projekcji XZ i brak fizycznych blokad na krawędzi areny. Takie przecinanie może stworzyć fizyczny skrót niewystępujący w grafie logicznym. Bez kontroli topologii geometrycznej i testu jazdy nie można oznaczyć G1b jako final PASS.

Uruchom `python Tools/check_r4_geometry.py`, aby wykryć niezgodność rzeczywistych kątów nachylenia z metadanymi R3. Test G1a porównuje obecnie **oba** warunki: deklarowane i wyliczane nachylenie; bardziej rygorystyczne ograniczenie wygrywa.
