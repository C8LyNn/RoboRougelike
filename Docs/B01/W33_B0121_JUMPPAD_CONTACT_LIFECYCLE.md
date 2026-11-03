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
