# B01-13 / PASS17 WAVE21 — REBUILD-CHOICE-REHEARSAL-001/R1

**Owner** B01-13 Human Playtesting | **HQ** #687 | Independent producer paths Research/B01-13/P17S/** and one dedicated B01-13 W21 workflow. **Purpose**: playable garage repair decision UX / safety edge-case rehearsal, not duplicate of W20 passive economy cost-comparator #676 or B01-10 mathematical forecast #686.

## A real runnable UX feature
This is a **self-contained fully offline interactive HTML garage**, with a shared executable deterministic action state machine, so the user can choose which 0-HP modules to queue, decide whether to consume the first free quota or pay for subsequent requests (price 70 example; wallet starts at 100), then simulate a subsequent win or loss. Five physical named modules per scenario (LIGHT and HEAVY), 0HP vs ready vs REBUILDING/unavailable, role-specific criticality. The UI shows persistent within-session local spent/free state, pending queue count, simulated wins, event IDs, genuine no-repair-until-next-win behavior, duplicate-event denial, the distinction between one free per win and one per run, a loaner counterfactual with a conspicuous **NOT APPROVED** banner, an explicit softlock warning if destroyed critical drive prevents normal deployment, reset controls, and live accessible feedback.

The event IDs are **DEMO-xxx LOCAL INVENTED ONLY**. Duplicate local ID detection is an *ephemeral pedagogical UX state* — it is NOT an authenticated idempotent game ledger, durable watermark or multi-instance security proof. We intentionally do NOT implement or modify B01-22's real single-owner authority, actual CAS, same-host two-process concurrency, original #510/556/596/647 or real Unity R3 save. We also do NOT silently invent an approved fallback. The purple loaner buttons are **counterfactual, explicitly unapproved**, only enabled if simplified criticality says a normal fight cannot begin, and only one try. Loss leaves existing queued rebuild pending/locked. A real project decision for prevention of a deploy softlock belongs to B01-10, owner #154 and native integration.

### Play it
Generate the HTML with Node 20+ **outside the Git worktree** (generated files are exclusive-create, no overwrite, private 0600 where supported):

    node Research/B01-13/P17S/rebuild_choice.mjs --output /private/b01-13-w21-garage.html

Open the single HTML file in a browser. It has no remote JS, fonts, fetch, network, cookies, localStorage, participant data or telemetry. An original hash-pinned inline script controls genuine UI interactions (same original code that is exported as the source action model). New results are ephemeral; refresh/reset clears everything. The GitHub workflow also creates and uploads an HTML ZIP artifact for direct download without Node; those artifacts are evidence of physical generation, **not** proof a human actually played it.

Main UX sequence:
1. LIGHT starts deployable even when optional gun/armor/battery are destroyed. Choose which 0HP piece uses the free slot; the next request costs 70; the third may be unaffordable. Queued items stay 0 HP and cannot be mounted.
2. LOSS leaves previous requests in REBUILDING; a distinct WIN completes exactly those earlier requests, never a same-event newly created one.
3. Replay the exact local event ID: exercise rejects the replay. A fresh win can refresh the free coupon only under PER_WIN.
4. Change to HEAVY, whose only drive is critically destroyed. Normal Win/Loss simulation is disabled to make the circular dependency visible. A separate purple unapproved loaner simulation may show one hypothetical path out (or its failure); this is not a shipping recommendation.
5. Switch PER_RUN and compare the persistent used coupon after a win; no arbitrary browser refresh/free-exploit is implemented because state is intentionally not persisted or authoritative.

## Specific W20 → W21 correction and decision semantics
B01-10 [#686 exact R2](https://github.com/C8LyNn/RoboRougelike/pull/686) independently confirmed earlier same-victory repair modeling violated the **NEXT** win gate and added hypothetical critical-module deployment guard. W21 UX models *only original already-queued* parts becoming ready after a future WIN; defeats, reload-like UI resets, destroyed essential/mobility components, and simulated loaner are separately explained. A reset is a **new synthetic exercise**, not a durable Save reload. No customer wallet or real economy state changes.

**Owner hypothesis:** one owner-global free rebuild reservation per authenticated unique victory epoch, subsequent quote paid, per-run cadence alternative unapproved. Initial bootstrap free is a **testable assumption**, not a release decision. Price 70 Scrap/100 initial are example input only. The real original ECON #252 was SOURCE CONTRACT RED for stale restore and two service instances (#620). An isolated new ECON authority source candidate [#673](https://github.com/C8LyNn/RoboRougelike/pull/673) exists, but real-world selected host integration and native Unity G1 are **NOT_RUN**. This lab's local event-ID list DOES NOT fix the source threat, and is never a trusted Save counterpart.

### Exact frozen refs (read-only information only)
- #510 gameplay d94400d95d13db7f27e1533df05c9292d5380431;
- #556 Save 33094f154c7b91119b36d58aab285e6e83c5de57;
- #596 historical bridge efb2b06eafedab5469b57f163152d96aca3c5915;
- #647 owner-final R4 09c1809b73324a6d31c13e5d0143de62ac1e3ca6, P0 component-only destruction SOURCE RELEASE RED (#670); successor #690 exists but independently reviewed;
- #252 ECON 12572117a3dce369257feabc57021cde58023369;
- losing #652/#644/#604 ALT_HOLD;
- Save API #681 selected by owner, #682 ALT_HOLD; B01-13 does not modify either.

No original C# sources, frozen branches, protected base, Unity scenes/Assets, Codex-owned paths or receiver files touched.

## Real source tests and their limits
The dedicated workflow independently checks 5 physically original scoped source/test/docs/workflow Git blobs plus byte lengths and SHA256 (manifest excludes itself), then executes **54 original Node 20/22 tests on Ubuntu24.04 and Windows2025** (matrix 4 jobs), and finally produces/upload actual isolated offline playable HTML under GitHub runner temp. Tests include deterministic action transitions and counterexamples, exactly one free event, paid debit and insufficient funds, id replay, multi-loss pending, semantic NEXT-win, critical 0HP circularity, hypothetical single loaner success/failure, strict ID/part data, private disk output/exclusive create, hash-pinned CSP and actual original inline JS executed via Node vm fake DOM with buttons, simulated battles/replay/restart/mode (fake DOM ≠ actual browser). **Only actual exact-SHA completed CI counts as PASS**. This does not prove any Unity gameplay, security against real rollback, actual UX accessibility or studies.

    python Research/B01-13/P17S/verify_package.py
    node --test Research/B01-13/P17S/test_rebuild_choice.mjs

## Handoff and six-stage gate
**B01-22/B01-10** receive the player-facing UX/softlock hazard and cadence distinctions; their authority/real price and whether to approve a loaner are separate decisions. **B01-40** checks source integrity G0 and non-destructive rollback; **B01-60** may perform independent actual browser/native checks where authorized; **B01-00** can schedule properly consented future human tests. Actual participants=0; source-only tests != human study; NATIVE G1 NOT_RUN, LIVE ECON/Save NOT_VERIFIED, Codex ACK NONE, G2 HOLD, no merge. To roll back, discard the isolated unmerged Draft and local disposable HTML, not original repository data.
