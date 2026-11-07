# R4 — kolejne modele broni na pojeździe

Na bazie #982 podłączono modele `Heavy`, `Shotgun`, `Railgun`, `RocketLauncher`, `BeamCutter` i `EmpEmitter` do istniejących fizycznych części i systemów walki. Razem z `Basic` działa siedem modeli na pojeździe gracza. Stary collider, HP i mechanika broni pozostają istniejące; wylot pocisku jest powiązany z widocznym modelem. Brak modelu lub niepowodzenie adaptera zostawia dotychczasowy wygląd.

`Automatic` nadal nie ma modelu źródłowego. `RotarySaw` jest celowo pozostawiony przy istniejącej geometrii: jego tarcza wymaga bezpiecznego podłączenia do aktualnego silnika piły. Ten etap nie podmienia modeli przeciwników, ghosta garażu ani części ruchowych.

## Wyniki

- Unity 6000.3.14f1, wybrane testy EditMode wchodzące do trybu gry: **12/12 PASS**, exit 0. XML `C:/Users/kubap/Desktop/Prototypy/r4-ranged-art-regression.xml`, SHA-256 `06982b6b2de7b225913f612bbd3370f686046cba6d610ce95b44ff3b02d22029`. Testy objęły rzeczywiste trafienia shotguna, railguna, rakiety, wiązki i Basic; działanie EMP; koszt energii i T2; własną osłonę; fizyczne collidery i fallback Automatic.
- Dodatkowo po rozszerzeniu testów: **2/2 PASS**, exit 0. XML `C:/Users/kubap/Desktop/Prototypy/r4-ranged-art-final.xml`, SHA-256 `a7e5d2ccbac496a1093ebb00ad0e3d30e54baca0d24993c40e131ea060620b6f`. Sprawdza wszystkie sześć nowych modeli i ich celowanie po obróceniu mocowania, shadery materiałów URP oraz realny strzał Heavy i jego koszt energii.
- **NOT RUN:** Windows Player z tym etapem, ręczna ocena wyglądu i walki, render materiałów na docelowej konfiguracji, animacje i wydajność przy wielu modelach, pełne EditMode/PlayMode. Komponent piły nie jest objęty podmianą.
- Surowe cztery diagnostyczne testy osi z #979 pozostają **FAIL**; adapter z #980 koryguje przestrzeń instancji. Nie oznacza to zatwierdzenia pełnej paczki wizualnej.

Build Windows opisany w `R4_BASIC_WEAPON_LIVE_QA.md` pochodzi z wcześniejszego commita i zawiera tylko model Basic, nie ten etap. Błąd `C0000005` przy automatycznym zamknięciu Playera pozostaje nierozwiązany.
