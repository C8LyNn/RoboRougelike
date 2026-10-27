# B01-22 — W16 CHL-003 economy boundary: QA ONLY

**Not a new gameplay package.** Governance: [#276](https://github.com/C8LyNn/RoboRougelike/issues/276), CHL ownership [#91](https://github.com/C8LyNn/RoboRougelike/issues/91), Codex scope [#275](https://github.com/C8LyNn/RoboRougelike/issues/275). Ownership B01-22 limited to this read-only tool and its own CI. No changes to Unity Assets, PARTSIZE, CHL producer, legacy ECON #252, guard #263, R3, R1 or Codex host.

## Real published C# source verified by this QA

The script downloads the *actual published Codex host code*, pin `88b8eed60345c49481648833e1fde6dc1a400258`, and validates original Git blobs before examining anything.

- `Assets/R1/R1RunController.cs`: Git blob `c6d5f3efe4949da68a5c48ec38ef5028af8b8410`
- `Assets/R3/R3VehicleModel.cs`: Git blob `cdd75fff4923a382b16c430dac1452320eda88f6`

Current pinned real `R1RunController.Update` awards one standard encounter payout in Combat state after the loss check and transitions into Salvage. Original win inputs include `Encounter.Cleared`, `CheckpointRace.Won` and `RoadChallenge.Completed`. It does **not** prove CHL-003 pays rewards or exists. The *run-state* gate is not itself a durable authenticated event-ID journal. `R3VehicleModel.RepairCost(0HP)` equals buying a new part on this **old pinned baseline**, already owned by Codex under #275; the QA does not patch it.

## Contract for the isolated CHL-003

CHL-003 standalone obstacle course must **NOT grant Scrap, items, salvage or trigger RunController victory**. Event `Completed` is a gameplay result, not authorization to modify the R3 wallet. Retry x3, duplicate collider contacts, relaunch and fall→restart must leave persistent R3 currency and inventory identical; future opt-in host adapter may grant only after a real, uniquely identified, approved victory with atomic journal and cannot be authored here. B01-20 remains sole CHL producer.

The scanner rejects CHL C# files with direct economy writes, raw `WinEncounter/ConfirmRewards` calls and R1/R3/R4 namespace dependencies in the host-neutral core. **It is static QA only**. Indirect callbacks, reflection and genuine physics must be reviewed independently. When CHL code isn't present, the report says `SOURCE_NOT_PRESENT` and **NOT_RUN**, NEVER PASS. Core demo and real Unity must be tested in licensed Unity6000.3.14f1 by B01-60 after code arrives.

## Reproduce exactly

```bash
python Tools/B01/QA-CHL-ECON-W16/prepare.py
python -m unittest discover -s Tools/B01/QA-CHL-ECON-W16 -p 'test_*.py' -v
python Tools/B01/QA-CHL-ECON-W16/audit.py --host Tools/B01/QA-CHL-ECON-W16/_pinned --chl Assets/RoboRougelikeChallenges/CHL003 --out artifacts/chl-econ/receipt.json
```

`test_boundary.py` test 01 inspects actual original R1/R3 C#; subsequent mutations test fail-closed source audit. CHL snippets in fixture tests 09–15 are explicitly **synthetic tests of the scanner**, NOT tests of CHL package nor Unity. Windows and Linux Actions execute both. CI artifact contains pinned blob receipts, static outcome, and own-scope manifest. No executable Windows CHL demo is bundled because B01-20 has not supplied it here.

**One receiver ask:** after B01-20 publishes CHL-003 R1 SHA, run this gate on actual source plus independent B01-60 native two-different-vehicle tests with R3 wallet + full physical inventory GUID snapshots before/after 3 retries, duplicate hits and Completed. Native source/Unity G1 remains NOT_RUN until that evidence exists. No G2, no merge, no CODEX_READY.
