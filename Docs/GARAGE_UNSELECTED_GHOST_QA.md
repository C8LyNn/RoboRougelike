# Widmo bez wybranej części

W trybie montażu widmo jest ukrywane, jeżeli garaż wymaga egzemplarza z magazynu, ale żaden istniejący egzemplarz nie jest wybrany. Dzięki temu po zmianie kategorii nie pojawia się czerwony obrys poprzedniego typu. Wybrana część nadal może pokazywać zielone albo czerwone widmo, a narzędzia zaznaczania, naprawy i zdejmowania nadal mogą podświetlać wskazany obiekt. Bez zmian w zakupie, walidacji lub fizyce.

Lokalna kompilacja Roslyn kodu gry i testów: exit 0/0. Bezokienna kontrola skompilowanej reguły: **7/7 PASS**, w tym brak celu, brak i obecność egzemplarza oraz pozostałe narzędzia. Wyniki: `Artifacts/Local/CSharp-GarageGhost-20261108/results.json`.

Rozszerzono przygotowany test Unity o widoczność widma przed i po wyborze. **Unity, Windows Player oraz rzeczywisty obraz: NOT RUN** — bieżące prace odbywają się bez przejmowania komputera. Poprzedni build `c332e6a5` nie zawiera tej poprawki.
