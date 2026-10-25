# M03-015 — Klatka ochronna generatora

Modularna klatka generatora do Unity 6 RoboRougelike. **Tylko model wizualny**, bez implementacji generatora i bez zmian istniejących mechanik.

- Jednostki: 1 Blender unit = 1 m; +Z w Blenderze, glTF eksportuje +Y-up.
- Wymiary nominalne: 1,12 m szerokości, 0,86 m głębokości, 1,04 m wysokości.
- Pivot całego modułu: (0, 0, 0) na spodzie ramy. Cztery punkty \`SNAP_INNER_00..03\` na płaszczyźnie z=0; osie mocowania -Z.
- Zawias klapy: (-0,50, +0,475, 0), oś +Z. Otwarcie na zewnątrz 0→100°.
- Dwa niezależne zatrzaski: obrót wokół lokalnej osi X, 0→65° przed otwarciem drzwi. Klatki 1/12/48/60/96/108.
- Materiały: grafitowa stal, szczotkowane pręty, ceramika, miedź, elastomer i bursztynowe oznaczenia.
- Osłona składa się z kratownic bocznych, tylnej, dachowej oraz otwieranej przedniej klapy.
- Połączenia prętów z ramą są celowymi stykami spawanymi. Poszczególne siatki są zamknięte, bez wewnętrznych samoprzecięć. Przednia klapa otwiera się wyłącznie na zewnątrz; kontrola 101 położeń.
- Zatrzaski są animacją wizualną, nie fizycznym układem blokującym. Brak kolizji runtime/prefabu dopóki nie zostanie przetestowany w Unity.

Uruchomienie poza Blenderem: \`python3 generate_m03_015.py --out /tmp/m03-015-static\` (OBJ/MTL zamkniętej klapy). Test: \`python3 qa_m03_015.py\`.

Uruchomienie w Blenderze: \`blender -b --factory-startup --python-exit-code 1 --python generate_m03_015.py -- --out /tmp/m03-015-blender\`. Eksport GLB/FBX/BLEND jest rzeczywisty tylko po wykonaniu tego polecenia. Test powtarzalności: \`blender -b --factory-startup --python-exit-code 1 --python qa_blender_m03_015.py\`.

**Unity EditMode/PlayMode: NOT RUN** do czasu niezależnego uruchomienia w silniku. Brak zmian w Assets, ProjectSettings i scenach.
