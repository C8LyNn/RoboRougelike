# B01-12 W26–W29 / UX.U4 v2 / exact-source research receipt

**THIS IS AN OFFLINE UX RESEARCH PROTOTYPE, NOT A UNITY IMPLEMENTATION OR G2.** No modifications to Codex, CHL #285, PARTSIZE #286, Assets, main, develop, or runtime.

- Authoritative v2 HTML: [Research/B01-12/W17/index.html](../W17/index.html)
- **Tested v2 HTML Git blob SHA1:** `bc17deba3c96307c8179a0ff564b26b3855287ce` (exact LF-terminated source).
- **Research branch:** `research/b01-12-w17-garage-chl-ux`, v2 commit `3dd37d0b63cb647655cccdab7a9a7ac4e9db3179`.
- **Previous v1 Git blob:** `65b84bdfded69eb56771fc529bbdde33ebab17` is NOT correct; actual previous v1 blob **`65b84bdfded69eb56771fc529b9a4c5cabfd77a5`**. The branch history remains intact.
- **Pinned PARTSIZE input:** #286 `be2be0e4802bc3f450179901f9ae91c6d84bcb5b`.
- **Pinned CHL input:** #285 `077e692785aff2990a2d72a80fd1db62ac57b0e8`; older queue CHL `f4ca318...` obsolete for native/source readiness.
- **Reviewed upstream:** B01-11 independently assessed v1 in #296 [comment 6440580983](https://github.com/C8LyNn/RoboRougelike/issues/296#issuecomment-6440580983), and earlier benchmark #292. Its structural 13 assertions are NOT browser/user/Unity testing.

## Result
- Original W17 Chromium regression **28/28 PASS** (rerun against v2 final HTML).
- New W26-v2 Chromium treatment tests **25/25 PASS**.
- B01-11 v1 review-specific creator Chromium tests **17/17 PASS**. Review checks: no false green functional-ready claim, unknown insertion requires explicit mock-fixture checkbox, P0 socket/physical reason separation, compact C working "Dlaczego?" disclosure, explicit CHL rejection banner, selected FAIL code + primary Retry, per-attempt ID and rig locking, modal keyboard focus trap/Escape restoration.
- **70 browser assertions PASS / 0 FAIL** on current exact v2 source. Browser engine actual Chromium headless + Playwright, 1440×960 and 390×840, loaded via `page.set_content` after environment file/http navigation policy rejected. This is **producer-only UX mock QA**.
- Six current v2 and five regression screenshots plus exact three Python test files, logs, SHA256-per-file manifest, standalone HTML exist in the conversation-produced `B01-12-W26-V2-EVIDENCE.zip`; ZIP is NOT a GitHub-hosted artifact. Research source itself IS on GitHub and is independently accessible.
- **Unity Edit/Play/PhysX: NOT_RUN**. True 3 Light +3 Heavy motor runs: **NOT_RUN**. Real user study 8/8: **NOT_RUN**. **Independent B01-11 browser re-review of v2: NOT_RUN** at this receipt. G1/G2 handoff NOT_APPLICABLE.

## Changes aligned to B01-11 UX-01 to UX-05
1. Free logical grid appears blue dashed, with visible **three authority layers** `SIATKA/FIZYKA/SOCKET` and `MOUNT_AUTHORITY: UNVERIFIED` sidebar. Never claim true green physics approval from source primitive size.
2. UNKNOWN reason codes `UNMEASURED_PHYSICAL_ENVELOPE`, `INCOMPLETE_EXTERNAL_BROADPHASE`, `PHYSICS_QUERY_UNVERIFIED`, `TYPED_SOCKET_UNVERIFIED`, `PHYSICAL_PENETRATION` are selectable **injected fixture strings**, not engine outputs. Physical unmeasured 11 types stay unmeasured.
3. UNKNOWN parts blocked by default; user can opt in to **unverified research prop** insertion; mock count visibly tracks them and does not certify mounting. Simulated functional completeness is never green runtime readiness.
4. A=minimal specific cause, B=technical measured-source primitive and separated envelopes/sockets, C=working `Dlaczego?` disclosure, causal next action. All three have access to reason/action.
5. CHL mock rejected skip/reverse/wheel-only events show prominent, screen-reader-announced reason; failure reason fixture (FALL/TIMEOUT/COLLISION); `LIGHT-01/HEAVY-01` result attribution; Retry primary for terminal trial, rig selection only at READY. No native gate/bridge clearance inferred.
6. Dialog explicit focus entry, trapped Tab, Escape and focus return.

**DEPENDENCY:** `UX.U3 B01-11 #296 comment 6440580983 DONE` → `UX.U4 B01-12 v2 DONE (browser-producer-only)` → `UX.U5 B01-11 independent exact-v2 review PENDING` → HQ decision PENDING. Detailed scenarios in [STUDY_CASES.md](STUDY_CASES.md).

**NEXT:** B01-11 reviews exact `bc17de...` blob and explicitly reports ACTUAL browser vs structural vs human statuses; B01-00 decides preference only after evidence. No automatic invoking other agent by this comment.
