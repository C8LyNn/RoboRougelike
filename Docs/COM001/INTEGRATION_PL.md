# COM-001 — integracja techniczna i kontrakty

## Architektura

`StructuralScanController.TryScan()` to metoda jawnej aktywacji (bez wiązania z klawiaturą). `StructuralScanRegistry` to lokalny rejestr adapterów części (brak `FindObjects` w pętli). `IStructuralScanPart` udostępnia **tylko odczyt** R6. `StructuralScanMath` i `StructuralScanClock` mają prostą logikę testowalną bez sceny. `StructuralScanPartProxy` / `StructuralScanEnergyStore` są przykładami, nie gotowym mostem do R6.

## Domyślna konfiguracja

| Pole | Wartość | Cel |
|---|---:|---|
| rangeMeters | 24 m | Maksymalna odległość części od sensora |
| fullConeDegrees | 75° | Pełny kąt stożka |
| cooldownSeconds | 8 s | Odnowienie po skutecznym skanie |
| displaySeconds | 5 s | Ważność kontaktów |
| activationEnergy | 0 | Opcjonalny koszt; 0 wyłącza wymaganie energii |
| maxRevealedParts | 16 | Najważniejsze spośród **widocznych** części |
| maxRegisteredParts | 512 | Górny limit liczby zarejestrowanych adapterów; fail closed |
| obstructionMask | All | Warstwy kolizji zakrywających / skanowanych brył |

Dla R6 należy przetestować 180 części i większą liczbę przeciwników, ustawiając limit rejestru adekwatnie do areny. Przekroczenie limitu blokuje skan bez kosztu. Zapytania fizyczne `RaycastNonAlloc` oraz `OverlapSphereNonAlloc` mają bufor 128 wyników; przepełnienie także blokuje dany kontakt.

## Przykładowe API

```csharp
using RoboRougelike.Mechanics.StructuralScan;

var controller = gameObject.AddComponent<StructuralScanController>();
controller.Bind(sensorMuzzleTransform, myVehicleRoot, myOwnerId, myTeamId,
    sceneRegistry, optionalEnergyMonoBehaviour, optionalOwnCollisionFilter);

// On user action only (R6 Input System):
if (controller.TryScan())
{
    foreach (StructuralScanContact c in controller.Contacts)
    {
        // c.OwnerKey, c.PartKey, c.Type, c.Position, c.HealthFraction, c.Priority
        // Prefer user HUD, NOT R6 combat damage or additional HP component.
    }
}

// On emitter destroyed/detached:
controller.SetOperational(false);
```

`Bind` resetuje poprzednie wyniki; `SetOperational(false)` usuwa widoczny skan. Normalny cooldown pozostaje po dezaktywacji modułu: nie można resetować go przez naprawę/ponowny montaż bez osobnej decyzji designu.

## Jak mapować system bloków R6

Stwórz **adapter** `MonoBehaviour, IStructuralScanPart` albo rejestrator grafu R6 zwracający klasy adapterów. Nie dodawaj nowego `PartHealth` ani drugiego rejestru konstrukcji. Podłącz:

- `OwnerKey` — stabilny identyfikator aktualnego właściciela/grupy pojazdu, **nie** identyfikator pojedynczego collidera; wartość `0` jest zarezerwowana.
- `TeamKey` — istniejący identyfikator drużyny, przeciw sojusznikom.
- `PartKey` — **unikalne w ramach OwnerKey, niezmieniane** przez collider, zgodne z R6 ID części.
- `IsAttached` — aktualna przynależność do żywej konstrukcji (zmiana właściciela wymaga ponownej rejestracji).
- `HealthFraction` — R6 currentHP / maxHP bez kopiowania liczników i systemu obrażeń.
- `StructuralImportance` — tylko jeśli istnieje realny wynik algorytmu grafowego; inaczej 0.
- `IsFunctional` — czy moduł sprawny; jego stan R6/EMP.
- `ScanPoint` — widoczny punkt z colliderem części na torze promienia. Nie podawaj punktu za osłoną, jeśli wtedy ray nie mógłby dotrzeć do fizycznej bryły.
- `OwnsCollider` — `true` dla **wszystkich** colliderów tego bloku i jego dzieci (wiele colliderów jest jednym kontaktem).

**Krytyczne**: aktualny LOS akceptuje collidery **nietriggerowe** na warstwach `obstructionMask`. Jeśli R6 ma bryły części tylko jako trigger, napisz specjalny adapter widoczności albo ustaw osobną fizyczną reprezentację skanowania; nie przełączaj globalnie kolizji wszystkich triggerów. Użytkownik musi otrzymać ostrzeżenie o tym ograniczeniu.

Rejestruj moduły zdarzeniowo przy spawn/mount, wyrejestruj przy despawn/detach; przy zmianie OwnerKey najpierw wyrejestruj, zmień ID i ponownie zarejestruj. Rejestr nie jest odpowiedzialny za cykl życia części R6.

## Wykrywanie i ochrona przed przeciekami informacji

1. Przeciwnik: `OwnerKey != scannerOwnerKey`, `TeamKey != scannerTeamKey`, `OwnerKey != 0`, `IsAttached`.
2. Stożek i zasięg: sprawdzane w przestrzeni świata 3D dla każdego widocznego punktu.
3. LOS: ignorowane własne collidery (`ownerRoot` i/lub `IStructuralScanSelfFilter`), solidne przeszkody muszą się znaleźć za częścią, inaczej blokują kontakt.
4. Najbliższy **widoczny** cel wybierany deterministycznie według kwadratu odległości, przy remisie według OwnerKey.
5. Z wybranego pojazdu w wynikach znajdują się tylko osobno widoczne części, posortowane wg priorytetu i PartKey. Ukryte elementy **nie są analizowane w HUD**.
6. Informacje to snapshot. Nie odświeżaj ich na bieżąco bez nowego skanu. Po 5 s znikają.
7. Brak widocznego celu / brak energii / przekroczony limit / brak sensora: **brak kosztu i cooldownu**.

Jeśli sensor wystartuje wewnątrz obcej bryły, krótki overlap przy początku promienia zablokuje skan zamiast przenikać przez przeszkodę. Saturacja buforów = fail-closed.

## Priorytet taktyczny

Wysoki score zyskują: rdzeń (90), podpora (80), uzbrojenie (72), mobilność (64), osłony (60), narzędzia (45), pancerz (20), unknown (10). Modyfikatory: ważność grafowa (0..35), uszkodzenie (0..12), nieaktywność (-20). Score jest heurystyką [0..150], NIE prawdopodobieństwem odłączenia lub gwarantowanym krytycznym punktem. Zalecamy HUD `potencjalnie ważny moduł` zamiast `zniszcz to, aby odpadła broń`.

## Integracja z UI / wydajność

Unikaj `OnGUI` w produkcji. Widok garażu/poligonu R6 powinien używać dostępnego UI i eventu `ResultsChanged` oraz czasu `DisplayLeft`. Wyniki pochodzą z `Contacts`, a nie z grafu całego wroga. Zaplanuj throttling animacji/pulsów i test sceny ze 180 częściami. Mechanika wykonuje zapytania fizyczne wyłącznie po aktywacji, nie w każdej klatce.

## Główne ryzyka

- Odłączona grupa ma niezaktualizowany OwnerKey / TeamKey -> fałszywe kontakty.
- Środek bloku `ScanPoint` nie jest trafialny przez collider -> brak wyniku.
- `obstructionMask` nie obejmuje warstw bloków i przeszkód -> wynik fail-closed (nie wykryto bryły).
- Zbyt wiele colliderów jednocześnie -> ostrożna odmowa zamiast przenikania.
- Czas wyświetlania wygasa niezależnie od czasu odnowienia.
- Obce collidery trigger nie są automatycznie traktowane jako przeszkody ani cele.
- W aktualnej wersji wybierany jest jeden **najbliższy widoczny pojazd**, nie zawsze obiekt pod środkiem celownika. Jeżeli gameplay wymaga skanowania celu wskazanego kursorem, Codex powinien dodać jawny parametr OwnerKey celu, bez ujawniania niewidocznych bloków.
