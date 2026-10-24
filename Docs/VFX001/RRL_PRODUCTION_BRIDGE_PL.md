# RRL — VFX-001 v5: implementacja na rzeczywistej grze (read-only bridge)

## Co wdrożono

- Zaimportowano pełną zweryfikowaną paczkę VFX-001 v5 (runtime, demo, EditMode/PlayMode, shadery, .meta) z oryginalnego archiwum.
- Dodano 6 zasobów materialowych URP Resources odwołujących się do shaderów VFX-001. Nie zastąpiono istniejących materiałów gry.
- Nowy `Assets/R6/Vfx001R6LiveBridge.cs` instaluje się automatycznie **tylko** dla scen z `R1RunController` po `AfterSceneLoad`.
- Nasłuchuje pośrednio istniejących znaczników `R6CombatFeedback` (`Błysk trafienia`) i nie zmienia kodu R6/obrażeń. W efekcie spawnuje pooled `VfxEvent` `Hit` albo `Explosion`.
- Dodatkowo znajduje nowe `R3Debris` podczas walki/poligonu co 0.25 sekundy i emituje `Detached` jeden raz dla każdego znalezionego obiektu.
- Filtr efektów oparty na wielkości istniejącego znacznika R6 jest wyłącznie kosmetyczny: nie jest autorytatywnym zapisem obrażeń ani trafienia.
- Mechanika nie dotyka `Rigidbody`, HP, zasad nagród, AI, sterowania pojazdu ani `R1RunController`.

## Ryzyka do sprawdzenia w Unity

- Adapter zakłada, że R6 nadal tworzy obiekty o nazwie `Błysk trafienia`. Zmiana nazwy przez innego agenta wymaga korekty adaptera.
- `Explosion` rozpoznawany po rozmiarze wizualnego znacznika (>=0.34); to heurystyka — przy nowym systemie walki preferowane będzie jawne, typowane zdarzenie wyemitowane już po faktycznym rozliczeniu HP.
- Testy shaderów URP/material YAML i Windows Player NIE WYKONANE w tej sesji — Codex sprawdza import, layout, efekt i usuwanie błędów shaderów.
- Oryginalne demo/efekty nie są automatycznie wciskane w prefaby; działa tylko produkcyjny most automatycznego podpinania do głównej sceny.
- VFX daje dodatkową warstwę nad starszymi znacznikami R6; docelowo po walidacji wizualnej można wyłączyć stare sfery bez dotykania funkcjonalności bojowej.

## Test odbioru dla Codexa

1. Z checkoutu nowej gałęzi uruchom Unity 6000.3.14f1 (URP), import i kompilację, brak błędów.
2. EditMode i PlayMode VFX001 oraz regresja MOV001-009.
3. Otwórz `Assets/scene 1.unity`, włącz Play. Sprawdź czy do obiektu RunController dodany został `Vfx001R6LiveBridge` i czy istnieje dziecko `VFX001 Live Pool (cosmetics only)`.
4. Zaatakuj wroga: po starym błysku R6 pojawia się pula cząsteczek VFX001; sprawdź `VfxPoolManager.ActiveCount`.
5. Zniszcz moduł, poczekaj na `R3Debris`: pojawia się `Detached`. Odłamki nadal zachowują dotychczasową fizykę.
6. Wyłącz `visualEffectsEnabled` przez Inspector / `SetVisualEffectsEnabled(false)`: liczba aktywnych VFX wraca do 0, mechanika obrażeń pozostaje ta sama.
7. Wykonaj kilkaset strzałów i monitoruj FPS, pamięć, GC, pulę i limity. Zrestartuj wyprawę/scenę — nie może pozostać osierocony pool.
8. Zbuduj Windows Standalone, skontroluj logi shaderów i widoczność 6 materiałów w Resources. Porównaj liczbę HP po trafieniu z bazą: NIE może się zmienić.
9. Zanotuj ewentualny konflikt z nieopublikowanym kodem Codexa. Nie nadpisuj PR #1 i nie scalaj automatycznie.

**Status:** SOURCE IMPLEMENTED / STATIC SOURCE SHA PASS / UNITY NOT RUN / DRAFT PR — WAIT QA.
