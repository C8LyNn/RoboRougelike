# B01-25 | PASS17 WAVE20 | WEIGHTLATCH001 R1

New independent Unity6 level obstacle: a heavy-only physical pressure plate opens a real vertical kinematic sliding barrier; Light vehicle must use a **permanent safe bypass**, so no softlock. Unlike COUNTERBRIDGE001 pivot, MAGNETLANE magnetic field, SINKPLATE drop, SWEEPORB moving hazard or B01-20 CHL-003 checkpoint/race, this is a **mass-threshold timed latch with retained gate access**. The original self-contained game logic does not modify canonical Unity R6, Save/Load, #510, #556, #596, Codex, #647 or #652.

## Physical mechanism and geometry

Genuine Unity6 gravity dynamic Rigidbody/CapsuleCollider for Light 1.5kg and Heavy 12kg, both physically distinct in hull scale. A genuine stationary pressure plate BoxCollider at (0,0.025,-3) with dimensions 3x0.05x2 is checked by **the plate collider's own real Collider.Raycast** from the physical body's height. The original C#9 controller accepts heavy mass >=8kg ONLY when real contact flag and within plate coordinates; opens with 1.2 second mechanical travel, stays open for 3 seconds after leaving, then slides shut over 1.2 seconds. The gate itself is a genuine kinematic Rigidbody with BoxCollider, moved with Rigidbody.MovePosition by 2.9 meters, substantial clearance for the player. World has physical south approach, center floor, north exit, and totally independent bypass floor X=4, 18m long (Light can always traverse outside the locked 5.8m wide gate). Colors encode locked(red), opening(yellow), open(green), closing(magenta). IMGUI drive, vehicle swap, pause, local restart/fall reset provided.

The controller is C#9/netstandard2.1 and deterministic, no prefab/save dependencies. Invalid/nonfinite/teleported physical input and dt above 50ms throw ArgumentException BEFORE game state mutation, including paused states. Source-level 0..1 open fraction, monotonic activation count and temporary reopening prevent local latch softlock. Two masses, real contact, timer, no-contact, negative conditions, cold restart and deterministic replay have original source regression tests. Source C#9 tests do NOT simulate PhysX; the genuine Unity6 moving gate physics and capsule collision need activated native engine to validate.

## Reproduce in disposable Unity only

Only NEW paths under Assets/B0125WeightLatch001 plus root .meta (19 source/meta Unity files, 12 new stable unique GUIDs, 3 isolated asmdefs), Research/B01-25/P17/WEIGHTLATCH001, Tools/B01/B01-25/P17W20WeightLatch and the unique W20 Actions workflow. The Unity Editor command B01/PASS17 W20/Create WEIGHTLATCH001 Standalone Demo creates an ADDITIVE new scene ONLY under its own namespace, refuses scene overwrite. Actual generated .unity scene is NOT committed or claimed as already imported. Six NUnit Unity PlayMode tests authored (heavy real plate opens, light doesn't, bypass doesn't, reset/pause, actual kinematic gate/physics, input atomicity), all **NATIVE UNITY/PHYSX NOT_RUN** pending B01-60 Windows runner/activated Unity 6000.3.14f1 and real NUnit XML/Editor.log/process code.

Original source tests on Windows or Ubuntu checkout with .NET8, Python3 and Git:

    P=Research/B01-25/P17/WEIGHTLATCH001
    dotnet build "$P/WeightLatchRuntime.csproj" -c Release -p:LangVersion=9.0 -warnaserror
    dotnet run --project "$P/WeightLatchTests.csproj" -c Release -- "$P/.output/results.json"
    python Tools/B01/B01-25/P17W20WeightLatch/check.py --out Tools/B01/B01-25/P17W20WeightLatch/.output/receipt.json

Dedicated GitHub Actions calls real C#9 and real 16-case original Git SHA / Unity .meta GUID / asmdef / original 20-member source ZIP / independent scratch install+rollback QA on Windows+Ubuntu, followed by third cross-OS downloaded original ZIP sha256sum and byte cmp. Source ZIP is only code+meta and not a Unity Player. Never credit results before exact HEAD Actions checks finish.

## Delivery and gates

Receiver B01-40 verify 27 original file Git blobs against manifest and 12 GUID, 3 asmdefs, reversible authorized disposable Unity staging preserving foreign EMPTY folders, symlinks and occupied folders; formal G0 source-only PENDING. B01-60 activated Windows Unity physics and PlayMode real test evidence or NOT_RUN. B01-00 determines any later Codex handoff, no autonomous CODEX_ACK or merge. Six lifecycle states separate: IMPLEMENTED / TESTED / QA_ACCEPTED / READY_FOR_INTEGRATOR / INTEGRATED / RELEASED. Current G1 NOT_RUN, G2 HOLD.

**Limitations:** heavy center-contact raycast is physically sourced but not a full complex vehicle suspension/contact-manifold validator; dynamic door can push a vehicle or leave clearance/friction problems, which must be native-tested. The local engine does not persist gate state, Save/Load, run checkpoint, free rebuilds or economy. No claim it integrates into canonical game. Avoid presenting these source-only tests as native Unity.

Rollback only remove NEW B01-25-owned files in disposable scratch, never delete foreign empty dirs/links, game Assets or prior freezes. External Save R4 #647/#652 exclusively B01-23/62; #510/#556/#596 remain frozen; #604/#644 ALT_HOLD.
