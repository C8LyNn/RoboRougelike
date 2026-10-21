# Pochodzenie i licencje zasobów

Audyt migracji z 2026-10-21 obejmuje cały publikowany katalog Assets, Packages i ProjectSettings oraz nowe pliki narzędzi/dokumentacji. Brak modeli, audio, paczek Unity Asset Store, DLL lub archiwów dostawców w publikowanych Assets. Geometria gry i materiały są generowane przez istniejące skrypty z prymitywów Unity. Skrypty Pack 12 oraz MOV001–009 pochodzą z dostarczonych materiałów projektu; nie mają nagłówków ograniczających redystrybucję. Nie nadano im dodatkowej licencji.

`Assets/TutorialInfo` i bazowe ustawienia/sceny URP pochodzą z szablonu **com.unity.template.urp-blank 17.0.14**. Jedyny PNG (`TutorialInfo/Icons/URP.png`, 24069 B) jest bajtowo identyczny z plikiem w zainstalowanym szablonie Unity. [Oryginalna nota szablonu](Licenses/Unity-URP-Template.md) została zachowana; obowiązuje [Unity Companion License](https://unity.com/legal/licenses/unity-companion-license), dla projektów tworzonych w Unity. Sama nota nie zmienia praw do autorskiej gry. Nie traktować szablonu jako niezależnego pakietu do innego silnika.

Manifest i lock odwołują się wyłącznie do oficjalnych pakietów `com.unity.*` z rejestru Unity i modułów wbudowanych. Pakiety nie są vendoryzowane; Unity pobiera je przy imporcie, z ich własnymi licencjami i Third Party Notices. `Library/PackageCache` jest wykluczony.

Przegląd nie znalazł przeszkód w publikacji obecnych zasobów; nie stanowi gwarancji pochodzenia przyszłych importów. Każda nowa paczka wymaga sprawdzenia licencji przed dodaniem. Nie importowano niezależnych materiałów MAP-SYSTEM/VFX ani nie publikowano oryginalnych ZIP-ów, prywatnych raportów lub backupów.
