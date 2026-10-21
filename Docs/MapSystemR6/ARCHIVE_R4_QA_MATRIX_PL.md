# R4 — Quality Gates i przypadki testowe

| Gate | Test | Realny wynik paczki |
|---|---|---|
| G0 | Spójność danych R3↔R4 | Skrypt Python dostępny; wynik w logu QA |
| G0 | Brak dangling ID, zduplikowanych ID i nieistniejących stanów | Unity EditMode, NIEURUCHOMIONE tutaj |
| G1a | Droga start→exit w intact | Unity EditMode, NIEURUCHOMIONE tutaj |
| G1a | Droga start→exit po collapse | Unity EditMode, NIEURUCHOMIONE tutaj |
| G1a | Przejazd nieosiągalny gdy wszystkie drogi zamknięte | Unity EditMode, NIEURUCHOMIONE tutaj |
| G1a | Zbyt szeroki pojazd nie przejeżdża | Unity EditMode, NIEURUCHOMIONE tutaj |
| G1b | MeshCollider istnieje dla pasów | Unity PlayMode, NIEURUCHOMIONE tutaj |
| G1b | Kolizje skrętu, geometria styków, promień, trakcja | NIEZAIMPLEMENTOWANE |
| G2 | Time Attack checkpoint first | Unity EditMode, NIEURUCHOMIONE tutaj |
| G2 | Escape trigger works | Unity EditMode, NIEURUCHOMIONE tutaj |
| G2 | Survival and Boss adapters | Stub only |
| G2 | Gauntlet unchanged | Wymaga wykonania w projekcie przez Codex |

## Blokery do zgłoszenia
- Brak rzeczywistych pomiarów i prefabów pojazdu.
- Brak Unity Editor/test runner w tym środowisku.
- Brak produkcyjnych adapterów AI, damage, building, rewards.

## Kryterium akceptacji R4
G0 PASS + G1a PASS + pozytywny import bez C# errors + PlayMode bez wyjątków + manualna inspekcja 4 scenariuszy. Dopóki nie wykonano testów Unity, wydanie oznaczyć DEVELOPMENT/UNVERIFIED, nie READY.
| G1b | Obwiednia `Physics.CheckBox` na jawnej masce przeszkód | Unity PlayMode, NIEURUCHOMIONE tutaj |
| G1a | Rzeczywiste nachylenie z koordynatów węzłów | Unity EditMode, NIEURUCHOMIONE tutaj |
| G0 | Orientacja górnej strony powierzchni drogi | Offline Python + Unity PlayMode |
