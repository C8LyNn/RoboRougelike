# B01-61 / PASS17 WAVE02 — DUALBREAK-001 R1

**Owner:** B01-61 CODEX REGRESSION QA. **Independent receiver** of B01-23 actual game-content [three specialist robot rigs, PR #348](https://github.com/C8LyNn/RoboRougelike/pull/348) and [38 one-owner weakpoint outcomes, PR #361](https://github.com/C8LyNn/RoboRougelike/pull/361). **NO B01-23/production source changes.** Unlike those two packages, this receiver tests **all 227 unordered simultaneous two-*physical-owner* losses** on Flanker Wheels, Bulwark Tracks, and Skirmisher Hover (14C2+14C2+10C2). Addresses multi-module fragility and accidental damage-induced orphaned guns/drive; no balance parameters are modified.

## Actual executable delivery

- [`dualbreak/Program.cs`](dualbreak/Program.cs): independently written C# graph-reachability/retained-gun/selected-drive analysis, **compiled together with the exact unchanged original** PARTSIZE `PartSizeContract.cs`, `PartSizeBaselineCatalog.cs`, and B01-23 `RigValidator.cs` from pinned original Git SHA. Checks the exact 38 original single-break committed outcomes before enumerating the NEW 227 double-break pairs. Rejects duplicate IDs, overlap and impossible resulting part counts. Each pair reports an explicit `nativePhysicalStatus: NOT_RUN`.
- [`run_dualbreak_qa.py`](run_dualbreak_qa.py): retrieves original exact-Git bytes of 7 producer files, fails closed if any immutable source blob differs, compiles/executes original C# (not a Python-only simulation), then **independently computes every pair by another Python BFS oracle**. Requires 227/227 exact parity including each detached real owner ID, functioning selected wheel/track/hover drive and number of still-connected guns. Injects four deliberately corrupt in-memory result fields (fake native PASS, fake owner, wrong gun count, wrong detached ID) that the independent verifier must all reject. Both C# and Python check owner duplication and logical cell uniqueness.
- [`manifest.json`](manifest.json): scope, exact original SHA, source Git blobs, limitations; dedicated [Windows+Ubuntu GitHub Actions workflow](../../../.github/workflows/b01-61-pass17-wave02-dualbreak.yml) writes **actual new data output** `dualbreak_output.json` and real terminal test logs as artifacts.
- **Output is an inspectable source-logical damage-risk QA dataset**, not a Unity-imported runtime asset. It does not add damage logic, weapon AI, save, balance or native colliders to Codex.

## Reproduction

Requires Git, Python 3.11+ stdlib and .NET 8 SDK. Run in a checkout of this QA PR:

```bash
git fetch --no-tags origin 0978497561c8b6400490235d3e5a401ac32bea2d
git fetch --no-tags origin 098ccdbe8f129253fa4ada52318436b7cc2b4dc4
git fetch --no-tags origin be2be0e4802bc3f450179901f9ae91c6d84bcb5b
python Tools/B01/B01-61-P17-W02/run_dualbreak_qa.py --repo . --output dualbreak_output.json
```

**Exit 0** means real original C# compilation and execution, 38/38 original single-source outcomes, 227/227 new original C# pair data + independent Python parity, 4/4 adversarial negatives and immutable pins. **Exit 2** means any mismatch or compile/missing source failure, never handoff GREEN. GitHub workflow matrix runs the exact command on Windows and Ubuntu and archives `dualbreak_output.json`/log, with source SHA-256 printed by the tool.

## Codex integration handoff

**Recommended no-conflict receiver:** Codex may **download the real generated pair dataset** from a pinned successful GitHub Actions artifact to guide robot build/test priorities and destruction-risk scenarios. It must first confirm the original #348 and #361 owner SHA, inspect the classification `SOURCE_LOGICAL_ONLY`, and independently recheck actual part damage/attachment on a disposable **licensed Unity 6000.3.14f1** host. For physical proof, use actual distinct part IDs/colliders, real detach actions and actual wheel/track/hover locomotion plus gun firing, without normalizing model loss to only a grid. Preserve original NUnit XML, Editor.log, Player outputs and exact game SHA. The `connectedGunCount` is *logical gun availability*, not measured damage/accuracy.

**Important limitation:** No claim of *sequential* loss simulation; these are simultaneous removals in a source-logical graph. If a part falls off after first impact, changing subsequent hit targets is a different gameplay experiment. Original R3 collider/typed sockets/PhysX, real Core destruction, brake/drive transitions, controlled GUI/human tests are all **NOT_RUN**. Do not mark G1/G2 green based on this .NET tool.

**Ownership:** only `Tools/B01/B01-61-P17-W02/**` and the dedicated own workflow. No modifications to `Assets/**`, PARTSIZE source, three original producer JSON, game Codex, main/develop, and no merge. **Rollback:** remove only the QA tool directory and its workflow; generated pair JSON is an optional artifact, not shipped game content.

**Receivers:** B01-40 validates source hashes/G0 and optional artifact provenance, B01-60 independently reruns and handles separately authorized native G1, B01-00 decides whether to forward to existing [CODEX INBOX #79](https://github.com/C8LyNn/RoboRougelike/issues/79). Any new original producer revision requires separate new pinned QA version; never rewrite this frozen v1.
