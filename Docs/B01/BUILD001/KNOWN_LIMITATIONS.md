# BUILD001 limitations
- This is a **separate proof-of-concept policy**: does not replace live R3MountingGrid, P12 damage graph, R3 Garage, or live W8 WheelContacts. Main game may still allow invalid tire mounts until Codex integration.
- A surface patch alone does not guarantee MeshCollider shape/continuous watertight silhouette; mesh-to-grid consistency follows PARTSIZE #141 and ARMOR #143. No arbitrary surface anchor snapping or async scene import; valid sockets are specified by designer.
- Gun barrel/weapon runtime sweep and muzzle LOS belong to FIRE #142, not performed by this paczka.
- R1 socket orientation is grid world-aligned at 90° XYZ and cell unit .55m. No angle-adjustable live hinge, arbitrary pivot transformations or collision sweep for moving parts. Source placement checks grid occupancy but collision envelope of oversized parts needs independent volume tests.
- No save migration or UI/R3 terrain installation; Codex must map existing part IDs/ContactMask and handle loaded legacy vehicles via explicit policy.
- Unity native Edit/Play, actual live game and user manual tests NOT_RUN; independent B01-60, B01-40 GUID/hash/collision, live Codex worktree reservation not acknowledged; G2 HOLD.
