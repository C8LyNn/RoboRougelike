# OBJ-001 execution evidence

Product source: `e0ea2d155fca2a6190b38c184d3399104373e4df`, Assets tree `f419cfec0d65eb512c62213ce509265da0861319`. Report/evidence commits after this do not alter product Assets.

- `PR-14.json`, `PR-125.json`: source/dependency snapshots. Neither is represented as merged or independently accepted.
- `original-reuse.json`: original/current SHA256 and exact ordered line matching for all six C# files; all16 original meta files byte-identical.
- `Targeted/`: actual raw XML and retained process exits, including intentional missing-contract/review REDs followed by GREENs. Runs overlap; do not sum counts as distinct tests.
- `Build-process.json`, `build-summary.txt`: actual final dual Windows build and warnings.
- `Native-API/`: controlled real AI/physics plus explicit canonical-damage lifecycle fixture,1280×720.364 assertion evaluations contain21 distinct names because intermission checks repeat. This is not an input-driven victory.
- `Native-Pilot/`: operator handles menus/rewards; combat pilot uses normal input events/aiming,1920×1080. Active AI and physical projectiles, three-wave win, reward/garage/ordinary fight. Process exit records the separate unresolved automation-attached C0000005.
- `Native-Retail/`: fresh ZIP extraction EXE/hash identity and human Alt+F4 result, actual native0, without UI automation. Does not establish a general shutdown fix.
- `binary-audit.json`: Cecil type inspection of retail/QA assemblies; original objective/host actor present and QA types excluded from retail.
- `release-package.json`: complete204-file retail archive, CRC/extraction hashes and exact runtime source.
- `frozen-release-audit.json`: prior W7/W8/B01 ZIPs still match their published hashes.

`full-unity-results.json` and `unity-results.zip` contain the fresh full1021/1021 EditMode and401/401 PlayMode runs, both native0. The final execution ledger records implementation decisions and review dispositions. Full logs remain under the checkout's ignored `Artifacts/Local/`; raw Editor/license/crash logs are not needed to reproduce numerical results.

Reproduce from the isolated checkout with licensed Unity6000.3.14f1:

```powershell
./Tools/CI/Run-Unity.ps1 -Task EditMode
./Tools/CI/Run-Unity.ps1 -Task PlayMode
./Tools/CI/Run-Unity.ps1 -Task Build
```

Use `-Python` to supply an available Python executable if it is not on PATH. Run one Editor per checkout. To execute the controlled native fixture, launch the QA executable with `-obj001-native-output` followed by a new absolute output directory. The fixture is absent from retail. Ordinary-input pilot flags are `-rrl-w2-qa-output DIR -rrl-w2-run-id OBJ001-input-smoke -rrl-w5-diagnostics-output DIR`; the operator chooses Heavy, reactor preparation, deployment and rewards through the real UI. Never label these fixtures human gameplay.

Human play: extract the full ZIP, launch EXE, choose a vehicle and **Obrona reaktora**, then **Broń reaktora** in the preparation garage. WASD/mouse/LPM, Esc pause. Keep600HP reactor alive through2/2/3 enemies; normal bonus/loot and garage follow victory. Enter restarts after loss.
