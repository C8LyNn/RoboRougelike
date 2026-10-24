# M02-006 — Koło kolczaste na rumowisko

**MODEL-02 / nowy model katalogowy** — bez modyfikowania mechanik, scen, prefabów i ProjectSettings Unity.

## Konstrukcja i wygląd

Koło do jazdy po rumowisku o średnicy zewnętrznej **1,070 m** (wraz z kolcami). Bieżnik gumowy ma profil z dwoma rowkami. Na jego obwodzie umieszczono **16 wymiennych kolców ze stali hartowanej**. Każdy kolec osadzono w stalowym bucie obejmującym oponę; osobna miedziana główka śruby sygnalizuje wymienność elementu. Dziesięć szprych łączy tytanową obręcz z miedzianą piastą; każda szprycha ma bursztynową wstawkę. Całość ma **7 materiałów**, w tym ceramikę tarczy hamulcowej.

Geometria jest zamknięta i składa się z osobnych, łatwych do wymiany części. Kolce nie są luźnymi, niezwiązanymi bryłami: końcówka zachodzi na but, but na gumę, a łeb śruby na but. Wszystkie są częścią jednego obracającego się zespołu.

**Mocowanie:** płaszczyzna ramy i pivot w `(0,0,0)`, oś koła **+X**, środek koła w pobliżu `X=0.18 m`. Nieruchoma flansza, wał i retainer są osobną hierarchią. Rzeczywisty otwór w piaście: promień **0,055 m**, wał promień **0,032 m**; nominalny luz promieniowy **23 mm**. Statyczny wał nie obraca się razem z kołem. Kolce zwiększają promień z 0,432 m do 0,535 m. Model jest wizualny: kontakt z podłożem i parametry fizyki należy ustalić dopiero podczas integracji.

## Źródło i eksport

`generate_m02_006.py` tworzy deterministyczne OBJ i MTL przy użyciu standardowej biblioteki Python. W środowisku `bpy` dodatkowo tworzy animowany GLB (pełny obrót w 2 s, pięć kluczy z wymuszonym próbkowaniem), a opcjonalnie FBX, jeśli lokalny eksporter Blendera udostępnia bezpieczny eksport tylko własnej kolekcji i bibliotekę kolekcji `M02-006.blend` do importu przez **Append**. Generator usuwa tylko własną kolekcję `M02-006_ASSET` i przywraca poprzedni stan sceny. Nie usuwa cudzych obiektów.

```bash
python3 Tools/ModelFactory/M02/M02-006/generate_m02_006.py --self-test
python3 Tools/ModelFactory/M02/M02-006/generate_m02_006.py --out build/M02-006
blender -b --python Tools/ModelFactory/M02/M02-006/generate_m02_006.py -- --out build/M02-006
```

Pliki wynikowe CI, jeśli eksport się powiedzie, znajdują się w `Tools/ModelFactory/M02/M02-006/generated/`.

## QA i ograniczenia

Self-test kontroluje każdą bryłę pod względem niezerowych trójkątów, zamkniętej rozmaitości krawędzi, dodatniej objętości i materiału. Sprawdza też mechaniczne kontakty kolców i szprych oraz **72 próbki pełnego obrotu** z porównaniem części nieruchomych i ruchomych. Test wykorzystuje analityczną separację radialną i osiową, nie symulację sztywnych ciał. Statyczne testy nie zastępują przeglądu normalnych, kolizji z pojazdem i Unity PlayMode.

**Status Blender/GLB/FBX i Unity** należy oceniać na podstawie rzeczywistych logów CI. Sam skrypt nie jest dowodem udanego eksportu. Unity 6 EditMode/PlayMode i wizualna kontrola w grze pozostają **NOT RUN** do czasu wykonania.

**Claim:** `Docs/ModelFactory/claims/M02-006.json` na `feature/rrl-model-factory-queue-v1`; commit CREATE `45b7b0ecd9972fab008402ac87f73f512616bc3c`.

**Import Unity:** OBJ może zostać zaimportowany jako model statyczny. Animowany GLB wymaga zgodnego importera glTF, np. glTFast. FBX jest opcjonalny i może być niedostępny w Blenderze 5.0.1 z Ubuntu, którego operator w tym środowisku nie udostępnia bezpiecznego eksportu wybranych obiektów. Jeśli powstanie `generated/M02-006_animated.fbx`, można spróbować natywnego importu Unity 6; jego poprawność pozostaje niezweryfikowana. Po imporcie trzeba sprawdzić oś +X, pivot, skalę 1 m i animację.
