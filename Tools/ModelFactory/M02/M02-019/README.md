# M02-019 — Hydrauliczny hamulec bębnowy

**MODEL-02 / katalog V1.** Wizualny model do Unity 6, bez zmian gameplayu.

## Mechanika i geometria
Oś koła: **+Y**. Punkt montażowy **(0,0,0)**; **1 jednostka = 1 m**.
Stacjonarna płyta nośna, dwa łukowe segmenty cierne z ceramiczną okładziną, dwa dolne sworznie obrotowe, górny cylinder hydrauliczny z dwoma tłoczkami, sprężyny powrotne, obracający się bęben z ośmioma otwartymi oknami inspekcyjnymi i kołnierz piasty. Osiem materiałów (stal, żeliwo, ceramika, brąz, niebieski lakier hydrauliki, pomarańcz, guma, polerowany wał).

Ruch: bęben 2 obroty / 121 klatek, szczęki +/-1,5 stopnia na osi Y, tłoczki +/-11 mm, sprężyna górna rozciąga się o 7,5%. Zewnętrzny promień okładziny 293 mm, wewnętrzny promień bębna 306 mm. Skrajne pozycje sprawdzane analitycznie w 121 próbkach.

## Uruchomienie
```sh
blender -b -noaudio --python Tools/ModelFactory/M02/M02-019/generate_m02_019.py
python3 Tools/ModelFactory/M02/M02-019/qa_m02_019.py
```
Pliki `.blend`, `_animated.glb` i `_qa.json` generowane do `generated/`. CI publikuje je jako artefakt workflow. Model jest źródłowym assetem wizualnym, nie fizyczną symulacją kontaktu klocków.

## Granice QA
Test obwiedni radialnej nie jest pełnym triangle-triangle CCD. Import do Unity, EditMode, PlayMode i testy pojazdu wymagają oddzielnego uruchomienia; nie wolno uznawać ich za PASS na podstawie eksportu GLB. Nie modyfikowano scen, ProjectSettings ani innych paczek.
