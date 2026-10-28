# B01-24-TACTICS-001 / R0.1 — isolated, working AI decision prototype

**Owner:** B01-24. **Integration owner:** Codex ONLY. **Source baseline:** Codex PR #310 @ `561c223f7f01c1e609548bf1c8c66b5132ec5485`, original `P12_PipelineSafeEnemyTurret.cs` blob `61995b65c4def25e93fa2ad17c09f1bbd7c001ff`. No production `Assets/**` changes, CHL/PART modifications, integration or merge.

## Run

From repository root:

```sh
node --test Research/B01-24/P16/CombatTactics/tactics.test.mjs
```

Node 18+ and built-in modules only. See [manifest.json](manifest.json) for pinned original Git blobs and evidence. The code is an executable, deterministic JavaScript tactical decision function, **not an implemented Unity C# AI**, and **not** a substitute for native PhysX validation.

## Contract and behavior

`decide({self:{position:{x,z},radius,drive,weapons},target:{position:{x,z}},allies:[{position,radius}],obstacles:[{minX,maxX,minZ,maxZ}],config})`

Returns `mode`, `movement {x,z}`, `step`, `choice`, `blocked`, `fireAllowed`, `fireReason`, `worldCover`, `blockedByAlly`.

- Normal weapon+drive: range-aware approach or hold. All weapon hardware lost but mobile: propose `RAM_APPROACH`. Drive destroyed but one weapon live: `STATIONARY`, permits advisory fire if safe. Both lost: `DISABLED`.
- Deterministic local steering uses **swept planar AABB and ally-disc segment checks** for each candidate, not a fake turn-only no-clip rule. If every candidate is blocked, STOP instead of penetrating.
- Ally between muzzle/target blocks advisory fire; an ally behind target does not. World cover is *observed only*: HQ has **not approved** suppressing enemy direct shots behind static geometry, and this prototype does not silently decide that policy.
- The decision output never spawns shells, changes a Rigidbody or applies damage. It cannot guarantee route completion, vertical clearance, live hull contacts, actual collision or proper damage tuning.

## Actual checks and integration steps

**Executed on exact GitHub source/test Git blobs under V8:** 18/18 assertions PASS (active, armament destroyed, drive destroyed, both destroyed, allies, obstacles, deterministic and negative inputs). The separately prepared local Node prototype also ran, but **the exact published Git files have not yet been run by Node CI**; do not relabel V8 execution as Node's `node --test` or Unity PASS.

**Codex acceptance path:** read-only review by B01-40 and B01-60; create a Codex-owned adapter only upon owner acceptance. Extract real live `R6VehicleDrive` mobility fraction and mounted gun online state, true 3D collision/LOS, and approved ram rules; compare actual maneuvers and projectiles with proposed decision output in a disposable Unity scene. Preserve existing `P12_PipelineSafeEnemyTurret` authority and do not merge this research JS into Unity.

**Source freeze:** No immutable `OFFERED` package asserted by B01-24. B01-40 controls source-package intake/freeze; Codex must explicitly acknowledge before `CODEX_CLAIMED`.

**Codex parity:** Existing unarmed ramming is already implemented and native-tested on Codex commit `16c2668104952f1b8db304cc7817b897fb48a9b1` (12/12 targeted Unity PASS). Do not integrate this prototype's matching ramming branch; only evaluate distinct avoidance research. This branch is not installed in the game.
