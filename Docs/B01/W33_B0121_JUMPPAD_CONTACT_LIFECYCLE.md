# B01-21 — PASS17 W33 | Original MAP-JUMPPAD-001 contact lifecycle repair

**Base playable game:** Codex #877 @ `3bd9b159997924dcecb4e1cd1d41137896e5199e` on #873.
**Scope:** versioned child of the original game receiver, ONE existing `MapJumpPad.cs` corrected; ONE existing owner PlayMode test expanded. No map generator, Save writer, economy, arena placement, garage, weapon, physics vehicle controller, protected branch, merge or release.

## SOURCE defect / bounded remediation

The original `contacts[Rigidbody]` hash-set only retires colliders on `OnTriggerExit`; a destructible robot part that gets disabled, destroyed or reparented without a matching callback remains counted. A replacement collider from that SAME real Rigidbody can then trigger `OnTriggerEnter`, but `colliders.Count > 0` suppresses the next legitimate launch even after the one-body cooldown. No new object authority exists.

W33 checks actual recorded collider liveness, `enabled`, active hierarchy and `attachedRigidbody` on the original `OnTriggerEnter` **before** deciding whether an entering collider is first. It uses one reusable scratch `List<Collider>` (no per-contact LINQ/delegate allocation), retains live multi-collider deduplication, `LayerMask`, cooldown, `ForceMode.VelocityChange` and active/kinematic checks unchanged. The original object/arena graph and prefab remain untouched.

## Repeatable layered evidence

1. Original real C# source callback compiled via .NET8 with explicit minimal Unity **shims**, both Windows2022 and Ubuntu24: compile original BASE Git blob and run original `OnTriggerEnter` with first contact, missing Exit + disabled/then-destroyed contacts, replacement contact and live duplicate contact. Historical expected RED marker and exit 23, revised callback expected GREEN marker and exit 0. This is direct original method logic with a **synthetic engine**, not native Unity or PhysX.
2. Existing `MapJumpPadPhysicsTests` now contains `MissingExitForDisabledPartColliderDoesNotBlockNextRealBodyContact` genuine Unity `[UnityTest]`: fault-inject missing Exit by calling the original MonoBehaviour callback through `SendMessage`, then verify real Rigidbody velocity after `WaitForFixedUpdate`. It is an authored Unity test **NOT_RUN**; explicit callback injection is not a live full-vehicle collision.
3. Independent previously authored B01-60 #901/#916 real compound R3/JumpPad `[UnityTest]` and B01-25 #911 arena routes remain authoritative for actual product behavior. **Do not duplicate or mark PASS.**

Commands:
```sh
dotnet run --project Tools/B01/B01-21/W33/B0121JumpPadGuard.csproj
# Native: Unity 6000.3.14f1 Edit/Play test filter MapJumpPadPhysicsTests
# Then B01-60 #901/#916, B01-25 #911, full Edit/Play and Windows extracted Player
```

## Gates / rollback

- Self/source C# cross-OS: see **final exact-head Actions logs**, not a presumed PASS.
- Independent composite G0 on selected whole-game SHA: PENDING B01-40/41/61.
- True Unity 6000.3.14f1 Editor Edit/Play: NOT_RUN.
- Real R3 battle + full vehicle jump / landing: NOT_RUN.
- Windows x64 build, extracted Player and Player.log: NOT_RUN.
- Integrator ACK/G2/merge/release: NO.

Rollback: discard this isolated child PR or omit its two changed original B01-21 package files and this owner-only test/CI/doc set. Old Codex #877 and B01-21 producer #810 are preserved unchanged. Intake ONLY after independent current exact-SHA G0 and real native test receipt.


## PASS17 WAVE34 — same original PR #921 in-place versioned continuation

**Verified original-source gap:** W33 pruned dead **colliders within a living Rigidbody**, but the existing `contacts` and `lastLaunch` dictionaries still retained destroyed **Rigidbody keys**, even after `OnTriggerExit` erased the live contacts, until the pad was itself disabled. Long-lived active pads could accumulate obsolete per-body records as enemy/rigidbody objects are destroyed.

**New original production delta:** `MapJumpPad.OnTriggerEnter` invokes `RetireDestroyedBodies()` before admitting the next actual body, enumerates both existing original dictionaries into one reused `List<Rigidbody>`, and removes only Unity-destroyed (fake-null) keys after enumeration. No allocation-heavy LINQ; no collection mutation during enumeration; no new physics authority or alternate pad. Live contacts, per-body cooldown, layer mask, 8.5m/s up, 2m/s forward, existing arena placement and `OnDisable` behavior untouched. This is passive housekeeping upon another valid body entering, not a periodic simulation task.

**Three pinned directly executed original C# source versions** in the SAME two-OS workflow:
1. Canonical #877 `3bd9b159...`: expected exit 23, dead **collider** does not release;
2. Previous B01-21 W33 #921 `b3a053d...`: expected exit 24, dead **Rigidbody** stays in both dictionaries;
3. New W34 #921 current exact SHA: expected exit 0, original source C# method retires dead body from both caches and still launches new body while preserving live-collider duplicate suppression.

The original `MapJumpPadPhysicsTests.cs` now also includes `DestroyedRigidbodyNoLongerOccupiesBothCachesWhenAnotherBodyEnters`: an authored Unity `[UnityTest]` that destroys a real engine Rigidbody then checks original cache counts after a new body enters. **NATIVE NOT_RUN**. Direct original C# + shim does not prove Unity fake-null timing or PhysX; both must be checked in a licensed 6000.3.14f1 Editor and extracted Win64 Player. No new Unity GUIDs/files or real R3 gameplay-source changes.

**Integrability caveat:** historical W29 Codex provenance workflow `Tools/B01/Codex/W29/verify_source.py` hard-pins original producer #810 `MapJumpPad.cs` byte-for-byte. On W33 #921 it failed as expected on Windows+Ubuntu [Actions #41713889293](https://github.com/C8LyNn/RoboRougelike/actions/runs/41713889293) with `B0100_W29_SOURCE_FAIL_CLOSED: source provenance mismatch`. New owner-modified JumpPad is a **versioned amended receiver**, not a byte-identical #810 import. **Do not bypass this check, edit Codex W29 workflow or claim G0 PASS.** B01-40/61 and Codex must independently revalidate this exact-versioned source and update their *own authorized receiver provenance policy* if intake is approved. Until then W29 SOURCE G0 = RED, W34 own source CI can be GREEN, and no game integration is authorized.

**Acceptance status:** `W34_SOURCE_RED_TO_GREEN=CHECK_ACTUAL_CURRENT_HEAD_CI`; `INDEPENDENT_G0=HOLD`; `UNITY_EDIT_PLAY=NOT_RUN`; `FULL_R3_VEHICLE_JUMP=NOT_RUN`; `WINDOWS_EXTRACTED_PLAYER=NOT_RUN`; `NO_MERGE/NO_RELEASE`. No Save (#923), V7 (#897/#910), #834, R5 geometry (#911), enemy gameplay (#901/#916) scope transfer.
