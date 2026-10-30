# B01-62 / PASS17 WAVE12 — Secure Reattach V2 R2: exact-SHA migration and rollback receiver QA

**Chosen source, ONLY:** [B01-23 Secure V2 R2 #510](https://github.com/C8LyNn/RoboRougelike/pull/510) at immutable `d94400d95d13db7f27e1533df05c9292d5380431`, version `B01-23-SECURE-REATTACH-V2/2.0.0`. **Reject #511 and #494** and pre-existing W07 (unsafe legacy) as parallel installs. This package is *independent B01-62 QA*, NOT the B01-40 formal source G0 acceptance and NOT a second B01-61 source/method test harness.

## Real distinct work

Independent source-package/receiver preflight: derive a **new, disposable Git worktree** at the producer's exact frozen SHA; call its original Python ZIP packager to physically create the authentic **35-member, 99,666 byte** selected ZIP, pre-authorized SHA256 `d4ddfa3528ef0416aefffca72d884000f3b0c9959fb8dff53096487ef95c9387`. Then independently re-open all bytes and verify every manifest file against original selected Git blob bytes, original SHA/parent, original W06 dependency, 14 distinct `.meta` GUIDs including selected V2 GUID, three actual asmdef dependency sets, lack of legacy W07/alternative owner paths, matching 34 source-file records and declared `native_unity=NOT_RUN`, `codex_ack=false`. Original ZIP is the actual source artifact; its GitHub Actions upload wrapper has its own different digest.

The source scanner examines a *provided* receiver path **read-only**, looking for existing target paths (including case-insensitive Windows collisions), legacy W07 source GUID `b017d070000000000000000000000008`, old class/asmdef names, #494 and #511 markers, uninspectable oversized resources or symlinks. A preflight failure is **HOLD**, never silent replacement. The real Codex target receiver is NOT connected here: passing a clean **synthetic disposable** receiver proves scanner logic but does not prove any actual canonical scene is clean.

Dedicated independent negative regressions exercise these receiver configurations and literal write-once scratch stage + byte-for-byte rollback. Rollback rejects foreign, changed or missing files without deleting them; no live target is written or migrated. This is a staging proof for the selected source, not a Unity migration acceptance.

## Reproduce in an isolated checkout

Requires git, Python3.11 and original repository access:
```bash
git fetch --no-tags --depth=1 origin d94400d95d13db7f27e1533df05c9292d5380431
dir=Tools/B01/B01-62/P17W12Secure510Receiver
git worktree add --detach "$dir/.source510" d94400d95d13db7f27e1533df05c9292d5380431
mkdir -p "$dir/.output" "$dir/.receiver"
python "$dir/.source510/Tools/B01/B01-23/P17W10SecureReattachV2/package.py" build --archive "$dir/.output/selected510.zip"
B01_W12_SOURCE="$dir/.source510" B01_W12_ZIP="$dir/.output/selected510.zip" python -m unittest discover -s "$dir" -p test_receiver.py -v
python "$dir/receiver_gate.py" --archive "$dir/.output/selected510.zip" --source-repo "$dir/.source510" --receiver "$dir/.receiver" --receipt "$dir/.output/preflight.json"
```

Run on a disposable checkout only; the CI does not copy files into the canonical project, bypass ownership or merge anything. Positive exit from the clean dummy receiver means `SOURCE_PACKAGE_RECEIVER_PATHS_CLEAR_ONLY` for THAT fixture; it does **not** mean actual Codex receiver verified.

## Independent gate matrix

- **W10 #486 historical RED:** old #488 rearm after physical mutation. Kept frozen; #510 new R2 original `AtomicDockLedger` moves consumed-ID transition ahead of callback and compensates injected failures. B01-61 [#517](https://github.com/C8LyNn/RoboRougelike/pull/517) independently executed 2 rigs × 3 public aliases and rollback at #510 SHA; do not rerun or count its same cases as new B01-62 tests.
- **Source provenance / physical package:** this QA checks all 35 physical bytes, GUID, asmdef, archive hash, and clean/hostile receiver dry-run. This independently strengthens B01-40 G0 intake. **B01-40** alone issues formal `ACCEPTED_SOURCE_ONLY` or `HOLD` after receiver ownership/real migration preflight.
- **Live save identity state:** original W06 `DebrisImpulseLedger` is in-memory (`HashSet<long>`); #510 new `AtomicDockLedger` also holds it privately. No approved game save schema, game serialization/readback/migration or production real receiver state is demonstrated. `SAVE_LOAD_NOT_VERIFIED` and any checkpoint claim remains `HOLD`. A reset destroys current rigs and is NOT a migration to a persistent saved vehicle.
- **G1:** licensed Unity 6000.3.14f1 Editor/PhysX/PlayMode actual `NOT_RUN`; this CI is Python/file IO only. B01-60 owns native.
- **G2 / Codex:** no canonical game state mutation, no offer or Codex ACK, no authorized game integration. B01-00 owns eventual #79 routing.

**Rollback:** close own Draft PR/remove ONLY B01-62 W12 QA paths and workflow on this branch. For disposable test checkout, `git worktree remove --force "$dir/.source510"` only after verifying it is the test-created worktree; delete `.output` and `.receiver` ONLY in own QA directory. No other package source (including #510) must be changed. Never force-reset someone else's receiver.

**Receivers:** B01-40 independent G0 source ZIP and actual receiver preflight; B01-61 route regression (already #517); B01-60 Unity native; B01-00 authorized Codex intake. Historical B01-62 [#503](https://github.com/C8LyNn/RoboRougelike/pull/503) against alternative #494 remains a distinct RED, NOT proof against selected #510.
