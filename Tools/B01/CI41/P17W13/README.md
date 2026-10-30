# CI41-ACTION-EVIDENCE-001/R1 — B01-41 / PASS17 WAVE13

[HQ W13 #535](https://github.com/C8LyNn/RoboRougelike/issues/535) · [Ownership #276](https://github.com/C8LyNn/RoboRougelike/issues/276) · selected immutable [Secure V2 #510@d94400d95d13db7f27e1533df05c9292d5380431](https://github.com/C8LyNn/RoboRougelike/pull/510).

## New executable function: original GitHub Actions *run/job/step* evidence attestation

`action_evidence.py` reads actual **live GitHub REST workflow run + all jobs/steps** for four original historical immutable GitHub Actions runs:

- Producer #510 actual source-only C# and ZIP workflow, run `41069853535`, SHA `d94400d95d13db7f27e1533df05c9292d5380431`;
- B01-60 native **readiness ZIP, not native Unity** #532, run `41074147973`, SHA `5778023163d8b51d23abfebaa17119e21bd5e131`;
- Original CI-02 EditMode run `41073443777`, exact SHA `fca69ec1b51978ebb8e84871826331ebe2bfc5da`;
- Original CI-03 PlayMode run `41073443940`, same exact SHA.

The executable validates **real** run IDs, repository, workflow identities, run attempts, immutable head SHA, full job count, every job SHA and attempt, actual step names, genuine `success` or `skipped` conclusions. A source-only success is classified `SOURCE_ONLY_EXECUTED / NATIVE_G1_NOT_RUN` (regardless of how "native" the workflow's name sounds). A Unity activation-credential failure with skipped Unity runner and skipped NUnit result check is classified `NATIVE_BLOCKED_ACTIVATION / NATIVE_G1_NOT_RUN`. It is **impossible for this V1 tool to mark native G1 PASS**; original XML and Editor.log on a licensed host with independent B01-60 review are required separately. The value is hard, auditable provenance and prevention of fake G1 promotion. It is not another Unity runner (#532) or receiver ZIP / migration checker (#521).

**Save/Load context:** the selected #510 source has no accepted durable detached/consumed-ID persistence contract according to [B01-62 #527](https://github.com/C8LyNn/RoboRougelike/pull/527). Report retains `save_load_persistence=HOLD_UNTIL_INDEPENDENT_ADAPTER_ACCEPTANCE`, even when source tests succeed. This helper does not implement save serialization, transaction fix or rollback and cannot claim their acceptance. B01-23/62 own implementation; B01-40/62 own safe rollback; B01-61 independent QA. **#511/#494 alternative HOLD, no mixed source.**

## Actual reproduction
```bash
python3 Tools/B01/CI41/P17W13/action_evidence.py --case native-532-readiness --capture-out /tmp/real-run532.json --out /tmp/receipt532.json
python3 Tools/B01/CI41/P17W13/action_evidence.py --case native-532-readiness --from-capture /tmp/real-run532.json
```

Dual-OS dedicated source CI downloads all four real original run/job metadata documents from GitHub API, then performs **35 independent positive/negative tests** by mutating copies of the *real original* captures. No fake "native passed" fixture counts. The raw captures, JSON classifications, full unit stdout are uploaded as evidence. A token can be provided via `GITHUB_TOKEN` with `actions:read`, but values are never logged. Calls are restricted to hardcoded repository + pinned workflow run IDs.

## Ownership, acceptance and rollback
Only `Tools/B01/CI41/P17W13/**` and `.github/workflows/b01-41-p17w13-action-evidence.yml` on independent Draft branch; **zero Assets/**, no Codex gameplay, no changes to #510, #532, prior #522 or main/develop. GitHub CI is source-only; it does not start Unity. Rollback: leave Draft unmerged / remove only own paths (or discard locally generated receipts). No game install, no package freeze rewrite, no cherry-pick or merge.

Receiver B01-40 formal source G0 / B01-60 native Unity G1 and real XML+Editor.log / B01-23/62 SaveLoad, B01-61 independent transaction/ID QA / B01-00 only handles #79. Status `CI41_PROVENANCE_PASS` is **not** game `SOURCE_G0`, `NATIVE_G1`, `SAVELOAD_ACCEPTED`, `CODEX_OFFER`, `ACK` or `INTEGRATED`.
