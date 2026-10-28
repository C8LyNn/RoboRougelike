# PASS 17 — checkpoint reload fix and corrected build

Gameplay/build source `203a0805251ed28293888e43f2d49f0df63767c6`, branch `feature/pass17-reload-fix`, Draft PR #383 based on #382, which is based on #310. Unity 6000.3.14f1 / Windows x64. No merge to main or develop. See `Docs/P17/QA.md` on the base PR #382 for the LD02 and checkpoint implementation and its earlier results.

## Reproduction and fix

The checkpoint source `e605d4a8` lost an expired positive reload deadline for an owned weapon stored in inventory. An actual R3 railgun was restored, remounted and driven through `RRLRailgunBinding`; the pre-fix test produced 0/12 rounds (RED, `red4.xml`, exit 2). The first three development attempts had test-fixture initialization errors and are not counted as the gameplay reproduction. The fix preserves the pending reload marker until the existing binding processes it. This changes neither magazine capacity nor balancing.

Native Unity final targeted checkpoint + reload tests: 6/6 PASS, exit 0 (`green.xml`). This includes real weapon binding, save/load, damage, IDs, wallet, corruption, version, group assignment and remount. Windows Player **specific expired-reload case NOT_RUN**; the existing save/restart/resume Player scenario passed.

## Corrected candidate

- Compile and Windows x64 build: PASS, exit 0, source `203a0805`.
- Windows Player from the extracted ZIP: wins, loss and save/restart/load/next fight each PASS with exit 0. These use API-driven victory/defeat, not an unassisted human playthrough; the AI shell is a physical projectile. No fatal errors or exceptions found in the three Player logs.
- All 212 extracted files matched staging SHA-256.
- Full PlayMode on this SHA: PASS 474/474, 0 skipped, exit 0. Full EditMode on this SHA: IN_PROGRESS. The prior full EditMode on `be2c4b6a` failed three cases and skipped one policy-dependent case; those three passed isolated rerun. The later `e605d4a8` full EditMode was deliberately interrupted without result when this defect was discovered. Prior full PlayMode on `e605d4a8` passed 474/474 but is not a result for this SHA.
- Human GUI, full manual combat, camera occlusion, and moving-target aiming: NOT_RUN. Automated aim on `e605d4a8` passed 28/28 stationary cases with both A/LMB and B/PPM; this one-line reload change was not part of that Player.

[Corrected Windows x64 ZIP on Google Drive](https://drive.google.com/file/d/1ioJNmYxnw4EoVBkJgQDde9fGo3WHu2G2/view?usp=drivesdk), 39,634,668 bytes; SHA-256 `97715fbaa31f66d4546c731994200fceb92340a82a659ec4538f278651748664`. Connector reported the matching file size but did not expose a remote hash. Local copy: `C:\Users\kubap\Desktop\Prototypy\Releases\RoboRougelike_PASS17_203a0805\RoboRougelike_PASS17_LD02_Save_Windows_x64.zip`.

## Manual playtest

Extract the whole archive and run `RoboRougelike/RoboRougelike.exe`. Choose a robot, prepare it, choose `Arena LD02 • walka` from the garage and defeat the enemies. Choose rewards, inspect and repair parts, then save in the garage. Exit and use `Wczytaj wyprawę` from the main menu; verify parts and resources, mount parts and enter the next battle. Try aiming, cover, camera and multiple drives. The checkpoint applies only in the ordinary expedition garage; no mid-fight autosave. Maps have independent saves.

## Safety note

An early checkpoint test lost its temporary path during Unity domain reload and wrote an identified test-generated checkpoint into the actual game data path. It was moved intact to `C:\Users\kubap\Desktop\Prototypy\RoboRougelike-P17-Save\Artifacts\Local\P17Save\misdirected-test-checkpoint.json`. Whether a pre-existing checkpoint was overwritten cannot be established. Later tests assert an isolated test path.

