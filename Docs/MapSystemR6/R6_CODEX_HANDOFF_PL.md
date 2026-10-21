# CODEX — MAP-SYSTEM R6 / Unity QA & driving evidence

**Repo:** `C8LyNn/RoboRougelike` · **Unity:** 6000.3.14f1 URP · **MODE:** isolated branch, no merge to main.

1. Najpierw ustal, czy aktualny projekt ma już własny `Assets/RoboRougelike/MapSystemR4`. Jeśli tak, zachowaj SHA/mety i zintegruj pełny R6 w jednej gałęzi; unikaj zduplikowanych klas.
2. Importuj całość Runtime/Editor/Tests/Samples z `.meta`. Zbierz log kompilacji. Uruchom wszystkie stare testy R4/R5 i nowe R6 EditMode/PlayMode; zaraportuj rzeczywiste `passed/failed/skipped`.
3. Utwórz demo na izolowanej scenie: `Tools > RoboRougelike > MAP R6 > Create Demo In Current Scene`. Włącz Gizmos, ustaw `r6IncomingEdge`, `r6OutgoingEdge`, `r6JunctionNode`. Uruchom `R6 / Print selected junction evaluation`.
4. Przed modyfikacją sceny sprawdź przypadki z `Docs/R6_REFERENCE_JUNCTION_REPORT.json`. Połączenia oznaczone `NEEDS_REDESIGN` nie są traktowane jako przejezdne. Zmień geometrię w izolowanej gałęzi, nie dopasowuj limitów testów, by sztucznie uzyskać PASS.
5. Przejdź do G1b: skonfiguruj jawne warstwy przeszkód (bez nawierzchni i pojazdu). Użyj `R5 / Audit obstacle layers`. Brak maski = NOT RUN, nie PASS.
6. G1c: podepnij `MapR6DriveEvidenceRecorder` do rzeczywistego Rigidbody. Dostarcz z adaptera rzeczywisty `SetDriverDemand01(abs(throttle))`. Zmierz faktyczne pokonanie zakrętów, zatrzymania, przechyły, kontakt podwozia i skręcanie. Zapisz JSONy z `Application.persistentDataPath/MapR6Evidence`.
7. Upewnij się, że demo/telemetria NIE steruje pojazdem (to jedynie rejestrator). Zachowaj istniejące systemy MOV i Gauntlet.
8. Po testach przygotuj `MAP_R6_UNITY_QA_REPORT.md` ze SHA źródeł, wersją Unity, PASS/FAIL, logami i konkretnymi nieprzejezdnymi skrzyżowaniami.

**Brak merge/push na `main`, brak zmian w fizyce produkcyjnej, brak fikcyjnych PASS.**

Dodatkowo R6 zawiera `MapR6RouteValidator` (sprawdza kolejne skręty) oraz korektę `lower_road.padWidth` 12 → 18 m w mapie v6. Szczegóły: `Docs/R6_GEOMETRY_FIX_PL.md`.
