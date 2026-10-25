# RoboRougelike

Roguelike z budowaniem modułowego pojazdu: garaż 3D, zniszczalne części, salvage, sześć spotkań Gauntlet i mechaniki ruchu MOV-001–MOV-009.

## Uruchomienie

1. Pobierz repozytorium: `git clone https://github.com/C8LyNn/RoboRougelike.git`.
2. W Unity Hub dodaj katalog repozytorium. Zainstaluj **Unity 6000.3.14f1** (`d68c3f99a318`) z obsługą Windows Build Support.
3. Otwórz projekt, poczekaj na pobranie zależności UPM i import. Nie zmieniaj wersji silnika.
4. Otwórz **Assets/scene 1.unity**, włącz Play i rozpocznij przez interfejs gry. `SampleScene` pozostaje sceną szablonu.

Na gałęzi integracji OBJ-001 wybierz pojazd i **Obrona reaktora • przygotuj pojazd**, a w garażu **Broń reaktora**. Obroń reaktor600HP przed trzema falami2/2/3 przeciwników; zwycięstwo prowadzi do zwykłych nagród i garażu. Obrona jest też dostępna w garażu wyprawy. [Raport integracji, instrukcja i rzeczywiste wyniki QA](Docs/OBJ001_INTEGRATION_QA.md).

WASD — jazda; mysz — celowanie/strzał według interfejsu broni; Shift — booster; Space — jump; Q — blink; E — grapple/release; G — gyro; Z/C — obrót Omni/Hover. Urządzenia wymagają odpowiednich części zamontowanych w garażu. Można wybrać Wheels, Tracks, Omni lub Hover; Hover wyłącza siłę aktywnego zawieszenia i przywraca ustawienie po zmianie napędu.

## Testy i rozwój

Na Windows, z aktywowanym Unity i Python 3:

```powershell
.\Tools\CI\Run-Unity.ps1 -Task All
# Osobno: -Task EditMode, -Task PlayMode, -Task Build
python -m unittest discover -s Tools/CI/tests -v
python Tools/CI/validate_repository.py
```

Logi/XML trafiają do unikalnego `Artifacts/Local/<run>/`; build lokalny do `Builds/MOV001-009-Windows/` i osobnego buildu QA. Wyniki i buildy są ignorowane przez Git. Pełne EditMode obejmuje również testy wchodzące w Play Mode i może trwać ponad 20 minut.

[Workflow pracy](Docs/DEVELOPMENT_WORKFLOW.md) · [Konfiguracja CI/licencji](Docs/CI_SETUP.md) · [Raport migracji](Docs/GITHUB_MIGRATION_AND_CI_REPORT.md) · [Pochodzenie zasobów](Docs/THIRD_PARTY_NOTICES.md).

`main` to stabilna baza, `develop` to integracja; nowa praca przez `feature/*` i PR. MAP-SYSTEM R3–R6, VFX-001 v1–v5 oraz późniejsze WPN/DEF/MOV010–011 nie są częścią tej migracji.

Repozytorium publiczne nie nadaje automatycznie licencji open source. Nie dodano nowej licencji do autorskiego kodu gry. Zasoby szablonu Unity zachowują własną licencję.
