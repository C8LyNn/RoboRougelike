# R4 modeli — próbny odbiór Unity

Stan: **BLOCKED — osie broni wymagają adaptera i dalszej walidacji.**
Ta gałąź zawiera wyłącznie próbny import i narzędzia edytora; nie podpina modeli do działającej gry.

## Źródła i baza

- Baza gry: `67250073760adf19052a0e654ce88d8743095496` (PR #978), potomek `2d67ef3` wskazanego w raporcie artystycznym.
- Broń #939: `23718ab9f3ecb579d224b928a25f713ddfdd8319`.
- Ruch #941: `c6b655d67c90ef91d7799460a0c0101756c4b975`.
- Klocki i raport #963: `03af5207143da76faeeb8004543a51f26c7f9c0d`.
- Silnik: Unity `6000.3.14f1`, URP `17.3.0`.
- Kontrola trzech oryginalnych manifestów SHA-256: **389 + 395 + 276 = 1060 plików zgodnych**. Wszelkie 155 skopiowanych FBX i JSON zachowało zgodność bajtową ze źródłami. Wśród 1309 plików `.meta` z GUID nie znaleziono duplikatów.

## Rzeczywiste wyniki

- **PASS — 49/49** FBX (8 broni, 11 ruchu, 30 klocków) wczytanych w Unity; ustawienia: skala 1, jednostki pliku, Bake Axis Conversion i Preserve Hierarchy; bez kamer, świateł i generowanych colliderów.
- **PASS — 49/49** pozycji punktów `Socket_Mount` w przestrzeni *całego prefabu*, z tolerancją 1 mm.
- **PASS — 19/19** zestawów animacji: nazwy, zakresy, pętle, utworzone `AnimationClip`, 24 FPS i czas trwania względem manifestów.
- **FAIL — 4 testy osi / przestrzeni**, szczegóły poniżej. Łączny wynik EditMode: **72 PASS, 4 FAIL**, kod procesu 2. Trzy pozytywne testy diagnostyczne tylko wypisują dane i nie oznaczają zgodności osi.
- **NOT RUN:** podłączenie modeli do istniejącej walki i napędów, wizualny odbiór materiałów URP, działanie animacji w rozgrywce, kształty klocków i ich kolizje, Save/Load, PlayMode oraz Windows Player z modelami.

## Rozbieżność osi

Po rzeczywistym wymuszonym imporcie `R4W_Basic` prefab ma transformację tożsamościową, ale jego węzeł `R4W_Basic_ROOT` ma lokalny obrót X około 90°. Port jest we właściwym miejscu w przestrzeni prefabu `(0, -0.26, 0)`, natomiast w przestrzeni samego `ROOT` jego składowa Y wynosi 0, a nie -0.26. Analogicznie dla `R4M_Wheel`: prefab `(0, +0.26, 0)`, składowa Y względem `ROOT` wynosi 0.

W próbce działa `Muzzle` znajduje się w przestrzeni prefabu przy `(0, 0.135, -0.66)`, choć opis wymaga kierunku +Z. Oś lokalna Y węzła `R4W_Basic_Yaw` nie jest osią pionową Unity: iloczyn skalarny z osią Y wynosi około 0. Test lokalnej osi i dwa testy pozycji względem `ROOT` pozostają celowo widoczne jako nieudane; nie obniżono ich tolerancji.

Prawdopodobną drogą jest adapter przestrzeni prefabu, który jawnie przeliczy oś obrotu i kierunek wylotu bez zmiany źródeł artysty ani fizycznych colliderów. Sam obrót całego modelu nie dowodzi poprawnego kierunku transformacji `Muzzle` ani działania yaw/pitch. Należy najpierw potwierdzić to na prawdziwych strzałach, a dopiero potem podłączyć modele do `R41PartGeometry` i `R3VehicleAssembly`.

## Ślady lokalne

- `C:/Users/kubap/Desktop/Prototypy/r4-model-intake-final.xml`, SHA-256 `861ebb7ee3849143a6fef3cd2f67538042d6543ec94dcbecfd905ace74eda408`.
- `C:/Users/kubap/Desktop/Prototypy/r4-model-intake-final.log`, SHA-256 `e44a27d83c8fdd644ec9c49ab7ea933b9b259a337fdc9cca5d286a646d9dbf2a`.
- `C:/Users/kubap/Desktop/Prototypy/r4-model-coordinate-diagnostics.xml` — współrzędne i obroty węzłów trzech próbek.
- Źródła nietknięte w `C:/Users/kubap/Desktop/Prototypy/R4_Model_Intake_Sources_03af520/`.

Model Automatic nadal nie jest dostarczony. Obecny wygląd pozostaje bez zmian. Żaden wynik nie stanowi akceptacji graficznej ani zezwolenia na merge.
