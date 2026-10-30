# B01-24 — ALLY-ALERT-RELAY-001 v1.0.0 / PASS 17 WAVE 08

## Rzeczywista funkcjonalność
Nowa, oddzielna od SQUAD-PRESSURE #429 i NOISE-INVESTIGATION #445 **mechanika przekazywania informacji o wykrytym graczu między członkami oddziału**. Oryginalne C#9 `AllyAlertRelay.cs`, oryginalne trzy scenariusze rozgrywki `radio_scenarios.json`, stabilny Unity GUID `e8b9b05a83cd44b8bff0138ed8116e74`, fizyczny zip źródeł z oryginalnym `.meta`, 48 rzeczywiście wykonywanych oryginalnych testów C# + 16 testów ZIP na Windows i Ubuntu.

**Źródłowy, kompilowalny game mechanic:** używa dosłownie niezmienionego `Assets/R3/R3EnemyArchetype.cs` Codexa (Git blob `66cd1ee734b129117b3753d1205469a0496cb673`), `System.Numerics.Vector2` dla XZ, bez UnityEngine zależności. Własne `AllyAlertRelay` tworzy niezależny stan dla 1–4 zarejestrowanych członków oddziału.

Każdy monotoniczny tick wywołuje `Tick(long, RadioMember[], SightMessage[])`. W `SightMessage` mogą znajdować się wyłącznie wiadomości od rzeczywiście **widzącego gracza** i działającego nadawcy, potwierdzone przez hosta (`HasVisualContact`); odbiorca uzyskuje starą pozycję celu, **nie** cudowną wizję w czasie rzeczywistym. Źródłowy filtr przestrzenny uwzględnia role Scout/Bruiser/Gunner/Flanker z prowizorycznymi promieniami (Scout=65, Bruiser=30, Gunner=52, Flanker=55), najkrótszy promień nadawcy i odbiorcy, opóźnienie 2 ticków, TTL **od oryginalnego zauważenia** 12 ticków. Zmienne CanTransmit/CanReceive modelują uszkodzony nadajnik/odbiornik lub zagłuszanie. Radio rejestruje tylko pojedyncze niepowtarzalne ID wydarzenia i przyjmuje najnowsze potwierdzone zgłoszenie; przy remisie wcześniejsze ID zapewnia niezależność od kolejności tablicy. Zagłuszenie w chwili odbioru usuwa oczekujący pakiet; odzyskanie odbiornika nie generuje starej wiadomości.

**Zastosowanie:** komunikacja taktyczna wroga, potencjalnie przydatna przy #445 (odgłosy/percepcja pojedynczego robota) i #429 (osobny scheduler akcji). Bez żadnego połączenia tych pakietów w źródłach: nie tworzymy jeszcze wspólnego adaptera ani kodu gameplay Codexa. Wszystkie 4 oryginalne archetypy kompilowane bez zmian; stan nie wpływa na broń, Rigidbody, obrażenia, nawigację, zasięg rzeczywistego LOS ani FPS. Wykorzystanie wyników `RadioIntel` do zachowań w grze wymaga przyszłego osobnego adaptera właściciela Codexa, prawdziwych ID jednostek, dostarczenia fizycznego LOS, pozycji gracza XZ, flag zagłuszania i monotonicznych ticków.

## Weryfikowalne testy

```bash
dotnet build Research/B01-24/P17/W08-ALLY-ALERT-RELAY-001/AllyAlertRelayCI.csproj -c Release --warnaserror
dotnet run --no-build -c Release --project Research/B01-24/P17/W08-ALLY-ALERT-RELAY-001/AllyAlertRelayCI.csproj
python -m unittest discover -s Research/B01-24/P17/W08-ALLY-ALERT-RELAY-001/Tools -p "test_receiver.py" -v
python Research/B01-24/P17/W08-ALLY-ALERT-RELAY-001/Tools/build_receiver.py --repo . --out b01-24-w08-source-zip
```

C# testuje latencję, wygasanie, źródłowe R3 role i promienie, przesył tylko prawdziwego wizualnego zgłoszenia, zagłuszenie nadajnika/odbiornika, śmierć, deduplikację, kolejność, 4-osobowy oddział, izolację squadów, zewnętrzne mutacje danych/policy, błędne ticki, niemożliwe koordynaty, obce ID, duplicate events. Trzy autorskie end-to-end scenariusze (16 ticków, 32 stany odbiorców) mają źródłowo sprawdzany powtarzalny SHA256.

16 negatywnych Python testów fizycznie weryfikuje ZIP, SHA256 bajtów, source Git blobs, `.meta` i identyczną produkcję cross-OS, blokując nadpisanie istniejącego katalogu, symlink, błędny manifest oraz dryf oryginalnego Codex R3. ZIP mieści dokładnie dwa Unity source assets: `Assets/B0124AllyAlertRelay/AllyAlertRelay.cs` + `Assets/B0124AllyAlertRelay/AllyAlertRelay.cs.meta`, nie jest `.unitypackage`, prefababem, sceną ani natywnym Unity buildem.

## Ownership / QA / Codex handoff / rollback

Wyłączny właściciel producenta: **B01-24**. Zakres **tylko nowy** `Research/B01-24/P17/W08-ALLY-ALERT-RELAY-001/**` i własny workflow CI; nie zmieniano `Assets/**`, źródeł Codexa, fizyki, CHL, PARTSIZE, MAPGEN i dotychczasowych paczek. B01-40 niezależnie sprawdza dokładny SHA/manifest/GUID/zip/scope. B01-60 wykonuje odrębny fuzz i negatywne testy latency, reported visual permission, death/jamming, ordering i zakresu, natywne Unity tylko po osobnym uprawnieniu. B01-00 sam nadaje ewentualną ofertę na #79, Codex sam jawnie potwierdza ACK i jest jedynym właścicielem adaptera dla istniejącego `P12PipelineSafeEnemyTurret`.

W przyszłej, autoryzowanej gałęzi Codexa: opcjonalny import dwóch plików C# i .meta do jednorazowego `Assets/B0124AllyAlertRelay/`, oddzielny adapter odczytu prawdziwych `RadioMember` z Unity, tylko rzeczywista LOS i realne EMI/jamming (bez wymyślania sygnałów), sprawdzenie trasy/kolizji/percepcji. Wyłączenie adaptera musi zachować oryginalny system przeciwników. **Rollback:** wyłączyć adapter i usunąć jedynie nowe dwa importowane pliki po kontroli lokalnych zmian; nigdy nie usuwać ani przepisywać kodu Codexa.

Status niezależnie od sukcesu .NET: `PRODUCER_SOURCE_G0_TESTED; INDEPENDENT_QA_PENDING; UNITY_EDITOR/PLAYMODE/PHYSX/PLAYER=NOT_RUN; GAME_NOT_INTEGRATED; CODEX_ACK_PENDING; NO_MERGE`. Parametry role radius, latency i TTL są **prowizoryczne**, a nie finalne zatwierdzone dane balansu.
