# CAMERA001 R1 actual evidence
Source C# and Unity assets committed to isolated GitHub branch. Exact SHA must be pinned after manifest complete.
C# compiler Unity 6000.3.14f1: NOT_RUN, no Editor.log/exit.
EditMode 840-case loop: AUTHORED TEST SOURCE, NOT_RUN, XML none.
PlayMode 3 scenarios: AUTHORED TEST SOURCE, NOT_RUN, XML none.
Native demo 720p+1080p and physical game aim: NOT_RUN, PNG none.
B01-60 independent QA: PENDING. B01-40 GUID/SHA target import: PENDING.
Any offline math surrogate must be explicitly recorded separately from C# runtime.
No game integration, merge or release.

## Source-independent offline framing surrogate executed 2026-10-25
- Source: Tools/B01/CAMERA001/geometry_probe.js in this branch.
- Execution environment: isolated JavaScript runtime, with equations independently ported from the C# `Camera001Framing`, **not** a C# or PhysX execution.
- Scenarios: 840/840 Light/Heavy x aspects/pitches/yaw samples evaluated, 0 axis-aligned hull crosshair intersections in the port.
- Additional checks: large body increases required camera distance, giant-body zoom saturation, 720/1080 equivalent aspect result; five aggregate assertions, **5/5 true**.
- Native Unity Edit/Play remain **NOT_RUN**. The standalone Node source contains its own exit code and should be rerun by B01-60 independently against the code; its result does not authorize G1.

## W9 R1.1 / RED → FIX → RETEST (source only)
BASE PR#134 HEAD: 894d3488dedb69e2b8d89683d750bbdbfedf4e55. RED: both EditMode and PlayMode asmdef references included UnityEngine.TestRunner and UnityEditor.TestRunner. FIX: remove these direct test runner refs, preserve B01.CAMERA001.Runtime, TestAssemblies, EditMode Editor restriction. RETEST: parsed both on pushed branch; 2/2 JSON assertion sets PASS (runtime ref, optional test assembly, includePlatforms, autoReferenced, forbidden refs). Persistent reproducible checker: python Tools/B01/CAMERA001/verify_asmdefs.py (script checked into source, not itself executed in Unity). No Unity license/Editor, so C# compile/EditMode/PlayMode remain NOT_RUN; B01-60 independent QA and B01-40 G2 pending. This is not G1 or G2 acceptance.
