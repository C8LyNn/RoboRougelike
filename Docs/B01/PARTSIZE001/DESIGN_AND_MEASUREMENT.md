# PARTSIZE-001 — rzeczywiste gabaryty / kontrakt przed integracją
Status: **PROPOSAL / MEASUREMENT_PENDING**, issue #141, 2026-10-25. Równoległe planowanie, nie ukończona paczka Unity.

## Fakty z repozytorium
R3/R41 używa rozstawu siatki 0,55 m i typowego bloku fizycznego 0,52 m. R3VehicleModel.OccupiedCells zwykle zajmuje jedną komórkę; BonusModule i niektóre specjalne moduły zajmują cztery komórki 2×1×2 (osie mogą się obracać XYZ), lecz mają jednego właściciela i HP. R3MountingGrid używa masek sześciu ścian. Nie wolno automatycznie skopiować maski na artystyczną siatkę modelu.

Wersjonowany rejestr: `size_contract.proposed.json` — 14 typów i kandydatów, wymagane lokalne osie, komórki logiczne, punkty mocowania, funkcjonalna strefa wykluczenia oraz **null zamiast zmyślonych pomiarów** mesh/collider/mass/HP. Nowe rodzaje ramp i jump padów są propozycjami, nie istniejącymi enumami R3.

## Zasady
1. Logic cell footprint ≠ Mesh AABB ≠ Collider AABB ≠ mount sockets ≠ clearance sweep. Te pięć warstw mierzyć i testować osobno.
2. Nie skalować globalnie istniejących kół/pojazdów, bo #125 oblicza faktyczny kontakt bieżnika z podłożem.
3. Dla każdej części odnotować: rzeczywisty prefab GUID, import scale, `Renderer.bounds` w WORLD, `Collider.bounds` w WORLD i lokalnych osiach, `Rigidbody.mass`, HP owner, sąsiadów, orientację 0/90/180/270 XYZ i mapę save/inventory.
4. Dla broni zmierzyć zamaszysty obrót yaw/pitch, muzzle i strefę wolną do raycast. Dla kół promień i boczną piastę, nie oponę. Dla pancerza kontaktowy obszar ściany, nie tylko prostokąt obrysu renderu.
5. Nowe klasy wymiarowe tylko po danych: SMALL 1×1×1, MEDIUM 1×2×1/2×1×2, LARGE 2×2×2 /2×3×2. To **kandydaci**, nie migrować starych pojazdów bez jawnej zgody.

## Prace B01-10 / B01-40
B01-10: ustalić co najmniej 14 wpisów i próbkę wyposażenia Light/Heavy; jakościowe kompromisy masa/HP/prześwit. B01-40: pobrać realne rozmiary prefabu/skali z PR #19, PR #125 i assetów M01–M05, porównać w checkerze na celowym SHA, zebrać brakujące binaria. Żaden brak danych nie dostaje arbitralnego PASS.
B01-20: po zamknięciu BUILD001 G0 kodowy fixture rozmiarów w osobnym Draft PR albo osobnym zatwierdzonym etapie, nie wprowadzać zmian współdzielonych bez Codex ACK.
B01-60: sprawdzić 90° X/Y/Z, multi-cell owner HP, hitbox/contact i save/migration w licencjonowanym Unity; zapisać XML, zrzuty, exit.

## Zależności
BUILD001 #140 (typed sockets) → FIRE001 #142 (weapon sectors) i ARMOR001 #143 (geometria/mount patch). Pakiet #141 może przygotować schema wcześniej, ale **G2 source-ready dopiero po realnych testach**.
