# B01-13 — PASS 17 / AIM-OBS001 R1

**Jedna działająca paczka research / screenshot-witness QA do RoboRougelike.** Otwierany lokalnie prototyp pozwala operatorowi oznaczać celownik (`R`), środek celu (`T`) i opcjonalne miejsce uderzenia (`I`) na autentycznym zrzucie Windows Playera, oblicza odległości **w pikselach na danym obrazie**, trzyma cztery porównywalne przypadki i grupy A/LMB / B/RMB, eksportuje **sam JSON + SHA-256 plików zdjęć** bez obrazów/PII. Pythonowy receiver fizycznie odczytuje te same PNG/JPEG/WebP i weryfikuje bajty/format/rozmiar/współrzędne/metryki. **NIE uruchamia ani nie modyfikuje Unity**, nie rozstrzyga na podstawie samych pikseli, czy pocisk naprawdę trafił. Domyślne dane i wszelkie dołączone wyniki są `SYNTHETIC`.

## Status i provenance

- `PACKAGE_ID=AIM-OBS001`, `VERSION=R1`, `PRODUCER=B01-13`, `OWNED_PATHS=Research/B01-13/P17/**`, izolowany Draft PR; **NIE** merge/integracja gry.
- Source game reference: Codex Draft [#310](https://github.com/C8LyNn/RoboRougelike/pull/310), `682dc0c5c13615577334375ffb6c27be5cdebf4a` (snapshot przy wyborze paczki). Oryginalny `Assets/R6/R6RangeHUD.cs` Git blob `adfd3c1a69fc4355e055581a736ce10cccc7ba91`; bieżący GUI/B-Reticle input musi zostać ponownie sprawdzony na **dokładnym** Player build użytym do zbierania zrzutów. Nie przepinaj gotowych raportów na inny commit.
- [#330](https://github.com/C8LyNn/RoboRougelike/issues/330) — PASS17. [#276](https://github.com/C8LyNn/RoboRougelike/issues/276) — single writer, canonical game tylko Codex.
- W #79 istnieje CX-02/CV-07 group-aware opt-in static Windows Player 28/28 dla broni × dystanse; ta paczka nie dubluje testu obrażeń. Zajmuje **nadal otwarty odbiór wizualny/dynamiczny** celownika, odległego/mobilnego celu, przesłony i zoomu, z oryginalnymi screenshotami dostarczonymi później przez legalnego operatora.

## Użytkowanie na prywatnym stanowisku

1. Wypakuj do izolowanego katalogu; otwórz `aim_observer.html` w Chromium/Edge (offline). Nie wymaga serwera ani połączenia z API. Hashowanie SHA-256 działa również bez WebCrypto w `file://`.
2. Wybierz `OPERATOR_SOLO` (techniczny operator **bez** realnych ludzi), wpisz `Sxx`, pełny **rzeczywisty commit SHA-1 gry** oraz SHA-256 **faktycznie użytego** Windows Player ZIP. `SYNTHETIC` służy tylko do prób funkcjonalnych. `HUMAN_APPROVED` wolno wybrać dopiero po osobnej zgodzie HQ GATE D, prywatnym świadectwie świadomej zgody, legalnym Player hoście i moderatorze; samo wpisanie referencji nie dowodzi autentycznej zgody.
3. W grze przygotuj zrzuty **CLOSE_STATIC**, **FAR_STATIC**, **OCCLUDED**, **ZOOM_MOVING**; oba A/LMB i B/RMB muszą zostać odnotowane w czterech przypadkach. Zrzuty mają być z rzeczywistej gry, nie z tego prototypu. Użyj właściwej lokalnej klatki PNG/JPEG/WebP (do 8 MB, max 8192 px). Wybierz `reticle` → kliknij obraz, `target` → kliknij, opcjonalnie `impact` → kliknij. Wybierz wynik **na podstawie rzeczywistych logów/obrażeń/efektu, nie współrzędnych**. Uwaga: próbka `OCCLUDED` nie zakłada automatycznie wyniku `BLOCKED`.
4. Zapisz dokładnie po jednej klatce na przypadek; eksport odblokuje się po 4 przypadkach i obu grupach. Eksport `AIM_OBS001_<MODE>_Sxx.json` zachowaj wraz z oryginalnymi obrazami w **prywatnym** katalogu; JSON zawiera jedynie metadane/hash/nazwy plików/punkty/notatki, **nie obrazy**. Używaj nazw plików bez danych osobowych i wyczyść je z uwag operatora. Pliki rzeczywistych osób nigdy nie trafiają na GitHub.
5. Zweryfikuj fizyczne obrazy z eksportem, pinując oba SHA (nie wolno wymyślać SHA):

```bash
python3 verify_aim_receipt.py /private/AIM_OBS001_OPERATOR_SOLO_S01.json \
  --image-dir /private/screenshots \
  --game-sha <ACTUAL_FULL_GAME_SHA40> \
  --zip-sha256 <ACTUAL_FULL_PLAYER_ZIP_SHA64>
```

Odbiornik wymaga Python 3.11+ i Pillow; przy błędnym SHA, zamianie pliku, brakującym obrazie, zdublowanym przypadku, fałszywym punkcie, sztucznie zmienionym dystansie lub złym LMB/RMB zwróci `FAIL_CLOSED`, exit `2`. Po poprawnych danych zwróci `STRUCTURAL_AND_IMAGE_BYTES_PASS_NOT_GAMEPLAY_PROOF`, exit `0`. **To nie jest potwierdzenie gameplayu ani legalnego udziału człowieka.**

### Lokalny test automatyczny

```bash
python -m pip install playwright pillow
python test_aim_observer.py
```

Test uruchamia **oryginalny** `aim_observer.html` w rzeczywistym Chromium. Ze względu na ograniczenie kontenera narzędziowego używa `page.set_content(original_bytes)`, czyli bez zmiany JS/CSS, zamiast nawigacji `file://`/localhost. Wykonuje cztery klatki z wygenerowanych lokalnie obrazów `SYNTHETIC`, kliknięcia w rzeczywisty DOM, eksport JSON, odbiór obrazów przez niezależny Python, negatywy (inny build SHA, mylna grupa, fałszywy plik/rozmiar, travel, sfałszowana odległość), wycofanie, reset, blokadę nieważnych human refs, 320/720px, screenshot, brak zewnętrznych requestów i JS errors. `QA_browser.json` jest dowodem tego **syntetycznego** testu; `QA_synthetic_preview.png` jest zrzutem **narzędzia**, nie gry.

### Zależności, ograniczenia i rollback

- Brak modyfikacji `Assets/**`, `Packages/**`, CI, prefabów lub kodu Codexa/CHL/PARTSIZE. Zero importu wymagającego GUID Unity.
- Źródło jest samodzielne HTML/JS; Python receiver zależy od Pillow, test Playwright i lokalnego Chromium. Nie ma zależności runtime od repo Unity.
- Pozycje 2D screenshotu nie mierzą 3D, RTT/PhysX, jitteru czasowego, wykrycia celu ani FPS. Różne rozdzielczości trzeba analizować oddzielnie; nie łącz pikseli 720p/1080p jak tej samej jednostki.
- Weryfikator gwarantuje spójność JSON z oryginalnym obrazem, lecz nie autentyczność obrazu (syntetyczny PNG również przejdzie). Zaznaczenia i wyniki są deklaracjami operatora. Brak rzeczywistych czterech screenshotów Playera w tej paczce: `PLAYER_GUI_NOT_RUN`.
- Przycisk wycofania czyści pamięć **bieżącej karty**, nie istniejące pobrane pliki/kopie/backups. Host musi usunąć je zgodnie z HQ polityką; brak automatycznej trwałej tombstone pomiędzy restartami. Nie używaj tego narzędzia jako repozytorium rzeczywistych danych.
- Import do Codexa **opcjonalny**: skopiować tylko `Research/B01-13/P17/` do operatora lub otworzyć HTML jako plik; nie kopiować pod `Assets/` i nie scalać do gry. Codex potrzebuje poprawnego Playera z prawdziwymi klatkami, jeśli ma rozstrzygać GUI. B01-40 weryfikuje zakres + hash; B01-60 niezależnie uruchamia tę paczkę na fizycznych bajtach. Rollback: usunąć prywatną kopię katalogu lub wycofać PR bez żadnego wpływu na gameplay.
