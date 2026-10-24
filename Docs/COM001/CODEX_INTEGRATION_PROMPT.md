# PROMPT DLA CODEXA — COM-001 / STRUCTURAL SCAN / R6

**Projekt:** RoboRougelike; **silnik:** Unity 6000.3.14f1 / Windows x64; **zadanie:** import i realna integracja. **Tryb:** DEVELOPMENT FIRST / TEST FIRST / brak merge do main i brak publikacji.

## 1. Źródło

Otrzymujesz paczkę `RoboRougelike_COM001_StructuralScan_v1.zip` jako osobny artefakt. Sprawdź manifest SHA-256 wszystkich plików. Nie zakładaj jej istnienia na Google Drive. Jeśli nie masz dostępu do paczki, zgłoś `WAIT: PACKAGE_UNAVAILABLE` zamiast przepisywać kod z pamięci.

Przeczytaj `README_START_HERE_PL.md`, `Docs/INTEGRATION_PL.md` i `Docs/QA_CHECKLIST_PL.md`.

## 2. Potwierdź stan R6

Ustal aktualną ścieżkę repozytorium. Ostatnio wskazywany katalog: `C:\Users\kubap\Desktop\Prototypy\RoboRougelike`. Potwierdź faktyczną wersję/commit i system części. Z raportu R6 znamy: siłową fizykę pojazdów, prawdziwe HP części, dziedziczenie ruchu oderwanych bloków, poligon, 96/96 zaliczonych testów i poprawny retail build. Nie zakładaj, że późniejsze pakiety MOV/WPN/DEF są już zintegrowane.

## 3. Import

Zaimportuj `Assets/RoboRougelikeMechanics/COM001StructuralScan/` bez modyfikowania niepowiązanych istniejących scen, GUID, `Packages`, `ProjectSettings` ani fabrycznych prefabów. Zapewnij kompilację assembly runtime i dwóch zespołów testowych. Wykonaj EditMode oraz PlayMode; napraw problemy rzeczywiście wykryte w Unity.

## 4. Adapter R6 — obowiązkowy

- Wykorzystaj R6 stabilne ID bloku, właściciela, drużynę, `currentHP/maxHP`, rzeczywiste collidery i aktualny stan mocowania.
- Utwórz minimalny read-only adapter `IStructuralScanPart`, bez duplikowania HP, grafu, wypadania modułów i odzysku Złomu.
- Podłącz rejestrację/wyrejestrowanie przy montażu, zniszczeniu, odłączeniu i despawn. Zmiana właściciela wymaga poprawnego update'u rejestru.
- Uzupełniaj `StructuralImportance` wyłącznie na podstawie faktycznie zweryfikowanego grafu R6; w przeciwnym przypadku 0. W UI nie obiecuj, że zniszczenie oznaczonej podpory na pewno odłączy część konstrukcji.
- Upewnij się, że własne części ignorowane są nawet dla odłączanych colliderów poza jedną hierarchią — użyj `IStructuralScanSelfFilter`.

## 5. Funkcjonalność gracza

Zintegruj moduł skanera jako montowalną i uszkadzalną część w garażu, jeśli system katalogu obsługuje bezpieczne rozszerzenie. W innym przypadku wdroż najpierw minimalny wariant na poligonie i przedstaw konkretny blocker rozbudowy katalogu.

Dodaj akcję skanowania korzystającą z obecnego Input System (nie wprowadzaj osobnego legacy `Input.GetKeyDown`). Pokaż na istniejącym HUD: chwilowe podświetlenie widocznych części przeciwnika, typ, opcjonalną kondycję i heurystyczną ważność. Użyj `Contacts` i `ResultsChanged`. Nie pokazuj ukrytych części przez osłony. Nie modyfikuj stanu HP przy skanowaniu.

Skan bez widocznego celu i brak energii nie mogą konsumować zasobów ani uruchamiać cooldownu. Po zniszczeniu/odłączeniu skaner ma przestać ujawniać nowe wyniki.

## 6. Testy konieczne do odbioru

- Uruchom **wszystkie dostarczone** NUnit EditMode i PlayMode; raportuj dokładne liczby wykonanych/passed/failed/skipped, nie liczby z opisu paczki.
- Porównaj przypadki: jawny rdzeń, rdzeń za pancerzem, rdzeń za ścianą, koło częściowo zasłonięte, wielokrotny collider jednego bloku, pojazd 180 części, dwa wrogie pojazdy, sojusznik, detached group, utracony sensor, brak energii i cooldown.
- Sprawdź tryby poligonu Standard/Light/Heavy i brak utraty zasobów oryginalnego garażu.
- Wykonaj aktualny komplet regresji R6 (bazowe 96 testów lub objaśnij rzeczywistą zmianę liczby), oraz build Windows retail i QA, jeżeli zmiana włączana jest do wydania.
- Odrębnie raportuj znany crash graficznego QA przy automatycznym `Application.Quit()` — nie interpretuj go jako zaliczonego procesu, jeśli kod !=0. Nie modyfikuj fizyki z powodu tej usterki bez dowodów.
- Wykonaj ręczny odbiór gracza: uruchom skan przy wrogu i zweryfikuj realny HUD, zasłony, cooldown i odłączenie modułu. Scenariusze API nie dowodzą natywnego inputu.

## 7. Zakazy

Nie przebudowuj R6, nie twórz równoległego HP, nie zmieniaj fundamentu napędu ani kolizji, nie publikuj i nie wykonuj merge do `main` bez dodatkowej autoryzacji. Nie dodawaj zależności od innych MOV/WPN/DEF, których nie potwierdzono w aktualnym repozytorium. Nie wrzucaj nieprzetestowanego kodu do głównego wydania.

## 8. Raport

Zapisz `Docs/Verification/COM001/COM001_INTEGRATION_REPORT.md` z:

- SHA-256 wejściowego ZIP i stanem aktualnego repo/commit;
- zmianami w plikach, opisem adapterów i mapowania ID;
- wynikami testów izolowanych i pełnej regresji R6;
- wynikami na poligonie, ekranami HUD i wynikiem sprawdzenia osłon;
- stanem zgodności z garażem i uszkodzeniami;
- wynikiem build Windows oraz ewentualnymi blockerami;
- stanem `READY` tylko po rzeczywistym zaliczeniu bramek lub `WAIT` z konkretną przyczyną.

**Zacznij implementację, nie kończ na audycie.**
