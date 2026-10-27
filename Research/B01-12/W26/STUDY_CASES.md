# PASS 5 / W34–W37 — LATEST LAB-VERIFIED FIELD PROTOCOL (supersedes obsolete fixture line in historical notes)

**Current immutable HTML**: `81f8913159f7a2273d73ab65f2b8d9abdab8d7f9` (unchanged from PASS4; do not silently promote independent older `bc17de...` 5/5 QA to this revision). **Source-confirmed UX03 mismatch fixed in research instrument only**: HTML emits `LOGICAL_OCCUPIED`, not `LOGICAL_OVERLAP`; 24/120 pilot cells and generator in [observations.csv](observations.csv) / [study.py](study.py) now match original HTML. Prior CSV blob `a185d60...` is historical; new CSV `45a77ec6d2cc214e0471d1603a85f5570d581aa9`, validator `1040a2083e3046ad66cfb3bc43fdd1c3e5256559`. Authored and ACTUALLY EXECUTED 20/20 Python tests incl 3 new negative/source contract checks; blank CSV still 0/120 `NOT_RUN`.

**Moderator procedure H3 — mandatory to prevent source-confirmed answer leak:** Browser fixture selector `#reviewReason` ("Fikcyjny powód porażki") is VISIBLY PRESENT by default. A participant asked to infer the failure must NEVER see it before answering.

1. Before showing participant browser, moderator opens CHL view, chooses `FALL`, `TIMEOUT` or `COLLISION` to match the assigned anonymized `fixture`, and masks the selector's entire wrapper in that local, research-only page using DevTools Console: `document.querySelector('#reviewReason').closest('.scenario').style.display='none'`. Close DevTools before handing over the display. If this cannot be performed without exposing the answer, **do not execute H3 on a live participant**; use a pre-recorded screenshot with fixture panel cropped out and declare it a capture-based comprehension task.
2. Research trial uses the mock `Rozpocznij próbę` / `Symuluj porażkę` buttons; moderator, not subject, operates any stimulus-triggering buttons. Ask for the first unprompted failure reason and next action **after the failure status appears**. Do not reveal `#eventLog` or moderator fixture settings before their answer if these are not part of the specified treatment; maintain equal conditions across A/B/C. This does not test real driving or physics.
3. After the session, restore moderator control with `document.querySelector('#reviewReason').closest('.scenario').style.removeProperty('display')`; reset mock result ledger; clear next person's browser context. Log only actual outcomes in anonymous CSV; no names/contact identifiers.
4. B01-12 performed real headless Chromium DOM sequence `select TIMEOUT → hide → Begin → Fail → status TIMEOUT → Retry → hidden FALL → Begin → Fail → status FALL → restore`, **6/6 checks PASS** on same HTML. This validates **UI operation / masking only**, **NOT** genuine unbiased human comprehension.
5. UX02/H2 scope is **only recognition of textual source reason categories**; this HTML does not contain an independent physical collider-overhang spatial stimulus. Do not claim H2 physical separation or achieved +25pp until appropriate physical source-derived visual stimuli and actual participants are available.

**Remaining external gates:** B01-11 independent exact-blob `81f891...` browser recheck **NOT_RUN** as last verified; HQ decision about voluntary recruitment/consent + genuine eight human participants **NOT_RUN**; Unity PhysX and 3×Light+3×Heavy **NOT_RUN**. No canonical approval, no G2, no Unity/Codex edits. An independent newer B01-11 pass could enable **EXPERIMENTAL_FROZEN** for this research HTML, not production readiness.

---

# PASS 4 / W30–W33 — CURRENT PILOT CONTRACT (supersedes historical PASS 3 notes below)

**Study HTML Git blob:** `81f8913159f7a2273d73ab65f2b8d9abdab8d7f9`; SHA256 `20daf5c0d63fa9aff22b65d0896fedac78d7dcf81030a4be9be2e1c521bb0124`. Previous B01-11 independently browser-reviewed source `bc17deba...` is **HISTORICAL** for the newly changed HTML; five prior assertions do not transfer automatically. Existing B01-12 browser regression **70/70 PASS** and 14 new copy/390px tests **PASS** on this current source; B01-11 independent review on new blob **NOT_RUN**.

**Applied B01-11 UX.U5 feedback:** neutral A/B/C treatment names; both CHL Light and Heavy explicitly state `Zwrotność: NIEZMIERZONA`. This removes an unsupported steering advantage cue before participant H3 research, without changing JS behavior, PhysX, assets or runtime.

**Study constraints:**
- H1 and H3 are eligible for a bounded *exploratory* 8-person moderated pilot, subject to HQ recruitment/consent decision; no final GUI acceptance.
- H2 is **TEXTUAL REASON CATEGORY comprehension only**. **Do NOT score physical collider overhang recognition**: this prototype has no independent real-collider spatial stimulus and 11 physical categories stay UNKNOWN. Original H2 spatial/25-percentage-point promise requires a different HQ-approved stimulus.
- Moderator must preselect mock reason and reset scenario **before the subject sees the screen**; subjects must not choose or be shown the injected answers in fixture selector. Use consistent neutral prompt and log fixture, timing, mistakes and false `PHYSICS/SOCKET APPROVED` assumptions.
- Use updated [anonymous 120-row blank sheet](observations.csv), source-verified [offline CSV gate/validator](study.py), and [existing HTML](../W17/index.html). Fill only genuine observations. Source `bc17de...` values in older instructions below are historical and must not be used for new observations.
- Honest present status: **0/8 humans, 0/120 observed trials; H1/H2/H3 NOT_RUN**. Unity native/PhysX/sockets/real driving NOT_RUN. Never treat this research as CODEX_READY or G2.

---

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
