# B01-40 W29 — niezależne G0 #839 EMP, bez zmian producenta

**Przeznaczenie:** sprawdzony w natywnym QA #849 bezpośredni EMP należy dopuścić do bezpiecznego odbioru istniejącego R3, nie tworząc drugiej mechaniki ani drugiego systemu zasobów.

**Piny:** #839 `fea000fe0a706df2b52b6f295bdb3135ba1f9384`, oryginalna baza #824 `e03d36f2b86f8c73a067e574b67237a8921f622a`, późniejszy opublikowany HEAD gry #824 `ed8cf4bcc131688b9a93968a89683d2496586683`, QA #849 `16d338f13779ee71ac74b8549e0168dce92bfda3`. Żaden aktywny lokalny worktree Codexa nie jest odczytywany.

**Wykonywalny G0:** `verify.py` sprawdza rzeczywisty Git diff oryginalnego #839 (wyłącznie 3 modyfikacje EMP i 1 nowa dokumentacja), dla każdego z 3 produkcyjnych/testowych plików zgodność starego blob SHA R3 z nowym opublikowanym R3 (brak driftu), rzeczywiste oryginalne `.cs.meta` GUID niezmienione, zgodność #849 tylko QA additions. Dodatkowo źródłowo sprawdza pre-energia/pre-cooldown/pre-physics rzeczywisty guard w `EmpEmitter.TryPulse` i `Ready`, aktualny `RRLDeviceBinding.Available`, poprawne podpięcie i EMP-status, zachowanie radialnego `OverlapSphereNonAlloc`, fizycznej osłony oraz oryginalnego testu 40→40 / 0 cooldown przy odmowie i 40→33 po zaakceptowanej emisji. Wykonywane są dwa testy mutacji (brak bramki wywołania / brak binding owner) i realny scratch stage/rollback, w którym cudzy pusty katalog i zmienione bajty blokują czyszczenie. Wytwarzany jest SOURCE-only ZIP (3 oryginalne aktualizowane .cs, per-file Git blob/sha256 manifest, czytelny STOP warunek) i porównywany bajtowo między Windows2025/Ubuntu24.04.

**Wykonane natywne testy kandydata (dowody innego właściciela, a NIE tego G0):** [#849](https://github.com/C8LyNn/RoboRougelike/pull/849) opisuje Unity 6000.3.14f1 12/12 EditMode, 22/22 PlayMode, Windows Player API-controlled detach/reattach/real pulse i 7 energii z rzeczywistym odbiornikiem. Są to izolowane testy #839 — **nie** natywny PASS finalnego wspólnego W29 HEAD, pełnego enemy modułu lub automatyczny merge.

**Bezpieczne wdrożenie:** B01-00 musi potwierdzić aktualną własność gałęzi/HEAD i autoryzować dokładne trzy `.cs` #839. Import TYLKO do nowego, czystego projektu/gałęzi Codexa po odbiorze G0 i wykonaniu testów; żaden komplet historycznych worktree/branch nie jest przepisany. Po instalacji B01-60 lub Codex robi nową wspólną kompilację Unity, test radial occlusion, no-energy/cooldown blocked, P1 gameplay/AI receiver, Player zip+log. W razie konfliktu źródła blokada, nie overwrite. Nie instaluj QA harness #849 w produkcji.

**Werdykt na etapie implementacji:** SOURCE_G0=PENDING_EXACT_CI, NATIVE_G1_THIS_QA=NOT_RUN, #849_NATIVE_ISOLATED_PASS_REPORTED, G2=HOLD, CURRENT_GAME_INTEGRATION=NO, RELEASE=NO, CODEX_OFFER_NEW=NONE, MERGE=NO.

**Następni:** B01-00 zatwierdzenie, Codex integrowanie w działającą grę, B01-60 full native, B01-41/61 host/security regression. B01-40 utrzymuje tylko source G0 i kontrolę kolizji z konkretnym wspólnym SHA.
