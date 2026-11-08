# Komunikat po montażu części ze stosu

Baza `36a5aee6` (Draft PR #1142). Po udanym montażu egzemplarza z magazynu garaż wyjaśnia, czy kolejna sprawna część jest wybrana, czy pozostały tylko wraki wymagające naprawy, czy stos jest pusty. Istniejący wybór najwyższego HP i zakaz automatycznego zakupu są zachowane.

- Kompilacja Roslyn runtime i testów Editor: **PASS**, kody 0/0; `Artifacts/Local/CSharp-StackDepletion-20261108/`.
- Wywołano skompilowaną funkcję komunikatu poza Unity: **3/3** przypadki.
- Test Unity montażu ostatniego sprawnego egzemplarza przy pozostałym wraku jest przygotowany, ale **NOT RUN**. Właściwy montaż, czytelność tekstu i Windows Player: **NOT RUN**.
- Nie przygotowano buildu.
