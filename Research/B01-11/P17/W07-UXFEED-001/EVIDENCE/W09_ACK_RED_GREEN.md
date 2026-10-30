# B01-11 / P17 W09 — ACKRED/GREEN, player-alert acknowledgment lifecycle

## Provenance and exact source commits
- Existing Draft PR [#438](https://github.com/C8LyNn/RoboRougelike/pull/438), historical R2 `68b90881bcd427c93e773bea4eb5b8370f3bcbfd` unchanged.
- **Test-first RED source untouched** commit `4b2c77092448ecb222592487ae134ec66ac396d2`: 15 original tests added against the old R2 source. `Ack` reflection adapter allowed old code to compile and return false if the new API did not exist.
- [Actual original-source RED GitHub Actions #41053157595](https://github.com/C8LyNn/RoboRougelike/actions/runs/41053157595) Ubuntu **100 PASS /10 FAIL**, Windows **100 PASS /10 FAIL**. These are **missing-feature** tests, not a pre-existing R2 bug. No existing R2 regression failed.
- New original C# source implementation commit `cce79c51c1f1b4088238215b937aeafc0a9cb627`: `TacticalFeed.AcknowledgeCritical(TacticalItem)`, operates on exact reference identity and live TTL; bounded alert queue only, returns boolean, never touches chronological history or game state.
- Final **direct, statically typed** original C# tests commit `a570c14bfe5da0f0178dbfd9a8aaea0743090c5e`: removes the temporary test-only reflection adapter, adds null and forged-ticket cases, total **112 tests each OS**.
- [GREEN original source GitHub Actions #41053373321](https://github.com/C8LyNn/RoboRougelike/actions/runs/41053373321): Ubuntu **112 PASS/0 FAIL**, Windows **112 PASS/0 FAIL**, compiled original source via .NET8, **224 original executable checks**. Final packaged R3 HEAD source CI must be checked separately.

## Functional acceptance
A user-acknowledged `PartDetached`/`WeaponOffline` alert hides from transient `Visible()` even while `History()` remains unchanged. A ticket is authentic by object identity; a new event for the same subject is a distinct object with new sequence. Old, expired, already-ACKed, fabricated, normal, or previous-run tickets cannot inadvertently dismiss a new alert. The API is pure stateful C# and intentionally has no game/UI bindings.

## QA ownership and limitations
B01-40 exact-SHA G0 independently pending; B01-60 compiled source QA independently pending; Codex acceptance and Unity adapter not authorized. Unity EditMode/PlayMode, Windows Player, actual UI input, controller keyboard, human accessibility tests **NOT_RUN**. B01-00 alone owns #79 handoff; no merge. Rollback same isolated source branch or Codex-owned adapter separately.
