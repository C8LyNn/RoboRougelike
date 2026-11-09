# Automatic ×2 — plan integracji

Cel: powiększyć nowo kupowane działo Automatic dwukrotnie względem rzeczywistego modelu, z jedną tożsamością, jednym HP i rozpoznawalnym mocowaniem do siatki 0,55 m. Dotychczasowe zapisane egzemplarze zachowują geometrię. Skala i komórki muszą być zgodne z fizyczną podstawą; zakres obrotu lufy pozostaje odrębną strefą funkcjonalną.

- [x] Test rzeczywistego zakupu, podglądu, montażu, obrotu i zapisu/odczytu starej oraz nowej części.
- [x] Wersja geometrii, obrys, port oraz jedna instancja HP/grafu; właściwa walidacja każdej zajętej komórki.
- [x] Ta sama podstawa i kolizja w pojeździe, podglądzie oraz sondzie sprawdzającej montaż i naprawę.
- [x] Dwukrotnie większy model, osie i oba wyloty pocisków dopasowane do istniejącego strzelania; bez drugiego systemu broni.
- [x] Czytelne rozróżnienie egzemplarzy w magazynie, brak łączenia różnej geometrii we fuzji.
- [x] Celowane testy Unity starego zapisu, montażu, kolizji, uzbrojenia i luf; odnotowanie ograniczeń testu widocznego Playera.

Zakres nie obejmuje automatycznego powiększenia pozostałych modeli, modyfikacji wyważenia ani migracji dawnych konstrukcji przez ich przesunięcie.

Powiększona podstawa rezerwuje 3 × 2 × 3 komórki (18 pól), ale ma pojedynczy port w komórce kotwiczącej. Rozmiar obejmuje całą podwójną podstawę, także niewielki fragment sięgający drugiej warstwy; poruszająca się lufa korzysta z oddzielnego sprawdzania przestrzeni. Zmiana dotyczy nowych zakupów Automatic; stare egzemplarze zachowują skalę 1×. Wrogowie korzystający z własnych starszych definicji pozostają bez zmian.

Unity 6000.3.14f1: celowana regresja EditMode 27/27 PASS (`auto_scale_final.xml`), a końcowy test realnego wskazania portu, obu trybów zakupu, obrotu, zniszczenia, naprawy, dwóch trafień i blokady własnym pancerzem 2/2 PASS (`auto_scale_socket.xml`). Celowanie na tarczę w teście było utrzymywane nieruchomo; to nie jest ocena komfortu gracza. Widoczny Windows Player, sterowanie ręczne, PlayMode jako osobny przebieg oraz pełna regresja: NOT RUN.
