# Kierunek wizualnych kół przeciwników

Lustrzany model koła po lewej stronie przeciwnika obracał się wizualnie wstecz podczas jazdy do przodu. Animacja uwzględnia teraz zwierciadlane odbicie rzeczywistego układu modelu. Zmiana nie dotyka colliderów, napędu R6, odpadania koła ani obrażeń.

Unity 6000.3.14f1: test lewego i prawego koła przed poprawką wykrył odwrotny kierunek lewego. Po poprawce **8/8 celowanych EditMode PASS**, `enemy_wheel_direction_final.xml`, exit 0: obejmuje koła gracza i przeciwnika, oryginalny collider, odpadanie oraz napęd. Samodzielny PlayMode, Windows Player i ręczna ocena: NOT RUN. Build nie był tworzony.
