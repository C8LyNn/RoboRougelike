# COM-001 — statyczna walidacja i ograniczenia dowodu

**Status:** przygotowany prototyp C#; wymagana walidacja w Unity 6000.3.14f1 / R6.

## Zrealizowane kontrole (poza Unity)

- Każdy plik źródłowy C# został sprawdzony pod kątem bilansu nawiasów klamrowych (podstawowa kontrola statyczna; NIE kompilacja).
- Pliki `.asmdef` sparsowane jako poprawny JSON, nazwy assembly i referencje sprawdzone.
- Zliczono deklaracje testów NUnit: **52 testy `[Test]`**, **3 przypadki `[TestCase]`**, **22 testy `[UnityTest]`** = **77 przypadków testowych**. To liczba zadeklarowana w źródłach, nie liczba wykonanych testów.
- Zweryfikowano obecność rejestru części i interfejsów adapterów, odseparowanie zapisów HP od skanera, ograniczenia nieudanej aktywacji, warunki widoczności i saturację buforów.
- Sprawdzono sumy SHA-256 plików oraz CRC każdego wpisu ZIP po spakowaniu (osobny manifest).

## Czego NIE potwierdzono

- Brak Unity Editor w środowisku tworzenia: **nie uruchomiono Unity Test Runner**.
- Brak kompilatora Unity C#: nie potwierdzono pełnej zgodności kompilacyjnej.
- Brak rzeczywistych źródeł projektu R6: adapter R6 jest wyłącznie SZABLONEM, nie funkcjonującym połączeniem.
- Nie przeprowadzono realnego testu grafu połączeń ani native keyboard/mouse; pokazane role nie dowodzą faktycznej krytyczności konstrukcji.
- Nie przeprowadzono builda Windows, weryfikacji wydajności 180+ części i wpływu na UI.

## Zastrzeżenia techniczne

1. Każdy kontakt wymaga fizycznego **solidnego collidera** celu; obecny raycast ignoruje triggery.
2. Skan raportuje wyłącznie części jawnie widoczne; ukryte części tego samego wroga nie są pokazywane w HUD.
3. LOS typu `Physics.RaycastNonAlloc` wymaga właściwego maskowania warstw i odpowiedzi `OwnsCollider` dla wszystkich colliderów jednego bloku.
4. Przepełnienie buforów 128 lub rejestru 512 zgłasza brak bezpiecznego kontaktu i nie zużywa energii.
5. Ocena priorytetu jest **heurystyką**, nie analizą cięć grafu R6.
6. Adaptery muszą aktualizować właściciela, drużynę i związanie części po rozpadzie pojazdu.

**Bramka:** `IMPLEMENTED SOURCE / UNITY QA PENDING`.
