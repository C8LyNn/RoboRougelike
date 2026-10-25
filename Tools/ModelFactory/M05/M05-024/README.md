# M05-024 — System chłodzenia kriogenicznego

**Katalog:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`.  
**Claim:** `Docs/ModelFactory/claims/M05-024.json`, atomowy CREATE commit `50bcc70fa3944fd09ee8852da5a0d997cbed5b4d`, 2026-10-25T02:53:33.272Z.

## Zakres

Wyłącznie model wizualny: przemysłowy moduł chłodzenia kriogenicznego montowany wewnątrz ramy robota. Dwa przeciwfazowo pracujące tłoki w pustych cylindrach, błękitne rury obiegu, radiator z piętnastoma lamelami, miedziany blok zaworowy, grafitowa podstawa, ceramiczne panele i diody statusowe. **Nie implementuje** chłodzenia, energii, gameplayu, temperatury ani czujników.

- **Jednostki:** 1 Blender unit = 1 m; Blender Z-up; przy imporcie Unity sprawdzić orientację.
- **Główny pivot mocowania:** `M05_024_MOUNT_ROOT` = (0,0,0), środek spodu podstawy.
- **Środki cylindrów:** X = ±0,35 m, Y = 0; Z = 0,39–1,10 m.
- **Ruch:** osobne puste pivoty `L_PISTON_PIVOT` i `R_PISTON_PIVOT` przy (0,0,0), translacja po lokalnej osi +Z. Klatki 1/31/61, 30 FPS: lewy 0/+0,10/0 m, prawy 0/−0,10/0 m. Każdy tłok ma własną akcję.
- **Geometria:** otwory cylindrów mają promień 0,108 m, pręty 0,076 m; teoretyczny luz promieniowy 0,032 m. Zewnętrzne obudowy nie przecinają się. Minimalna przerwa między górną głowicą a korpusem wynosi co najmniej 0,2775 m.
- **Markery:** MOUNT_ROOT, cztery MOUNT_BOLT, FLOW_IN, FLOW_OUT, L/R_SERVICE_PORT, L/R_PISTON_AXIS.
- **Materiały:** graphite, titanium, ceramic, copper, rubber, ice_blue, cyan_led, amber_led.

## Reprodukcja i QA

```sh
python3 -m py_compile Tools/ModelFactory/M05/M05-024/*.py
blender -b -t 2 --python Tools/ModelFactory/M05/M05-024/generate_m05_024.py -- --out /tmp/m05-024 --self-test
python3 Tools/ModelFactory/M05/M05-024/validate_m05_024.py /tmp/m05-024
```

Generator usuwa wyłącznie własną kolekcję i osierocone własne materiały/akcje. Tryb self-test wykonuje dwa generowania, weryfikuje stałość liczby obiektów i zachowanie obcego obiektu. Blender sprawdza manifold wszystkich siatek i 122 próbki kinematyczne, zapisuje GLB, BLEND, opcjonalny FBX i raport JSON. Walidator niezależnie czyta GLB.

**Ograniczenie QA:** testy prześwitów są analityczne, nie są pełnym triangle-pair collision sweep. Rzeczywisty import Unity 6, URP i PlayMode muszą zostać wykonane osobno. Bez potwierdzonego uruchomienia CI nie deklarować PASS.

**Izolacja:** gałąź od `develop`, Draft PR do `develop`. Nie modyfikować scen, ProjectSettings, istniejących assetów ani mechanik.
