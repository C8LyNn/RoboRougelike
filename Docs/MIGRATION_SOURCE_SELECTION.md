# Wybór bazy migracji (2026-10-21)

Przeszukano lokalne katalogi projektów na pulpicie (w tym Prototypy i historyczne backupy); sprawdzono również znane lokalizacje Unity Hub i katalogi Documents/Downloads. Wersję wybierano przez zawartość Assets/Packages/ProjectSettings i istniejące dowody testów, nie przez datę katalogu. Tabela nie publikuje prywatnych ścieżek konta systemowego.

| Kopia lokalna (względem Prototypy) | Unity | Pliki source/config/meta | Tylko w kopii / tylko w bazie / różne |
|---|---|---:|---|
| Robocraft Rougelike | 6000.3.14f1 | 105 | 79 / 601 / 4 |
| RoboRougelike/Docs/Backups/R1-20261019-223236 | 6000.3.14f1 | 129 | 0 / 498 / 8 |
| RoboRougelike/Docs/Backups/R2-20261019-230658 | 6000.3.14f1 | 171 | 0 / 456 / 18 |
| RoboRougelike/Docs/Backups/R3-20261020-010730 | 6000.3.14f1 | 171 | 0 / 456 / 15 |
| RoboRougelike/Docs/Backups/R4-20261020-031009 | 6000.3.14f1 | 192 | 0 / 435 / 19 |
| RoboRougelike/Docs/Backups/R4_1-20261020-113330 | 6000.3.14f1 | 212 | 0 / 415 / 20 |
| RoboRougelike/Docs/Backups/R5-20261020-154302 | 6000.3.14f1 | 219 | 0 / 408 / 20 |
| RoboRougelike/Docs/Backups/R6-20261020-185349 | 6000.3.14f1 | 245 | 0 / 382 / 20 |
| RoboRougelike | 6000.3.14f1 | 279 | 0 / 348 / 11 |
| RoboRougelike-MOV001-009 | 6000.3.14f1 | 627 | 0 / 0 / 0 |
| RoboRougelike-MOV009 | 6000.3.14f1 | 335 | 0 / 292 / 13 |
| RoboRougelike-POSTR6-Multi | 6000.3.14f1 | 335 | 0 / 292 / 13 |

Wybrano **RoboRougelike-MOV001-009**. Wszystkie 627 plików bazowych zostały skopiowane, bez utraty scen, skryptów, danych, GUID-ów lub testów. Trzy różnice prywatności/infrastruktury w kopii: wyczyszczenie `cloudProjectId` i `organizationId` w ProjectSettings, wyczyszczenie `m_LastProjectPath` w layout szablonu oraz automatyczne wyłączenie UnityConnect (`m_Enabled: 0`) przez Unity po odłączeniu prywatnego konta. Zachowano stan UnityConnect użyty podczas świeżych testów/buildów. Zachowano wersję silnika, manifest/lock i konfigurację rozgrywki. Oryginalne kopie pozostają na dysku.

Dowody sprzed migracji (sprawdzone pliki XML/summary oraz raporty w kopiach, nie samo nazewnictwo katalogów): R6 — 96/96 testów i udane buildy; MOV009 — 146 EditMode + 14 PlayMode = 160/160; MOV001–009 — 346 EditMode + 176 PlayMode = 522/522, dwa udane buildy, standalone headless i widoczny po 49 asercji. POSTR6-Multi ma identyczne 335 plików source/config co MOV009 i nie zawiera późniejszych integracji. Robocraft Rougelike jest wcześniejszym szkieletem. Historyczne backupy przedstawiają starsze iteracje.

Baza zawiera sześć spotkań Gauntlet, garaż 3D, salvage/inwentarz, zniszczalne moduły i istniejące bronie/AI oraz dziewięć MOV: Booster, JumpDrive, Blink, Gyro, Omni, Grapple, Hover, Tracks, ActiveSuspension. Nie importowano nowych MAP-SYSTEM R3–R6, VFX-001 v1–v5, WPN001/002, DEF001 ani MOV010/011.

Te wcześniejsze wyniki uzasadniają wybór bazy. Nie są wynikami nowego uruchomienia CI. Świeże QA kopii migracyjnej, porównanie nazw przypadków i Actions opisuje raport migracji.
