# R4 collider staging prototype

`R4ColliderStage.TryCreate` is an explicit, opt-in helper that requests cached source meshes and creates a disabled convex `MeshCollider` child for each piece under a supplied owner. It preserves the owner's existing colliders, layer and Rigidbody ownership; it rejects missing owners, unknown shapes and nonpositive/nonuniform scales. Its owner must dispose the stage before disposing the shared mesh cache. A failed creation destroys any children already created; disposal is idempotent and disables staged colliders before removing them.

**Nothing calls this helper in gameplay.** It does not activate colliders, replace an existing collider, resolve ports, modify an inventory/save, or make shapes buildable. Source XYZ and winding are unchanged; the inverse uniform scale is only a preparation for the existing scaled block transform. Whether Unity cooks and positions these colliders correctly must be checked in the native engine before any integration.

## Lightweight validation

- Actual catalog, validator, factory, cache and stage C# compiled and ran in the offline single-worker .NET 8 smoke project using minimal Unity stand-ins. The pre-implementation build failed because the stage did not exist. The smoke passed for three disabled Arch collider children, preservation of an existing collider, parent/layer and inverse scale, idempotent disposal, cleanup after a simulated partial child-creation error, and nonuniform-scale rejection. Earlier catalog, bundle, factory and cache checks also passed.
- New Unity `.meta` GUIDs were generated without launching Unity and checked for uniqueness under project `Assets`/`Packages`.
- **NOT RUN:** Unity compilation and EditMode (a targeted test was added), actual attached Rigidbody behavior, `MeshCollider.convex` cooking, transform/axis alignment, collision, performance, garage, saves, Windows Player and gameplay. Stand-ins cannot establish native Unity behavior.

Before production use, resolve the four source socket discrepancies, test actual collider cooking and all relevant rotations/scales, align placement/restoration and the P12 graph, and define stage/cache lifetime in the game. Existing game behavior is unchanged.
