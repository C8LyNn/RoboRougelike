# B01-22 — W16 CHL-003 economy boundary: QA ONLY

**Not a new gameplay package.** Governance: [#276](https://github.com/C8LyNn/RoboRougelike/issues/276), CHL ownership [#91](https://github.com/C8LyNn/RoboRougelike/issues/91), Codex scope [#275](https://github.com/C8LyNn/RoboRougelike/issues/275). Ownership B01-22 limited to this read-only tool and its own CI. No changes to Unity Assets, PARTSIZE, CHL producer, legacy ECON #252, guard #263, R3, R1 or Codex host.

## Real published C# source verified by this QA

The script downloads the *actual published Codex host code*, pin `88b8eed60345c49481648833e1fde6dc1a400258`, and validates original Git blobs before examining anything.

- `Assets/R1/R1RunController.cs`: Git blob `c6d5f3efe4949da68a5c48ec38ef5028af8b8410`
- `Assets/R3/R3VehicleModel.cs`: Git blob `cdd75fff4923a382b16c430dac1452320eda88f6`

Current pinned real `R1RunController.Update` awards one standard encounter payout in Combat state after the loss check and transitions into Salvage. Original win inputs include `Encounter.Cleared`, `CheckpointRace.Won` and `RoadChallenge.Completed`. It does **not** prove CHL-003 pays rewards or exists. The *run-state* gate is not itself a durable authenticated event-ID journal. `R3VehicleModel.RepairCost(0HP)` equals buying a new part on this **old pinned baseline**, already owned by Codex under #275; the QA does not patch it.

## Contract for the isolated CHL-003

CHL-003 standalone obstacle course must **NOT grant Scrap, items, salvage or trigger RunController victory**. Event `Completed` is a gameplay result, not authorization to modify the R3 wallet. Retry x3, duplicate collider contacts, relaunch and fall→restart must leave persistent R3 currency and inventory identical; future opt-in host adapter may grant only after a real, uniquely identified, approved victory with atomic journal and cannot be authored here. B01-20 remains sole CHL producer.

The scanner rejects CHL C# files with direct economy writes, raw `WinEncounter/ConfirmRewards` calls and R1/R3/R4 namespace dependencies in the host-neutral core. **It is static QA only**. Indirect callbacks, reflection and genuine physics must be reviewed independently. When CHL code isn't present, the report says `SOURCE_NOT_PRESENT` and **NOT_RUN**, NEVER PASS. Core demo and real Unity must be tested in licensed Unity6000.3.14f1 by B01-60 after code arrives.

## Reproduce exactly

```bash
python Tools/B01/QA-CHL-ECON-W16/prepare.py
python -m unittest discover -s Tools/B01/QA-CHL-ECON-W16 -p 'test_*.py' -v
python Tools/B01/QA-CHL-ECON-W16/audit.py --host Tools/B01/QA-CHL-ECON-W16/_pinned --chl Assets/RoboRougelikeChallenges/CHL003 --out artifacts/chl-econ/receipt.json
```

`test_boundary.py` test 01 inspects actual original R1/R3 C#; subsequent mutations test fail-closed source audit. CHL snippets in fixture tests 09–15 are explicitly **synthetic tests of the scanner**, NOT tests of CHL package nor Unity. Windows and Linux Actions execute both. CI artifact contains pinned blob receipts, static outcome, and own-scope manifest. No executable Windows CHL demo is bundled because B01-20 has not supplied it here.

**One receiver ask:** after B01-20 publishes CHL-003 R1 SHA, run this gate on actual source plus independent B01-60 native two-different-vehicle tests with R3 wallet + full physical inventory GUID snapshots before/after 3 retries, duplicate hits and Completed. Native source/Unity G1 remains NOT_RUN until that evidence exists. No G2, no merge, no CODEX_READY.

## Follow-up W16 / real producer CHL-003 #285 — receiver boundary QA

**Now testing actual immutable CHL source:** [Draft #285](https://github.com/C8LyNn/RoboRougelike/pull/285) at `bec51e4a66ada534b875ce13f08b83d63f2cd7b8`, B01-20 exclusive producer. `prepare.py` now fetches **11 original immutable CHL Git blobs** (4 runtime .cs, 1 demo .cs, 2 original Edit/Play .cs, 3 asmdefs, 1 serialized Unity demo scene) PLUS the original two frozen Codex host sources, all pinned byte-for-byte using Git blob hashes. No PR285 source copied into or edited in this QA PR; fetched files are temporary CI inputs, never tracked producer output. Audit pins `--chl` to exactly this tree and scans **7/7 original CHL C# files**, rejecting direct `Award`, `ReceiveSalvage`, `WinEncounter`, Scrap edits, module HP edits, inventory writes, PlayerPrefs/File/Json save writes, and forbidden R1/R3/R4 imports in CHL core. **This is static source QA, NOT Unity native.**

**Actual compiled source behavioral evidence:** `CrossSource/CrossSource.csproj` directly links the ORIGINAL frozen `CHL003CourseCore.cs` downloaded from #285, not a reimplementation. `Program.cs` executes named C# cases: genuine ordered completion; duplicate/foreign/early rejection; failure/retry; cancellation/pause; finish after 3 retries (a *fresh Completed event per completed attempt*); duplicate subscribers (two callbacks after ONE finish); an intentionally detached and immutable host state witness; failure→win; seeded 512 valid replay trials; and throwing receiver callback after core has marked state Completed. There is no production payment logic or fake wallet implementation — event counters only. Results are **original C# behavior in .NET8, not Unity Physics, not R3 host integration**.

### Concrete owner/receiver interface risks discovered from #285

1. **Completion is PER ATTEMPT, not per activity/reward identity.** `CHL003CourseCore.Retry` resets State/CheckpointIndex but retains subscribed delegates. `Completed` fires again on a fresh valid finish, including after 3 retries. This is expected CHL semantics, but **future Codex reward grants MUST use their own stable activity-victory ID + durable replay journal + explicit repeatability policy**, not `Completed` count or `ElapsedSeconds`.
2. **Repeated subscription is not internally deduplicated.** A Codex receiver that subscribes on every Retry or on re-entry without unsubscribing will receive multiple `Completed` notifications for a single finish. Receiver must subscribe exactly once per live course instance and unsubscribe on teardown or reconfigure; reward settlement must be idempotent even when notifications multiply.
3. **Callback throwing after win is ambiguous for unguarded host.** Core transitions to `Completed` before invoking handlers, and a throwing handler will leave core Completed. Host must checkpoint one-time transaction outcome and not re-grant after reload/error; a retry is not authority to compensate by issuing another payout.
4. **The demo itself creates its rigs.** `CHL003DemoBootstrap` instantiates an isolated parent and synthetic Light/Heavy rigidbodies. Swap deliberately calls `Destroy(vehicle.gameObject)`; it MUST NOT be repurposed with an authoritative R3 robot or inventory item. `CHL003CourseController.Configure` accepts any supplied nonkinematic Rigidbody: **the API does not prove that the actor is a disposable clone**, so a future integration must enforce independent physics instance ownership.
5. **Retry intentionally changes physics state.** `CHL003CourseController.Retry()` resets configured actor linear/angular velocity and world position/rotation. That is a temporary trial-state change, not serialized vehicle/HP change. But if Codex binds a real production actor, external damage/spawn/save hooks could observe or persist altered state. Restrict to isolated demo/cloned actor, or provide explicit host transaction/snapshot and restoration at return.
6. **No direct persistent economy mutation in shipped CHL producer source**: in the original seven C# files there is no R3 wallet, inventory, part-HP or save API write detected, and original runtime asmdef references no host assembly. This supports **static isolation of the shipped code only**, not a native claim or a guarantee against future receiver-side callbacks.

### Mandatory Codex native integration acceptance (NOT_RUN)

For both physically distinct Light and Heavy, in disposable Unity6000.3.14f1 scene, record **before and after** snapshots: `R3VehicleModel.Scrap`, purchased/salvaged inventory (sorted physical GUIDs), mounted GUID set, each part Health/upgrade/metadata, **serialized garage build bytes or canonical hash**, external reward ledger, and real actor Rigidbody pose. Execute: (A) 4 ordered CP→one finish; (B) duplicate collider trigger + one finish; (C) miss checkpoint/foreign actor; (D) fall → Retry three times → finish; (E) pause/resume; (F) cancel/scene exit and re-enter, including rig swap; (G) callback double registration and throws; (H) crash/reload after accepted result and replay of same activity ID. **Standalone CHL must have ZERO delta on wallet, physical GUIDs, HP, rewards, persisted builds**; temporary demo actor pose can reset. Native physics + gameplay values remain **NOT_RUN** without licensed Unity scene and actual saved R3 integration evidence.

**Host-side option ONLY AFTER G2 and Codex ownership:** default standalone CHL completion gives **no reward**. If game design explicitly authorizes a reward-bearing integration, Codex must authenticate active run + activity/attempt/actor ownership, separate a unique durable reward transaction key from repeatable trial completion, gate settlement on validated eligible finished state, apply exactly once atomically to authoritative R3 inventory+wallet+save, and reject duplicated/old signals. No new economy module, host adapter or ledger is authored in this QA-only change.
