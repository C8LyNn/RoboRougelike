# Skróty narzędzi garażu

Baza `919b4f12` (Draft PR #1141). Klawisze 1–4 oraz odpowiedniki numeryczne wybierają montaż, zaznaczanie, naprawę i zdejmowanie. Etykiety na pasku narzędzi pokazują numer. Modyfikatory Shift/Ctrl/Alt/Command nie uruchamiają tych skrótów. Istniejące kierowanie zdarzeń garażu blokuje je przy otwartym katalogu, ustawieniach, pauzie i podczas przejścia. Ponowne naciśnięcie aktywnego narzędzia nie czyści wskazanego celu.

- Kompilacja Roslyn runtime i testów Editor: **PASS**, kody 0/0, logi `Artifacts/Local/CSharp-GarageToolShortcuts-20261108/`.
- Poza Unity na rzeczywistej skompilowanej funkcji mapującej: **8/8** sprawdzeń klawiszy i modyfikatorów.
- Test Unity rzeczywistej zmiany narzędzia został przygotowany, ale **NOT RUN**; faktyczne zdarzenia klawiatury, tekst na pasku, stan modali i Player są **NOT RUN**. Przed testem sprawdź także klawiaturę numeryczną i skupienie pola wyszukiwania.
- Nie przygotowano buildu.
