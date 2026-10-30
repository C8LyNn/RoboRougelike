# CI41-SAVE-ARTIFACT-PARITY-001/R1 — B01-41 PASS17/WAVE14

[HQ issue #552](https://github.com/C8LyNn/RoboRougelike/issues/552) · [single-owner governance #276](https://github.com/C8LyNn/RoboRougelike/issues/276) · selected immutable [Secure V2 #510 @ d94400d95d13db7f27e1533df05c9292d5380431](https://github.com/C8LyNn/RoboRougelike/pull/510).

## One new executable CI function (distinct from W13 job-status metadata)
`save_artifact_parity.py` downloads ORIGINAL GitHub Actions binary artifact outer ZIPs for two **unselected alternative Save/Load contracts**, checks original exact run+artifact ID+head SHA, safely inspects actual nested producer source ZIPs (ZIP-slip, symlink/duplicate/special, oversize/bomb guards), verifies **pinned inner SHA256, exact byte count, member count, selected #510 source pin, all per-file hash and length manifests and no fake Unity/ACK/live-Save claims**, and compares Windows vs Ubuntu original byte-identical inner payloads.

Original source-only proposal [#544 @ e77529262b8f03c6e9f0e25b642038c351872044](https://github.com/C8LyNn/RoboRougelike/pull/544): original run #41082219558, inner physical ZIP **41,644 bytes SHA256 3b7a4e8524732faafe0b770e557fb594af7a03997fc7d014e7d22ab99fce50dd**, 8 members; artifacts Windows #12989423688 / Ubuntu #12990211693. Its source archive parity is **not independent security acceptance** and does not choose this contract.

Original source-only proposal [#549 @ 6da2c523e03766cd5a6deaed0cb3cccf8765354b](https://github.com/C8LyNn/RoboRougelike/pull/549): original run #41082728707, inner physical ZIP **55,682 bytes SHA256 97656f6cb2a08820f8682079c9b896cd58ca9df0f94150ae9bdbf8e4b6a13eee**, 13 members; Windows #12991140072 / Ubuntu #12989419205. **Independent original-source QA #550** [run #41082794585](https://github.com/C8LyNn/RoboRougelike/actions/runs/41082794585), original Windows artifact #12989159727, genuinely demonstrates **mutable already validated Parts array and externally mutable original W06 ledger**, marked `SAVE_CONTRACT_RELEASE_GATE_RED=MUTABLE_VALIDATED_SNAPSHOT_AND_EXPOSED_LEDGER`. Our tool reads that original QA stdout **from original artifact bytes** and requires the RED marker for release HOLD; the QA-run's green exit is **not a security acceptance**.

No source-code/serializer patch here: B01-23/62 select ONE contract and version a correction of QA RED; B01-61 tests corrected exact SHA; B01-40/62 fix real receiver rollback; B01-60 original #532 real licensed Unity G1 or NOT_RUN. This gate supplies independent **physical archival evidence** only. Selected game #510 remains FROZEN. #511/#494 ALT HOLD. No candidate is selected by the CI41 artifact parity gate.

## Reproduce
```bash
GITHUB_TOKEN=<GitHub Actions read token> python3 Tools/B01/CI41/P17W14/save_artifact_parity.py --output /tmp/source-parity.json
python3 -m unittest discover -s Tools/B01/CI41/P17W14 -p test_save_artifact_parity.py -v
```
The script uses GitHub REST read-only API with source run/outer artifact IDs pinned; GitHub token is only sent to api.github.com. The signed artifact redirect must be HTTPS to supported trusted GitHub/Azure blob hosts and is downloaded **without** forwarding the token. Binary sources are checked in memory and never unpacked to disk. Local fixture unit tests (30 real in-memory physical ZIP/manifest/QA adversarial mutations) do not require network; CI performs both them and **real original artifact downloads on both OS**. Produces one read-only, machine-readable receipt as artifact.

## Ownership / rollback
Only `Tools/B01/CI41/P17W14/**` plus dedicated `.github/workflows/b01-41-p17w14-save-archive-parity.yml` on own isolated branch. No changes to Unity Assets, original producers, B01-40/60/61/62, frozen #510, Codex code, previous frozen #541, main/develop; no merge. Rollback: keep Draft unmerged / discard only own new paths and own generated JSON receipts.

Independent receiver: B01-23/62 selection + source fix; B01-61 corrected actual QA; B01-40/62 physical G0/migration+rollback; B01-60 licensed native Unity; B01-00 #79. `CI41_SOURCE_ARCHIVE_PARITY_PASS != SOURCE_G0 != SAVELOAD_ACCEPTED != UNITY_G1 != CODEX_ACK != INTEGRATED`. Release G2 HOLD.
