# RoboRougelike — COM-001 Structural Scan v1

**Unity 6 / R6 integration target / standalone package — no edits to main project**

## Co robi?

Przycisk/akcja skanu wskazuje **najbliższy widoczny w stożku pojazd przeciwnika** i prezentuje maksymalnie 16 **rzeczywiście widocznych** części. W raporcie kontaktu znajdują się stabilne ID części i właściciela, typ, snapshot HP, pozycja oraz **heurystyczny** priorytet taktyczny. Skan nie prześwietla ścian, nie podaje ukrytych bloków, nie zadaje obrażeń i nie modyfikuje R6.

Po 5 s kontakty wygasają; cooldown to 8 s. Parametry są zmienne. Energia domyślnie **wyłączona**, opcjonalna przez `IStructuralScanEnergy`.

**Ważne:** punkt podparcia typu `StructuralSupport` dostaje wysoką ocenę heurystyczną, ale system **NIE** wyznacza rzeczywistych węzłów krytycznych grafu. `StructuralImportance` dostarczony przez R6 może podnieść ocenę, jeśli prawdziwy algorytm to obliczył. Nie sugerować graczowi gwarantowanego odpadnięcia części.

## Jak uruchomić?

1. Wprowadź wyłącznie folder `Assets/RoboRougelikeMechanics/COM001StructuralScan/` do kopii roboczej projektu Unity 6000.3.14f1 (bez `Samples~` i `Docs` w Runtime).
2. Otwórz Unity Test Runner, uruchom EditMode i PlayMode tej paczki; sprawdź ewentualne problemy kompilacji przed integracją.
3. Do demonstracji: skopiuj `Samples~/StructuralScanDemoBootstrap.cs` do `Assets/`, dodaj go do pustego GameObject na nowej testowej scenie i uruchom Play. Kliknij **SCAN** w UI. Obie widoczne części powinny zostać wykryte; rdzeń ukryty za barierą nie.
4. Przeczytaj `Docs/CODEX_INTEGRATION_PROMPT.md` — pełna instrukcja adaptacji do R6.
5. Nie używaj `StructuralScanPartProxy` jako produkcyjnego źródła HP; to demonstrator. Produkcja używa adapterów `IStructuralScanPart` czytających prawdziwe R6 ID, HP i graf.

## Zawartość

- `Runtime/` — kontroler, matematyka, rejestr, zegar, kontrakty, opcjonalny magazyn energii i proxy demonstracyjny.
- `Tests/EditMode/` — logika geometryczna, ranking, stan i walidacje.
- `Tests/PlayMode/` — fizyczna widoczność, kolizje, osłony, cooldown, właściciel, filtr sojuszy i stabilność ID.
- `Samples~/` — opcjonalny bootstrap wizualnego demonstratora.
- `Docs/` — API, prompt, lista QA, szablon R6, dowody kontroli statycznej.
- `SHA256_MANIFEST.txt` — SHA-256 każdego pliku paczki (nie zawiera samego manifestu).

## Stan odbioru

Kod i testy są dostarczone jako **źródła**. W środowisku przygotowania **brak edytora Unity i kompilatora C#**, więc **nie** wykonano testów NUnit ani buildu Windows. Wykonano tylko statyczną inspekcję i kontrolę ZIP/manifestu. Status: **IMPLEMENTED SOURCE / UNITY QA PENDING**.

Nie zakładaj, że wcześniejsze MOV/WPN/DEF są zintegrowane z R6; samodzielny COM-001 nie zależy od nich.
