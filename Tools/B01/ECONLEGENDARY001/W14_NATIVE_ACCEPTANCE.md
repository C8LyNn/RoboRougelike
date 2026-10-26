# W14 — RC2 ECON/LEGENDARY native acceptance / one finite QA queue

Source owner B01-22; producer Draft #263 (current immutable HEAD from PR), source dependencies
LEGENDARY #242 `dffabbc6960304fdb33322b5a23c884feac804c6` and
ECON #252 `12572117a3dce369257feabc57021cde58023369`.
Target unified RC2 #258 production source `46b42f62d591ad4aeb9dbfdeda9997044c6b5e1f`
is **REFERENCE ONLY / NO LIVE INSTALL OR EDIT**.
BUILD R1.4 #260 `95707c68c8071a949b5d9fec7ff57926e92f8198`
has separate native G1; do not mix its unapproved sockets into this acceptance.

## Original source bug proof
- W14 RED at GH workflow original `984ca22457a41d61d6b1100a1b7945180f6bc84b`:
  `W14_same_guid_different_physical_owner_must_reject` failed; source
  allowed two separate backing objects with equal GUID/kind/HP to pass.
- Fixed `ISharedR3PhysicalOwnerIdentity.AuthoritativeR3Model` requires
  both ports to expose the same actual R3VehicleModel reference.
- Receiver C# bridges generated from pinned actual upstream install examples,
  original Git blob hashes must match. No R3/Unity/Pack replacement.

## Exact acceptance sequence for Codex or licensed B01-60 Windows operator
1. **Disposable Unity6000.3.14f1 URP worktree**, pinned all three source
   packages. Verify 3 asmdef names `B01.Legendary.Unity`,
   `B01.EconRebuild.Runtime`, `B01.EconLegendary.Guard`;
   import all corresponding `.meta` with target root GUID preserved;
   compile `Unity -batchmode -nographics -projectPath ... -quit -logFile ...`
   and retain original Editor.log and exit. Never execute on active game.
2. Native EditMode: **8 authored tests**, including two NEW:
   `IdenticalGuidAcrossDifferentR3StoresMustReject` and
   `MissingRealR3OwnerProofRejects`. Require original NUnit XML
   `8/8 PASS` with Unity process exit 0; if only 4 run, it's old source.
3. Native R3-backed test (may need small receiver hook test, not authored
   in this isolated package): two independently constructed
   `R3VehicleModel` objects with SAME `R3PartState.Id`, kind, HP0.
   Same-object owners: guard permits valid healthy item operations once
   reservations and typed sockets are valid. Different-object owners:
   mutation **BLOCKED** and 0 currency debit / 0 ownership change.
   Real native observer logs must include both runtime object identity
   and immutable physical instance ID, not just IDs.
4. Full win-bound physical flow: actual R3 item 0HP (mounted + inventory
   cases) → ECON queue → defeat/retry no completion and 0 instant R3 heal
   → genuinely authenticated next challenge WIN event → ONE batch,
   same physical GUID, HP restored, exactly one debit/free usage. Duplicate
   win event after save/load must be ignored; reload mismatched R3/ECON
   owners rejects before mutations. Validate typed MobilityHub rejection
   until BUILD authority certified, and other robot unaffected.
5. Crash-injection: host wallet/R3 Parts inventory/ECON+LEGEND sidecars
   must commit as ONE durable checkpoint. Inject failure after each stage,
   cold restart and prove either old snapshot or complete new snapshot,
   NEVER half-restored, missing or duplicate physical GUIDs.
   External integration test owned by Codex, **NOT authored/passed here**.

## Pass thresholds and remaining dependencies
- Portable producer gate: verified real immutable #252+#242 C# blobs, C# build,
  receiver install-only bridge C# build, original 20/20+ tests, unique GUID,
  deterministic ZIP and SHA256. **This is not native**.
- Native G1 acceptance = real Unity engine compile + eight original NUnit
  + R3 object-backed ownership + 0HP victory/save results; otherwise
  `NOT_RUN` / `FAIL`, no fabricated PASS.
- Anti-softlock loaner decision #154/#253 remains HQ-only, costs and
  LEGEND mass/fit unapproved; 0HP one-win delay not shortened.
- B01-40 G0 live destination SHA and ZIP checks; B01-60 independent
  native G1, only B01-00 G2. No merge or active RC2 modification.

**One receiver ask:** isolated G1 test of same-GUID two distinct real
`R3VehicleModel` objects and one shared-object healthy control, using
patched receiver examples from this exact CI artifact. Preserve XML,
Editor.log, source and test run SHA, wallet and inventory snapshots.

## W15 added native negatives — both must reject before any R3 mutation
- Authored `NoncanonicalUppercasePhysicalGuidMustFailClosed`: same R3
  authoritative object, uppercase physical GUID and matching ECON view;
  `INVALID_PHYSICAL_INSTANCE`, repair count zero.
- Authored `EmptyPhysicalGuidOrHpOverMaxMustFailClosed`: Guid.Empty and
  nonfinite/out-of-range physical HP, reject without invoking R3 Repair.
- Updated native authored total: **8**, actual licensed Unity result:
  **NOT_RUN** until evidence exists. Do not call C# surrogate compilation native.
