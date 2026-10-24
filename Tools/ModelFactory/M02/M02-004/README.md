# M02-004 — koło stalowe perforowane

**MODEL-02**, pozycja z [CATALOG_V1.md](https://github.com/C8LyNn/RoboRougelike/blob/feature/rrl-model-factory-queue-v1/Docs/ModelFactory/CATALOG_V1.md). Rezerwacja utworzona operacją CREATE w `Docs/ModelFactory/claims/M02-004.json` na gałęzi katalogu, commit `8aba85d7f0589a8cd2be8b6173b786c7fa71958b`, 2026-10-24 13:47:22 UTC. Nie powielaj tego ID.

## Mechaniczna konstrukcja

Pojedyncze boczne koło stalowe z **12 rzeczywistymi otworami przelotowymi w tarczy**, 24 wymiennymi zębami trakcyjnymi, frezowanym obrzeżem, przednim miedzianym pierścieniem piasty, sześcioma łbami śrub, nieruchomą osią i uszczelnieniem pyłowym. Brak gumowej opony; stalowa obręcz i nakładki przenoszą kontakt z podłożem.

- Skala: **1 Blender unit = 1 metr**; Blender **Z up**. Pivot i port mocowania: `M02-004_MOUNT_SIDE_X0` przy **(0, 0, 0)**. Oś obrotu **+X**; pozycja koła względem mocowania od X=0.100 do 0.365 m.
- Promień maksymalny **0.478 m**, średnica **0.956 m**; szerokość modelu od punktu mocowania **0.365 m**. Przy integracji w Unity należy sprawdzić konwersję osi Blendera Z-up do silnika Y-up.
- Stalowy rdzeń tarczy: 12 faktycznie otwartych perforacji pomiędzy promieniami 0.165 i 0.332 m, cztery z ośmiu sektorów azymutu na każde okno.
- Stała oś ma promień 0.055 m, otwór piasty 0.085 m: **30 mm luzu promieniowego**. Pierścień uszczelniający ma promień 0.078 m: **7 mm luzu**. Brak ruchomej piasty przenikającej nieruchomą oś.
- Animacja: 360° obrotu rotora wokół lokalnej osi X od klatki 1 do 73, interpolacja LINEAR. Zęby, tarcza, obręcz, śruby i dekoracje poruszają się razem jako jedna grupa; nieruchoma oś, kołnierz i uszczelka pozostają w ramie.
- Materiały: `forged_steel`, `dark_steel`, `copper`, `ceramic`, `rubber`. Zęby trakcyjne są celowo wsunięte o 4 mm w obręcz jako **połączenie spawane**, nie przypadkowa kolizja.

## Artefakty i wykonanie

Źródłem jest `generate_m02_004.py`, który w czystym Pythonie generuje realny `M02-004.obj`, `M02-004.mtl` i `M02-004.static-qa.json`, a w prawdziwym Blenderze dodatkowo `M02-004.blend` i animowany `M02-004_animated.glb`. Generacja jest deterministyczna, z rozdzielonymi nieruchomymi i obracającymi się komponentami. Skrypt usuwa wyłącznie własną kolekcję przy ponownym uruchomieniu i eksportuje GLB tylko z własnych obiektów.

```bash
python3 Tools/ModelFactory/M02/M02-004/generate_m02_004.py --self-test --output-dir /tmp/m02-004
blender --background --factory-startup --python Tools/ModelFactory/M02/M02-004/generate_m02_004.py -- --output-dir /tmp/m02-004 --require-blender
```

Workflow `.github/workflows/ci-model-m02-004.yml` sprawdza prawdziwą geometrię OBJ/MTL, zamkniętość siatki, rozmiary i 73 próbki obrotu. Osobne zadanie instaluje Blendera na runnerze, uruchamia eksport animowanego GLB i kontroluje obecność animacji oraz nagłówka glTF 2.0. **Status należy odczytać z GitHub Actions — samo istnienie workflow nie jest potwierdzeniem uruchomienia.** Eksporty są uploadowane jako artefakty przebiegu CI, a nie zapisywane automatycznie do gałęzi.

**Unity:** OBJ/MTL są statycznymi formatami importowalnymi przez Unity; GLB wymaga zgodnego importera (np. glTFast). Unity EditMode/PlayMode oraz faktyczny import w projekcie: **NOT RUN**. Nie dodano komponentów gameplay, zmian scen, ustawień projektu ani ingerencji w inne paczki.

## QA — ograniczenia

Statyczny test topologii weryfikuje zamknięcie siatki przez liczność krawędzi i niezerową objętość. Kolizja rotora z osią i uszczelką sprawdzana jest analitycznie (promienie i 73 kąty); **nie jest to symulacja Unity PhysX**. Styk spawanych zębów i obręczy jest celowy. Kontrola wizualna materiałów i płynności animacji w grze wymaga osobnego uruchomienia Unity. Jeśli CI Blendera jest niedostępne, raportuj jego rzeczywisty stan jako BLOCKED/NOT RUN, a nie PASS.
