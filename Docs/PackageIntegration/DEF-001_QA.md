# DEF-001 Directional Shield integration QA

Source: verified kit `RoboRougelike_DEF001_DirectionalShield_v1.zip`, SHA256 `66a6320db5463ade7f57c13a950b31df15c83cdbc03ee5b290172a4f96d1308c`. Base: WPN001 checkpoint `c8df75f` on frozen W5 `2154b4341dd6b6fefb135187dc07b4b12d44f908`. Feature: `feature/rrl-def001-integration`. Original importer dry-run/apply:13 Assets files, no overwrites. New Unity metas only; old GUIDs/scenes/packages unchanged.

**INTEGRATED; targeted API/physics PASS; BLOCKED pending W6 Windows/native/full-wave regression.**

The original generator/receiver now run on one actual physical R3 four-cell 1x2x2 module (all XYZ rotations/contact ports), with one P12 HP component and one collider. The shared device gate reads existing structural ownership, HP, player pause/transition and enemy combat state. It does not add another HP pool or Rigidbody solver. Pre-HP filtering resolves all P12 damage through the original directional receiver once. Defaults:100 charge,100 maximum absorption per hit,12 charge/s regeneration after3s,60-degree half-angle,9m radius; consumed charge is the source resource (optional energy cost remains original0). Gameplay tuning:180 module HP,32kg,60 scrap,limit2. Existing shop/HUD/inventory expose the shield.

Charge and recharge age persist in the canonical R3 state through damage, detach, capture, salvage, paid repair and DeepCopy/rebuild. Source ShieldState/Generator gained bounded restore and opt-out automatic-clock APIs; original absorption/regen algorithms are unchanged. The wrapper manually advances once after refreshing operational state, preventing a source Update from regenerating before a detach/transition check. HP death disables defense immediately. The first normal enemy of encounters with at least3 enemies carries a real shield; the separate shotgun escort remains. Enemy loot snapshots actual HP/charge/rotation before cleanup.

The source `Samples~/ShieldManualDemo.cs` is preserved unchanged as `ReferenceSources/DEF001/ShieldManualDemo.cs.txt` outside Assets because Unity ignores Samples~ and does not generate its meta; no demo is used by gameplay.

## Evidence

- RED:2 new cases failed because W5 had no physical shield catalog.
- Final EditMode filter Shield: **55/55 PASS**, native0, including3 physical/model integration cases.
- Final original PlayMode filter Shield: **21/21 PASS**, native0.
- Shared-device/shotgun regression filter RRLW6: **5/5 PASS**, native0 (before adding the third shield salvage case; overlapping cases are not counted again as unique).
- Actual mounted shield absorbs front40 before P12 HP (charge100 to60); rear40 reaches HP; destruction disables it in the same damage operation; subsequent front40 reaches HP. Paid60 repair/rebuild retains charge60, preventing charge farming.
- All64 XYZ orientations retain4 occupied cells and Bonus ordinal13 is preserved.
- Actual enemy core defeat produces selectable shield salvage with HP.75,charge35,age1.2,rotationY2 and stable ID through inventory/DeepCopy.
- Machine-readable case names/results: [test summaries](DEF-001_test_summary.json). Raw XML/logs remain in ignored Artifacts/Local directories listed there.

At the package checkpoint Windows/native and full regression were pending; the wave report now supplies those results. These API-driven physics tests are not reported as human-input play. Known W5 UIA shutdown limitation is unchanged; all listed Unity test processes exited0.

Integration [Draft PR #22](https://github.com/C8LyNn/RoboRougelike/pull/22), runtime SHA 52abbbb80b212b1bff296568cb00a01a74577924.


Current aggregate source `8cf4ff8c80178f04f8d60dc6f57d03a2e7f7f32f`: all W6 gates and known limits are recorded in [W6 wave QA](W6_WAVE_QA.md). Native per-device checks use APIs/real physics; UI-driven garage/campaign flow and virtual-input combat are identified separately. Earlier frozen feature SHAs do not include all shared closure fixes.
