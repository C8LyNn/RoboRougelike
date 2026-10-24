# RoboRougelike — VFX-001 Combat Feedback v5

**Kompletna, kontynuowana paczka VFX-001 (v1–v5), nie dodatek do v4.**

Docelowa gra: **RoboRougelike**, Unity **6000.3.14f1**, URP, PC/Windows.
Paczka **nie jest jeszcze wdrożona** do gry, nie aktualizuje repozytorium GitHub i nie modyfikuje projektów R6 / MOV-009 / MOV-001–009.

## Co zawiera

### Mechaniki wizualne z v1–v4
- Proceduralne trafienia z profilami materiałów; iskrzenie, dym, odłamki 3D.
- Pooled VFX, limity liczby efektów, grupowanie zdarzeń.
- Wielowarstwowe wybuchy i wieloetapowe zniszczenie rdzenia.
- Dekoracyjne ślady zniszczenia pancerza, pożary i dym uszkodzonych modułów.
- Smugi spalin/dopalaczy, kurz i ślady kontaktu z podłożem.
- Wizualny odrzut broni i animacja ciągu.

### Nowe w v5
- `VfxMechanicalJolt`: bezpieczne animacje wibracji, uderzenia i zniszczonego pancerza na **dekoracyjnym dziecku** transformu.
- `VfxCameraImpulseReceiver`: tłumione i dystansowo redukowane wstrząsy kamery, limit 6 impulsów, brak zmian gameplayowego rig-a.
- `VfxHeatImpactController`: krótkotrwałe rozżarzone miejsca trafienia, maks. 3 markery na moduł, automatyczny zanik.
- `VFX001HeatGlowURP.shader`: shader URP unlit z radialnym gradientem rozżarzenia.
- `VfxFeedbackOrchestrator`: opcjonalna integracja trafień z wybuchem, lokalnym drganiem, rozżarzeniem i kamerą.
- `VfxImpulseEnvelope`: matematyka zaniku i ograniczeń siły efektów testowana niezależnie.
- Rozszerzona scena pokazowa, testy EditMode i PlayMode oraz instrukcja dla Codexa.

## Instrukcja demonstratora
1. Otwórz **odizolowaną kopię** projektu Unity 6000.3.14f1 z URP.
2. Skopiuj cały `Assets/RoboRougelikePackages/VFX001` z archiwum z zachowaniem `.meta`.
3. Uruchom `Tools > RoboRougelike > VFX001 > Create URP Materials`.
4. Uruchom `Tools > RoboRougelike > VFX001 > Build Demo Scene`.
5. Otwórz i uruchom `Demo/VFX001_Showcase.unity`.
6. Wypróbuj przyciski `V5: ...` oraz wcześniejsze demonstracje.
7. Uruchom Unity Test Runner w EditMode i PlayMode.

**Ważne:** Materiały generowane przez menu Editor w folderze `Resources` muszą zostać zapisane wraz z projektem przed budowaniem Windows. Samo `Shader.Find` nie gwarantuje, że shader nie zostanie usunięty podczas stripowania builda.

## Bezpieczeństwo
Wszystkie dodatki kosmetyczne. Nie zmieniają HP, obrażeń, sił Rigidbody, colliderów, rzeczywistych grafów części ani mechaniki rozłączania. Wymagane jest podłączenie do **prawdziwych zdarzeń gry** przez adaptery Codexa. Nie montować mechanicznych animacji i kamery na fizycznym korzeniu pojazdu lub aktywnej strukturze celowania.

## Status testów
- **Statyczna kontrola archiwum:** wykonywana po spakowaniu.
- **Kompilacja Unity, EditMode, PlayMode, manual QA:** jeszcze NIE wykonane w tej sesji; wymagana weryfikacja przez Codexa.

## Dokumentacja
- `Docs/CHANGELOG_v4_v5.md`
- `Docs/INTEGRATION_PL.md`
- `Docs/QA_CHECKLIST_PL.md`
- `Docs/CODEX_INTEGRATION_PROMPT.md`
- `Docs/STATIC_VALIDATION_RESULT_v5.json`
