# M01-011 — Długi karabin precyzyjny (MODEL-01)

**Katalog:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`.  
**Claim:** `Docs/ModelFactory/claims/M01-011.json` na tej samej gałęzi.  
**Claim CREATE commit:** `47f860d62d927fdbc2126df0a3ea0a130589de38` (2026-10-24T20:49:24Z).  
**Zakres:** wyłącznie model wizualny. Nie modyfikuje mechanik WPN, scen, prefabów ani ProjectSettings.

## Sylwetka i geometria

Długi karabin z prawdziwie przelotową, wydrążoną lufą, wielosekcyjnym hamulcem wylotowym, wysoką optyką, asymetrycznymi bocznymi wspornikami z mechanicznymi połączeniami, osłonami ceramicznymi i zewnętrznym prawostronnym zamkiem. Konstrukcja zachowuje czytelność sylwetki w grze z kamerą trzecioosobową i w podglądzie części.

Siedem materiałów: grafitowa powłoka, satynowy tytan, biała ceramika, anodowana miedź, ciemny elastomer, turkusowe szkło optyczne z emisją i bursztynowy wskaźnik. Emisja służy tylko do akcentów.

## Mocowanie i ruch

Skala: **1 Blender unit = 1 metr**. `MOUNT_TOP_SOCKET` jest w `(0,0,0)` i oznacza rzeczywisty punkt kontaktu z górnym gniazdem pojazdu. **+Z** jest kierunkiem strzału/lufy, **+Y** kierunkiem do góry, **+X** prawą stroną broni.

`BOLT_CARRIER_R` jest osobnym węzłem animowanym. Suwadło porusza się wzdłuż lokalnej osi **−Z** o **120 mm** i wraca. Klucze: 1 (zamknięty), 12 (otwarty), 24 (otwarty), 36 (zamknięty), 48 (zamknięty), 60 FPS. Wsporniki szyny są fizycznie osadzone w odbiorniku, a stopka zamka spoczywa na prowadnicy. Ruch nie wymaga żadnego skryptu gameplay.

## Generowanie

```bash
blender -b --factory-startup --python-exit-code 1 \
  --python Tools/ModelFactory/M01/M01-011/generate_m01_011.py \
  -- --out /tmp/m01-011
python3 Tools/ModelFactory/M01/M01-011/validate_m01_011.py /tmp/m01-011
```

Generator usuwa tylko swoją poprzednią kolekcję, nie mnoży obiektów modelu w tej samej scenie. Eksportuje tylko własną kolekcję i przywraca zaznaczenie użytkownika. Generowane pliki: `M01-011_long_precision_rifle.blend`, `_animated.glb`, `_animated.fbx`, `.obj`, `.mtl`, `M01-011_qa.json`.

## Bramka QA

Generator testuje zamknięte, poprawnie skierowane i niedegenerowane siatki, co najmniej trzy materiały, mocowanie, podparcie prowadnicy i kontakt stopki. W **48 klatkach** bada rzeczywiste światowe AABB wszystkich części ruchomych względem nieruchomych. Zamierzone styki stopki z szyną i wspornikami są dozwolone tylko jako kontakt bez objętościowego przenikania. Niezależny walidator odczytuje rzeczywiste pliki GLB, FBX, BLEND, OBJ i MTL, w tym animację translacji zamka i zakres 120 mm.

**CI:** `.github/workflows/ci-model-m01-011.yml`. Tylko po faktycznym sukcesie Blender/QA workflow zapisuje binarne assety do `generated/` na własnej gałęzi. Jeśli CI nie uruchomi się lub nie przejdzie, status eksportu pozostaje **NOT RUN / FAILED**, a sam skrypt nie oznacza gotowego modelu.

**Niewykonane:** Unity 6 import, EditMode, PlayMode, ocena wizualna renderu, integracja z rozgrywką. QA AABB jest testem konserwatywnym dla pozycji animacji, a nie fizycznym testem kolizji w Unity.
