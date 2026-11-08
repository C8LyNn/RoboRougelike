# Heavy: dojazd do rampy poligonu

Unity 6000.3.14f1, oryginalny pojazd R3 i solver R6, rampa poligonu 18°. Kod gry na `06b050e187ad24d5e6b555916311c5637406720b`; późniejsze zmiany dotyczą wyłącznie testów. Pojazd ustawiono jednorazowo na płaskim podłożu przed rampą `(18, 0.7, 20)`, a następnie sterowano go siłami napędu przez 600 kroków fizyki. To nie jest ręczna gra ani dojazd od oryginalnego spawnu.

| Próba | Wynik obserwacji |
| --- | --- |
| Ciężki bez dodatkowego silnika, jazda prosto | Wjechał około 5,80 m od miejsca startu i zatrzymał się przy dolnej części rampy. Maksymalna wysokość środka 1,22 m; szczyt nieosiągnięty. |
| Ciężki z jednym silnikiem T1, bez korekt | Wjechał wyżej, do 3,39 m, po czym zjechał bokiem z rampy. Szczyt nieosiągnięty. |
| Ciężki z jednym silnikiem T1, automatyczna korekta kierunku | Wjechał na szczyt; maksymalna wysokość środka 4,18 m. Próba nie określa komfortu sterowania przez człowieka. |

Nie zmieniono siły, masy, przyczepności ani sterowania. Testy diagnostyczne sprawdzają kontakt kół i ruch z dojazdu, a wynik wjechania zapisują jako telemetrię. `PASS` testu diagnostycznego nie oznacza, że każda konstrukcja pokona rampę. Dwa pierwsze przebiegi: `Artifacts/Local/NativeCatchup-RampApproach-20261108/`; sterowany: `Artifacts/Local/NativeCatchup-RampSteered-20261108/`. W każdym katalogu znajdują się oryginalne XML, Editor.log i kod zakończenia procesu. Ręczny wjazd, hamowanie i zjazd pozostają do sprawdzenia.
