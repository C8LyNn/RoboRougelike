# BUILD001 R1 API
Namespace `B01.BUILD001`, assembly `B01.BUILD001.Runtime`. `GridMetres=.55f` matches original baseline. No direct references to R3.
- `BuildSocket`: `Cell` local occupied grid cell, `Face` unit outward normal, `Kind` enum, `Offset` and `HalfSize` for actual rectangular contact patch within a cell face.
- `BuildDefinition`: stable id, role Core/Armor/Wheel/Gun/Suspension/Device, integer `Cells` ownership and `Sockets`; `SupportsOtherParts`. For Wheel only axle socket, cannot act as structural support; Gun only base socket, cannot act as support.
- `BuildPose(Anchor,QuarterTurns)`: XYZ quarter turns in Unity Quaternion.Euler order. `BuildSocketGeometry.Valid(def,out reason)`, `Occupied`, `Touches(...,out areaM2)`.
- `BuildPlacementRules.Check(installed,candidate,out supportsPartId)`: Valid, InvalidDefinition, DuplicatePartId, OccupiedCells, NoSupport, NoCompatibleSocket, InvalidCore, OutOfBounds. `Reachable`: diagnostic graph rooted in alive Core; canonical P12 detachment untouched.
- `BuildSocketRaycast.ValidateHit`: Unity actual hit, authoritative root, definition/pose and world grid origin; returns Valid/NoOwner/DecorativeSurface/OutsidePatch/ObliqueHit and world face.
- `BuildFixtureCatalog`: isolated original fixtures ArmorCube, Core, Wheel, Gun, Suspension, Wide2x2. NOT registration of actual existing R3PartKind; GUID/HP/structural source remains target-owned.

Important: this R1 simplifies socket patches to axis-aligned rectangular faces in grid coordinates; physical contact area is checked mathematically. Actual artistic mesh and collider envelope must be separately verified using PARTSIZE (#141) before production integration.
