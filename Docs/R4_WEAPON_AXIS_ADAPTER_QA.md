# R4 — adapter osi wizualizacji broni

Ten etap rozwija próbny import z PR #979 (`497c13dc053dd4f9d65ddb8b9bf58555069f5713`). Adapter **nie jest jeszcze podłączony do konstrukcji ani walki** i nie zmienia źródłowych modeli artysty.

`R4ArtWeaponAxisAdapter` ustawia widok w przestrzeni montażu Unity, kompensuje jednolitą skalę fizycznego klocka i tworzy potomny transform wylotu skierowany zgodnie z lufą. Zapisuje lokalne osie yaw/pitch względem rzeczywistej hierarchii FBX; EMP nie ma węzła pitch. Nie dodaje colliderów ani nie tworzy pocisków. Niejednolita lub zerowa skala rodzica jest odrzucana bez modyfikacji modelu.

## Wyniki

- Unity 6000.3.14f1 EditMode: **17/17 PASS**, exit 0. Osiem modeli × rodzic tożsamościowy lub obrócony i przeskalowany do 0,52; sprawdzono port, kierunek wylotu, yaw +90°, pitch +25°, połączony yaw -90°/pitch +25° oraz brak dodatkowych colliderów. Osobny test sprawdza odrzucenie niejednolitej skali.
- XML: `C:/Users/kubap/Desktop/Prototypy/r4-axis-adapter-verified.xml`, SHA-256 `e1f023728018f6843073fa52a242bf0398fb8281625b1eff1db00c88255c5124`.
- Przed poprawką kompensacji skali osiem przypadków dla klocka 0,52 **FAIL**: port przy -0,1352 m zamiast -0,26 m. Po poprawce cały zestaw przechodzi.
- Cztery testy surowej hierarchii z PR #979 pozostają **FAIL**: model źródłowy sam w sobie nadal ma odmienny układ osi. Wynik adaptera nie zmienia tych obserwacji.

## Pozostaje przed użyciem w grze

Podpiąć adapter do istniejącego celowania i fizycznego `Muzzle`, bez zastępowania systemu obrażeń, colliderów i pocisków; zweryfikować rzeczywiste trafienia, obrót zmontowanego pojazdu, ograniczenia broni i brak strzelania przez własny pancerz. Sprawdzić oddziaływanie klipów animacji na węzły sterowane kodem, wygląd URP i render w Windows Playerze. Model `Automatic` nadal nie istnieje i wymaga dotychczasowego wyglądu zastępczego.

**PlayMode, strzał z pojazdu, Windows Player z modelami i ręczny odbiór wizualny: NOT RUN.** To etap adaptera, nie akceptacja kompletnej integracji modeli.
