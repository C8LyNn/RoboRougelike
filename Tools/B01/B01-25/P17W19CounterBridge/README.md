# B01-25 | P17 W19 | COUNTERBRIDGE001 R1

An original self-contained obstacle: **3x6m physical counterweighted tilt bridge**. Unlike the previous magnetic force field, sweeping orb and sinking platform, the bridge dynamically tilts due to vehicle weight and real contact leverage, then spring-recovers. It has mechanical +/-18 degree stops, original C#9 spring/inertia math and genuinely separate permanent bypass X=4. This is NOT the CHL-003 checkpoint/race mechanic and never edits the original game's R6, Garage, weapon, camera or Save/Load sources.

## Actual implemented functionality

The standalone Unity6 MonoBehaviour physically creates entrance/exit BoxColliders, a dynamic platform with real BoxCollider and kinematic Rigidbody that rotates via MoveRotation, and a gravity-driven dynamic CapsuleCollider/Rigidbody. Actual Collider.Raycast only against the real bridge checks physical support of the vehicle (not merely proximity or untrusted trigger flags). Controlled physical vehicle forces are applied by Rigidbody.AddForce ForceMode.Force. Original Light 1.5kg and Heavy 12kg bodies differ in mass and collider width. Torque = mass * 9.81 * longitudinal-Z lever in Nm; inertia 250kg*m², spring 500Nm/rad, damping 600Nms/rad, max tilt +/-18 degrees; 50ms max fixed physics tick. The alternate safe route is a separate BoxCollider at X4 across full approach/exit, outside bridge pressure volume. Pause, restart, actual fall respawn and IMGUI buttons implemented. The generated scene's camera and directional light are part of the Unity Editor factory.

Source core implements the deterministic angular model, not PhysX itself. Bridge input includes actual Collider.Raycast support. Raycast is an approximate center pressure probe, not full compound-hull support/continuous collision. Native Unity/PhysX may reveal physics issues, so do not claim G1.

## Repro and files

Only brand-new paths Assets/B0125CounterBridge001/** plus its .meta, Research/B01-25/P17/COUNTERBRIDGE001/**, Tools/B01/B01-25/P17W19CounterBridge/** and the single unique W19 workflow. Source assembly has 12 fresh GUID .meta, 3 dedicated asmdef, no dependencies on any canonical Codex assembly. Portable Research/CounterBridgeCore.cs and Unity Runtime/CounterBridgeCore.cs are byte-identical. Unity6 native PlayMode test sources include 6 authored tests, but licensed native Edit/Play and real PhysX remain NOT_RUN (no XML/Editor.log/exit proof).

The Editor menu B01/PASS17 W19/Create COUNTERBRIDGE001 Standalone Demo creates a **new nonoverwriting additive** Unity .unity scene only in disposable Unity6000.3.14f1. It will refuse pre-existing same name rather than mutate shared scenes. The generated scene file is not precommitted or claimed as imported.

From the repository checkout root using .NET8, Python3 and Git:
- dotnet build Research/B01-25/P17/COUNTERBRIDGE001/CounterBridgeRuntime.csproj -c Release -p:LangVersion=9.0 -warnaserror
- dotnet run --project Research/B01-25/P17/COUNTERBRIDGE001/CounterBridgeTests.csproj -c Release -- Research/B01-25/P17/COUNTERBRIDGE001/.output/source-results.json
- python Tools/B01/B01-25/P17W19CounterBridge/check.py --out Tools/B01/B01-25/P17W19CounterBridge/.output/receipt.json

Dedicated GitHub Actions invokes actual source build/tests on Ubuntu and Windows, 16 executable exact physical source/rollback/negative tests and a third independent actual original Unity SOURCE ZIP sha256sum/cmp job. Don't state PASS until CI on exact HEAD produces successful original job logs. Original source ZIP has 19 Unity source+meta paths plus embedded PACKAGE_MANIFEST.json, not a built Unity Player. The independent producer manifest pins Git blobs for every new nonmanifest file.

## Receivers and safety

B01-40 verifies original exact Git SHA/manifest, 12 unique GUIDs, three asmdefs, physical original ZIP and reversible scratch stage without deleting foreign empty dirs/files/symlinks (formal independent SOURCE G0 currently PENDING). B01-60 independently runs activated real Unity6000.3.14f1 PlayMode/PhysX tests with original XML/Editor.log+exit or reports NATIVE_G1_NOT_RUN. B01-00 alone decides any future Codex source offer. Codex ACK NONE / G2 HOLD / NOT INTEGRATED. Frozen #510, #556, #596 and alternative #604 ALT_HOLD unmodified. #642/#644 Save/Load under B01-23 exclusively.

Rollback only removes new B01-25 own source paths on an isolated disposable branch: never recursively remove unknown/foreign occupied or empty paths, old game code, or Codex assets.
