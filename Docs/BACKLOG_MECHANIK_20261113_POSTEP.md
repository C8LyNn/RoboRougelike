# Backlog mechanik 13.11.2026 — postęp

Źródło: `RoboRougelike_Codex_backlog_mechanik_2026-11-13.md`, dostarczone przez właściciela.
Lista poniżej opisuje tylko faktycznie sprawdzone fragmenty, nie zastępuje źródłowej checklisty.

## 10–11. Fuzja części — częściowo sprawdzone i uzupełnione

- Istniały operacja fuzji trzech fizycznych części oraz przycisk w katalogu garażu. Nowa część otrzymuje średnią procentową stanu wejść, także dla wejścia z 0 HP.
- Uzupełniono podgląd o stan HP wyniku i stany wszystkich trzech składników przed zatwierdzeniem operacji, w obu widokach katalogu.
- Unity 6000.3.14f1, EditMode, `BacklogPartFusionTests`: **6 PASS, 0 FAIL**. Test obejmuje wejścia 50%, 0%, 100% i wynik 50%, zużycie wejść, tożsamość oraz zapis stanu części.
- Unity 6000.3.14f1, `BacklogPartFusionGarageTests`: **1 PASS, 0 FAIL**; test wchodzi w Play Mode i sprawdza fuzję oraz montaż otrzymanej części w garażu.
- Rzeczywisty ręczny test Windows Player: **NOT RUN**. Pełny punkt 10 (czytelność i używalność podglądu podczas gry) pozostaje do potwierdzenia.
- Pierwsza próba Unity w sandboxie zakończyła się awarią edytora podczas startu usługi sieciowej; wynik testowy nie powstał. Ponowna próba poza sandboxem przeszła.

## 01–05. Sloty i limity — w przygotowaniu

Obecny zapis oraz sterowanie przewidują dwie grupy broni. Zmiana limitu bez równoległej obsługi ognia, migracji zapisu i przypisywania slotów udostępniłaby graczowi broń, której nie może kontrolować. Wymagane jest wdrożenie jako jednej zgodnej paczki.
