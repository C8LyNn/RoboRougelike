# B01-62 — MAPPIPE-RECEIVER-001 / PASS17 WAVE02

**Purpose:** independent, actually executed receiver of three prior, unmodified producer packages. It exercises a realistic *offline* authoring/recovery scenario on an original 360-item arena, not another map editor or another rewrite of either backup/diff tool.

## Original immutable dependencies

| Producer | PR and exact input SHA | Physical original file | Git blob |
|---|---|---|---|
| B01-62 MAPSAFE-001 | #347 @ 956115a26fc1987f78b6bb64c00df4bf46d5756b | Tools/B01/B01-62/MapSafe001/mapsafe.py | 7748b554ce75a4988a7e4099845dc82d5d6c48e4 |
| B01-62 MAPDIFF-001 | #351 @ 9bd0caf2ada19ccf887496eee2c72fc5acbfad73 | Tools/B01/B01-62/MapDiff001/mapdiff.py | 674dbb09460421d0af48d9f614f7f68bd01c5c18 |
| B01-25 SLALOM-001 | #346 @ f565e250659692541d672cdab108c4cfb8cfdd31 | Research/B01-25/P17/SLALOM001/arena.json | 013f5fe2058e76233f580a439e8402c0fe09f124 |

All three are read as original Git blobs by the scoped CI. Neither source is copied into our Git tree, rebased or modified. The QA code and workflow live ONLY in Tools/B01/B01-62/MapPipelineQA/** and .github/workflows/b01-62-mappipe-receiver.yml.

## What the executable really does

1. Open a new disposable physical Maps/ and Vault/ directory and copy the **actual** SLALOM-001 arena with 360 items (252 Ground). Execute the original MAPSAFE user CLI to create and verify a SHA-256 backup.
2. Remove the uniquely identified existing LowBarrier at (6,6) from a **copy**, leaving 359 valid items. Execute actual MAPSAFE restore with *no overwrite consent*, then with an intentionally wrong expected hash. Both must fail without writing anything or creating an undo copy.
3. Execute original MAPDIFF CLI to generate a real JSON+SVG BEFORE/AFTER comparison. Require exactly one removed barrier, one changed footprint cell and physical SVG XML validation; verify input file hashes exactly.
4. Execute MAPSAFE **explicit restore with correct expected hash**, then independently verify original full byte identity and a separately generated undo backup containing the exact 359-item modified bytes.
5. Execute MAPDIFF on original vs restored copy and demand **zero differences**. Verify no leftover temporary files and no changes to any of the three original producer inputs. Emit reproducible receipt.json and the actual controlled-change.svg for another QA reviewer.

The tool does not attempt to save a run, does not load Unity or simulate native PhysX, does not modify Codex's gameplay or alter any submitted arena. Generated SVG and JSON allow Codex to inspect actual changes, consent, restoration and undo in one linked reproducible workflow.

## Run and reproduce

Requires Python 3.11+ standard library, Git, and an existing checkout with access to the three source commits.

    git fetch origin 956115a26fc1987f78b6bb64c00df4bf46d5756b
    git fetch origin 9bd0caf2ada19ccf887496eee2c72fc5acbfad73
    git fetch origin f565e250659692541d672cdab108c4cfb8cfdd31

Retrieve the three exact Git files and check their Git blob IDs against the table (the Actions YAML has copy-ready fetch/extract commands). Put extracted files with original names into one existing folder outside the repository, e.g. original-inputs/ (mapsafe.py, mapdiff.py, arena.json).

    MAPPIPE_INPUTS=/absolute/path/original-inputs python -m unittest discover -s Tools/B01/B01-62/MapPipelineQA -p test_receiver.py -v
    mkdir -p receiver-output
    python Tools/B01/B01-62/MapPipelineQA/receiver.py --source-dir original-inputs --out-dir receiver-output

On Windows, set the MAPPIPE_INPUTS environment variable via PowerShell before running unittest. The tool refuses to overwrite existing receiver outputs. Real GitHub Actions executes full Windows/Ubuntu tests and requires identical bytes for output SVG/JSON between those two operating systems.

## Defect/QA gate and receiver

The receiver is independent source execution and externally validates **actual unchanged source and data** from three packages; it is **not** B01-40's formal provenance acceptance or B01-60's independent native G1. No synthetic human results or gameplay acceptance.

- **G0:** exact owner-source blob/provenance, actual working CLI process exit codes and filesystem data invariants.
- **G1:** Unity 6000.3.14f1 native import/EditMode/PlayMode NOT_RUN by this receiver.
- **G2:** integrated game NOT_CLAIMED. Codex ACK PENDING. No merge or Codex source changes.

**Codex handoff:** inspect this PR's tested output artifacts, run receiver on verified pinned inputs, then decide whether to adopt either existing utility as a separate development-time authoring tool. No import inside Assets/**, no automatic gameplay save/restore or UX policy change.

**Rollback:** delete only this new QA folder and dedicated workflow. Retain every owner source package, original game map, generated screenshots/reports and MAPSAFE vault without mutation.

Known limits: verification is offline and uses subprocess Python CLIs rather than native Unity.JsonUtility; local-filesystem atomic replacement is not safe as concurrent live-game CAS. The controlled one-barrier removal is intentionally a test copy, not a recommended new level and not a geometry/vehicle traversal acceptance.
