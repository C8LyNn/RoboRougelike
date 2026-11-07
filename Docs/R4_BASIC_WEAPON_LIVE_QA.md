# R4 — pierwszy model broni na pojeździe

Pierwszy wycinek integracji po katalogu #981 (`751a5504a59d7a73a2756e3bbb02f44e6a754d69`): istniejąca broń `Basic` na rzeczywiście złożonym pojeździe może użyć modelu `R4W_Basic`. Pozostałe bronie zachowują dotychczasowy wygląd. Brak modelu lub niemożność zawiązania adaptera powoduje powrót do istniejącej geometrii.

Model jest wyłącznie wizualny. Istniejący fizyczny pivot i jego collidery pozostają aktywne; renderer starej lufy jest ukrywany po poprawnym podłączeniu modelu. Ten sam istniejący system celowania i pocisków używa skorygowanego, widocznego wylotu modelu. Zmiana położenia wylotu jest zamierzona i wymaga regresji toru strzału.

## Wyniki

- Unity 6000.3.14f1, wybrane testy EditMode wchodzące do trybu gry: **7/7 PASS**, exit 0, XML `C:/Users/kubap/Desktop/Prototypy/r4-basic-live-regression.xml`, SHA-256 `d5fa54db3448bdce412e07342e5602eb20118df95affd30d97cc5661a49051e5`.
- Potwierdzono złożenie fizycznego pojazdu z modelem Basic, pocisk z jego widocznego wylotu, 40 obrażeń celu, działający collider starej broni, skorygowane celowanie na obróconym mocowaniu oraz zablokowanie strzału przez własny blok. Automatyczny karabin nadal korzysta z istniejącego wyglądu. Przeszły wcześniejsze testy T2, toru strzału i trzech pojazdów.
- Materiały załadowane w teście zgłaszały shader `Universal Render Pipeline/Lit`. To nie jest wizualna ocena kolorów, oświetlenia ani braku artefaktów.
- Surowe testy osi z #979 pozostają **4 FAIL**; są dokumentacją układu oryginalnych FBX i nie zostały wyłączone. Osobne testy adaptera #980 przechodziły 17/17.
- Windows x64 z kodu `4ecebd90c49005c87a702734c1add88cf2d5bbb7`: kompilacja **PASS**, exit 0, 0 błędów / 2 ostrzeżenia. ZIP wypakowano, rzeczywisty Player uruchomił się do inicjalizacji menu (**PASS**, log zawiera `[R5] Gotowość`). Nie wykonano interaktywnej rozgrywki ani oceny modelu.
- Automatyczne zamknięcie ukrytego okna przez `WM_CLOSE`: **FAIL**, proces zakończył się kodem `-1073741819` / `0xC0000005`. Log pokazuje przygotowanie zamknięcia `NativeClose`, sprzątanie fizyki i zatrzymanie Input System, bez diagnozy przyczyny. Jest to podobny objaw do historycznego problemu automatyzacji, ale nie stanowi dowodu jego przyczyny ani poprawnego ręcznego wyjścia tej wersji. Proces testowy już nie działa.
- Build: `C:/Users/kubap/Desktop/Prototypy/RoboRougelike_R4_Basic_4ecebd90_Windows_x64.zip`, 41 845 868 bajtów, SHA-256 `411b31919306351eb2fcd133b724632a6eb061438a48094a04ab9a591ecd58f5`. Do uruchomienia należy rozpakować całe archiwum. Logi: `r4-basic-982-windows-build.log` i `r4-basic-982-player-smoke.log` w tym samym katalogu.
- **NOT RUN:** ręczna rozgrywka i ocena wizualna, klipy animacji w walce, celowniki i skala większej liczby modeli w walce, model Basic w podglądzie ghosta garażu, pozostałe bronie i moduły ruchu, pełny pakiet PlayMode/EditMode.

To nie jest pełna integracja wszystkich modeli R4 ani akceptacja artystyczna. Należy sprawdzić umiejscowienie modelu, tor strzału i wydajność w Windows Playerze przed dalszą integracją.
