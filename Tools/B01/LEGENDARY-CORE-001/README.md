# B01-22 / LEGENDARY-CORE-001 R1 — isolated executable prototype

Issue [#224](https://github.com/C8LyNn/RoboRougelike/issues/224), owner design [#222](https://github.com/C8LyNn/RoboRougelike/issues/222). Implemented as pure `.NET 8`, outside Unity `Assets`. This does **not** ship legendary effects, geometry, damage, new garage inventory, or Unity integration.

## Invariants / operating contract

- `LegendaryDefinition.Id` is a stable TYPE ID; `PhysicalPart.InstanceId` is a unique physical COPY ID, mapped in a future adapter to `R3PartState.Id`.
- On each **individual robot**, at most one physically mounted item per non-null `LegendaryDefinitionId` — even if the item is at 0 HP. Different legendary types coexist without a global legend cap. Another robot may equip the same type.
- The inventory may own several physical items with the same legendary type. The prototype maintains a **single dictionary** keyed by physical ID; `RobotId == null` denotes unequipped inventory, not a second copy. **Do not instantiate this prototype as an independent garage inventory in Unity**; the adapter must use R3 `Parts` and `Inventory`.
- A legendary Railgun and ordinary Railguns count as the same `Railgun` weapon family; a vehicle may equip at most **two weapon families**. Number of individual barrels is unrestricted in this *logical* prototype (physical mounts/size are outside scope).
- `CanEquip` and `TryEquip` reject duplicates *without altering inventory, IDs, currencies or robot state*. `TryAcquire` atomically rejects malformed/duplicate-instance reward batches and replayed event IDs.
- Zero-HP parts do not vanish. Unmount preserves their instance ID. Repair restores health without issuing an ID; financial eligibility is **not** simulated.
- Save/load validates type mapping, unique physical IDs, per-robot legendary and family caps, mounted robot references, and reward event ledger. Preview copies are not extra owned copies.

## Catalogue — two **proposed** designs

| Stable type ID | Name | Base R3 part kind | Weapon family | Footprint proposal | Mass proposal | Effect state |
|---|---|---|---|---|---|---|
| `LEG-WPN-001` | Przebijacz | Gun | Railgun | 3×2×2 cells | 220 kg | Kinetic penetration / high recoil: **GDD ONLY** |
| `LEG-MOV-001` | Serce Zawisu | HoverEngine | n/a | 3×3×2 cells | 150 kg | Lateral hover drift / EMP weakness: **GDD ONLY** |

Masses, footprints and socket tags are **hypotheses**, not values validated against Unity physics. Serce Zawisu was chosen over Atlas because `R3PartKind.HoverEngine` already exists in the canonical `main` model, whereas Atlas would require unresolved track/traction architecture decisions. The original `main` `R4WeaponDefinition.cs` exposes Basic/Automatic/Heavy; `Railgun` is a target family name that needs explicit mapping once the newer weapon branch passes integration gates.

## Run real tests

Requires `dotnet SDK 8.x`. No external NuGet test runner/packages are required. The test executable contains named unit assertions and a seeded **2,000-transition property regression**. The JSON report is written even when individual cases fail.

```bash
dotnet build Tools/B01/LEGENDARY-CORE-001/tests/LegendaryCore.Tests/LegendaryCore.Tests.csproj -c Release
dotnet run --project Tools/B01/LEGENDARY-CORE-001/tests/LegendaryCore.Tests/LegendaryCore.Tests.csproj -c Release --no-build -- --report artifacts/legendary-core/test-results.json
```

CI: `.github/workflows/b01-legendary-core.yml`. It builds both projects, actually executes tests, validates catalogue IDs/schema, generates `test-results.json`, `source-sha256.txt`, `ci-receipt.json`, `ci-summary.md`, and uploads them as a workflow artifact. Check the **specific run conclusion** before claiming PASS. `native_unity_tests: NOT_RUN`.

### Hypothetical robot build matrix — not a physics simulation

| Distinct legendary types | Logical uniqueness | Mass/size validation | Implication |
|---|---|---|---|
| 0 | allowed | Unity pending | baseline for Light/Standard/Heavy |
| 1 | allowed | Unity pending | test Przebijacz recoil and socket/load |
| 2 | allowed (both pilots) | Unity pending | test combined mass 370 kg / competing space |
| 4 | allowed by design if four different types exist | **not executable with two pilot definitions** | future catalogue + collision/CG stress |
| 4 copies of same type on one robot | rejected | not needed | invariant violation |

## R3 audit and future adapter — **NOT IMPLEMENTED**

Canonical baseline: `main` commit `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`. Read `Assets/R3/R3VehicleModel.cs`, `Assets/R4/R4WeaponDefinition.cs`, `Assets/R3/R3GarageView.cs` and issue #41 before integration.

1. After HQ B01-00 explicitly reserves production sources and gates, extend or externally map `R3PartState` with **optional stable legendary definition ID**, never replace its per-instance `Id`. Do not add enum members per legend.
2. Adapt `R3VehicleModel.PlacementError/CanMount/Mount` to check the mounted vehicle's `Parts` by definition ID before deductions or list mutation. The invariant is per robot, not per run. Preserve `Parts` + existing private `inventory` ownership; no second runtime storage.
3. `R3VehicleModel.Remove` currently adds removed parts to inventory **only when Health > 0**, which would destroy ownership of a dismantled 0-HP legendary. Resolve this in the authoritative adapter before offering that UI operation; preserve ID or explicitly disallow it, never silently generate a replacement.
4. `ReceiveSalvage` validates IDs only within one R3 vehicle. Enforce live-instance integrity across reward callbacks/robot migrations using an authoritative ownership path if required; do **not** introduce a global *legendary type exclusivity* registry. `DeepCopy()` previews retain IDs on purpose and are not additional owned parts. The design constructor regenerates IDs from templates: do not use it for loading already-owned items.
5. Respect actual R3 connectivity, placement, Repair/RepairInventory costs, scrap and legal removal. Prototype `TryRepair` is *health-only* and cannot be integrated directly as a free repair API.
6. Coordinate with #41 weapon family remap, #136 ECON, #137 ECOMOD, #140 BUILD, #141 PARTSIZE, #142 FIRE and real hover physics. Multi-cell placement, sockets, mass, recoil, EMP and visuals have **no executable implementation here**. Validate with Unity 6000.3.14f1 and independent B01-60 QA before G1/G2.
7. Do not touch active Codex/OBJ/PHY branches, `Assets/R3`, `Assets/R4` or existing production scene/prefabs. B01-40 owns integration checks; B01-00 owns G2.

## Evidence and release gates

- `G0_SOURCE`: deterministic .NET compilation and regression suite + hashes + catalog validation.
- `G0_TARGET`: R3 adapter compatibility review **NOT_RUN**.
- `G1_UNITY_NATIVE`: real Editor/PlayMode/Windows physics/garage/serialization tests **NOT_RUN**.
- `G2_OWNER_INTEGRATION`: **NOT_APPROVED**. This branch/PR must remain Draft.

Note that independent robot models may have copied preview states containing the same instance IDs; this sample forbids duplication **within a single authoritative state**, but cannot establish full game-wide authoritative ownership without a future live R3 adapter.
