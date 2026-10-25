# M02-018 — Mechaniczny hamulec tarczowy

Model wizualny do Unity 6 RoboRougelike, **bez implementacji gameplayu**. Katalog: M02-018, mocowanie przy kole, ruch tarczy i szczęk/zacisku, wyróżnik: zacisk z ceramiczną tarczą/okładziną.

## Budowa

Perforowana stalowa tarcza z 18 rzeczywistymi otworami wentylacyjnymi, piasta i wielośrubowy kołnierz, odsłonięty nieruchomy zacisk typu C, dwie ceramiczne okładziny cierne, płytki nośne, śruby prowadzące, ręczna dźwignia cięgnowa, punkty montażowe i znaczniki stanu. Materiały: stal, grafit, tytan, miedź, ceramika, guma, bursztyn i cyjan.

Skala: 1 Blender unit = 1 m. Oś koła: +Y; oś środka tarczy: (0,0,0.36) m. Root/pivot montażu: MOUNT_DATUM_ORIGIN=(0,0,0). Znacznik mocowania osi: WHEEL_AXLE_SOCKET_Y. Korpus zacisku pozostaje nieruchomy.

## Animacja

ROTOR_SPIN_Y obraca się o 720° w klatkach 1–121 (30 FPS). PAD_SLIDE_NEG_Y oraz PAD_SLIDE_POS_Y przesuwają się symetrycznie o 3 mm, następnie wracają; CABLE_CAM_LEVER_PIVOT_Y obraca dźwignię o 0.15 rad. Tarcza ma grubość 18 mm, początkowy luz osiowy pad–tarcza to 4 mm na stronę, minimalny 1 mm. To celowo **animacja demonstracyjna bez fizycznego kontaktu**; w grze siłę hamowania wyznacza kod fizyki, nie animacja.

## Generowanie i QA

```bash
python3 Tools/ModelFactory/M02/M02-018/qa_m02_018.py
blender --background --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M02/M02-018/generate_m02_018.py -- --out /tmp/M02-018
python3 Tools/ModelFactory/M02/M02-018/qa_m02_018.py --verify --out /tmp/M02-018
```

Blender zapisuje BLEND, animowany GLB i raport QA; FBX jest próbą best-effort, a brak eksportera raportowany jako BLOCKED. Pliki eksportowane przechowuje artefakt CI, nie są commitowane do repo. Testy w skrypcie sprawdzają zamkniętość siatek i 121 próbek luzu osiowego. **Nie** są pełną symulacją kontaktu trójkątów, fizyki PhysX ani testem Unity. Przed scaleniem potrzebny jest import do Unity 6, oględziny materiałów URP i test animacji.

## Izolacja

Tylko Tools/ModelFactory/M02/M02-018 i osobny workflow; bez zmian w main, develop, ProjectSettings, scenach lub cudzych paczkach.
