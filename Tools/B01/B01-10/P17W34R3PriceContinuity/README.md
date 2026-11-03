# B01-10 / PASS 17 WAVE 34 — actual #923 one-Store R3 price provenance (source negative witness)

**Target:** selected B01-62 [#923@0d4904b735999f772e048be4bddc6bcc7bb9a9f3](https://github.com/C8LyNn/RoboRougelike/pull/923), one physical outer Save under the original #896 V3 lineage. This is strictly **B01-10 acceptance QA**, NOT a second Save writer, gameplay replacement, new ECON reducer, policy exception or a Unity Player.

**Observation grounded in actual original code:** #923's real C#9 Store `WholeGameEconomySlotV3.Semantic` validates dynamic-world and V7 economic **identical GUID+HP sets, generations, owner/run** but has no authoritative way to bind `OwnedModuleV7.PurchaseScrap` or `Category` to the actual current R3 part kind, weapon and individual acquisition/equivalent NEW price. The current B01-62 original source OS factory fixture explicitly assigns **`PurchaseScrap=100` for EVERY owned part**, while original selected game `Assets/R3/R3VehicleModel.cs` defaults are **Armor=5, Wheel=15, Basic Gun=30** (R3 base purchase price; actual custom weapons/prices can differ). This is consistent with the declared **NO real game binder yet** status; do not incorrectly present it as a dishonest Save writer or an exploit in a shipped Player.

**Balance impact if integrator copies this fixture into a live game:** Armor P5 must rebuild for `floor(5*65/100)=3` Scrap, but a syntactically valid persisted `PurchaseScrap=100` charges 65; persisted P1 would book **PAID0 instead of PAID3** despite identical world GUID+HP. Both can survive cold Save and may affect one-run progression/CanDeploy. The fix **does not belong inside raw #923 file writer by guessing static Armor cost**, because dynamic actual acquisition/weapon/legendary prices are legitimate. It belongs at the same **trusted original R3+V7 game-facing staging/capture boundary** with explicit persisted or validated purchase authority, while keeping one original wallet and atomic `.owner` envelope.

## New tests (source-only)

`Program.cs` links **unchanged original** #923 real R3 dynamic codec, one original OS writer, cold reader, original #877 real factory with explicit test-only Unity shim, and frozen #780 V7 policy. The three tests execute original Save `Commit` + close + genuine cold disk read:

1. **Positive control:** default Light20 Armor P5 → correct PAID quote3 remains after cold read.
2. **EXPECTED SOURCE-PROVENANCE GAP:** same original Light20 with wrong Armor P100 → store accepts same valid GUID+HP and cold replays quote65 (not the factory's 3).
3. **EXPECTED SOURCE-PROVENANCE GAP:** default Light20 Armor0HP recorded as P1: genuine original V7 `TryReserveDestroyed` produces `PAID0, Free=false`, and original Store commits it as one pending 0HP ticket across cold g1→g2 with GUID preserved; original authoritative default would be PAID3. No fake victory or immediate HP1 is introduced.

**Important:** a current Store can legitimately accept custom purchase prices. A source **negative witness** demonstrates a missing *game-facing price attestation/roundtrip test*, not a requirement that raw Save reject all non-default prices. No API or runtime change proposed on #923; do not patch producer code to hardcode 5/15/30. The source-only C# tests are not Unity EditMode/PlayMode/real PhysX or Windows Player.

## Receiver criteria

- **Codex / B01-20/22/23/62:** bind original R3 `Price(part.Kind,part.Weapon)` (including dynamic/custom purchase/equivalent NEW and legendary modifiers as approved) into `OwnedModuleV7.PurchaseScrap` under single canonical owner. If actual acquisition price is intentionally preserved rather than live catalog price, explicitly specify the policy and its trusted input provenance. Reject mismatched original physical part identity, category, HP, GUID, pending ticket and price; never treat `PAID 0` as `FREE`. Do not reconstruct values from #923's `100` shim.
- **B01-40/41/61:** independent composite exact-SHA G0 after actual typed R3/ECON/world binder; test 20/83/139 factory default and custom purchase/upgrade cases, same owned GUID across destructive battle, inventory/detached, positive paid0, negative default Armor P100 and P1, lost ACK/cold restart.
- **B01-60:** real licensed Unity6000.3.14f1 Edit/Play & Windows extracted Player, one authentic Win1→Win2 deferred 0HP rebuild, inspect price, money, unique IDs, pending and physical clearance after cold restart; #834 one-shot loaner remains OWNER PENDING.

**Stages at submission:** `ORIGINAL_SAVE_PRODUCER_MODIFIED=NO | NEW_GENUINE_SOURCE_TEST=YES | EXACT_HEAD_SOURCE_CI=PENDING | NATIVE_G1=NOT_RUN | AUTHENTIC_WINS=NOT_RUN | WINDOWS_PLAYER=NOT_BUILT | GAME_SAVE_BINDER=NOT_IMPLEMENTED | GAME_ACCEPTED=NO | MERGED=NO | RELEASED=NO`. Rollback: close QA-only child Draft; original #923/#877/#780 and user Save remain untouched.
