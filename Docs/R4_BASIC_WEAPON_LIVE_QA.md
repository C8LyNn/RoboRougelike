# R4 — pierwszy model broni na pojeździe

Pierwszy wycinek integracji po katalogu #981 (`751a5504a59d7a73a2756e3bbb02f44e6a754d69`): istniejąca broń `Basic` na rzeczywiście złożonym pojeździe może użyć modelu `R4W_Basic`. Pozostałe bronie zachowują dotychczasowy wygląd. Brak modelu lub niemożność zawiązania adaptera powoduje powrót do istniejącej geometrii.

Model jest wyłącznie wizualny. Istniejący fizyczny pivot i jego collidery pozostają aktywne; renderer starej lufy jest ukrywany po poprawnym podłączeniu modelu. Ten sam istniejący system celowania i pocisków używa skorygowanego, widocznego wylotu modelu. Zmiana położenia wylotu jest zamierzona i wymaga regresji toru strzału.

## Wyniki

- Unity 6000.3.14f1, wybrane testy EditMode wchodzące do trybu gry: **7/7 PASS**, exit 0, XML `C:/Users/kubap/Desktop/Prototypy/r4-basic-live-regression.xml`, SHA-256 `d5fa54db3448bdce412e07342e5602eb20118df95affd30d97cc5661a49051e5`.
- Potwierdzono złożenie fizycznego pojazdu z modelem Basic, pocisk z jego widocznego wylotu, 40 obrażeń celu, działający collider starej broni, skorygowane celowanie na obróconym mocowaniu oraz zablokowanie strzału przez własny blok. Automatyczny karabin nadal korzysta z istniejącego wyglądu. Przeszły wcześniejsze testy T2, toru strzału i trzech pojazdów.
- Materiały załadowane w teście zgłaszały shader `Universal Render Pipeline/Lit`. To nie jest wizualna ocena kolorów, oświetlenia ani braku artefaktów.
- Surowe testy osi z #979 pozostają **4 FAIL**; są dokumentacją układu oryginalnych FBX i nie zostały wyłączone. Osobne testy adaptera #980 przechodziły 17/17.
- **NOT RUN:** ręczna rozgrywka i ocena wizualna, Windows Player z tym modelem, klipy animacji w walce, celowniki i skala większej liczby modeli w walce, model Basic w podglądzie ghosta garażu, pozostałe bronie i moduły ruchu, pełny pakiet PlayMode/EditMode.

To nie jest pełna integracja wszystkich modeli R4 ani akceptacja artystyczna. Należy sprawdzić umiejscowienie modelu, tor strzału i wydajność w Windows Playerze przed dalszą integracją.
