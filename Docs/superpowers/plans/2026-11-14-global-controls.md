# Globalne przypisania sterowania — kroki

1. Ustalić wszystkie rzeczywiste ścieżki wejścia: jazda i cztery bronie w `P12_PipelineSafeTankController`, aktywne i rozstawiane w `MOVSeriesInput`, osobne akcje i HUD.
2. Dodać trwały profil przypisań z jawnym wykrywaniem konfliktów, bez nadpisywania nieczytelnego profilu, oraz testy zapisu, odczytu i przywrócenia domyślnych.
3. Podłączyć stopniowo wejście do profilu, zachowując domyślne mapowanie oraz wejście kontrolera; sprawdzić scenariusze wejścia przez Unity Input System.
4. Udostępnić sterowanie w ustawieniach; po przechwyceniu klawisza jawnie zgłaszać konflikt. Powiązać napisy w HUD z faktycznymi przypisaniami.
5. Projekt pojazdu opcjonalnie zapisuje kopię profilu, a przywracanie klawiszy wymaga osobnego wyboru; starsze projekty pozostają zgodne.
6. Uruchomić celowane testy Unity i osobno oznaczyć brak ręcznej rozgrywki. Nie deklarować pełnego pokrycia, dopóki nie podłączono wszystkich akcji i interfejsu.
