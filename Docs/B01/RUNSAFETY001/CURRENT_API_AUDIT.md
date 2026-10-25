# Verified live integration seam (READ ONLY)
Source inspected: Codex Draft PHY PR#125 HEAD `916654634d1bfdc2bbc14e64b9eeac856ede1fa8`; **NOT** main or active unpublished Codex target SHA. Unity 6000.3.14f1.

`Assets/R3/R3VehicleModel.cs`: `RepairCost` for Health<=0 returns **full purchase price** today, unlike approved future ECON; `Repair` immediately deducts Scrap/sets Health=1; `RepairAffordable` loops `Repair` to cover all affordable parts; `RepairInventory` immediately restores inventory HP; `EmergencyRepair` calculates a shortest broken-link plan and **immediately sets Health=1**, charges costs if scrap enough. All five require careful 0HP routing once #136 is integrated.
`R3VehicleModel.CanDeploy` derives from `DeployError`; it requires alive Core, functional healthy/connected drive, valid gun and structural connectivity. Side-specific wheel/track mobility is enforced by `HasMobility`.
`Assets/R1/R1RunController.cs` `StartNextEncounter()` currently refuses if `!VehicleModel.CanDeploy` before `Progress.LeaveGarage()`. `GarageRepairAffordable` and `GarageEmergencyRepair` route through `EditGarage`; `GarageRepairInventory` can instantly heal stock.
`Assets/R1/R1RunProgress.cs` has states Garage/Combat/Won/Lost and real win processing `WinEncounter()`; finished run/loss is already canonical. No attempt made to change them.
PR#149 BUILD typed socket and #103 MAPGEN remain separate Drafts, not prerequisites for this isolated pure model.

## Codex-owned eventual adapter, FOR LATER after G2 / HQ policy GO
Map `R3PartState.Id` => SafetyOwnedPart ID (no cloning as real R3PartState), 0HP queue from ECON #136 (not two competing stores). Derive SafetyVehicleSnapshot from authoritative R3.CanDeploy+actual functional graph/weapon/drive and state of Progress; never infer from visual prefab children alone.
On Garage attempting StartNextEncounter, if normal CanDeploy then pass unchanged; if loaner gate valid and approved, instantiate a separately owned player-controllable minimal vehicle once and transition via **current canonical Progress.LeaveGarage exactly once**. Do not set CanDeploy globally true or ignore other blockers. Real victory emits an ID-stable event exactly once to the ECON queue; real defeat does NOT emit success. Canonical run loss sets external run-ended state and cleans lease without granting restart.
Loss report, reward identity and saved owned parts remain existing game responsibility.
Abort if Codex already owns/modified R1/R3/ECON target paths or target branch/HEAD differs from acknowledged SHA. Use disposable checkout to evaluate source before approval.
