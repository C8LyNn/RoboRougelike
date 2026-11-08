# Komunikat zapisu w wyborze aktywności

Kod `61eb6a0da775d021c3421db1c52d9b11dcaeace8` pokazuje wynik operacji zapisu w otwartym oknie aktywności. Komunikat jest czyszczony przy ponownym otwarciu okna; wcześniejsze potwierdzenie nie jest przedstawiane jako wynik nieudanej próby. Format, ścieżka i sam mechanizm zapisu pozostają bez zmian.

Lokalna kompilacja C# kodu gry oraz kodu testów zakończyła się **exit 0/0**. Następnie wykonano bez Unity pięć wywołań rzeczywistej skompilowanej funkcji formatowania: sukces, nowy błąd, wcześniejszy sukces po nieudanej próbie, brak komunikatu i powtórzony błąd. Wynik: **5/5 PASS**. Nie odczytywano ani nie zapisywano stanu gry.

Dowody: [wyniki JSON](QA/OfflineActivitySave-20261108/results.json) i [skrypt](QA/OfflineActivitySave-20261108/Run.ps1); SHA-256 DLL `AC5375FC33D1E9DD5681EE1EE57E75B1E1B8E1A32D6BA6166C0A5D80279A7911`. Skrypt wymaga lokalnego skompilowanego DLL, pliku odpowiedzi i bibliotek w `Artifacts/Local/` oraz `Library/ScriptAssemblies`; sam nie zawiera wszystkich zależności.

**Unity EditMode/PlayMode, faktyczny zapis i wczytanie oraz wygląd okna w Windows Playerze: NOT RUN.** Przed ręczną próbą wykonaj kopię istniejącego checkpointu — pomyślny zapis może go zastąpić.
