# Celowany pakiet QA — zmiany po ostatnim buildzie

Kod odniesienia dla początkowej kolejki: `b616c8722749031424445d90ea6b1240864589c1` (Draft PR #1124); późniejsze poprawki mogą zmieniać kod i rozszerzać tę listę. Przed uruchomieniem zapisz bieżący HEAD i potwierdź czystość plików `Assets`, `Packages` i `ProjectSettings`. Ten pakiet **nie został wykonany**. Roslyn i kontrole bez Unity opisane w poszczególnych raportach nie są natywną walidacją.

## Jedna celowana sesja Unity

Użyj Unity 6000.3.14f1 i zachowaj XML, Editor.log oraz kody zakończenia. Nie uruchamiaj dodatkowej pełnej regresji bez konkretnego powodu. Wybrane testy EditMode wykorzystują `[UnityTest]` z wejściem w Play Mode; nie stanowią pełnego zestawu PlayMode.

- `MapStorageIntegrityTests`: `RealEditorRequiresExplicitSecondSaveForOverwrite`, `LoadingAStoredMapRequiresConfirmationForUnsavedChanges`, `ReturningToMenuFromMapGarageWarnsAboutUnsavedDraft`, `QuitAndReturnRequireSeparateConfirmationForUnsavedMap`.
- `UI943PortraitGarageTests`: `QuickRepairDoesNotOverlapPartListOrVehicleView`; `UI943PortraitCatalogTests`: `SearchMatchesNamesWithoutCaseOrPolishDiacritics`, `SharedCatalogContainsAllDistinctBaseGroups`; `UI943PortraitCatalogActionsTests`: `CategorySwitchCannotBuyOrMountPreviousPartAndCatalogKeepsCategoryInSync`, `DestroyedItemKeepsCatalogOpenUntilRepaired`, `SearchClearsOldSelectionWithoutChangingInventoryOrWallet`, `OwnedFilterIncludesWrecksAndCannotKeepAnInvisiblePurchaseSelection`, `BatchRepairPreviewPicksAffordablePartsAndMatchesRealCharges`, `BatchRepairOnlyChangesTheSelectedInventoryStack`, `QuickRepairSelectsRecoveredWreckOnlyWhenMounting`.
- `UI943PortraitRewardsActionsTests`: `CollectedFinalPickupDoesNotConsumeRewardConfirmationKey`, `EscapeClosesRewardInventoryBeforePausingAndCancelsConfirmation`.
- `UI943WeaponStatusTests`: `ManualReloadHintRespectsDeviceAvailability`, `DefenseRowsUseActualMountedDevicesInOrderAcrossPages`.
- `RRLW34RewardTests`: `ScrapPreviewAndLedgerReportOnlyAmountActuallyCredited`, `DismantlePreviewMatchesWalletChangeAtTheLimit`.
- `BacklogEnergyRegenTests`: `EveryPassiveTierReservesTwentyFiveAndDestructionReleasesIt`, `MixedPassiveTiersEachReserveTwentyFive`, `CheckpointPreservesTieredBatteryPassiveReservationAndCurrentCharge`; `BacklogDeviceTierTests`: `TierTwoRegeneratorImprovesRecoveryAndReservesTwentyFive`, `TierThreeRegeneratorHasFourTimesTheEffectAndTwentyFiveReservation`; `BacklogDeviceTierGameplayTests`: `UpgradedMotorUsesTwoPowerUnitsAndTwentyFiveEnergyReservation`.
- `RRLW6ShieldPhysicalTests`: `EnemyShieldKeepsPassiveReservationDuringEmpUntilDestroyed`.

Przed testami sprawdź izolację danych. Testy map używają losowych nazw plików w katalogu map i usuwają własne pliki; testy w batch Editorze używają osobnego profilu technologii. Nie nadpisuj ani nie usuwaj zapisów gracza. Jeśli izolacji nie da się potwierdzić, oznacz odpowiedni przypadek NOT RUN.

## Późniejszy test interaktywny

1. Garaż: przełącz kategorię, sprawdź brak starej części/widma, kup i zamontuj wybrany egzemplarz. Włącz filtr posiadanych części i sprawdź także uszkodzony egzemplarz oraz powrót z pełnego katalogu. W odizolowanym profilu uszkódź kilka egzemplarzy jednej grupy, porównaj koszt naprawy grupowej w katalogu i na szybkiej liście z faktycznym wydatkiem, sprawdź ograniczony budżet oraz brak działania przy otwartych ustawieniach. Napraw grupę samych wraków i sprawdź, czy naprawiony egzemplarz jest gotowy do montażu; powtórz w trybie zaznaczania. W katalogu pionowym i poziomym wyszukaj koło bez polskich znaków, sprawdź brak wyników, wyczyść filtr, kup lub wybierz wynik oraz sprawdź, że wcześniejsza część nie pozostała wybrana.
2. Mapa: zapisz, zmień i spróbuj wczytać lub wyjść przez pauzę; sprawdź osobne potwierdzenia, anulowanie i ponowne ostrzeżenie po edycji. X i Alt+F4 nadal nie ostrzegają o niezapisanej mapie.
3. Po walce: zbierz ostatnią kostkę przed końcem odliczania; po wyborze nagród pierwszy Enter powinien je zatwierdzić. Podczas odliczania Enter nadal powinien je pominąć.
4. Złom: tylko na odizolowanym profilu blisko limitu portfela porównaj podgląd, rzeczywistą wypłatę i podsumowanie.
5. Strzelba, railgun i obrona punktowa: sprawdź komunikaty przy pełnym/pustym magazynku, przeładowaniu i blokadzie EMP.
6. Ustawienia pojazdu → Napęd: sprawdź czytelność wymagań kół, gąsienic, Omni i Hover oraz odblokowanie wyboru po montażu sprawnych części.
7. Energia modułów pasywnych: dla poziomów T1/T2/T3 sprawdź rezerwację 25 energii za każdy sprawny moduł, zgodnie z instrukcją użytkownika, oraz uwalnianie rezerwy po jego zniszczeniu. Starszy kod naliczał 25/30/35; obecna poprawka nie zmienia efektów poziomów. Test fizycznego pojazdu jest odłożony.

Oceń tekst i sterowanie także w pionowym i poziomym układzie. Nie traktuj przygotowanych testów ani kompilacji Roslyn jako PASS w Unity. Historyczny problem automatycznego zamykania C0000005 pozostaje niewyjaśniony.

**Build:** nie był przygotowany dla tego kodu. Nowy build Windows powstanie dopiero na osobną prośbę użytkownika; wcześniej udostępniony build `c332e6a5` nie zawiera wymienionych poprawek.
