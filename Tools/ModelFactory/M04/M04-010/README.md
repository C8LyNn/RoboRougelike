# M04-010 — Stabilizator odśrodkowy (MODEL-04)

**Katalog:** M04-010, „Stabilizator odśrodkowy”, mocowanie górne gniazdo, ruch obrót przeciwwag, wyróżnik widoczne ciężarki. Jest to nowy, niezależny wizualny model Unity 6 RoboRougelike. Nie implementuje fizyki stabilizacji ani mechaniki gry.

## Projekt i sylwetka

Kompaktowy moduł dwupokładowy z dwiema parami przeciwległych ciężarków wolframowych. Dolna tarcza obraca się zgodnie z osią +Z, górna przeciwnie. Masy są widoczne przez dwie pierścieniowe osłony bezpieczeństwa. Każdy ciężarek jest połączony z centralną piastą przez metalowe ramię i z zewnętrznym pierścieniem przez poprzeczkę; elementy ozdobne nie są odłączonymi „pływającymi” bryłami. W środku biegnie nieruchomy, wąski wał. Obie wirujące piasty mają prawdziwy pusty środek i pozostawiają luz promieniowy do wału.

Osłony stacjonarne są połączone z czterema słupkami klatki przez diagonalne łączniki. Powierzchnie zewnętrzne wykorzystują grafit, tytan, wolfram, miedź, ceramikę i elastomer, a akcenty cyan/amber mają emisję. Wszystkie elementy wykonane są w metrach, z ograniczoną liczbą ostrych krawędzi i zaokrąglonymi narożnikami ciężarków (profil ośmiokątny).

## Montaż i animacja

- **Układ Blender:** Z do góry, 1 jednostka = 1 metr.
- **Główne mocowanie:** TOP_SOCKET_ROOT w (0,0,0); spód płyty mocującej na Z=0, wszystko powyżej. Przeznaczenie: górne gniazdo ramy.
- **Dolny pivot:** LOW_SPIN_Z, środek (0,0,0.280) m, obrót +360° wokół lokalnej osi Z w 121 klatkach.
- **Górny pivot:** HIGH_SPIN_Z, środek (0,0,0.510) m, obrót −360° wokół lokalnej osi Z w 121 klatkach.
- **FPS:** 30; czas jednej pełnej demonstracji 4 s. Klucze co ćwierć obrotu, liniowa interpolacja (ochrona przed zapadnięciem się pełnego obrotu przy eksporcie do kwaternionów GLB).
- **Prześwity:** każda obracająca się grupa ma osobną, nieruchomą względem osi obrotu obwiednię promieniową. Poziomy obu wirników nie nachodzą na siebie w osi Z. QA testuje zachowawcze cylindry obrotu względem wszystkich nieruchomych części w pełnym zakresie 360°, a dodatkowo sprawdza wierzchołki dla 121 pozycji. Połączenia wewnątrz tej samej sztywnej grupy są celowe.

## Generowanie

Wystarczy Python 3.10+ dla kontroli geometrii i statycznego OBJ/MTL. Do prawdziwego animowanego GLB i BLEND potrzebny jest Blender z modułem bpy.

    python3 Tools/ModelFactory/M04/M04-010/qa_m04_010.py
    python3 Tools/ModelFactory/M04/M04-010/generate_m04_010.py --static-obj --out build/M04-010
    blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M04/M04-010/generate_m04_010.py -- --out build/M04-010
    python3 Tools/ModelFactory/M04/M04-010/qa_m04_010.py --check-glb build/M04-010/M04-010_centrifugal_stabilizer.glb

Generator jest powtarzalny: przy kolejnym uruchomieniu usuwa wyłącznie własną kolekcję o prefiksie RRL_M04_010_ i niewykorzystywane własne zasoby. Nie ingeruje w główne sceny, ProjectSettings ani prefabrykaty gry. Statyczny OBJ jest zapisem pozycji spoczynkowej; animacja znajduje się tylko w GLB / BLEND.

## Zakres QA i ograniczenia

Niezależny qa_m04_010.py sprawdza faktyczne wierzchołki/faces OBJ, poprawność i dodatnią objętość zamkniętych siatek, zgodność z proceduralnym źródłem, dwa identyczne przebiegi eksportu OBJ/MTL, analityczne bezpieczne obwiednie obu obracających się grup i 121 próbek ich transformacji. Dodatkowa opcja --check-glb odczytuje strukturę wyeksportowanego GLB i kontroluje oba kanały obrotu oraz ich zmienność. Raport PASS należy przypisywać tylko wykonanym testom CI.

**Nie obejmuje:** faktycznego importu do Unity 6, uruchomienia Unity EditMode/PlayMode, strojenia fizyki gry ani ręcznej inspekcji wizualnej. Te czynności są NOT RUN, dopóki nie zostaną uruchomione i potwierdzone.

**Claim:** Docs/ModelFactory/claims/M04-010.json na gałęzi feature/rrl-model-factory-queue-v1; commit rezerwacji 4628aa215b8baad2480f9feb62289c49ceea1c56. Własna gałąź produkcyjna: feature/rrl-model-m04-010-centrifugal-stabilizer, PR Draft do develop. Bez merge.
