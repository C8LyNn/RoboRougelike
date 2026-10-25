# B01 mandatory package release gate / W9 amendment
Dispatch HQ-B01-20261025-W9-SOURCE-REPAIR. True native RED compiler evidence: Codex [QA Draft PR #212](https://github.com/C8LyNn/RoboRougelike/pull/212), Unity6000.3.14f1 exits 1 on original CAMERA/BUILD/RAC.
MANDATORY ORDER:
1. STATIC asmdef JSON check: duplicate references or direct UnityEngine.TestRunner/UnityEditor.TestRunner alongside optionalUnityReferences TestAssemblies = RED. Check every Editor & PlayMode asmdef.
2. STATIC C# #if/#elif/#else/#endif nesting checks; missing or stray directive = RED. This is not full C# preprocessing or the actual Unity compiler.
3. STATIC all .cs/.asmdef/.unity source pairs with .meta; GUID format+uniqueness and actual Git SHA1/byte manifest against exact commit, SHA256 if provided. Receiver target collision must be independently checked against actual current target SHA/scope ACK, not just package internals.
4. ACTUAL LICENSED Unity6000.3.14f1 C# compilation is REQUIRED BEFORE expensive testing/handoff. Preserve command/process exit, complete Editor.log, package/host exact SHA and errors; do NOT infer Unity PASS from static or .NET surrogate compilation.
5. Only after real compile EXIT0, run NUnit EditMode+PlayMode XML/exits, package demos/physical 720p/1080 tests, independent B01-60 retest exact source SHA. No NUnit XML = NOT_RUN, not zero test failures.
6. B01-40 independently validates source hash/target GUID, ZIP and install/rollback; B01-00/HQ G2 only after G0/G1 PASS; every new source SHA invalidates older QA. No merge/integration. MAPGEN-001 PR #211 remains NEXT unassigned Codex package.

Reusable independent script: Tools/B01/PackagePreflight/w9_preflight.py; CI compares immutable original and corrected SHA for CAMERA, BUILD, RAC, and runs 12 scanner regression controls. This executes STATIC checks only, G1 Unity NOT_RUN and G2 HOLD despite CI green.
