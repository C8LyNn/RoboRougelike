# M02-020 — Układ kierowniczy z listwą

**MODEL-02 / RoboRougelike / Unity 6.** Wyłącznie wizualny moduł mechaniczny, bez zmian w gameplayu, wspólnych scenach i ProjectSettings.

## Mocowanie, skala, kinematyka

Root: `MOUNT_CHASSIS__Z0__UNITY_1M` w **(0,0,0)** na dolnej płaszczyźnie mocowania. **1 Blender unit = 1 metr**. Cztery śruby mocujące, dwie podpory, stalowa płyta, aluminiowe prowadnice i otwarta obudowa. Oś X to ruch listwy, Y wzdłuż pojazdu, Z pionowo (pinion i obie zwrotnice).

Listwa: skok **±65 mm**. Pinion: 20 zębów, promień podziałowy 60 mm, moduł nominalny 6 mm, obrót **±62,07°**. Dwie zwrotnice w rozstawie 1,2 m obracają się zgodnie z rozwiązaniem geometrycznym dwóch okręgów; oba drążki kierownicze zachowują stałą długość, a nie sztucznie zmieniają skalę. Dwie gumowe manszety mają po dziewięć osobno animowanych fałd. Elementy mają **8 materiałów PBR**: stal, aluminium, brąz, guma, pomarańcz, niebieski lakier, chrom i stal utwardzana.

**Ograniczenie inżynierskie:** zęby listwy i pinionu zachowują konserwatywny **2 mm odstęp obwiedni**. Jest to mechanicznie czytelna wizualizacja ruchu, a nie obliczona inwolutowa para zębata do przenoszenia sił. Brak symulacji kontaktu, luzów łożysk pod obciążeniem i PhysX.

## Uruchomienie

```bash
python3 Tools/ModelFactory/M02/M02-020/qa_m02_020.py --math-only
blender -b -noaudio --python Tools/ModelFactory/M02/M02-020/generate_m02_020.py
python3 Tools/ModelFactory/M02/M02-020/qa_m02_020.py
```

Generator dwukrotnie odtwarza swoją kolekcję w jednej sesji i porównuje liczby obiektów/materiałów. Sprawdza manifold i zdegenerowane powierzchnie źródłowe. Walidator niezależnie bada 121 próbek ruchu (242 połączenia drążków), a po realnym eksporcie strukturę pliku glTF 2.0, liczbę materiałów i kanały animacji.

Oczekiwane pliki **dopiero po faktycznym uruchomieniu Blendera**: `generated/M02-020.blend`, `generated/M02-020_animated.glb`, `generated/M02-020_qa.json`. GitHub Actions publikuje pliki binarne jako artefakt workflow; sam skrypt nie jest dowodem wykonania Blendera.

**NOT RUN, dopóki nie zostanie zweryfikowane osobno:** import Unity 6, EditMode, PlayMode, fizyka pojazdu, pełna kolizja trójkątów i ręczny przegląd wizualny. PR pozostaje Draft.
