# Lekka stylizacja — odwracalny prototyp URP

Baza: PR #1229 / `4c4d28f511d73dcb72e0c714fc2967b189d14706`, Unity 6000.3.14f1, URP 17.3.0. Zmiana jest domyślnie wyłączona. Nie zmienia materiałów, świateł, modeli, tekstur, paczek ani istniejących profili.

## Konfiguracja i sposób działania

Sceną gry jest `Assets/scene 1.unity`; kamera główna nie ma zapisanego komponentu `UniversalAdditionalCameraData`, a domyślna wartość URP dla post-processingu to wyłączony. Profil `Assets/Settings/DefaultVolumeProfile.asset` ma neutralny ColorAdjustments, tonemapping None i bloom o intensywności 0. Istnieje też `SampleSceneProfile` dla innej sceny; zawiera on bloom i winietę. Istniejący `PC_Renderer` ma SSAO; nie jest on edytowany. Bieżący HUD używa IMGUI, poza obrazem z kamery.

Osobny profil `Resources/VisualPrototype/LekkaStylizacja` zawiera tylko ColorAdjustments (kontrast +4, nasycenie +8) i WhiteBalance (temperatura +2). Waga Volume jest regulowana od 0 do 1; brak nowych efektów bloom, ziarna, rozmycia, konturów i posterizacji. Nie zmienia się tonemappingu istniejących profili; korekta tonu polega na balansie bieli. Jeśli kamera wcześniej miała post-processing wyłączony, na czas działania filtra korzysta wyłącznie z warstwy prototypu, aby nie obudzić istniejących efektów sceny. Jeśli był włączony, wcześniejsza maska jest zachowana i uzupełniona o warstwę prototypu. OFF i 0% przywracają pierwotną maskę oraz stan post-processingu. Warstwa wybierana jest spośród warstw 8–31 niezajętych przez inne Volume; jeżeli brak wolnej, filtr nie aktywuje się i pokazuje komunikat.

## Sterowanie i usunięcie

- **F8** — przełącza filtr w dowolnej chwili. Po uruchomieniu jest wyłączony, a jego siła wynosi 0%; aby zobaczyć efekt, trzeba włączyć filtr i zwiększyć siłę w panelu.
- **Esc → F10** — w pauzie pokazuje panel z przełącznikiem i suwakiem 0–100%; zamknij F10 lub wznów Esc. Regulacja w pauzie nie wywołuje strzału ani montażu.
- Aby wyłączyć bez zmian w projekcie: F8 albo suwak 0%. Stan prototypu nie jest zapisywany i po restarcie gry wraca do OFF.
- Aby całkowicie usunąć eksperyment: usuń `Assets/R1/LightStylePrototype.cs` z jego `.meta`, katalog `Assets/Resources/VisualPrototype` i jego `.meta`, trzy pliki testowe `LightStyle*Tests.cs` z `.meta`; usuń dodanie komponentu w `R1Bootstrap.cs` oraz wywołanie `DrawPausePanel` w `R1RunHUD.cs`. Można też odwrócić commit tego PR. Istniejące profile nie wymagają przywracania.

## Wyniki

- **PASS — cztery celowane EditMode**, w tym testy przechodzące w Play Mode: profil bez Bloom, OFF/0% odtwarzają stan i maskę kamery, wcześniej włączony post-processing pozostaje włączony, istniejący profil Bloom nie jest mutowany, restart sceny i renderowanie par. Wynik: [XML](Evidence/celowane-testy.xml), kod procesu 0.
- **PASS — porównanie kamery 1920×1080:** pary OFF/ON z tej samej nieruchomej kamery dla rzeczywistego garażu, areny i stanu walki. W próbie walki wywołano strzał poprzez rzeczywisty system zamontowanej broni. Przebieg sterowany API, nie ręczna rozgrywka. Zatrzymano symulację na czas każdej pary. RenderTexture przechwytuje obraz kamery **bez IMGUI/HUD**; czarne marginesy w garażu wynikają z częściowego viewportu. W każdym z trzech ujęć obraz 0% był identyczny bajtowo z OFF. Istotna różnica OFF/ON jest widoczna w plikach; ocena artystyczna pozostaje subiektywna.
- **Częściowo — czytelność:** w tych ujęciach widoczne są ciemne partie tła, żółte obręcze ostrzeżeń i jasne cyjanowe pole; nie wykonano osobnej kontroli bardzo ciemnej lokacji, eksplozji ani HUD-u w widocznym Playerze.
- **PASS techniczny / ograniczenie pomiaru:** RTX 5080, Direct3D 11, Unity Editor batch; garaż, zamrożona kamera, siła 60%, po 8 naprzemiennych pomiarów po rozgrzewce. Mediana `Camera.Render + synchroniczny GPU readback`: 1080p OFF **3,59 ms**, ON **3,67 ms**; 4K OFF **8,59 ms**, ON **8,58 ms**. Wynik obejmuje odczyt danych i synchronizację, a nie izolowany koszt GPU, rzeczywistą liczbę FPS lub miarodajny benchmark gry; różnice są małe względem rozrzutu. [Surowe dane](Evidence/render-readback-samples.csv), [środowisko](Evidence/render-readback-environment.txt), [XML pomiaru](Evidence/pomiar-test.xml), kod procesu 0.
- **NOT RUN:** widoczny Windows Player i ręczna ocena HUD/grywalności, samodzielny pomiar czasu GPU na klatkę, pełna regresja i testy innych rozdzielczości. Baza #1229 miała wcześniej 181 błędów EditMode; nie zostały uznane za naprawione tą zmianą.

## Obrazy z kamery

| Scena | OFF | ON 60% |
|---|---|---|
| Garaż | [OFF](Evidence/garaz-OFF.png) | [ON](Evidence/garaz-ON.png) |
| Arena | [OFF](Evidence/arena-OFF.png) | [ON](Evidence/arena-ON.png) |
| Stan walki po wywołaniu strzału | [OFF](Evidence/walka-stan-OFF.png) | [ON](Evidence/walka-stan-ON.png) |

Porównania obok siebie (pomniejszone kopie oryginalnych obrazów): [garaż](Evidence/garaz-porownanie.png), [arena](Evidence/arena-porownanie.png), [walka](Evidence/walka-stan-porownanie.png).
