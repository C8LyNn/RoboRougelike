# Koło Monster: pilotaż większego obrysu

Nowo kupione koło Monster zajmuje dziewięć komórek w płaszczyźnie koła (3 × 3). Obrót części obraca również jej zajęte pola. Nadal jest to jeden przedmiot z jednym identyfikatorem, stanem zdrowia i kosztem naprawy. Port znajduje się przy osi; zewnętrzne pola opony nie są punktami mocowania. Podgląd garażu i walidacja montażu korzystają z tego samego obrysu, a fizyka zachowuje istniejący promień opony 0,8 m.

Wersja obrysu jest zapisywana przy części. Starsze zapisane koła Monster zachowują dotychczasowy obrys jednego pola; gra nie przestawia ani nie usuwa istniejących konstrukcji przy odczycie. Stare i nowe koła nie są łączone jako ten sam typ przy fuzji. Nieznane wersje obrysu są odrzucane przy odczycie.

To pierwszy model pilotażowy. Pozostałe koła i bronie nie zostały tym samym automatycznie powiększone: potrzebują odrębnego dopasowania geometrii, portów, fizyki i kompatybilności zapisów. Ustawienia wymagają jeszcze wizualnej oceny w grze.

Celowane testy Unity 6000.3.14f1 EditMode: 17/17 PASS (`monster_targeted_final.xml`), w tym zakup, montaż, naprawa, zapis i odczyt, zgodność starych kół i jazda pojazdem z fizycznymi kołami Monster. PlayMode, wizualny odbiór w Windows Playerze i pełna regresja: NOT RUN. Pierwsza próba w sandboxie nie uruchomiła testów z powodu błędu usługi licencji Unity; powyższy wynik pochodzi z ponowienia poza sandboxem.
