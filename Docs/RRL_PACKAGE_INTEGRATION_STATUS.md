# Package integration status — W6–W9

Frozen W5 PR20: `2154b4341dd6b6fefb135187dc07b4b12d44f908`. Unity6000.3.14f1. No shared-branch merge.

The user supplied the latest Downloads archive after initial search. Outer SHA256 **PASS**, exact requested `bbbc3bdf74ad1e4445fc5ca231eddc60f189090cc68104e6a23e1b1f9469407b`. Original `verify_and_stage.py --verify-all`: **18/18 PASS**, exit0. Twelve selected originals; six older MAP/VFX references verified, not reimported. [Source proof](PackageIntegration/source-verification.json). Temporary source block resolved.

Source integrity is not gameplay readiness. Each row stays BLOCKED until its own compile/targeted tests/runtime/native gates pass. Current implementation and remaining gates are recorded per package below.

| ID | Source / hash | Integration PR | Runtime state | Tests | Decision / next dependency |
|---|---|---|---|---|---|
| COM-001 | kit ZIP `bc671889fb26491a7cbbd5cf415891c6224898be3140b888a1bf5c749bde345f`; source corrections PR#4 `cc9fbfbf895a2285a536ecfe70b9557c04921488` | Not created | NOT IMPORTED | Source PASS; Unity/native NOT RUN | BLOCKED: implementation gates pending |
| DEF-001 | kit ZIP `66a6320db5463ade7f57c13a950b31df15c83cdbc03ee5b290172a4f96d1308c` | [Draft #22](https://github.com/C8LyNn/RoboRougelike/pull/22) | INTEGRATED: real shield/HP/charge/repair/AI loot | Edit55/55 + Play21/21, native0; [report](PackageIntegration/DEF-001_QA.md) | BLOCKED: W6 native/full regression pending |
| DEF-002 | kit ZIP `38b7a2aec225e88f1f203ab367110e081e9e304a4d012baa5a1dd694f0ceb591` | [Draft #26](https://github.com/C8LyNn/RoboRougelike/pull/26) | INTEGRATED: actual P17/rocket interception, physical mount and canonical salvage | Edit filter30/30 + Play23/23; final W6 regression15/15, native0; [report](PackageIntegration/DEF-002_QA.md) | BLOCKED: W6 native/full regression pending |
| MAP-R6 | kit ZIP `137e2e55065b3ba373fca2f8a32f5a47f0d01d6329560059228f06ba08bcd80e`; source corrections PR#3 `79b9605e07864b60fb38892c9e38f1cbb4072218` | Not created | NOT IMPORTED | Source PASS; Unity/native NOT RUN | BLOCKED: implementation gates pending |
| MOV-011 | kit ZIP `5de4ef944023395f42cd239d3d6a466e45bcabad71049d6c90416aba589e5fa6` | Not created | NOT IMPORTED | Source PASS; Unity/native NOT RUN | BLOCKED: implementation gates pending |
| VFX-001-v5 | kit ZIP `86133261e6693cf90017e7434fdc26f02e72bb4443b3f493a8de835ca6a7d597`; source corrections PR#6 `43010f6ac3aecff24d2a84d116ee686faeecd667` | Not created | NOT IMPORTED | Source PASS; Unity/native NOT RUN | BLOCKED: implementation gates pending |
| WPN-001 | kit ZIP `1a4dd6b2fe9ad92a6655e880bda4faea72c7737638be5edbf0c5defec96da2db` | [Draft #21](https://github.com/C8LyNn/RoboRougelike/pull/21) | INTEGRATED: P12/R3 physical API gameplay PASS | Edit43/43 + Play18/18, native0; [report](PackageIntegration/WPN-001_QA.md) | BLOCKED: W6 native/full regression pending |
| WPN-002 | kit ZIP `887f10cc5f7d7103e293cf49d88f0f1862d9201ef5381eb8edc4d02a1132b392` | [Draft #23](https://github.com/C8LyNn/RoboRougelike/pull/23) | INTEGRATED: physical P17 penetration, R3 ammo and AI salvage | Edit45/45 + Play22/22; shared regression8/8, native0; [report](PackageIntegration/WPN-002_QA.md) | BLOCKED: W6 native/full regression pending |
| WPN-003 | kit ZIP `5186dbb7eebada6969f8b9f5fb9b853ae11eb9cdae06968bb05aa3e787242c2d` | [Draft #24](https://github.com/C8LyNn/RoboRougelike/pull/24) | INTEGRATED: real continuous ray/heat, physical gun and AI loot | Edit30/30 + Play17/17, native0; [report](PackageIntegration/WPN-003_QA.md) | BLOCKED: W6 native/full regression pending |
| WPN-004 | kit ZIP `d03102b35ea7ed08e93387ee866689fa7cd897145d16438633f328c2eec25179` | [Draft #25](https://github.com/C8LyNn/RoboRougelike/pull/25) | INTEGRATED: original swept rocket/AOE, P12/shield and ammo/loot | Edit39/39 + Play17/17; shared regression12/12, native0; [report](PackageIntegration/WPN-004_QA.md) | BLOCKED: W6 native/full regression pending |
| WPN-005 | kit ZIP `240afef467981b1b051c241212aa39392c1e97b806915468900dd58dbee2efe8` | Pending Draft | INTEGRATED: actual contact saw, jam/heat, shared garage preview and melee AI | Edit32/32 + Play18/18; final shared18/18, native0; [report](PackageIntegration/WPN-005_QA.md) | BLOCKED: W6 native/full regression pending |
| WPN-006 | kit ZIP `19afc48aa519f093b476af2df779a52daf92766a787af54a1f8a9a22b0c342c0` | Not created | NOT IMPORTED | Source PASS; Unity/native NOT RUN | BLOCKED: implementation gates pending |
| COV-001 | PR#16 `b2d22c4f7ed7dd9e07fec0fa6137272c541138a6` | Not created | NOT IMPORTED | Git source audited; Unity/native NOT RUN | BLOCKED: implementation gates pending |
| HAZ-001 | PR#12 `d102a27bb7cb56349a4f125ee828712825221922` | Not created | NOT IMPORTED | Git source audited; Unity/native NOT RUN | BLOCKED: implementation gates pending |
| SEN-001 | PR#15 `f70dc1612ef5293a5f6592dee0024f998bdb7239` | Not created | NOT IMPORTED | Git source audited; Unity/native NOT RUN | BLOCKED: implementation gates pending |
| RAC-001 | PR#13 `7884f2737197e26769842c087dcd2a1ead75ac95` | Not created | NOT IMPORTED | Git source audited; Unity/native NOT RUN | BLOCKED: implementation gates pending |
| OBJ-001 | PR#14 `cdc36875efe4b5661a7c41c0b7b60d0010850e2b` | Not created | NOT IMPORTED | Git source audited; Unity/native NOT RUN | BLOCKED: implementation gates pending |

W6 order: WPN001 → DEF001 → WPN002 → WPN003 → WPN004 → DEF002 → WPN005 → WPN006. Shared weapon/damage/device adapters are developed with the first packages, using one R3/P12/R6 authority. W7 follows; W8 evaluates COM/SEN overlap before code and MOV011 uses R6; W9 tests isolated road/race/reactor activities before any arena replacement.
