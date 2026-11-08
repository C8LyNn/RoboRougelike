# Nagroda bez części z wraków

Baza `2d8e46dc` (Draft PR #1147). Jeżeli po walce nie ma zestawu części do wyboru, ekran nagród wskazuje wybór samego bonusu zamiast wymagać nieistniejącego drugiego wyboru. Oba układy używają tego samego podpisu przycisku. Gdy zestawy istnieją, tekst i wymaganie wybrania zestawu są zachowane. Zasady przyznawania i miejsce w magazynie nie zmieniły się.

- Kompilacja Roslyn runtime i testów Editor: **PASS**, 0/0; `Artifacts/Local/CSharp-RewardNoLootLabel-20261108/`.
- Poza Unity wykonano **5/5** kontroli skompilowanych napisów i istniejącej logiki gotowości; rzeczywiste odebranie nagrody nie było częścią tej próby.
- Test Unity i faktyczna walka bez łupów, renderowanie obu orientacji oraz Player: **NOT RUN**. Brak nowego buildu.
