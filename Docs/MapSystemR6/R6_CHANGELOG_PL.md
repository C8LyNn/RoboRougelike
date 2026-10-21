# MAP-SYSTEM R6 — zmiany względem R5

- **R6-J01:** planowanie drogi skrętu pomiędzy dwiema krawędziami grafu zorientowanymi w kierunku jazdy; kierunki działają również dla odcinków przejeżdżanych wstecz względem ich definicji;
- **R6-J02:** próbkowany minimalny promień i maksymalne nachylenie drogi manewrowej; za ciasny manewr oznaczany jako niemożliwy według podanego profilu;
- **R6-J03:** rysowanie w Scene View kandydata przejazdu. **Brak generowania dodatkowych MeshColliderów** w celu uniknięcia dublowania fizycznych powierzchni węzłów;
- **R6-D01:** rejestrator telemetrii `Rigidbody`: dystans, żądany ruch, przechył, zdarzenia bez postępu;
- **R6-D02:** jawny ręczny eksport evidence JSON bez ingerowania w pliki projektu;
- **R6-QA:** testy jednostkowe planisty i telemetrii, testy offline, raport przykładowej mapy;
- **Zgodność:** istniejące assembly names i folder `MapSystemR4` zachowane dla importu bez zmiany GUIDów.

**Znane ograniczenia**
- krzywa planowanego manewru nie jest jeszcze połączoną fizycznie nawierzchnią colliderów;
- geometria pojedynczych odcinków R5 nadal może mieć uskoki na styku z padami i w praktyce zablokować przejazd;
- klasa pasywnej telemetrii nie steruje pojazdem i nie potwierdza wykonania trasy;
- walidacja promienia łuku jest próbkowana, nie jest dowodem ograniczeń sił i przyczepności;
- żaden z testów Unity i żaden build nie zostały uruchomione w tej sesji.

Dodatkowo R6 zawiera `MapR6RouteValidator` (sprawdza kolejne skręty) oraz korektę `lower_road.padWidth` 12 → 18 m w mapie v6. Szczegóły: `Docs/R6_GEOMETRY_FIX_PL.md`.

- **R6-G1b:** `MapR6JunctionClearanceProbe` sprawdza obwiednię pojazdu na wyliczonym łuku przy jawnej masce przeszkód; brak maski to InvalidConfiguration.
