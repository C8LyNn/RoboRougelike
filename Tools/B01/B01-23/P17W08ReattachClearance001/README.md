# B01-23 — PASS 17 / WAVE 08 — REATTACH-CLEARANCE-001 v1.0.0

## Real gameplay feature — optional collision-safe reattachment

This new **isolated Unity 6 physical docking gate** prevents a detached module from being mounted through an obstructing Collider. It is NOT a collision/physics claim about canonical Codex game. The W08 program is opt-in to the existing W07 physical playground, and **never modifies frozen original parent source**:
- W06 [Draft #421](https://github.com/C8LyNn/RoboRougelike/pull/421) exact `cb2b3a76e84266e2730fffc4ab048c1039f0652f`.
- W07 [Draft #439](https://github.com/C8LyNn/RoboRougelike/pull/439) exact `cdcaea750fda1a9d7db6018e34970f4b9f206427`.

**Concrete actual production C# code:** `DockClearancePolicy.cs` applies fail-closed immutable decision rules to real Collider penetration observations, required reach, physical body state and capacity saturation; `DebrisSafeReattachGate.cs` queries native `Physics.OverlapBoxNonAlloc` **at the planned dock pose** and uses actual `Physics.ComputePenetration`, supports Box/Sphere/Capsule/convex MeshCollider, ignores its own detached Rigidbody and trigger volumes, denies non-convex/unsupported shapes, excessive depth (>5mm), overflow and >3m reattachment distance. Crucially no parent/velocity/detachment state is mutated if rejected. Only on `Allowed`, it calls the original W07 `DebrisRigPlayground.Reattach(index)`, which is solely responsible for reattachment state mutation.

**Actual opt-in demo UI:** W08 `DebrisSafeReattachGate.OnGUI` provides separate **safe reattach** actions for Light and Heavy. The original W07 demo's old unguarded Reattach controls still exist and are **NOT automatically intercepted**—a canonical game integrator must remove or redirect those original controls after real QA; do not claim full UI enforcement.

**Editor helper:** `B01 / PASS17 / Add safe reattach gate to W07 demo` adds W08 gate only to an already existing W07 demo controller in an isolated scene, with Unity Undo. No main game scenes or Codex assets edited.

**Native tests (authored, NOT executed):** 6 actual Unity PlayMode tests for occupied dock rejecting without consuming original part identity, blocker removed → reattach, not detached refusal, remote part out-of-reach, triggers ignored, invalid rig index; plus real rig Colliders in W07. Real native Unity 6000.3.14f1 compile/PhysX/UI/Player **NOT_RUN** here.

## Execute exact source tests / physical ZIP

```bash
dotnet run --project Tools/B01/B01-23/P17W08ReattachClearance001/SourceTests.csproj -c Release
python -m unittest discover -s Tools/B01/B01-23/P17W08ReattachClearance001 -p test_package.py -v
python Tools/B01/B01-23/P17W08ReattachClearance001/package.py build --archive dist/B0123_W08_ReattachClearance_Delta.zip
```

- .NET8/C#9 compiles **the actual original W08 Unity-portable `DockClearancePolicy.cs`**, executes true positive/negative test matrix, no translation from other language.
- Real Git object packager locks exact W06/W07 Git blob SHAs and parent commit ancestry, 12 W08 Unity .meta GUIDs, 3 assembly references/roles, deterministic per-file SHA256+bytes manifest, native=NOT_RUN and CODEX_ACK=false. Same physical inner ZIP hash must match Windows/Ubuntu before source acceptance.
- Stage/rollback only in **new external disposable** directory; no overwrite, no source-repo install, refuse deletion of any foreign, changed or missing source file:
  `python Tools/B01/B01-23/P17W08ReattachClearance001/package.py stage --archive INNER_ZIP --destination ABSENT_EXTERNAL_DIRECTORY`
  `python Tools/B01/B01-23/P17W08ReattachClearance001/package.py rollback --archive INNER_ZIP --destination SAME_DIRECTORY`

## Genuine integration limits and receiver

W08 is a **delta on W07, which is itself stacked on W06**; do not claim source acceptance as Unity integration. B01-40: independently verify producer source/physical artifact SHA/12 GUID/asmdef/rollback before any receiver install. B01-60: independently verify actual native Unity scene, Physics query/penetration, blockers/trigger, PhysX contacts and all 6 Unity PlayMode tests. B01-61: optional GUID/collision source verification; Codex integration only after HQ B01-00 schedules, Codex explicitly ACKs and authorizes replacement of unguarded original dock UI. `SOURCE_PASS` means only executed portable source, **G1_NATIVE_NOT_RUN / G2_HOLD / CODEX_NOT_SENT/ACK_NO/NOT_INTEGRATED**. Do not merge main/develop, edit Codex files, or force-update previous packages.

**Source scope:** only new `Assets/B0123ReattachClearance001/**`, `Tools/B01/B01-23/P17W08ReattachClearance001/**`, own workflow. Old parents remain immutable. The optional clear/blocked decision uses real target part local dimensions but W07 test rig only; generic compound/child collider/rotated mount or dynamic owner target pose is **not supported** and must be validated/extended by Codex after handoff if desired.
