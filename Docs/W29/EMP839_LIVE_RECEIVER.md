# W29 — EMP selected source receiver, not yet promoted as native GAME PASS

Source baseline live playable W27 [#824] ed8cf4bcc131688b9a93968a89683d2496586683. EXACT producer [#839] fea000fe0a706df2b52b6f295bdb3135ba1f9384. Parent source identity checked on **all three original files**: existing W27 C# blobs were byte-identical to #839 parent e03d36f2. This child stages ONLY the 3 byte-exact Git blobs from #839, not the producer branch or #849 QA harness. 0 new GUID and no new Save/ECON/writer.

Native isolated evidence: [QA #849](https://github.com/C8LyNn/RoboRougelike/pull/849), original Unity6000.3.14f1 12/12 EditMode+22/22 PlayMode, actual Windows Player detached -> no energy/no cooldown, reattached -> 7 energy cost and actual EmpReceiver disruption; harness-only QA commit 0eb1bf... is NOT the live game. API-controlled, no human manual test. These outcomes are valid for original #839 candidate, NOT for this freshly composed W29 target HEAD.

Game integration source behavior: RRLDeviceBinding checks current real mounted part HP, owner/body/structural/EMP state at direct TryPulse; same real radial EMP physics/occlusion/team filtering and existing R3 binding, no projectile substitution. No extra energy/cooldown when disabled.

Gates: EXACT SOURCE TRANSPLANT VERIFIED (3 Git blobs); independent target-specific B01-40/61 source/G0 review PENDING; target native Unity compile/EditMode/PlayMode/Player NOT_RUN; EMP_GAME_INTEGRATED=SOURCE_STAGED_NOT_GAME_ACCEPTED; merge/release NO.

NEXT: B01-60 real target HEAD full WPN006 original 22+ PlayMode, W27 weapons, existing actual Wasteland EMP opponent plus Windows Player, store original Editor.log/NUnit XML/Player.log/ZIP SHA. Only after green QA and authorization may Codex integrate into W27/game release. Other B01 sources #827/#810/#843/#814/#845 not pulled. Rollback discard isolated child PR, parent unchanged.
