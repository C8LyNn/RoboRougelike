# B01-23 / PASS17 W12 — FROZEN #510 ONLY (read-only producer-side handoff)
This package is **NOT a new docking controller or independent G0**. It does NOT modify #510, #511, #494, W06, Codex, main, develop, existing Unity assets or any receiver. A separate isolated four-file Draft PR provides an executable source identity/migration-preflight helper; its own SHA is **NOT** a new Secure V2 candidate SHA.

**Only candidate** [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) Secure V2 R2 v2.0.0 source `d94400d95d13db7f27e1533df05c9292d5380431`. Previous V2 v1 [#488](https://github.com/C8LyNn/RoboRougelike/pull/488), legacy alternative [#494](https://github.com/C8LyNn/RoboRougelike/pull/494), and competing v1.1 alternative [#511](https://github.com/C8LyNn/RoboRougelike/pull/511) never co-install. The original old W07/W08/W09 components are not migrated or allowed in exclusive prepared receiver. **Original #510 source remains frozen**; no merge or Codex ACK.

**Original #510 evidence:** [GitHub Actions #41069853535](https://github.com/C8LyNn/RoboRougelike/actions/runs/41069853535) completed SUCCESS on exact #510 SHA Windows+Ubuntu: per OS 241 source transactional + 186 source public aliases + 20 Git package + 12 R2 provenance = **459/459**. Real original physical inner ZIP **99,666 bytes**, **35 entries**, SHA256 `d4ddfa3528ef0416aefffca72d884000f3b0c9959fb8dff53096487ef95c9387`; 34 original Git files + manifest, 14 Unity meta GUID and three asmdefs. [Windows artifact #12983224073](https://github.com/C8LyNn/RoboRougelike/actions/runs/41069853535/artifacts/12983224073) / [Ubuntu #12984572135](https://github.com/C8LyNn/RoboRougelike/actions/runs/41069853535/artifacts/12984572135) are **outer wrappers**, not the inner ZIP hash. Independent B01-61 [#517](https://github.com/C8LyNn/RoboRougelike/pull/517) Windows+Ubuntu 167/167 source tests per OS [run #41070443412](https://github.com/C8LyNn/RoboRougelike/actions/runs/41070443412). NONE is native Unity/PhysX G1.

## Read-only tooling, actual executable
This tool checks original selected #510 Git source IDs and reconstructs its published physical ZIP *byte-identically* from immutable Git blobs. It scans a supplied **already staged** external source directory; rejects incorrect #510 blob bytes (therefore any #511 sources sharing V2 paths), W07/W08/W09 and #494 paths, old exact Unity MonoScript GUIDs in .unity/.prefab/.asset, old host C# type refs, and unexpected extras in the exclusive selected asset directory. It does **not** copy, modify, delete, rollback or install any target files. A passing test on synthetic staged fixtures is NOT proof of an actual Codex receiver scan.

Run on a W12 helper checkout (after `git fetch origin refs/pull/510/head`):

```sh
python Tools/B01/B01-23/P17W12Freeze510001/freeze_gate.py freeze
python -m unittest discover -s Tools/B01/B01-23/P17W12Freeze510001 -p test_freeze_gate.py -v
python Tools/B01/B01-23/P17W12Freeze510001/freeze_gate.py verify-artifact --zip /path/to/ACTUAL_INNER_510.zip
python Tools/B01/B01-23/P17W12Freeze510001/freeze_gate.py scan-receiver --receiver /path/to/ALREADY_STAGED_SCRATCH
```

**Remaining QA:** B01-40 independently downloads original #510 physical artifact, performs its own exact-SHA/GUID/asmdef/atomic staging+rollback, including extra files and foreign empty directories, and issues formal G0 PASS/HOLD. B01-61/62 independently retest #486 transaction and all aliases on **#510**. B01-60 must run licensed Unity6000.3.14f1 real Edit/PlayMode/PhysX or report NOT_RUN. G2 HOLD. B01-00 only may source-offer in #79 with explicit future Codex ACK; no integration or merge. This read-only helper is never itself CODEX_READY.
