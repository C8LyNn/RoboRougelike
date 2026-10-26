# BUILD-001 R1.3 — executable coverage and outstanding QA

| Requirement | New executable test | Static/portable | Native |
|---|---|---|---|
| W9-01 child decorative collider | PlayMode `W9_01_DecorativeChildRayCannotBecomeStructuralSocket`; exact authorized +X collider paired control | source/metadata gate | NOT_RUN |
| W9-02 second core + orphan | Edit `W9_02_TwoLoadedCoreRootsCannotAuthorizeIsland`, `W9_02_StrictInstallReportsDisconnectedFragment` | Python packager + future portable | NOT_RUN |
| W9-03 disjoint/missing bonds | Edit `W9_03_DisjointCellsCannotShareOneOwner`, `W9_03_AdjacentCellsWithoutDeclaredPhysicalJoinCannotRelay`, positive `W9_03_Joined2x2RotationStillWorks` | CI integrity gate | NOT_RUN |
| W9-04 actual penetration 0.31m | Play `W9_04_ActualPhysXPenetrationRejectsLargePhysicalPart`, positive `W9_04_GenuineNonOverlappingBodiesCanCommit`, Edit `W9_04_NullEvidenceMustNotBeCommitted` | gate/source only; NEVER claim surrogate PhysX | NOT_RUN |
| URP material not magenta | Play `DemoURPShaderIsIncludedAndSupported` + 10 images (5 states x 720/1080) | explicit shader file + meta/Resources verified by Python | NOT_RUN |
| No wildcard mobility socket | Edit `TypedMobilityHubDoesNotWildcardIntoStructural` | source | NOT_RUN |
| Original baseline regressions | old `Build001SocketTests` 11 Edit, `Build001PhysicalHitTests` 2 Play | baseline QA #239 original 13/13 was PASS only on **R1.2**, not R1.3 | NOT_RUN on R1.3 |

### QA instructions

Run Unity6000.3.14f1 native in disposable checkout with 11+7=18 Edit, 2+4=6 Play, record XML plus original native exit/logs. If shader failures persist, return exact shader/URP pipeline version, shader importer messages, Player PNG and screenshots; never override with a known-good hidden substitute. Validate both true registered collider and decorative child at x~0.31m; real `Physics.ComputePenetration` must report 0.31m +/-0.002m. Check critical garage warning flags separately (no mobility/no weapon) under existing user policy; BUILD R1.3 does not implement garage UI warning ownership.

Independent QA/recipient import and G2 remain HOLD.
