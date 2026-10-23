# DEF-002 Point Defense integration QA

Source: verified kit DEF002PointDefense, SHA256 `38b7a2aec225e88f1f203ab367110e081e9e304a4d012baa5a1dd694f0ceb591`. Feature feature/rrl-def002-integration; base WPN004 checkpoint7754a41 on frozen W5. Original dry-run/apply imported14 Assets source files without overwrites. Existing GUIDs/scenes/packages unchanged.

The original controller and swept interceptor now run on a physical R3 four-cell1x2x2 device, visible in shop, inventory and combat HUD. One P12 HP/collider owns the module. Its shared availability gate and a single controlled source physics clock stop it on loss, disabled combat, pause or transition. An inactive owned interceptor template provides actual counter-projectiles; they target only the original locked threat and are cleaned on transitions. Projectile adapters expose existing P17 kinematics (one-hit interception, no invented HP pool) and original rocket velocity/12HP. Rocket interception calls original ApplyInterceptionDamage, never fake proximity damage. Friendly/owner threats are rejected by original faction/trajectory/LOS logic.

Original default detection24m,scan.16s,look-ahead5s,defended radius5m,traverse200deg/s,tolerance5deg,shot interval.24s,speed65m/s,lifetime2s,radius.15m,recoil0,energy0 remain. Explicit live balance: use finite40-round magazine and6 damage/interceptor so two hits can stop the original12HP rocket in its24m approach; original default damage1 and disabled ammo consumption are not silently presented as live defaults. Canonical R3 ammo/cooldown/reload persist; T reload3s. Chassis tuning:35kg,160HP,70scrap,limit2,full XYZ/contact footprint. Source controller gained bounded state restoration and an opt-out automatic clock/public Advance; its targeting, leading and interception algorithms are unchanged.

Actual enemy devices now attach to a real armor part. The shared mount helper also corrects the prior shield's bare-root attachment: destroying mounting armor carries the physical device into detached debris, revoking availability without another solver. A normal enemy in groups4 can carry shield and PDS; its salvage uses actual condition and resources.

A RED lifecycle test caught lossy enemy loot conversion: weapon cooldown/reload were reset0. R4SalvageItem now supports an immutable copy of the complete canonical R3PartState; weapons/shield/PDS use it, retaining current and future resource fields, ID, rotation and contacts. Legacy constructors remain compatible. Repair does not refill resources or erase cooldown.

## Evidence

- RED2/2: missing physical PDS and lost cooldown (expected5.005...,actual0).
- Original+initial integration Edit filter PointDefense30/30 PASS, original Play23/23 PASS, native0.
- Final shared RRLW6 regression15/15 PASS, including3 PDS lifecycle cases and all previous package integration cases. Original28 Edit cases plus3 PDS cases are31 distinct compatible Edit cases across these commands, not one invented31-case run.
- Mounted PDS actually depletes the original rocket HP to0 with real interceptors; natural impact/lifetime resolution cannot satisfy the test. Module HP remains unchanged; ammo is consumed, capture/rebuild/paid repair retain remaining rounds.
- Actual enemy mounting armor destruction immediately makes source controller non-operational; detached module becomes selectable damaged salvage, paid repair retains7 rounds and its exact cooldown.
- Actual enemy shotgun salvage retains ammo3 and exact cooldown/reload deadlines.

Native Windows, full W6 campaign and full regression subsequently passed; see the current wave report. All physics tests above are API-driven and distinct from native player-input QA.

**Current decision: READY in the W6 RC2 aggregate, with the documented Unity/UIA shutdown limitation.** API-driven physics is recorded separately from native player-input gameplay.

[Machine-readable test evidence](DEF-002_test_summary.json). Raw XML and logs: ignored Artifacts/Local paths listed there.

Draft PR #26: https://github.com/C8LyNn/RoboRougelike/pull/26; runtime SHA b92c3f41c02b155a6b8a6de969ebf71fff8fe12d.


Current aggregate source `8cf4ff8c80178f04f8d60dc6f57d03a2e7f7f32f`: all W6 gates and known limits are recorded in [W6 wave QA](W6_WAVE_QA.md). Native per-device checks use APIs/real physics; UI-driven garage/campaign flow and virtual-input combat are identified separately. Earlier frozen feature SHAs do not include all shared closure fixes.
