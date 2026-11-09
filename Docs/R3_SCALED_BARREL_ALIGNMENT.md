# Dopasowanie fizycznych luf do modeli ×2

Sonda używana do kontroli montażu i naprawy odtwarza fizyczną lufę, podczas gdy właściwy pocisk powstaje na wylocie widocznego modelu. Po powiększeniu stara sonda kończyła się zbyt daleko: 1,716 m zamiast 1,320 m dla działa podstawowego, 1,872 m zamiast 1,400 m dla Automatic oraz 2,288 m zamiast 1,200 m dla ciężkiej armaty. Mogło to niepotrzebnie blokować prawidłowy montaż.

Długość fizycznej lufy i jej wylotu dostosowano do zmierzonych w Unity punktów widocznych modeli. Dwie fizyczne lufy Automatic zachowują teraz również rozstaw odpowiadający widocznym wylotom. Te same wymiary stosuje pojazd, podgląd oraz sonda używana przy montażu i naprawie. Nadal obowiązuje fizyczna blokada strzału przez własną konstrukcję.

Trzy testy porównujące długości fizycznych i widocznych luf przed poprawką FAIL, po poprawce wraz z celowaną regresją Unity 6000.3.14f1 EditMode **24/24 PASS** (`scaled_barrel_green.xml`): montaż i naprawa, fizyczne trafienia, własna przeszkoda, zapis i grupy uzbrojenia. Osobny PlayMode, ręczny Windows Player i pełna regresja: NOT RUN. Wymiary pochodzą z aktualnych modeli; przy ich wymianie należy powtórzyć pomiar.

Dodatkowy rzeczywisty test garażu **1/1 PASS** (`barrel_placement.xml`): przeszkoda przecinająca widoczną lufę blokuje zakup bez pobrania złomu, a przeszkoda za wylotem nie blokuje samego montażu.
