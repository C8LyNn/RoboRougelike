# B01-61 / PASS17 WAVE10 — Original W09 public reattach API contract regression

**Package:** `B01-61-P17W10-REATTACH-PUBLIC-API-NEGATIVE-QA-001 v1.0.0`
**Owner:** B01-61 CODEX REGRESSION QA; **no B01-23 or Codex source changes**.
**Frozen target:** producer B01-23 #471 `763748f1be0c61356fb4462144640bb359364226` (new W09 files stacked on frozen #455), W07 #439 `cdcaea750fda1a9d7db6018e34970f4b9f206427`, W08 #455 `99dfbd25d471f9983f9472b12d3725f4c002f7e9`.

## Specific W10 negative integration contract (distinct from #472 / #476)

Older #472/#476 demonstrate W07 GUI invoking its unguarded W07 API when W08 is installed. New W09 #471 protects the *new demo GUI* but intentionally leaves the original `DebrisRigPlayground.Reattach(int)` public and unguarded. This QA targets specifically the **external public C# method call after W09 `HardenedDockDemo.Arm()` has disabled legacy GUIs**, rather than re-testing the old GUI. This is a distinct external-caller/bypass regression.

**Native fixture, source committed, native execution NOT_RUN:** `PublicReattachApiPlayModeTests.cs` instantiates real W07+W08+W09 MonoBehaviours and original Light/Heavy Rigidbody rigs on isolated test host; verifies protected `TryDock` rejects (1) physical non-default-layer BoxCollider at Light mount and (2) remote Heavy body; then an external caller invokes original `legacy.Reattach` directly. Both cases assert it must **return false without changing detached flag, Rigidbody or ID ledger**. These assertions should currently **FAIL** once licensed Unity6 PlayMode can actually run, exposing unsafe public method. It does not patch or weaken existing game behavior.

**Executed-source workflow, not a Unity substitute:** `check.py` validates THREE unchanged original producer Git blob bytes, compares actual W07 public mutating code against W08/W09 guarded APIs and proves specific bypass. Five negative/positive mutation-fixture unit tests test detection. A normal successful `--evidence` run means the known **blocker is reproducible**; `--release` deliberately exits **3** until the owner fixes the public route. Windows 2025 and Ubuntu 24.04 Actions preserve unit results, evidence logs, release status. No rerun of existing 47/49k/65k harnesses.

From repository root with Python 3.11 and Git:
```sh
python Tools/B01/B01-61/P17W10PublicReattachApiQA/check.py --mode unit
python Tools/B01/B01-61/P17W10PublicReattachApiQA/check.py --mode evidence
python Tools/B01/B01-61/P17W10PublicReattachApiQA/check.py --mode release
```
The third command must return 3 for the present immutable #471 and is a **HOLD** gate, not a CI harness malfunction.

**Precise repair owner:** B01-23 must publish a **new separate immutable source version** that makes all public/direct caller routes fail closed if there is blocked geometry, out-of-reach, invalid collision query or disabled gate; no change to immutable #439/#455/#471. Simply disabling MonoBehaviour/OnGUI is not sufficient. Integrating a genuinely guarded sole reattach API may require redesigning W07's public method or replacing it via Codex-authorized host adapter in a NEW version; preserve owner locks #276. Then independent B01-61/B01-60 should rerun both native tests on actual new SHA, including blocked and clear positive paths.

**Gates:** source audit only / negative contract reproduced on CI when completed. `G0_SOURCE_RELEASE=HOLD`; `G1_UNITY_EDITMODE/PLAYMODE/PHYSX=NOT_RUN`; `G2=HOLD`; Codex ACK=NOT_OBSERVED / NOT_INTEGRATED / NO_MERGE. Rollback = delete only the new isolated QA-owned paths and close this unmerged Draft PR. B01-40 receives manifest/asmdef/GUID and B01-60 native fixture. B01-00 controls any future Codex offer.

**QA source path scope:** `Tools/B01/B01-61/P17W10PublicReattachApiQA/**`, `Assets/B0161PublicReattachQA/**` and five new isolated .meta GUIDs, unique workflow only. No existing source overwritten.
