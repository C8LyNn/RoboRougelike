# B01-10 — RUN-SAFETY-001 / S04 independent design & balance review R1

**TASK:** `RS-S04-DESIGN-001` | **Role:** B01-10 Game Design & Balance | **Priority:** P0 design/policy gate  
**Reviewed producer:** [Draft PR #154](https://github.com/C8LyNn/RoboRougelike/pull/154) at immutable `b2623f50e2bdb16106b88d0107d215867ed8791b`.  
**Authority:** [RUN-SAFETY #152](https://github.com/C8LyNn/RoboRougelike/issues/152), [role assignment #155](https://github.com/C8LyNn/RoboRougelike/issues/155), [ECON canonical #136](https://github.com/C8LyNn/RoboRougelike/issues/136).  
**Decision state:** `POLICY_C_RECOMMENDED` / **NOT HQ APPROVED** / `G2 HOLD`. No existing Unity files or Codex worktree modified.

## 1. Findings: exact source, not guessed gameplay outcomes

| ID | Priority | Observed source behavior | Reproduction / expected product policy | Consequence / ownership |
| --- | --- | --- | --- | --- |
| RS-DES-01 | **P0 conditional on future policy C integration** | `SafetyVehicleSnapshot.Evaluate` checks only functional boolean flags after canonical CanDeploy. It does **not** inspect genuine owned HP=0 root cause or the feasibility of alternatives. | Fixture `SOURCE-01` and `SOURCE-02`: `canDeploy=false`, gun unavailable, while `ownedParts=[]` or healthy gun still owned. Source-equivalent port returns `LoanerOffered`; proposed acceptance `EL-02` and `EL-03` => `NO_QUALIFYING_DESTROYED_OWNED_PART`. | Player could intentionally remove/sell a working gun and request free temporary vehicle if host forwards such a snapshot. Neither Unity exploit nor real reward farming is proven; B01-20/Codex adapter should fail closed before hosting.
| RS-DES-02 | **P0 conditional on policy C** | Snapshot has `NextChallengeAvailable` bool but no activity-type compatibility or physical loaner viability. | `SOURCE-03`: unsupported activity still `LoanerOffered`. `EL-05` rejects `LOANER_INCOMPATIBLE_WITH_ACTIVITY`; `EL-06` refuses uncertified loaner. | Physically impossible attempt risk; B01-00 must authorize which encounter types are loaner-compatible and demand real R3/R6 proof.
| RS-DES-03 | **P1** | Existing snapshot treats missing weapon as launch blocker even when a noncombat activity could allow weaponless vehicle. | `SOURCE-04` suggests loaner for weaponless objective. `EL-16` => `WEAPONLESS_ACTIVITY_REQUIRES_HOST_LAUNCH_POLICY` rather than forcing gun loaner. | Activity-specific deployment belongs to canonical run controller, not an assumed bypass.
| RS-DES-04 | **P0 future integration** | `RecordOutcome(encounterId, victory)` trusts a caller-supplied `victory` boolean; matches active ID, but source alone cannot prove R1 issued the win. | `ECO-07` explicitly rejects untrusted event. Host adapter **must** bind to one canonical R1 event with stable event identity and immutable outcome; reject stale/spoofed call. | Replay/rebuild/reward authority must remain R1/ECON, not a second new ledger.
| RS-DES-05 | **P1 measured/native gap** | No real R3/R6 temporary loaner, native G1 or actual sustainable encounter success evidence; demo merely uses a Rigidbody primitive. | 100 fixed seeds × easy/normal/hard × required activity classes; check controllable left/right drive, working weapon when necessary, actual finish, falls, reward IDs, owned inventory untouched. | `PHYSICAL_VIABILITY_NOT_RUN` until B01-60/Codex test; do not claim softlock solution complete.

**Method:** direct GitHub source inspection; independent JS reproduction of the `Evaluate()` boolean decision, plus a **separate proposed policy oracle** and independent model of ECON ledger invariants. `SOURCE-*` are source-equivalent *port* observations, not a Unity invocation. This does not establish that the real user can gain free rewards in the unintegrated prototype.

## 2. Decision matrix for HQ approval

| Option | Deadlock without operational owned gun/drive and zero Scrap | Canonical one-real-win rebuild delay | Balance and abuse | Integration burden | B01-10 position |
| --- | --- | --- | --- | --- | --- |
| **A** Immediate emergency repair | Can supply working vehicle, but before win | **Fails:** heals 0 HP before required win | Repeated restoration can erase damage costs and softlock stakes | Low, but violates accepted economy | **REJECT** as ongoing rule |
| **B** Special service challenge / puzzle without rover | Could work if separate playable activity exists | Only if HQ defines service completion as real winning challenge | New easiest-stage farming, reward abuse, detaches player from robot-building USP | High: new challenge, UI, scoring, replay guard | **DEFER** pending owner design |
| **C** One eligible real encounter using temporary underpowered **leased rover** | Conditional yes | **Preserves**: permanent player part stays 0 HP until real win | Need eligibility checks, finite canonical challenge progression, no loaner part sales or special extra payouts | Medium: isolated actor, R1 event and ECON adapter | **RECOMMEND FOR HQ APPROVAL**, not yet approved |

Recommend variant C **only if** a next canonical encounter exists and a weak loaner is physically capable of completing that encounter. Do **not** introduce a blanket one-loaner-per-cycle limit: that could recreate the very softlock if several different essential items need successive victories and player has zero Scrap. Prevent farming through proof of a real destroyed essential item, canonical encounter identity, unrepeatable rewards, and no leasing from an otherwise recoverable healthy build. Defeat remains canonical R1 outcome and can end run; loaner should not protect the run from legitimate loss.

**Open for B01-00/HQ (do not self-approve):** (1) select C over A/B; (2) whether canonical full encounter rewards are retained (recommend normal payout once so paid rebuild stays affordable); (3) which activity types and weaponless rules may use loaner; (4) native viability threshold, eligible difficulty envelope and optional fallback for incompatible encounters; (5) whether a 0 HP *queued* essential part is necessary, or merely owned 0 HP is sufficient (recommend owned destroyed eligible is sufficient, since player might have used free rebuild slot on another essential item).

## 3. Precise proposed eligibility contract (subject to HQ approval)

1. R1 active, garage phase, genuine next canonical encounter, living authorized Core and no unrelated start guard. Do not revive Core or mutate run loss.
2. `CanonicalCanDeploy == false` due to **verified actual** lack of structure or functional weapon for this activity, or functional two-sided drive; **not** due to merely absent optional equipment, deliberately unequipped healthy kit, or arbitrary GUI warning.
3. R3 authoritative owner state must identify at least one destroyed (0 HP) **owned** component of the essential missing functional role and prove no immediately deployable healthy owned alternative in build/inventory. Do not accept decorative/present-but-disconnected as functional.
4. Loaner is certified for the **next actual** challenge's modes/objectives and functional requirements. Weaponless modes need independent activity-specific host start policy; no forced weapon, no fake `CanDeploy=true`.
5. Lease is one temporary physical actor for **one distinct real encounter**, ID linked to canonical R1 encounter; not inventory/salvage/shop/blueprint. Owner original parts retain IDs and HP, queue/pending cannot mount or be sold/duplicated.
6. One free 0HP rebuild order per real-victory cycle; further orders at canonical ECON quote strictly less than purchase; valid purchase=1/quote=0 remains allowed, but second zero-priced paid quote reports PAID not FREE. One **next real win** completes previously queued parts (not automatically mounts). A defeat/garage reentry/seed change/reload never shortens it.
7. Only canonical R1 victory callback with matching stable encounter ID may increment serial; canonical reward/economy settlement once by reward event ID; defeat never grants progress, fake event ignored. External run ending terminates opportunity without changing R1 rules.
8. When info is missing/untrusted (no save HP evidence, mismatched graph/owner, unknown challenge physics) => `BLOCKED`, never offer; explanatory HUD must name the actual blocker and recommend repair/switching equipment.

## 4. Executable checks, quantification and QA gating

**Portable automated tests:** `node run.mjs` runs **48 scenario fixtures** (24 eligibility, four producer source-equivalent gate counterexamples, 15 ECON/ownership ledger transitions, five option-policy tests); **3 mutation controls**; **2,000 fixed-seed stochastic ECON sequences** asserting single free cycle, purchase>quote including 1→0, no duplicate event settlement, no instant rebuild on defeat, unchanged identity, no loaner items entering owned inventory. Proof is about the modeled contract **not** production C#/Unity. Raw actual result in `EVIDENCE.json`.

**Native acceptance for B01-60 after real source repair:** (A) 0HP last gun, both-side mobility lost, broken bridge, no Scrap; (B) 0HP Core/run ended, unrelated start guard, no next challenge, no real defeated parts; (C) spare healthy gun in stock but unequipped (must not receive free lease unless truly irrecoverable); (D) one noncombat race/escape activity requiring drive but no gun, without rewriting normal host rules; (E) duplicate begin/end/reward/save/restart and two successive different win cycles; (F) sandbox temporary rover cannot be sold/salvaged into inventory; (G) 720/1080 URP readability, HUD warning, ability to steer/fire, real R3/R6 success and exact PHY target.

**Measured balance, not arbitrary declarations:** 100 seeds per eligible activity+difficulty+rig, 0/1/2/3 owned essential failures, loaner vs normal starting vehicle with matched player skill/controller, time-to-finish, lost encounters, genuine gun availability, drive contacts, fall/roll, actual paid rebuild affordability, repeat rewards and identity count. Do not certify a loaner viable from a cube on floor. Never claim Unity PASS without test XML, logs, Player captures and exact SHA.

## 5. Producer/recipient handoff

- **B01-00/HQ:** approve or reject policy C and open decisions before assigning source fixes; source #154 remains Draft. Producer has no G2.
- **B01-20 (only after exclusive slot/owner approval):** retain isolated `Assets/B01Packages/RUNSAFETY001/**` source ownership if assigned; provide `TryOfferLoaner(SafetyVehicleSnapshot, CanonicalOwnedParts, ActivityRules)` fail-closed intake adapter in separate package revision, canonical stable event bind, no direct live R1/R3 overrides. Import `EL-01..24`, `SOURCE-01..04` negative fixtures as receiver NUnit. No main/active Codex writes.
- **B01-22/ECON:** canonical one-win paid/free queue, loss/repair identity, reward event authority; avoid shadow inventory, preserve 0HP and no immediate EmergencyRepair restoration while queue enabled.
- **B01-30:** loaner visual/HUD critical warnings: loaned temporary, why allowed, 0/1 wins remaining, rebuilding unmountable, no-payout policy invention, color accessible and original URP.
- **B01-60:** independent Unity 6000.3.14f1 negative matrices/PlayMode, real physics and exploit tests; publish XML/log/seed/player proof.
- **B01-40:** provenance, independent receiver/GUID hashes/target scope; never mark G2 without the three approvals.
- **Codex:** only afterward B01-00/G2 authorized integration against exact current R1/R3/ECON and genuine Windows QA; no direct source modification by B01-10.

**Final design verdict:** `POLICY_C_RECOMMENDED_NOT_APPROVED / SOURCE_RISK_FUTURE_INTEGRATION / CONTRACT_TESTS_PASS / NATIVE_UNITY_NOT_RUN / G2_HOLD / NO_MERGE`.
