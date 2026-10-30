# B01-23 / PASS17 WAVE07 — DEBRIS-PHYSICS-PLAYGROUND-001 v1.0.0

## Real Unity functionality
This is **ONE new isolated two-rig Unity 6.0 physics playground**, stacked on the immutable producer [DEBRIS-REPULSE-001 #421](https://github.com/C8LyNn/RoboRougelike/pull/421), SOURCE_SHA `cb2b3a76e84266e2730fffc4ab048c1039f0652f`.
**It is not a game/garage/R3/R6 or PARTSIZE/CHL edit**. Original producer #421 is NOT modified or re-issued.

New actual Unity components:
- `Runtime/RigDemoDefinition.cs`: original C#9 immutable dimension/part ID/mass specification for two different rigs, LIGHT 1.5kg and HEAVY 12kg, positive spatial clearance, checked finite values. This demo does not claim canonical game balance.
- `Runtime/DebrisRigPlayground.cs`: creates real Unity `GameObject`, distinct `Rigidbody` and BoxCollider for each chassis and detachable part, fixed world-space separation from its owner. Calls the **unaltered original #421** `DebrisRepulsionEmitter` on user-initiated **Detach**, uses real `ForceMode.Impulse`, mounted/kinematic -> detached/dynamic, velocity based on actual mass/direction; explicit **Reattach** and **Reset** buttons, live m/s readout. No modification of original combat, HP, inventory, Save or physics driver.
- `Editor/DebrisPlaygroundSceneBuilder.cs`: **B01 / PASS17 / Create isolated debris physics playground** editor menu creates a real new scene under `Assets/B0123DebrisPlayground001/Generated/` (never overwrites; asks to save modified scenes). Generates floor, camera, light and demo controller. Then **Play** for two real physical chassis and part bodies. The Generated directory and scene are optional local outputs, not claims of prefab/scene serialized in this PR.
- `Tests/PlayMode/DebrisRigPlayModeTests.cs`: 7 original NUnit/UnityTest cases **AUTHORED**, including two real masses/Colliders, kinematic refusal, mass-normalized physical speeds, non-stacking, physical reattachment, reset and no duplicate detach. **NATIVE NOT_RUN** until licensed Unity 6000.3.14f1 environment executes them.
- 3 owned asmdefs with explicit correct dependency references and **12 original .meta GUIDs**, no auto-references to Codex host.

## Actual source test and reproducible physical delivery
```bash
dotnet run --project Tools/B01/B01-23/P17W07DebrisPlayground001/RuntimeSourceTests.csproj -c Release
python -m unittest discover -s Tools/B01/B01-23/P17W07DebrisPlayground001 -p test_package.py -v
python Tools/B01/B01-23/P17W07DebrisPlayground001/package.py build --archive dist/B0123_W07_DebrisPlayground_Delta.zip
```

- The C# source harness **actually compiles the original frozen core from #421** plus original new W07 `RigDemoDefinition.cs`, enforced C#9; no translated JS/math-only mock.
- Python tests independently inspect exact original Git HEAD for 12 .meta GUIDs, proper Editor/PlayMode asmdefs and frozen parent original Git blob SHAs. The original delta ZIP contains only new owner paths and has embedded per-file SHA256 & bytes, exact current SHA, and **source-only** claims. Strict physical ZIP verification, safe stage/rollback and negative cases. Two-OS GitHub Actions results to verify; written != executed.
- To stage external disposable target: `python Tools/B01/B01-23/P17W07DebrisPlayground001/package.py stage --archive INNER.zip --destination ABSENT_EXTERNAL_DIRECTORY`; rollback `... rollback --archive INNER.zip --destination SAME_DIRECTORY`. Refuses current repo, existing destination, overwritten/missing/foreign files. **Never attempt to stage W07 delta without pinned #421 exact source**.

## Isolation, native test, rollback and acceptance
- Base and prerequisite: immutable #421 producer source `cb2b3a76e84266e2730fffc4ab048c1039f0652f`, original core Git blob `fa1aa5e531927cbe73cf5fdfe161b4a981316db5`, adapter blob `2b3a8465c550bd49a5c98be5a8064a4ce0947f4b`.
- New W07 PR is stacked on #421 branch; **W07 diff only** `Assets/B0123DebrisPlayground001/**`, `Tools/B01/B01-23/P17W07DebrisPlayground001/**`, one own CI workflow. Import parent #421 first and only if owner authorizes it, then the W07 delta; no merging, editing canonical game or automatically integrating the optional hook.
- Real Unity native EditMode/PlayMode/PhysX, Player, Editor-scene construction and manual visual checks are **NOT_RUN** in this producer environment; two authored rigs are not proof they were rendered in Unity. Unity license absence is a blocker, not gameplay FAIL.
- **Rollback:** remove generated demo scene and the isolated W07 delta (using verified byte-match rollback for external staging), leaving parent #421 and any canonical source untouched. B01-40 reviews path/GUID/physical ZIP/safe import; B01-60 independently tests scene creation and all 7 real PlayMode cases, physical contacts, reattach safety and Light/Heavy movement. Then B01-00 selects whether to offer to Codex inbox; Codex ACK required. Status: SOURCE CANDIDATE / INDEPENDENT QA PENDING / G1 NOT_RUN / G2 HOLD.

**Receiver:** B01-40 + B01-60, B01-00 only after QA. No unauthorized merge.
