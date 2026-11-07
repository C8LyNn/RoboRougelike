# R4 rotated port native QA

Unity 6000.3.14f1 EditMode, exact source `4b80764176d10c9594fd49e02b4bfffa96f4d7c0`: **3/3 PASS**, process exit 0. Evidence: `Artifacts/Local/RC2-R4-RotatedPorts-4b807641-20261107T165951Z/EditMode.xml` and `EditMode.log`.

The new test covers 64 XYZ quarter-turn combinations and establishes that they correspond to 24 distinct orientations. For every combination, the actual BUILD001 socket geometry reports a Wedge's small structural patch overlaps a neighboring wheel, but the separate full-port-only support definition excludes that wheel; the full Cube patch accepts it. The other two tests cover source bonds, defensive copies, invalid shapes and the OctColumn quarantine.

This exercises rotated geometric definitions only. New R4 shapes are not connected to placement, collision, P12 graph, inventory or save/load. It does not prove rotated meshes or complete player workflows in Unity.
