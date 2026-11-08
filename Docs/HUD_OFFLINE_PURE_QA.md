# Bezokienna kontrola wybranych funkcji HUD

Kod gry: `fa8fb66b56832f0558e393d115b34a4b8b897f66`. Wykonano 16 wywołań skompilowanych funkcji klasy `R1RunHUD` w PowerShell 7.6.9 / .NET 10.0.14, bez uruchamiania edytora Unity ani Windows Playera. Wynik: **16 PASS / 0 FAIL**.

| Zakres | Wynik |
| --- | --- |
| Ostrzeżenie seeda: puste, nieliczbowe, poza zakresem, minimum int, poprawne | 5/5 |
| Mapowanie Q/W/E i brak naciśnięcia na indeks zestawu | 4/4 |
| Opis stosu: same wraki, sprawny egzemplarz, blokada, pusty | 4/4 |
| Nazwa stosu bonusów dla T2, T3 i bez tytułu | 3/3 |

Uruchomiono rzeczywisty `Assembly-CSharp.dll` z lokalnej kompilacji C# dla powyższego kodu, przez refleksję, z wcześniej zbudowanymi lokalnymi zależnościami Unity i projektu. SHA-256 DLL: `AB34E5DD4FB951657955BDD3A04CEC41A34E62D2B12CC7E43A7CB0AE2F114903`. [Wynik JSON](QA/OfflineHudPure-20261108/results.json) oraz [skrypt](QA/OfflineHudPure-20261108/Run.ps1) są dołączone do raportu. Skrypt wymaga lokalnego DLL, pliku odpowiedzi kompilatora i bibliotek w `Artifacts/Local/` oraz `Library/ScriptAssemblies`; sam raport nie zawiera tych zależności i nie zapewnia samodzielnej reprodukcji na innym komputerze.

Pierwsza próba poza Unity zakończyła się brakiem biblioteki projektu. Następna próba z obsługą rozwiązywania zależności przez skrypt PowerShell spowodowała przepełnienie stosu **procesu pomocniczego PowerShell**; nie był to proces gry ani wynik testu Unity. Końcowa próba jawnie załadowała dostępne biblioteki i wykonała powyższe przypadki.

To nie jest wynik Unity EditMode/PlayMode, test wejścia Input System, renderowania, rozmieszczenia UI, rozgrywki ani potwierdzenie działania Windows Playera. Przygotowane dla tych ścieżek testy nadal mają status **NOT RUN**.
