# Garaż — wyszukiwanie części i regresja

Baza: 899a9ced, PR #1184. Wyszukiwanie fizycznych części i stanów modelu podczas sprawdzania punktów montażu wykorzystuje indeks identyfikatorów zamiast ponownego skanowania całej listy dla każdego elementu. Reguły połączeń, walidacja, fizyka i limit konstrukcji pozostają takie same.

Unity 6000.3.14f1, rzeczywisty EditMode: 11/11 PASS, `garage_lookup_regression2.xml`, kod procesu 0. Obejmuje walidację niedozwolonego montażu na kołach i broni, rzeczywistą kolizję z przeszkodą, dopasowanie koła, odświeżanie punktów i powiększoną broń automatyczną. Poprawiono przestarzałe scenariusze testowe pod obecne pozycje większych konstrukcji i czerwone widmo; pierwotny przebieg miał 6 niezgodności.

Pojedynczy pomiar pierwszego wyznaczenia punktów ciężkiego pojazdu wyniósł 55 ms przed i 59 ms po zmianie; nie jest to dowód przyspieszenia. Wyszukiwanie osiągalnych połączeń w BUILD001 oraz liczba obiektów znaczników nadal wymagają oceny przy większych konstrukcjach. Limit 180 zajętych komórek i ograniczenia wymiarów pozostają aktywne: ta zmiana sama nie umożliwia zwiększenia liczby klocków 8×. Pełna regresja, pomiar wydajności podczas gry i ręczny test: NOT RUN. Nie tworzono nowego buildu.
