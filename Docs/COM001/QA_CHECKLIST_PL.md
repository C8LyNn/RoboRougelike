# COM-001 — plan weryfikacji

**Uwaga: testy zostały NAPISANE, lecz NIE były wykonywane w Unity w środowisku tworzenia paczki.** Statyczne kontroly nie są wynikami NUnit ani buildu Windows.

## G0 — integralność i import

- [ ] SHA-256 ZIP i manifest zgadzają się z dostarczoną paczką.
- [ ] Kod Unity 6000.3.14f1 kompiluje się bez błędów.
- [ ] Dwie assemblies testowe są rozpoznane przez Unity Test Runner.
- [ ] Żaden istniejący GUID/scene/PackageSetting nie został niepotrzebnie zmieniony.

## G1 — Edytor / matematyka

- [ ] Stożek ogranicza dystans, kierunek i kąt.
- [ ] Priorytet funkcji jest tylko heurystyką.
- [ ] Cooldown, czas ekspozycji i config clamps działają.
- [ ] Rejestr deduplikuje OwnerKey+PartKey, deregistracja działa.
- [ ] Opcjonalny magazyn energii odmawia przekroczenia budżetu.

## G2 — PlayMode / LOS / identyfikacja

- [ ] Widoczny pojedynczy blok w stożku jest odnaleziony.
- [ ] Ściana zasłaniająca blok całkowicie go ukrywa.
- [ ] Części za rdzeniem/pancerzem nie są przeciekane do HUD.
- [ ] Wiele colliderów jednego bloku daje jeden wynik.
- [ ] Sojusznicy i własne bloki nie są raportowani.
- [ ] Osobny element bez połączenia nie jest raportowany.
- [ ] Własny collider (również poza główną hierarchią przez adapter) nie blokuje skanu.
- [ ] Sensor w obcej bryle nie prześwietla przeszkód.
- [ ] Brak celu/energii nie pobiera zasobów, nie blokuje skanu.
- [ ] Wyniki wygasają, cooldown liczy się osobno.
- [ ] W scenie 180 części brak degradacji i wyjątków.
- [ ] Bufory LOS po osiągnięciu limitu odmawiają zamiast ujawniać ukryty cel.
- [ ] Osobno sprawdzone warstwy i triggery R6.

## G3 — Integracja

- [ ] Odczyt HP, ID i kategorii pochodzi z R6, nie proxy demo.
- [ ] `StructuralImportance` jest z rzeczywistego grafu albo wynosi zero.
- [ ] Skaner jest normalną montowalną częścią, jeśli katalog wspiera nową kategorię.
- [ ] Zniszczony skaner nie ujawnia nowych informacji.
- [ ] Widoczne kontury/UI nie pokazują zakrytych bloków ani całego nieprzeskanowanego pojazdu.
- [ ] Poligon w trybie isolated copy zachowuje magazyn i Złom garażu.
- [ ] Bazowe testy R6 i testy COM-001 przechodzą w aktualnej wersji projektu.
- [ ] Wynik retail Windows build, testy użytkownika i osobny stan crasha QA `Application.Quit()` są udokumentowane.

## G4 — Status

- `READY`: kompilacja, wykonane testy, działający input/HUD i prawdziwy adapter R6.
- `WAIT`: brak kodu projektu, konflikt architektury, niezaliczone testy, nieprzetestowana integracja lub brak dowodów.
