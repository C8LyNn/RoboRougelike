# B01-22 / P17-W23 / ECONCOREGUARD005: trusted Core and encounter preflight

**New isolated runtime:** CoreGuardedDeploymentProbeV5 implements the already-selected W22 ITrustedDeploymentProbeV4 interface. No competing session lock, Save/Load, currency authority, repair queue or additional live owner. Parent W22 [Draft PR #716](https://github.com/C8LyNn/RoboRougelike/pull/716) at immutable source SHA 1135ca4f5d685f6188b7c221c7237a449e21f226; predecessor ECON V3 [#694](https://github.com/C8LyNn/RoboRougelike/pull/694), V2 #673, V1 #252 untouched. The selected #729 and storage #712/#669 remain separate because .owner vs .lease is NOT resolved here.

## Functionality / actual trust requirements

Only an authorized future canonical private #510 game adapter may implement ICanonicalCoreAndEncounterProbeV5. It must derive actual designated Core physical GUID, owner/run/generation, Core functionality, real CanDeploy, a genuine eligible next challenge and physical 0HP blockers in ONE main-thread-quiesced observation while the SINGLE selected host session owns the physical lease. A UI boolean is NOT a trusted input. Do not infer Core identity from a part name, Kind or client model.

Adapter validates designated Core is physically owned; at Health=0 it cannot be functional or deployable, and must appear among its actual blocking physical GUIDs. Duplicated/unknown blockers, foreign owner/run, stale generation, claimed alive 0HP Core, claimed destroyed healthy Core, missing Core evidence and exceptions fail closed. Adapter hands defensive copy to provider, passes only detached facts to W22 full owner snapshot double-read and equal-generation fork check. Output is **ADVISORY, NEVER a combat/deployment/loaner token**: AuthorizesCombatStart is false even for NormalChallengeReady. A subsequent world edit invalidates that advice; actual combat must recheck under a held trusted owner fence.

No Core alive or no genuine next challenge => no legal one-time borrowed robot under provisional #154. The loaner remains NOT APPROVED, no temporary free healing or counterfeit victory is issued. This package reports risk; it does not implement policy C. Other essential 0HP modules may block normal deploy even with healthy Core.

## Evidence / boundaries

Dedicated two-OS CI compiles the genuine unchanged W21 V3 and W22 V4 C#9 runtime, plus this new V5, and REUSES the frozen W22 C# original source test fixture (rather than cloning a competing harness). Actual test file uses synthetic disposable OS JSON, not live Unity, world Rigidbody, #510 Rig[]/AtomicDockLedger, #729 canonical Save, #712 trusted storage or genuine player battle. Includes forged dead-Core claim, false CanDeploy, no eligible next challenge, mismatched owner/run/physical IDs, double-observer, same-gen changed ledger during probe, immutable wallet/pending and separate cold CLR process. G1 Unity6000.3.14f1 **NOT_RUN** until actual owner-opt-in Editor.log/NUnit XML; human participant count 0.

**Balance model only:** 12 distinct wins with two newly destroyed parts after each fight = 24 queued, 22 settled, last two pending; temporary quote 70 Scrap yields 840 vs 1,610 Scrap for per-win vs per-run free cadence. Nothing approved by owner yet. Consult B01-10 [#686](https://github.com/C8LyNn/RoboRougelike/pull/686) for Core/encounter assumptions and B01-13 [#722](https://github.com/C8LyNn/RoboRougelike/pull/722) for bounded recovery-route analysis.

## Handoff and rollback

B01-23/62: connect selected **ONE** #729 + #712/#669 physical owner, provide genuine Core and CanDeploy encounter proof, don't import ALT_HOLD. B01-61/41: independent exact-source hostile input/fork QA. B01-40: formal physical additive manifest/GUID G0 and safe scratch. B01-60: real opt-in Unity Windows G1 or NOT_RUN. B01-10/13: measured balance and #154 policy/human tests, currently 0 participants. B01-00: decide emergency loaner. No Codex ACK, integrator feedback NOT_MEASURED.

Rollback: close unmerged Draft or discard newly isolated scratch. DO NOT delete, modify or restore an actual pending owner Save or any frozen producer source. Do not install without independent source G0 and native G1 acceptance. G2 HOLD.
