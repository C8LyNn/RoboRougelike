# Biblioteka projektów pojazdów

Cel: zachować sam układ i przypisania slotów, bez przenoszenia części, HP ani waluty między wyprawami.

1. Zbudować wersjonowany format projektu oraz atomowy zapis lokalnych slotów, używając istniejącego zapisu z sumą kontrolną. Zachować rodzaj, kształt, tier, orientację i pola części oraz przypisania typów do slotów.
2. Przygotować podgląd braków wyłącznie na podstawie posiadanych fizycznych egzemplarzy; nie dopuszczać do ponownego użycia jednego identyfikatora. Uszkodzone i brakujące części zgłaszać osobno.
3. Odtwarzanie oprzeć na rzeczywistych operacjach magazynu, połączeń i fizycznej walidacji garażu; wykonać transakcyjnie, z pełnym cofnięciem po błędzie. Nie tworzyć ani nie usuwać egzemplarzy, nie zmieniać ich HP i portfela.
4. Dodać dostęp do zapisu, podglądu oraz świadomego odtworzenia w garażu. Globalne keybindy pozostawić oznaczone jako niedostępne, dopóki punkt 06 nie ma działającego systemu ustawień.
5. Wykonać celowane testy zapisu, starych danych, braków, powtórzeń, rollbacku i przejścia przez rzeczywisty garaż; opisać ręczną weryfikację jako NOT RUN, jeśli jej nie wykonano.
