# B01-40 — W30: niezależny odbiór faktycznej gry #873 + garażu #877

**Nie produkuje nowej wersji systemu.** Kontynuuje istniejące źródła gry:
- Codex działająca baza #824 @ `ed8cf4bcc131688b9a93968a89683d2496586683`
- wybrana integracja gry #873 @ `d537b7310979b91df27a08b14d0f1c74d0889089` (37 changed paths)
- aktualny child garażu #877 @ `20273c3580079dfb299c30c85a67e76f8d893e7f` (2 modyfikacje oryginalnego Unity gameplay + jeden raport)
- oryginalne bezpośrednie źródła #810 `05db0ddf0a48da592d924e68b7658eea6bba764e` (19), #839 `fea000fe0a706df2b52b6f295bdb3135ba1f9384` (3), #866 `3b3f402ca2adcc9525d96388cca17f0b5e1ae06d` (6)
- #861 @ `8321131aac3e175255a357cf5a30b7716ef6e8fd` **nie jest częścią obecnej gry**; wyłącznie oddzielny przegląd źródeł i HOLD native

## Co naprawdę uruchamia CI

Wykonywalne `audit.py` bierze **oryginalne obiekty Git**, a nie stare repo branch HEAD:
- potwierdza rzeczywisty ancestry i **37+3** dokładne oryginalne ścieżki. Zatrzymuje test przy wprowadzonym produkcyjnym źródle spoza wybranego zakresu, jak również przy utracie oryginalnych #810/#839/#866.
- skanuje wszystkie `Assets/**` z końcowego #877, sprawdza realne Unity GUID i nazwy asmdef, oraz `.meta` dla źródeł nowego JumpPad.
- bada **aktualny kanoniczny R3BuildValidation**: fizyczny BUILD001 before FunctionalWeaponPlacementIssue; realną fizykę piły ClosestPoint/IsOnCuttingDisc i krótką lufę SphereCastAll; wyjątek EMP **radial-only**; brak drugiego walidatora. Oddzielnie odczytuje prawdziwy kod R3VehicleModel i R3GarageView, aby źródłowo sprawdzić: podgląd→CheckPlacement→installationGate, Buy/Mount dopiero po pozytywnej odpowiedzi, brak Scrap/ID przed walidacją. Wymaga oryginalnego napisanego UnityTest z dokładnie odrzuconą kolidującą piłą oraz legalnym zewnętrznym montażem. **Te kontrole source-only nie dowodzą rzeczywistego zderzenia PhysX, 0/90° ani Playera.**
- potwierdza realne źródło `R5Arena` z jednym autentycznym JumpPad trigger, ForceMode.VelocityChange oraz dwoma testami sceny Unity **AUTHORED/NOT_RUN**.
- kontroluje kandydat #861 wyłącznie jako **niezintegrowany oddzielny poprawiony source**, oczekuje właściwych pięciu zmian, OverflowedLastTick, poprawionego zewnętrznego mocowania i Enemy_Yaw. Nie instaluje #861 i nie stwierdza Unity PASS.
- tworzy realny, pusty scratch i wykonuje fizyczne kopiowanie 40 zmienionych źródeł; odmowa rollbacku obcego pustego katalogu, zmienionych bajtów i ochrona niezależnej obcej gałęzi danych. Tylko w czystym scratch.
- tworzy deterministyczny ZIP **READ_ONLY_CURRENT_DIFF**, z dokładnie 40 rzeczywistymi, obecnymi plikami #877 i pełnym manifestem Git blob/SHA256. Trzeci job CI pobiera oryginalne dwa artefakty Windows2025 i Ubuntu24 i sprawdza binarną identyczność ZIP+manifest. **Nie instaluje nic na aktywnym Codex.**

## Dalszy rzeczywisty odbiór gry — bramki

`SOURCE_G0=PASS` dopiero na finalnym SHA i sprawdzonym CI. Następne obowiązkowe kroki:
1. B01-60/Codex musi uruchomić native Unity6000.3.14f1 EXACT #877 na prawdziwej scenie `Assets/scene 1.unity` i `R5Arena`. EditMode i PlayMode: pierwotne `GarageEditFreedomTests`, zabezpieczenie piły / 0° 90° / wszystkie pięć kategorii, montaż Wheel/Track/Gun/Multi; brak utraty Scrap i stabilne ID; zachowanie grup broni EMP direct gate 12+22 osobne dotychczasowe dowody; obecny #877 **nie** kończy mechanizmu swept joint/blue-orange fizycznie.
2. #810 JumpPad: naprawdę w Salvage Yard, fizyczny Rigidbody + kontakt z graczami/przeciwnikami, wielokrotny Collider i cooldown; Wasteland bez pad; Windows Player wyjęty z ZIP na finalnym SHA.
3. B01-24/B01-60 oddzielnie native #861: oryginalne WPN005 20/20 i RRLW6 4/4, overflow i fizyczne trafienia/nagrzewanie, rozsunięta geometria Enemy Saw vs wygląd. Dopiero po G1 B01-00 decyduje o kontrolowanym złączeniu.
4. Jeden Save #826: wcześniejsze formalne source composite G0 B01-40 #859/#862; prawdziwy runtime binder do `Rig[]/AtomicDockLedger`, portfel/WIN/loaner, prawdziwy Windows cold Save/Load. Nie importować #797 ani raw #787 jako drugi writer. #858 i #855 osobne nowe źródła niezaakceptowane automatycznie.
5. #812/#837 zależy od decyzji właściciela w issue #834, bez zmiany designu. R41 Turret v5 osie nadal zły yaw/muzzle, nie zastępować działającego modelu.
6. Pełny Windows x64 build, EditMode/PlayMode na **końcowym nowym SHA**; autentyczny Editor.log, NUnit XML, Player.log, ZIP SHA, extracted Player i test 2 walk. Osobno API-controlled i człowiek.

**Status:** IMPLEMENTED=EXISTING_#873+#877_PRODUCTION_SOURCE; SOURCE_G0=PENDING_CI; UNITY_NATIVE_G1_THIS_QA=NOT_RUN; GAME_INTEGRATION_PROVED_BY_ACTUAL_PLAYER=NO; W30_WINDOWS_ZIP=NOT_PRODUCED; G2=HOLD; NO MERGE/RELEASE. Ten PR dodaje wyłącznie B01-40 QA, manifest/CI, nie wprowadza kolejnej gry, drugiego walidatora ani nowego Save.
