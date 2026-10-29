# CI41-NATIVE-TRIAGE-001 / B01-41 / PASS17 WAVE03

**Owner:** B01-41 (CI/CD & Test Automation); **purpose:** consume ORIGINAL Unity-native NUnit XML plus process receipts in the Git history of Codex PR #383, preserve failures even if isolated retest is green, and produce actionable JSON with exact failing test names, error messages, first stack frames, skipped-policy cases, and original source identity.

**This is real executable code and cross-platform automated testing but is NOT an invocation of Unity.** The underlying XML was produced earlier by Codex's native Unity 6000.3.14f1 runner; its original Git bytes are re-read, not regenerated here. Original full-suite failures remain blockers.

## Original source input, boundaries and real findings

- Original **native gameplay commit:** `203a0805251ed28293888e43f2d49f0df63767c6`
- Original **report-only commit containing NUnit + process logs:** `f92c873bb1e397a598b133e107ae704e15f86768`, Draft PR #383 (stacked on #382/#310). Required genuine ancestor relation.
- Four original evidence pairs under `Docs/P17Reload/Evidence/`: `EditMode.xml` + `EditMode-process.json`; `PlayMode.xml` + `PlayMode-process.json`; `Targeted-EditMode.xml` + `Targeted-EditMode-process.json`; `Failure-retest.xml` + `Failure-retest-process.json`.
- **Original full EditMode FAIL:** 1330 total, 1327 passed, 2 failed, 1 policy-conditional skipped, source-process exit **2**.
- Actual full failures:
  - `Camera275NativeTests.ScrollZoomIsSmoothAndRemainsAnchoredToVehicle` — first-step zoom 1.00205421 vs expected under 1 m.
  - `MapgenRC1IntegrationTests.MeasuredHeavySpawnsOnGeneratedFloorWithCanonicalEnemies` — grounded wheel sample 0 vs expected greater than 0.
- **Original full PlayMode PASS:** 474/474, process exit **0**.
- **Original targeted EditMode PASS:** 20/20, process exit **0**.
- **Original filtered failure retest PASS:** 2/2, process exit **0**, both formerly failing cases are present. Filtered retest does **not** cancel the failed full suite; specific reasons (order, timing, setup) require independent investigation.
- **Final classification:** `HOLD_FULL_NATIVE_REGRESSION`, `g1_assessment=HOLD`. A source-only audit cannot award native G1 PASS, even when all XML appears green.

## Reproduction on actual immutable Codex commits

```bash
git fetch --no-tags --depth=250 origin '+refs/pull/383/head:refs/remotes/origin/ci41-w03-codex'
python Tools/B01/CI41/P17W03/native_receipt_triage.py \
  --repo . \
  --evidence-commit f92c873bb1e397a598b133e107ae704e15f86768 \
  --gameplay-sha 203a0805251ed28293888e43f2d49f0df63767c6 \
  > codex-native-triage.json
# Expected CLI exit 2: genuine failed full EditMode (do not override).
```

```bash
python -m unittest discover -s Tools/B01/CI41/P17W03 -p test_native_receipt_triage.py -v
```

Source-only GitHub Actions [#40900659128](https://github.com/C8LyNn/RoboRougelike/actions/runs/40900659128) runs **12 tests per OS** with original real NUnit XML plus adversarial negative XML/process fixtures. An original output JSON is saved independently for Windows and Ubuntu. The workflow treats CLI exit `2` **as expected for the known non-green native evidence**, while requiring that the JSON remains `HOLD_FULL_NATIVE_REGRESSION`; it must never silently convert the failed full suite to green. The branch-owned workflow is **not** a canonical required check.

## Receiver contract, QA, import and rollback

The package is independent of frozen earlier B01-41 packages: [CI41-UNITYREF-001 #345](https://github.com/C8LyNn/RoboRougelike/pull/345), [CI41-ASSETIMPACT-001 #352](https://github.com/C8LyNn/RoboRougelike/pull/352) and [CI41-PATHPORT-001 #378](https://github.com/C8LyNn/RoboRougelike/pull/378). Their source blobs were not modified. Only own paths `Tools/B01/CI41/P17W03/**` and `.github/workflows/b01-41-w03-native-receipt-triage.yml` are changed; no Unity game, CHL/PARTSIZE, or global CI edits.

**B01-40** audits original source/receipt SHA and offers frozen package; **B01-60/61** independently verify findings or run fresh native Unity when authorized; **B01-00** schedules possible CODEX INBOX #79 intake. Codex owns camera/MAPGEN gameplay, native reruns and any integration; merely reporting a failure here is NOT a Codex ACK. **Do not merge** this Draft PR without explicit HQ approval.

**Rollback:** close the unmerged Draft or remove optional Python/workflow after authorized adoption. No project import, GUID, save, game state, or data migration.

**Limitations:** Original Git provenance checks SHA/ancestry, source references, 4 pairs of process flags + XML, exit statuses and exact test-case counters. It cannot independently prove a particular local Unity process really produced those bytes, validate execution environment, recover missing/forged external logs, or infer a root cause of the two failures. Full system trust requires authenticated CI event/runner logs and independent native QA. This tool never supplies Unity/Player native PASS.
