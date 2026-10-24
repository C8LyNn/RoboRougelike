# VFX-001 v5 — QA checklist

## Kompilacja i materiały
- [ ] Unity 6000.3.14f1 z URP, brak C# i shader compile errors.
- [ ] Materiały `VFX001_HeatGlow` i wcześniejsze wygenerowane, przechodzą do builda Windows.
- [ ] Demo scena otwiera się, nowe przyciski v5 widać po przewinięciu GUI.

## Testy izolowane
- [ ] Poprzednie testy EditMode i PlayMode.
- [ ] Nowe testy `VfxV5ImpulseEnvelopeTests`.
- [ ] Nowe testy `VfxV5CosmeticTransformTests`.
- [ ] Nowe testy `VfxV5RuntimeTests`.
- [ ] Test integracji `VfxV5OrchestrationTests`.

## Funkcjonalne
- [ ] Uszkodzona osłona drży i wraca do pozycji spoczynkowej.
- [ ] Impuls kamery jest wyciszany dystansem.
- [ ] Kilka impulsów kamery jest ograniczonych budżetem.
- [ ] Po wyłączeniu komponentów dzieci wracają do spoczynkowej pozycji/rotacji.
- [ ] Rozżarzone miejsce trafienia zanika po czasie.
- [ ] Liczba nakładek żaru na moduł jest ograniczona.
- [ ] Zniszczenie rdzenia daje wyraźniejszy feedback niż zwykłe trafienie.
- [ ] Brak zmiany fizycznych Rigidbody/colliderów/HP/połączeń.
- [ ] Nie nakładaj dwóch komponentów modyfikujących pozycję tego samego visual child.

## Wydajność, build i dowody
- [ ] Test przeciążenia przy wielu trafieniach i dużej liczbie bloków.
- [ ] Test PlayMode, test EditMode i Windows build wykonane z zachowanymi logami.
- [ ] Raport z testów manualnych faktycznej gry, nie tylko showcase.
- [ ] Test grafiki Low/Medium/High i brak przecieku obiektów po resetach.
- [ ] Przegląd dyfów i PR bez merge przed autoryzacją.
