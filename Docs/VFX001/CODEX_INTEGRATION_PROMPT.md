# CODEX — RoboRougelike VFX-001 Combat Feedback v5

**STATUS:** PACKAGE REVIEW AND ISOLATED INTEGRATION — NO MAIN MERGE

Repozytorium: `https://github.com/C8LyNn/RoboRougelike`

## Zakres

Rozpakuj całą paczkę `RoboRougelike_VFX001_CombatFeedback_v5.zip`. To **pełny następca v1–v4**. NIE dodawaj oddzielnie starszych paczek v1–v4.

Obowiązkowo przejrzyj `README_START_HERE_PL.md`, `Docs/INTEGRATION_PL.md`, `Docs/QA_CHECKLIST_PL.md`, `Docs/CHANGELOG_v4_v5.md`, manifest SHA-256.

## Środowisko

1. Zweryfikuj bazowy projekt Unity 6000.3.14f1 URP na GitHubie i faktyczne zakończenie/stan MOV-001–MOV-009.
2. Pracuj na izolowanej gałęzi `feature/vfx001-combat-feedback-v5`; nie nadpisuj innej pracy i NIE ruszaj main.
3. Skopiuj `Assets/RoboRougelikePackages/VFX001` z `.meta`. Sprawdź konflikty z istniejącym namespace i shaderami.
4. Utwórz materiały URP przez menu `Tools > RoboRougelike > VFX001 > Create URP Materials`. Upewnij się, że `.mat` z `Resources` znajdują się w buildzie Windows; nie polegaj tylko na `Shader.Find`.

## Wdrożenie bez wpływu na rozgrywkę

- Podłącz prawdziwe zdarzenia trafienia → `VfxFeedbackOrchestrator` (pool, kamera, lokalne drgania i żar).
- Podłącz utratę części i core destruction do istniejącego pipeline; zarejestruj tylko wizualną reakcję po zdarzeniu gameplayu.
- Animacje mechaniczne podpinaj wyłącznie pod dekoracyjne dzieci modułów bez fizyki.
- Shaker kamery ma własny visual child; NIE modyfikuj kierunku celowania ani głównego rig-a kontrolera kamery.
- Nie zmieniaj obrażeń, HP, sił Rigidbody, połączeń, kolizji, AI, ekonomii, ani trybów sterowania.

## QA

- Uruchom EditMode i PlayMode tests, regresję całego projektu, poligon oraz rzeczywisty Windows build.
- Zweryfikuj manualnie, że nowe efekty są czytelne i ograniczone budżetem; sprawdź wielokrotne trafienia oraz usunięcie obiektu w trakcie efektu.
- Oddziel statyczne sprawdzenie ZIP od uruchomionych testów Unity. Nie deklaruj PASS bez logu wykonania.

## Handoff

Otwórz PR (draft do przeglądu) z opisem adaptacji, SHA, raportem testów, zrzutami, ograniczeniami i znanymi regresjami. Nazwij raport `VFX001_V5_INTEGRATION_REPORT.md`.

**Bez zgody użytkownika nie merguj do develop/main ani nie publikuj release.**
