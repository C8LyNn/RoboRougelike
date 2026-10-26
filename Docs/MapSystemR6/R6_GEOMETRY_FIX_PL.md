# R6 — poprawka referencyjnego węzła lower_road

W `MAP_R4_quarry_03.json` zwiększono `padWidth` węzła `lower_road` z **12 m do 18 m**, a `version` mapy z 5 na **6**.

Motywacja: manewr `junction_lower → lower_exit` miał oszacowany promień **3,84 m** i nie spełniał demonstracyjnego profilu `minTurnRadius=4,5 m`. Po zmianie węzła obliczenie offline daje około **5,86 m**.

**Uwaga:** poszerzenie padu to zmiana geometrii placu manewrowego; w Unity należy sprawdzić nakładanie się siatek oraz rzeczywisty przejazd. To nie stanowi potwierdzenia bezkolizyjności ani działania prawdziwej fizyki pojazdu.

R6 dodaje również `MapR6RouteValidator` — filtr `G1a+` analizujący kolejne manewry pomiędzy krawędziami trasy. Walidacja nadal nie zastępuje G1b i G1c.
