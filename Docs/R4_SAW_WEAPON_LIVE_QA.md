# R4 — model piły i fizyczna tarcza

Piła `RotarySaw` na pojeździe gracza może używać dostarczonego modelu i węzła `R4W_RotarySaw_Blade`. Istniejący `RRLSawBinding` obraca tę tarczę z prędkością swojego rzeczywistego silnika. Brak modelu lub brak wymaganego węzła pozostawia dawny wygląd.

Fizyka, obrażenia, zużycie energii, detekcja kontaktu i punkt cięcia pozostają w istniejącym systemie. Walidacja garażu nadal używa dotychczasowego środka i promienia. Źródłowy FBX nie jest modyfikowany.

**Ważna decyzja tymczasowa:** źródłowa tarcza ma promień 0,3 m, natomiast istniejący kontakt i podgląd garażu używają 0,9 m. Podczas działania powiększana jest sama widoczna tarcza i wyrównywana do istniejącego środka cięcia. Może to wpływać na proporcje modelu i połączenie z ramieniem — potrzebna jest ręczna ocena oraz ewentualna decyzja artystyczna. Nie jest to dowód zatwierdzenia wyglądu.

## Wyniki

- Unity 6000.3.14f1, celowane testy EditMode wchodzące do trybu gry: **5/5 PASS**, exit 0. XML `C:/Users/kubap/Desktop/Prototypy/r4-saw-art-axis-regression.xml`, SHA-256 `c6013d0b75fbb1418c855cf81b80aa3cb1ad69bc04dbe3d31430610134a3cec0`.
- Sprawdzono fizyczny pojazd, widoczną tarczę o średnicy 1,8 m, zgodność środka i osi z punktem cięcia przy obrocie, rzeczywisty wzrost obrotów silnika, cięcie prawdziwego celu, blokadę na ścianie, zachowanie ciepła, dotychczasowy podgląd oraz zakaz kolizji piły i własnego pancerza w garażu.
- **NOT RUN:** Windows Player dla tego commita, ręczna ocena proporcji i animacji, pełne zestawy EditMode/PlayMode oraz wydajność wielu pił.

Wraz z #983 daje to osiem podłączonych modeli broni na pojeździe gracza. `Automatic` nadal nie ma modelu i pozostaje przy dotychczasowym wyglądzie; modele ruchu, przeciwników oraz podgląd ghosta nie zostały tu podłączone. Wcześniejszy build opisany w `R4_BASIC_WEAPON_LIVE_QA.md` zawiera tylko `Basic` i nie zawiera tej zmiany. Historyczny `C0000005` automatycznego zamykania Playera pozostaje niewyjaśniony.
