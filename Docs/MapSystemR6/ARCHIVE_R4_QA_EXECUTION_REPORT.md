# MAP-SYSTEM R4 — faktycznie wykonane QA

Wyniki dotyczą wyłącznie skryptów uruchomionych w środowisku Python/Linux. Kompilacja C#, Unity Test Runner, fizyka PhysX, UI i Windows build **nie były uruchomione**.

## R3 testy jednostkowe (8)

- Exit code: **0**

```text
test_dangling_edge_rejected (test_validation.ScenarioGraphValidationTests.test_dangling_edge_rejected) ... ok
test_escape_route_after_bridge_failure (test_validation.ScenarioGraphValidationTests.test_escape_route_after_bridge_failure) ... ok
test_four_modes_are_supported_on_one_map (test_validation.ScenarioGraphValidationTests.test_four_modes_are_supported_on_one_map) ... ok
test_missing_anchor_type_rejected (test_validation.ScenarioGraphValidationTests.test_missing_anchor_type_rejected) ... ok
test_oversized_vehicle_rejected (test_validation.ScenarioGraphValidationTests.test_oversized_vehicle_rejected) ... ok
test_route_unavailable_when_three_branches_fail (test_validation.ScenarioGraphValidationTests.test_route_unavailable_when_three_branches_fail) ... ok
test_structure_is_clean (test_validation.ScenarioGraphValidationTests.test_structure_is_clean) ... ok
test_unknown_state_rejected (test_validation.ScenarioGraphValidationTests.test_unknown_state_rejected) ... ok

----------------------------------------------------------------------
Ran 8 tests in 0.001s

OK
```

## R3 przykładowe scenariusze

- Exit code: **0**

```text
PASS race_intact: valid
PASS survival_intact: valid
PASS escape_collapsed: valid
PASS boss_intact: valid
PASS escape_softlock: rejected [Unreachable path spawn_west -> exit_east in all_eastern_routes_closed for standard_ref]
PASS escape_oversized_vehicle: rejected [Unreachable path spawn_west -> exit_east in intact for oversized_ref]
PASS race_unknown_state: rejected [Unknown state not_a_state]
PASS unknown_challenge: rejected [Unknown challenge]
```

## R3/R4 zgodność danych

- Exit code: **0**

```text
PASS: R3-to-R4 logical graph conversion unchanged (nodes, edges, anchors, states, connectivity metadata)
```

## R4 geometria, winding i nachylenia

- Exit code: **0**

```text
R4 blockout geometry: nodes=7, roads=8, hard_errors=0, warnings=3
WARN: Metadata slope mismatch junction_upper: geometry=3.0 deg, declared=12.0 deg
WARN: Metadata slope mismatch junction_lower: geometry=0.0 deg, declared=8.0 deg
WARN: Metadata slope mismatch lower_exit: geometry=0.0 deg, declared=8.0 deg
RESULT: PASS (geometry metadata / winding only; NOT Unity physics)
```

## R4 struktura kodu i Unity meta

- Exit code: **0**

```text
PASS static Unity source layout: 21 source/assets, 28 unique .meta GUIDs, 4 assembly definitions
```

## Znane ograniczenia

- Trzy ostrzeżenia o niezgodności nachyleń (`junction_upper`, `junction_lower`, `lower_exit`): dane R3 są wartościami referencyjnymi, nie zmierzoną geometrią.
- W mapie blockoutowej na skrzyżowaniach odcinki są prostymi pasami, a nie gotowymi wyprofilowanymi jezdniami; wymaga to przeglądu topologii fizycznej.
- Test `Physics.CheckBox` wymaga przypisania właściwej maski warstw przeszkód i nie ocenia prowadzenia pojazdu.
- Survival i Boss posiadają tylko testowe warunki ukończenia, bez systemu wrogów.
- Złącza zapewniają wyrównanie prefabów, ale nie sprawdzają fizycznej kolizji po połączeniu.
- Gra produkcyjna i repozytorium GitHub nie zostały zmodyfikowane.

## Ocena

**Offline QA: PASS**
**Unity runtime QA: NOT RUN — blokada akceptacji pełnego R4.**
