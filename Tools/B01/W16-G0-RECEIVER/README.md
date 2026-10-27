# B01-40 / W16 — Two-package G0 receiver, ownership and freeze gate

This is an **independent, read-only, fail-closed** preflight for the *only* two W16 production streams. It never edits the Unity project, imports code, creates a CODEX_ACK, promotes G1/G2, or transfers ownership. Source code ownership remains **B01-23 PARTSIZE** and **B01-20 CHL-003**. The gate's own files live exclusively under `Tools/B01/W16-G0-RECEIVER/**`, `Docs/B01Validation/20261027/W16/**` and its own workflow.

## Required producer input

Ask the **producer** for its complete original package ZIP, an **external** frozen per-file receipt, and a full source commit in a local Git repository containing the producer branch. Do not generate new producer code to satisfy the receiver. The only recognized IDs, version, producer and path roots are:

| Package | Version | Producer | Allowed ZIP roots |
|---|---|---|---|
| PARTSIZE-001 | R1 | B01-23 | `Assets/B01Packages/PARTSIZE001/`, `Docs/B01/PARTSIZE001/` |
| CHL-003 | R1 | B01-20 | `Assets/RoboRougelikeChallenges/CHL003/`, `Docs/B01/CHL003/` |

These are proposed **receiver intake scope locks** matching the isolated design, not permission to edit either producer's files. If producer uses a different approved root, obtain B01-00 scope decision before modifying the receiver rule; do not silently broaden the allowlist.

The receipt is an external UTF-8 JSON object:

```json
{
  "package_id":"PARTSIZE-001", "version":"R1", "producer":"B01-23",
  "state":"OFFERED_FROZEN", "source_sha":"<exact 40-hex Git commit>",
  "producer_pr":"https://github.com/C8LyNn/RoboRougelike/pull/<real-id>",
  "zip_sha256":"<64 lowercase hex>",
  "files":[{"path":"Assets/B01Packages/PARTSIZE001/Runtime/X.cs","bytes":123,"sha256":"<64 lowercase hex>"}],
  "external_assemblies":[]
}
```

The example contains placeholders, **not a valid receipt**. Each archive file needs its own complete record including `.meta`, source/tests/docs. No registered payload may be outside the owner roots. Physically present Runtime C#, `.unity` demo, actual EditMode/PlayMode C# tests including a named Negative or Invalid test source, at least the four handoff documents, and (CHL only) at least two separately authored rig `.prefab` assets are mandatory minimum *source* preflight. Their presence is **not proof of functional physics**, two actually different Rigidbody rigs, correct save migration, or native NUnit PASS.

## Usage (Windows PowerShell, read-only)

```powershell
python Tools/B01/W16-G0-RECEIVER/gate.py `
  --zip C:\QA\PARTSIZE001_R1_original.zip `
  --receipt C:\QA\PARTSIZE001_R1_external_receipt.json `
  --git-repo C:\QA\producer-fetched-full-repo `
  --target-index C:\QA\optional_target_file_index.json
```

The `--git-repo` must contain **the precise Git object at the receipt's `source_sha`**, not just a similarly named branch. It is read-only: `git rev-parse sha^{commit}`, `git ls-tree` and independent Git blob hashes of all ZIP files. An `OFFERED_FROZEN` or claimed source **fails** without this commit proof. Local GUID-based `.asmdef` references are resolved against real in-ZIP `.asmdef.meta` GUIDs. Explicitly declared external assembly names/GUIDs remain dependency declarations, **not** proof that the target has them. Optional target index is external JSON mapping exact paths to `{"sha256":"...", "guid":"..."}` entries; conflicts fail. The index must be independently generated from an **authorized, clean target snapshot**. Never infer live Codex tree state from historical #272 GitHub commit alone. Exit `0` means **only `G0_SOURCE=PASS_SOURCE_BYTES`** within provided evidence, `2` means refusal. Output always retains `codex_ready=false, G2=HOLD`.

## Mandatory receiver workflow after successful G0

1. Cross-check producer PR's actual HEAD with the receipt exact source commit and #79 latest ownership state. `OFFERED_FROZEN` is immutable by identity (`PACKAGE_ID + VERSION + SHA`); *never overwrite that version*.
2. Run original immutable ZIP + actual Git commit source check with this tool. Preserve real `Assets/B01Packages.meta` from target when applicable; no blind meta replacement. Use existing [PACKAGE-AUTO-001 #238](https://github.com/C8LyNn/RoboRougelike/pull/238) for **separate marked detached worktree** dry-run/stage/rollback after B01-00 authorizes target scope. This tool intentionally performs no install and makes no rollback claim.
3. Obtain *independent* B01-60 execution/review of actual Unity 6000.3.14f1 Edit/Play, demos, negative PhysX/overlap and CHL Light/Heavy crossing/retry. Retain raw XML, logs, process exits, native source SHA, seed/rig identity and screenshot/video as relevant.
4. B01-00 alone evaluates G2; after OFFERED freeze, **Codex ACK only from explicit Codex record**. Even a syntactically plausible `codex_ack_link` is printed `NOT_VERIFIED` until humans independently inspect the comment and scope. `CODEX_READY` is *not* a recognized state in the gate.
5. Do not edit R3/R6/Pack/Garage/camera/weapon/ECON/any Codex-frozen source. No main/develop merge or live Codex installation.

## Tests and limits

`python -m unittest discover -s Tools/B01/W16-G0-RECEIVER/tests -v`

Unit tests build **real synthetic ZIP bytes** (positive PARTSIZE and CHL, unsafe path/ZIP/tamper/GUID/casefold/missing `.meta`, wrong ownership, source commit mismatch, incomplete physical archive, invalid ack and target conflict). One fixture creates a **real temporary Git repository and commit**, then validates each independent Git blob against the ZIP. These are tests of this receiver algorithm only, *not* producer packages or Unity runtime. CI is Windows+Ubuntu with Python stdlib; no Unity credentials, native tests **NOT_RUN**. ZIP size defaults 100MiB total, 30MiB per entry; if legitimate package exceeds limits request explicit receiver review, never silently disable safety.
