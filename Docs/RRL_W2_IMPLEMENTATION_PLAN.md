# RRL-W2 implementation and QA plan

> Execute inline using superpowers:executing-plans; one fresh final reviewer before publication.

**Goal:** Finish PR #1 as a playable, verified rewards/economy/density/debris update.
**Architecture:** Preserve existing R1/R3/R4/R5/R6/MOV integration; repair its boundary contracts. Run state owns reward application, salvage owns inventory/scrap settlement, model owns repair quotes/payment, one encounter definition supplies actual spawn/threat data, debris owns temporary separation lifecycle.
**Tech:** Unity 6000.3.14f1, C#, NUnit/Unity Test Framework, Windows build, native Computer Use for observed gameplay.
**Spec:** User RRL-W2 request plus `Docs/RRL_W2_REWARDS_DEBRIS_QA.md`.

## Constraints
- Existing PR #1 branch `feature/unified-rewards-debris-density`, target develop; no main changes, no merge/force push/destructive Git.
- Rewards: 0–3 total, three distinct offered bonuses plus real non-armor/non-core loot, 0HP storage/rebuild; one claim and one screen. Final victory resolves salvage to Won without a redundant bonus screen.
- Damage repair ceil(price * missing HP * .35), min1; intact0, destroyed full price/100%HP. Quotes and sufficient funds across single/inventory/bulk/emergency.
- Nonboss base1/2/3 becomes2/3/4; boss stays1. No additional HP/damage scaling or invented elite.
- Immediate mass-independent outward/upward ejection, temporary nonblocking colliders/gravity, restore/pause/cleanup, preserve structure/MOV.
- All raw logs/XML in ignored Artifacts/Local; final report and sanitized evidence on PR, no new MAP/VFX packages.

## Review focus
1. Reward mutations/duplicate/invalid selection cannot partially debit/award or double-apply; test every 0–3 composition and final encounter.
2. Free emergency rebuilds/near-100%-HP exploits and insufficient-funds partial changes; independently derive literal costs and repeat calls.
3. Spawn/threat/UI disagreement or extra variant inheriting elite; verify actual count/normal type, catalog budget and all arena placements.
4. Same-frame shielding before Start, original disabled colliders, owner overlap, pause or destroyed references; test live projectiles and intact detached chunks.
5. Dense reward pages overflow, omitted source/type/condition or hidden confirm; inspect native screenshots at 1280x720 and 1920x1080 with many entries, plus Windows player.

## Tasks
- [x] Baseline targeted model tests/import/compile; read existing integration and reproduce defects with tests.
- [x] Reward and repair tests RED; fix model/controller settlement and UI, keep legacy fixtures meaningful under new intended rules. GREEN targeted suite.
- [x] Density/threat and real physics tests RED; fix actual roster accounting and immediate debris lifecycle, pause/cleanup/projectiles. GREEN targeted suite.
- [x] Adapt old expectations (do not delete cases), full EditMode, full PlayMode, Windows build, observed gameplay loop/native screenshots and visual debris tuning.
- [x] Fresh whole-PR review, verify/fix material findings RED→GREEN, audit source/meta/secrets and clean refs; report READY/BLOCKED with actual evidence; push existing PR branch only.

## Final gate outcome

Implementation `c16c6aeb81ec20e44cf5b3ef171a1ad9bf14e753`: EditMode385/385, PlayMode176/176, Windows retail+QA PASS; all old cases retained. Native operator + input pilot completed6/6 at1920 before the later garage-only fix; final build separately passed3 physical fights, industrial-garage framing/orbit and range/return at1280. See final QA report for exact evidence, independent-review boundary, engine UIAutomation shutdown failure and cloud activation status. Report/push tasks are executed as delivery work; an overall BLOCKED result is not a claim that the engine failure is fixed.
