# B01-31 W25 — Real P12 Damageable -> W24 3-state Armor Visual Runtime Adapter

**Package:** `B01-31-P17W25-REAL-P12-ARMOR-VISUAL-BINDING-001/P17-W25-R1`. [HQ W25 #731](https://github.com/C8LyNn/RoboRougelike/issues/731). Ownership B01-31 Concept Art & Visual Exploration. This is an **actual new game visual behavior**, not another mesh or concept texture.

### What is actually implemented

New original Unity MonoBehaviour `B0131W25P12DamageVisualBinding.cs` under isolated owner directory `Assets/B01_31/W25P12DamageVisualBinding`, **stacked on exact [W24 #770](https://github.com/C8LyNn/RoboRougelike/pull/770) SHA `668c7eed8f96cdb0c65062c4ec3a0d80bf26ee93`**, no W24 source copied. Reads REAL pre-existing [P12 gameplay Damageable](https://github.com/C8LyNn/RoboRougelike/blob/main/Assets/Pack/P12_PipelineSafeDamageable.cs) original exact Git blob `544bed274b616b91fd98c45d2b194b2f3a2d112a`, through public `CurrentHp`, `MaxHp`, `IsDead`, `Died` and maps to existing exact original W24 visual `B0131W24ArmorDamageVisual.SetVisualPhase` (Git blob `8cf3664b9b6e5b0fd6623be7c3f4e72fd9164664`). **Actual visual geometry**: health above 50% => OK; health in (0,50%] => DAMAGED; death or nonpositive HP => DESTROYED, with real `Died` event changing visual during original P12 `Die` before detach. `RestoreHealthRatio` visibly returns to OK at next frame. User-adjustable damage threshold restricted to 5-95%, explicit `Bind`, safe `OnEnable`/`OnDisable` subscriber ownership, visual state fail-closed for NaN/Infinity/contradictory HP, old P12 event unsubscribe on rebind, death latch prevents a false return to healthy until explicitly re-bound to a different real source.

**No gameplay authority:** no `ApplyDamage`/HP mutation, Rigidbody, CanDeploy, component ID, inventory, Scrap, win/rebuild, loaner, Save/Load APIs, lock, GUID graph or canonical session changes. The real P12 source can physically despawn on death; this binder only updates rendered state immediately while the visual's GameObject still exists. Attach this adapter to an isolated W24 visual prefab, then **explicitly bind the actual P12 source and the W24 visual** from approved integration code/Inspector; it never searches an unrelated unit. This does not implement P12 as a new canonical game HP provider, nor modify existing damage processing.

### Physical incremental Unity import artifact

Original deterministic `B0131W25-P12-DamageToArmorVisual-DELTA.unitypackage` contains **ONLY three new own GUID assets** (new folder, new C# binder, README) as real 8-member Unity TAR/GZIP with exact checked-in `.meta` byte parity, no duplicate previous W24 13 GUID package, no duplicate P12 source. Intended as *reviewed incremental import AFTER W24 package* into approved **disposable Unity project**, not automatic install on user's main PC, game, main/develop, Codex. Real original binary SHA receipt included as `receipt.json`.

### Reproducible real managed source QA (native Unity not claimed)

```bash
dotnet run -c Release --project Tools/B01-31/P17/W25P12DamageVisualBinding001/Harness/P12LiveVisualHarness.csproj
python Tools/B01-31/P17/W25P12DamageVisualBinding001/build_delta.py --repo . --output build/W25
python Tools/B01-31/P17/W25P12DamageVisualBinding001/verify_delta.py --repo . --output build/W25
```

The actual .NET8 tests compile THREE **REAL checked-in C# source files verbatim**: original gameplay `P12_PipelineSafeDamageable.cs`, original W24 visual `B0131W24ArmorDamageVisual.cs`, W25 new binder. **ONLY UnityEngine and P12's external R3/R6 dependencies are SYNTHETIC test stubs** (no fake Damageable class). Exercise **29 original source checks** including real original P12 `ApplyDamage->Die->Died` event, real source health and repair, subscribe/unsubscribe/rebind, visual threshold and invalid state. This is stronger than a model-only mock, but **not native Unity/PhysX Editor or PlayMode**. Separate independent Python physical binary receiver does NOT import package producer; verifies exact original P12/W24 git blob SHAs and 3 live W25 Unity GUID/meta/path source bytes, 8 malformed tar/foreign changes correctly RED. Two full ZIP-equivalent Unitypackage+receipt exports must byte-match, Windows downloads actual Ubuntu bytes and repeats real-source C# test and independent read-only verification.

### Native Unity receiver, scope and rollback

**Formal G0 B01-30/40:** review W25-only stacked diff, exact parent SHA, 3 .meta GUIDs + actual tar source and collision/rollback in safe disposable scratch, no import into a dirty project. **Native G1 B01-60:** with owner opt-in and valid Unity 6000.3.14f1 license, import W24 then incremental W25 into disposable project, explicitly add binder to instantiated W24 visual/actual P12 component, confirm visual changes after real `ApplyDamage`, `RestoreHealthRatio`, `Died`, actual on-disable and rebind with Editor.log, Unity NUnit XML and screenshots where available. Without actual execution: **G1 NOT_RUN**.

**P0 boundaries:** Save candidate #749 versus #773 bug fix is owned by B01-00/23/62 and must not be promoted by B01-31. #753/#741 ALT_HOLD. Real Unity Rig physical port #772 B01-23. ColdRestart #750 B01-23/61. Owner-approved victory rebuild, 65% Scrap, loaner and CanDeploy are handled by B01-10/22/20/23/62, **not this visual binder**. Frozen #510/#556/#690/#729 and storage #712/#669 untouched. Codex quota unavailable.

**Rollback:** Remove ONLY 13 brand-new W25 source paths (not earlier W24 code) or W25 new namespace after human verified no foreign/even empty directory changes in a separately approved scratch, NEVER P12 Damageable, W24 original Unitypackage, previous W16-23 art or Codex. No automatic source merge or user PC installation. Six-state ledger in manifest, formal independent G0 and native G1 both pending, G2 HOLD.
