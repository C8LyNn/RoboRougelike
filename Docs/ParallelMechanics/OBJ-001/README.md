# OBJ-001 — obrona reaktora / R1

Issue: https://github.com/C8LyNn/RoboRougelike/issues/9  
PR: https://github.com/C8LyNn/RoboRougelike/pull/14  
Unity: **6000.3.14f1**.

## Zakres
Izolowany komponent `ReactorDefenseObjective`: HP, limit czasu, stany Idle/Defending/Won/Lost, zdarzenia ataku i naprawy, reset, walidacja NaN/Infinity i dodatnich wartości, limit napraw, bezpieczny stan terminalny i deterministyczny zegar. Osobna niezapisana scena demonstracyjna dostępna przez menu Tools/RoboRougelike/OBJ-001/Open Isolated Reactor Demo.

## Granice
Wyłącznie `Assets/RoboRougelikeMechanics/OBJ001DefendReactor/**` i `Docs/ParallelMechanics/OBJ-001/**`. Bez edycji sceny głównej, R1/R3/R4/R6, AI, nagród, ruchu, spawnera, istniejących prefabów ani plików Codexa. Pakiet NIE jest automatycznie zintegrowany z Gauntlet.

## QA
Dodano **11 EditMode + 3 PlayMode**. Testy nie zostały wykonane w Unity; **NOT RUN**. CI-01 to kontrola struktury, nie dowód działania gameplay. Raport: `QA_R1.md`. Przed integracją wymagane uruchomienie testów Unity 6000.3.14f1 i ocena kolizji z PR #1.
