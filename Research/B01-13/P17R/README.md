# B01-13 — PASS17 WAVE20 — REBUILD-ECON-LAB-001/R1

Producer/owner: B01-13 HUMAN PLAYTESTING. HQ dispatch #668. Isolation: Research/B01-13/P17R/** and dedicated source CI workflow. Dependencies: Node.js >=20, Python 3 for integrity verification. No Unity packages, no Save/Load writes.

## What this delivers
**Actual executable game-balance hypothesis simulator AND single-file interactive offline HTML comparison.** It models three original technical scenario fixtures with destroyed robot parts, verified-model win events and fixed starting Scrap: LIGHT_SHORT (2 victories, 3 affected parts), HEAVY_STREAK (2 victories, 5 affected parts), TWO_RUNS (3 victories across 2 distinct runs). Two alternative RESEARCH assumptions are executed from the same fixture and player currency budget:

1. PER_VICTORY: ONE owner-global free reservation triggered by each **unique** victory event, even if several rebuilds are needed in that win; other reservations cost the user-selected price.
2. PER_RUN: ONE owner-global free reservation **in the entire run** (used when the first qualifying victory has an affected part), no additional free reservation from later victories in that run.

Default 70 Scrap / paid repair and 250 initial Scrap are **provisional example inputs, not approved gameplay tuning**. HTML offers responsive scenario selector, price 20–150 and Scrap budget 0–600 sliders, real computed spend/free/paid/unaffordable counts, event-by-event comparison table and a labeled bar. All status feedback is live to assistive technologies. The payoff is concrete comparison of **accessibility/scarcity tradeoffs and economy friction**, not an engineered replacement for B01-22's authoritative economy.

Different from older B01-13 Save/Load technical observer reports W12–W18 and W19 visual damage-cue tutorial #655, this is **first W20 economy hypothesis UX tool + executable mathematical budget model**, explicitly addressing owner's #668 direction. It uses *no original frozen ECON game source*; source sim was written independently as reference hypotheses. Does not author or claim a real anti-rollback fix. B01-22 is the sole owner of its production authority/transaction implementation.

## Critical truth boundary
Frozen selected canonical game #510 SHA d94400d95d13db7f27e1533df05c9292d5380431; Save #556 SHA 33094f154c7b91119b36d58aab285e6e83c5de57; original host R2 #596 SHA efb2b06eafedab5469b57f163152d96aca3c5915; original ECON producer #252 SHA 12572117a3dce369257feabc57021cde58023369. Original selected R3 #642 remains frozen, #644 and #604 ALT_HOLD. R4 #647/#652 selection is B01-23/62 responsibility; neither imported or changed.

The original ECON #252 (see independent B01-22 PR #620) has **confirmed source-contract RED for duplicated free reservations with two service instances and stale restore victory replay in a declared .NET surrogate**. This independent model deliberately **rejects duplicate event IDs in its input** to prevent misleading balancing math. That is **not** a demonstration or fix of real game's cross-instance anti-rollback/transaction behavior and does not prove a real Unity host works. The simulator processes one deterministic virtual authority and assumes valid authenticated wins; that assumption is **not a security guarantee**. The model treats requests as attempts at the moment of a victory; it does not implement queue persistence, real unlock timing, physical GUID lifecycle, repair delay, actual Scrap earning, deterministic economy exploits, host crash safety, or canonical run-ending behavior. No claim that budget 250 or paid cost 70 are real prices.

**Actual human participants: 0.** UI comprehension/behavior preference, encounter and repair difficulty, price elasticity, game UX, native Unity and PhysX, actual Save/Load, authoritative cross-process transactions or losses **NOT_RUN / NOT_VERIFIED**. Never infer adoption, fairness or winning hypothesis solely from simulation.

## Use offline
Using Node.js 20+ on an authorized local machine, create HTML **in a writable private directory OUTSIDE a Git checkout**, no overwrite:

    node Research/B01-13/P17R/economy_lab.mjs --output /private/b01-13-economy-lab.html

Open generated HTML directly in the browser without internet. The single file embeds only original deterministic fixtures/CSS/JS; no network requests, tracking, WebSockets, cookies, localStorage, user input names, untrusted source URLs or telemetry. CSP authorizes only one known SHA256 inline script. The output is private 0600 and fsync where filesystem support permits. All scenario fixtures are validated: distinct event IDs, strict run order, no duplicate part IDs per run, valid part and victory ID syntax, no user-authored text. Direct Node API for original model:

    import { SCENARIOS, simulate, compare } from './economy_lab.mjs';
    const result = compare(SCENARIOS[0], { price:70, budget:250 });

Illustrative example at 70 Scrap / 250:
- LIGHT_SHORT PER_VICTORY: free 2, paid 1, spent 70, blocked 0.
- LIGHT_SHORT PER_RUN: free 1, paid 2, spent 140, blocked 0.
- HEAVY_STREAK PER_VICTORY: free 2, paid 3, spent 210, blocked 0.
- HEAVY_STREAK PER_RUN: free 1, paid 3, spent 210, blocked 1. **Important:** the alternative can reduce available rebuilds without increasing actual spending when budget is exhausted.

These are expected mathematical fixture results, NOT measurements of human frustration, session completion, game health or production repair prices.

## Test plan
The dedicated source-only workflow runs 48 original model/negative/CLI/client-DOM-simulation tests on **Ubuntu24.04 / Windows2025 × Node20 / Node22**. Actual tests validate strict victory/part dedup, unique per-win vs per-run cadence, budget exhausted cases, physical file writes/no overwrite/Git directory refusal, original HTML CSP, and executing the ORIGINAL embedded JS in a Node-vm simulated DOM (NOT a real browser or Unity). Each runner first checks five owned files' exact physical byte lengths, git blob SHA1 and prints actual SHA256; exact source evidence is then published as GitHub Actions logs. After source test success, workflow writes a playable single-file HTML to GitHub runner temporary directory (NOT repo), uploads it as a downloadable artifact per OS/node. No external UI runtime or Unity license required. Only exact-head completed CI logs and downloadable artifacts count as real PASS.

    python Research/B01-13/P17R/verify_package.py
    node --test Research/B01-13/P17R/test_economy_lab.mjs

## Suggested future manual playtest (not executed)
When separately authorized, compare players’ **expected repair affordability** before they see a quote, ability to explain one free per-win vs one per-run after two wins, and frustration with a blocked repair. Use equivalent initial chassis/encounter damage conditions, randomize experimental cadence order and collect explicitly consented, anonymized feedback. Never present synthetic fixtures as completed participant sessions. Actual production tuning choice remains B01-10/B01-22/owner decision.

## Ownership and rollback
B01-22 + B01-10: review economic assumptions, compare per-victory vs per-run fairness, coordinate price/repair eligibility decisions. B01-60: source/balance independent QA and later true browser/native if authorized. B01-40: physical G0 provenance, safe non-destructive intake. B01-00: schedule real player study and integrator reception. No owner authorization for source merge, canonical host installation, economy singleton fix or game release. Roll back by abandoning isolated unmerged Draft, and remove exported private synthetic HTML under retention policy. Current integration ACK none. Six lifecycle stages must be reported separately.
