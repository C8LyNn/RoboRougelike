# PARTSIZE-001 R1 — isolated physical part dimensions (B01-23)

**State: IN_PROGRESS, not OFFERED, CODEX_READY or G2.** Only two new W16 streams exist: PARTSIZE-001 and CHL-003. PARTSIZE is independently owned by B01-23. The existing Codex #272 BUILD R1.4 and active #275 Garage/CAMERA/weapon/economy scopes remain **Codex-only**. No B01 script here edits them.

## Audited original game facts, no guessed model measurements

- Latest published working integrator source pinned: `88b8eed60345c49481648833e1fde6dc1a400258` (Codex #272); branch PR head is a separate report/metadata SHA and must not be treated as current unpushed worktree.
- Source `Assets/R4_1/R41PartGeometry.cs`, Git blob `76a14de4c347dd3e91b0a37487d804023eab5da1`: `Grid=.55f`, `BlockSize=.52f`. The root of a normal R3 part is a .52m cube; wheels, guns and other 3D payloads can protrude much further.
- Source `Assets/R3/R3BonusModuleGeometry.cs`, Git blob `99f55a2002a80603f1707466d7a0066260d5c3ee`: one physical owner with **four occupied logical cells in 2x1x2**, root BoxCollider size `1.07 x .52 x 1.07m`, center `(.275,0,.275)m`, plus any separate external/functional payload still requiring measurement.
- Source `Assets/R3/R3VehicleModel.cs`, blob `cdd75fff4923a382b16c430dac1452320eda88f6`: 19 `R3PartKind` variants, six 2x1x2 logical wide kinds, remaining 1x1x1; user-generated rotations are integer XYZ quarter-turns. No automatic migration of an old save to a larger envelope is permitted.
- `observed_r3_size_matrix.csv` lists **all 19 real R3 source kinds**; native Collider/mesh/mass/HP/swept clearances are not falsely populated. The eight source-derived root boxes do **not** certify all physical subcolliders.

## Actual source implementation (inside sole PARTSIZE packet)

- `PartSizeContract.cs`: host-independent, genuinely production-compiled C# type/cell geometry (one part ID, N occupied cells), integer Z→X→Y quarter turns, exact metric 8-corner box rotation, six-face external-only contacts, occupied-owner intersections, 5mm out-of-volume threshold, fail-closed UNMEASURED.
- `PartSizeBaselineCatalog.cs`: all 19 existing R3 names in audited pinned source; six wide kinds receive only source-derived core BoxCollider, Armor/Core receive only source-derived root cube. **Eleven** movable/decorative kinds remain `UNMEASURED_PHYSICAL_ENVELOPE` and cannot be certified by assumed .52m box.
- `PartSizeNativePhysics.cs`: **actual Unity-only Physics.ComputePenetration** on physically inventoried owned Colliders + independently discovered nearby world Colliders from `Physics.OverlapBox` (no caller-attested completeness shortcut). Omitting a real overlapping neighbor now returns `INCOMPLETE_EXTERNAL_BROADPHASE`, even when a caller claims complete=true. `ValidateMountSocketAuthority` always denies an exposed logical grid face without a true trusted typed socket registry. Verifies full child Collider inventory, owner pose/scale, Collider AABB within rotated owned-cell bounds and 5mm tolerance; absence of complete nearby broadphase fails closed. This is a standalone candidate oracle, *not* a replacement R3 installer.
- `Demo/PARTSIZE001_Demo.prefab`: put on empty disposable Unity scene → Play; spawns source-derived cube, yaw90 2x1x2, and deliberately oversize 1.2m negative. Runtime labels and green/red geometries. Demo is authored, **Unity native preview NOT_RUN**.
- NUnit EditMode **17 authored cases** including seven rotation parameter cases; PlayMode **11 authored cases** including real BoxCollider/Physics and intentionally omitted collider. Authored means native NOT_RUN until licensed receiver logs/XML.
- `Portable/PartSizePortable.csproj`: builds **the same real runtime .cs source** (pure math + catalog) using .NET8, no engine stubs or fake PhysX. CI executes thousands of real geometry/permutation checks, not Unity or whole-game tests.
- `package_partsize.py`: complete physical Unity package/source docs/tests, stable .meta/GUID, zip SHA256+all source hashes/bytes, deterministic duplicate packaging. All source deliveries require B01-40 independent G0.

## Reproduction and safety

```bash
dotnet run --project Tools/B01/PARTSIZE001/Portable/PartSizePortable.csproj -c Release
python Tools/B01/PARTSIZE001/test_package_partsize.py
python Tools/B01/PARTSIZE001/package_partsize.py --out dist/B01-PARTSIZE001-R1-FULL-SOURCE.zip
```

**Never copy** `Assets/B01Packages.meta` from a producer into Codex. Existing target root GUID must be preserved; the package includes only its own **`Assets/PARTSIZE001.meta`** and descendants.

**Next genuinely unfinished gates:** Unity 6000.3.14f1 compile, 17 EditMode / 11 PlayMode original XML/log/exits, actual prefab at 720/1080 screenshot and real PhysX; B01-60 independent test; B01-40 verified installation/rollback on detached target and controlled Codex claim; design choices for sizing moving parts after measurements. No source shell generated ready receipt or positive physics claim.
