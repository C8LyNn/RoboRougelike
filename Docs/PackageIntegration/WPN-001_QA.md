# WPN-001 Shotgun integration QA

Base: W5 PR20 `2154b4341dd6b6fefb135187dc07b4b12d44f908`; feature branch `feature/rrl-wpn001-integration`. Source archive `RoboRougelike_WPN001_Shotgun_v1.zip`, SHA256 `1a4dd6b2fe9ad92a6655e880bda4faea72c7737638be5edbf0c5defec96da2db`. Outer kit and original --verify-all18/18PASS. Original staging dry-run/apply imported20Assets files without overwrites; Unity generated new metas only. No old GUID/scene/prefab/config version changed.

**State: INTEGRATED, API/physical gameplay tests PASS. Decision: BLOCKED pending W6 Windows/native and wave regression gates.** Import alone is not called READY. Integration [Draft PR #21](https://github.com/C8LyNn/RoboRougelike/pull/21). Verified source commit `8540c9424e2a58ba406c3f468d4122e10e858f50`; later report-only commits do not alter that runtime.

## Runtime behavior

The original ShotgunModule is bound to an actual R3 gun/P12 HP anchor. Its sink emits eight real Rigidbody/P17 pellets atomically with original world-space directions/inherited velocity, owner rejection, lifetime/range40m and global projectile capacity guard. Co-located pellets ignore collisions within their volley. One shared hit router resolves real collider ownership/faction and then existing P12 HP; no demo HP, vehicle motor or demo projectile is used by gameplay. Dead/debris/own/allied colliders cannot consume a valid hostile shot.

Original prototype settings retained: eight pellets, full cone14degrees, speed58m/s, damage9perpellet, range40m, lifetime2s, cooldown.85s, inherited velocity factor1, recoil0. Existing run damage/reload bonuses apply through the tank's authoritative modifiers. Integration tuning (not original balance approval): mass28kg, HP140, price45scrap; 12-salvo magazine stored in R3PartState, T reload1.5s. Ammo/readiness/reload survive model copies and garage rebuilds. HP0 or structural detachment stops the original module.

The paged garage weapon catalog exposes Shotgun without reindexing Basic0/Automatic1/Heavy2. Mounted weapon HUD shows ammo/reload. A normal escort in encounters with at least3enemies carries/uses an actual shotgun and its condition/ammo enter real salvage. Inventory/repair continue to use the existing atomic W2/W34 flow. Physical bonus1x2x2/XYZ/contacts remain untouched.

## Verified targeted gates

- Expected RED:2/2 new integration tests failed because W5 had no Shotgun catalog entry.
- Fresh final EditMode **43/43PASS**, native0:40original package cases +3new integration cases.
- Fresh original PlayMode **18/18PASS**, native0.
- New actual physical test:8P17 pellets, 72total damage to existing P12 HP exactly once, 12→11stored ammo, same-frame cooldown rejection without second debit, destroyed original module cannot fire.
- Recovered destroyed shotgun: same ID/kind/ammo5 survives paid45-scrap rebuild and DeepCopy.
- [Machine-readable case summaries](WPN-001_test_summary.json). Local raw XML/logs stay under the listed ignored Artifacts/Local directories.

Original package compatibility fixes: PlayMode asmdef incorrectly used Editor-only, so physics tests were classified as EditMode. It now targets Any platform under its existing test define. Demo runtime asmdef also incorrectly made attachable MonoBehaviours Editor-only; Any platform with UNITY_EDITOR constraint permits Editor Play tests while excluding demo code from retail. Original runtime C# algorithms are unchanged. All18original physics cases passed after correction.

The integration fixture uses the existing R6 UnitySetUp/EnterPlayMode pattern and starts real combat (menu intentionally pauses physics). An observed flight trace showed WaitForSeconds in the EditMode enumerator advanced only one physics step; it now waits actual game frames to a Time.time deadline. No game physics solver was changed to accommodate that fixture.

NOT RUN yet: standalone Windows/native UI, full W6 regression, complete six-stage campaign with the whole new W6 loadout set. These execute at wave closure, not after every small edit. Known W5 UIA shutdown limitation remains separate; passing test processes here exited0.
