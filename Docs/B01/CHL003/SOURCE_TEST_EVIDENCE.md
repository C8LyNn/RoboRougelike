# CHL-003 R1 — source-only RED→FIX→RETEST receipt

All facts below derive from actual GitHub Actions on the exclusive CHL producer branch; they are **not** proof of licensed Unity compilation or real completed obstacle crossings.

- [RED run #40476956772](https://github.com/C8LyNn/RoboRougelike/actions/runs/40476956772) exercised exact new C# `CHL003CourseCore.Configure` and failed with real `CHL003_NOT_CONFIGURED`: Configure invoked Retry before initial phase Ready.
- Runtime fix in `CHL003CourseCore.cs` sets Ready before shared reset, without relaxing any assertions.
- [RED run #40477121952](https://github.com/C8LyNn/RoboRougelike/actions/runs/40477121952) then compiled/ran actual state engine 17,612/17,612 PASS but independent Unity importer source gate failed: `Demo.meta` had a 33-character GUID.
- Corrected offending Demo folder importer to 32 hexadecimal characters; verified all 18 package `.meta` GUIDs unique and syntactically correct.
- [GREEN run #40477389729](https://github.com/C8LyNn/RoboRougelike/actions/runs/40477389729) compiled/executed exact core C# 17,612/17,612 PASS; actual source/meta/scene GUID negative fixture and importer gate PASS, 10 named EditMode and 6 named PlayMode tests counted as AUTHORED ONLY.
- New source-only GUI controls, visible nonblocking checkpoint posts, demo directional illumination and archived CI recipe are extra producer refinements after GREEN; **final SHA requires another entire CI retest**, and all native Unity results remain NOT_RUN.
- Core physics interface is code and scene authored only: Unity Test Framework XML/compile/PlayMode, Physics.ComputePenetration behavior, Light/Heavy playthrough, 3× retry and Windows build have not been executed in Unity.

No SOURCE_PACKAGE_FREEZE/CODEX_READY claim until physical SHA256 receipt, CI source retest, B01-40 independent G0 and B01-60 Unity QA. B01-00 only may grant G2.
