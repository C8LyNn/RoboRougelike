# B01-40 PASS17 WAVE09 — immutable G0 receiver for #429 and #421

This is an **independent physical source intake**, not another mechanical or gameplay QA suite. It **never edits either producer branch**, Codex source, main or develop.

## Fixed immutable inputs

| Package | Source PR | Exact frozen SHA | Independent QA |
|---|---|---|---|
| SQUAD-PRESSURE-001 v1.0.0 | #429 | `4d13f76e6c0c08033e305900daf61c0a7267a725` | #434 and #465 |
| DEBRIS-REPULSE-001 v1.0.0 | #421 | `cb2b3a76e84266e2730fffc4ab048c1039f0652f` | #443 and #452 |
| Codex R3 receiver (read only) | #383 | `203a0805251ed28293888e43f2d49f0df63767c6` | dependency pin |

The CI checks producer HEADs **and all owner changed paths** relative to their individual bases. Squad additionally compares seven actual Git objects against the producer's manifest and exact Codex R3 blob. Debris uses the **producer's actual original `package.py`** to generate, verify, externally stage and rollback its original physical 14-entry inner ZIP; compares the entire binary against known SHA-256 `e98ee8bb7a428f954290633240e8abb2434a566d46be17a6b96ae9c11aa473b2`.

Checks include tracked Unity GUID uniqueness and collision against pinned Codex receiver, five Debris original `.meta` files, real `asmdef` semantics, producer source/archive/member SHA-256 consistency and external stage/rollback. For Squad, the receiver also emits a **new receiver-owned physical source ZIP**, with seven source files, original producer manifest, per-file Git/blob+SHA256 manifest, and tests the external staging including refusing changed and foreign members before rollback.

## Re-run from four separate immutable checkouts

```bash
python gate/Tools/B01/B01-40/P17W09/ReceiverG0/gate.py \
  --squad squad --debris debris --receiver receiver --out dist
```

The script expects four siblings under the working directory: `gate` (this PR), `squad` (pinned #429), `debris` (pinned #421), `receiver` (pinned #383). CI checks out all four exact commits and runs the actual script **twice**, on Windows and Ubuntu independently, uploads receipts and real binary ZIPs. The script creates temporary external staging only; no source file overwrite or code merge.

## Gate classification and limitations

- **G0 SOURCE** = immutable producer Git pins, scope, GUID/asmdef and genuine physical source archive accepted if the **exact final HEAD** dual-OS workflow succeeds.
- **G1 NATIVE UNITY** = NOT_RUN; requires licensed Unity 6000.3.14f1 on authorized machine, native compilation, real GameObject/PhysX EditMode and PlayMode. Stubbed UnityEngine/.NET C# source tests do not count as G1.
- **G2 BUILD/INTEGRATION** = HOLD. No Codex ACK, no runtime adapter, no merge.
- Squad has original C# source + single `.cs.meta`, but **no producer Unity folder `.meta` or asmdef**: accepted source material is not a drop-in finished Unity package. Codex must own the Unity import boundary; no pretend assembly acceptance.
- Debris has Unity source/asmdef/GUIDs but real Rigidbody physics/native build is still NOT_RUN.
- HAZARDPULSE #430 is **NOT ACCEPTED**: independent #444 found the original real 10×0.1 vs 4×0.25 fractional timestep RED. Only B01-25 can issue a **new version/SHA** and B01-60 can retest it.
- Receiver handoff: B01-40 G0 findings -> B01-60 native G1 -> B01-00 queue decision #79 -> Codex explicit ACK -> integration. Until then all packages remain not integrated.

## Rollback

The G0 procedure changes only this dedicated QA branch and external temporary files/artifacts. To abandon, close the Draft PR and remove its unique `Tools/B01/B01-40/P17W09/ReceiverG0/` path and dedicated workflow. Never remove/fix any frozen #429 or #421 producer file. Optional future Codex integration must import to a separate authorized receiver branch with prior backup and exclusively revert Codex-approved new adapter/asset additions.
