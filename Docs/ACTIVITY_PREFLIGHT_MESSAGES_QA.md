# Powody błędów przygotowania trasy

Baza `c964139f` (Draft PR #1146). Wyścig podaje przyczynę, gdy nie da się zmierzyć obrysu pojazdu albo wygenerowana trasa nie ma checkpointów. Tryb drogowy podaje przyczynę braku obrysu lub danych mapy. To te same istniejące ścieżki odmowy i czyszczenia; nie zmieniono generatora, walidacji ani zasad startu.

- Runtime Roslyn z istniejącymi referencjami Unity: **PASS**, kod 0; `Artifacts/Local/CSharp-ActivityPreflightMessages-20261108/runtime.log`.
- Wywołanie faktycznych ścieżek błędu, fizyczne collidery, dane Resources, GUI, Unity i Windows Player: **NOT RUN**. W późniejszym QA użyć wyłącznie izolowanego projektu i wywołać odpowiednie warunki błędu bez modyfikowania zapisów gracza.
- Nie przygotowano buildu.
