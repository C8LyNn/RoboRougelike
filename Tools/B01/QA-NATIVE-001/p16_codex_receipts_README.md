# B01-60 PASS16 — independent native-evidence intake (two scoped recipients)

**Only QA tooling, not a Unity gameplay package.** B01-60 independently parses and validates **original Codex-produced native evidence** at immutable Git source `589b60a912096d1e9917cb0bbac73a53e7543b4f` from Codex PR #310. No game-source mutation or independently run Unity.

## Reproduce on Windows and Linux

```sh
python Tools/B01/QA-NATIVE-001/p16_receive_codex_cx02_player.py
python Tools/B01/QA-NATIVE-001/p16_receive_codex_cx03_integrity.py
python Tools/B01/QA-NATIVE-001/p16_receive_codex_reward_atomicity.py
```

Both scripts require only Git and Python 3.11. They fetch pinned exact original Git objects, verify original Git blob identities and reject unexpected/missing/malformed evidence. The optional negative tests corrupt **in-memory copies of receipt data**, not original Codex game, not the Windows Player binaries and not original Unity results. For cross-platform execution, inspect `.github/workflows/b01-60-p16-codex-receipts.yml`.

## Recipient CX-02 — real Windows Player aim results (Codex-authored)

The original pre-fix fixture contained 28 positions, 7 false-negative outcomes from sending only LMB regardless of group. The corrected original recorder uses actual A/LMB or B/RMB for seven weapon families at 10/30/60/100m and recorded 28 case results: 23 positive actual damage/HP values and five correct range-miss zero-damage observations. Verify source SHA, process/exit, complete 7×4 matrix, unique cases, expected group, finite muzzle/direction/aim and actual damage. Six deliberate receipt mutations (missing case, wrong group, positive with 0 HP damage, inverted expected range, invalid bool damage, duplicate) must all be refused. The evidence was originally **executed by Codex on Windows**; this QA runner is the independent document/record receiver.

## Recipient CX-03 — original Unity integrity regression XML

Original `red.xml` (Unity EditMode) has 4 executed cases: **three failing invalid-kind atomic transactions** and one valid control; original process exit 2. Corrected `regression.xml` has **42/42 Passed**, including the original three cases, zero skipped and exit 0. Strict raw XML, complete suite summary, exact three named RED identities, fullname-class-name consistency and unique tests are required; five receipt mutations must be refused. This validates **the recorded Unity outputs**, not an independent execution on B01-60's machine or an independent 42-test gameplay re-run.

## Recipient P16Reward — newly changed real Codex game R1 reward atomicity

A NEW Codex original production source change `Assets/R1/R1RunController.cs` old Git blob `a6759500...` @`589b60a...` → new blob `3db48a...` @`561c223...` addresses the distinct B01-22 observed **invalid three-choice reward catalog**: old controller could pay 80 scrap before failure, potentially repeatedly, while state stayed Combat. Recorded Windows Unity EditMode original `Docs/P16Reward/Evidence/red.xml` one case RED (exit 2), new `regression.xml` **32/32 Passed**, including target `VictoryRewardAtomicityTests.InvalidRewardCatalogCannotAwardScrapOrRepeatVictory` and three separate original positive controls, exit 0. The third script pins both game C# blobs and the raw XML/exit Git blobs and rejects five adversarial receipt mutations. This is not a direct gameplay simulation and does not verify full post-fix EditMode or ordinary-player reachability. B01-22 is the independent economy producer/owner; B01-60 independently receives the **recorded engine evidence**, not its source implementation.

## Gating and limitations

- The **corrected Player source SHA** is `4aec116282eb583fd852469c2bb7dc7e6d85b9e3`, an ancestor of evidence commit `589b60a912096d1e9917cb0bbac73a53e7543b4f`. Later docs-only evidence commit does not change canonical weapon firing code in this diff.
- Original native Unity activation/license belongs to Codex's environment; B01-60 **did not** launch Editor/PhysX/Windows Player; human UI and independent package G1 remain `NOT_RUN`.
- The latest integrated full EditMode formerly recorded **1288/1303 with 15 FAIL**; a later separately reported 15/15 scoped retest is **not a replacement for a fresh full 1303-case EditMode GREEN**. Do not claim release eligibility from these two focused receipts. Full PlayMode reported 474/474 on owner source, not independently executed here.
- B01-62 independently owns original save/load validation; B01-22 economy; B01-61 cross-feature/arena regression. This QA does not mutate or duplicate their source packages.

## To receive or extend in Codex Inbox #79

Use the source-pinned manifest `p16_codex_receipt_manifest.json`. Read both scripts and original SHA before execution; run the two Python commands on a read-only checkout. Preserve stdout and CI job link. Report `RECEIPT_CONTENT_GREEN` only if original blob hashes, all controls and mutation checks pass. To verify a **new** integrated commit, publish new raw XML, JSON and logs, pin new Git blobs and source/build commit and rerun as a new receiver revision; never carry earlier native G1 or result over by inference.

**Rollback:** revert QA-only commits in Draft PR #245; no production/Codex/CHL/PART state is changed by this package.
