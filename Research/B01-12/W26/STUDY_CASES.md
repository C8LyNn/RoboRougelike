# B01-12 UX.U5 source-backed study requests to B01-11 (NOT executed on people)

Source: exact v2 HTML Git blob `bc17deba3c96307c8179a0ff564b26b3855287ce`. Reviewed original independent B01-11 v1 [comment](https://github.com/C8LyNn/RoboRougelike/issues/296#issuecomment-6440580983) and benchmark [#292](https://github.com/C8LyNn/RoboRougelike/issues/292).

## Five sequential cases
- **UX-01:** preset ready → hover missing wheel/free cell → Is it physically mount-ready? Check sidebar `MOUNT_AUTHORITY UNVERIFIED`. Click UNKNOWN without checkbox (must not insert); with `rekwizyt badawczy` checkbox (mock insertion counts independently, not approval).
- **UX-02:** armor 2×2 yaw90, unmeasured wheel, select `TYPED_SOCKET_UNVERIFIED` and `INCOMPLETE_EXTERNAL_BROADPHASE`; ask testers to distinguish logical occupancy, actual collider verification and typed physical socket. No guessed collider measurements for 11 unknown.
- **UX-03:** choose C, invalid overlap/unknown, locate working `Dlaczego?` by keyboard/mouse, determine cause and remedy.
- **UX-04:** CHL → begin → skip/reverse/wheel-graze, read **visible** rejected reason before inspecting log; select FALL/TIMEOUT/COLLISION fail fixture and name next action. CP events are buttons, not PhysX evidence.
- **UX-05:** Heavy attempt CP1–4, click mock Finish then attempt Light switch without Retry (blocked), retry then Light start, check `HEAVY-01` history remains correctly attributed and new `LIGHT-01` represents new attempt. Pause/Retry + modal keyboard Tab/Escape.

## 8-user exploratory design (B01-11 ownership)
8 target-player volunteers, counterbalance ABC/ACB/BAC/BCA/CAB/CBA+ABC/CBA; source-blind neutral tasks, observer captures `participant_id,source_html_blob,variant,order,case_id,success,seconds_to_first_correct_answer,wrong_clicks,said_physics_approved,said_socket_approved,failed_to_find_reason,wrong_attempt_attribution,participant_quote,observer_note`. Hypothesis gates: H1 >=90% source-safe classification/zero UNKNOWN interpreted approved, H2 >=25pt improvement cause attribution, H3 >=80% failure understanding and >=90% Retry found within 5s. **These are NOT measured outcomes; no human study performed.**

**Next action:** independent B01-11 SOURCE+CHROMIUM re-review current v2 with exact HTML blob and 5 scenarios, then actual humans only if accessible. Without genuine humans report `HUMAN_NOT_RUN`; never claim results from LLM agents.


## PASS 3 — rzeczywisty pakiet organizacyjny badania (bez uczestników)

**Stan danych: `0/120`, `HUMAN_NOT_RUN`.** Prototyp nie został zmodyfikowany: [v2 oryginalny HTML](../W17/index.html), Git blob `bc17deba3c96307c8179a0ff564b26b3855287ce`, SHA-256 lokalnego pliku `116660582df599d0d337110e8956a81666b3065a868ae882a435151a43df084e`.

**Gotowy [pusty arkusz 120 obserwacji](observations.csv)** — osiem anonimowych P01–P08 × trzy warianty A/B/C × pięć przypadków UX01–UX05. Niczego nie wpisano jako wynik ludzi. Kolejność wariantów jest przypisana w arkuszu (`ABC,ACB,BAC,BCA,CAB,CBA,ABC,CBA`); pracuj na jednej niezmienionej kopii HTML, resetuj scenariusz między wariantami. Uwaga: każde powtórzenie zadania może tworzyć efekt uczenia się — pilotaż eksploracyjny, nie dowód statystyczny.

**Do przeprowadzenia:** prowadzący prosi o dobrowolny udział, odnotowuje zgodę poza arkuszem bez danych osobowych. Dla każdego pola źródła wprowadza tylko faktyczne obserwacje. Nie zapisuje prawdziwych imion, emaili, identyfikatorów, nagrań ani cytatów bez osobnej zgody.

**Wymagane pola każdej przeprowadzonej obserwacji:** `answer` (UNKNOWN/REJECTED/APPROVED/NA); `seconds_to_first_answer` i `wrong_clicks` (liczby nieujemne); `physically_approved`, `socket_approved`, `correct_cause`, `correct_next_action` (0 lub 1); `retry_seconds` (czas znalezienia Retry, tylko UX05). `observer_note` opcjonalne i anonimowe. Kolumny `source_blob`, `variant_order`, `variant`, `case_id`, `sequence`, `fixture` są przypięte: nie wolno ich zmieniać w celu dopasowania wyników.

**Warunki prerejestrowane, nieobserwowane:** H1 >=90% odpowiedzi UNKNOWN dla UX01/UX02 i *zero* deklaracji zatwierdzonego fizycznego montażu/socketu; H2 >=25 punktów procentowych poprawy rozpoznania przyczyny w wariancie B względem A (UX02); H3 >=80% poprawnych przyczyn porażki UX04, >=90% poprawnych działań i odnalezienia Retry w <=5 s (UX05). Raportuj mianowniki oraz efekt kolejności zamiast tylko słowa „PASS”.

**Narzędzie lokalnego odbiorcy**: komplet ZIP `B01-12-PASS3-STUDY-KIT.zip` przekazany w bieżącej rozmowie B01-12 zawiera `study.py`, identyczny `index.html`, pusty arkusz, raport `NOT_RUN`, 17-testowy wynik sprawdzenia parsera, negatywny test manipulacji plikiem oraz SHA256. ZIP nie znajduje się w repo; inni agenci mają niezależny dostęp do *HTML i CSV tutaj*, a moduł Python musi być fizycznie dostarczony, zanim ogłoszą jego odbiór. Przykładowe użycie w katalogu ZIP: `python study.py selftest`; `python study.py report --csv observations.csv --html index.html --out study_report.json`. Pusta próba musi dać `NOT_RUN`. Testy syntetyczne CLI nie są badaniem ludzi.

**Warunek W29:** B01-11 najpierw wykonuje niezależny UX.U5 v2 review na dokładnym HTML blob, następnie rzeczywisty pilotaż z ludźmi (jeżeli dostępni). Do uzyskania pisemnego werdyktu U5 nie ma podstaw do nowego patcha UI. Nie stanowi to zatwierdzenia Unity, socketów, PhysX, G2 ani uruchomienia Codexa.
