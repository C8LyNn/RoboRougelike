# MAPGEN-DIAG-001 — executed evidence and provenance
## Generator immutable original SHA
`003bced54480d509ffc0d49d26daf87160a8d609`, full stacked PR #99+#103+#211. Source Git blob identities of Planner/Validator/Data/original PlayMode test verified by Actions before compiling B01-21 diagnostics. No changes to MAPGEN source.

## Our executed headless tests
- B01-21 `a7f9b4a64a09fec0f7ed51f86b6c666cfc5a0d8f` [Actions run 40186564053](https://github.com/C8LyNn/RoboRougelike/actions/runs/40186564053): actual .NET8 build/runtime **PASS 27/27**, 0 unexpected FAIL. Run process and all cases preserved by actions/upload-artifact `if:always()`.
- [Artifact 12595589268](https://github.com/C8LyNn/RoboRougelike/actions/runs/40186564053/artifacts/12595589268): **31 files**, `results.json`, readable `cases.md`, **27 separate JSON receipts**, console log and exact branch SHA (some files aggregate); 23,258 bytes ZIP, workflow log reported archive SHA256 `54699d10f3630c5f32fc47dc4e7cb988c9656879c674c2297b53dce3c85f10b3`. That archive digest was reported by GitHub Actions and was not independently rehashed by B01-21.
- PIT original unsupported Start=Finish returns **START_BLOCKED** (confirmed earlier rejection, original assertion defect), separately constructed supported endpoints with middle floor gap returns **SPINE_FOOTPRINT_BLOCKED**. 12 default × 7 Heavy suspicious × 5 Light narrow (24 per-seed) headless blueprint/route/forward waypoint/seam tests PASS. Explicit monolithic UnityTest early-abort diagnostic PASS.
- Fixed subsequent `cases.md` header formatting; exact latest CI receipt should be consulted separately, do not treat older run as latest source certification.

## Other agent's actual Unity evidence — not B01-21's own execution
- Codex [QA PR #226](https://github.com/C8LyNn/RoboRougelike/pull/226), [report](https://github.com/C8LyNn/RoboRougelike/blob/d799878ce8f814b74bd47f8c664c86a954a1a1bb/Docs/MAPGEN001NativeQA/MAPGEN_R32_NATIVE_QA.md): Windows Unity6000.3.14f1 compile PASS; original Edit22/23 exit2, Play5/6 exit2.
- Real actual PR125/OBJ210 R6 wheel full routes PASS **19/19 attempts, 17 unique configs**, 12 basic + 7 historical Heavy; API-steered (not manual), independent of our headless surrogate. Three Light Course rows each include one zero-ground-contact physics query. Separate optional ramp Light PASS, Heavy FAIL_PROBE_SPAWN at side approach. Original URP surfaces render magenta and producer root .meta omitted from package manifest. Not independently replayed by B01-21/B01-60.

## Open gates
Our diagnostics = **EXECUTED C# SOURCE PASS**. Original native suites = **RED**. Physical source R6 native **Codex QA PASS 19/19 within tested matrix**, but independent B01-60 **PENDING**, B01-40 target G0 **PENDING**, B01-00 G2 **HOLD**. No B01-21 licensed Unity/PhysX execution, game integration or merge.
