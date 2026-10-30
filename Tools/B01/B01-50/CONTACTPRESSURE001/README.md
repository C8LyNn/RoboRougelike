# B01-50 / CONTACTPRESSURE-001/R1 — PASS17 WAVE21

## New real opt-in functionality
Production Core/ContactPressureMeter.cs stores a fixed 4..512 tick ring of collision callbacks Enter/Stay/Exit and contact-point counts. Production Unity/B0150OptionalContactPressureProbe.cs is an opt-in MonoBehaviour recording original OnCollisionEnter/Stay/Exit(Collision) and opening one observation bucket in each FixedUpdate. TryCapture(out ContactPressureSnapshot) returns immutable counts, peak and nearest-rank p95 callbacks per observed physics tick. Purely observational: NEVER changes collision response, Rigidbody, forces, component lifetime, Save/Load, gameplay or other agents' physics authority.

Unlike BROADPHASE-001 (synthetic first-hit physics ordering), PERFVIEW (FPS) and SCENECOST (static scene audit), this package instruments callbacks in actual Unity code when installed in an independently authorized scratch runtime. However NO native Unity has run yet; all measured callback behavior is source-only under explicitly disclosed surrogate UnityEngine API until B01-60 activates Unity 6000.3.14f1. No performance improvement or native GC-free proof is claimed.

## Semantics and limits
- Fixed capacity 4..512 ticks (default 120), two preallocated arrays including a sort scratch. No allocations added by TryRecord; Source-level code review only, not a Unity Profiler allocation measurement.
- BeginTick(long tick) accepts strictly increasing nonnegative tick IDs; rejects duplicates/backwards without mutation; gaps become zero-callback ticks; gaps >= capacity reset prior observations without unbounded loops.
- TryRecord(ContactKind kind, int points) accepts Enter/Stay/Exit and 0..4096 contact points per callback, rejecting other values with no mutation. Counter overflow is rejected atomically. Zero-point Exit still counts as a callback.
- Snapshot produces separate Enter/Stay/Exit totals, points, total callbacks, last tick, peak and nearest-rank p95 per observed fixed tick, including zero-gap ticks. Value copies are independent; Snapshot reuses scratch sorting buffer. Main-thread only; NOT thread safe.
- Unity FixedUpdate starts bucket and collision callbacks record into latest observed bucket. Exact native Unity event ordering, callback coalescing, contactCount semantics and exposure to solver substeps require actual G1 testing; callbacks are NOT unique contact pairs or solver contacts.
- Invalid serialized window disables optional component. OnEnable resets observation history. No OnGUI allocations, logging per callback, persistent save file or automatic upload. MonoBehaviour attached only to a user-selected Collider/Rigidbody; no global scene scanner.

## Runnable original source tests
tests/CONTACTPRESSURE.Tests.csproj compiles both ORIGINAL C# production files using .NET8 and a small named TEST-ONLY UnityEngine API surrogate. Test fixture invokes the REAL adapter Awake/OnEnable/FixedUpdate/OnCollision methods via reflection; it never reflects into #510, Unity internals, game scene or frozen Save APIs. 32 executable positive/negative cases include reject/gap/eviction/p95/overflow validation, real adapter event capture, reset, 2 independent owners and invalid Unity config. Dedicated Windows2025/Ubuntu24.04 Actions verifies exact 9 source/docs/test/workflow Git blob SHA pins, 2 new MonoImporter GUIDs vs original Assets metas, then builds production code and runs 32 source tests plus original JSON receipts. Surrogate PASS NEVER means Unity native PASS.

## Native G1 — receiver controlled, not performed
After formal B01-40 source/GUID/safe transport G0, B01-60 may manually test with owner consent in NEW disposable Unity 6000.3.14f1 URP scratch: copy EXACT two .cs+.meta pairs into new private Assets subfolder; add Collider floor and falling Rigidbody+Collider sphere and attach probe to the sphere, query TryCapture after physics steps in independent receiver script. Observe real collisions, Editor.log/Unity Console, NUnit/PlayMode result, one- and multi-part scenarios, Physics Profiler callbacks and GC allocations. Do not copy into the real game or alter #510. No automatic user PC/SSH/credentials used.

Rollback removes ONLY verified own two script+.meta pairs from authorized scratch after backing up any references. Never recursively delete shared or foreign empty directories, symlinks, scenes, saves or prefab files.

## Governance
Producer B01-50; package B01-50-CONTACTPRESSURE-001/R1; only newly added Tools/B01/B01-50/CONTACTPRESSURE001/** and dedicated .github/workflows/b01-50-w21-contactpressure.yml. No edits to prior LIGHTBUDGET #654, CILATENCY #683, Assets/**, frozen #510/#556/#596/#647, Save/Load API #666 or competing #652. Receivers B01-40 G0, B01-60 independent native Unity/PhysX, B01-00 coordination. Source G0_PENDING, native G1_NOT_RUN, FPS_GPU_NOT_MEASURED, G2_HOLD, CODEX_ACK_NONE, NO_MERGE.
