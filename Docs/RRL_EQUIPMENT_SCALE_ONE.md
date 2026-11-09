# Przywrócenie skali wyposażenia ×1

Domyślne działa wracają do jednokomórkowego obrysu i wcześniejszego modelu; duże boczne koła LargeOffroad i Monster otrzymują mniejszy model, odpowiadający mu collider oraz dopasowanie osi do rzeczywistego punktu styku. Podstawowa siatka i reguły punktów mocowania nie są usuwane. Wymiary, modele i obrysy większej wersji pozostają w projekcie; tryb powiększony można w przyszłości włączyć na etapie konfiguracji przed uruchomieniem, po ponownej weryfikacji. Nie jest to przełącznik gracza.

Wczytanie zapisu najpierw waliduje oryginał, a na kopii zmniejsza zamontowaną i magazynowaną broń oraz powiększone koła, zachowując ID, HP, punkty mocowania i oryginalny dokument. Jeśli zmiana starych punktów mocowania odłącza części, obowiązuje dotychczasowa transakcyjna procedura odzysku; przy braku miejsca odczyt jest blokowany bez modyfikacji zapisu. Powrót do większej wersji przenosi wymagające rozszerzenia części do magazynu, aby uniknąć nakładania; brak miejsca dla kół blokuje odczyt zamiast zmieniać źródło.

Działa w 12 pojazdach startowych umieszczono wyżej na podpartych klockach, ponieważ po zmniejszeniu przedni pancerz zasłaniał ich lufy. Koszty i balans nie zmienione.

Unity 6000.3.14f1, rzeczywiste celowane testy EditMode (w tym wejścia do Play Mode):
- `scale_one_final2.xml`: 42/42 PASS; obejmuje ×1, zapisy, pełny magazyn, 12 konstrukcji, fizyczne koła i zachowaną logikę ×2.
- `scale_one_starter_paths.xml`: 1/1 PASS; wszystkie 12 konstrukcji, drożność strzału i brak penetracji colliderów dział.
- `scale_one_compatibility.xml`: 3/3 PASS; pełny magazyn, prawdziwe działo ×2 w garażu i jazda większym kołem w trybie testowym. Ten przebieg wykonano przed podniesieniem dział ×1; nie zmieniono konstrukcji ×2.
- Próba działa ×1 w `scale_one_final2.xml` montuje broń w rzeczywistym garażu, uruchamia poligon, strzela w cel i weryfikuje obrażenia oraz brak kosztu energii.

Pełny EditMode, oddzielny pełny PlayMode i ocena ręcznej gry/wyglądu: NOT RUN. Test jazdy 12 pojazdów na poprzednim kroku używał ograniczeń pochyłu i wysokości; nie dowodzi komfortu prowadzenia na rampach. Build należy sprawdzić ręcznie, zwłaszcza koła po obu stronach, celowanie i rozmieszczenie broni.
