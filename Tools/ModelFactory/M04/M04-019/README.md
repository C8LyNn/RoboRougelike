# M04-019 — Turbina z rozkładanymi segmentami

Model wizualny do Unity 6 RoboRougelike; bez zmian mechaniki, scen i ustawień projektu. Pozycja z katalogu `feature/rrl-model-factory-queue-v1`, kolejka M04.

## Konstrukcja

Turbina kanałowa z **siedmioma profilowanymi łopatkami** i **sześcioma łuskowymi panelami osłony** rozkładanymi na zawiasach stycznych. Stały kanał ma otwór o promieniu 0,274 m, wirnik promień końcówki do 0,240 m; zewnętrzna osłona ma wewnętrzny promień 0,385 m. Rozkładanie odsuwa tylne krawędzie paneli od kanału zamiast przesuwać je do środka.

Osiem materiałów: grafit, tytan, ceramika, miedź, guma, ciemna stal, cyjanowe i bursztynowe elementy emisyjne. Akcenty emisyjne nie zastępują geometrii.

**Układ odniesienia:** 1 jednostka = 1 metr. Blender Z-up; eksport GLB przenosi orientację do Unity. Płaszczyzna bocznego montażu **X=0**, zewnętrzna oś turbiny **+X**, korzeń sceny `SIDE_MOUNT_X0_ORIGIN` w (0,0,0). Wirnik obraca się wokół lokalnej osi X o 360° w 121 klatkach (30 FPS), a sześć osłon otwiera się **0°→30°→0°** wokół własnych osi stycznych Z. Zawiasy są na obwodzie obudowy, a nie na górnej powierzchni.

## Generowanie i walidacja

```bash
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M04/M04-019/generate_m04_019.py -- --out build/M04-019
python3 Tools/ModelFactory/M04/M04-019/qa_m04_019.py --out build/M04-019
```

Generator zapisuje `M04-019_segment_turbine.blend`, animowany `.glb` i manifest `_manifest.json`. Walidator weryfikuje rzeczywisty plik GLB, obecność kanałów obrotu wszystkich siedmiu pivotów, materiałów i siatek. Generowanie jest powtarzalne: przy każdym wywołaniu usuwana jest poprzednia scena, więc nie mnożą się obiekty.

## Granice QA

Generator próbuje 41 pozycji animacji, sprawdza promieniowy odstęp łopatek od kanału, paneli od stałego korpusu oraz odstęp kątowy sąsiednich paneli na podstawie transformowanych wierzchołków. Są to konserwatywne **testy obwiedni w próbkowanych klatkach**, nie pełne ciągłe testy trójkąt–trójkąt. Ręczna ocena wyglądu, import, URP, EditMode/PlayMode i fizyka Unity wymagają osobnego uruchomienia; nie wolno deklarować PASS bez dowodu.

PR pozostaje DRAFT do `develop`, bez merge.
