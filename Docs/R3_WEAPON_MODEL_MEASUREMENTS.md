# Pomiary modeli broni przed powiększeniem

Pomiary wykonano w Unity 6000.3.14f1 na rzeczywistych modelach używanych przez `R4WeaponArt`, przy siatce 0,55 m. Dołączony test tworzy rzeczywisty payload i adapter osi, mierzy renderery nieruchomej podstawy oraz cały model dla kątów 0°, 90°, 180° i 270°. Wyniki są osiowymi obwiedniami renderingu, nie są colliderami ani gwarancją bezpiecznego obrotu dla wszystkich kątów i pochyleń.

| Model | Szerokość nieruchomej podstawy | Największe wysunięcie modelu od osi przy poziomej lufie |
| --- | ---: | ---: |
| Basic | 0,513 m | 0,624 m |
| Automatic | 0,513 m | 0,664 m |
| Heavy | 0,515 m | 0,565 m |
| Shotgun | 0,513 m | 0,414 m |
| Railgun | 0,513 m | 0,807 m |
| Beam Cutter | 0,513 m | 0,433 m |
| Rocket Launcher | 0,513 m | 0,376 m |
| Rotary Saw | 0,513 m | 1,755 m |
| EMP | 0,513 m | 0,256 m |

Wyloty strzałów również są w hierarchii modeli: np. 0,660 m dla Basic, 0,700 m dla Automatic i 0,840 m dla Railgun przy zerowym obrocie. Piła używa własnego promienia roboczego i wymaga oddzielnej kontroli. Przed zmianą rozmiaru broni trzeba spójnie przeskalować model względem punktu montażu, fizyczny obrys części, miejsce obrotu i wylot pocisku; istniejący zapis wymaga wersji geometrii. Nie należy zamieniać całego ruchomego obrysu w stałe pola konstrukcji.

Wykonany test EditMode: **1/1 PASS**, obejmuje 9 modeli × 4 pozycje obrotu (`weapon_bounds_final.xml`, `weapon_bounds_final.log` w lokalnym katalogu QA). Test nie sprawdza pełnych 360°, pochylenia, rzeczywistej gry, kolizji ani przyszłych większych wersji modeli. Nie dokonano powiększenia broni w tym etapie.
