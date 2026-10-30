# B01-23 — PASS17 / WAVE10 — SECURE-REATTACH-V2 (1.0.0)

## Purpose: close the original W07 public API bypass, not just hide legacy GUI

Independent [B01-61 #472](https://github.com/C8LyNn/RoboRougelike/pull/472) and [B01-62 #476](https://github.com/C8LyNn/RoboRougelike/pull/476) prove frozen W07 [#439](https://github.com/C8LyNn/RoboRougelike/pull/439) exposed an unguarded PUBLIC `DebrisRigPlayground.Reattach(int)`, and the W08 [#455](https://github.com/C8LyNn/RoboRougelike/pull/455) gate was opt-in. W09 [#471](https://github.com/C8LyNn/RoboRougelike/pull/471) disables legacy OnGUI but the PUBLIC W07 method remains callable from any code (explicitly confirmed by #471's own independent reviewer). **This is a genuinely new versioned replacement, not another facade over W07.**

**Real new Unity6 demo source:** `SecureDebrisPlaygroundV2` builds two genuine differently massed Rigidbody / BoxCollider rigs, applies the **unchanged original #421** real `DebrisRepulsionEmitter.TryApplyDetachImpulse` on physical detach, and holds all reattach state mutation inside **private `CommitDock`**. ALL public entry points **`Reattach`, `TrySafeReattach`, `ReattachFromGui`** call original `GuardedDockCommand`, which ALWAYS calls the same single physics inspector before commit. The only OnGUI reattach button uses `ReattachFromGui`, never raw state mutator. Re-entrant calls and malformed indices fail-closed.

**Actual PhysX verification:** `InspectDock` supplies planned part target collider centre / extents; always queries `Physics.OverlapBoxNonAlloc(..., SecureDockPolicy.AllLayers=-1, QueryTriggerInteraction.Ignore)`, calls `Physics.ComputePenetration` with actual scene shapes and planned pose, fails closed on collision >5mm, unsupported nonconvex shapes, truncated buffers, distance over 3m, abnormal transforms and invalid physical input. Only after clearance can it re-parent the Rigidbody, stop velocity and rearm its original stable part ID. Original W06 emitter remains unchanged.

**Standalone, physically different source/scene**: W10 depends directly on original frozen W06 [#421 @ `cb2b3a76e84266e2730fffc4ab048c1039f0652f`](https://github.com/C8LyNn/RoboRougelike/pull/421) **only**, NOT W07/W08/W09. An isolated new Editor menu **B01 / PASS17 / W10 Create guarded reattach V2 replacement scene** creates a genuinely EMPTY new scene under its exclusive folder (floor, camera, light, `SecureDebrisPlaygroundV2` only). It refuses to overwrite existing scenes and does NOT install/copy any W07 `DebrisRigPlayground` or W08/W09 guards; **do not keep original W07 controller in the same receiver scene**. No canonical Codex/CHL/PARTSIZE game sources touched.

**Native PlayMode authored, NOT_RUN:** 7 real Unity NUnit cases exercise source-level bypass as genuine physics path: calling public `Reattach(0)` directly with physical blocker on layer 8 MUST refuse and retain original ID; all three public aliases blocked; moved obstacle allows legitimate reattach; invalid indices/not detached; remote part fails; trigger allowed; high layer 31 obstacle denied. These tests have not been run on a licensed Unity 6000.3.14f1 host.

**Actual C#9/.NET8 executable tests:** `DockV2SourceTests.csproj` compiles original Unity-portable `SecureDockPolicy.cs` AND original `SecureRigSpec.cs` directly from Assets, calls every PUBLIC docking alias of `GuardedDockCommand`, verifies no mutation on blocked and exactly one mutation on clear, tests reentrancy, invalid indices, layer bypass, buffer saturation, NaN/Inf and 128 triple-route parameter combinations. CI on Windows/Ubuntu, not a substitute for native Unity runtime.

## Physically reproducible source archive + rollback

```sh
dotnet run --project Tools/B01/B01-23/P17W10SecureReattachV2/DockV2SourceTests.csproj -c Release
python -m unittest discover -s Tools/B01/B01-23/P17W10SecureReattachV2 -p test_package.py -v
python Tools/B01/B01-23/P17W10SecureReattachV2/package.py build --archive dist/B0123_SecureReattachV2_Only.zip
python Tools/B01/B01-23/P17W10SecureReattachV2/package.py stage --archive INNER.zip --destination NEW_EXTERNAL_FOLDER
python Tools/B01/B01-23/P17W10SecureReattachV2/package.py rollback --archive INNER.zip --destination THAT_EXTERNAL_FOLDER
```

Only real W10 delta files + embedded SHA256/byte-size source manifest. Original frozen W06 Git source blob pins checked; all 13 W10 Unity .meta GUIDs must be unique, 3 own asmdefs and their dependencies pin. Receiver safe stage only NEW external scratch, refuses overwrite, rollback refuses dirty/extra/missing/foreign files and symlinks. Never install into working Codex repo or merge without authorization.

**SAFETY BOUNDARY:** All supported V2 **public reattach methods** are guarded, including exact old method name; external scripts that explicitly create/use the deprecated W07 component **are not migrated by this new package**. A production Codex receiver must replace legacy components in actual game scenes, route other gameplay events through V2 or an equivalent canonical validated socket API, and forbid old W07 API use before claiming entire game safe. No attack-proof reflection/serialized save/compound collider guarantee. This is an isolated physical demo replacement with authored but NOT_RUN native tests. B01-40 formal physical source G0, B01-60/B01-61 independent source/native Unity G1, B01-62 saved ID continuity separately, B01-00 for Codex inbox after acceptance. `SOURCE_G0_PENDING/UNITY_G1_NOT_RUN/G2_HOLD/CODEX_NOT_SENT/ACK_NO/NOT_INTEGRATED`.
