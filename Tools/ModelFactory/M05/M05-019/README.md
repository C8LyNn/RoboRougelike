# M05-019 — Projektor holograficzny wizualny

Źródło: katalog Model Factory V1, kolejka M05. Model wizualny bez nowej mechaniki sensora.

## Konstrukcja
- Montaż: górne gniazdo, pivot M05_019_MOUNT_TOP = (0,0,0), 1 Blender unit = 1 m.
- Cztery ceramiczne panele rozsuwają się promieniowo po prowadnicach: r=0.27 -> 0.40 -> 0.27 m.
- Animacja: klatki 1/25/49, 30 FPS, interpolacja liniowa, niezależne pivoty PANEL_PIVOT_0..3.
- Oś projekcji: lokalne +Z. Stożek półprzezroczysty, cztery pierścienie i centralna soczewka.
- Materiały: grafit, tytan, ceramika, miedź, guma, cyjanowa emisja, bursztynowa emisja, hologram.
- Markery: MOUNT_TOP, POWER_INPUT, DATA_INPUT, PROJECTION_ORIGIN i cztery PANEL_PIVOT.

## Uruchomienie
blender -b -noaudio --python Tools/ModelFactory/M05/M05-019/generate.py -- --output /tmp/m05-019

Generator tworzy GLB, BLEND, opcjonalnie FBX oraz raport QA JSON. Powtarzalne generowanie nie usuwa cudzych obiektów.

## QA
- Wbudowane: >=3 materiały, kontrola krawędzi manifold w bmesh, 121 próbek ruchu, separacja rdzenia i stożka, klucze animacji, dwukrotne wykonanie.
- Rozstaw paneli sprawdzony analitycznie, nie jest pełnym testem trójkąt-trójkąt.
- Blender i eksport: status tylko po faktycznym wykonaniu workflow.
- Unity 6 import/EditMode/PlayMode/URP: NOT RUN.
- Bez zmian scen, ProjectSettings i mechanik rozgrywki.
