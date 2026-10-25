# B01-20 — BUILD R1.3 executable repair contract (P0)

**Evidence**: Codex QA [#239](https://github.com/C8LyNn/RoboRougelike/pull/239) at `1b515154f5fadf56dabbd21977f5359c88208b23`: Unity native compile PASS; 34/38 NUnit PASS, four failing receiver cases reproduced twice; source producer #215 exact `489676414a3eb45fe70124098e3778a81d0aaff2`. Player 10/10 magenta/error shader at 720p/1080p. No actual R3/R6 mounting/drive test.

**Contract outputs**: Fixtures `HIT-*` `ROOT-*` `FP-*` `VOL-*` `DEMO-*`. `contract.json` is mandatory. Implement narrow isolated package only; no host/Codex files.

**Required conceptual interfaces (producer adapts names after inspection)**:
```csharp
BuildHitResult AuthorizeHit(RaycastHit hit, IRegisteredMountCollider colliderRegistry, PartInstanceId ownerId, SocketId socketId, BuildPose pose);
BuildPlacementResult ValidateCanonicalRoot(BuildSnapshot loaded, TrustedCoreId authoritativeCore);
BuildPlacementResult ValidateConnectedFootprint(PartDefinition def, IReadOnlyList<OccupiedCell> cells, IReadOnlyList<VerifiedPhysicalJoin> joins);
BuildVolumeResult CheckPhysicsEnvelope(PartCandidate candidate, IEnumerable<Collider> authoritativeBlockers, float toleranceMetres=0.005f);
```
All calls reject before state mutation; candidate commit only after all gates PASS. No automatic “nearest collider” authorization.

**HIT/W9-01** A collider must be explicitly registered as structural under an authoritative logical part instance AND explicitly listed by active socket ID. Parent ancestry, ray normal, near plane or same transform hierarchy cannot grant rights. QA sample: decorative child hit (0.31,1,0) with face(+X), <=.075m old patch proximity MUST `DECORATIVE_COLLIDER`. Real registered structural hit PASS. Wheel tread/gun barrel never structural.

**CORE/W9-02** One canonical trusted core key per robot. Save containing a second Core => `MULTIPLE_CORE_ROOTS` (quarantine build-edit until migration; keep save data, no silent destroy). Core-only BFS from trusted living root, every fragment requires verified support reachability; no 0HP support. Repaired/migrated save should preserve original item IDs and reject untrusted roots, including multiple roots in a loading transaction.

**FOOTPRINT/W9-03** Enforce 6-neighbour occupied-cell topology + separately verified physical weld/joint bridges for every edge of that topology; one OwnerId cannot connect cells 0 and 2 through an empty cell. Reject disconnected set or missing verified edge `DISCONNECTED_FOOTPRINT`. A legitimate rotated 2×2×1 one-owner module must PASS and retain one HP owner.

**VOLUME/W9-04** Logical occupancy alone never authorizes placement. In Unity, run broadphase then actual `Physics.ComputePenetration` (sync transforms) against real enabled non-trigger blocking colliders of live attached parts and map. All collider sizes including 1.2m oversized single cell; a real measured depth0.3100m REJECT `COLLIDER_PENETRATION`. Define numerical jitter tolerance ≤0.005m; missing/unsupported collider evidence => `PHYSICS_UNVERIFIED`. Valid socket contact may touch but positive penetration receives no blanket exemption. Strict test of rotated/compound colliders and placement near world obstacles required.

**DEMO/URP** Explicit compatible shader `Universal Render Pipeline/Lit` or `Universal Render Pipeline/Simple Lit`, else fail with diagnostics; never `Hidden/InternalErrorShader`. Assign original compatible materials to preview AND fixtures without global graphics/quality project override. Ten actual Player screenshots (5 states×2 resolutions), no >1% magenta in inspected rendered candidate area, both red rejected/green accepted visually distinct. Console exception-free alone is insufficient.

**Producer done:** new immutable Draft SHA, green all four W9 native regressions + 13 original and 21 positive (38 total baseline), 10 URP Player captures and test XML/logs, actual R3/R6 later through recipient, independent B01-60 green, B01-40 G2 once approved. Codex only integrates approved package.
