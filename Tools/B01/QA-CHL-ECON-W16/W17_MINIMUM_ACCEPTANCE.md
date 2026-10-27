# B01-22 / W17 — CHL-003 minimum no-reward / no-saved-damage acceptance

**Purpose:** one small, reproducible receiving checklist; not a new Unity package, not a reward implementation. **Owner:** B01-20 exclusive CHL-003, Codex sole R1/R3 and any subsequent authorized adapter; B01-22 independent source QA only. Governance [#276](https://github.com/C8LyNn/RoboRougelike/issues/276), W17 [HQ dispatch](https://github.com/C8LyNn/RoboRougelike/issues/71#issuecomment-6438657467). **G2 HOLD, NO MERGE.**

## 1. Original exact source verified (not merely producer prose)

- Latest **observed CHL PR #285 HEAD** at W17 dispatch: `30c0355a62764eb37798347612564ee51f3b36e1`.
- Prior independently **actually executed** CHL pin: `23d40eb21a7c0db97238516c9e9ec6e708f54998`. GitHub `compare_commits` `23d40eb...30c035...`: ten changed files, **all** in `Docs/B01/CHL003/**` or `Tools/B01/CHL003/source_gate.py`; **zero** `Assets/RoboRougelikeChallenges/CHL003/**` differences. Thus **all 16 pinned, Git-blob-verified original CHL Assets** (12 C#, 3 asmdef, 1 scene) remain byte-identical in observed W17 HEAD. This evidence is limited to that exact HEAD; verify fresh comparison again before native reception if producer moves.
- Original frozen Codex R1/R3 host sources used for source interface comparison: integrated host `88b8eed60345c49481648833e1fde6dc1a400258`; `R1RunController.cs` Git blob `c6d5f3efe4949da68a5c48ec38ef5028af8b8410`, `R3VehicleModel.cs` Git blob `cdd75fff4923a382b16c430dac1452320eda88f6`. **This is a pinned historical baseline**, not a claim that Codex's live current workspace has those exact sources.

### Source-level responsibility boundary

- CHL `CourseCore` emits `Completed(float ElapsedSeconds)` after moving to Completed and retains delegates across Retry (actual original [source lines 23–25, 63–72, 104–117](https://github.com/C8LyNn/RoboRougelike/blob/23d40eb21a7c0db97238516c9e9ec6e708f54998/Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003CourseCore.cs)). **Per-attempt success callback ≠ durable entitlement**. A second valid completion after Retry fires a second notification. That is intentional game-result behavior, not an existing standalone wallet exploit.
- CHL `CourseController.Retry()` resets **configured Rigidbody** velocities and world position/rotation; `SetPaused` toggles `isKinematic` ([original controller lines 96–140](https://github.com/C8LyNn/RoboRougelike/blob/23d40eb21a7c0db97238516c9e9ec6e708f54998/Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003CourseController.cs)). CHL demo `SelectRig` destroys its synthetic vehicle GameObject ([lines 160–172](https://github.com/C8LyNn/RoboRougelike/blob/23d40eb21a7c0db97238516c9e9ec6e708f54998/Assets/RoboRougelikeChallenges/CHL003/Demo/CHL003DemoBootstrap.cs)). **Never pass a live game-owned R3 actor directly into CHL demo**. Use a disposable rig or independently prove snapshot/restoration.
- CHL runtime `B01.CHL003.Runtime.asmdef` has `"references": []` ([source](https://github.com/C8LyNn/RoboRougelike/blob/23d40eb21a7c0db97238516c9e9ec6e708f54998/Assets/RoboRougelikeChallenges/CHL003/Runtime/B01.CHL003.Runtime.asmdef)); independent scan of the 12 original C# files detected **no direct calls/writes** to R3 Scrap, module HP, inventory, buy/repair, Reward/Salvage or authoritative game saves. This is static evidence only; indirect future host subscriptions and real persistent state are not covered by the scanner.
- Historical R1 `Update` has one `VehicleModel.Award(LastScrapReward)` after its Combat/loss-first check, and moves to Salvage ([lines 242–273](https://github.com/C8LyNn/RoboRougelike/blob/88b8eed60345c49481648833e1fde6dc1a400258/Assets/R1/R1RunController.cs)). CHL currently **does not feed this authority**. An eventual bridge must not assume `Completed` grants permission to invoke `Award` or `WinEncounter`.

## 2. Minimum executable acceptance — independent receiver, both rigs

**Snapshot before launching challenge:** `S = {R3.Scrap, inventory(sorted physical part GUID + kind + HP + upgrades + metadata), mounted(sorted GUID+cell+rotation+HP), saved build canonical bytes/SHA256, reward ledger or equivalent}`. Capture from **real authoritative R3**, not from a fake `Dictionary` or detached fixture. Repeat snapshots after finish, Retry, Cancel and reload. For standalone CHL all economic/persistent fields must be **byte- or semantic-exactly identical** to S (no new money, no lost part, no 0HP repair, no duplicate GUID, no new saved design). Logs written by Unity `Debug.Log` to Editor/Player.log are permitted diagnostics, **not** garage saves. Physics position/velocity of an isolated **demo-only rig** may reset.

| ID | Exact negative / positive sequence | Expected persistent outcome |
|---|---|---|
| A1 | Light: physically cross 4 ordered gates and finish; Heavy: same course. Each rig `3` separate complete trials | No delta in R3 wallet, physical inventory, HP, saved build, rewards. `Completed` one per successful attempt, **not payout** |
| A2 | Wrong Rigidbody, early finish, duplicated gate from multi-collider, finish overlap only in same FixedUpdate without retained contact | No earned reward; no saved-state delta; no invalid success |
| A3 | Fail by falling, timeout, Cancel; Retry×3 and eventually complete a valid later attempt | Fail/Retry/Cancel never grant; successful per-trial callbacks may repeat, **but not economy mutation** |
| A4 | Pause during motion; resume then Retry. Inspect that pause/resume affects only the disposable actor | R3 persistent HP and builds unchanged; actual garage-owned Rigidbody untouched |
| A5 | Switch Light↔Heavy and leave/reenter scene; verify destroy/reconfigure hooks | Live R3 mounted parts and serialized design still exist unchanged; no orphaned listener |
| A6 | Intentionally subscribe receiver twice and inject exception after `Completed` | Two notifications possible; **zero standalone rewards**; future Codex entitlement settlement once-only even when handler throws |
| A7 | After a completed trial, close/reopen activity, simulate interrupted save, replay same result | Standalone no payout; **future authorized rewards** require durable unique activity+award key, atomic R3/save commit, no double grant |

**Host settlement must default to disabled** for CHL standalone. Only Codex under separate B01-00 authorization may implement reward entitlement, validate actor/activity identity, and use unique persisted transaction IDs with atomic receiver ownership. **This checklist does not decide repeatability of rewards or add a ledger.**

## 3. Evidence ledger and G1 operator steps

**Source QA executed:** [PR #283 actual Windows+Ubuntu run #40483027671](https://github.com/C8LyNn/RoboRougelike/actions/runs/40483027671) on independent QA SHA `dbdb8564a7964ab6da6784e7399ab8940809d784`: 16 CHL + 2 host original Git blobs verified, 12/12 genuine CHL pure-C# tests (including 512 seeded replay trials) + 28/28 Python source/mutation tests **PASS on each OS**. Historical detached witness is NOT a substitute for real R3 snapshot.

**Native Unity compile, EditMode, PlayMode, contact physics and real R3 saves = NOT_RUN.** Unity CI-02/03 credential preflight failure before engine execution is not a Unity failure or Unity PASS.

**One next owner action:** B01-20 provides current source HEAD with disposable Light+Heavy scene and exact logs; B01-60, in a licensed isolated Unity 6 checkout together with Codex-designated authoritative R3, executes A1–A7 and provides before/after state hashes, per-run XML/Editor.log, actual test exit codes, collision and ordered crossing telemetry. **B01-00 may advance G2 only after independent review**. B01-22 never changes Codex/producer game sources.
